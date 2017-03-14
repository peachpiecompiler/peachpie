using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Errors;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CodeGen
{
    partial class CodeGenerator
    {
        /// <summary>
        /// Emits <c>context</c> onto the evaluation stack.
        /// </summary>
        public TypeSymbol EmitLoadContext()
        {
            if (_contextPlace == null)
                throw new InvalidOperationException("Context is not available.");

            return _contextPlace.EmitLoad(_il);
        }

        /// <summary>
        /// Gets <see cref="IPlace"/> of current <c>Context</c>.
        /// </summary>
        public IPlace ContextPlaceOpt => _contextPlace;

        public void EmitCallerRuntimeTypeHandle()
        {
            var caller = this.CallerType;
            if (caller != null)
            {
                // RuntimeTypeHandle
                EmitLoadToken(caller, null);
            }
            else
            {
                // default(RuntimeTypeHandle)
                EmitLoadDefaultOfValueType(this.CoreTypes.RuntimeTypeHandle);
            }
        }

        /// <summary>
        /// Gets place referring to array of unoptimized local variables.
        /// Always valid in context of global scope.
        /// </summary>
        public IPlace LocalsPlaceOpt => _localsPlaceOpt;

        /// <summary>
        /// Gets value indicating the routine uses unoptimized locals access.
        /// This means, all the local variables are stored within an associative array instead of local slots.
        /// This value implicates, <see cref="LocalsPlaceOpt"/> is not <c>null</c>.
        /// </summary>
        public bool HasUnoptimizedLocals => LocalsPlaceOpt != null;

        /// <summary>
        /// Emits reference to <c>this</c>.
        /// </summary>
        /// <returns>Type of <c>this</c> in current context, pushed on top of the evaluation stack.</returns>
        public TypeSymbol EmitThis()
        {
            Contract.ThrowIfNull(_thisPlace);
            return EmitThisOrNull();
        }

        public TypeSymbol EmitThisOrNull()
        {
            if (_thisPlace == null)
            {
                _il.EmitNullConstant();
                return CoreTypes.Object;
            }
            else
            {
                return _thisPlace.EmitLoad(_il);
            }
        }

        /// <summary>
        /// If possible, based on type analysis, unwraps most specific type from give variable without a runtime type check.
        /// </summary>
        internal TypeSymbol TryEmitVariableSpecialize(BoundExpression expr)
        {
            Debug.Assert(expr.Access.IsRead);

            if (!expr.Access.IsEnsure && !expr.TypeRefMask.IsAnyType && !expr.TypeRefMask.IsRef)
            {
                // avoiding of load of full value if not necessary
                return TryEmitVariableSpecialize(PlaceOrNull(expr), expr.TypeRefMask);
            }
            else
            {
                // we have to call expr.Emit() to generate ensureness correctly (Ensure Object, Ensure Array, Read Alias)
                return null;
            }
        }

        /// <summary>
        /// If possible, based on type analysis, unwraps most specific type from give variable without a runtime type check.
        /// </summary>
        internal TypeSymbol TryEmitVariableSpecialize(IPlace place, TypeRefMask tmask)
        {
            if (place != null && tmask.IsSingleType && !tmask.IsRef)
            {
                if (place.HasAddress)
                {
                    if (place.TypeOpt == CoreTypes.PhpNumber)
                    {
                        // access directly without type checking
                        if (IsDoubleOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.get_Double)
                                .Expect(SpecialType.System_Double);
                        }
                        else if (IsLongOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.get_Long)
                                .Expect(SpecialType.System_Int64);
                        }
                    }
                    else if (place.TypeOpt == CoreTypes.PhpValue)
                    {
                        // access directly without type checking
                        if (IsDoubleOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Double)
                                .Expect(SpecialType.System_Double);
                        }
                        else if (IsLongOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Long)
                                .Expect(SpecialType.System_Int64);
                        }
                        else if (IsBooleanOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Boolean)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else if (IsReadonlyStringOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_String)
                                .Expect(SpecialType.System_String);
                        }
                        //else if (IsArrayOnly(tmask))
                        //{
                        //    place.EmitLoadAddress(_il);
                        //    return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Array)    NOTE!! PhpValue.Array is PhpArray
                        //        .Expect(CoreTypes.IPhpArray);
                        //}
                        else if (IsClassOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Object.Getter)
                                .Expect(SpecialType.System_Object);

                            // DEBUG:
                            //if (tmask.IsSingleType)
                            //{
                            //    var tref = this.TypeRefContext.GetTypes(tmask)[0];
                            //    var clrtype = (TypeSymbol)this.DeclaringCompilation.GlobalSemantics.GetType(tref.QualifiedName);
                            //    if (clrtype != null && !clrtype.IsErrorType())
                            //    {
                            //        this.EmitCastClass(clrtype);
                            //        return clrtype;
                            //    }
                            //}

                            return this.CoreTypes.Object;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// If possible, based on type analysis, unwraps more specific type from a value currently on stack without a runtime type check.
        /// </summary>
        /// <returns>New type on top of evaluation stack.</returns>
        internal TypeSymbol EmitSpecialize(BoundExpression expr)
        {
            // load resulting value directly if resolved:
            if (expr.ConstantValue.HasValue && !expr.Access.IsEnsure)
            {
                if (expr.Access.IsNone)
                {
                    return CoreTypes.Void;
                }

                if (expr.Access.IsRead)
                {
                    return expr.ResultType = EmitLoadConstant(expr.ConstantValue.Value, expr.Access.TargetType);
                }
            }

            //
            if (expr.Access.IsNone) // no need for specializing, the value won't be read anyway
            {
                return (expr.ResultType = expr.Emit(this));
            }
            else
            {
                Debug.Assert(expr.Access.IsRead);

                return expr.ResultType = (TryEmitVariableSpecialize(expr) ?? EmitSpecialize(expr.Emit(this), expr.TypeRefMask));
            }
        }

        /// <summary>
        /// If possible, based on type analysis, unwraps more specific type from a value currently on stack without a runtime type check.
        /// </summary>
        /// <param name="stack">Type of value currently on top of evaluationb stack.</param>
        /// <param name="tmask">Result of analysis what type will be there in runtime.</param>
        /// <returns>New type on top of evaluation stack.</returns>
        internal TypeSymbol EmitSpecialize(TypeSymbol stack, TypeRefMask tmask)
        {
            // specialize type if possible
            if (tmask.IsSingleType && !tmask.IsRef)
            {
                if (stack == this.CoreTypes.PhpNumber)
                {
                    if (IsDoubleOnly(tmask))
                    {
                        EmitPhpNumberAddr();
                        return EmitCall(ILOpCode.Call, this.CoreMethods.PhpNumber.get_Double)
                            .Expect(SpecialType.System_Double);
                    }
                    else if (IsLongOnly(tmask))
                    {
                        EmitPhpNumberAddr();
                        return EmitCall(ILOpCode.Call, this.CoreMethods.PhpNumber.get_Long)
                            .Expect(SpecialType.System_Int64);
                    }
                }
                else if (stack == CoreTypes.PhpValue)
                {
                    // access directly without type checking
                    if (IsDoubleOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Double)
                            .Expect(SpecialType.System_Double);
                    }
                    else if (IsLongOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Long)
                            .Expect(SpecialType.System_Int64);
                    }
                    else if (IsBooleanOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_Boolean)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (IsReadonlyStringOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.get_String)
                            .Expect(SpecialType.System_String);
                    }
                }
                else if (stack.IsReferenceType && this.Routine != null)
                {
                    var tref = this.TypeRefContext.GetTypes(tmask)[0];
                    if (tref.IsObject)
                    {
                        HashSet<DiagnosticInfo> useSiteDiagnostic = null;
                        var t = _routine.DeclaringCompilation.SourceAssembly.GetTypeByMetadataName(tref.QualifiedName.ClrName(), true, false);
                        if (t != null && !t.IsErrorType() && t.IsDerivedFrom(stack, false, ref useSiteDiagnostic)) // TODO: or interface
                        {
                            EmitCastClass(t);
                            return t;
                        }
                        else
                        {
                            // TODO: class aliasing
                            //Debug.Assert(t != null);
                            if (t == null)
                            {
                                Debug.WriteLine($"'{tref.QualifiedName}' is unknown!");
                            }
                        }
                    }
                }
            }

            //
            return stack;
        }

        public void EmitOpCode(ILOpCode code) => _il.EmitOpCode(code);

        public void EmitPop(TypeSymbol type)
        {
            Contract.ThrowIfNull(type);

            if (type.SpecialType != SpecialType.System_Void)
            {
                _il.EmitOpCode(ILOpCode.Pop, -1);
            }
        }

        public TypeSymbol Emit(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);
            return EmitSpecialize(expr);
        }

        /// <summary>
        /// Loads <see cref="RuntimeTypeHandle"/> of given type.
        /// </summary>
        public TypeSymbol EmitLoadToken(TypeSymbol type, SyntaxNode syntaxNodeOpt)
        {
            if (type != null)
            {
                _il.EmitLoadToken(_moduleBuilder, _diagnostics, type, syntaxNodeOpt);
            }
            else
            {
                EmitLoadDefaultOfValueType(this.CoreTypes.RuntimeTypeHandle);
            }

            return this.CoreTypes.RuntimeTypeHandle;
        }

        /// <summary>
        /// Loads <see cref="RuntimeMethodHandle"/> of given method.
        /// </summary>
        public TypeSymbol EmitLoadToken(MethodSymbol method, SyntaxNode syntaxNodeOpt)
        {
            _il.EmitLoadToken(_moduleBuilder, _diagnostics, method, syntaxNodeOpt);

            return this.CoreTypes.RuntimeMethodHandle;
        }

        public void EmitBox(TypeSymbol valuetype)
        {
            Contract.ThrowIfNull(valuetype);

            if (valuetype.IsValueType)
            {
                _il.EmitOpCode(ILOpCode.Box);
                EmitSymbolToken(valuetype, null);
            }
        }

        /// <summary>
        /// Emits "!= 0" operation. This method expects I4 value on top of evaluation stack.
        /// </summary>
        public void EmitLogicNegation()
        {
            _il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
            _il.EmitOpCode(ILOpCode.Ceq);
        }

        /// <summary>
        /// Loads field address on top of evaluation stack.
        /// </summary>
        public void EmitFieldAddress(FieldSymbol fld)
        {
            Debug.Assert(fld != null);
            _il.EmitOpCode(fld.IsStatic ? ILOpCode.Ldsflda : ILOpCode.Ldflda);
            EmitSymbolToken(fld, null);
        }

        internal void EmitSymbolToken(TypeSymbol symbol, SyntaxNode syntaxNode)
        {
            _il.EmitSymbolToken(_moduleBuilder, _diagnostics, symbol, syntaxNode);
        }

        internal void EmitSymbolToken(FieldSymbol symbol, SyntaxNode syntaxNode)
        {
            _il.EmitSymbolToken(_moduleBuilder, _diagnostics, symbol, syntaxNode);
        }

        //private void EmitSymbolToken(MethodSymbol method, SyntaxNode syntaxNode)
        //{
        //    _il.EmitToken(_moduleBuilder.Translate(method, syntaxNode, _diagnostics, null), syntaxNode, _diagnostics);
        //}

        internal void EmitSequencePoint(LangElement element)
        {
            if (element != null)
            {
                EmitSequencePoint(element.Span);
            }
        }
        internal void EmitSequencePoint(Span span)
        {
            if (_emitPdbSequencePoints && span.IsValid && !span.IsEmpty)
            {
                EmitSequencePoint(new Microsoft.CodeAnalysis.Text.TextSpan(span.Start, span.Length));
            }
        }

        internal void EmitSequencePoint(Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            if (_emitPdbSequencePoints && span.Length > 0)
            {
                _il.DefineSequencePoint(_routine.ContainingFile.SyntaxTree, span);
            }
        }

        public TypeSymbol EmitDereference(TypeSymbol t)
        {
            if (t == CoreTypes.PhpAlias)
            {
                // Template: <alias>.Value
                return this.Emit_PhpAlias_GetValue();
            }
            else if (t == CoreTypes.PhpValue)
            {
                // Template: <value>.GetValue()
                this.EmitPhpValueAddr();
                return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.GetValue);
            }
            else
            {
                // value already dereferenced
                return t;
            }
        }

        /// <summary>
        /// Emits load of <c>PhpAlias.Value</c>,
        /// expecting <c>PhpAlias</c> on top of evaluation stack,
        /// pushing <c>PhpValue</c> on top of the stack.
        /// </summary>
        public TypeSymbol Emit_PhpAlias_GetValue()
        {
            // <stack>.Value
            EmitOpCode(ILOpCode.Ldfld);
            EmitSymbolToken(CoreMethods.PhpAlias.Value, null);
            return this.CoreTypes.PhpValue;
        }

        /// <summary>
        /// Emits load of <c>PhpAlias.Value</c>,
        /// expecting <c>PhpAlias</c> on top of evaluation stack,
        /// pushing <c>PhpValue</c> on top of the stack.
        /// </summary>
        public void Emit_PhpAlias_GetValueAddr()
        {
            // ref <stack>.Value
            EmitOpCode(ILOpCode.Ldflda);
            EmitSymbolToken(CoreMethods.PhpAlias.Value, null);
        }

        /// <summary>
        /// Emits store to <c>PhpAlias.Value</c>,
        /// expecting <c>PhpAlias</c> and <c>PhpValue</c> on top of evaluation stack.
        /// </summary>
        public void Emit_PhpAlias_SetValue()
        {
            // <stack_1>.Value = <stack_2>
            EmitOpCode(ILOpCode.Stfld);
            EmitSymbolToken(CoreMethods.PhpAlias.Value, null);
        }

        /// <summary>
        /// Emits <c>new PhpAlias</c>, expecting <c>PhpValue</c> on top of the evaluation stack.
        /// </summary>
        public TypeSymbol Emit_PhpValue_MakeAlias()
        {
            // new PhpAlias(<STACK>, 1)
            _il.EmitIntConstant(1);
            return EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpAlias_PhpValue_int);
        }

        /// <summary>
        /// Emits load of PhpValue representing void.
        /// </summary>
        public TypeSymbol Emit_PhpValue_Void()
            => Emit_PhpValue_Void(_il, _moduleBuilder, _diagnostics);

        /// <summary>
        /// Emits load of PhpValue representing void.
        /// </summary>
        static TypeSymbol Emit_PhpValue_Void(ILBuilder il, Emit.PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            il.EmitOpCode(ILOpCode.Ldsfld);
            il.EmitSymbolToken(module, diagnostic, module.Compilation.CoreMethods.PhpValue.Void, null);
            return module.Compilation.CoreTypes.PhpValue;
        }

        /// <summary>
        /// Emits load of PhpValue representing null.
        /// </summary>
        public TypeSymbol Emit_PhpValue_Null()
        {
            _il.EmitOpCode(ILOpCode.Ldsfld);
            EmitSymbolToken(CoreMethods.PhpValue.Null, null);
            return CoreTypes.PhpValue;
        }

        /// <summary>
        /// Creates new empty <c>PhpArray</c> where modifications are not expected.
        /// </summary>
        public TypeSymbol Emit_PhpArray_NewEmpty()
        {
            // PhpArray.NewEmpty()
            var t = CoreTypes.PhpArray.Symbol;
            return EmitCall(ILOpCode.Call, (MethodSymbol)t.GetMembers("NewEmpty").Single());
        }

        /// <summary>
        /// Emits LOAD of <c>PhpArray.Empty</c> fields.
        /// The loaded value must not be modified, use only in read-only context.
        /// </summary>
        public TypeSymbol Emit_PhpArray_Empty()
        {
            // PhpArray.Empty
            Builder.EmitOpCode(ILOpCode.Ldsfld);
            EmitSymbolToken(CoreMethods.PhpArray.Empty, null);

            return CoreMethods.PhpArray.Empty.Symbol.Type
                .Expect(CoreTypes.PhpArray);
        }

        public void Emit_NewArray(TypeSymbol elementType, BoundExpression[] values)
        {
            if (values.Length != 0)
            {
                // new []
                _il.EmitIntConstant(values.Length);
                _il.EmitOpCode(ILOpCode.Newarr);
                EmitSymbolToken(elementType, null);

                // { argI, ..., argN }
                for (int i = 0; i < values.Length; i++)
                {
                    _il.EmitOpCode(ILOpCode.Dup);   // <array>
                    _il.EmitIntConstant(i);         // [i]
                    EmitConvert(values[i], elementType);
                    _il.EmitOpCode(ILOpCode.Stelem);
                    EmitSymbolToken(elementType, null);
                }
            }
            else
            {
                // empty array
                Emit_EmptyArray(elementType);
            }
        }

        void Emit_EmptyArray(TypeSymbol elementType)
        {
            var array_empty_T = ((MethodSymbol)this.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Array__Empty))
                .Construct(elementType);

            EmitCall(ILOpCode.Call, array_empty_T);
        }

        /// <summary>
        /// Emits array of <paramref name="elementType"/> containing all current routine PHP arguments value.
        /// </summary>
        void Emit_ArgsArray(TypeSymbol elementType)
        {
            var routine = this.Routine;
            if (routine == null)
            {
                throw new InvalidOperationException("Routine is null!");
            }

            var ps = routine.Parameters;
            var last = ps.LastOrDefault();
            var variadic = (last != null && last.IsParams && last.Type.IsSZArray()) ? last : null;  // optional params
            var variadic_element = (variadic?.Type as ArrayTypeSymbol)?.ElementType;
            var variadic_place = variadic != null ? new ParamPlace(variadic) : null;

            ps = ps.Where(p => !p.IsImplicitlyDeclared && !p.IsParams).ToImmutableArray();  // parameters without implicitly declared parameters

            if (ps.Length == 0 && variadic_element == elementType)
            {
                // == params
                variadic_place.EmitLoad(_il);
            }
            else
            {
                // COUNT: (N + params.Length)
                _il.EmitIntConstant(ps.Length);

                if (variadic_place != null)
                {
                    // + params.Length
                    variadic_place.EmitLoad(_il);
                    EmitArrayLength();
                    _il.EmitOpCode(ILOpCode.Add);
                }

                // new [<COUNT>]
                _il.EmitOpCode(ILOpCode.Newarr);
                EmitSymbolToken(elementType, null);

                // tmparr = <array>
                var tmparr = this.GetTemporaryLocal(ArrayTypeSymbol.CreateSZArray(this.DeclaringCompilation.SourceAssembly, elementType));
                _il.EmitLocalStore(tmparr);

                // { p1, .., pN }
                for (int i = 0; i < ps.Length; i++)
                {
                    _il.EmitLocalLoad(tmparr);      // <array>
                    _il.EmitIntConstant(i);         // [i]
                    EmitConvert(new ParamPlace(ps[i]).EmitLoad(_il), 0, elementType);
                    _il.EmitOpCode(ILOpCode.Stelem);
                    EmitSymbolToken(elementType, null);
                }

                if (variadic != null)
                {
                    // { params[0, .., paramsN] }

                    // Template: for (i = 0; i < params.Length; i++) <array>[i + N] = params[i]

                    var lbl_block = new object();
                    var lbl_cond = new object();

                    // i = 0
                    var tmpi = GetTemporaryLocal(CoreTypes.Int32);
                    _il.EmitIntConstant(0);
                    _il.EmitLocalStore(tmpi);
                    _il.EmitBranch(ILOpCode.Br, lbl_cond);

                    // {body}
                    _il.MarkLabel(lbl_block);

                    // <array>[i+N] = (T)params[i]
                    _il.EmitLocalLoad(tmparr);   // <array>
                    _il.EmitIntConstant(ps.Length);
                    _il.EmitLocalLoad(tmpi);
                    _il.EmitOpCode(ILOpCode.Add);

                    variadic_place.EmitLoad(_il);
                    _il.EmitLocalLoad(tmpi);
                    _il.EmitOpCode(ILOpCode.Ldelem);
                    EmitSymbolToken(variadic_element, null);
                    EmitConvert(variadic_element, 0, elementType);

                    _il.EmitOpCode(ILOpCode.Stelem);
                    EmitSymbolToken(elementType, null);

                    // i++
                    _il.EmitLocalLoad(tmpi);
                    _il.EmitIntConstant(1);
                    _il.EmitOpCode(ILOpCode.Add);
                    _il.EmitLocalStore(tmpi);

                    // i < params.Length
                    _il.MarkLabel(lbl_cond);
                    _il.EmitLocalLoad(tmpi);
                    variadic_place.EmitLoad(_il);
                    EmitArrayLength();
                    _il.EmitOpCode(ILOpCode.Clt);
                    _il.EmitBranch(ILOpCode.Brtrue, lbl_block);

                    //
                    ReturnTemporaryLocal(tmpi);
                }

                // <array>
                _il.EmitLocalLoad(tmparr);   // <array>

                //
                ReturnTemporaryLocal(tmparr);
                tmparr = null;
            }
        }

        /// <summary>
        /// Builds <c>PhpArray</c> out from <c>System.Array</c>.
        /// </summary>
        public TypeSymbol ArrayToPhpArray(IPlace arrplace, bool deepcopy = false)
        {
            var tmparr = GetTemporaryLocal(CoreTypes.PhpArray);
            var arr_element = ((ArrayTypeSymbol)arrplace.TypeOpt).ElementType;

            // Template: tmparr = new PhpArray(arrplace.Length)
            arrplace.EmitLoad(_il);
            EmitArrayLength();
            EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpArray_int);
            _il.EmitLocalStore(tmparr);

            // Template: for (i = 0; i < params.Length; i++) <phparr>.Add(arrplace[i])

            var lbl_block = new object();
            var lbl_cond = new object();

            // i = 0
            var tmpi = GetTemporaryLocal(CoreTypes.Int32);
            _il.EmitIntConstant(0);
            _il.EmitLocalStore(tmpi);
            _il.EmitBranch(ILOpCode.Br, lbl_cond);

            // {body}
            _il.MarkLabel(lbl_block);

            // <array>.Add((T)arrplace[i])
            _il.EmitLocalLoad(tmparr);   // <array>
            
            arrplace.EmitLoad(_il);
            _il.EmitLocalLoad(tmpi);
            _il.EmitOpCode(ILOpCode.Ldelem);
            EmitSymbolToken(arr_element, null);
            var t = (deepcopy) ? EmitDeepCopy(arr_element, true) : arr_element;
            EmitConvert(t, 0, CoreTypes.PhpValue);    // (PhpValue)arrplace[i]

            EmitPop(EmitCall(ILOpCode.Call, CoreMethods.PhpArray.Add_PhpValue));    // <array>.Add( value )

            // i++
            _il.EmitLocalLoad(tmpi);
            _il.EmitIntConstant(1);
            _il.EmitOpCode(ILOpCode.Add);
            _il.EmitLocalStore(tmpi);

            // i < params.Length
            _il.MarkLabel(lbl_cond);
            _il.EmitLocalLoad(tmpi);
            arrplace.EmitLoad(_il);
            EmitArrayLength();
            _il.EmitOpCode(ILOpCode.Clt);
            _il.EmitBranch(ILOpCode.Brtrue, lbl_block);

            //
            ReturnTemporaryLocal(tmpi);
            ReturnTemporaryLocal(tmparr);

            //
            _il.EmitLocalLoad(tmparr);
            return (TypeSymbol)tmparr.Type;
        }

        public void EmitUnset(BoundReferenceExpression expr)
        {
            Debug.Assert(expr != null);

            if (!expr.Access.IsUnset)
                throw new ArgumentException();

            var place = expr.BindPlace(this);
            Debug.Assert(place != null);

            place.EmitStorePrepare(this);
            place.EmitStore(this, null);
        }

        /// <summary>
        /// Emits <c>Array.Length</c> call expecting an array instance on top of the stack, returning <c>int</c>.
        /// </summary>
        internal void EmitArrayLength()
        {
            EmitOpCode(ILOpCode.Ldlen);
            EmitOpCode(ILOpCode.Conv_i4);
        }

        /// <summary>
        /// Emits call to given method.
        /// </summary>
        /// <param name="code">Call op code, Call, Callvirt, Calli.</param>
        /// <param name="method">Method reference.</param>
        /// <returns>Method return type.</returns>
        internal TypeSymbol EmitCall(ILOpCode code, MethodSymbol method)
        {
            return _il.EmitCall(_moduleBuilder, _diagnostics, code, method);
        }

        internal TypeSymbol EmitCall(ILOpCode code, MethodSymbol method, BoundExpression thisExpr, ImmutableArray<BoundExpression> arguments, BoundTypeRef staticType = null)
        {
            Contract.ThrowIfNull(method);

            TypeSymbol thisType;

            // <this>
            if (thisExpr != null)
            {
                if (method.HasThis)
                {
                    // <thisExpr> -> <TObject>
                    EmitConvert(thisExpr, thisType = method.ContainingType);

                    if (thisType.IsValueType)
                    {
                        EmitStructAddr(thisType);   // value -> valueaddr
                    }
                }
                else
                {
                    // POP <thisExpr>
                    EmitPop(Emit(thisExpr));
                    thisType = null;
                }
            }
            else
            {
                if (method.HasThis && code != ILOpCode.Newobj)
                {
                    if (ThisPlaceOpt != null && ThisPlaceOpt.TypeOpt != null &&
                        ThisPlaceOpt.TypeOpt.IsEqualToOrDerivedFrom(method.ContainingType))
                    {
                        // implicit $this instance
                        thisType = EmitThis();
                    }
                    else
                    {
                        throw new ArgumentException();  // TODO: PHP would create temporary instance of class
                    }
                }
                else
                {
                    thisType = null;
                }
            }

            // .callvirt -> .call
            if (code == ILOpCode.Callvirt && (!method.HasThis || !method.IsMetadataVirtual()))
            {
                code = ILOpCode.Call;
            }

            // arguments
            var parameters = method.Parameters;
            int arg_index = 0;      // next argument to be emitted from <arguments>
            var param_index = 0;    // loaded parameters
            var writebacks = new List<WriteBackInfo>();

            for (; param_index < parameters.Length; param_index++)
            {
                var p = parameters[param_index];

                // special implicit parameters
                if (arg_index == 0 && p.IsImplicitlyDeclared && !p.IsParams)
                {
                    if (SpecialParameterSymbol.IsContextParameter(p))
                    {
                        // <ctx>
                        Debug.Assert(p.Type == CoreTypes.Context);
                        EmitLoadContext();
                    }
                    else if (SpecialParameterSymbol.IsLocalsParameter(p))
                    {
                        // <locals>
                        Debug.Assert(p.Type == CoreTypes.PhpArray);
                        if (!this.HasUnoptimizedLocals) throw new InvalidOperationException();
                        LocalsPlaceOpt.EmitLoad(Builder)
                            .Expect(CoreTypes.PhpArray);
                    }
                    else if (SpecialParameterSymbol.IsCallerArgsParameter(p))
                    {
                        // ((NamedTypeSymbol)p.Type).TypeParameters // TODO: IList<T>
                        Emit_ArgsArray(CoreTypes.PhpValue); // TODO: T
                    }
                    else if (SpecialParameterSymbol.IsCallerClassParameter(p))
                    {
                        if (p.Type == CoreTypes.PhpTypeInfo)
                        {
                            if (this.CallerType != null)
                                BoundTypeRef.EmitLoadPhpTypeInfo(this, this.CallerType);
                            else
                                Builder.EmitNullConstant();
                        }
                        else if (p.Type.SpecialType == SpecialType.System_String)
                        {
                            Builder.EmitStringConstant(this.CallerType != null
                                ? ((IPhpTypeSymbol)this.CallerType).FullName.ToString()
                                : null);
                        }
                        else
                        {
                            if (p.Type == CoreTypes.RuntimeTypeHandle)
                            {
                                // LOAD <RuntimeTypeHandle>
                                this.EmitLoadToken(this.CallerType, null);
                            }
                            else
                            {
                                throw ExceptionUtilities.UnexpectedValue(p.Type);
                            }
                        }
                    }
                    else if (SpecialParameterSymbol.IsLateStaticParameter(p))
                    {
                        // PhpTypeInfo
                        if (staticType != null)
                        {
                            // LOAD <statictype>
                            staticType.EmitLoadTypeInfo(this);
                        }
                        else if (thisType != null)
                        {
                            // LOAD PhpTypeInfo<thisType>
                            BoundTypeRef.EmitLoadPhpTypeInfo(this, thisType);
                        }
                        else
                        {
                            throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    continue;
                }

                // load arguments
                if (p.IsParams)
                {
                    Debug.Assert(p.Type.IsArray());

                    // wrap remaining arguments to array
                    var values = (arg_index < arguments.Length) ? arguments.Skip(arg_index).ToArray() : Array.Empty<BoundExpression>();
                    arg_index += values.Length;
                    Emit_NewArray(((ArrayTypeSymbol)p.Type).ElementType, values);
                    break;  // p is last one
                }

                if (arg_index < arguments.Length)
                {
                    EmitLoadArgument(p, arguments[arg_index++], writebacks);
                }
                else
                {
                    EmitParameterDefaultValue(p);
                }
            }

            // emit remaining not used arguments
            for (; arg_index < arguments.Length; arg_index++)
            {
                EmitPop(Emit(arguments[arg_index]));
            }

            // call the method
            var result = EmitCall(code, method);

            //
            WriteBackInfo.WriteBackAndFree(this, writebacks);

            //
            return result;
        }

        /// <summary>
        /// Emits .call to <paramref name="target"/> assuming it takes the same arguments as passed to the caller method (<paramref name="thismethod"/>).
        /// </summary>
        /// <param name="target">Method to be called.</param>
        /// <param name="thismethod">Current method.</param>
        /// <returns>Return of <paramref name="target"/>.</returns>
        internal TypeSymbol EmitThisCall(MethodSymbol target, MethodSymbol thismethod)
        {
            if (target == null)
            {
                return CoreTypes.Void;
            }

            if (target.HasThis)
            {
                Debug.Assert(thismethod.HasThis);
                Debug.Assert(this.ThisPlaceOpt != null);
                Debug.Assert(this.ThisPlaceOpt.TypeOpt.IsEqualToOrDerivedFrom(target.ContainingType));
                this.EmitThis();
            }

            var targetps = target.Parameters;
            var givenps = thismethod.Parameters;
            var writebacks = new List<WriteBackInfo>();

            int srcp = 0;
            while (srcp < givenps.Length && givenps[srcp].IsImplicitlyDeclared && !givenps[srcp].IsParams)
            {
                srcp++;
            }

            for (int i = 0; i < targetps.Length; i++)
            {
                var targetp = targetps[i];
                if (targetp.IsImplicitlyDeclared && !targetp.IsParams)
                {
                    if (SpecialParameterSymbol.IsContextParameter(targetp))
                    {
                        this.EmitLoadContext();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    if (srcp < givenps.Length)
                    {
                        var p = givenps[srcp];
                        EmitLoadArgument(
                            targetp,
                            new BoundVariableRef(new BoundVariableName(new VariableName(p.MetadataName)))
                            {
                                Variable = new BoundParameter(p, null),
                                Access = BoundAccess.Read
                            },
                            writebacks);
                    }
                    else
                    {
                        EmitParameterDefaultValue(targetp);
                    }

                    srcp++;
                }
            }

            //
            var result = this.EmitCall(ILOpCode.Call, target);

            //
            WriteBackInfo.WriteBackAndFree(this, writebacks);

            //
            return result;
        }

        /// <summary>
        /// Emits necessary conversion and copying of value returned from a method call.
        /// </summary>
        /// <param name="stack">Result value type on stack.</param>
        /// <param name="method">Called method.</param>
        /// <param name="access">Expression access.</param>
        /// <returns>New type on stack.</returns>
        internal TypeSymbol EmitMethodAccess(TypeSymbol stack, MethodSymbol method, BoundAccess access)
        {
            // cast -1 or null to false (CastToFalse) 
            // and copy the value on stack if necessary
            if (access.IsRead && method.CastToFalse)
            {
                // casts to false and copy the value
                stack = EmitCastToFalse(stack, access.IsReadCopy);
            }
            else if (access.IsReadCopy)
            {
                // copy the value
                stack = EmitDeepCopy(stack);
            }

            //
            return stack;
        }

        /// <summary>
        /// Converts <b>negative</b> number or <c>null</c> to <c>FALSE</c>.
        /// </summary>
        /// <param name="stack">Type of value on stack.</param>
        /// <param name="deepcopy">Whether to deep copy returned non-FALSE value.</param>
        /// <returns>New type of value on stack.</returns>
        internal TypeSymbol EmitCastToFalse(TypeSymbol stack, bool deepcopy)
        {
            if (stack.SpecialType == SpecialType.System_Boolean)
            {
                return stack;
            }

            // Template: <stack> ?? FALSE

            var lblfalse = new NamedLabel("CastToFalse:FALSE");
            var lblend = new NamedLabel("CastToFalse:end");

            _il.EmitOpCode(ILOpCode.Dup);   // <stack>

            // emit branching to lblfalse
            if (stack.SpecialType == SpecialType.System_Int32)
            {
                _il.EmitIntConstant(0);     // 0
                _il.EmitBranch(ILOpCode.Blt, lblfalse);
            }
            else if (stack.SpecialType == SpecialType.System_Int64)
            {
                _il.EmitLongConstant(0);    // 0L
                _il.EmitBranch(ILOpCode.Blt, lblfalse);
            }
            else if (stack.SpecialType == SpecialType.System_Double)
            {
                _il.EmitDoubleConstant(0.0);    // 0.0
                _il.EmitBranch(ILOpCode.Blt, lblfalse);
            }
            else if (stack.IsReferenceType)
            {
                _il.EmitNullConstant(); // null
                _il.EmitBranch(ILOpCode.Beq, lblfalse);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(stack);
            }

            // test(<stack>) ? POP,FALSE : (PhpValue)<stack>

            if (deepcopy)
            {
                // DeepCopy(<stack>)
                stack = EmitDeepCopy(stack, false);
            }
            // (PhpValue)<stack>
            EmitConvertToPhpValue(stack, 0);
            _il.EmitBranch(ILOpCode.Br, lblend);

            // POP, PhpValue.False
            _il.MarkLabel(lblfalse);
            EmitPop(stack);
            _il.EmitBoolConstant(false);
            EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Boolean);

            //
            _il.MarkLabel(lblend);

            //
            return CoreTypes.PhpValue;
        }

        /// <summary>
        /// Initializes place with a default value.
        /// This applies to structs without default ctor that won't work properly when uninitialized.
        /// </summary>
        internal void EmitInitializePlace(IPlace place)
        {
            Contract.ThrowIfNull(place);
            var t = place.TypeOpt;
            Contract.ThrowIfNull(t);

            switch (t.SpecialType)
            {
                // we don't have to initialize those:
                case SpecialType.System_Boolean:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Double:
                case SpecialType.System_Object:
                    break;

                default:
                    if (t.IsValueType || t == CoreTypes.PhpAlias)   // PhpValue, PhpNumber, PhpAlias
                    {
                        // fld = default(T)
                        place.EmitStorePrepare(_il);
                        EmitLoadDefault(t, 0);
                        place.EmitStore(_il);
                    }
                    break;
            }
        }

        /// <summary>
        /// Temporary data used to call routines that expect ref or out parameters when given variable can't be passed by ref.
        /// </summary>
        struct WriteBackInfo
        {
            /// <summary>
            /// The temporary local passed by reference to a function call.
            /// After the call, it's value has to be written back to <see cref="Target"/>.
            /// </summary>
            public LocalDefinition TmpLocal;

            /// <summary>
            /// Original variable passed to the function call.
            /// Target of the write-back routine.
            /// </summary>
            public BoundReferenceExpression Target;

            /// <summary>
            /// Loads temporary local variable as an argument to <paramref name="targetp"/>.
            /// </summary>
            /// <param name="cg"></param>
            /// <param name="targetp">Target parameter.</param>
            /// <param name="expr">Value to be passed as its argument.</param>
            /// <returns><see cref="WriteBackInfo"/> which has to be finalized with <see cref="WriteBackAndFree(CodeGenerator)"/> once the routine call ends.</returns>
            public static WriteBackInfo CreateAndLoad(CodeGenerator cg, ParameterSymbol targetp, BoundReferenceExpression expr)
            {
                var writeback = new WriteBackInfo()
                {
                    TmpLocal = cg.GetTemporaryLocal(targetp.Type),
                    Target = expr,
                };

                //
                writeback.EmitLoadArgument(cg, targetp);

                //
                return writeback;
            }

            void EmitLoadArgument(CodeGenerator cg, ParameterSymbol targetp)
            {
                Debug.Assert(TmpLocal != null);
                Debug.Assert(targetp.Type == (Symbol)TmpLocal.Type);

                if (targetp.RefKind != RefKind.Out)
                {
                    // copy Target to TmpLocal
                    // Template: TmpLocal = Target;
                    cg.EmitConvert(Target, (TypeSymbol)TmpLocal.Type);
                    cg.Builder.EmitLocalStore(TmpLocal);
                }

                if (targetp.RefKind != RefKind.None)
                {
                    // LOAD_REF TmpLocal
                    cg.Builder.EmitLocalAddress(TmpLocal);
                }
                else
                {
                    // unreachable
                    // LOAD TmpLocal
                    cg.Builder.EmitLocalLoad(TmpLocal);
                }
            }

            /// <summary>
            /// Writes the value back to <see cref="Target"/> and free resources.
            /// </summary>
            public void WriteBackAndFree(CodeGenerator cg)
            {
                // Template: <Target> = <TmpLocal>;
                var place = Target.BindPlace(cg);
                place.EmitStorePrepare(cg, null);
                cg.Builder.EmitLocalLoad(TmpLocal);
                place.EmitStore(cg, (TypeSymbol)TmpLocal.Type);

                // free <TmpLocal>
                cg.ReturnTemporaryLocal(TmpLocal);
                TmpLocal = null;
            }

            public static void WriteBackAndFree(CodeGenerator cg, IList<WriteBackInfo> writebacks)
            {
                if (writebacks != null && writebacks.Count != 0)
                {
                    foreach (var w in writebacks)
                    {
                        w.WriteBackAndFree(cg);
                    }
                }
            }
        }

        /// <summary>
        /// Loads argument 
        /// </summary>
        void EmitLoadArgument(ParameterSymbol targetp, BoundExpression expr, List<WriteBackInfo> writebacks)
        {
            if (targetp.RefKind == RefKind.None)
            {
                EmitConvert(expr, targetp.Type); // load argument
            }
            else
            {
                var refexpr = expr as BoundReferenceExpression;
                if (refexpr != null)
                {
                    var place = refexpr.Place(_il);
                    if (place != null && place.HasAddress && place.TypeOpt == targetp.Type)
                    {
                        // ref place directly
                        place.EmitLoadAddress(_il);
                        return;
                    }

                    // write-back
                    writebacks.Add(WriteBackInfo.CreateAndLoad(this, targetp, refexpr));
                    return;
                }
                else
                {
                    throw new ArgumentException("Argument must be passed as a variable.");
                }
            }
        }

        /// <summary>
        /// Emits default value of given parameter.
        /// Puts value of target parameter's type.
        /// </summary>
        /// <param name="targetp">Parameter to emit its default value.</param>
        internal void EmitParameterDefaultValue(ParameterSymbol targetp)
        {
            Contract.ThrowIfNull(targetp);

            //
            TypeSymbol ptype; // emitted type to be eventually converted to target parameter type

            // emit targetp default value:
            ConstantValue cvalue;
            BoundExpression boundinitializer;

            if ((cvalue = targetp.ExplicitDefaultConstantValue) != null)
            {
                // keep NULL if parameter is a reference type
                if (cvalue.IsNull && targetp.Type.IsReferenceType && targetp.Type != CoreTypes.PhpAlias)
                {
                    _il.EmitNullConstant();
                    return;
                }

                //
                ptype = EmitLoadConstant(cvalue.Value, targetp.Type);
            }
            else if ((boundinitializer = (targetp as SourceParameterSymbol)?.Initializer) != null)
            {
                using (var cg = new CodeGenerator(this, (SourceRoutineSymbol)targetp.ContainingSymbol))
                {
                    cg.EmitConvert(boundinitializer, ptype = targetp.Type);
                }
            }
            else if (targetp.IsParams)
            {
                // new T[0]
                Emit_NewArray(((ArrayTypeSymbol)targetp.Type).ElementType, Array.Empty<BoundExpression>());
                return;
            }
            else
            {
                ptype = targetp.Type;
                EmitLoadDefault(ptype, 0);
            }

            // eventually convert emitted value to target parameter type
            EmitConvert(ptype, 0, targetp.Type);
        }

        internal TypeSymbol EmitGetProperty(IPlace holder, PropertySymbol prop)
        {
            Debug.Assert(prop.IsStatic || holder != null);
            Debug.Assert(prop.GetMethod != null);
            Debug.Assert(prop.GetMethod.ParameterCount == 0);

            var getter = prop.GetMethod;

            if (holder != null && !getter.IsStatic)
            {
                Debug.Assert(holder.TypeOpt != null);
                if (holder.TypeOpt.IsValueType)
                {
                    holder.EmitLoadAddress(_il);
                }
                else
                {
                    holder.EmitLoad(_il);
                }
            }

            return EmitCall(getter.IsMetadataVirtual() ? ILOpCode.Callvirt : ILOpCode.Call, getter);
        }

        internal void EmitCastClass(TypeSymbol from, TypeSymbol to)
        {
            if (!from.IsEqualToOrDerivedFrom(to))
            {
                EmitCastClass(to);
            }
        }

        internal void EmitCastClass(TypeSymbol type)
        {
            // (T)
            _il.EmitOpCode(ILOpCode.Castclass);
            EmitSymbolToken(type, null);
        }

        public void EmitEcho(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);

            // Template: <ctx>.Echo(expr);

            this.EmitLoadContext();
            var type = EmitSpecialize(expr);

            //
            MethodSymbol method = null;

            switch (type.SpecialType)
            {
                case SpecialType.System_Void:
                    EmitPop(this.CoreTypes.Context);
                    return;
                case SpecialType.System_String:
                    method = CoreMethods.Operators.Echo_String.Symbol;
                    break;
                case SpecialType.System_Double:
                    method = CoreMethods.Operators.Echo_Double.Symbol;
                    break;
                case SpecialType.System_Int32:
                    method = CoreMethods.Operators.Echo_Int32.Symbol;
                    break;
                case SpecialType.System_Int64:
                    method = CoreMethods.Operators.Echo_Long.Symbol;
                    break;
                case SpecialType.System_Boolean:
                    // TODO: if (VALUE) echo("1");
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Bool).Expect(SpecialType.System_String);
                    method = CoreMethods.Operators.Echo_String.Symbol;
                    break;
                default:
                    if (type == CoreTypes.PhpString)
                    {
                        method = CoreMethods.Operators.Echo_PhpString.Symbol;
                    }
                    else if (type == CoreTypes.PhpNumber)
                    {
                        method = CoreMethods.Operators.Echo_PhpNumber.Symbol;
                    }
                    else if (type == CoreTypes.PhpValue)
                    {
                        method = CoreMethods.Operators.Echo_PhpValue.Symbol;
                    }
                    else if (type == CoreTypes.PhpAlias)
                    {
                        Emit_PhpAlias_GetValue();
                        method = CoreMethods.Operators.Echo_PhpValue.Symbol;
                    }
                    else if (type.IsOfType(CoreTypes.IPhpArray))
                    {
                        this.EmitConvertToString(type, 0);  // Array -> string
                        method = CoreMethods.Operators.Echo_String.Symbol;
                    }
                    else
                    {
                        // TODO: check expr.TypeRefMask if it is only NULL
                        EmitBox(type);
                        method = CoreMethods.Operators.Echo_Object.Symbol;
                    }
                    break;
            }

            //
            Debug.Assert(method != null);
            EmitCall(ILOpCode.Call, method);
        }

        /// <summary>
        /// Emits the expression decorated with error reporting disabling routine.
        /// </summary>
        public TypeSymbol EmitWithDisabledErrorReporting(BoundExpression expr)
        {
            //		context.DisableErrorReporting();
            //		<expr>
            //		context.EnableErrorReporting();

            EmitLoadContext();
            EmitCall(ILOpCode.Callvirt, CoreMethods.Context.DisableErrorReporting)
                .Expect(SpecialType.System_Void);

            var t = Emit(expr);

            EmitLoadContext();
            EmitCall(ILOpCode.Callvirt, CoreMethods.Context.EnableErrorReporting)
                .Expect(SpecialType.System_Void);

            //
            return t;
        }

        public void EmitIntStringKey(int key)
        {
            _il.EmitIntConstant(key);
            EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_int);
        }

        public void EmitIntStringKey(string key)
        {
            _il.EmitStringConstant(key);
            EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_string);
        }

        public void EmitIntStringKey(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);

            var constant = expr.ConstantValue;
            if (constant.HasValue)
            {
                if (constant.Value is string)
                {
                    EmitIntStringKey((string)constant.Value);
                }
                else if (constant.Value is long)
                {
                    EmitIntStringKey((int)(long)constant.Value);
                }
                else if (constant.Value is int)
                {
                    EmitIntStringKey((int)constant.Value);
                }
                else if (constant.Value is double)
                {
                    EmitIntStringKey((int)(double)constant.Value);
                }
                else
                {
                    throw new NotSupportedException();
                }

                return;
            }

            var t = Emit(expr); // TODO: ConvertToArrayKey
            EmitConvertToIntStringKey(t, 0);
        }

        /// <summary>
        /// Emits declaring function into the context.
        /// </summary>
        public void EmitDeclareFunction(SourceFunctionSymbol f)
        {
            Debug.Assert(f != null);

            var field = f.EnsureRoutineInfoField(_moduleBuilder);

            this.EmitSequencePoint(((FunctionDecl)f.Syntax).HeadingSpan);

            // <ctx>.DeclareFunction(RoutineInfo)
            EmitLoadContext();
            new FieldPlace(null, field).EmitLoad(_il);

            EmitCall(ILOpCode.Call, CoreMethods.Context.DeclareFunction_RoutineInfo);
        }

        /// <summary>
        /// Emits declaring type into the context.
        /// </summary>
        public void EmitDeclareType(SourceTypeSymbol t)
        {
            Debug.Assert(t != null);

            // 
            this.EmitSequencePoint(t.Syntax.HeadingSpan);

            // <ctx>.DeclareType<T>()
            EmitLoadContext();
            EmitCall(ILOpCode.Call, CoreMethods.Context.DeclareType_T.Symbol.Construct(t));
        }

        /// <summary>
        /// Emits call to main method.
        /// </summary>
        /// <param name="mainmethod">Static Main method representing the script global code.</param>
        /// <returns>Main method result value type.</returns>
        public TypeSymbol EmitCallMain(MethodSymbol mainmethod)
        {
            Contract.ThrowIfNull(mainmethod);
            Debug.Assert(mainmethod.IsStatic);
            Debug.Assert(mainmethod.Name == WellKnownPchpNames.GlobalRoutineName);

            foreach (var p in mainmethod.Parameters)
            {
                switch (p.Name)
                {
                    case SpecialParameterSymbol.ContextName:
                        EmitLoadContext();
                        break;
                    case SpecialParameterSymbol.LocalsName:
                        Debug.Assert(LocalsPlaceOpt != null);
                        LocalsPlaceOpt.EmitLoad(_il);
                        break;
                    default:
                        if (p.Name == SpecialParameterSymbol.ThisName)
                        {
                            EmitThisOrNull();
                            break;
                        }
                        throw ExceptionUtilities.UnexpectedValue(p.Name);
                }
            }

            //
            return EmitCall(ILOpCode.Call, mainmethod);
        }

        public TypeSymbol EmitLoadConstant(object value, TypeSymbol targetOpt = null)
        {
            if (value == null)
            {
                if (targetOpt != null)
                {
                    EmitLoadDefault(targetOpt, 0);
                    return targetOpt;
                }
                else
                {
                    Builder.EmitNullConstant();
                    return CoreTypes.Object;
                }
            }
            else
            {
                if (value is int)
                {
                    if (targetOpt != null)
                    {
                        switch (targetOpt.SpecialType)
                        {
                            case SpecialType.System_Boolean:
                                _il.EmitBoolConstant((int)value != 0);
                                return targetOpt;
                            case SpecialType.System_Int64:
                                _il.EmitLongConstant((long)(int)value);
                                return targetOpt;
                            case SpecialType.System_String:
                                _il.EmitStringConstant(value.ToString());
                                return targetOpt;
                        }
                    }

                    Builder.EmitIntConstant((int)value);
                    return CoreTypes.Int32;
                }
                else if (value is long)
                {
                    if (targetOpt != null)
                    {
                        switch (targetOpt.SpecialType)
                        {
                            case SpecialType.System_Boolean:
                                _il.EmitBoolConstant((long)value != 0);
                                return targetOpt;
                            case SpecialType.System_Int32:
                                _il.EmitIntConstant((int)(long)value);
                                return targetOpt;
                            case SpecialType.System_String:
                                _il.EmitStringConstant(value.ToString());
                                return targetOpt;
                        }
                    }

                    Builder.EmitLongConstant((long)value);
                    return CoreTypes.Long;
                }
                else if (value is string)
                {
                    Builder.EmitStringConstant((string)value);
                    return CoreTypes.String;
                }
                else if (value is bool)
                {
                    if (targetOpt != null)
                    {
                        switch (targetOpt.SpecialType)
                        {
                            case SpecialType.System_String:
                                _il.EmitStringConstant((bool)value ? "1" : "");
                                return targetOpt;
                        }
                    }

                    Builder.EmitBoolConstant((bool)value);
                    return CoreTypes.Boolean;
                }
                else if (value is double)
                {
                    if (targetOpt != null)
                    {
                        switch (targetOpt.SpecialType)
                        {
                            case SpecialType.System_Boolean:
                                _il.EmitBoolConstant((double)value != 0.0);
                                return targetOpt;
                            case SpecialType.System_Int64:
                                _il.EmitLongConstant((long)(double)value);
                                return targetOpt;
                        }
                    }

                    Builder.EmitDoubleConstant((double)value);
                    return CoreTypes.Double;
                }
                else if (value is float)
                {
                    Builder.EmitSingleConstant((float)value);
                    return DeclaringCompilation.GetSpecialType(SpecialType.System_Single);
                }
                else if (value is uint)
                {
                    Builder.EmitIntConstant(unchecked((int)(uint)value));
                    return DeclaringCompilation.GetSpecialType(SpecialType.System_UInt32);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(value);
                }
            }
        }

        public void EmitLoadDefault(TypeSymbol type, TypeRefMask typemask)
        {
            Debug.Assert(type != null);

            switch (type.SpecialType)
            {
                case SpecialType.System_Void:
                    break;
                case SpecialType.System_Double:
                    _il.EmitDoubleConstant(0.0);
                    break;
                case SpecialType.System_Int32:
                    _il.EmitIntConstant(0);
                    break;
                case SpecialType.System_Int64:
                    _il.EmitLongConstant(0);
                    break;
                case SpecialType.System_Boolean:
                    _il.EmitBoolConstant(false);
                    break;
                case SpecialType.System_String:
                    _il.EmitStringConstant(string.Empty);
                    break;
                default:
                    if (type == CoreTypes.PhpAlias)
                    {
                        // new PhpAlias(void, 1);
                        Emit_PhpValue_Void();
                        Emit_PhpValue_MakeAlias();
                    }
                    //else if (CoreTypes.PhpArray.Symbol.IsOfType(type))
                    //{
                    //    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpArray);
                    //}
                    //else if (type == CoreTypes.PhpString)
                    //{
                    //    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpString);
                    //}
                    else if (type.IsReferenceType)
                    {
                        _il.EmitNullConstant();
                    }
                    else
                    {
                        if (type == CoreTypes.PhpValue)
                        {
                            if (typemask.IsSingleType && this.Routine != null)
                            {
                                var typectx = this.TypeRefContext;

                                if (typectx.IsBoolean(typemask))
                                {
                                    _il.EmitBoolConstant(false);
                                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Boolean);
                                    break;
                                }
                                else if (typectx.IsLong(typemask))
                                {
                                    _il.EmitLongConstant(0);
                                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Long);
                                    break;
                                }
                                else if (typectx.IsDouble(typemask))
                                {
                                    _il.EmitDoubleConstant(0.0);
                                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Double);
                                    break;
                                }
                                else if (typectx.IsAString(typemask))
                                {
                                    // return ""
                                    _il.EmitStringConstant(string.Empty);
                                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_String);
                                    break;
                                }
                                else if (typectx.IsArray(typemask))
                                {
                                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpArray);
                                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_PhpArray);
                                    break;
                                }
                            }
                        }

                        //
                        EmitLoadDefaultOfValueType(type);
                    }
                    break;
            }
        }

        /// <summary>
        /// Emits <c>default(valuetype)</c>.
        /// Handles special types with a default ctor.
        /// </summary>
        public void EmitLoadDefaultOfValueType(TypeSymbol valuetype)
        {
            Debug.Assert(valuetype != null && valuetype.IsValueType);

            if (valuetype == CoreTypes.PhpNumber)
            {
                // PhpNumber.Default ~ 0L
                _il.EmitOpCode(ILOpCode.Ldsfld);
                EmitSymbolToken(CoreMethods.PhpNumber.Default, null);
            }
            else if (valuetype == CoreTypes.PhpValue)
            {
                // PhpValue.Null
                Emit_PhpValue_Null();
            }
            else
            {
                // default(T)

                var loc = this.GetTemporaryLocal(valuetype, true);

                // ldloca <loc>
                // .initobj <type>
                Builder.EmitLocalAddress(loc);
                Builder.EmitOpCode(ILOpCode.Initobj);
                EmitSymbolToken(valuetype, null);
                // ldloc <loc>
                Builder.EmitLocalLoad(loc);
            }
        }

        public void EmitRetDefault()
        {
            // return default(RETURN_TYPE);

            var return_type = this.Routine.ReturnType;

            EmitLoadDefault(return_type, this.Routine.ResultTypeMask);
            EmitRet(return_type);
        }

        /// <summary>
        /// Emits .ret instruction with sequence point at closing brace.
        /// </summary>
        public void EmitRet(TypeSymbol stack)
        {
            // sequence point
            var body = AstUtils.BodySpanOrInvalid(Routine?.Syntax);
            if (body.IsValid)
            {
                EmitSequencePoint(new Span(body.End - 1, 1));
                EmitOpCode(ILOpCode.Nop);
            }

            //
            if (_il.InExceptionHandler)
            {
                ((ExitBlock)this.Routine.ControlFlowGraph.Exit).EmitTmpRet(this, stack);
            }
            else
            {
                _il.EmitRet(stack.SpecialType == SpecialType.System_Void);
            }
        }

        /// <summary>
        /// Emits <c>place != null</c> expression.
        /// </summary>
        public void EmitNotNull(IPlace place)
        {
            Debug.Assert(place != null);
            Debug.Assert(place.TypeOpt.IsReferenceType);

            // {place} != null : boolean
            place.EmitLoad(_il);
            _il.EmitNullConstant();
            _il.EmitOpCode(ILOpCode.Cgt_un);
        }

        /// <summary>
        /// Emits <c>Debug.Assert([<paramref name="place"/>]) in debug compile mode.</c>
        /// </summary>
        /// <param name="place">The variable to emit assertion for.</param>
        /// <param name="messageOpt">Optional second argument for assert.</param>
        public void EmitDebugAssertNotNull(IPlace place, string messageOpt = null)
        {
            if (IsDebug)
            {
                //EmitNotNull(place);
                //EmitDebugAssert(messageOpt);
            }
        }

        /// <summary>
        /// Emits <c>Debug.Assert([stack]).</c>
        /// </summary>
        /// <param name="messageOpt">Optional second argument for assert.</param>
        public void EmitDebugAssert(string messageOpt = null)
        {
            //var dt = this.DeclaringCompilation.GetTypeByMetadataName("System.Diagnostics.Debug"); // System.dll
            //dt.GetMember("Assert")
            throw new NotImplementedException();
        }

        public TypeSymbol EmitDeepCopy(TypeSymbol t, bool nullcheck)
        {
            if (IsCopiable(t))
            {
                object lblnull = null;
                if (nullcheck && t.IsReferenceType)
                {
                    if (nullcheck)
                    {
                        // ?.
                        var lbltrue = new object();
                        lblnull = new object();

                        _il.EmitOpCode(ILOpCode.Dup);
                        _il.EmitBranch(ILOpCode.Brtrue, lbltrue);
                        _il.EmitOpCode(ILOpCode.Pop);
                        _il.EmitNullConstant();
                        _il.EmitBranch(ILOpCode.Br, lblnull);
                        _il.MarkLabel(lbltrue);
                    }
                }

                if (t == CoreTypes.PhpValue)
                {
                    EmitPhpValueAddr();
                    t = EmitCall(ILOpCode.Call, CoreMethods.PhpValue.DeepCopy);
                }
                else if (t == CoreTypes.PhpString)
                {
                    t = EmitCall(ILOpCode.Callvirt, CoreMethods.PhpString.DeepCopy);
                }
                else if (t == CoreTypes.PhpArray)
                {
                    t = EmitCall(ILOpCode.Callvirt, CoreMethods.PhpArray.DeepCopy);
                }

                //
                if (lblnull != null)
                {
                    _il.MarkLabel(lblnull);
                }
            }

            return t;
        }

        /// <summary>
        /// Emits copy of value from top of the stack if necessary.
        /// </summary>
        public TypeSymbol EmitDeepCopy(TypeSymbol t, TypeRefMask thint = default(TypeRefMask))
        {
            if (IsCopiable(thint))
            {
                return EmitDeepCopy(t, thint.IsAnyType || thint.IsUninitialized || this.TypeRefContext.IsNull(thint));
            }
            else
            {
                return t;
            }
        }

        /// <summary>
        /// Emit dereference and deep copy if necessary.
        /// </summary>
        public TypeSymbol EmitReadCopy(TypeSymbol targetOpt, TypeSymbol type, TypeRefMask thint = default(TypeRefMask))
        {
            // dereference & copy

            // if target type is not a copiable type, we don't have to perform deep copy since the result will be converted to a value anyway
            var deepcopy = IsCopiable(thint) && (targetOpt == null || IsCopiable(targetOpt));
            if (!deepcopy)
            {
                return type;
            }

            // dereference

            if (type == CoreTypes.PhpValue)
            {
                // ref.GetValue().DeepCopy()
                EmitPhpValueAddr();
                type = EmitCall(ILOpCode.Call, CoreMethods.PhpValue.GetValue);
            }
            else if (type == CoreTypes.PhpAlias)
            {
                // ref.Value.DeepCopy()
                Emit_PhpAlias_GetValueAddr();
                return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.DeepCopy);
            }

            // copy

            return EmitDeepCopy(type);
        }
    }

    internal static class ILBuilderExtension
    {
        public static void EmitLoadToken(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, TypeSymbol type, SyntaxNode syntaxNodeOpt)
        {
            il.EmitOpCode(ILOpCode.Ldtoken);
            EmitSymbolToken(il, module, diagnostics, type, syntaxNodeOpt);
        }

        public static void EmitLoadToken(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, MethodSymbol method, SyntaxNode syntaxNodeOpt)
        {
            il.EmitOpCode(ILOpCode.Ldtoken);
            EmitSymbolToken(il, module, diagnostics, method, syntaxNodeOpt);
        }

        public static void EmitSymbolToken(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, TypeSymbol symbol, SyntaxNode syntaxNode)
        {
            il.EmitToken(module.Translate(symbol, syntaxNode, diagnostics), syntaxNode, diagnostics);
        }

        public static void EmitSymbolToken(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, MethodSymbol symbol, SyntaxNode syntaxNode)
        {
            il.EmitToken(module.Translate(symbol, syntaxNode, diagnostics, true), syntaxNode, diagnostics);
        }

        public static void EmitSymbolToken(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, FieldSymbol symbol, SyntaxNode syntaxNode)
        {
            il.EmitToken(symbol, syntaxNode, diagnostics);
        }

        /// <summary>
        /// Emits call to given method.
        /// </summary>
        /// <returns>Method return type.</returns>
        public static TypeSymbol EmitCall(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, ILOpCode code, MethodSymbol method)
        {
            Contract.ThrowIfNull(method);
            Debug.Assert(code == ILOpCode.Call || code == ILOpCode.Calli || code == ILOpCode.Callvirt || code == ILOpCode.Newobj);
            Debug.Assert(!method.IsErrorMethod());

            var stack = method.GetCallStackBehavior();

            if (code == ILOpCode.Newobj)
                stack += 1 + 1;    // there is no <this>, + it pushes <newinst> on stack

            il.EmitOpCode(code, stack);
            il.EmitToken(module.Translate(method, diagnostics, false), null, diagnostics);
            return (code == ILOpCode.Newobj) ? method.ContainingType : method.ReturnType;
        }
    }
}
