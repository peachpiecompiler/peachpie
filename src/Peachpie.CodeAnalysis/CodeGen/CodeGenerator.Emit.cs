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

        /// <summary>
        /// Emits <c>RuntimeTypeHandle</c> of current class context.
        /// </summary>
        public void EmitCallerRuntimeTypeHandle()
        {
            var caller = this.CallerType;
            if (caller != null)
            {
                // RuntimeTypeHandle
                EmitLoadToken(caller, null);
            }
            else if (this.Routine is SourceGlobalMethodSymbol global)
            {
                global.SelfParameter.EmitLoad(_il)
                    .Expect(CoreTypes.RuntimeTypeHandle);
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
        /// Gets place referring to compiler generated temporal variables.
        /// </summary>
        /// <remarks>
        /// Must not be null for methods that contain any synthesized variables.
        /// </remarks>
        public IPlace TemporalLocalsPlace => _tmpLocalsPlace;

        /// <summary>
        /// Gets value indicating the routine uses unoptimized locals access.
        /// This means, all the local variables are stored within an associative array instead of local slots.
        /// This value implicates, <see cref="LocalsPlaceOpt"/> is not <c>null</c>.
        /// </summary>
        public bool HasUnoptimizedLocals => LocalsPlaceOpt != null;

        /// <summary>
        /// Gets value indicating the routine has locals already inicialized. 
        /// </summary>
        public bool InitializedLocals => _localsInitialized;

        /// <summary>
        /// Emits reference to <c>this</c>.
        /// </summary>
        /// <returns>Type of <c>this</c> in current context, pushed on top of the evaluation stack.</returns>
        public NamedTypeSymbol EmitThis()
        {
            Contract.ThrowIfNull(_thisPlace);
            return EmitThisOrNull();
        }

        public NamedTypeSymbol EmitThisOrNull()
        {
            if (_thisPlace == null)
            {
                _il.EmitNullConstant();
                return CoreTypes.Object;
            }
            else
            {
                return (NamedTypeSymbol)_thisPlace.EmitLoad(_il);
            }
        }

        public TypeSymbol EmitGeneratorInstance()
        {
            Contract.ThrowIfNull(this.GeneratorStateMachineMethod);
            // .ldarg <generator>
            return new ParamPlace(this.GeneratorStateMachineMethod.GeneratorParameter).EmitLoad(_il);
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
            if (!type.IsErrorTypeOrNull())
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
        /// Emits check if type on top of the stack is null.
        /// Results in boolean (<c>i4</c>) with value of <c>0</c> or <c>1</c> on top of the stack.
        /// </summary>
        public void EmitNotNull(TypeSymbol t, TypeRefMask tmask)
        {
            // CanBeNull(tmask)
            // CanBeNull(t)

            // t != null
            if (t.IsReferenceType)
            {
                // != null
                _il.EmitNullConstant();
                _il.EmitOpCode(ILOpCode.Cgt_un);
                return;
            }

            // PhpAlias.Value
            if (t == CoreTypes.PhpAlias)
            {
                // dereference
                t = Emit_PhpAlias_GetValue();
                // continue ->
            }

            // IsSet(PhpValue) ~ !IsNull
            if (t == CoreTypes.PhpValue)
            {
                EmitCall(ILOpCode.Call, CoreMethods.Operators.IsSet_PhpValue);
                return;
            }

            // cannot be null:
            Debug.Assert(!CanBeNull(t));
            EmitPop(t);
            _il.EmitBoolConstant(false);
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

        internal void EmitSymbolToken(MethodSymbol method, SyntaxNode syntaxNode)
        {
            _il.EmitSymbolToken(_moduleBuilder, _diagnostics, method, syntaxNode);
        }

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

        public void Emit_NewArray(TypeSymbol elementType, ImmutableArray<BoundArgument> values) => Emit_NewArray(elementType, values, a => a.Value);
        public void Emit_NewArray(TypeSymbol elementType, ImmutableArray<BoundExpression> values) => Emit_NewArray(elementType, values, a => a);

        public void Emit_NewArray<T>(TypeSymbol elementType, ImmutableArray<T> values, Func<T, BoundExpression> selector)
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
                    EmitConvert(selector(values[i]), elementType);
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

        TypeSymbol Emit_EmptyArray(TypeSymbol elementType)
        {
            // Array.Empty<elementType>()
            var array_empty_T = ((MethodSymbol)this.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Array__Empty))
                .Construct(elementType);

            return EmitCall(ILOpCode.Call, array_empty_T);
        }

        /// <summary>
        /// Emits array of <paramref name="elementType"/> containing all current routine PHP arguments value.
        /// </summary>
        TypeSymbol Emit_ArgsArray(TypeSymbol elementType)
        {
            var routine = this.Routine;
            if (routine == null)
            {
                throw new InvalidOperationException("Routine is null!");
            }

            TypeSymbol arrtype;

            var ps = routine.SourceParameters;
            var variadic = routine.GetParamsParameter();  // optional params
            var variadic_element = (variadic?.Type as ArrayTypeSymbol)?.ElementType;
            var variadic_place = variadic != null ? new ParamPlace(variadic) : null;

            var useparams = (routine is SourceLambdaSymbol) ? ((SourceLambdaSymbol)routine).UseParams.Count : 0;    // lambda function 'use' parameters

            ps = ps.Skip(useparams).Where(p => !p.IsParams).ToArray();  // parameters without implicitly declared parameters

            if (ps.Length == 0 && variadic == null)
            {
                // empty array
                return Emit_EmptyArray(elementType);
            }
            else if (ps.Length == 0 && variadic_element == elementType)
            {
                // == params
                arrtype = variadic_place.EmitLoad(_il);
            }
            else
            {
                arrtype = ArrayTypeSymbol.CreateSZArray(this.DeclaringCompilation.SourceAssembly, elementType);

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
                var tmparr = this.GetTemporaryLocal(arrtype);
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

            //
            return arrtype;
        }

        /// <summary>
        /// Emits <c>PhpValue[]</c> containing given <paramref name="args"/>.
        /// Argument unpacking is taken into account and flatterned.
        /// </summary>
        internal void Emit_ArgumentsIntoArray(ImmutableArray<BoundArgument> args, PhpSignatureMask byrefargs)
        {
            if (args.Length == 0)
            {
                Emit_EmptyArray(CoreTypes.PhpValue);
            }
            else if (args.Last().IsUnpacking)   // => handle unpacking   // last argument must be unpacking otherwise unpacking is not allowed anywhere else
            {
                UnpackArgumentsIntoArray(args, byrefargs);
            }
            else
            {
                Emit_NewArray(CoreTypes.PhpValue, args);
            }
        }

        /// <summary>
        /// Emits <c>PhpValue[]</c> containing given <paramref name="args"/>.
        /// Argument unpacking is taken into account and flatterned.
        /// </summary>
        /// <param name="args">Arguments to be flatterned into a single dimensional array.</param>
        /// <param name="byrefargs">Mask of arguments that must be passed by reference.</param>
        /// <remarks>The method assumes the arguments list contains a variable unpacking. Otherwise this method is not well performance optimized.</remarks>
        /// <returns>Type symbol corresponding to <c>PhpValue[]</c></returns>
        internal TypeSymbol UnpackArgumentsIntoArray(ImmutableArray<BoundArgument> args, PhpSignatureMask byrefargs)
        {
            if (args.IsDefaultOrEmpty)
            {
                // Array.Empty<PhpValue>()
                return Emit_EmptyArray(this.CoreTypes.PhpValue);
            }

            // assuming the arguments list contains a variable unpacking,
            // otherwise we could do this much more efficiently
            Debug.Assert(args.Any(a => a.IsUnpacking));

            // TODO: args.Length == 1 => unpack directly to an array // Template: Unpack(args[0]) => args[0].CopyTo(new PhpValue[args[0].Count])

            // Symbol: List<PhpValue>
            var list_phpvalue = DeclaringCompilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T).Construct(CoreTypes.PhpValue);
            var list_ctor_int = list_phpvalue.InstanceConstructors.Single(m => m.ParameterCount == 1 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32);
            var list_add_PhpValue = list_phpvalue.GetMembers(WellKnownMemberNames.CollectionInitializerAddMethodName).OfType<MethodSymbol>().SingleOrDefault(m => m.ParameterCount == 1 && !m.IsStatic);
            var list_toarray = (MethodSymbol)list_phpvalue.GetMembers("ToArray").Single();

            // Symbol: Operators.Unpack
            var unpack_methods = CoreTypes.Operators.Symbol.GetMembers("Unpack").OfType<MethodSymbol>();
            var unpack_list_value_ulong = unpack_methods.Single(m => m.Parameters[1].Type == CoreTypes.PhpValue);
            var unpack_list_array_ulong = unpack_methods.Single(m => m.Parameters[1].Type == CoreTypes.PhpArray);

            // 1. create temporary List<PhpValue>

            // Template: new List<PhpValue>(COUNT)
            _il.EmitIntConstant(args.Length + 8);   // estimate unpackged arguments count
            EmitCall(ILOpCode.Newobj, list_ctor_int)
                .Expect(list_phpvalue);

            // 2. evaluate arguments and unpack them to the List<PhpValue> (on <STACK>)
            for (int i = 0; i < args.Length; i++)
            {
                _il.EmitOpCode(ILOpCode.Dup);   // .dup <STACK>

                if (args[i].IsUnpacking)
                {
                    // Template: Unpack(<STACK>, args[i], byrefs)
                    var t = Emit(args[i].Value);
                    if (t == CoreTypes.PhpArray)
                    {
                        _il.EmitLongConstant((long)(ulong)byrefargs);    // byref args
                        EmitCall(ILOpCode.Call, unpack_list_array_ulong)
                            .Expect(SpecialType.System_Void);
                    }
                    else
                    {
                        EmitConvert(t, args[i].Value.TypeRefMask, CoreTypes.PhpValue);
                        _il.EmitLongConstant((long)(ulong)byrefargs);    // byref args
                        EmitCall(ILOpCode.Call, unpack_list_value_ulong)
                            .Expect(SpecialType.System_Void);
                    }
                }
                else
                {
                    // Template: <STACK>.Add((PhpValue)args[i])
                    EmitConvert(args[i].Value, CoreTypes.PhpValue);
                    EmitCall(ILOpCode.Call, list_add_PhpValue)
                        .Expect(SpecialType.System_Void);
                }
            }

            // 3. copy the list into PhpValue[]
            // Template: List<PhpValue>.ToArray()
            return EmitCall(ILOpCode.Call, list_toarray);
        }

        /// <summary>
        /// Builds <c>PhpArray</c> out from <c>System.Array</c>.
        /// </summary>
        public TypeSymbol ArrayToPhpArray(IPlace arrplace, bool deepcopy = false)
        {
            var phparr = GetTemporaryLocal(CoreTypes.PhpArray);

            // Template: tmparr = new PhpArray(arrplace.Length)
            arrplace.EmitLoad(_il);
            EmitArrayLength();
            EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpArray_int);
            _il.EmitLocalStore(phparr);

            // enumeration body:
            EmitEnumerateArray(arrplace, 0, (srcindex, element_loader) =>
            {
                // Template: <tmparr>.Add((T)arrplace[i])
                _il.EmitLocalLoad(phparr);   // <array>
                var t = element_loader();   // arrplace[i]
                t = (deepcopy) ? EmitDeepCopy(t, true) : t;
                EmitConvert(t, 0, CoreTypes.PhpValue);    // (PhpValue)arrplace[i]
                EmitPop(EmitCall(ILOpCode.Call, CoreMethods.PhpArray.Add_PhpValue));    // <array>.Add( value )
            });

            _il.EmitLocalLoad(phparr);

            ReturnTemporaryLocal(phparr);

            //
            return (TypeSymbol)phparr.Type;
        }

        /// <summary>
        /// Creates array from source array.
        /// </summary>
        /// <param name="srcarray">Source array.</param>
        /// <param name="srcfrom">First element to be copied.</param>
        /// <param name="targetArrElement">Target array element type.</param>
        /// <returns>Type of target array which is left on top of stack.</returns>
        public TypeSymbol ArrayToNewArray(IPlace srcarray, int srcfrom, TypeSymbol targetArrElement)
        {
            // Template: <tmplength> = srcarray.Length - srcfrom
            srcarray.EmitLoad(_il);
            EmitArrayLength();
            _il.EmitIntConstant(srcfrom);
            _il.EmitOpCode(ILOpCode.Sub);
            var tmplength = GetTemporaryLocal(CoreTypes.Int32);
            _il.EmitLocalStore(tmplength);

            // Template: if (tmplength > 0) ... else { Array.Empty<T>(); }

            var lblempty = new object();
            var lblend = new object();

            _il.EmitLocalLoad(tmplength);
            _il.EmitIntConstant(0);
            _il.EmitBranch(ILOpCode.Ble, lblempty); // length <= 0 : goto lblempty;

            // <tmparr> = new T[tmplength]
            _il.EmitLocalLoad(tmplength);
            _il.EmitOpCode(ILOpCode.Newarr);
            EmitSymbolToken(targetArrElement, null);
            var tmparr = GetTemporaryLocal(ArrayTypeSymbol.CreateSZArray(this.DeclaringCompilation.SourceAssembly, targetArrElement));
            _il.EmitLocalStore(tmparr);

            ReturnTemporaryLocal(tmplength);

            // enumerator body:
            EmitEnumerateArray(srcarray, srcfrom, (srcindex, element_loader) =>
            {
                // Template: tmparr[srcindex - srcfrom] = (T)element_loader();
                _il.EmitLocalLoad(tmparr);

                _il.EmitLocalLoad(srcindex);    // srcindex
                _il.EmitIntConstant(srcfrom);   // srcfrom
                _il.EmitOpCode(ILOpCode.Sub);   // -

                EmitConvert(element_loader(), 0, targetArrElement);  // (T) LOAD source element

                _il.EmitOpCode(ILOpCode.Stelem);    // STORE
                EmitSymbolToken(targetArrElement, null);
            });

            _il.EmitLocalLoad(tmparr);  // LOAD tmparr
            _il.EmitBranch(ILOpCode.Br, lblend);    // goto end;

            // lblempty:
            _il.MarkLabel(lblempty);
            Emit_EmptyArray(targetArrElement);

            // lblend:
            _il.MarkLabel(lblend);

            //
            ReturnTemporaryLocal(tmparr);
            return (TypeSymbol)tmparr.Type;
        }

        /// <summary>
        /// Emits for-loop of elements in given array provided through <paramref name="arrplace"/>.
        /// </summary>
        /// <param name="arrplace">Place representing source array.</param>
        /// <param name="startindex">First element index to enumerato from.</param>
        /// <param name="bodyemit">Action used to emit the body of the enumeration.
        /// Gets source element index and delegate that emits the LOAD of source element.</param>
        public void EmitEnumerateArray(IPlace arrplace, int startindex, Action<LocalDefinition, Func<TypeSymbol>> bodyemit)
        {
            var arr_element = ((ArrayTypeSymbol)arrplace.TypeOpt).ElementType;

            // Template: for (i = 0; i < params.Length; i++) <phparr>.Add(arrplace[i])

            var lbl_block = new object();
            var lbl_cond = new object();

            // i = <startindex>
            var tmpi = GetTemporaryLocal(CoreTypes.Int32);
            _il.EmitIntConstant(startindex);
            _il.EmitLocalStore(tmpi);
            _il.EmitBranch(ILOpCode.Br, lbl_cond);

            // {body}
            _il.MarkLabel(lbl_block);

            bodyemit(tmpi, () =>
                {
                    // LOAD arrplace[i]
                    arrplace.EmitLoad(_il);
                    _il.EmitLocalLoad(tmpi);
                    _il.EmitOpCode(ILOpCode.Ldelem);
                    EmitSymbolToken(arr_element, null);
                    return arr_element;
                });

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

        /// <summary>
        /// Emits <c>this</c> instance for a method call.
        /// </summary>
        NamedTypeSymbol LoadMethodThisArgument(MethodSymbol method, BoundExpression thisExpr)
        {
            var containingType = method.ContainingType;

            if (thisExpr != null)
            {
                if (method.HasThis)
                {
                    // <thisExpr> -> <TObject>
                    EmitConvert(thisExpr, containingType);

                    if (containingType.IsValueType)
                    {
                        EmitStructAddr(containingType);   // value -> valueaddr
                    }

                    //
                    return containingType;
                }
                else
                {
                    // POP <thisExpr>
                    EmitPop(Emit(thisExpr));

                    // We need to remember the type for late static binding, e.g.: $instance->staticMethodUsingLSB()
                    // TODO: Resolve from $instance dynamically (it may be its subclass)
                    return (method as SourceRoutineSymbol)?.RequiresLateStaticBoundParam == true ? containingType : null;
                }
            }
            else
            {
                if (method.HasThis)
                {
                    if (ThisPlaceOpt != null && ThisPlaceOpt.TypeOpt != null &&
                        ThisPlaceOpt.TypeOpt.IsEqualToOrDerivedFrom(method.ContainingType))
                    {
                        // implicit $this instance
                        return EmitThis();
                    }
                    else
                    {
                        // $this is undefined
                        // PHP would throw a notice when undefined $this is used

                        // create dummy instance
                        // TODO: when $this is accessed from PHP code, throw error
                        // NOTE: we can't just pass NULL since the instance holds reference to Context that is needed by API internally

                        var dummyctor =
                            (MethodSymbol)(containingType as IPhpTypeSymbol)?.InstanceConstructorFieldsOnly ??    // .ctor that only initializes fields with default values
                            containingType.InstanceConstructors.Where(m => m.Parameters.All(p => p.IsImplicitlyDeclared)).FirstOrDefault();   // implicit ctor

                        if (containingType.IsReferenceType && dummyctor != null)
                        {
                            // new T(Context)
                            EmitCall(ILOpCode.Newobj, dummyctor, null, ImmutableArray<BoundArgument>.Empty, null)
                                .Expect(containingType);
                        }
                        else
                        {
                            // TODO: empty struct addr
                            throw new NotImplementedException();
                        }

                        //
                        return containingType;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        TypeSymbol LoadMethodSpecialArgument(ParameterSymbol p, TypeSymbol thisType, BoundTypeRef staticType)
        {
            // Context
            if (SpecialParameterSymbol.IsContextParameter(p))
            {
                Debug.Assert(p.Type == CoreTypes.Context);
                return EmitLoadContext();
            }
            // <locals>
            else if (SpecialParameterSymbol.IsLocalsParameter(p))
            {
                Debug.Assert(p.Type == CoreTypes.PhpArray);
                if (!this.HasUnoptimizedLocals)
                {
                    throw new InvalidOperationException();
                }

                return LocalsPlaceOpt.EmitLoad(Builder)
                    .Expect(CoreTypes.PhpArray);
            }
            // arguments
            else if (SpecialParameterSymbol.IsCallerArgsParameter(p))
            {
                // ((NamedTypeSymbol)p.Type).TypeParameters // TODO: IList<T>
                return Emit_ArgsArray(CoreTypes.PhpValue); // TODO: T
            }
            // class context
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
                        return this.EmitLoadToken(this.CallerType, null);
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(p.Type);
                    }
                }
                return p.Type;
            }
            // late static
            else if (SpecialParameterSymbol.IsLateStaticParameter(p))
            {
                // PhpTypeInfo
                if (staticType != null)
                {
                    // LOAD <statictype>
                    return staticType.EmitLoadTypeInfo(this);
                }
                else if (thisType != null)
                {
                    // LOAD PhpTypeInfo<thisType>
                    return BoundTypeRef.EmitLoadPhpTypeInfo(this, thisType);
                }
                else
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
            // unhandled
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Emits known method call if arguments have to be unpacked before the call.
        /// </summary>
        internal TypeSymbol EmitCall_UnpackingArgs(ILOpCode code, MethodSymbol method, BoundExpression thisExpr, ImmutableArray<BoundArgument> packedarguments, BoundTypeRef staticType = null)
        {
            Contract.ThrowIfNull(method);
            Debug.Assert(packedarguments.Any(a => a.IsUnpacking));

            // {this}
            var thisType = (code != ILOpCode.Newobj) ? LoadMethodThisArgument(method, thisExpr) : null;

            // .callvirt -> .call
            if (code == ILOpCode.Callvirt && (!method.HasThis || !method.IsMetadataVirtual()))
            {
                // ignores null check in method call
                code = ILOpCode.Call;
            }

            // arguments
            var parameters = method.Parameters;
            int arg_index = 0;      // next argument to be emitted from <arguments>
            var param_index = 0;    // loaded parameters
            var writebacks = new List<WriteBackInfo>();

            // unpack arguments (after $this was evaluated)
            // Template: PhpValue[] <tmpargs> = UNPACK <arguments>
            var tmpargs = GetTemporaryLocal(UnpackArgumentsIntoArray(packedarguments, method.GetByRefArguments()));
            _il.EmitLocalStore(tmpargs);
            var tmpargs_place = new LocalPlace(tmpargs);

            //
            for (; param_index < parameters.Length; param_index++)
            {
                var p = parameters[param_index];

                // special implicit parameters
                if (arg_index == 0 &&           // no source parameter were loaded yet
                    p.IsImplicitlyDeclared &&   // implicitly declared parameter
                    !p.IsParams)
                {
                    LoadMethodSpecialArgument(p, thisType, staticType);
                    continue;
                }

                // pass argument:
                if (p.IsParams)
                {
                    Debug.Assert(p.Type.IsArray());

                    if (((ArrayTypeSymbol)p.Type).ElementType == CoreTypes.PhpValue && arg_index == 0)
                    {
                        // just pass argsarray as it is
                        tmpargs_place.EmitLoad(_il);
                    }
                    else
                    {
                        // create new array and copy&convert values from argsarray

                        ArrayToNewArray(tmpargs_place, arg_index, ((ArrayTypeSymbol)p.Type).ElementType);
                    }

                    break;  // p is last one
                }
                else
                {
                    // Template: (index < tmpargs.Length) ? tmpargs[index] : default
                    var lbldefault = new object();
                    var lblend = new object();

                    _il.EmitIntConstant(arg_index);                 // LOAD index
                    _il.EmitLocalLoad(tmpargs); EmitArrayLength();  // LOAD <tmpargs>.Length
                    _il.EmitBranch(ILOpCode.Bge, lbldefault);       // .bge (lbldefault)
                    EmitLoadArgument(p, tmpargs_place, arg_index,
                        writebacks);                                // LOAD tmpargs[index]
                    _il.EmitBranch(ILOpCode.Br, lblend);            // goto lblend;
                    _il.MarkLabel(lbldefault);                      // lbldefault:
                    EmitParameterDefaultValue(p);                   // default(p)
                    _il.MarkLabel(lblend);                          // lblend:

                    //
                    arg_index++;
                }
            }

            // return <tmpargs> asap
            ReturnTemporaryLocal(tmpargs);
            tmpargs = null;

            // call the method
            var result = EmitCall(code, method);

            // write ref parameters back if necessary
            WriteBackInfo.WriteBackAndFree(this, writebacks);

            //
            return result;
        }

        internal TypeSymbol EmitCall(ILOpCode code, MethodSymbol method, BoundExpression thisExpr, ImmutableArray<BoundArgument> arguments, BoundTypeRef staticType = null)
        {
            Contract.ThrowIfNull(method);

            Debug.Assert(arguments.All(a => !a.IsUnpacking), "Unpacking does not allow us to call the method directly.");

            // {this}
            var thisType = (code != ILOpCode.Newobj) ? LoadMethodThisArgument(method, thisExpr) : null;

            // .callvirt -> .call
            if (code == ILOpCode.Callvirt && (!method.HasThis || !method.IsMetadataVirtual()))
            {
                // ignores null check in method call
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
                if (arg_index == 0 &&           // no source parameter were loaded yet
                    p.IsImplicitlyDeclared &&   // implicitly declared parameter
                    !p.IsParams)
                {
                    LoadMethodSpecialArgument(p, thisType, staticType);
                    continue;
                }

                // load arguments
                if (p.IsParams)
                {
                    Debug.Assert(p.Type.IsArray());

                    // wrap remaining arguments to array
                    var values = (arg_index < arguments.Length) ? arguments.Skip(arg_index).AsImmutable() : ImmutableArray<BoundArgument>.Empty;
                    arg_index += values.Length;
                    Emit_NewArray(((ArrayTypeSymbol)p.Type).ElementType, values);
                    break;  // p is last one
                }

                if (arg_index < arguments.Length)
                {
                    EmitLoadArgument(p, arguments[arg_index++].Value, writebacks);
                }
                else
                {
                    EmitParameterDefaultValue(p);
                }
            }

            // emit remaining not used arguments
            for (; arg_index < arguments.Length; arg_index++)
            {
                EmitPop(Emit(arguments[arg_index].Value));
            }

            // call the method
            var result = EmitCall(code, method);

            // write ref parameters back if necessary
            WriteBackInfo.WriteBackAndFree(this, writebacks);

            //
            return result;
        }

        /// <summary>
        /// Emits .call to <paramref name="target"/> with the same arguments the caller method parameters (<paramref name="thismethod"/>) including reference to <c>this</c>.
        /// </summary>
        /// <param name="target">Method to be called.</param>
        /// <param name="thismethod">Current method.</param>
        /// <returns>Return of <paramref name="target"/>.</returns>
        internal TypeSymbol EmitForwardCall(MethodSymbol target, MethodSymbol thismethod)
        {
            if (target == null)
            {
                return CoreTypes.Void;
            }
            // bind "this" expression if needed
            BoundExpression thisExpr;

            if (target.HasThis)
            {
                thisExpr = new BoundVariableRef(new BoundVariableName(VariableName.ThisVariableName))
                {
                    Variable = BoundLocal.CreateFromPlace(this.ThisPlaceOpt),
                    Access = BoundAccess.Read
                };
            }
            else
            {
                thisExpr = null;
            }

            // bind arguments
            var givenps = thismethod.Parameters;
            var arguments = new List<BoundArgument>(givenps.Length);
            for (int i = 0; i < givenps.Length; i++)
            {
                var p = givenps[i];
                if (p.IsImplicitlyDeclared && !p.IsParams) continue;

                var expr = new BoundVariableRef(new BoundVariableName(new VariableName(p.MetadataName)))
                {
                    Variable = new BoundParameter(p, null),
                    Access = BoundAccess.Read
                };

                var arg = p.IsParams
                    ? BoundArgument.CreateUnpacking(expr)
                    : BoundArgument.Create(expr);

                arguments.Add(arg);
            }


            // emit call of target
            return arguments.Any(arg => arg.IsUnpacking)
                ? EmitCall_UnpackingArgs(ILOpCode.Call, target, thisExpr, arguments.AsImmutableOrEmpty(), null)
                : EmitCall(ILOpCode.Call, target, thisExpr, arguments.AsImmutableOrEmpty(), null);
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
                if (stack == CoreTypes.PhpAlias)
                {
                    // dereference & deep copy
                    // Template: <PhpAlias>.Value.DeepCopy()
                    Emit_PhpAlias_GetValueAddr();
                    stack = EmitCall(ILOpCode.Call, CoreMethods.PhpValue.DeepCopy);
                }
                else
                {
                    // deep copy
                    // note: functions return refs only if their return type is PhpAlias, see above
                    stack = EmitDeepCopy(stack);
                }
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
        /// Emits <c>??</c> operation against the value on top of the evaluation stack.
        /// </summary>
        /// <param name="nullemitter">Routine that emits the FALSE branch of the operator.</param>
        internal void EmitNullCoalescing(Action<CodeGenerator> nullemitter)
        {
            Debug.Assert(nullemitter != null);

            var lbl_notnull = new NamedLabel("NotNull");
            _il.EmitOpCode(ILOpCode.Dup);
            _il.EmitBranch(ILOpCode.Brtrue, lbl_notnull);

            _il.EmitOpCode(ILOpCode.Pop);
            nullemitter(this);

            _il.MarkLabel(lbl_notnull);
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
        class WriteBackInfo
        {
            class WriteBackInfo_ArgsArray : WriteBackInfo
            {
                public IPlace arrplace;
                public int argindex;

                TypeSymbol arr_element => ((ArrayTypeSymbol)arrplace.TypeOpt).ElementType;

                protected override void EmitLoadTarget(CodeGenerator cg, TypeSymbol type)
                {
                    // Template: (type)arr[argindex] 

                    Debug.Assert(arr_element == cg.CoreTypes.PhpValue);

                    arrplace.EmitLoad(cg.Builder);
                    cg.Builder.EmitIntConstant(argindex);
                    cg.Builder.EmitOpCode(ILOpCode.Ldelem);
                    cg.EmitSymbolToken(arr_element, null);
                    cg.EmitConvert(arr_element, 0, type);
                }

                protected override void WriteBackAndFree(CodeGenerator cg)
                {
                    // Template: Operators.SetValue(ref arr[argindex], <TmpLocal>)

                    var arr_element = ((ArrayTypeSymbol)arrplace.TypeOpt).ElementType;
                    Debug.Assert(arr_element == cg.CoreTypes.PhpValue);

                    // ref arr[argindex] : &PhpValue
                    arrplace.EmitLoad(cg.Builder);
                    cg.Builder.EmitIntConstant(argindex);
                    cg.Builder.EmitOpCode(ILOpCode.Ldelema);
                    cg.EmitSymbolToken(arr_element, null);

                    // (PhpValue)TmpLocal : PhpValue
                    cg.Builder.EmitLocalLoad(TmpLocal);
                    cg.EmitConvert((TypeSymbol)TmpLocal.Type, 0, arr_element);

                    // CALL SetValue
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetValue_PhpValueRef_PhpValue);

                    //
                    Dispose(cg);
                }
            }

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

            public static WriteBackInfo CreateAndLoad(CodeGenerator cg, ParameterSymbol targetp, IPlace arrplace, int argindex)
            {
                var writeback = new WriteBackInfo_ArgsArray()
                {
                    TmpLocal = cg.GetTemporaryLocal(targetp.Type),
                    arrplace = arrplace,
                    argindex = argindex,
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
                    EmitLoadTarget(cg, (TypeSymbol)TmpLocal.Type);
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

            protected virtual void EmitLoadTarget(CodeGenerator cg, TypeSymbol type)
            {
                cg.EmitConvert(Target, type);
            }

            /// <summary>
            /// Writes the value back to <see cref="Target"/> and free resources.
            /// </summary>
            protected virtual void WriteBackAndFree(CodeGenerator cg)
            {
                // Template: <Target> = <TmpLocal>;
                var place = Target.BindPlace(cg);
                place.EmitStorePrepare(cg, null);
                cg.Builder.EmitLocalLoad(TmpLocal);
                place.EmitStore(cg, (TypeSymbol)TmpLocal.Type);

                //
                Dispose(cg);
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

            protected void Dispose(CodeGenerator cg)
            {
                // free <TmpLocal>
                cg.ReturnTemporaryLocal(TmpLocal);
                TmpLocal = null;
            }
        }

        /// <summary>
        /// Loads argument from bound expression.
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
        /// Loads argument from arguments array.
        /// </summary>
        void EmitLoadArgument(ParameterSymbol targetp, IPlace arrplace, int argindex, List<WriteBackInfo> writebacks)
        {
            // assert arrplace is of type PhpValue[]
            Debug.Assert(arrplace != null);
            Debug.Assert(arrplace.TypeOpt.IsSZArray());

            var arr_element = ((ArrayTypeSymbol)arrplace.TypeOpt).ElementType;
            Debug.Assert(arr_element == CoreTypes.PhpValue);

            //
            if (targetp.RefKind == RefKind.None)
            {
                // Template: (T)arrplace[argindex]
                arrplace.EmitLoad(_il);
                _il.EmitIntConstant(argindex);
                _il.EmitOpCode(ILOpCode.Ldelem);
                EmitSymbolToken(arr_element, null);
                EmitConvert(CoreTypes.PhpValue, 0, targetp.Type);
            }
            else
            {
                // write-back
                writebacks.Add(WriteBackInfo.CreateAndLoad(this, targetp, arrplace, argindex));
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
                Emit_EmptyArray(((ArrayTypeSymbol)targetp.Type).ElementType);
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

        /// <summary>
        /// Emits <c>PhpString.Append</c> expecting <c>PhpString</c> and <paramref name="ytype"/> on top of evaluation stack.
        /// </summary>
        /// <param name="ytype">Type of argument loaded on stack.</param>
        internal void Emit_PhpString_Append(TypeSymbol ytype)
        {
            if (ytype == CoreTypes.PhpAlias)
            {
                ytype = Emit_PhpAlias_GetValue();
            }

            if (ytype == CoreTypes.PhpString)
            {
                // Append(PhpString)
                EmitCall(ILOpCode.Callvirt, CoreMethods.PhpString.Append_PhpString);
            }
            else if (ytype == CoreTypes.PhpValue)
            {
                // Append(PhpValue, Context)
                EmitLoadContext();
                EmitCall(ILOpCode.Callvirt, CoreMethods.PhpString.Append_PhpValue_Context);
            }
            else
            {
                // Append(string)
                EmitConvertToString(ytype, 0);
                EmitCall(ILOpCode.Callvirt, CoreMethods.PhpString.Append_String);
            }
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
                    method = CoreMethods.Operators.Echo_Bool.Symbol;
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
                if (constant.Value == null)
                {
                    EmitIntStringKey(string.Empty);
                }
                else if (constant.Value is string)
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
        /// Emits type declaration into the context.
        /// </summary>
        public void EmitDeclareType(SourceTypeSymbol t)
        {
            Contract.ThrowIfNull(t);
            Debug.Assert(!t.IsErrorType(), "Cannot declare an error type.");

            // 
            this.EmitSequencePoint(t.Syntax.HeadingSpan);

            // autoload base types or throw an error
            if (t.HasVersions)
            {
                // emit declaration of type that has ambiguous versions
                EmitVersionedTypeDeclaration(t.AllVersions());
            }
            else
            {
                var dependent = t.GetDependentSourceTypeSymbols();

                // ensure all types are loaded into context,
                // autoloads if necessary
                dependent.ForEach(EmitExpectTypeDeclared);

                // <ctx>.DeclareType<T>()
                EmitLoadContext();
                EmitCall(ILOpCode.Call, CoreMethods.Context.DeclareType_T.Symbol.Construct(t));
            }

            //
            Debug.Assert(_il.IsStackEmpty);
        }

        /// <summary>
        /// If necessary, emits autoload and check the given type is loaded into context.
        /// </summary>
        void EmitExpectTypeDeclared(NamedTypeSymbol d)
        {
            if (this.Routine != null && ReferenceEquals((d as SourceTypeSymbol)?.ContainingFile, this.Routine.ContainingFile) && !d.IsConditional)
            {
                // declared in same file unconditionally,
                // we don't have to check anything
                return;
            }

            // Template: ctx.ExpectTypeDeclared<d>
            EmitLoadContext();
            EmitCall(ILOpCode.Call, CoreMethods.Context.ExpectTypeDeclared_T.Symbol.Construct(d));
        }

        /// <summary>
        /// Emit declaration of one of given versions (of the same source type) based on actually declared types that versions depend on.
        /// </summary>
        /// <param name="versions">Array of multiple versions of a source type declaration.</param>
        void EmitVersionedTypeDeclaration(ImmutableArray<SourceTypeSymbol> versions)
        {
            Debug.Assert(versions.Length > 1);

            // ensure all types are loaded into context and resolve version to declare

            // collect dependent types [name x symbols]
            var dependent = new Dictionary<QualifiedName, HashSet<NamedTypeSymbol>>();
            foreach (var v in versions)
            {
                var deps = v.GetDependentSourceTypeSymbols();
                foreach (var d in deps.OfType<IPhpTypeSymbol>())
                {
                    if (dependent.ContainsKey(d.FullName))
                    {
                        dependent[d.FullName].Add((NamedTypeSymbol)d);
                    }
                    else
                    {
                        dependent[d.FullName] = new HashSet<NamedTypeSymbol>() { (NamedTypeSymbol)d };
                    }
                }
            }

            //
            var dependent_handles = new Dictionary<QualifiedName, LocalDefinition>();

            // resolve dependent types:
            foreach (var d in dependent)
            {
                var first = d.Value.First();

                if (d.Value.Count == 1)
                {
                    EmitExpectTypeDeclared(first);
                }
                else
                {
                    var tname = ((IPhpTypeSymbol)first).FullName;

                    // Template: tmp_d = ctx.GetDeclaredTypeOrThrow(d_name, autoload: true).TypeHandle
                    EmitLoadContext();
                    _il.EmitStringConstant(tname.ToString());
                    _il.EmitBoolConstant(true);
                    EmitCall(ILOpCode.Call, CoreMethods.Context.GetDeclaredTypeOrThrow_string_bool);
                    var thandle = EmitCall(ILOpCode.Call, CoreMethods.Operators.GetTypeHandle_PhpTypeInfo.Getter);

                    var tmp_handle = GetTemporaryLocal(thandle);
                    _il.EmitLocalStore(tmp_handle);

                    //
                    dependent_handles.Add(tname, tmp_handle);
                }
            }

            Debug.Assert(dependent_handles.Count != 0);

            // At this point, all dependant types are loaded, otherwise runtime would throw an exception

            // find out version to declare:
            var lblFail = new NamedLabel("declare_fail");
            var lblDone = new NamedLabel("declare_done");
            EmitDeclareTypeByDependencies(versions, dependent_handles.ToArray(), 0, dependent, lblDone, lblFail);

            // Template: throw new Exception("Cannot declare {T}");
            // TODO: compile type dynamically (eval of type declaration with actual base types)
            _il.MarkLabel(lblFail);
            EmitThrowException(string.Format(ErrorStrings.ERR_UnknownTypeDependencies, versions[0].FullName));

            _il.MarkLabel(lblDone);

            // return tmp variables
            dependent_handles.Values.ForEach(ReturnTemporaryLocal);
        }

        /// <summary>
        /// Emits decision tree.
        /// </summary>
        /// <param name="versions">Versions to decide of.</param>
        /// <param name="dependency_handle">Map of dependant types and associated local variable holding resolved real type handle.</param>
        /// <param name="index">Index to <paramref name="dependency_handle"/> where to decide from.</param>
        /// <param name="dependencies">Map of type names and possible real types.</param>
        /// <param name="lblDone">Label where to jump upon decision is done.</param>
        /// <param name="lblFail">Label where to jump when dependy does not match.</param>
        void EmitDeclareTypeByDependencies(
            ImmutableArray<SourceTypeSymbol> versions,
            KeyValuePair<QualifiedName, LocalDefinition>[] dependency_handle, int index,
            Dictionary<QualifiedName, HashSet<NamedTypeSymbol>> dependencies,
            NamedLabel lblDone, NamedLabel lblFail)
        {
            if (index == dependency_handle.Length || versions.Length == 1)
            {
                Debug.Assert(versions.Length == 1);

                // <ctx>.DeclareType<T>();
                // goto DONE;
                EmitLoadContext();
                EmitCall(ILOpCode.Call, CoreMethods.Context.DeclareType_T.Symbol.Construct(versions[0]));
                _il.EmitBranch(ILOpCode.Br, lblDone);
                return;
            }

            var thandle = dependency_handle[index];
            var types = dependencies[thandle.Key];
            Debug.Assert(types.Count > 1);

            /* [A, B]:
             * if (A == A1) {
             *   if (B == B1) Declare(X11); goto Done;
             *   if (B == B2) Declare(X12); goto Done;
             *   goto Fail;
             * }
             * if (A == A2) {
             *   if (B == B1) Declare(X21); goto Done;
             *   if (B == B2) Declare(X22); goto Done;
             *   goto Fail;
             * }
             * Fail: throw;
             * Done:
             */

            object lblElse = null;

            foreach (var h in types)
            {
                if (lblElse != null) _il.MarkLabel(lblElse);

                // Template: if (thandle.Equals(h)) { ... } else goto lblElse;
                _il.EmitLocalAddress(thandle.Value);
                EmitLoadToken(h, null);
                EmitCall(ILOpCode.Call, CoreTypes.RuntimeTypeHandle.Method("Equals", CoreTypes.RuntimeTypeHandle));
                _il.EmitBranch(ILOpCode.Brfalse, lblElse = new object());

                // Template: *recursion*
                // h == thandle: filter versions depending on thandle
                var filtered_versions = versions.Where(v => v.GetDependentSourceTypeSymbols().Contains(h));
                EmitDeclareTypeByDependencies(filtered_versions.AsImmutable(), dependency_handle, index + 1, dependencies, lblDone, lblFail);
            }
            _il.MarkLabel(lblElse);
            _il.EmitBranch(ILOpCode.Br, lblFail);
        }

        /// <summary>
        /// Emits <code>throw new Exception(message)</code>
        /// </summary>
        public void EmitThrowException(string message)
        {
            var exception_ctor = CoreTypes.Exception.Ctor(CoreTypes.String);
            _il.EmitStringConstant(message);
            EmitCall(ILOpCode.Newobj, exception_ctor);
            _il.EmitThrow(false);
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
                    case SpecialParameterSymbol.ThisName:
                        EmitThisOrNull();
                        break;
                    case SpecialParameterSymbol.SelfName:
                        this.EmitCallerRuntimeTypeHandle();
                        break;
                    default:
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
                    var str = (string)value;
                    if (targetOpt != null)
                    {
                        switch (targetOpt.SpecialType)
                        {
                            case SpecialType.System_Char:
                                if (str != null && str.Length == 1)
                                {
                                    Builder.EmitCharConstant(str[0]);
                                    return DeclaringCompilation.GetSpecialType(SpecialType.System_Char);
                                }
                                break;
                        }
                    }

                    Builder.EmitStringConstant(str);
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
                else if (value is char)
                {
                    if (targetOpt != null)
                    {
                        switch (targetOpt.SpecialType)
                        {
                            case SpecialType.System_String:
                                Builder.EmitStringConstant(value.ToString());
                                return CoreTypes.String;
                        }
                    }

                    Builder.EmitCharConstant((char)value);
                    return DeclaringCompilation.GetSpecialType(SpecialType.System_Char);
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
                case SpecialType.System_Char:
                    _il.EmitCharConstant('\0');
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
                _il.EmitValueDefault(this.Module, this.Diagnostics, this.GetTemporaryLocal(valuetype, true));
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

        public static void EmitValueDefault(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, LocalDefinition tmp)
        {
            Debug.Assert(tmp.Type.IsValueType);
            il.EmitLocalAddress(tmp);
            il.EmitOpCode(ILOpCode.Initobj);
            il.EmitSymbolToken(module, diagnostics, (TypeSymbol)tmp.Type, null);
            // ldloc <loc>
            il.EmitLocalLoad(tmp);
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

        public static void EmitCharConstant(this ILBuilder il, char value)
        {
            il.EmitIntConstant(unchecked((int)value));
        }

        public static TypeSymbol EmitLoad(this ParameterSymbol p, ILBuilder il)
        {
            Debug.Assert(p != null, nameof(p));
            return new ParamPlace(p).EmitLoad(il);
        }
    }
}
