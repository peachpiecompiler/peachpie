using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
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
        /// Copies <c>PhpNumber</c> into a temp variable and loads its address.
        /// </summary>
        internal void EmitPhpNumberAddr() => EmitStructAddr(CoreTypes.PhpNumber);

        /// <summary>
        /// Copies <c>PhpString</c> into a temp variable and loads its address.
        /// </summary>
        internal void EmitPhpStringAddr() => EmitStructAddr(CoreTypes.PhpString);

        /// <summary>
        /// Copies <c>PhpValue</c> into a temp variable and loads its address.
        /// </summary>
        internal void EmitPhpValueAddr() => EmitStructAddr(CoreTypes.PhpValue);

        /// <summary>
        /// Copies a value type from the top of evaluation stack into a temporary variable and loads its address.
        /// </summary>
        internal void EmitStructAddr(TypeSymbol t)
        {
            Debug.Assert(t.IsStructType());
            var tmp = GetTemporaryLocal(t, true);
            _il.EmitLocalStore(tmp);
            _il.EmitLocalAddress(tmp);
        }

        public void EmitConvertToBool(TypeSymbol from, TypeRefMask fromHint)
        {
            this.EmitImplicitConversion(from, CoreTypes.Boolean);
        }

        public void EmitConvertToBool(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);

            expr.Access = expr.Access.WithRead(CoreTypes.Boolean);

            var place = PlaceOrNull(expr);
            var type = TryEmitVariableSpecialize(place, expr.TypeRefMask);
            if (type != null)
            {
                EmitConvertToBool(type, 0);
            }
            else
            {
                EmitConvertToBool(Emit(expr), expr.TypeRefMask);
            }
        }

        public TypeSymbol EmitConvertToPhpValue(BoundExpression expr)
        {
            if (expr == null || expr.ConstantValue.IsNull())
            {
                return Emit_PhpValue_Null();
            }
            else if (expr.ConstantValue.IsBool(out var b))
            {
                return b ? Emit_PhpValue_True() : Emit_PhpValue_False();
            }
            else
            {
                return EmitConvertToPhpValue(Emit(expr), expr.TypeRefMask);
            }
        }

        public TypeSymbol EmitConvertToPhpValue(TypeSymbol from, TypeRefMask fromHint)
        {
            // Nullable<T> -> HasValue ? T : NULL
            if (from.IsNullableType())
            {
                from = EmitNullableCastToNull(from, false); // (HasValue ? Value : NULL)
            }

            var conv = DeclaringCompilation.ClassifyCommonConversion(from, CoreTypes.PhpValue.Symbol);
            if (conv.IsImplicit)
            {
                this.EmitConversion(conv, from, CoreTypes.PhpValue.Symbol);
            }
            else
            {
                // some conversion we did not implement as operator yet:

                if (from.IsReferenceType)
                {
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.FromClass_Object)
                        .Expect(CoreTypes.PhpValue);
                }
                else if (from.SpecialType == SpecialType.System_Void)
                {
                    // PhpValue.Void
                    Emit_PhpValue_Void();
                }
                else
                {
                    throw ExceptionUtilities.NotImplementedException(this, $"{from.Name} -> PhpValue");
                }
            }
            //
            return CoreTypes.PhpValue;
        }

        public void EmitConvertToPhpNumber(TypeSymbol from, TypeRefMask fromHint)
        {
            if (from != CoreTypes.PhpNumber)
            {
                from = EmitSpecialize(from, fromHint);
            }

            this.EmitImplicitConversion(from, CoreTypes.PhpNumber);
        }

        /// <summary>
        /// In case there is <c>Int32</c> or <c>bool</c> on the top of evaluation stack,
        /// converts it to <c>Int64</c>.
        /// </summary>
        /// <param name="stack">New type on top of stack.</param>
        /// <returns></returns>
        internal TypeSymbol EmitConvertIntToLong(TypeSymbol stack)
        {
            if (stack.SpecialType == SpecialType.System_Boolean ||
                stack.SpecialType == SpecialType.System_Byte ||
                stack.SpecialType == SpecialType.System_Int16 ||
                stack.SpecialType == SpecialType.System_UInt16 ||
                stack.SpecialType == SpecialType.System_Int32 ||
                stack.SpecialType == SpecialType.System_UInt32)
            {
                var int64 = CoreTypes.Long.Symbol;
                this.EmitImplicitConversion(stack, int64);
                stack = int64;
            }

            return stack;
        }

        internal TypeSymbol EmitPhpAliasDereference(ref TypeSymbol stack)
        {
            if (stack == CoreTypes.PhpAlias)
            {
                stack = Emit_PhpAlias_GetValue();
            }

            return stack;
        }

        /// <summary>
        /// In case there is <c>string</c> or <c>PhpString</c> on the top of evaluation stack,
        /// converts it to <c>PhpNumber</c>.
        /// </summary>
        /// <returns>New type on top of stack.</returns>
        internal TypeSymbol EmitConvertStringToPhpNumber(TypeSymbol stack)
        {
            if (stack.SpecialType == SpecialType.System_String ||
                stack == CoreTypes.PhpString)
            {
                this.EmitImplicitConversion(stack, CoreTypes.PhpNumber);
                return CoreTypes.PhpNumber;
            }

            return stack;
        }

        /// <summary>
        /// In case there is <c>Int32</c> or <c>bool</c> or <c>PhpNumber</c> on the top of evaluation stack,
        /// converts it to <c>double</c>.
        /// </summary>
        internal TypeSymbol EmitConvertNumberToDouble(BoundExpression expr)
        {
            // emit number literal directly as double
            var constant = expr.ConstantValue;
            if (constant.HasValue)
            {
                if (constant.Value is long)
                {
                    _il.EmitDoubleConstant((long)constant.Value);
                    return this.CoreTypes.Double;
                }
                if (constant.Value is int)
                {
                    _il.EmitDoubleConstant((int)constant.Value);
                    return this.CoreTypes.Double;
                }
                if (constant.Value is bool)
                {
                    _il.EmitDoubleConstant((bool)constant.Value ? 1.0 : 0.0);
                    return this.CoreTypes.Double;
                }
            }

            // emit fast ToDouble() in case of a PhpNumber variable
            var place = PlaceOrNull(expr);
            var type = TryEmitVariableSpecialize(place, expr.TypeRefMask);
            if (type == null)
            {
                if (place != null && place.HasAddress)
                {
                    if (place.Type == CoreTypes.PhpNumber)
                    {
                        place.EmitLoadAddress(_il);
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToDouble)
                            .Expect(SpecialType.System_Double);
                    }
                }

                type = EmitSpecialize(expr);
            }

            Debug.Assert(type != null);

            if (type.SpecialType == SpecialType.System_Int32 ||
                type.SpecialType == SpecialType.System_Int64 ||
                type.SpecialType == SpecialType.System_Boolean)
            {
                _il.EmitOpCode(ILOpCode.Conv_r8);    // int|bool -> long
                type = this.CoreTypes.Double;
            }
            else if (type == CoreTypes.PhpNumber)
            {
                EmitPhpNumberAddr();
                EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToDouble);    // number -> double
                type = this.CoreTypes.Double;
            }

            //
            return type;
        }

        /// <summary>
        /// Emits conversion to <see cref="System.String"/>.
        /// </summary>
        public void EmitConvertToString(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            from = EmitSpecialize(from, fromHint);

            this.EmitImplicitConversion(from, CoreTypes.String);
        }

        /// <summary>
        /// Emits conversion to <c>PhpString</c> (aka writable string).
        /// </summary>
        public void EmitConvertToPhpString(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            from = EmitSpecialize(from, fromHint);

            var conv = this.DeclaringCompilation.ClassifyCommonConversion(from, this.CoreTypes.PhpString.Symbol);
            if (conv.IsImplicit == false)
            {
                if (from.SpecialType == SpecialType.System_Void)
                {
                    // Template: new PhpString("")
                    _il.EmitStringConstant(string.Empty);
                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpString_string);
                }
                else
                {
                    // new PhpString(string)
                    EmitConvertToString(from, fromHint);
                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpString_string);
                }
            }
            else
            {
                this.EmitConversion(conv, from, CoreTypes.PhpString.Symbol);
            }
        }

        /// <summary>
        /// Emits conversion to <c>PhpArray</c>.
        /// Anyting else than <c>NULL</c> and <c>array</c> causes an exception of type <see cref="InvalidCastException"/> in runtime.
        /// </summary>
        public TypeSymbol EmitConvertToPhpArray(TypeSymbol from, TypeRefMask fromHint)
        {
            if (from.IsOfType(CoreTypes.PhpArray))
            {
                return from;
            }

            if (from == CoreTypes.PhpAlias)
            {
                // Template: <PhpAlias>.Value.GetArray()
                this.Emit_PhpAlias_GetValueAddr();
                return this.EmitCall(ILOpCode.Call, CoreMethods.PhpValue.GetArray);
            }

            if ((from.SpecialType != SpecialType.None && from.SpecialType != SpecialType.System_Object) ||
                (from.IsValueType && from != CoreTypes.PhpValue) ||
                from.IsOfType(CoreTypes.PhpResource))
            {
                // EXCEPTION:
                // TODO: diagnostics
                return EmitCastClass(from, CoreTypes.PhpArray);
            }
            else if (from.IsReferenceType)
            {
                // Template: (PhpArray)<STACK>
                return EmitCastClass(from, CoreTypes.PhpArray);
            }
            else
            {
                // Template: ((PhpValue)<from>).GetArray()
                EmitConvert(from, 0, CoreTypes.PhpValue);
                EmitPhpValueAddr();
                return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.GetArray);
            }
        }

        /// <summary>
        /// Emits conversion "as object" keeping a reference type on stack or <c>null</c>.
        /// </summary>
        public TypeSymbol EmitAsObject(TypeSymbol from)
        {
            return EmitAsObject(from, out bool isnull);
        }

        internal TypeSymbol EmitAsObject(TypeSymbol from, out bool isnull)
        {
            isnull = false;

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                // <alias>.Value.AsObject()
                Emit_PhpAlias_GetValueAddr();
                return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.AsObject);
            }

            // PhpValue -> object
            if (from == CoreTypes.PhpValue)
            {
                // Template: Operators.AsObject(value)
                return EmitCall(ILOpCode.Call, CoreMethods.Operators.AsObject_PhpValue);
            }

            if (!from.IsReferenceType ||
                from == CoreTypes.PhpArray ||
                from.IsOfType(CoreTypes.PhpResource) ||
                from == CoreTypes.PhpString ||
                from.SpecialType == SpecialType.System_String)
            {
                EmitPop(from);
                _il.EmitNullConstant();
                isnull = true;
                return CoreTypes.Object;
            }
            else
            {
                return from;
            }
        }

        private void EmitConvertToIPhpCallable(TypeSymbol from, TypeRefMask fromHint)
        {
            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                from = Emit_PhpAlias_GetValue();
            }

            // (IPhpCallable)
            if (!from.IsEqualToOrDerivedFrom(CoreTypes.IPhpCallable))
            {
                if (from.SpecialType == SpecialType.System_String)
                {
                    EmitLoadToken(this.CallerType, null);
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.AsCallable_String_RuntimeTypeHandle);
                }
                else if (
                    from.SpecialType == SpecialType.System_Int64 ||
                    from.SpecialType == SpecialType.System_Boolean ||
                    from.SpecialType == SpecialType.System_Double)
                {
                    throw new ArgumentException($"{from.Name} cannot be converted to a class of type IPhpCallable!");  // TODO: ErrCode
                }
                else
                {
                    EmitConvertToPhpValue(from, fromHint);
                    EmitLoadToken(this.CallerType, null);
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.AsCallable_PhpValue_RuntimeTypeHandle);
                }
            }
        }

        /// <summary>
        /// Emits conversion to an object of given type.
        /// </summary>
        /// <param name="from">Type of value on top of the evaluation stack.</param>
        /// <param name="fromHint">Hint in case of multitype value.</param>
        /// <param name="to">Target type.</param>
        private void EmitConvertToClass(TypeSymbol from, TypeRefMask fromHint, TypeSymbol to)
        {
            Contract.ThrowIfNull(from);
            Contract.ThrowIfNull(to);
            Debug.Assert(to.IsReferenceType);
            Debug.Assert(to != CoreTypes.PhpAlias);
            Debug.Assert(!to.IsErrorType(), "Trying to convert to an ErrorType");

            // -> IPhpCallable
            if (to == CoreTypes.IPhpCallable)
            {
                EmitConvertToIPhpCallable(from, fromHint);
                return;
            }

            // -> System.Array
            if (to.IsArray())
            {
                var arrt = (ArrayTypeSymbol)to;
                if (arrt.IsSZArray)
                {
                    // byte[]
                    if (arrt.ElementType.SpecialType == SpecialType.System_Byte)
                    {
                        // Template: (PhpString).ToBytes(Context)
                        EmitConvertToPhpString(from, fromHint); // PhpString
                        EmitPhpStringAddr();
                        this.EmitLoadContext();                 // Context
                        EmitCall(ILOpCode.Call, CoreMethods.PhpString.ToBytes_Context)
                            .Expect(to);  // ToBytes()
                        return;
                    }

                    throw this.NotImplementedException($"Conversion from {from.Name} to {arrt.ElementType.Name}[] is not implemented.");
                }

                throw this.NotImplementedException($"Conversion from {from.Name} to array {to.Name} is not implemented.");
            }

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                // <alias>.Value.AsObject() : object
                Emit_PhpAlias_GetValueAddr();
                from = EmitCall(ILOpCode.Call, CoreMethods.PhpValue.AsObject)
                    .Expect(SpecialType.System_Object);
            }

            if (from.IsReferenceType && from.IsOfType(to))
            {
                return;
            }

            Debug.Assert(to != CoreTypes.PhpArray && to != CoreTypes.PhpString && to != CoreTypes.PhpAlias);

            switch (from.SpecialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Boolean:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    // Template: null
                    EmitPop(from);
                    _il.EmitNullConstant();
                    return;

                default:

                    Debug.Assert(from != CoreTypes.PhpAlias);

                    if (from.IsValueType)
                    {
                        if (from == CoreTypes.PhpValue)
                        {
                            if (IsClassOnly(fromHint))
                            {
                                // <STACK>.Object
                                EmitPhpValueAddr();
                                from = EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Object.Getter)
                                    .Expect(SpecialType.System_Object);
                            }
                            else
                            {
                                // Convert.AsObject( <STACK> )
                                from = EmitCall(ILOpCode.Call, CoreMethods.Operators.AsObject_PhpValue)
                                    .Expect(SpecialType.System_Object);
                            }
                        }
                        else
                        {
                            // null
                            EmitPop(from);
                            _il.EmitNullConstant();
                            return;
                        }
                    }

                    //
                    break;
            }

            // Template: (T)object
            EmitCastClass(from, to);
        }

        public void EmitConvert(BoundExpression expr, TypeSymbol to)
        {
            Debug.Assert(expr != null);
            Debug.Assert(to != null);

            // pop effectively
            if (to.IsVoid())
            {
                expr.Access = BoundAccess.None;

                if (!expr.IsConstant() && !IsDebug)
                {
                    // POP LOAD <expr>
                    EmitPop(Emit(expr));
                }

                return;
            }

            // bind target expression type
            expr.Access = expr.Access.WithRead(to);

            if (!expr.Access.IsReadRef)
            {
                // constants
                if (expr.ConstantValue.HasValue && to != null)
                {
                    EmitConvert(EmitLoadConstant(expr.ConstantValue.Value, to), 0, to);
                    return;
                }

                // loads value from place most effectively without runtime type checking
                var place = PlaceOrNull(expr);
                if (place != null && place.Type != to)
                {
                    var type = TryEmitVariableSpecialize(place, expr.TypeRefMask);
                    if (type != null)
                    {
                        EmitConvert(type, 0, to);
                        return;
                    }
                }

                // avoiding of load of full value
                if (place != null && place.HasAddress && place.Type != null && place.Type.IsValueType)
                {
                    var conv = DeclaringCompilation.ClassifyCommonConversion(place.Type, to);
                    if (conv.Exists && conv.IsUserDefined && !conv.MethodSymbol.IsStatic)
                    {
                        // (ADDR expr).Method()
                        this.EmitImplicitConversion(EmitCall(ILOpCode.Call, (MethodSymbol)conv.MethodSymbol, expr, ImmutableArray<BoundArgument>.Empty), to, @checked: true);
                        return;
                    }
                }
            }

            //
            EmitConvert(expr.Emit(this), expr.TypeRefMask, to);
        }

        /// <summary>
        /// Emits conversion from one CLR type to another using PHP conventions.
        /// </summary>
        /// <param name="from">Type of value on top of evaluation stack.</param>
        /// <param name="fromHint">Type hint in case of a multityple type choices (like PhpValue or PhpNumber or PhpAlias).</param>
        /// <param name="to">Target CLR type.</param>
        public void EmitConvert(TypeSymbol from, TypeRefMask fromHint, TypeSymbol to)
        {
            Contract.ThrowIfNull(from);
            Contract.ThrowIfNull(to);

            Debug.Assert(!from.IsUnreachable);
            Debug.Assert(!to.IsUnreachable);
            Debug.Assert(!to.IsErrorType(), "Conversion to an error type.");

            // conversion is not needed:
            if (from.SpecialType == to.SpecialType &&
                (from == to || (to.SpecialType != SpecialType.System_Object && from.IsOfType(to))))
            {
                return;
            }

            //
            from = EmitSpecialize(from, fromHint);

            if (!this.TryEmitImplicitConversion(from, to))
            {
                // specialized conversions:
                if (to == CoreTypes.PhpValue)
                {
                    EmitConvertToPhpValue(from, fromHint);
                }
                else if (to == CoreTypes.PhpString)
                {
                    // -> PhpString
                    EmitConvertToPhpString(from, fromHint);
                }
                else if (to == CoreTypes.PhpAlias)
                {
                    EmitConvertToPhpValue(from, fromHint);
                    Emit_PhpValue_MakeAlias();
                }
                else if (to.IsReferenceType)
                {
                    if (to == CoreTypes.PhpArray || to == CoreTypes.IPhpArray || to == CoreTypes.IPhpEnumerable || to == CoreTypes.PhpHashtable)
                    {
                        // -> PhpArray
                        // TODO: try unwrap "value.Object as T"
                        EmitConvertToPhpArray(from, fromHint);
                    }
                    else
                    {
                        // -> Object, PhpResource
                        EmitConvertToClass(from, fromHint, to);
                    }
                }
                else if (to.IsNullableType(out var ttype))
                {
                    // Template: new Nullable<T>( (T)from )
                    EmitConvert(from, fromHint, ttype);
                    EmitCall(ILOpCode.Newobj, ((NamedTypeSymbol)to).InstanceConstructors[0]);
                }
                else
                {
                    throw this.NotImplementedException($"Conversion from '{from}' to '{to}'.");
                }
            }
        }
    }
}
