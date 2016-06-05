using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

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
                cg.Builder.EmitOpCode(ILOpCode.Nop);
            }
            cg.Scope.ContinueWith(NextBlock);
        }
    }

    partial class ConditionalEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            Contract.ThrowIfNull(Condition);

            if (IsLoop) // perf
            {
                cg.Builder.EmitBranch(ILOpCode.Br, this.Condition);

                // {
                cg.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }

                // if (Condition)
                cg.EmitSequencePoint(this.Condition.PhpSyntax);
                cg.Builder.MarkLabel(this.Condition);
                cg.EmitConvert(this.Condition, cg.CoreTypes.Boolean);
                cg.Builder.EmitBranch(ILOpCode.Brtrue, TrueTarget);
            }
            else
            {
                // if (Condition)
                cg.EmitSequencePoint(this.Condition.PhpSyntax);
                cg.EmitConvert(this.Condition, cg.CoreTypes.Boolean);
                cg.Builder.EmitBranch(ILOpCode.Brfalse, FalseTarget);

                // {
                cg.GenerateScope(TrueTarget, NextBlock.Ordinal);
                // }
            }

            cg.Scope.ContinueWith(FalseTarget);
        }
    }

    partial class TryCatchEdge
    {
        internal override void Generate(CodeGenerator cg)
        {
            throw new NotImplementedException();
        }
    }

    partial class ForeachEnumereeEdge
    {
        LocalDefinition _enumeratorLoc;
        MethodSymbol _moveNextMethod;
        PropertySymbol _currentValue, _currentKey, _current;

        internal void EmitMoveNext(CodeGenerator cg)
        {
            Debug.Assert(_enumeratorLoc != null);
            Debug.Assert(_moveNextMethod != null);
            Debug.Assert(_moveNextMethod.IsStatic == false);

            if (_enumeratorLoc.Type.IsValueType)
            {
                // <locaddr>.MoveNext()
                cg.Builder.EmitLocalAddress(_enumeratorLoc);
            }
            else
            {
                // <loc>.MoveNext()
                cg.Builder.EmitLocalLoad(_enumeratorLoc);
            }

            cg.EmitCall(_enumeratorLoc.Type.IsValueType ? ILOpCode.Call : ILOpCode.Callvirt, _moveNextMethod)
                .Expect(SpecialType.System_Boolean);
        }

        internal void EmitGetCurrent(CodeGenerator cg, BoundReferenceExpression valueVar, BoundReferenceExpression keyVar)
        {
            Debug.Assert(_enumeratorLoc != null);

            var enumeratorPlace = new LocalPlace(_enumeratorLoc);

            if (valueVar is BoundListEx)
            {
                throw new NotImplementedException();    // TODO: list(vars) = enumerator.GetCurrent()
            }

            if (_currentValue != null && _currentKey != null)
            {
                // special PhpArray enumerator

                if (keyVar != null)
                {
                    //cg.EmitSequencePoint(keyVar.PhpSyntax);
                    var keyTarget = keyVar.BindPlace(cg);
                    keyTarget.EmitStorePrepare(cg);
                    keyTarget.EmitStore(cg, cg.EmitGetProperty(enumeratorPlace, _currentKey));
                }

                //cg.EmitSequencePoint(valueVar.PhpSyntax);
                var valueTarget = valueVar.BindPlace(cg);
                valueTarget.EmitStorePrepare(cg);
                valueTarget.EmitStore(cg, cg.EmitGetProperty(enumeratorPlace, _currentValue));
            }
            else
            {
                Debug.Assert(_current != null);

                if (keyVar != null)
                {
                    throw new InvalidOperationException();
                }

                var valueTarget = valueVar.BindPlace(cg);
                valueTarget.EmitStorePrepare(cg);
                var t = cg.EmitGetProperty(enumeratorPlace, _current);  // TOOD: PhpValue.FromClr
                valueTarget.EmitStore(cg, t);
            }
        }

        internal void Close(CodeGenerator cg)
        {
            cg.ReturnTemporaryLocal(_enumeratorLoc);
            _enumeratorLoc = null;

            // unbind
            _moveNextMethod = null;
            _currentValue = null;
            _currentKey = null;
            _current = null;
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
            var getEnumeratorMethod = enumereeType.LookupMember<MethodSymbol>(WellKnownMemberNames.GetEnumeratorMethodName);

            TypeSymbol enumeratorType;

            if (enumereeType.IsEqualToOrDerivedFrom(cg.CoreTypes.PhpArray))
            {
                cg.Builder.EmitBoolConstant(_aliasedValues);
                cg.EmitCallerRuntimeTypeHandle();

                // PhpArray.GetForeachtEnumerator(bool, RuntimeTypeHandle)
                enumeratorType = cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.GetForeachEnumerator);
            }
            // TODO: IPhpEnumerable
            // TODO: Iterator
            else if (getEnumeratorMethod != null && getEnumeratorMethod.ParameterCount == 0 && enumereeType.IsReferenceType)
            {
                // enumeree.GetEnumerator()
                enumeratorType = cg.EmitCall(getEnumeratorMethod.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call, getEnumeratorMethod);
            }
            else
            {
                cg.EmitConvertToPhpValue(enumereeType, 0);
                cg.Builder.EmitBoolConstant(_aliasedValues);
                cg.EmitCallerRuntimeTypeHandle();

                // Operators.GetForeachtEnumerator(PhpValue, bool, RuntimeTypeHandle)
                enumeratorType = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetForeachEnumerator_PhpValue_Bool_RuntimeTypeHandle);
            }

            //
            _current = enumeratorType.LookupMember<PropertySymbol>(WellKnownMemberNames.CurrentPropertyName);   // TODO: Err if no Current
            _currentValue = enumeratorType.LookupMember<PropertySymbol>(_aliasedValues ? "CurrentValueAliased" : "CurrentValue");
            _currentKey = enumeratorType.LookupMember<PropertySymbol>("CurrentKey");

            //
            _enumeratorLoc = cg.GetTemporaryLocal(enumeratorType);
            cg.Builder.EmitLocalStore(_enumeratorLoc);

            // bind methods
            _moveNextMethod = enumeratorType.LookupMember<MethodSymbol>(WellKnownMemberNames.MoveNextMethodName);    // TODO: Err if there is no MoveNext()
            Debug.Assert(_moveNextMethod.ReturnType.SpecialType == SpecialType.System_Boolean);
            Debug.Assert(_moveNextMethod.IsStatic == false);
            // ...

            // TODO: try { NextBlock } finally { enumerator.Dispose }

            //
            cg.Scope.ContinueWith(NextBlock);
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

            var lblMoveNext = new object();
            var lblBody = new object();

            cg.Builder.EmitBranch(ILOpCode.Br, lblMoveNext);
            cg.Builder.MarkLabel(lblBody);

            // $value, $key
            this.EnumereeEdge.EmitGetCurrent(cg, this.ValueVariable, this.KeyVariable);

            // {
            cg.GenerateScope(this.BodyBlock, NextBlock.Ordinal);
            // }

            // if (enumerator.MoveNext())
            //cg.EmitSequencePoint(this.Condition.PhpSyntax);
            cg.Builder.MarkLabel(lblMoveNext);
            this.EnumereeEdge.EmitMoveNext(cg); // bool
            cg.Builder.EmitBranch(ILOpCode.Brtrue, lblBody);

            //
            this.EnumereeEdge.Close(cg);

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

            if (this.CaseBlocks.Length == 0 || this.CaseBlocks[0].IsDefault)
            {
                // no SWITCH or IF needed

                cg.EmitPop(this.SwitchValue.WithAccess(BoundAccess.None).Emit(cg)); // None Access, also using BoundExpression.Emit directly to avoid CodeGenerator type specialization which is not needed
                if (this.CaseBlocks.Length == 1)
                {
                    cg.GenerateScope(this.CaseBlocks[0], NextBlock.Ordinal);
                }
                else
                {
                    throw new InvalidOperationException();  // default case should be the last one
                }
            }
            else
            {
                // TODO: CIL Switch:
                //bool allconsts = this.CaseBlocks.All(c => c.IsDefault || c.CaseValue.ConstantValue.HasValue);
                //bool allconstints = allconsts && this.CaseBlocks.All(c => c.IsDefault || IsInt32(c.CaseValue.ConstantValue.Value));
                //bool allconststrings = allconsts && this.CaseBlocks.All(c => c.IsDefault || IsString(c.CaseValue.ConstantValue.Value));

                //if (allconstints)
                //{

                //}
                //else if (allconststrings)
                //{

                //}
                //else
                {
                    // <switch_loc> = <SwitchValue>;
                    cg.EmitSequencePoint(this.SwitchValue.PhpSyntax);
                    var switch_type = cg.Emit(this.SwitchValue);
                    var switch_loc = cg.GetTemporaryLocal(switch_type);
                    cg.Builder.EmitLocalStore(switch_loc);

                    //
                    for (int i = 0; i < this.CaseBlocks.Length; i++)
                    {
                        var this_block = this.CaseBlocks[i];
                        var next_case = (i + 1 < this.CaseBlocks.Length) ? this.CaseBlocks[i + 1] : null;
                        object next_mark = (object)next_case?.CaseValue ?? next_case ?? NextBlock;

                        if (!this_block.IsDefault)
                        {
                            // <CaseValue>:
                            cg.EmitSequencePoint(this_block.CaseValue.PhpSyntax);
                            cg.Builder.MarkLabel(this_block.CaseValue);

                            // if (<switch_loc> == c.CaseValue)
                            cg.Builder.EmitLocalLoad(switch_loc);
                            BoundBinaryEx.EmitEquality(cg, switch_type, this_block.CaseValue);
                            cg.Builder.EmitBranch(ILOpCode.Brfalse, next_mark);
                        }

                        // {
                        cg.GenerateScope(this_block, (next_case ?? NextBlock).Ordinal);
                        // }
                    }

                    cg.ReturnTemporaryLocal(switch_loc);
                }
            }

            //
            cg.Scope.ContinueWith(NextBlock);
        }
    }
}
