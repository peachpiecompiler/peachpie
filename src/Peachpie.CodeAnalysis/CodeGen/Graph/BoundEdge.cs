using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.PooledObjects;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class Edge : IGenerator
    {
        /// <summary>
        /// Generates or enqueues next blocks to the worklist.
        /// </summary>
        internal abstract void Generate(CodeGenerator cg);

        void IGenerator.Generate(CodeGenerator cg) => this.Generate(cg);
    }

    partial class SimpleEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            if (cg.IsDebug && this.PhpSyntax != null)
            {
                cg.EmitSequencePoint(this.PhpSyntax);
            }
            cg.Scope.ContinueWith(NextBlock);
        }
    }

    partial class LeaveEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            // nop
        }
    }

    partial class ConditionalEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            Contract.ThrowIfNull(Condition);

            // !COND?T:F -> COND?F:T
            bool isnegation = this.Condition.IsLogicNegation(out var negexpr);
            var condition = isnegation ? negexpr : this.Condition;

            if (TrueTarget == FalseTarget)
            {
                // condition always results in the same code flow
                cg.EmitSequencePoint(this.Condition.PhpSyntax);
                cg.EmitPop(cg.Emit(this.Condition));
            }
            else if (IsLoop) // perf
            {
                cg.EmitHiddenSequencePoint();
                cg.Builder.EmitBranch(ILOpCode.Br, condition);

                // {
                cg.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }

                // if (Condition)
                cg.EmitHiddenSequencePoint();
                cg.Builder.MarkLabel(condition);

                cg.EmitSequencePoint(this.Condition.PhpSyntax);
                cg.EmitConvert(condition, cg.CoreTypes.Boolean);

                cg.Builder.EmitBranch(isnegation ? ILOpCode.Brfalse : ILOpCode.Brtrue, TrueTarget);
            }
            else
            {
                // if (Condition)
                cg.EmitSequencePoint(this.Condition.PhpSyntax);
                cg.EmitConvert(condition, cg.CoreTypes.Boolean);

                cg.Builder.EmitBranch(isnegation ? ILOpCode.Brtrue : ILOpCode.Brfalse, FalseTarget);

                // {
                cg.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }
            }

            cg.Scope.ContinueWith(FalseTarget);
        }
    }

    partial class TryCatchEdge
    {
        /// <summary>
        /// Whether to emit catch and finally bodies outside the TryCatchFinally scope.
        /// This allows to branch inside catch or finally from outside,
        /// or branch outside of try without calling finally (required for yield and return functionality).
        /// </summary>
        public bool EmitCatchFinallyOutsideScope { get; internal set; }

        internal override void Generate(CodeGenerator cg)
        {
            EmitTryStatement(cg);

            //
            cg.Scope.ContinueWith(NextBlock);
        }

        void EmitTryStatement(CodeGenerator cg, bool emitCatchesOnly = false)
        {
            // Stack must be empty at beginning of try block.
            cg.Builder.AssertStackEmpty();

            // mark label before "try" block,
            // used by generator state maching and awaits eventually
            cg.Builder.MarkLabel(this);

            // IL requires catches and finally block to be distinct try
            // blocks so if the source contained both a catch and
            // a finally, nested scopes are emitted.
            bool emitNestedScopes = (!emitCatchesOnly &&
                //(_catchBlocks.Length != 0) && // always true; there is at least one "catch" block (ScriptDiedException)
                (_finallyBlock != null && !EmitCatchFinallyOutsideScope));

            // finally block not handled by CLR
            var nextExtraFinallyBlock = cg.ExtraFinallyBlock;

            cg.Builder.OpenLocalScope(ScopeType.TryCatchFinally);

            cg.Builder.OpenLocalScope(ScopeType.Try);
            // IL requires catches and finally block to be distinct try
            // blocks so if the source contained both a catch and
            // a finally, nested scopes are emitted.

            //_tryNestingLevel++;
            if (emitNestedScopes)
            {
                EmitTryStatement(cg, emitCatchesOnly: true);
            }
            else
            {
                // jump table for nested yield or await
                EmitJumpTable(cg);

                // remember finally block
                if (_finallyBlock != null && EmitCatchFinallyOutsideScope)
                {
                    cg.ExtraFinallyBlock = _finallyBlock;
                    cg.ExtraFinallyStateVariable ??= cg.GetTemporaryLocal(cg.CoreTypes.Int32, longlive: true, immediateReturn: false);
                    cg.ExceptionToRethrowVariable ??= cg.GetTemporaryLocal(cg.CoreTypes.Exception, longlive: true, immediateReturn: false);
                }

                // try body
                cg.GenerateScope(_body, (_finallyBlock ?? NextBlock).Ordinal);

                //
                if (NextBlock != null && NextBlock.FlowState != null) // => next is reachable
                {
                    cg.Builder.EmitBranch(ILOpCode.Br,
                        (_finallyBlock != null && EmitCatchFinallyOutsideScope)
                        ? _finallyBlock // goto finally
                        : NextBlock     // goto next
                    );
                }
            }

            //_tryNestingLevel--;
            // Close the Try scope
            cg.Builder.CloseLocalScope();

            if (!emitNestedScopes)
            {
                EmitScriptDiedBlock(cg);

                //
                foreach (var catchBlock in _catchBlocks)
                {
                    EmitCatchBlock(cg, catchBlock);
                }

                // emit default catch block that continues to "finally" before rethrow;
                // only if EmitCatchFinallyOutsideScope
                EmitDefaultCatchBlock(cg);
            }

            if (!emitCatchesOnly && _finallyBlock != null && !EmitCatchFinallyOutsideScope)
            {
                cg.Builder.OpenLocalScope(ScopeType.Finally);
                cg.GenerateScope(_finallyBlock, NextBlock.Ordinal);

                // close Finally scope
                cg.Builder.CloseLocalScope();
            }

            // close the whole try statement scope
            cg.Builder.CloseLocalScope();

            // emit catch and finally blocks outside the try scope:
            if (!emitNestedScopes && EmitCatchFinallyOutsideScope)
            {
                var nextBlockOrExit = NextBlock?.FlowState != null ? NextBlock : cg.ExitBlock.GetReturnLabel();
                var continuewith = _finallyBlock ?? nextBlockOrExit;

                cg.Builder.EmitBranch(ILOpCode.Br, continuewith);

                //
                foreach (var catchBlock in _catchBlocks)
                {
                    cg.GenerateScope(catchBlock, NextBlock.Ordinal);
                    cg.Builder.EmitBranch(ILOpCode.Br, continuewith);
                }

                // forget finally block
                cg.ExtraFinallyBlock = nextExtraFinallyBlock;

                //
                if (_finallyBlock != null)
                {
                    Debug.Assert(cg.ExtraFinallyStateVariable != null);

                    // emit finally block
                    cg.GenerateScope(_finallyBlock, NextBlock.Ordinal);
                    cg.Builder.AssertStackEmpty();

                    // several "finally" states (cg.ExtraFinallyStateVariable):
                    // - 0: none; continue to NextBlock
                    // - 1: return; continue to nextExtraFinallyBlock, eventually EmitRet
                    // - 2: exception; rethrow exception (cg.ExceptionToRethrowVariable)

                    var stateloc = cg.GetTemporaryLocal(cg.ExtraFinallyStateVariable.EmitLoad(cg.Builder), immediateReturn: true);
                    cg.Builder.EmitLocalStore(stateloc);
                    Debug.Assert(stateloc.Type.TypeCode == Microsoft.Cci.PrimitiveTypeCode.Int32);
                    
                    cg.Builder.EmitIntegerSwitchJumpTable(
                        new[]
                        {
                            new KeyValuePair<ConstantValue, object>(ConstantValue.Create((int)CodeGenerator.ExtraFinallyState.None), nextBlockOrExit),
                            new KeyValuePair<ConstantValue, object>(ConstantValue.Create((int)CodeGenerator.ExtraFinallyState.Return), nextExtraFinallyBlock ?? cg.ExitBlock.GetReturnLabel()),
                            //new KeyValuePair<ConstantValue, object>(ConstantValue.Create((int)CodeGenerator.ExtraFinallyState.Exception), nextExtraFinallyBlock ?? cg.ExitBlock.GetRethrowLabel()),
                        },
                        nextExtraFinallyBlock ?? cg.ExitBlock.GetRethrowLabel(), // ExtraFinallyState.Exception
                        stateloc,
                        Microsoft.Cci.PrimitiveTypeCode.Int32);
                }
            }
        }

        void EmitScriptDiedBlock(CodeGenerator cg)
        {
            // handle ScriptDiedException (caused by die or exit) separately and rethrow the exception

            var il = cg.Builder;

            // Template: catch (ScriptDiedException) { rethrow; }
            il.AdjustStack(1); // Account for exception on the stack.

            il.OpenLocalScope(ScopeType.Catch, cg.CoreTypes.ScriptDiedException.Symbol);
            il.EmitThrow(true);
            il.CloseLocalScope();
        }

        void EmitDefaultCatchBlock(CodeGenerator cg)
        {
            if (!EmitCatchFinallyOutsideScope || _finallyBlock?.FlowState == null)
            {
                return;
            }

            // emit default catch block that continues to "finally" before rethrow;
            
            var il = cg.Builder;

            // Template: 
            // catch (Exception ex) {
            //   ExceptionToRethrow = ex;
            //   ExtraFinallyState = 2;
            //   goto _finally;
            // }

            il.AdjustStack(1); // Account for exception on the stack.

            il.OpenLocalScope(ScopeType.Catch, cg.Module.Translate(cg.CoreTypes.Exception.Symbol, null, cg.Diagnostics));

            // ExceptionToRethrow = ex;
            cg.ExceptionToRethrowVariable.EmitStore();

            // ExtraFinallyState = 2;
            il.EmitIntConstant((int)CodeGenerator.ExtraFinallyState.Exception); // rethrow state
            cg.ExtraFinallyStateVariable.EmitStore();

            // .leave _finally;
            il.EmitBranch(ILOpCode.Br, _finallyBlock);

            il.CloseLocalScope();
        }

        void EmitTypeCheck(CodeGenerator cg, BoundTypeRef tref)
        {
            var il = cg.Builder;

            // STACK : object

            if (tref.ResolvedType.IsErrorTypeOrNull())
            {
                // Template: filter(Operators.IsInstanceOf(<stack>, type))
                tref.EmitLoadTypeInfo(cg, false);
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.IsInstanceOf_Object_PhpTypeInfo)
                    .Expect(SpecialType.System_Boolean);
            }
            else
            {
                // Template: filter (<stack> is Interface)
                il.EmitOpCode(ILOpCode.Isinst);
                cg.EmitSymbolToken(tref.ResolvedType, null);
                il.EmitNullConstant();
                il.EmitOpCode(ILOpCode.Cgt_un); // value > null : bool
            }

            // STACK: i4 (boolean)
        }

        void EmitMultipleTypeCheck(CodeGenerator cg, ImmutableArray<BoundTypeRef> trefs)
        {
            var il = cg.Builder;

            // STACK : object

            var lblFound = new NamedLabel("filter_found");
            var lblEnd = new NamedLabel("filter_end");

            for (int i = 0; i < trefs.Length; i++)
            {
                il.EmitOpCode(ILOpCode.Dup);

                // (ex is T) : bool
                EmitTypeCheck(cg, trefs[i]);

                // if (STACK) goto lblFound;
                il.EmitBranch(ILOpCode.Brtrue, lblFound);
            }

            il.EmitOpCode(ILOpCode.Pop);    // POP object
            il.EmitBoolConstant(false);
            il.EmitBranch(ILOpCode.Br, lblEnd);

            il.MarkLabel(lblFound);
            il.EmitOpCode(ILOpCode.Pop);    // POP object
            il.EmitBoolConstant(true);

            il.MarkLabel(lblEnd);

            // STACK: i4 (boolean)
        }

        void EmitCatchBlock(CodeGenerator cg, CatchBlock catchBlock)
        {
            Debug.Assert(catchBlock.Variable == null || catchBlock.Variable.Variable != null);

            var il = cg.Builder;
            TypeSymbol extype;

            il.AdjustStack(1); // Account for exception on the stack.

            // set of types we catch in this catch block
            var trefs = catchBlock.TypeRef is TypeRef.BoundMultipleTypeRef mt
                ? mt.TypeRefs
                : ImmutableArray.Create((BoundTypeRef)catchBlock.TypeRef);

            // do we have to generate .filter or just .catch<type>:
            if (trefs.Length != 1 || trefs[0].ResolvedType.IsErrorTypeOrNull() || !trefs[0].ResolvedType.IsOfType(cg.CoreTypes.Exception))
            {
                // Template: catch when
                il.OpenLocalScope(ScopeType.Filter);

                // STACK : object

                if (trefs.Length == 1)
                {
                    EmitTypeCheck(cg, trefs[0]);

                    extype = trefs[0].ResolvedType.IsErrorTypeOrNull()
                        ? cg.CoreTypes.Object.Symbol
                        : trefs[0].ResolvedType;
                }
                else
                {
                    EmitMultipleTypeCheck(cg, trefs);

                    extype = cg.CoreTypes.Object.Symbol;
                }

                // STACK : i4 ? handle : continue

                il.MarkFilterConditionEnd();

                // STACK : object
                cg.EmitCastClass(cg.CoreTypes.Exception);   // has to be casted to System.Exception in order to generate valid IL
                cg.EmitCastClass(extype);
            }
            else
            {
                // Template: catch (TypeRef)
                extype = trefs[0].ResolvedType;
                il.OpenLocalScope(ScopeType.Catch, cg.Module.Translate(extype, null, cg.Diagnostics));
            }

            // STACK : extype
            if (catchBlock.Variable != null)
            {
                cg.EmitSequencePoint(catchBlock.Variable.PhpSyntax);

                // <tmp> = <ex>
                var tmploc = cg.GetTemporaryLocal(extype);
                il.EmitLocalStore(tmploc);

                // $x = <tmp>
                var varplace = catchBlock.Variable.BindPlace(cg);
                varplace.EmitStore(cg, tmploc, catchBlock.Variable.TargetAccess());
                cg.ReturnTemporaryLocal(tmploc);
            }
            else
            {
                il.EmitOpCode(ILOpCode.Pop);
            }

            //
            if (EmitCatchFinallyOutsideScope)
            {
                // .br
                il.EmitBranch(ILOpCode.Br, catchBlock);
            }
            else
            {
                // { .. }
                cg.GenerateScope(catchBlock, NextBlock.Ordinal);
            }

            //
            il.CloseLocalScope();
        }

        void EmitJumpTable(CodeGenerator cg)
        {
            var yields = cg.Routine.ControlFlowGraph.Yields;
            if (yields.IsDefaultOrEmpty)
            {
                return;
            }

            // local <state> = g._state that is switched on (can't switch on remote field)
            Debug.Assert(cg.GeneratorStateLocal != null);

            // create label for situation when state doesn't correspond to continuation: 0 -> didn't run to first yield
            var noContinuationLabel = new NamedLabel("noStateContinuation");

            // prepare jump table from yields
            var yieldExLabels = new List<KeyValuePair<ConstantValue, object>>();
            foreach (var yield in yields)
            {
                // only applies to yields inside this "try" block
                var node = yield.ContainingTryScopes.First;
                while (node != null && node.Value != this)
                {
                    node = node.Next;
                }
                if (node == null) continue;

                // jump to next nested "try" or inside "yield" itself
                var target = (object)node.Next?.Value/*next try block*/ ?? yield/*inside yield*/;

                // case YieldIndex: goto target;
                yieldExLabels.Add(new KeyValuePair<ConstantValue, object>(ConstantValue.Create(yield.YieldIndex), target));
            }

            if (yieldExLabels.Count != 0)
            {
                // emit switch table that based on g._state jumps to appropriate continuation label
                cg.Builder.EmitIntegerSwitchJumpTable(yieldExLabels.ToArray(), noContinuationLabel, cg.GeneratorStateLocal, Microsoft.Cci.PrimitiveTypeCode.Int32);

                cg.Builder.MarkLabel(noContinuationLabel);
            }
        }
    }

    partial class ForeachEnumereeEdge
    {
        CodeGenerator.TemporaryLocalDefinition _enumeratorLoc, _synthesizedIndexLoc;
        LocalDefinition _aliasedValueLoc;
        MethodSymbol _moveNextMethod, _disposeMethod, _currentValue, _currentKey, _current, _iterator_next;

        internal object _lbl_MoveNext = new object();

        void EmitReleaseRef(CodeGenerator cg)
        {
            if (_aliasedValueLoc != null)
            {
                Debug.Assert((TypeSymbol)_aliasedValueLoc.Type == cg.CoreTypes.PhpAlias);
                // This is temporary workaround introducing a simple reference counting (https://github.com/peachpiecompiler/peachpie/issues/345)
                // Note: it is not correct but it works in the way PHP developers thinks it works :)

                // <_aliasedValue>?.ReleaseRef();
                cg.Builder.EmitLocalLoad(_aliasedValueLoc);

                var lbl_end = new object();

                cg.Builder.EmitBranch(ILOpCode.Brfalse, lbl_end);
                cg.Builder.EmitLocalLoad(_aliasedValueLoc);
                cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.ReleaseRef));
                cg.Builder.MarkLabel(lbl_end);
            }
        }

        internal void EmitIteratorNext(CodeGenerator cg)
        {
            if (_iterator_next != null)
            {
                // Template: Iterator.next()
                cg.EmitPop(VariableReferenceExtensions.EmitLoadValue(cg, _iterator_next, _enumeratorLoc));
            }
        }

        internal void EmitMoveNext(CodeGenerator cg)
        {
            Debug.Assert(_enumeratorLoc.IsValid);
            Debug.Assert(_moveNextMethod != null);
            Debug.Assert(_moveNextMethod.IsStatic == false);

            // leaving scope of `foreach` body

            EmitReleaseRef(cg);

            if (_enumeratorLoc.Type.IsValueType)
            {
                // <locaddr>.MoveNext()
                _enumeratorLoc.EmitLoadAddress(cg.Builder);
            }
            else
            {
                // <loc>.MoveNext()
                _enumeratorLoc.EmitLoad(cg.Builder);
            }

            cg.EmitCall(ILOpCode.Callvirt, _moveNextMethod)
                .Expect(SpecialType.System_Boolean);
        }

        TypeSymbol EmitGetCurrentHelper(CodeGenerator cg)
        {
            var getter = _currentValue ?? _current;
            Debug.Assert(getter != null);

            var t = VariableReferenceExtensions.EmitLoadValue(cg, getter, _enumeratorLoc);

            if (t.Is_PhpValue())
            {
                if (_aliasedValues)  // current() may get PhpAlias wrapped in PhpValue, make it PhpAlias again so it is handled properly
                {
                    // .EnsureAlias()
                    cg.EmitPhpValueAddr();
                    t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureAlias_PhpValueRef);
                }
                else
                {
                    // .GetValue()
                    cg.EmitPhpValueAddr();
                    t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.GetValue);
                }
            }

            if (_aliasedValueLoc != null && (TypeSymbol)_aliasedValueLoc.Type == t)
            {
                // <_aliasedValue> = <STACK>
                cg.Builder.EmitOpCode(ILOpCode.Dup);
                cg.Builder.EmitLocalStore(_aliasedValueLoc);
            }

            return t;
        }

        internal void EmitPrepare(CodeGenerator cg)
        {
            if (_currentValue != null && _aliasedValues && _currentValue.ReturnType == cg.CoreTypes.PhpAlias &&
                cg.GeneratorStateMachineMethod == null)
            {
                _aliasedValueLoc = cg.GetTemporaryLocal(_currentValue.ReturnType, immediateReturn: false);

                cg.Builder.EmitNullConstant();
                cg.Builder.EmitLocalStore(_aliasedValueLoc);
            }

            var nextedge = NextBlock.NextEdge as ForeachMoveNextEdge;
            if (_currentKey == null && nextedge.KeyVariable != null && !IsAPairValue(_current.ReturnType, out _, out _))
            {
                // KeyVariable will be iterated from 1
                _synthesizedIndexLoc = cg.GetTemporaryLocal(cg.CoreTypes.Long, true, immediateReturn: false);

                // Template: KeyVariable = 0;
                cg.Builder.EmitLongConstant(0L);
                _synthesizedIndexLoc.EmitStore();
            }
        }

        static bool IsAPairValue(TypeSymbol type, out Symbol key, out Symbol value)
        {
            key = value = default;

            if (type.IsValueType)
            {
                if (type.Name == "ValueTuple" && ((NamedTypeSymbol)type).Arity == 2)
                {
                    key = type.GetMembers("Item1").Single();
                    value = type.GetMembers("Item2").Single();
                }
                else if (type.Name == "KeyValuePair" && ((NamedTypeSymbol)type).Arity == 2)
                {
                    key = type.GetMembers("Key").Single();
                    value = type.GetMembers("Value").Single();
                }
            }

            //
            return key != null && value != null; ;
        }

        internal void EmitGetCurrent(CodeGenerator cg, BoundReferenceExpression valueVar, BoundReferenceExpression keyVar)
        {
            Debug.Assert(_enumeratorLoc.IsValid);

            // NOTE: PHP writes first to {valueVar} then to {keyVar}

            if (_currentValue != null && _currentKey != null)
            {
                // PhpArray enumerator or Iterator

                cg.EmitSequencePoint(valueVar.PhpSyntax);
                valueVar.BindPlace(cg).EmitStore(cg, () => EmitGetCurrentHelper(cg), valueVar.TargetAccess());

                if (keyVar != null)
                {
                    cg.EmitSequencePoint(keyVar.PhpSyntax);
                    keyVar.BindPlace(cg).EmitStore(cg, () => VariableReferenceExtensions.EmitLoadValue(cg, _currentKey, _enumeratorLoc), keyVar.TargetAccess());
                }
            }
            else
            {
                if (_current == null)
                {
                    throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(_current);
                }

                var valuetype = _current.ReturnType;

                // ValueTuple<T1, T2> (Item1, Item2)
                // KeyValuePair<TKey, TValue> (Key, Value)
                if (IsAPairValue(valuetype, out var skey, out var svalue))
                {
                    // tmp = current;
                    var tmp = cg.GetTemporaryLocal(valuetype);
                    VariableReferenceExtensions.EmitLoadValue(cg, _current, _enumeratorLoc);
                    cg.Builder.EmitLocalStore(tmp);

                    var tmploc = new LocalPlace(tmp);

                    var keyplace = skey switch
                    {
                        FieldSymbol fld => (IPlace)new FieldPlace(tmploc, fld, cg.Module),
                        PropertySymbol prop => new PropertyPlace(tmploc, prop, cg.Module),
                        _ => throw Roslyn.Utilities.ExceptionUtilities.Unreachable,
                    };

                    var valueplace = svalue switch
                    {
                        FieldSymbol fld => (IPlace)new FieldPlace(tmploc, fld, cg.Module),
                        PropertySymbol prop => new PropertyPlace(tmploc, prop, cg.Module),
                        _ => throw Roslyn.Utilities.ExceptionUtilities.Unreachable,
                    };

                    // value = tmp.Item2;
                    cg.EmitSequencePoint(valueVar.PhpSyntax);
                    valueVar.BindPlace(cg).EmitStore(cg, valueplace, valueVar.TargetAccess());

                    // key = tmp.Item1;
                    if (keyVar != null)
                    {
                        cg.EmitSequencePoint(keyVar.PhpSyntax);
                        keyVar.BindPlace(cg).EmitStore(cg, keyplace, keyVar.TargetAccess());
                    }

                    //
                    cg.ReturnTemporaryLocal(tmp);
                }
                // just a value
                else
                {
                    cg.EmitSequencePoint(valueVar.PhpSyntax);
                    valueVar.BindPlace(cg).EmitStore(cg, () => EmitGetCurrentHelper(cg), valueVar.TargetAccess());

                    if (keyVar != null)
                    {
                        Debug.Assert(_synthesizedIndexLoc != null);

                        cg.EmitSequencePoint(keyVar.PhpSyntax);

                        // key = LOAD KeyVariable
                        keyVar.BindPlace(cg).EmitStore(cg, () => _synthesizedIndexLoc.EmitLoad(cg.Builder), keyVar.TargetAccess());

                        // KeyVariable ++
                        _synthesizedIndexLoc.EmitLoad(cg.Builder); // Key
                        cg.Builder.EmitLongConstant(1L);           // 1

                        cg.Builder.EmitOpCode(ILOpCode.Add);

                        _synthesizedIndexLoc.EmitStore();
                    }
                }
            }
        }

        void EmitDisposeAndClean(CodeGenerator cg)
        {
            // enumerator.Dispose()
            if (_disposeMethod != null)
            {
                // TODO: if (enumerator != null)

                if (_enumeratorLoc.Type.IsValueType)
                {
                    _enumeratorLoc.EmitLoadAddress(cg.Builder);
                }
                else
                {
                    _enumeratorLoc.EmitLoad(cg.Builder);
                }

                cg.EmitCall(ILOpCode.Callvirt, _disposeMethod)
                    .Expect(SpecialType.System_Void);
            }

            //// enumerator = null;
            //if (!_enumeratorLoc.Type.IsValueType)
            //{
            //    cg.Builder.EmitNullConstant();
            //    cg.Builder.EmitLocalStore(_enumeratorLoc);
            //}

            //
            if (_aliasedValueLoc != null)
            {
                cg.ReturnTemporaryLocal(_aliasedValueLoc);
                _aliasedValueLoc = null;
            }

            if (_synthesizedIndexLoc != null)
            {
                cg.ReturnTemporaryLocal(_synthesizedIndexLoc);
                _synthesizedIndexLoc = null;
            }

            cg.ReturnTemporaryLocal(_enumeratorLoc);
            _enumeratorLoc = null;

            // unbind
            _moveNextMethod = null;
            _disposeMethod = null;
            _currentValue = null;
            _currentKey = null;
            _current = null;
            _iterator_next = null;
        }

        internal override void Generate(CodeGenerator cg)
        {
            Debug.Assert(this.Enumeree != null);

            // get the enumerator,
            // bind actual MoveNext() and CurrentValue and CurrentKey

            // Template: using(
            // a) enumerator = enumeree.GetEnumerator()
            // b) enumerator = Operators.GetEnumerator(enumeree)
            // ) ...

            cg.EmitSequencePoint(this.Enumeree.PhpSyntax);

            var enumereeType = cg.Emit(this.Enumeree);
            Debug.Assert(enumereeType.SpecialType != SpecialType.System_Void);

            var getEnumeratorMethod = enumereeType.LookupMember<MethodSymbol>(WellKnownMemberNames.GetEnumeratorMethodName);

            TypeSymbol enumeratorType;

            if (enumereeType.IsOfType(cg.CoreTypes.PhpArray))
            {
                // optimized array enumeration if possible
                // does not make sense in state machines
                if (cg.GeneratorStateMachineMethod == null && _aliasedValues)
                {
                    // Operators.GetFastEnumerator(PhpArray, bool)
                    cg.Builder.EmitBoolConstant(_aliasedValues);
                    enumeratorType = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetFastEnumerator_PhpArray_Boolean);
                }
                else
                {
                    Debug.Assert(enumereeType.IsReferenceType);

                    // PhpArray.GetForeachEnumerator(bool aliasedValues)
                    cg.Builder.EmitBoolConstant(_aliasedValues);
                    enumeratorType = cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.GetForeachEnumerator_Boolean);  // TODO: IPhpArray
                }
            }
            else if (enumereeType.IsOfType(cg.CoreTypes.IPhpEnumerable))
            {
                var GetForeachEnumerator_Bool_RuntimeTypeHandle = cg.CoreTypes.IPhpEnumerable.Method("GetForeachEnumerator", cg.CoreTypes.Boolean, cg.CoreTypes.RuntimeTypeHandle);

                // enumeree.GetForeachEnumerator(bool aliasedValues, RuntimeTypeHandle caller)
                cg.Builder.EmitBoolConstant(_aliasedValues);
                cg.EmitCallerTypeHandle();
                enumeratorType = cg.EmitCall(ILOpCode.Callvirt, GetForeachEnumerator_Bool_RuntimeTypeHandle);
            }
            else if (enumereeType.IsOfType(cg.CoreTypes.Iterator))
            {
                // use Iterator directly,
                // do not allocate additional wrappers
                enumeratorType = cg.CoreTypes.Iterator; // cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetForeachEnumerator_Iterator);
            }
            // TODO: IPhpArray
            else if (getEnumeratorMethod != null &&
                getEnumeratorMethod.ParameterCount == 0 &&
                getEnumeratorMethod.DeclaredAccessibility == Accessibility.Public &&
                !getEnumeratorMethod.IsStatic &&
                !enumereeType.Is_PhpValue())
            {
                // enumeree.GetEnumerator()
                if (enumereeType.IsReferenceType)
                {
                    enumeratorType = cg.EmitCall(ILOpCode.Callvirt, getEnumeratorMethod);
                }
                else
                {
                    cg.EmitStructAddr(enumereeType);
                    enumeratorType = cg.EmitCall(ILOpCode.Call, getEnumeratorMethod);
                }
            }
            else
            {
                cg.EmitConvertToPhpValue(enumereeType, 0);
                cg.Builder.EmitBoolConstant(_aliasedValues);
                cg.EmitCallerTypeHandle();

                // Operators.GetForeachEnumerator(PhpValue, bool, RuntimeTypeHandle)
                enumeratorType = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetForeachEnumerator_PhpValue_Bool_RuntimeTypeHandle);
            }

            // store the enumerator:
            _enumeratorLoc = cg.GetTemporaryLocal(enumeratorType, longlive: true, immediateReturn: false);
            _enumeratorLoc.EmitStore();

            //
            if (enumeratorType == cg.CoreTypes.Iterator)
            {
                // if iterator is null, goto end
                _enumeratorLoc.EmitLoad(cg.Builder);
                cg.Builder.EmitBranch(ILOpCode.Brfalse, NextBlock.NextEdge.NextBlock);

                // Template: iterator.rewind() : void
                _enumeratorLoc.EmitLoad(cg.Builder);
                cg.EmitPop(cg.EmitCall(ILOpCode.Callvirt, enumeratorType.LookupMember<MethodSymbol>("rewind")));

                // TODO: declaredaccessibility
                // bind methods
                _iterator_next = enumeratorType.LookupMember<MethodSymbol>("next"); // next()
                _current = _currentValue = enumeratorType.LookupMember<MethodSymbol>("current");    // current()
                _currentKey = enumeratorType.LookupMember<MethodSymbol>("key");     // key()
                _moveNextMethod = enumeratorType.LookupMember<MethodSymbol>("valid");   // valid() // use it as the loop's control expression (MoveNext)
            }
            else
            {
                // TODO: declaredaccessibility
                // bind methods
                _current = enumeratorType.LookupMember<PropertySymbol>(WellKnownMemberNames.CurrentPropertyName)?.GetMethod;   // TODO: Err if no Current
                _currentValue = enumeratorType.LookupMember<PropertySymbol>(_aliasedValues ? "CurrentValueAliased" : "CurrentValue")?.GetMethod;
                _currentKey = enumeratorType.LookupMember<PropertySymbol>("CurrentKey")?.GetMethod;
                _disposeMethod = enumeratorType.LookupMember<MethodSymbol>(WellKnownMemberNames.DisposeMethodName, m => !m.IsStatic && m.ParameterCount == 0);

                _moveNextMethod = enumeratorType.LookupMember<MethodSymbol>(WellKnownMemberNames.MoveNextMethodName, m => !m.IsStatic && m.ParameterCount == 0);
                Debug.Assert(_moveNextMethod.ReturnType.SpecialType == SpecialType.System_Boolean);
            }

            if (_disposeMethod != null
                && cg.GeneratorStateMachineMethod == null)  // Temporary workaround allowing "yield" inside foreach. Yield cannot be inside TRY block, so we don't generate TRY for state machines. Remove this condition once we manage to bind try/catch/yield somehow
            {
                /* Template: try { body } finally { enumerator.Dispose }
                 */

                // try {
                cg.Builder.AssertStackEmpty();
                cg.Builder.OpenLocalScope(ScopeType.TryCatchFinally);
                cg.Builder.OpenLocalScope(ScopeType.Try);

                //
                EmitBody(cg);

                // }
                cg.Builder.CloseLocalScope();   // /Try

                // finally {
                cg.Builder.OpenLocalScope(ScopeType.Finally);

                // enumerator.Dispose() & cleanup
                EmitDisposeAndClean(cg);

                // }
                cg.Builder.CloseLocalScope();   // /Finally
                cg.Builder.CloseLocalScope();   // /TryCatchFinally
            }
            else
            {
                EmitBody(cg);
                EmitDisposeAndClean(cg);
            }
        }

        void EmitBody(CodeGenerator cg)
        {
            Debug.Assert(NextBlock.NextEdge is ForeachMoveNextEdge);
            this.EmitPrepare(cg);
            cg.Builder.EmitBranch(ILOpCode.Br, _lbl_MoveNext);
            cg.GenerateScope(NextBlock, NextBlock.NextEdge.NextBlock.Ordinal);
            cg.Builder.EmitBranch(ILOpCode.Br, NextBlock.NextEdge.NextBlock);
        }
    }

    partial class ForeachMoveNextEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            /* Template:
             *  for (;MoveNext(enumerator);)
             *      $value = CurrentValue(enumerator);
             *      $key = CurrentKey(enumerator);
             *      {body}
             *  }
             */

            var lblMoveNext = new NamedLabel("MoveNext");
            var lblBody = new object();

            //
            cg.EmitHiddenSequencePoint();
            cg.Builder.EmitBranch(ILOpCode.Br, lblMoveNext);
            cg.Builder.MarkLabel(lblBody);

            // $value, $key
            this.EnumereeEdge.EmitGetCurrent(cg, this.ValueVariable, this.KeyVariable);

            // {
            cg.GenerateScope(this.BodyBlock, NextBlock.Ordinal);
            // }

            // if (enumerator.MoveNext())
            cg.EmitHiddenSequencePoint();
            cg.Builder.MarkLabel(lblMoveNext);
            this.EnumereeEdge.EmitIteratorNext(cg); // Iterator.next() : void (only if we are enumerating the Iterator directly)

            cg.EmitHiddenSequencePoint();
            cg.Builder.MarkLabel(this.EnumereeEdge._lbl_MoveNext);

            cg.EmitSequencePoint(MoveNextSpan);
            this.EnumereeEdge.EmitMoveNext(cg); // bool
            cg.Builder.EmitBranch(ILOpCode.Brtrue, lblBody);

            //
            cg.Scope.ContinueWith(NextBlock);
        }
    }

    partial class SwitchEdge
    {
        static bool IsInt32(object value) => value is int || (value is long && (long)value <= int.MaxValue && (long)value >= int.MinValue);
        static bool IsString(object value) => value is string;

        internal override void Generate(CodeGenerator cg)
        {
            // four cases:
            // 1. just single or none case label that can be replaced with single IF
            // 2. switch over integers, using native CIL switch
            // 3. switch over strings, using C# static Dictionary and CIL switch
            // 4. PHP style switch which is just a bunch of IFs

            if (this.CaseBlocks.Length == 0 || (this.CaseBlocks[0].IsDefault && this.CaseBlocks.Length == 1))
            {
                Debug.Assert(this.CaseBlocks.Length <= 1);

                // no SWITCH or IF needed

                cg.EmitPop(this.SwitchValue.WithAccess(BoundAccess.None).Emit(cg)); // None Access, also using BoundExpression.Emit directly to avoid CodeGenerator type specialization which is not needed
                if (this.CaseBlocks.Length != 0)
                {
                    cg.GenerateScope(this.CaseBlocks[0], NextBlock.Ordinal);
                }
            }
            else
            {
                // CIL Switch:
                bool allConst = this.CaseBlocks.All(c => c.IsDefault || (c.CaseValue.IsOnlyBoundElement && c.CaseValue.BoundElement.ConstantValue.HasValue));
                bool allIntConst = allConst && this.CaseBlocks.All(c => c.IsDefault || IsInt32(c.CaseValue.BoundElement.ConstantValue.Value));
                //bool allconststrings = allconsts && this.CaseBlocks.All(c => c.IsDefault || IsString(c.CaseValue.ConstantValue.Value));

                var default_block = this.DefaultBlock;

                // <switch_loc> = <SwitchValue>;
                TypeSymbol switch_type;
                LocalDefinition switch_loc;

                // Switch Header
                if (allIntConst)
                {
                    switch_type = cg.CoreTypes.Int32;
                    cg.EmitSequencePoint(this.SwitchValue.PhpSyntax);
                    cg.EmitConvert(this.SwitchValue, switch_type);
                    switch_loc = cg.GetTemporaryLocal(switch_type);
                    cg.Builder.EmitLocalStore(switch_loc);

                    // switch (labels)
                    cg.Builder.EmitIntegerSwitchJumpTable(GetSwitchCaseLabels(CaseBlocks), default_block ?? NextBlock, switch_loc, switch_type.PrimitiveTypeCode);
                }
                //else if (allconststrings)
                //{

                //}
                else
                {
                    // legacy jump table
                    // IF (case_i) GOTO label_i;

                    cg.EmitSequencePoint(this.SwitchValue.PhpSyntax);
                    switch_type = cg.Emit(this.SwitchValue);
                    switch_loc = cg.GetTemporaryLocal(switch_type);
                    cg.Builder.EmitLocalStore(switch_loc);

                    //
                    foreach (var this_block in this.CaseBlocks)
                    {
                        var caseValueBag = this_block.CaseValue;
                        if (caseValueBag.IsEmpty) { continue; }

                        if (!caseValueBag.IsOnlyBoundElement)
                        {
                            cg.ReturnTemporaryLocal(switch_loc); // statements in pre-bound-blocks could return (e.g. yieldStatement) & destroy stack-local switch_loc variable -> be defensive
                            caseValueBag.PreBoundBlockFirst.Emit(cg); // emit all blocks that have to go before case value emit
                        }

                        // reininiaze switch_loc if destroyed previously
                        if (!caseValueBag.IsOnlyBoundElement)
                        {
                            cg.Emit(this.SwitchValue);
                            switch_loc = cg.GetTemporaryLocal(switch_type);
                            cg.Builder.EmitLocalStore(switch_loc);
                        }

                        // <CaseValue>:
                        var caseValue = caseValueBag.BoundElement;
                        cg.EmitSequencePoint(caseValue.PhpSyntax);

                        // if (<switch_loc> == c.CaseValue) goto this_block;
                        cg.Builder.EmitLocalLoad(switch_loc);
                        BoundBinaryEx.EmitEquality(cg, switch_type, caseValue);
                        cg.Builder.EmitBranch(ILOpCode.Brtrue, this_block);
                    }

                    // default:
                    cg.Builder.EmitBranch(ILOpCode.Br, default_block ?? NextBlock);
                }

                // FREE <switch_loc>
                cg.ReturnTemporaryLocal(switch_loc);

                // Switch Body
                for (int i = 0; i < this.CaseBlocks.Length; i++)
                {
                    var next_case = (i + 1 < this.CaseBlocks.Length) ? this.CaseBlocks[i + 1] : null;

                    // {
                    cg.GenerateScope(this.CaseBlocks[i], (next_case ?? NextBlock).Ordinal);
                    // }

                }
            }

            cg.Scope.ContinueWith(NextBlock);
        }

        /// <summary>
        /// Gets case labels.
        /// </summary>
        static KeyValuePair<ConstantValue, object>[] GetSwitchCaseLabels(IEnumerable<CaseBlock> sections)
        {
            var labelsBuilder = ArrayBuilder<KeyValuePair<ConstantValue, object>>.GetInstance();
            foreach (var section in sections)
            {
                if (section.IsDefault)
                {
                    // fallThroughLabel = section
                }
                else
                {
                    labelsBuilder.Add(new KeyValuePair<ConstantValue, object>(Int32Constant(section.CaseValue.BoundElement.ConstantValue.Value), section));
                }
            }

            return labelsBuilder.ToArrayAndFree();
        }

        // TODO: move to helpers
        static ConstantValue Int32Constant(object value)
        {
            if (value is int) return ConstantValue.Create((int)value);
            if (value is long) return ConstantValue.Create((int)(long)value);
            if (value is double) return ConstantValue.Create((int)(double)value);

            throw new ArgumentException();
        }
    }
}
