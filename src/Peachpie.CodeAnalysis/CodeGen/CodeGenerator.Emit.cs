﻿using Devsense.PHP.Syntax;
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
using Peachpie.CodeAnalysis.Errors;
using Peachpie.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;
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
        public TypeSymbol EmitCallerTypeHandle()
        {
            var caller = this.CallerType;
            if (caller != null)
            {
                // RuntimeTypeHandle
                EmitLoadToken(caller, null);
            }
            else
            {
                var place = RuntimeCallerTypePlace;
                if (place != null)
                {
                    place.EmitLoad(_il).Expect(CoreTypes.RuntimeTypeHandle);
                }
                else
                {
                    // default(RuntimeTypeHandle)
                    EmitLoadDefaultOfValueType(this.CoreTypes.RuntimeTypeHandle);
                }
            }

            //
            return CoreTypes.RuntimeTypeHandle;
        }

        /// <summary>
        /// In case current routine has a caller context provided in runtime,
        /// gets its <see cref="IPlace"/>.
        /// </summary>
        public IPlace RuntimeCallerTypePlace
        {
            get
            {
                if (_callerTypePlace == null)
                {
                    if (GeneratorStateMachineMethod != null)
                    {
                        if (this.Routine is SourceLambdaSymbol)
                        {
                            // Operator.GetGeneratorDynamicScope(g)
                            _callerTypePlace = new OperatorPlace(
                                CoreTypes.Operators.Method("GetGeneratorDynamicScope", CoreTypes.Generator),
                                new ParamPlace(GeneratorStateMachineMethod.GeneratorParameter));
                        }
                        // otherwise the caller type is resolve statically already
                    }
                    else if (this.Routine is SourceGlobalMethodSymbol global)
                    {
                        _callerTypePlace = new ParamPlace(global.SelfParameter);
                    }
                    else if (this.Routine is SourceLambdaSymbol lambda)
                    {
                        _callerTypePlace = lambda.GetCallerTypePlace();
                    }
                }

                return _callerTypePlace;
            }
            set
            {
                _callerTypePlace = value;
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
        /// Emits value of <c>$this</c>.
        /// Available only within source routines.
        /// In case no $this is available, nothing is emitted and function returns <c>null</c> reference.
        /// </summary>
        TypeSymbol EmitPhpThis()
        {
            if (GeneratorStateMachineMethod != null)
            {
                return GeneratorStateMachineMethod.ThisParameter.EmitLoad(_il);
            }

            if (Routine != null)
            {
                if (Routine.IsGeneratorMethod())
                {
                    // but GeneratorStateMachineMethod == null; We're not emitting SM yet
                    Debug.Fail("$this not resolved");
                }

                //
                var thisplace = Routine.GetPhpThisVariablePlace(this.Module);
                if (thisplace != null)
                {
                    return thisplace.EmitLoad(_il);
                }
            }

            //
            return null;
        }

        /// <summary>
        /// Emits value of <c>$this</c>.
        /// Available only within source routines.
        /// In case no $this is available, <c>NULL</c> is loaded on stack instead.
        /// </summary>
        public TypeSymbol EmitPhpThisOrNull()
        {
            var t = EmitPhpThis();
            if (t == null)
            {
                _il.EmitNullConstant();
                t = CoreTypes.Object;
            }

            return t;
        }

        public TypeSymbol EmitGeneratorInstance()
        {
            Contract.ThrowIfNull(this.GeneratorStateMachineMethod);
            // .ldarg <generator>
            return this.GeneratorStateMachineMethod.GeneratorParameter.EmitLoad(_il);
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
                //if (tmask.IsSingleType && TypeRefContext.IsNull(tmask))
                //{
                //    // NULL
                //    _il.EmitNullConstant();
                //    return;
                //}

                if (place.HasAddress)
                {
                    if (place.Type == CoreTypes.PhpNumber)
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
                    else if (place.Type == CoreTypes.PhpValue)
                    {
                        // access directly without type checking
                        if (IsDoubleOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Double.Getter)
                                .Expect(SpecialType.System_Double);
                        }
                        else if (IsLongOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Long.Getter)
                                .Expect(SpecialType.System_Int64);
                        }
                        else if (IsBooleanOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Boolean.Getter)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else if (IsReadonlyStringOnly(tmask))
                        {
                            place.EmitLoadAddress(_il);
                            return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.String.Getter)
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

                            if (tmask.IsSingleType)
                            {
                                var tref = this.TypeRefContext.GetTypes(tmask).FirstOrDefault();
                                var clrtype = (TypeSymbol)tref.ResolveTypeSymbol(DeclaringCompilation);
                                if (clrtype.IsValidType() && !clrtype.IsObjectType())
                                {
                                    this.EmitCastClass(clrtype);
                                    return clrtype;
                                }
                            }

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
                    return expr.ResultType = CoreTypes.Void;
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
            Debug.Assert(!stack.IsUnreachable);

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
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Double.Getter)
                            .Expect(SpecialType.System_Double);
                    }
                    else if (IsLongOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Long.Getter)
                            .Expect(SpecialType.System_Int64);
                    }
                    else if (IsBooleanOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Boolean.Getter)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (IsReadonlyStringOnly(tmask))
                    {
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.String.Getter)
                            .Expect(SpecialType.System_String);
                    }
                }
                else if (stack.Is_PhpArray() || stack.IsStringType())
                {
                    // already specialied reference types
                }
                else if (stack.IsReferenceType && !stack.IsSealed && this.Routine != null)
                {
                    var tref = this.TypeRefContext.GetTypes(tmask).FirstOrDefault();
                    if (tref.IsObject)
                    {
                        // naive IL beutifier,
                        // that casts a reference type to its actual type that we determined in type analysis

                        var t = (TypeSymbol)tref.ResolveTypeSymbol(DeclaringCompilation);
                        if (t == stack)
                        {
                            return stack;
                        }

                        if (t.IsValidType())
                        {
                            if (stack.IsTypeParameter() || t.IsOfType(stack))
                            {
                                EmitCastClass(t);
                                return t;
                            }
                        }
                        else
                        {
                            // TODO: class aliasing
                            Debug.WriteLine($"'{tref}' is {(t is AmbiguousErrorTypeSymbol ? "ambiguous" : "unknown")}!");
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

            var t = EmitSpecialize(expr);
            if (t == null)
            {
                throw ExceptionUtilities.UnexpectedValue(null);
            }

            return t;
        }

        /// <summary>
        /// Loads <see cref="RuntimeTypeHandle"/> of given type.
        /// </summary>
        public TypeSymbol EmitLoadToken(TypeSymbol type, SyntaxNode syntaxNodeOpt)
        {
            if (type.IsValidType())
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
            Debug.Assert(!t.IsUnreachable);
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

            // Nullable<T>
            if (t.IsNullableType(out var ttype))
            {
                Debug.Assert(t.IsValueType);
                // Template: {STACK}.HasValue
                this.EmitStructAddr(t);
                EmitCall(ILOpCode.Call, DeclaringCompilation.System_Nullable_T_HasValue(t));
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

        /// <summary>
        /// Emits <c>typeof(symbol) : System.Type</c>.
        /// </summary>
        internal TypeSymbol EmitSystemType(TypeSymbol symbol)
        {
            // ldtoken !!T
            EmitLoadToken(symbol, null);

            // call class System.Type System.Type::GetTypeFromHandle(valuetype System.RuntimeTypeHandle)
            return EmitCall(ILOpCode.Call, (MethodSymbol)DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Type__GetTypeFromHandle));
        }

        internal void EmitHiddenSequencePoint()
        {
            if (EmitPdbSequencePoints)
            {
                _il.DefineHiddenSequencePoint();
            }
        }

        internal void EmitSequencePoint(LangElement element)
        {
            if (ExpressionsExtension.AllowSequencePoint(element))
            {
                EmitSequencePoint(element.Span);
            }
        }
        internal void EmitSequencePoint(Span span)
        {
            if (EmitPdbSequencePoints && span.IsValid && !span.IsEmpty)
            {
                EmitSequencePoint(span.ToTextSpan());
            }
        }

        internal void EmitSequencePoint(Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            if (EmitPdbSequencePoints && span.Length > 0)
            {
                _il.EmitOpCode(ILOpCode.Nop);
                _il.DefineSequencePoint(ContainingFile.SyntaxTree, span);
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
        /// GetPhpTypeInfo&lt;T&gt;() : PhpTypeInfo
        /// </summary>
        public TypeSymbol EmitLoadPhpTypeInfo(ITypeSymbol t)
        {
            Contract.ThrowIfNull(t);

            // CALL GetPhpTypeInfo<T>()
            return EmitCall(ILOpCode.Call, CoreMethods.Dynamic.GetPhpTypeInfo_T.Symbol.Construct(t));
        }

        /// <summary>
        /// Emits <c>PhpTypeInfo</c> of late static bound type.
        /// </summary>
        /// <returns>
        /// Type symbol of <c>PhpTypeInfo</c>.
        /// </returns>
        public TypeSymbol EmitLoadStaticPhpTypeInfo()
        {
            if (Routine != null)
            {
                if (GeneratorStateMachineMethod != null)
                {
                    this.EmitGeneratorInstance(); // LOAD Generator
                    return this.EmitCall(ILOpCode.Call, CoreMethods.Operators.GetGeneratorLazyStatic_Generator)
                        .Expect(CoreTypes.PhpTypeInfo);
                }

                if (Routine is SourceLambdaSymbol lambda)
                {
                    // Handle lambda since $this can be null (unbound)
                    // Template: CLOSURE.Static();
                    lambda.ClosureParameter.EmitLoad(Builder);
                    return EmitCall(ILOpCode.Call, CoreMethods.Operators.Static_Closure)
                        .Expect(CoreTypes.PhpTypeInfo);
                }

                var thisVariablePlace = Routine.GetPhpThisVariablePlace(Module);
                if (thisVariablePlace != null)
                {
                    // Template: GetPhpTypeInfo(this)
                    thisVariablePlace.EmitLoad(Builder);
                    return EmitCall(ILOpCode.Call, CoreMethods.Dynamic.GetPhpTypeInfo_Object);
                }

                var lateStaticParameter = Routine.LateStaticParameter();
                if (lateStaticParameter != null)
                {
                    // Template: LOAD @static   // ~ @static parameter passed by caller
                    return lateStaticParameter
                        .EmitLoad(Builder)
                        .Expect(CoreTypes.PhpTypeInfo);
                }

                var caller = CallerType;
                if (caller is SourceTypeSymbol srct && srct.IsSealed)
                {
                    // `static` == `self` <=> self is sealed
                    // Template: GetPhpTypeInfo<CallerType>()
                    return EmitLoadPhpTypeInfo(caller);
                }
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Loads <c>PhpTypeInfo</c> of <c>self</c>.
        /// </summary>
        /// <param name="throwOnError">Whether to expect only valid scope.</param>
        /// <returns>Type symbol of PhpTypeInfo.</returns>
        public TypeSymbol EmitLoadSelf(bool throwOnError = false)
        {
            var caller = CallerType;
            if (caller != null)
            {
                // current scope is resolved in compile-time:
                // Template: GetPhpTypeInfo<CallerType>()
                return EmitLoadPhpTypeInfo(caller);
            }
            else
            {
                // Template: Operators.GetSelf( {caller type handle} )
                EmitCallerTypeHandle();
                return EmitCall(ILOpCode.Call, throwOnError
                    ? CoreMethods.Operators.GetSelf_RuntimeTypeHandle
                    : CoreMethods.Operators.GetSelfOrNull_RuntimeTypeHandle);
            }
        }

        /// <summary>
        /// Loads <c>PhpTypeInfo</c> of current scope's <c>parent</c> class;
        /// </summary>
        /// <returns>
        /// Type symbol of <c>PhpTypeInfo</c>.
        /// </returns>
        public TypeSymbol EmitLoadParent()
        {
            var caller = CallerType;
            if (caller != null)
            {
                // current scope is resolved in compile-time:
                // Template: Operators.GetParent( GetPhpTypeInfo<CallerType>() )
                EmitLoadPhpTypeInfo(caller);
                return EmitCall(ILOpCode.Call, CoreMethods.Operators.GetParent_PhpTypeInfo);
            }
            else
            {
                // Template: Operators.GetParent( {caller type handle} )
                EmitCallerTypeHandle();
                return EmitCall(ILOpCode.Call, CoreMethods.Operators.GetParent_RuntimeTypeHandle);
            }
        }

        /// <summary>
        /// Emits load of <c>PhpAlias.Value</c>,
        /// expecting <c>PhpAlias</c> on top of evaluation stack,
        /// pushing <c>PhpValue</c> on top of the stack.
        /// </summary>
        public TypeSymbol Emit_PhpAlias_GetValue()
        {
            // CALL <stack>.get_Value()
            return EmitCall(ILOpCode.Callvirt, CoreMethods.PhpAlias.Value.Getter);
        }

        /// <summary>
        /// Emits store to <c>PhpAlias.Value</c>,
        /// expecting <c>PhpAlias</c> and <c>PhpValue</c> on top of evaluation stack.
        /// </summary>
        public void Emit_PhpAlias_SetValue()
        {
            // <stack_1>.Value = <stack_2>
            EmitCall(ILOpCode.Callvirt, CoreMethods.PhpAlias.Value.Setter);
        }

        /// <summary>
        /// Emits <c>new PhpAlias</c>, expecting <c>PhpValue</c> on top of the evaluation stack.
        /// </summary>
        public TypeSymbol Emit_PhpValue_MakeAlias()
        {
            //// new PhpAlias(<STACK>, 1)
            //_il.EmitIntConstant(1);
            //return EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpAlias_PhpValue_int);

            // PhpAlias.Create( <STACK> )
            return EmitCall(ILOpCode.Call, CoreMethods.PhpAlias.Create_PhpValue);
        }

        /// <summary>
        /// Emits load of PhpValue representing void.
        /// </summary>
        public TypeSymbol Emit_PhpValue_Void() => Emit_PhpValue_Null();

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
        /// Emits load of PhpValue representing true.
        /// </summary>
        public TypeSymbol Emit_PhpValue_True()
        {
            _il.EmitOpCode(ILOpCode.Ldsfld);
            EmitSymbolToken(CoreMethods.PhpValue.True, null);
            return CoreTypes.PhpValue;
        }

        /// <summary>
        /// Emits load of PhpValue representing false.
        /// </summary>
        public TypeSymbol Emit_PhpValue_False()
        {
            _il.EmitOpCode(ILOpCode.Ldsfld);
            EmitSymbolToken(CoreMethods.PhpValue.False, null);
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

        public void Emit_NewArray(TypeSymbol elementType, ImmutableArray<BoundArgument> values) => Emit_NewArray(elementType, values, a => Emit(a.Value));
        public void Emit_NewArray(TypeSymbol elementType, ImmutableArray<BoundExpression> values) => Emit_NewArray(elementType, values, a => Emit(a));

        public TypeSymbol Emit_NewArray(TypeSymbol elementType, int length)
        {
            if (length == 0)
            {
                return Emit_EmptyArray(elementType);
            }
            else if (length < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            else
            {
                _il.EmitIntConstant(length);
                _il.EmitOpCode(ILOpCode.Newarr);
                EmitSymbolToken(elementType, null);

                return ArrayTypeSymbol.CreateSZArray(this.DeclaringCompilation.SourceAssembly, elementType);
            }
        }

        public TypeSymbol Emit_NewArray<T>(TypeSymbol elementType, ImmutableArray<T> values, Func<T, TypeSymbol> emitter)
        {
            if (values.IsDefaultOrEmpty == false)
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
                    EmitConvert(emitter(values[i]), 0, elementType);
                    _il.EmitOpCode(ILOpCode.Stelem);
                    EmitSymbolToken(elementType, null);
                }

                // T[]
                return ArrayTypeSymbol.CreateSZArray(this.DeclaringCompilation.SourceAssembly, elementType);
            }
            else
            {
                // empty array
                return Emit_EmptyArray(elementType);
            }
        }

        /// <summary>Template: <code>Array.Empty&lt;elementType&gt;()</code></summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        internal TypeSymbol Emit_EmptyArray(TypeSymbol elementType)
        {
            // Array.Empty<elementType>()
            var array_empty_T = ((MethodSymbol)this.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Array__Empty))
                .Construct(elementType);

            return EmitCall(ILOpCode.Call, array_empty_T);
        }

        /// <summary>
        /// Emits array of <paramref name="elementType"/> containing all current routine PHP arguments value.
        /// </summary>
        internal TypeSymbol Emit_ArgsArray(TypeSymbol elementType)
        {
            var routine = this.Routine ?? throw this.NotImplementedException(nameof(Routine));

            if (routine.IsGlobalScope)
            {
                // NOTE: this produces warning: Called from the global scope - no function context
                _il.EmitNullConstant();
                return ArrayTypeSymbol.CreateSZArray(DeclaringCompilation.SourceAssembly, CoreTypes.PhpValue);
            }

            if (routine.IsGeneratorMethod())
            {
                Debug.Assert(LocalsPlaceOpt != null);
                Debug.Assert(LocalsPlaceOpt.Type == CoreTypes.PhpArray);

                // TODO: this is not correct for varargs
                // <locals> does not contain all the parameters,
                // also it contains all the default values which should not be in listed here
                Debug.Fail("varargs in Generator has an incorrect behavior");

                // args = <locals>.GetValues();
                LocalsPlaceOpt.EmitLoad(_il);
                return EmitCall(ILOpCode.Call, CoreTypes.PhpHashtable.Method("GetValues"));
            }

            TypeSymbol arrtype;

            // [implicit] [use parameters, source parameters] [... varargs]

            var variadic = routine.GetParamsParameter();  // optional params
            var variadic_element = (variadic?.Type as ArrayTypeSymbol)?.ElementType;
            var variadic_place = variadic != null ? new ParamPlace(variadic) : null;

            // get used source parameters:
            var ps = routine.SourceParameters
                .Skip(routine is SourceLambdaSymbol lambda ? lambda.UseParams.Count : 0)    // without lambda use parameters
                .TakeWhile(x => variadic != null ? x.Ordinal < variadic.Ordinal : true)     // up to the params parameter (handled separately)
                .ToArray();

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
                    EmitConvert(ps[i].EmitLoad(_il), 0, elementType);
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
        public TypeSymbol ArrayToPhpArray(IPlace arrplace, bool deepcopy = false, int startindex = 0)
        {
            var phparr = GetTemporaryLocal(CoreTypes.PhpArray);

            // Template: tmparr = new PhpArray(arrplace.Length)
            arrplace.EmitLoad(_il);
            EmitArrayLength();  // array size hint
            EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpArray_int);
            _il.EmitLocalStore(phparr);

            // enumeration body:
            EmitEnumerateArray(arrplace, startindex, (srcindex, element_loader) =>
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
            if (arrplace == null) throw new ArgumentNullException(nameof(arrplace));

            Debug.Assert(arrplace.Type.IsSZArray());

            var arr_element = ((ArrayTypeSymbol)arrplace.Type).ElementType;

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
        /// Emits <paramref name="thisExpr"/> to be used as target instance of method call, field or property.
        /// </summary>
        internal TypeSymbol LoadTargetInstance(BoundExpression thisExpr, MethodSymbol method)
        {
            NamedTypeSymbol targetType = method.HasThis ? method.ContainingType : CoreTypes.Void;

            if (thisExpr != null)
            {
                if (targetType.SpecialType != SpecialType.System_Void)
                {
                    //var lhs = default(LhsStack);
                    //lhs = VariableReferenceExtensions.EmitReceiver(this, ref lhs, method, thisExpr);

                    var receiverPlace = PlaceOrNull(thisExpr);
                    if (receiverPlace != null && targetType.IsValueType)
                    {
                        // load addr of the receiver:
                        var lhs = VariableReferenceExtensions.EmitReceiver(this, receiverPlace);

                        if (lhs.Stack == null || lhs.Stack.IsVoid())
                        {
                            throw this.NotImplementedException();
                        }
                    }
                    else
                    {
                        // <thisExpr> -> <TObject>
                        EmitConvert(thisExpr, targetType);

                        if (targetType.IsValueType)
                        {
                            EmitStructAddr(targetType);   // value -> valueaddr
                        }
                    }

                    //
                    return targetType;
                }
                else
                {
                    // POP <thisExpr>
                    EmitPop(Emit(thisExpr));

                    return null;
                }
            }
            else
            {
                if (targetType.SpecialType != SpecialType.System_Void)
                {
                    if (ThisPlaceOpt != null && ThisPlaceOpt.Type != null &&
                        ThisPlaceOpt.Type.IsOfType(targetType))
                    {
                        // implicit $this instance
                        return EmitThis();
                    }
                    else
                    {
                        // $this is undefined
                        // PHP would throw a notice when undefined $this is used

                        if (targetType.IsValueType)
                        {
                            // Template: ADDR default(VALUE_TYPE)
                            Builder.EmitValueDefaultAddr(this.Module, this.Diagnostics, this.GetTemporaryLocal(targetType, true));
                        }
                        else
                        {
                            // create dummy instance
                            // TODO: when $this is accessed from PHP code, throw error
                            // NOTE: we can't just pass NULL since the instance holds reference to Context that is needed by emitted code

                            var dummyctor =
                                (MethodSymbol)(targetType as IPhpTypeSymbol)?.InstanceConstructorFieldsOnly ??    // .ctor that only initializes fields with default values
                                targetType.InstanceConstructors.Where(m => !m.IsPhpHidden && m.Parameters.All(p => p.IsImplicitlyDeclared)).FirstOrDefault();   // implicit ctor

                            if (dummyctor != null)
                            {
                                // new T(Context)
                                EmitCall(ILOpCode.Newobj, dummyctor, null, ImmutableArray<BoundArgument>.Empty, null)
                                    .Expect(targetType);
                            }
                            else
                            {
                                // TODO: empty struct addr
                                throw this.NotImplementedException();
                            }
                        }

                        //
                        return targetType;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Loads "self" class onto the stack.
        /// </summary>
        /// <param name="astype">Type to be used to represent "self" - PhpTypeInfo, string, RuntimeTypeHandle.</param>
        /// <returns></returns>
        TypeSymbol EmitLoadCurrentClassContext(TypeSymbol astype)
        {
            if (astype == CoreTypes.PhpTypeInfo)
            {
                if (this.CallerType == null && this.RuntimeCallerTypePlace == null)
                {
                    // null
                    _il.EmitNullConstant();
                }
                else
                {
                    EmitLoadSelf(throwOnError: false);
                }
            }
            else if (astype.SpecialType == SpecialType.System_String)
            {
                if (this.CallerType == null && this.RuntimeCallerTypePlace == null)
                {
                    // null
                    Builder.EmitNullConstant();
                }
                else if (this.CallerType is IPhpTypeSymbol phpt)
                {
                    // type known in compile-time:
                    Builder.EmitStringConstant(phpt.FullName.ToString());
                }
                else
                {
                    // {LOAD PhpTypeInfo}?.Name
                    EmitLoadSelf(throwOnError: false);
                    EmitNullCoalescing(
                        () => EmitCall(ILOpCode.Call, CoreMethods.Operators.GetName_PhpTypeInfo.Getter).Expect(SpecialType.System_String),
                        () => Builder.EmitNullConstant());
                }
            }
            else if (astype == CoreTypes.RuntimeTypeHandle)
            {
                // LOAD <RuntimeTypeHandle>
                return EmitCallerTypeHandle();
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(astype);
            }

            return astype;
        }

        TypeSymbol LoadMethodSpecialArgument(ParameterSymbol p, BoundTypeRef staticType, ITypeSymbol selfType)
        {
            // Context
            if (SpecialParameterSymbol.IsContextParameter(p))
            {
                Debug.Assert(p.Type == CoreTypes.Context);
                return EmitLoadContext();
            }
            // ImportValueAttribute( ValueSpec )
            else if (SpecialParameterSymbol.IsImportValueParameter(p, out var value))
            {
                switch (value)
                {
                    case ImportValueAttributeData.ValueSpec.CallerScript:
                        Debug.Assert(ContainingFile != null);
                        Debug.Assert(p.Type == CoreTypes.RuntimeTypeHandle);
                        return EmitLoadToken(ContainingFile, null);    // RuntimeTypeHandle

                    case ImportValueAttributeData.ValueSpec.CallerArgs:
                        //Debug.Assert(p.Type.IsSZArray() && ((ArrayTypeSymbol)p.Type).ElementType.Is_PhpValue()); // PhpValue[]
                        //return Emit_ArgsArray(CoreTypes.PhpValue);     // PhpValue[]
                        if ((Symbol)FunctionArgsArray?.Type == p.Type)
                        {
                            _il.EmitLocalLoad(FunctionArgsArray);
                            return p.Type;
                        }
                        else
                        {
                            throw this.NotImplementedException(
                                "cannot pass caller arguments, " +
                                FunctionArgsArray == null ? "arguments not fetched" : "parameter type does not match");
                        }

                    case ImportValueAttributeData.ValueSpec.Locals:
                        Debug.Assert(p.Type.Is_PhpArray());
                        if (!HasUnoptimizedLocals) throw new InvalidOperationException();
                        return LocalsPlaceOpt.EmitLoad(Builder).Expect(CoreTypes.PhpArray);    // PhpArray

                    case ImportValueAttributeData.ValueSpec.This:
                        Debug.Assert(p.Type.IsObjectType());
                        return this.EmitPhpThisOrNull();           // object

                    case ImportValueAttributeData.ValueSpec.CallerStaticClass:
                        // current "static"
                        if (p.Type == CoreTypes.PhpTypeInfo)
                        {
                            return EmitLoadStaticPhpTypeInfo();
                        }
                        throw ExceptionUtilities.UnexpectedValue(p.Type);

                    case ImportValueAttributeData.ValueSpec.CallerClass:
                        // current class context (self)
                        // note, can be obtain dynamically (global code, closure)
                        return EmitLoadCurrentClassContext(p.Type);

                    case ImportValueAttributeData.ValueSpec.LocalVariable:
                        // load local variable with the name {p.Name}
                        if (this.Routine.LocalsTable.TryGetVariable(new VariableName(p.Name), out var variable))
                        {
                            var lhs = default(LhsStack);
                            // TODO: get alias without increasing reference count
                            EmitConvert(variable.EmitLoadValue(this, ref lhs, p.Type.Is_PhpAlias() ? BoundAccess.ReadRef : BoundAccess.Read), 0, p.Type);
                            return p.Type;
                        }
                        else if (p.Type.IsReferenceType) // PhpAlias
                        {
                            this.Builder.EmitNullConstant();
                            return p.Type;
                        }
                        else
                        {
                            return EmitLoadDefault(p.Type);
                        }

                    default:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }
            // class context
            else if (SpecialParameterSymbol.IsSelfParameter(p))
            {
                return EmitLoadCurrentClassContext(p.Type);
            }
            // late static
            else if (SpecialParameterSymbol.IsLateStaticParameter(p))
            {
                // PhpTypeInfo
                if (staticType != null)
                {
                    if (staticType.IsSelf() || staticType.IsParent())
                    {
                        return EmitLoadStaticPhpTypeInfo();
                    }
                    else
                    {
                        // LOAD <statictype>
                        return (TypeSymbol)staticType.EmitLoadTypeInfo(this);
                    }
                }
                else if (selfType != null && selfType.Is_PhpValue() == false && selfType.Is_PhpAlias() == false)
                {
                    // late static bound type is self:
                    return this.EmitLoadPhpTypeInfo(selfType);
                }
                else
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
            // dummy parameter for ctors
            else if (SpecialParameterSymbol.IsDummyFieldsOnlyCtorParameter(p))
            {
                return EmitLoadDefaultOfValueType(p.Type);  // default()
            }
            // unhandled
            else
            {
                throw ExceptionUtilities.UnexpectedValue(p.Type);
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
            var thisType = (code != ILOpCode.Newobj) ? LoadTargetInstance(thisExpr, method) : null;

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
                    LoadMethodSpecialArgument(p, staticType, thisType ?? method.ContainingType);
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

            // {this}
            var thisType = (code != ILOpCode.Newobj) ? LoadTargetInstance(thisExpr, method) : null;

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

            int arg_params_index = (arguments.Length != 0 && arguments[arguments.Length - 1].IsUnpacking) ? arguments.Length - 1 : -1; // index of params argument, otherwise -1
            int arg_params_consumed = 0; // count of items consumed from arg_params if any

            Debug.Assert(arg_params_index < 0 || (arguments[arg_params_index].Value is BoundVariableRef v && v.Variable.Type.IsSZArray()), $"Argument for params is expected to be a variable of type array, at {method.ContainingType.PhpName()}::{method.Name}().");

            for (; param_index < parameters.Length; param_index++)
            {
                var p = parameters[param_index];

                // special implicit parameters
                if (arg_index == 0 &&           // no source parameter were loaded yet
                    p.IsImplicitlyDeclared &&   // implicitly declared parameter
                    !p.IsParams)
                {
                    LoadMethodSpecialArgument(p, staticType, thisType ?? method.ContainingType);
                    continue;
                }

                // load arguments

                if (arg_index == arg_params_index) // arg is params
                {
                    #region LOAD params T[]

                    //
                    // treat argument as "params T[]"
                    //

                    var arg_type = (ArrayTypeSymbol)arguments[arg_index].Value.Emit(this); // {args}

                    // {args} on STACK:

                    if (p.IsParams)
                    {
                        // Template: {args}
                        if (arg_type != p.Type || arg_params_consumed > 0)  // we need to create new array from args
                        {
                            // T[] arrtmp = {arrtmp};
                            var arrtmp = GetTemporaryLocal(arg_type, false);
                            _il.EmitLocalStore(arrtmp);

                            // {args}.Skip({arg_params_consumed}) -> new T[]
                            this.ArrayToNewArray(new LocalPlace(arrtmp), arg_params_consumed, ((ArrayTypeSymbol)p.Type).ElementType);

                            //
                            ReturnTemporaryLocal(arrtmp);
                        }
                    }
                    else
                    {
                        // Template: {args}.Length <= {arg_params_consumed} ? default(T) : (T)args[arg_params_consumed]

                        if (p.RefKind != RefKind.None)
                        {
                            throw this.NotImplementedException($"p.RefKind == {p.RefKind}", thisExpr);
                        }

                        var lbldefault = new object();
                        var lblend = new object();

                        this.EmitArrayLength();                     // .Length
                        _il.EmitIntConstant(arg_params_consumed);   // {arg_params_consumed}
                        _il.EmitBranch(ILOpCode.Ble, lbldefault);

                        arguments[arg_index].Value.Emit(this);      // <args>
                        _il.EmitIntConstant(arg_params_consumed);   // <i>

                        if (p.Type.Is_PhpAlias() && arg_type.ElementType.Is_PhpValue())
                        {
                            // {args}[i].EnsureAlias()
                            _il.EmitOpCode(ILOpCode.Ldelema);               // ref args[i]
                            EmitSymbolToken(arg_type.ElementType, null);    // PhpValue
                            EmitCall(ILOpCode.Call, CoreMethods.Operators.EnsureAlias_PhpValueRef);
                        }
                        else
                        {
                            // (T)args[i]
                            _il.EmitOpCode(ILOpCode.Ldelem); // args[i]
                            EmitSymbolToken(arg_type.ElementType, null);
                            EmitConvert(arg_type.ElementType, 0, p.Type);
                        }

                        _il.EmitBranch(ILOpCode.Br, lblend);

                        // default(T)
                        _il.MarkLabel(lbldefault);
                        EmitParameterDefaultValue(p);

                        //
                        _il.MarkLabel(lblend);

                        //
                        arg_params_consumed++;
                    }

                    #endregion

                    continue;
                }

                if (p.IsParams)
                {
                    Debug.Assert(parameters.Length == param_index + 1, $"params should be the last parameter, at {method.ContainingType.PhpName()}::{method.Name}()."); // p is last one
                    Debug.Assert(p.Type.IsArray(), $"params should be of type array, at {method.ContainingType.PhpName()}::{method.Name}().");

                    var p_element = ((ArrayTypeSymbol)p.Type).ElementType;

                    if (arg_params_index >= 0)
                    {
                        // we have to load remaining arguments into an array and unroll the arg_params
                        // { arg_i, ..., arg_params[] }

                        #region LOAD new Array( ..., ...params )

                        /*
                         * Template:
                         * params = new PhpValue[ {arguments.Length - arg_index - 1} +  {arguments[arg_params_index]}.Length ]
                         * params[0] = arg_i
                         * params[1] = ..
                         * Array.Copy( params, arguments[arg_params_index] )
                         */

                        int arr_size1 = arguments.Length - arg_index - 1; // remaining arguments without arg_params
                        var arg_params = arguments[arg_params_index].Value as BoundVariableRef;
                        Debug.Assert(arg_params != null, $"Argument for params is expected to be a variable reference expression, at {method.ContainingType.PhpName()}::{method.Name}().");

                        // <params> = new [arr_size1 + arg_params.Length]

                        _il.EmitIntConstant(arr_size1);
                        arguments[arg_params_index].Value.Emit(this); // {args}
                        EmitArrayLength();
                        _il.EmitOpCode(ILOpCode.Add);

                        _il.EmitOpCode(ILOpCode.Newarr);
                        EmitSymbolToken(p_element, null);

                        var params_loc = GetTemporaryLocal(p.Type, false);
                        _il.EmitLocalStore(params_loc);

                        // { arg_i, ..., arg_(n-1) }
                        for (int i = 0; i < arr_size1; i++)
                        {
                            _il.EmitLocalLoad(params_loc);  // <params>
                            _il.EmitIntConstant(i);         // [i]
                            EmitConvert(arguments[arg_index + i].Value, p_element);
                            _il.EmitOpCode(ILOpCode.Stelem);
                            EmitSymbolToken(p_element, null);
                        }

                        // { ... arg_params } // TODO: use Array.Copy() if element types match
                        EmitEnumerateArray(arg_params.Place(), 0, (loc_i, loader) =>
                        {
                            _il.EmitLocalLoad(params_loc);  // <params>

                            _il.EmitIntConstant(arr_size1);         // arr_size1
                            _il.EmitLocalLoad(loc_i);               // {i}
                            _il.EmitOpCode(ILOpCode.Add);           // +

                            EmitConvert(loader(), 0, p_element);
                            _il.EmitOpCode(ILOpCode.Stelem);
                            EmitSymbolToken(p_element, null);
                        });

                        // params : PhpValue[]
                        _il.EmitLocalLoad(params_loc);
                        ReturnTemporaryLocal(params_loc);

                        arg_index = arguments.Length;

                        #endregion
                    }
                    else
                    {
                        // easy case,
                        // wrap remaining arguments to array
                        var values = (arg_index < arguments.Length) ? arguments.Skip(arg_index).AsImmutable() : ImmutableArray<BoundArgument>.Empty;
                        arg_index += values.Length;
                        Emit_NewArray(p_element, values);
                    }

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
            for (; arg_index < arguments.Length && arg_index != arg_params_index; arg_index++)
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
        /// <param name="thisPlaceExplicit">Optionaly specified place of object instance to call the method on.</param>
        /// <param name="callvirt">Whether to call the method virtually through <c>.callvirt</c>.</param>
        /// <returns>Return of <paramref name="target"/>.</returns>
        internal TypeSymbol EmitForwardCall(MethodSymbol target, MethodSymbol thismethod, IPlace thisPlaceExplicit = null, bool callvirt = false)
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
                    Variable = PlaceReference.Create(thisPlaceExplicit ?? this.ThisPlaceOpt),
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
            BoundTypeRef staticTypeRef = null;

            for (int i = 0; i < givenps.Length; i++)
            {
                var p = givenps[i];
                if (arguments.Count == 0 && p.IsImplicitlyDeclared && !p.IsParams)
                {
                    if (SpecialParameterSymbol.IsLateStaticParameter(p))
                    {
                        staticTypeRef = BoundTypeRefFactory.CreateFromPlace(new ParamPlace(p));
                    }

                    continue;
                }

                var expr = new BoundVariableRef(new BoundVariableName(new VariableName(p.MetadataName)))
                {
                    Variable = new PlaceReference(new ParamPlace(p)), // new ParameterReference(p, Routine),
                    Access = BoundAccess.Read
                };

                var arg = p.IsParams
                    ? BoundArgument.CreateUnpacking(expr)   // treated as "params PhpArray[]" by EmitCall
                    : BoundArgument.Create(expr);           // treated as ordinary parameter

                arguments.Add(arg);
            }

            //
            ILOpCode opcode = callvirt ? ILOpCode.Callvirt : ILOpCode.Call;

            // emit call of target
            return EmitCall(opcode, target, thisExpr, arguments.AsImmutableOrEmpty(), staticTypeRef);
        }

        /// <summary>
        /// Emits necessary conversion and copying of value returned from a method call.
        /// </summary>
        /// <param name="stack">Result value type on stack.</param>
        /// <param name="method">Called method. Can be <c>null</c> for indirect method calls.</param>
        /// <param name="access">Expression access.</param>
        /// <returns>New type on stack.</returns>
        internal TypeSymbol EmitMethodAccess(TypeSymbol stack, MethodSymbol method, BoundAccess access)
        {
            // cast negative number or null to false (CastToFalse)
            // and copy the value on stack if necessary
            if (access.IsRead)
            {
                if (method != null && method.CastToFalse)
                {
                    // casts to false and copy the value
                    //if (stack.IsNullableType())
                    //{
                    //    // unpack Nullable<T>
                    //    stack = EmitNullableCastToFalse(stack, access.IsReadValueCopy);
                    //} else

                    //
                    stack = EmitCastToFalse(stack, access.TargetType);
                }

                if (access.EnsureArray)
                {
                    // GetArrayAccess

                    if (stack == CoreTypes.PhpAlias)
                    {
                        // <stack>.EnsureArray()
                        return EmitCall(ILOpCode.Callvirt, CoreMethods.PhpAlias.EnsureArray);
                    }

                    if (stack == CoreTypes.PhpValue)
                    {
                        // <stack>.GetArrayAccess()
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.Operators.GetArrayAccess_PhpValueRef);
                    }

                    if (stack.IsReferenceType)
                    {
                        if (stack.ImplementsInterface(CoreTypes.IPhpArray))
                        {
                            // IPhpArray
                            return stack;
                        }

                        // Operators.EnsureArray(<stack>)
                        return EmitCall(ILOpCode.Call, CoreMethods.Operators.EnsureArray_Object);
                    }
                }
                else if (access.EnsureObject)
                {
                    // AsObject

                    if (stack == CoreTypes.PhpAlias)
                    {
                        // <stack>.EnsureObject()
                        return EmitCall(ILOpCode.Callvirt, CoreMethods.PhpAlias.EnsureObject);
                    }

                    if (stack == CoreTypes.PhpValue)
                    {
                        // <stack>.AsObject()
                        EmitPhpValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.AsObject);
                    }
                }
                else if (access.IsReadRef)
                {
                    if (stack != CoreTypes.PhpAlias)
                    {
                        EmitConvertToPhpValue(stack, 0);
                        stack = Emit_PhpValue_MakeAlias();
                    }
                }
                else
                {
                    // routines returning aliased value but
                    // read by value must dereference:
                    // BoundCopyValue is not bound
                    EmitPhpAliasDereference(ref stack);
                    // TODO: DeepCopy if being assigned ?
                }
            }

            //
            return stack;
        }

        /// <summary>
        /// Converts <b>negative</b> number or <c>null</c> to <c>FALSE</c>.
        /// </summary>
        /// <param name="stack">Type of value on stack.</param>
        /// <param name="targetType">Optional hint, the expected conversion of the resulting value.</param>
        /// <returns>New type of value on stack.</returns>
        internal TypeSymbol EmitCastToFalse(TypeSymbol stack, TypeSymbol targetType = null)
        {
            if (stack.SpecialType == SpecialType.System_Boolean)
            {
                return stack;
            }

            if (stack.IsNullableType(out var ttype))
            {
                // optimization:
                if (targetType?.SpecialType == SpecialType.System_Boolean)
                {
                    // Template: stack.HasValue && (bool)stack.Value
                    return EmitNullableCoalescing(stack,
                    (t) =>
                    {
                        // (PhpValue)<stack>
                        return EmitConvertToBool(t, 0);
                    },
                    () =>
                    {
                        // false
                        Builder.EmitBoolConstant(false);
                        return CoreTypes.Boolean;
                    });
                }

                // stack.HasValue ? stack.Value : FALSE
                return EmitNullableCoalescing(stack,
                    (t) =>
                    {
                        // (PhpValue)<stack>
                        return EmitConvertToPhpValue(t, 0);
                    },
                    () =>
                    {
                        // PhpValue.False
                        return Emit_PhpValue_False();
                    });
            }

            // optimization:
            if (targetType?.SpecialType == SpecialType.System_Boolean)
            {
                // will be converting to bool anyways
                if (stack.SpecialType == SpecialType.System_Int32)
                {
                    _il.EmitIntConstant(0);     // > 0
                    _il.EmitOpCode(ILOpCode.Cgt);
                    return CoreTypes.Boolean;
                }
                else if (stack.SpecialType == SpecialType.System_Int64)
                {
                    _il.EmitLongConstant(0);    // > 0L
                    _il.EmitOpCode(ILOpCode.Cgt);
                    return CoreTypes.Boolean;
                }
                else if (stack.SpecialType == SpecialType.System_Double)
                {
                    _il.EmitDoubleConstant(0.0);    // > 0.0
                    _il.EmitOpCode(ILOpCode.Cgt);
                    return CoreTypes.Boolean;
                }
                else if (stack.SpecialType == SpecialType.System_String)
                {
                    // Convert.ToBoolean({stack})
                    return EmitCall(ILOpCode.Call, CoreMethods.Operators.ToBoolean_String);
                }
                else if (stack.Is_PhpString())
                {
                    // Convert.ToBoolean({stack})
                    return EmitCall(ILOpCode.Call, CoreMethods.Operators.ToBoolean_PhpString);
                }
                else if (stack.IsReferenceType)
                {
                    // TODO: {stack} != null && Convert.ToBoolean({stack})
                }
            }

            if (targetType?.SpecialType == SpecialType.System_String)
            {
                if (stack.SpecialType == SpecialType.System_String)
                {
                    // STRING ?? FALSE).ToString() =>
                    // STRING ?? ""

                    // ?? ""
                    EmitNullCoalescing((_cg) =>
                    {
                        _cg.Builder.EmitStringConstant(string.Empty);
                    });
                    return stack; // String
                }
            }

            // Template: <stack> ?? FALSE

            var lblfalse = new NamedLabel("CastToFalse:FALSE");
            var lblend = new NamedLabel("CastToFalse:end");

            _il.EmitOpCode(ILOpCode.Dup);   // <stack>

            // emit branching to lblfalse
            if (stack.SpecialType == SpecialType.System_Int32)
            {
                _il.EmitIntConstant(0);     // < 0
                _il.EmitBranch(ILOpCode.Blt, lblfalse);
            }
            else if (stack.SpecialType == SpecialType.System_Int64)
            {
                _il.EmitLongConstant(0);    // < 0L
                _il.EmitBranch(ILOpCode.Blt, lblfalse);
            }
            else if (stack.SpecialType == SpecialType.System_Double)
            {
                _il.EmitDoubleConstant(0.0);    // < 0.0
                _il.EmitBranch(ILOpCode.Blt, lblfalse);
            }
            else if (stack == CoreTypes.PhpString)
            {
                EmitCall(ILOpCode.Call, CoreMethods.PhpString.IsNull_PhpString);    // PhpString.IsDefault
                _il.EmitBranch(ILOpCode.Brtrue, lblfalse);
            }
            else if (stack.IsReferenceType)
            {
                _il.EmitNullConstant(); // == null
                _il.EmitBranch(ILOpCode.Beq, lblfalse);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(stack);
            }

            // test(<stack>) ? POP,FALSE : (PhpValue)<stack>

            // (PhpValue)<stack>
            EmitConvertToPhpValue(stack, 0);
            _il.EmitBranch(ILOpCode.Br, lblend);

            // POP, PhpValue.False
            _il.MarkLabel(lblfalse);
            EmitPop(stack);
            Emit_PhpValue_False();

            //
            _il.MarkLabel(lblend);

            //
            return CoreTypes.PhpValue;
        }

        /// <summary>
        /// Converts <b>Nullable</b> without a value to <c>NULL</c>.
        /// </summary>
        /// <param name="stack">Type of Nullable&lt;T&gt; value on stack.</param>
        /// <param name="deepcopy">Whether to deep copy returned non-FALSE value.</param>
        /// <returns>New type of value on stack.</returns>
        internal TypeSymbol EmitNullableCastToNull(TypeSymbol stack, bool deepcopy)
        {
            return EmitNullableCoalescing(stack,
                (t) =>
                {
                    if (deepcopy)
                    {
                        // DeepCopy(<stack>)
                        t = EmitDeepCopy(t, false);
                    }
                    // (PhpValue)<stack>
                    return EmitConvertToPhpValue(t, 0);
                },
                () =>
                {
                    // PhpValue.Null
                    return Emit_PhpValue_Null();
                });
        }

        /// <summary>
        /// Emits code that converts Nullable. Expects Nullable{T} on stack:
        /// {stack}.HasValue ? {stack}.GetValueOrDefault() : default
        /// </summary>
        /// <param name="stack">Type of Nullable&lt;T&gt; value on stack.</param>
        /// <param name="valueEmitter">Delegate to emit a conversion from &lt;T&gt;. &lt;T&gt; is on the stack.</param>
        /// <param name="novalueEmitter">Delegate to emit a value if the Nullable has no value. If no provided, a default of {T} is put on the stack.</param>
        /// <returns>New type of value on stack.</returns>
        internal TypeSymbol EmitNullableCoalescing(TypeSymbol stack, Func<TypeSymbol, TypeSymbol> valueEmitter, Func<TypeSymbol> novalueEmitter = null)
        {
            if (stack.IsNullableType(out var t) == false)
            {
                throw new ArgumentException("Not Nullable`1", nameof(stack));
            }

            // Template:
            // tmp = stack;
            // tmp.HasValue ? tmp.Value : NULL

            var lbltrue = new NamedLabel("has value");
            var lblend = new NamedLabel("end");

            var tmp = GetTemporaryLocal(stack, immediateReturn: true);
            _il.EmitLocalStore(tmp);

            // Template: tmp.HasValue ??
            _il.EmitLocalAddress(tmp);
            EmitCall(ILOpCode.Call, DeclaringCompilation.System_Nullable_T_HasValue(stack))
                .Expect(SpecialType.System_Boolean);

            _il.EmitBranch(ILOpCode.Brtrue, lbltrue);

            // Template: PhpValue.Null
            var t1 = novalueEmitter != null ? novalueEmitter() : EmitLoadDefault(t);

            _il.EmitBranch(ILOpCode.Br, lblend);

            // Template: (PhpValue)tmp.GetValueOrDefault()
            _il.MarkLabel(lbltrue);
            _il.EmitLocalAddress(tmp);
            EmitCall(ILOpCode.Call, DeclaringCompilation.System_Nullable_T_GetValueOrDefault(stack))
                .Expect(t);

            // (PhpValue)<stack>
            var t2 = valueEmitter(t);

            //
            _il.MarkLabel(lblend);

            //
            if (t1 != t2) throw new InvalidOperationException($"Value types do not match, '{t1}' and '{t2}'.");
            return t1;
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
        /// Emits <c>?:</c> operation against the value on top of the evaluation stack.
        /// </summary>
        internal void EmitNullCoalescing(Action notnullemitter, Action nullemitter)
        {
            var lbl_notnull = new NamedLabel("NotNull");
            var lbl_end = new object();

            _il.EmitOpCode(ILOpCode.Dup);
            _il.EmitBranch(ILOpCode.Brtrue, lbl_notnull);

            _il.EmitOpCode(ILOpCode.Pop);
            nullemitter();
            _il.EmitBranch(ILOpCode.Br, lbl_end);

            _il.MarkLabel(lbl_notnull);
            notnullemitter();

            _il.MarkLabel(lbl_end);
        }

        /// <summary>
        /// Initializes place with a default value.
        /// This applies to structs without default ctor that won't work properly when uninitialized.
        /// </summary>
        internal void EmitInitializePlace(IPlace place)
        {
            Contract.ThrowIfNull(place);
            var t = place.Type;
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
                    // uninitialized:
                    if (t == CoreTypes.PhpString ||
                        t == CoreTypes.PhpValue)
                    {
                        Debug.Assert(t.IsValueType);
                        break;
                    }

                    // PhpNumber, PhpAlias:
                    if (t.IsValueType || t == CoreTypes.PhpAlias)
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

                TypeSymbol arr_element => ((ArrayTypeSymbol)arrplace.Type).ElementType;

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

                    var arr_element = ((ArrayTypeSymbol)arrplace.Type).ElementType;
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
                cg.EmitConvert(Target, type); // TODO: DetermineConversionKind(targetp)
            }

            /// <summary>
            /// Writes the value back to <see cref="Target"/> and free resources.
            /// </summary>
            protected virtual void WriteBackAndFree(CodeGenerator cg)
            {
                // Template: <Target> = <TmpLocal>;
                Target.BindPlace(cg).EmitStore(cg, TmpLocal, BoundAccess.Write);

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
        /// Determine whether the argument load complies with CLR semantic rather than PHP semantic.
        /// </summary>
        static bool DetermineClrSemantic(ParameterSymbol targetp)
        {
            var t = targetp.ContainingType;
            if (t.IsPhpSourceFile() || t.IsPhpUserType()) // || t.GetPhpExtensionAttribute() != null)
            {
                // PHP semantic
                return false;
            }

            if (t.ContainingAssembly is PEAssemblySymbol ass)
            {
                if (ass.IsPeachpieCorLibrary || ass.IsExtensionLibrary)
                {
                    // PHP semantic
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determine the argument load complies with PHP strict semantic.
        /// </summary>
        static bool DetermineStrictSemantic(ParameterSymbol targetp)
        {
            // TODO: strict mode on file/class level // cg.ContainingFile // [PhpStrictMode( On/Off/Clr/... )]

            var t = targetp.ContainingType;
            if (t.IsPhpSourceFile() || t.IsPhpUserType())
            {
                return true;
            }
            else if (targetp.Type.IsObjectType())
            {
                // CLR "object"
                return true;
            }

            // a library function
            return false;
        }

        static ConversionKind DetermineConversionKind(ParameterSymbol targetp)
        {
            if (DetermineStrictSemantic(targetp))
            {
                return ConversionKind.Strict;
            }
            else
            {
                return ConversionKind.Implicit;
            }
        }

        /// <summary>
        /// Loads argument from bound expression.
        /// </summary>
        void EmitLoadArgument(ParameterSymbol targetp, BoundExpression expr, List<WriteBackInfo> writebacks)
        {
            if (targetp.RefKind == RefKind.None)
            {
                // load argument

                if (targetp.Type.IsObjectType() && DetermineClrSemantic(targetp))
                {
                    // (object)expr.ToClr() // .box
                    EmitBox(Emit_ToClr(Emit(expr)));
                }
                else
                {
                    EmitConvert(expr, targetp.Type,
                        conversion: DetermineConversionKind(targetp),
                        notNull: targetp.HasNotNull);
                }
            }
            else
            {
                if (expr is BoundReferenceExpression refexpr)
                {
                    var place = refexpr.Place();
                    if (place != null && place.HasAddress && place.Type == targetp.Type)
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
                    // TODO: report as a diagnostic before this happens!
                    throw this.NotImplementedException("Argument must be passed as a variable.");
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
            Debug.Assert(arrplace.Type.IsSZArray());

            var arr_element = ((ArrayTypeSymbol)arrplace.Type).ElementType;
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
            FieldSymbol defaultvaluefield;

            if ((cvalue = targetp.ExplicitDefaultConstantValue) != null)
            {
                ptype = EmitLoadConstant(cvalue.Value, targetp.Type);
            }
            else if ((defaultvaluefield = targetp.DefaultValueField) != null)
            {
                Debug.Assert(defaultvaluefield.IsStatic);
                ptype = defaultvaluefield.EmitLoad(this);

                if (ptype.Is_Func_Context_PhpValue())
                {
                    this.EmitLoadContext();

                    // .Invoke( ctx )
                    ptype = this.Builder.EmitCall(Module, Diagnostics, ILOpCode.Callvirt, ptype.DelegateInvokeMethod());
                }
            }
            else if ((boundinitializer = (targetp as IPhpValue)?.Initializer) != null)
            {
                // DEPRECATED AND NOT USED ANYMORE:
                Debug.WriteLine("SHOULD NOT BE USED ANYMORE");

                var cg = this;

                if (targetp.OriginalDefinition is SourceParameterSymbol)
                {
                    // emit using correct TypeRefContext:

                    // TODO: use `boundinitializer.Parent` instead of following
                    // magically determine the source routine corresponding to the initializer expression:
                    SourceRoutineSymbol srcr = null;
                    for (var r = targetp.ContainingSymbol;
                         srcr == null && r != null; // we still don't have to original SourceRoutineSymbol, but we have "r"
                         r = (r as SynthesizedMethodSymbol)?.ForwardedCall?.OriginalDefinition) // dig in and find original SourceRoutineSymbol wrapped by this synthesized stub
                    {
                        srcr = r as SourceRoutineSymbol;
                    }

                    Debug.Assert(srcr != null, "!srcr");

                    if (srcr != null)
                    {
                        cg = new CodeGenerator(this, srcr);
                    }
                }

                //
                cg.EmitConvert(boundinitializer, ptype = targetp.Type);
            }
            else if (targetp.IsParams)
            {
                // Template: System.Array.Empty<T>()
                Emit_EmptyArray(((ArrayTypeSymbol)targetp.Type).ElementType);
                return;
            }
            else
            {
                ptype = EmitLoadDefault(targetp.Type, 0);
            }

            // eventually convert emitted value to target parameter type
            EmitConvert(ptype, 0, targetp.Type);

            // ref, out
            if (targetp.RefKind == RefKind.Ref || targetp.RefKind == RefKind.Out) // this usually won't happen, ref parameters are not optional
            {
                // T tmp = <DEFAULT>
                var tmp = GetTemporaryLocal(targetp.Type, true); // TODO: should not be returned immediatelly, remember tmp and return it postcall
                Builder.EmitLocalStore(tmp);

                // ref tmp, 
                Builder.EmitLocalAddress(tmp);
                return;
            }
        }

        internal TypeSymbol EmitGetProperty(IPlace holder, PropertySymbol prop)
        {
            Debug.Assert(prop.IsStatic || holder != null);
            Debug.Assert(prop.GetMethod != null);
            Debug.Assert(prop.GetMethod.ParameterCount == 0);

            return prop.EmitLoadValue(this, holder);
        }

        internal TypeSymbol EmitCastClass(TypeSymbol from, TypeSymbol to)
        {
            Debug.Assert(!from.IsUnreachable);
            Debug.Assert(!to.IsUnreachable);

            if (from.IsOfType(to))
            {
                return from;
            }
            else
            {
                EmitCastClass(to);
                return to;
            }
        }

        internal void EmitCastClass(TypeSymbol type)
        {
            Debug.Assert(!type.IsUnreachable);

            // (T)
            if (type.IsReferenceType)
            {
                _il.EmitOpCode(ILOpCode.Castclass);
                EmitSymbolToken(type, null);
            }
            else
            {
                _il.EmitOpCode(ILOpCode.Unbox_any);
                EmitSymbolToken(type, null);
            }
        }

        /// <summary>
        /// Emits <c>PhpString.Blob.Append</c> expecting <c>PhpString.Blob</c> on top of evaluation stack.
        /// </summary>
        /// <param name="value">The expression to be appended.</param>
        /// <param name="expandConcat">Whether to skip evaluation of <c>concat</c> expression and directly append its arguments.</param>
        internal void Emit_PhpStringBlob_Append(BoundExpression value, bool expandConcat = true)
        {
            if (value is BoundConcatEx concat && expandConcat)
            {
                var args = concat.ArgumentsInSourceOrder;
                for (int i = 0; i < args.Length; i++)
                {
                    if (i < args.Length - 1)
                    {
                        _il.EmitOpCode(ILOpCode.Dup);   // PhpString.Blob
                    }

                    Emit_PhpStringBlob_Append(args[i].Value);
                }
            }
            else
            {
                if (!IsDebug && value.IsConstant() && ExpressionsExtension.IsEmptyStringValue(value.ConstantValue.Value))
                {
                    _il.EmitOpCode(ILOpCode.Pop);
                }
                else
                {
                    Emit_PhpStringBlob_Append(Emit(value));
                }
            }
        }

        /// <summary>
        /// Emits <c>PhpString.Blob.Append</c> expecting <c>PhpString.Blob</c> and <paramref name="ytype"/> on top of evaluation stack.
        /// </summary>
        /// <param name="ytype">Type of argument loaded on stack.</param>
        void Emit_PhpStringBlob_Append(TypeSymbol ytype)
        {
            if (ytype == CoreTypes.PhpAlias)
            {
                ytype = Emit_PhpAlias_GetValue();
            }

            if (ytype.SpecialType == SpecialType.System_Void)
            {
                _il.EmitOpCode(ILOpCode.Pop);
            }
            else if (ytype == CoreTypes.PhpString)
            {
                // Append(PhpString)
                EmitCall(ILOpCode.Callvirt, CoreMethods.PhpStringBlob.Add_PhpString);
            }
            else if (ytype == CoreTypes.PhpValue)
            {
                // Append(PhpValue, Context)
                EmitLoadContext();
                EmitCall(ILOpCode.Callvirt, CoreMethods.PhpStringBlob.Add_PhpValue_Context);
            }
            else if (ytype.IsByteArray())
            {
                // Append(byte[])
                EmitCall(ILOpCode.Callvirt, CoreMethods.PhpStringBlob.Add_ByteArray);
            }
            else
            {
                // Append(string)
                EmitConvertToString(ytype, 0);
                EmitCall(ILOpCode.Callvirt, CoreMethods.PhpStringBlob.Add_String);
            }
        }

        public void EmitEcho(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);
            Debug.Assert(expr.Access.IsRead);

            var concat = expr as BoundConcatEx;

            if (concat != null && concat.ArgumentsInSourceOrder.Length == 1)
            {
                EmitEcho(concat.ArgumentsInSourceOrder[0].Value);
                return;
            }

            if (expr is BoundLiteral literal && literal.ConstantValue.Value is byte[] bytes)
            {
                // Template: Operators.Echo(byte[])
                // avoids allocation
                // avoids conversion to PhpString
                EmitEcho(bytes.AsImmutable());
                return;
            }

            if (_optimizations.IsRelease())
            {
                // check if the value won't be an empty string:
                if (expr.ConstantValue.HasValue && ExpressionsExtension.IsEmptyStringValue(expr.ConstantValue.Value))
                {
                    return;
                }

                // avoid concatenation if possible:
                if (concat != null)
                {
                    // Check if arguments can be echo'ed separately without concatenating them,
                    // this is only possible if the arguments won't have side effects:
                    var issafe = true;
                    var concat_args = concat.ArgumentsInSourceOrder;
                    for (int i = 1; i < concat_args.Length; i++)
                    {
                        issafe &=
                            // TODO: add more expressions that are safe to echo
                            concat_args[i].Value.IsConstant() ||
                            concat_args[i].Value is BoundGlobalConst ||
                            concat_args[i].Value is BoundPseudoConst ||
                            concat_args[i].Value is BoundPseudoClassConst ||
                            concat_args[i].Value is BoundVariableRef;
                    }

                    if (issafe)
                    {
                        for (int i = 0; i < concat_args.Length; i++)
                        {
                            EmitEcho(concat_args[i].Value);
                        }
                        return;
                    }
                }
            }

            //
            EmitEcho(EmitSpecialize(expr));
        }

        public void EmitEcho(ImmutableArray<byte> data)
        {
            if (data.IsDefaultOrEmpty)
            {
                return;
            }

            //var span_T = DeclaringCompilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T).Construct(ImmutableArray.Create(CoreTypes.Byte.Symbol));

            //var ctor = (MethodSymbol)DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__ctor);

            //Builder.EmitArrayBlockFieldRef(data, null, Diagnostics);
            //Builder.EmitIntConstant(data.Length);

            //// consumes target ref, data ptr and size, pushes nothing
            //Builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -3);
            //EmitSymbolToken(ctor.AsMember(span_T), null);

            //// CALL Echo( ReadOnlySpan<byte>, Context )
            //EmitPop(EmitCall(ILOpCode.Call, CoreMethods.Operators.Echo_SpanByte_Context));

            // byte[] // TODO: NETSTANDARD2.1 // ReadOnlySpan<byte>, avoids creating the array
            Emit_NewArray(DeclaringCompilation.GetSpecialType(SpecialType.System_Byte), data.Length);
            Builder.EmitArrayBlockInitializer(data, null, Diagnostics);

            // Context
            EmitLoadContext();

            // CALL Echo( byte[], Context )
            EmitPop(EmitCall(ILOpCode.Call, CoreMethods.Operators.Echo_ByteArray_Context));
        }

        /// <summary>Emits <c>echo</c> statement of the type on stack.</summary>
        public void EmitEcho(TypeSymbol type)
        {
            //
            MethodSymbol method;

            switch (type.SpecialType)
            {
                case SpecialType.System_Void:
                    // nothing
                    return;
                case SpecialType.System_String:
                    method = CoreMethods.Operators.Echo_String_Context;
                    break;
                case SpecialType.System_Double:
                    method = CoreMethods.Operators.Echo_Double_Context;
                    break;
                case SpecialType.System_Int32:
                    method = CoreMethods.Operators.Echo_Int32_Context;
                    break;
                case SpecialType.System_Int64:
                    method = CoreMethods.Operators.Echo_Long_Context;
                    break;
                case SpecialType.System_Boolean:
                    method = CoreMethods.Operators.Echo_Bool_Context;
                    break;
                default:
                    if (type == CoreTypes.PhpString)
                    {
                        method = CoreMethods.Operators.Echo_PhpString_Context;
                    }
                    else if (type == CoreTypes.PhpNumber)
                    {
                        method = CoreMethods.Operators.Echo_PhpNumber_Context;
                    }
                    else if (type == CoreTypes.PhpValue)
                    {
                        method = CoreMethods.Operators.Echo_PhpValue_Context;
                    }
                    else if (type == CoreTypes.PhpAlias)
                    {
                        method = CoreMethods.Operators.Echo_PhpAlias_Context;
                    }
                    else if (type.IsNullableType())
                    {
                        EmitEchoOfNullable(type);
                        return;
                    }
                    else if (type.IsByteArray())
                    {
                        EmitLoadContext();
                        // CALL Echo( byte[], Context )
                        EmitCall(ILOpCode.Call, CoreMethods.Operators.Echo_ByteArray_Context);
                        return;
                    }
                    else
                    {
                        // TODO: check expr.TypeRefMask if it is only NULL
                        EmitBox(type);
                        method = CoreMethods.Operators.Echo_Object_Context;
                    }
                    break;
            }

            Debug.Assert(method != null);

            // Template: Operators.Echo(<stack>, <ctx>);
            this.EmitLoadContext();
            EmitCall(ILOpCode.Call, method);
        }

        void EmitEchoOfNullable(TypeSymbol stack)
        {
            if (stack.IsNullableType(out var t) == false)
            {
                throw new ArgumentException("Not Nullable`1", nameof(stack));
            }

            // Template:
            // tmp = <stack>;
            // if (tmp.HasValue) echo tmp.Value;

            var lblend = new NamedLabel("end");

            var tmp = GetTemporaryLocal(stack, immediateReturn: true);
            _il.EmitLocalStore(tmp);

            // Template: if (tmp.HasValue)
            _il.EmitLocalAddress(tmp);
            EmitCall(ILOpCode.Call, DeclaringCompilation.System_Nullable_T_HasValue(stack));
            _il.EmitBranch(ILOpCode.Brfalse, lblend);

            // Template: Echo( tmp.GetValueOrDefault() )
            _il.EmitLocalAddress(tmp);
            EmitCall(ILOpCode.Call, DeclaringCompilation.System_Nullable_T_GetValueOrDefault(stack));
            EmitEcho(t);

            // lblend:
            _il.MarkLabel(lblend);
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

        public void EmitIntStringKey(long key)
        {
            _il.EmitLongConstant(key);
            EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_long);
        }

        public bool TryEmitCachedIntStringKey(string key)
        {
            // lookup common keys:
            var key_fields = CoreTypes.CommonPhpArrayKeys.Symbol.GetMembers(key);   // 0 or 1, FieldSymbol
            var key_field = key_fields.IsDefaultOrEmpty ? null : (FieldSymbol)key_fields[0];
            if (key_field != null)
            {
                Debug.Assert(key_field.IsStatic, "!key_field.IsStatic");
                Debug.Assert(key_field.Type == CoreTypes.IntStringKey, "key_field.Type != IntStringKey");

                // Template: .ldsfld key_field
                key_field.EmitLoad(this);
                return true;
            }

            return false;
        }

        public TypeSymbol EmitIntStringKey(string key)
        {
            // try convert string to integer as it is in PHP:
            if (TryConvertToIntKey(key, out var ikey))
            {
                EmitIntStringKey(ikey);
            }
            else if (TryEmitCachedIntStringKey(key))
            {
                // ok
            }
            else
            {
                // Template: new IntStringKey( <key> )
                _il.EmitStringConstant(key);
                EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_string);
            }

            return this.CoreTypes.IntStringKey;
        }

        internal static bool TryConvertToIntKey(string key, out long ikey)
        {
            ikey = default;

            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            // See Pchp.Core.Convert.StringToArrayKey:

            if (key.Length > 1)
            {
                // following are treated as string keys:
                // "-0..."
                // "-0"
                // "0..."
                if (key[0] == '0') return false;
                if (key[0] == '-' && key[1] == '0') return false;
            }


            return long.TryParse(key, out ikey);
        }

        public TypeSymbol EmitIntStringKey(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);

            var constant = expr.ConstantValue;
            if (constant.HasValue)
            {
                // the following can be optimized in compile time:
                switch (constant.Value)
                {
                    case string s:
                        return EmitIntStringKey(s);
                    case null:
                        return EmitIntStringKey(string.Empty);
                }
            }

            //
            return this.EmitConvertToIntStringKey(Emit(expr));
        }

        /// <summary>
        /// Emits declaring function into the context.
        /// </summary>
        public void EmitDeclareFunction(SourceFunctionSymbol f)
        {
            Debug.Assert(f != null);
            Debug.Assert(!f.IsUnreachable);

            // <ctx>.DeclareFunction(RoutineInfo)
            EmitLoadContext();
            f.EmitLoadRoutineInfo(this);

            EmitCall(ILOpCode.Call, CoreMethods.Context.DeclareFunction_RoutineInfo);
        }

        /// <summary>
        /// Set of types which declaration was already emitted at the beginning of script.
        /// </summary>
        HashSet<SourceTypeSymbol> _staticallyDeclaredTypes;

        /// <summary>
        /// Emits declaration of types that can be declared at the beginning of script.
        /// </summary>
        public void EmitDeclareTypesInParsePhase(IEnumerable<SourceTypeSymbol> types)
        {
            Contract.ThrowIfNull(types);

            foreach (var t in types)
            {
                // try to declare the type
                // NOTE: do not declare conditionally declared types (i.e. contained in IF)
                // NOTE: DO declare even types that are unreachable (e.g. after `return` statement)

                if (!t.IsConditional && EmitDeclareType(t, parsePhase: true))
                {
                    _staticallyDeclaredTypes ??= new HashSet<SourceTypeSymbol>();
                    _staticallyDeclaredTypes.Add(t);
                }
            }
        }

        /// <summary>
        /// Emits type declaration into the context.
        /// </summary>
        /// <param name="t">Type to be declared.</param>
        /// <param name="parsePhase"><c>true</c> in case we're just trying to declare the type; it won't cause any autoload, it won't cause any runtime exception.</param>
        /// <returns>Value indicating the type got declared unconditionally.</returns>
        public bool EmitDeclareType(SourceTypeSymbol t, bool parsePhase)
        {
            Contract.ThrowIfNull(t);

            if (t.IsErrorType())
            {
                throw new InvalidOperationException("Attempt to declare an error type at " + ExceptionUtilities.GuessSourceLocation(this));
            }

            if (_staticallyDeclaredTypes?.Contains(t) == true)
            {
                // already declared statically
                return true;
            }

            // optional label after the "DeclareType" operation
            object lblSkip = null;
            bool alwaysDeclared = true;

            // autoload base types or throw an error
            var versions = t.HasVersions ? t.AllReachableVersions() : default;
            if (versions.IsDefault == false && versions.Length > 1)
            {
                if (parsePhase)
                {
                    // TODO: check what dependant types are declared and based on that, declare "t", or nothing
                    return false;
                }

                // emit declaration of type that has ambiguous versions
                EmitVersionedTypeDeclaration(versions);
            }
            else
            {
                // Ensure to emit only reachable type
                if (t.HasVersions)
                {
                    // TODO: Error when all the ancestors have been eliminated
                    Debug.Assert(versions.Length == 1);
                    t = versions[0];
                }

                // types that are expected to be declared prior to declaring "t"
                var dependent = t.GetDependentSourceTypeSymbols();

                if (parsePhase)
                {
                    foreach (var d in dependent)
                    {
                        if (IsTypeDeclaredCheckNecessary(d))
                        {
                            lblSkip ??= new NamedLabel("skip_DeclareType");

                            alwaysDeclared = false; // type might not be declared at the end, we'll have to declare it properly again

                            // Template: if (<ctx>.IsUserTypeDeclared(d) == false) goto lblSkip;
                            EmitLoadContext();

                            if (d.IsTraitType())
                            {
                                EmitLoadToken(d.AsUnboundGenericType(), null);
                                EmitCall(ILOpCode.Call, CoreMethods.Dynamic.GetPhpTypeInfo_RuntimeTypeHandle);
                            }
                            else
                            {
                                EmitLoadPhpTypeInfo(d);
                            }

                            EmitCall(ILOpCode.Call, CoreMethods.Helpers.IsUserTypeDeclared_Context_PhpTypeInfo); // bool

                            _il.EmitBranch(ILOpCode.Brfalse, lblSkip); // if (false) goto lblSkip;
                        }
                    }
                }
                else
                {
                    foreach (var d in dependent)
                    {
                        // ensure all types are loaded into context,
                        // autoloads if necessary
                        EmitExpectTypeDeclared(d);
                    }
                }

                if (t.Arity == 0)
                {
                    // <ctx>.DeclareType<T>()
                    EmitLoadContext();
                    EmitCall(ILOpCode.Call, CoreMethods.Context.DeclareType_T.Symbol.Construct(t));
                }
                else
                {
                    // <ctx>.DeclareType( PhpTypeInfo, Name )
                    EmitLoadContext();
                    EmitLoadToken(t.AsUnboundGenericType(), null);
                    EmitCall(ILOpCode.Call, CoreMethods.Dynamic.GetPhpTypeInfo_RuntimeTypeHandle);
                    Builder.EmitStringConstant(t.FullName.ToString());
                    EmitCall(ILOpCode.Call, CoreMethods.Context.DeclareType_PhpTypeInfo_String);
                }
            }

            //
            if (lblSkip != null)
            {
                _il.MarkLabel(lblSkip);
            }

            //
            Debug.Assert(_il.IsStackEmpty);
            return alwaysDeclared;
        }

        /// <summary>
        /// Gets value indicating runtime check for given type existance may be necessary.
        /// </summary>
        private bool IsTypeDeclaredCheckNecessary(ITypeSymbol d)
        {
            // the type was statically declared in this file already
            if (d.OriginalDefinition is SourceTypeSymbol srct && _staticallyDeclaredTypes?.Contains(srct) == true)
            {
                return false;
            }

            if (d is NamedTypeSymbol ntype)
            {
                if (ntype.IsAnonymousType || !ntype.IsPhpUserType())
                {
                    // anonymous classes are not declared
                    // regular CLR types declared in app context
                    return false;
                }

                // TODO: type has been checked already in current branch -> skip

                if (this.CallerType != null && this.CallerType.IsOfType(ntype))
                {
                    // the type is a sub-type of current class context, so it must be declared for sure
                    // e.g. self, parent
                    return false;
                }


                return true;
            }

            return false;
        }

        /// <summary>
        /// If necessary, emits autoload and check the given type is loaded into context.
        /// </summary>
        public void EmitExpectTypeDeclared(ITypeSymbol d)
        {
            Debug.Assert(((TypeSymbol)d).IsValidType());
            Debug.Assert(!((TypeSymbol)d).IsUnreachable);

            if (IsTypeDeclaredCheckNecessary(d) && d is NamedTypeSymbol ntype)
            {
                if (ntype.OriginalDefinition is SourceTypeSymbol srct && ReferenceEquals(srct.ContainingFile, this.ContainingFile) && !srct.Syntax.IsConditional)
                {
                    // declared in same file unconditionally,
                    // we don't have to check it here
                    return;
                }

                if (ntype.OriginalDefinition is IPhpTypeSymbol phpt && phpt.AutoloadFlag == 2)
                {
                    // type is autoloaded without side effects
                    return;
                }

                if (ntype.Arity == 0)
                {
                    // Template: ctx.ExpectTypeDeclared<d>
                    EmitLoadContext();
                    EmitCall(ILOpCode.Call, CoreMethods.Context.ExpectTypeDeclared_T.Symbol.Construct(ntype));
                }
                else
                {
                    // workaround for traits - constructed traits do not match the declaration in Context
                    if (ntype.IsTraitType())
                    {
                        // Template: ctx.ExpectTypeDeclared(GetPhpTypeInfo(RuntimeTypeHandle(T<>)))
                        EmitLoadContext();

                        EmitLoadToken(ntype.AsUnboundGenericType(), null);
                        EmitCall(ILOpCode.Call, CoreMethods.Dynamic.GetPhpTypeInfo_RuntimeTypeHandle);

                        EmitCall(ILOpCode.Call, CoreMethods.Context.ExpectTypeDeclared_PhpTypeInfo);
                    }
                    else
                    {
                        // should not happen,
                        // user types cannot be generic types
                        Debug.Fail($"Unexpected: a user type '{ntype.Name}' has type arguments.");
                    }
                }
            }
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
                Debug.Assert(!v.IsUnreachable);

                // TODO: error when the type version reports it depends on a user type which will never be declared because of a library type

                var deps = v.GetDependentSourceTypeSymbols();
                foreach (var d in deps)
                {
                    var fullname = d.PhpQualifiedName(); // class, interface, or constructed trait

                    if (!dependent.TryGetValue(fullname, out var set))
                    {
                        dependent[fullname] = set = new HashSet<NamedTypeSymbol>();
                    }

                    set.Add(d);
                }
            }

            //
            var dependent_handles = new Dictionary<QualifiedName, LocalDefinition>();

            // resolve dependent types:
            foreach (var d in dependent)
            {
                if (d.Value.Count == 1)
                {
                    EmitExpectTypeDeclared(d.Value.Single());
                }
                else
                {
                    var tname = d.Key;

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

            Debug.Assert(dependent_handles.Count != 0, "the type declaration is not versioned in result, there should be a single version");

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
                Debug.Assert(versions[0].Arity == 0);   // declare as unbound generic type, see EmitDeclareType
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
                Debug.Assert(!h.IsUnreachable);

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
                        this.EmitCallerTypeHandle();
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(p.Name);
                }
            }

            //
            return EmitCall(ILOpCode.Call, mainmethod);
        }

        public TypeSymbol EmitLoadConstant(object value, TypeSymbol targetOpt = null, bool notNull = false)
        {
            if (value == null)
            {
                if (notNull)
                {
                    // should be reported already
                    // Diagnostics.Add( ... )
                    Debug.Fail("value cannot be null");
                }

                if (targetOpt != null && targetOpt.IsValueType)
                {
                    return EmitLoadDefaultOfValueType(targetOpt);
                }
                else // reference type
                {
                    if (targetOpt == CoreTypes.PhpAlias)
                    {
                        // new PhpAlias(PhpValue.Null)
                        Emit_PhpValue_Null();
                        return Emit_PhpValue_MakeAlias();
                    }
                    else
                    {
                        Builder.EmitNullConstant();
                        return targetOpt ?? CoreTypes.Object;
                    }
                }
            }
            else if (value is int i)
            {
                switch (targetOpt.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Boolean:
                        _il.EmitBoolConstant(i != 0);
                        return targetOpt;
                    case SpecialType.System_Int64:
                        _il.EmitLongConstant(i);
                        return targetOpt;
                    case SpecialType.System_Double:
                        _il.EmitDoubleConstant(i);
                        return targetOpt;
                    case SpecialType.System_String:
                        _il.EmitStringConstant(i.ToString());
                        return targetOpt;
                }

                Builder.EmitIntConstant((int)value);
                return CoreTypes.Int32;
            }
            else if (value is long l)
            {
                switch (targetOpt.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Boolean:
                        _il.EmitBoolConstant(l != 0);
                        return targetOpt;
                    case SpecialType.System_Int32:
                        _il.EmitIntConstant((int)l);
                        return targetOpt;
                    case SpecialType.System_Double:
                        _il.EmitDoubleConstant(l);
                        return targetOpt;
                    case SpecialType.System_Single:
                        _il.EmitSingleConstant(l);
                        return targetOpt;
                    case SpecialType.System_String:
                        _il.EmitStringConstant(l.ToString());
                        return targetOpt;
                    default:
                        break;
                }

                Builder.EmitLongConstant(l);
                return CoreTypes.Long;
            }
            else if (value is string str)
            {
                switch (targetOpt.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Char:
                        if (str != null && str.Length == 1)
                        {
                            Builder.EmitCharConstant(str[0]);
                            return targetOpt;
                        }
                        break;
                    case SpecialType.System_Int32:
                        if (int.TryParse(str, out i))
                        {
                            Builder.EmitIntConstant(i);
                            return targetOpt;
                        }
                        break;
                    case SpecialType.System_Int64:
                        if (long.TryParse(str, out l))
                        {
                            Builder.EmitLongConstant(l);
                            return targetOpt;
                        }
                        break;
                    case SpecialType.System_Double:
                        if (double.TryParse(str, out var d))
                        {
                            Builder.EmitDoubleConstant(d);
                            return targetOpt;
                        }
                        break;
                }

                Builder.EmitStringConstant(str);
                return CoreTypes.String;
            }
            else if (value is byte[] bytes)
            {
                // CONSIDER: use the cached field directly
                // var field = Module.GetFieldForData(bytes.AsImmutable(), null, Diagnostic);

                var type = Emit_NewArray(DeclaringCompilation.GetSpecialType(SpecialType.System_Byte), bytes.Length);
                Builder.EmitArrayBlockInitializer(bytes.AsImmutable(), null, this.Diagnostics);
                return type;
            }
            else if (value is bool b)
            {
                switch (targetOpt.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Boolean:
                        break;
                    case SpecialType.System_String:
                        _il.EmitStringConstant(b ? "1" : "");
                        return targetOpt;
                    default:
                        if (targetOpt == CoreTypes.PhpValue)
                        {
                            return b ? Emit_PhpValue_True() : Emit_PhpValue_False();
                        }
                        break;
                }

                // template: LOAD bool
                Builder.EmitBoolConstant(b);
                return CoreTypes.Boolean;
            }
            else if (value is double d)
            {
                switch (targetOpt.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Boolean:
                        _il.EmitBoolConstant(d != 0.0);
                        return targetOpt;
                    case SpecialType.System_Int64:
                        _il.EmitLongConstant((long)d);
                        return targetOpt;
                }

                Builder.EmitDoubleConstant(d);
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
            else if (value is ulong ul)
            {
                switch (targetOpt.GetSpecialTypeSafe())
                {
                    case SpecialType.System_Boolean:
                        _il.EmitBoolConstant(ul != 0.0);
                        return targetOpt;
                    case SpecialType.System_Int64:
                        _il.EmitLongConstant((long)ul);
                        return targetOpt;
                    case SpecialType.System_Double:
                        _il.EmitDoubleConstant((double)ul);
                        return targetOpt;
                    case SpecialType.System_String:
                        _il.EmitStringConstant(ul.ToString());
                        return targetOpt;
                }

                _il.EmitLongConstant(unchecked((long)ul));
                return DeclaringCompilation.GetSpecialType(SpecialType.System_UInt64);
            }
            else if (value is char)
            {
                switch (targetOpt.GetSpecialTypeSafe())
                {
                    case SpecialType.System_String:
                        Builder.EmitStringConstant(value.ToString());
                        return targetOpt;
                }

                Builder.EmitCharConstant((char)value);
                return DeclaringCompilation.GetSpecialType(SpecialType.System_Char);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(value);
            }
        }

        public TypeSymbol EmitLoadDefault(TypeSymbol type, TypeRefMask typemask = default)
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
                                    Emit_PhpValue_False();
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

            return type;
        }

        /// <summary>
        /// Emits <c>default(valuetype)</c>.
        /// Handles special types with a default ctor.
        /// </summary>
        public TypeSymbol EmitLoadDefaultOfValueType(TypeSymbol valuetype)
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
            else if (valuetype == CoreTypes.RuntimeTypeHandle)
            {
                // LOAD Helpers.EmptyRuntimeTypeHandle
                _il.EmitOpCode(ILOpCode.Ldsfld);
                EmitSymbolToken(CoreMethods.Helpers.EmptyRuntimeTypeHandle, null);
            }
            else if (valuetype == CoreTypes.PhpString)
            {
                // LOAD PhpString.Default
                _il.EmitOpCode(ILOpCode.Ldsfld);
                EmitSymbolToken(CoreMethods.PhpString.Default, null);
            }
            else if (valuetype.IsNullableType(out var tType))
            {
                // CALL Helpers.EmptyNullable_T< tType >
                var method = CoreMethods.Helpers.EmptyNullable_T.Symbol.Construct(ImmutableArray.Create(tType));
                return EmitCall(ILOpCode.Call, method).Expect(valuetype);
            }
            else
            {
                // default(T)
                _il.EmitValueDefault(this.Module, this.Diagnostics, this.GetTemporaryLocal(valuetype, true));
            }

            //
            return valuetype;
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
        /// Eventually emits branching to closing block.
        /// </summary>
        public void EmitRet(TypeSymbol stack, bool yielding = false)
        {
            // sequence point
            var body = AstUtils.BodySpanOrInvalid(Routine?.Syntax);
            if (body.IsValid && EmitPdbSequencePoints)
            {
                EmitSequencePoint(new Span(body.End - 1, 1));
            }

            //
            if (_il.InExceptionHandler || (ExtraFinallyBlock != null && !yielding))
            {
                this.ExitBlock.EmitTmpRet(this, stack, yielding);
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
            Debug.Assert(place.Type.IsReferenceType);

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

                if (t == CoreTypes.PhpValue)
                {
                    EmitPhpValueAddr();
                    t = EmitCall(ILOpCode.Call, CoreMethods.PhpValue.DeepCopy);
                }
                else if (t == CoreTypes.PhpString)
                {
                    Debug.Assert(t.IsStructType());
                    // Template: new PhpString( <STACK> )
                    t = EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpString_PhpString);
                }
                else if (t == CoreTypes.PhpArray)
                {
                    t = EmitCall(ILOpCode.Callvirt, CoreMethods.PhpArray.DeepCopy);
                }
                else
                {
                    throw this.NotImplementedException("copy " + t.Name);
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
                return EmitDeepCopy(t, thint.IsAnyType || this.TypeRefContext.IsNullOrVoid(thint));
            }
            else
            {
                return t;
            }
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
            il.EmitToken(module.Translate(symbol, syntaxNode, diagnostics, needDeclaration: false), syntaxNode, diagnostics);
        }

        public static void EmitSymbolToken(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, FieldSymbol symbol, SyntaxNode syntaxNode)
        {
            il.EmitToken(module.Translate(symbol, syntaxNode, diagnostics), syntaxNode, diagnostics);
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
        /// Gets addr of a default value. Used to call a method on default value.
        /// </summary>
        public static void EmitValueDefaultAddr(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, LocalDefinition tmp)
        {
            Debug.Assert(tmp.Type.IsValueType);
            il.EmitLocalAddress(tmp);
            il.EmitOpCode(ILOpCode.Initobj);
            il.EmitSymbolToken(module, diagnostics, (TypeSymbol)tmp.Type, null);
            // ldloca <loc>
            il.EmitLocalAddress(tmp);
        }

        /// <summary>
        /// Gets or create a local variable and returns it back to pool.
        /// </summary>
        public static LocalDefinition GetTemporaryLocalAndReturn(this ILBuilder il, TypeSymbol t)
        {
            var definition = il.LocalSlotManager.AllocateSlot((Microsoft.Cci.ITypeReference)t, LocalSlotConstraints.None);

            il.LocalSlotManager.FreeSlot(definition);

            return definition;
        }

        /// <summary>
        /// Copies a value type from the top of evaluation stack into a temporary variable and loads its address.
        /// </summary>
        public static void EmitStructAddr(this ILBuilder il, TypeSymbol t)
        {
            Debug.Assert(t.IsValueType);

            var tmp = GetTemporaryLocalAndReturn(il, t);
            il.EmitLocalStore(tmp);
            il.EmitLocalAddress(tmp);
        }

        /// <summary>
        /// Emits call to given method.
        /// </summary>
        /// <returns>Method return type.</returns>
        public static TypeSymbol EmitCall(this ILBuilder il, PEModuleBuilder module, DiagnosticBag diagnostics, ILOpCode code, MethodSymbol method)
        {
            Contract.ThrowIfNull(method);
            Debug.Assert(code == ILOpCode.Call || code == ILOpCode.Calli || code == ILOpCode.Callvirt || code == ILOpCode.Newobj);
            Debug.Assert(!method.IsErrorMethodOrNull());

            var stack = method.GetCallStackBehavior();

            if (code == ILOpCode.Newobj)
            {
                stack += 1 + 1;    // there is no <this>, + it pushes <newinst> on stack
            }

            if (code == ILOpCode.Callvirt && !method.IsAbstract && (!method.IsVirtual || method.IsSealed || method.ContainingType.IsSealed))
            {
                code = ILOpCode.Call; // virtual dispatch is unnecessary
            }

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

            var index = p.Ordinal;
            var hasthis = ((MethodSymbol)p.ContainingSymbol).HasThis ? 1 : 0;

            il.EmitLoadArgumentOpcode(index + hasthis);
            return p.Type;
        }

        public static TypeSymbol EmitLoad(this FieldSymbol f, CodeGenerator cg, IPlace holder = null)
        {
            Debug.Assert(f != null, nameof(f));

            if (!f.IsStatic)
            {
                // {holder}
                Debug.Assert(holder != null);
                VariableReferenceExtensions.EmitReceiver(cg.Builder, holder);
            }

            // .ldfld/.ldsfld {f}
            cg.Builder.EmitOpCode(f.IsStatic ? ILOpCode.Ldsfld : ILOpCode.Ldfld);
            cg.Builder.EmitToken(cg.Module.Translate(f, null, DiagnosticBag.GetInstance()), null, DiagnosticBag.GetInstance());

            //
            return f.Type;
        }
    }
}
