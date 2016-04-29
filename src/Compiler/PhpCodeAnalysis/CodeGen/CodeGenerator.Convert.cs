using Microsoft.CodeAnalysis;
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
        /// Copies <c>PhpNumber</c> into a temp variable and loads its address.
        /// </summary>
        private void EmitPhpNumberAddr() => EmitStructAddr(CoreTypes.PhpNumber);

        /// <summary>
        /// Copies <c>PhpValue</c> into a temp variable and loads its address.
        /// </summary>
        private void EmitPhpValueAddr() => EmitStructAddr(CoreTypes.PhpValue);

        /// <summary>
        /// Copies a value type from the top of evaluation stack into a temporary variable and loads its address.
        /// </summary>
        private void EmitStructAddr(NamedTypeSymbol t)
        {
            Debug.Assert(t.IsStructType());
            var tmp = GetTemporaryLocal(t, true);
            _il.EmitLocalStore(tmp);
            _il.EmitLocalAddress(tmp);
        }

        public void EmitConvertToBool(TypeSymbol from, TypeRefMask fromHint, bool negation = false)
        {
            // TODO: handle {negation} within the switch to avoid unnecessary conversions
            // TODO: use {fromHint} to emit casting in compile time

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            //
            from = EmitSpecialize(from, fromHint);

            //
            if (from.SpecialType != SpecialType.System_Boolean)
            {
                switch (from.SpecialType)
                {
                    case SpecialType.System_Int32:
                        break; // nop

                    case SpecialType.System_Int64:
                        _il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                        _il.EmitOpCode(ILOpCode.Conv_i8, 0);
                        _il.EmitOpCode(ILOpCode.Cgt_un);
                        // or ?
                        // _il.EmitOpCode(ILOpCode.Conv_i4);
                        break;

                    case SpecialType.None:
                        if (from == CoreTypes.PhpValue)
                        {
                            EmitPhpValueAddr();
                            EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToBoolean);
                            break;
                        }
                        else if (from == CoreTypes.PhpString)
                        {
                            EmitCall(ILOpCode.Call, CoreMethods.PhpString.ToBoolean);
                            break;
                        }
                        else if (from == CoreTypes.PhpNumber)
                        {
                            EmitPhpNumberAddr();
                            EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToBoolean);
                            break;
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }

                    default:
                        throw new NotImplementedException();
                }
            }

            // !<I4>
            if (negation)
            {
                EmitLogicNegation();
            }
        }

        public void EmitConvertToBool(BoundExpression expr, bool negation = false)
        {
            Contract.ThrowIfNull(expr);

            var place = PlaceOrNull(expr);
            var type = TryEmitVariableSpecialize(place, expr.TypeRefMask);
            if (type != null)
            {
                EmitConvertToBool(type, 0, negation);
            }
            else
            {
                // avoiding of load of full value
                if (place != null && place.HasAddress)
                {
                    if (place.TypeOpt == CoreTypes.PhpNumber)
                    {
                        // < place >.ToBoolean()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToBoolean);
                        return;
                    }
                    else if (place.TypeOpt == CoreTypes.PhpValue)
                    {
                        // < place >.ToBoolean()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToBoolean);
                        return;
                    }
                }

                //
                EmitConvertToBool(Emit(expr), expr.TypeRefMask, negation);
            }
        }

        public void EmitConvertToPhpValue(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            switch (from.SpecialType)
            {
                case SpecialType.System_Boolean:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Boolean);
                    break;
                case SpecialType.System_Int32:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // Int32 -> Int64
                    goto case SpecialType.System_Int64; // PhpValue.Create((long)<stack>)
                case SpecialType.System_Int64:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Long);
                    break;
                case SpecialType.System_Double:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_Double);
                    break;
                default:
                    if (from == CoreTypes.PhpAlias)
                    {
                        Emit_PhpAlias_GetValue();
                        return;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        // nop
                        break;
                    }
                    else if (from == CoreTypes.String)
                    {
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_String).Expect(CoreTypes.PhpValue);
                        break;
                    }
                    else if (from == CoreTypes.PhpNumber)
                    {
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_PhpNumber).Expect(CoreTypes.PhpValue);
                        break;
                    }
                    else if (from  == CoreTypes.PhpArray)
                    {
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Create_PhpArray).Expect(CoreTypes.PhpValue);
                        break;
                    }
                    else if (from.IsReferenceType)
                    {
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.FromClass_Object).Expect(CoreTypes.PhpValue);
                        break;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        public void EmitConvertToPhpNumber(TypeSymbol from, TypeRefMask fromHint)
        {
            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            switch (from.SpecialType)
            {
                case SpecialType.System_Int32:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // Int32 -> Int64
                    goto case SpecialType.System_Int64; // PhpValue.Create((long)<stack>)
                case SpecialType.System_Int64:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.Create_Long);
                    break;
                case SpecialType.System_Double:
                    EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.Create_Double);
                    break;
                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        // nop
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        public void EmitConvertToLong(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            //
            from = EmitSpecialize(from, fromHint);

            switch (from.SpecialType)
            {
                case SpecialType.System_Int32:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // Int32 -> Int64
                    return;

                case SpecialType.System_Int64:
                    // nop
                    return;

                case SpecialType.System_Double:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // double -> int64
                    break;

                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        EmitPhpNumberAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToLong);
                        return;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        EmitPhpValueAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToLong);
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        public NamedTypeSymbol EmitConvertToDouble(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            from = EmitSpecialize(from, fromHint);
            var dtype = CoreTypes.Double.Symbol;

            switch (from.SpecialType)
            {
                case SpecialType.System_Int32:
                    _il.EmitOpCode(ILOpCode.Conv_r8);   // Int32 -> Double
                    return dtype;
                case SpecialType.System_Int64:
                    _il.EmitOpCode(ILOpCode.Conv_r8);   // Int64 -> Double
                    return dtype;
                case SpecialType.System_Double:
                    // nop
                    return dtype;
                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        EmitPhpNumberAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToDouble);
                        return dtype;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        EmitPhpValueAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToDouble);
                        return dtype;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        /// <summary>
        /// Emits conversion to <see cref="System.String"/>.
        /// </summary>
        public void EmitConvertToString(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            from = EmitSpecialize(from, fromHint);

            //
            switch (from.SpecialType)
            {
                case SpecialType.System_String:
                    // nop
                    break;
                case SpecialType.System_Boolean:
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Bool);
                    break;
                case SpecialType.System_Int32:
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Int32);
                    break;
                case SpecialType.System_Int64:
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Long);
                    break;
                case SpecialType.System_Double:
                    EmitLoadContext();
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToString_Double_Context);
                    break;
                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        EmitPhpNumberAddr(); // PhpNumber -> PhpNumber addr
                        EmitLoadContext();  // Context
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToString_Context)
                            .Expect(SpecialType.System_String);
                        break;
                    }
                    else if (from == CoreTypes.PhpString)
                    {
                        EmitLoadContext();  // Context
                        EmitCall(ILOpCode.Call, CoreMethods.PhpString.ToString_Context)
                            .Expect(SpecialType.System_String);
                        break;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        EmitPhpValueAddr(); // PhpValue -> PhpValue addr
                        EmitLoadContext();  // Context
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToString_Context)
                            .Expect(SpecialType.System_String);
                        break;
                    }
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Emits conversion to <c>PhpArray</c>.
        /// </summary>
        public void EmitConvertToPhpArray(TypeSymbol from, TypeRefMask fromHint)
        {
            if (from == CoreTypes.PhpArray)
                return;

            throw new NotImplementedException($"(array){from.Name}");
        }

        public void EmitAsPhpArray(BoundExpression expr)
        {
            expr.Access = expr.Access.WithRead(CoreTypes.PhpArray);

            var type = Emit(expr);

            switch (type.SpecialType)
            {
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Double:
                case SpecialType.System_Boolean:
                    throw new InvalidOperationException();
                case SpecialType.System_String:
                    throw new NotImplementedException();    // StringArray helper
                default:
                    var diag = new HashSet<DiagnosticInfo>();
                    if (type.IsEqualToOrDerivedFrom(CoreTypes.PhpArray, false, ref diag))
                    {
                        return;
                    }
                    else if (type == CoreTypes.PhpAlias)
                    {
                        // <PhpAlias>.Value.AsArray()
                        _il.EmitOpCode(ILOpCode.Ldflda);
                        EmitSymbolToken(CoreMethods.PhpAlias.Value, null);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.AsArray).Expect(CoreTypes.PhpArray);
                        return;
                    }
                    else if (type == CoreTypes.PhpValue)
                    {
                        // <PhpValue>.AsArray()
                        EmitPhpValueAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.AsArray).Expect(CoreTypes.PhpArray);
                        return;
                    }
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Emits conversion to a class object.
        /// </summary>
        /// <param name="from">Type of value on top of the evaluation stack.</param>
        /// <param name="fromHint">Hint in case of multitype value.</param>
        /// <param name="to">Target type.</param>
        private void EmitConvertToClass(TypeSymbol from, TypeRefMask fromHint, TypeSymbol to)
        {
            Contract.ThrowIfNull(from);
            Contract.ThrowIfNull(to);
            Debug.Assert(to.IsReferenceType);   // TODO: structs other than primitive types
            Debug.Assert(to != CoreTypes.PhpAlias);

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            if (from == to)
                return;

            Debug.Assert(to != CoreTypes.PhpArray && to != CoreTypes.PhpString && to != CoreTypes.PhpAlias);

            switch (from.SpecialType)
            {
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Boolean:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    if (to == CoreTypes.Object)
                    {
                        // TODO: new stdClass(){ $scalar = VALUE }
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new ArgumentException();  // TODO: ErrorCode
                    }
                default:
                    if (from == CoreTypes.PhpValue)
                    {
                        // Object
                        EmitPhpValueAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToClass)
                            .Expect(SpecialType.System_Object);

                        // (T)
                        EmitCastClass(to);
                        return;
                    }
                    if (from == CoreTypes.PhpNumber)
                    {
                        // Object
                        EmitPhpNumberAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToClass)
                            .Expect(SpecialType.System_Object);

                        // (T)
                        EmitCastClass(to);
                        return;
                    }
                    else if (from == CoreTypes.PhpArray)
                    {
                        // (T)PhpArray.ToClass();
                        EmitCastClass(EmitCall(ILOpCode.Call, CoreMethods.PhpArray.ToClass), to);
                        return;
                    }
                    else if (from.IsReferenceType)
                    {
                        Debug.Assert(from != CoreTypes.PhpAlias);
                        // (T)obj   // let .NET deal with eventual cast error for now
                        EmitCastClass(from, to);
                        return;
                    }
                    throw new NotImplementedException();
            }
        }

        public void EmitConvert(BoundExpression expr, TypeSymbol to)
        {
            // bind target expression type
            expr.Access = expr.Access.WithRead(to);

            // loads value from place most effectively without runtime type checking
            var place = PlaceOrNull(expr);
            var type = TryEmitVariableSpecialize(place, expr.TypeRefMask);
            if (type != null)
            {
                EmitConvert(type, 0, to);
                return;
            }

            // avoiding of load of full value
            if (place != null && place.HasAddress)
            {
                if (place.TypeOpt == CoreTypes.PhpNumber)
                {
                    if (to.SpecialType == SpecialType.System_Int64)
                    {
                        // <place>.ToLong()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToLong);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_Double)
                    {
                        // <place>.ToDouble()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToDouble);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_Boolean)
                    {
                        // <place>.ToBoolean()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToBoolean);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_String)
                    {
                        // <place>.ToString(<ctx>)
                        place.EmitLoadAddress(_il);
                        EmitLoadContext();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToString_Context);
                        return;
                    }
                    if (to == CoreTypes.PhpValue)
                    {
                        // TODO
                    }

                    // TODO: Object, Array
                }
                else if (place.TypeOpt == CoreTypes.PhpValue)
                {
                    if (to.SpecialType == SpecialType.System_Int64)
                    {
                        // <place>.ToLong()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToLong);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_Double)
                    {
                        // <place>.ToDouble()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToDouble);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_Boolean)
                    {
                        // <place>.ToBoolean()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToBoolean);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_String)
                    {
                        // <place>.ToString(<ctx>)
                        place.EmitLoadAddress(_il);
                        EmitLoadContext();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToString_Context);
                        return;
                    }
                    if (to.SpecialType == SpecialType.System_Object)
                    {
                        // <place>.ToClass()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToClass);
                        return;
                    }
                    //if (to == CoreTypes.PhpArray)
                    //{
                    //    // <place>.AsArray()
                    //    place.EmitLoadAddress(_il);
                    //    EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToArray);
                    //    return;
                    //}
                }
                else if (place.TypeOpt == CoreTypes.Long)
                {
                    if (to.SpecialType == SpecialType.System_String)
                    {
                        // <place>.ToString()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.Operators.Long_ToString);
                        return;
                    }
                }
            }

            // literals
            if (expr.ConstantValue.HasValue && to != null)    // <= (expr is BoundLiteral)
            {
                //EmitLiteral(expr.ConstantValue.Value, to);
            }

            //
            EmitConvert(expr.Emit(this), expr.TypeRefMask, to);
        }

        //public TypeSymbol EmitLiteral(object value, TypeSymbol astype)
        //{
        //    Contract.ThrowIfNull(astype);

        //    if (value == null)
        //    {
        //        EmitLoadDefaultValue(astype, 0);
        //    }
        //    else
        //    {
        //        // TODO
        //    }
        //}

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

            // conversion is not needed:
            if (to == from ||
               (from.SpecialType == to.SpecialType && from.SpecialType != SpecialType.None))
                return;

            //
            from = EmitSpecialize(from, fromHint);

            // specialized conversions:
            switch (to.SpecialType)
            {
                case SpecialType.System_Void:
                    EmitPop(from);
                    return;
                case SpecialType.System_Boolean:
                    EmitConvertToBool(from, fromHint);
                    return;
                case SpecialType.System_Int64:
                    EmitConvertToLong(from, fromHint);
                    return;
                case SpecialType.System_Double:
                    EmitConvertToDouble(from, fromHint);
                    return;
                case SpecialType.System_String:
                    EmitConvertToString(from, fromHint);
                    return;
                case SpecialType.System_Object:
                    EmitConvertToClass(from, fromHint, to);
                    return;
                default:
                    if (to == CoreTypes.PhpValue)
                    {
                        EmitConvertToPhpValue(from, fromHint);
                    }
                    else if (to == CoreTypes.PhpAlias)
                    {
                        EmitConvertToPhpValue(from, fromHint);
                        Emit_PhpValue_MakeAlias();
                    }
                    else if (to == CoreTypes.PhpNumber)
                    {
                        EmitConvertToPhpNumber(from, fromHint);
                        return;
                    }
                    else if (to == CoreTypes.PhpArray)
                    {
                        EmitConvertToPhpArray(from, fromHint);
                        return;
                    }
                    else if (to.IsReferenceType)
                    {
                        EmitConvertToClass(from, fromHint, to);
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    return;
            }

            //
            throw new NotImplementedException();
        }
    }
}
