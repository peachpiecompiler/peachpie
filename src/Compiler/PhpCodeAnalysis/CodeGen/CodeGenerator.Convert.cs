using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
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
            // TODO: use {fromHint} to emit casting in compile time

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                // <PhpAlias>.Value.ToBoolean()
                Emit_PhpAlias_GetValueRef();
                EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToBoolean);

                // !
                if (negation)
                {
                    EmitLogicNegation();
                }

                //
                return;
            }

            //
            from = EmitSpecialize(from, fromHint);

            //
            switch (from.SpecialType)
            {
                case SpecialType.System_Void:
                    _il.EmitBoolConstant(negation ? true : false);  // (bool)void == false
                    return;

                case SpecialType.System_Boolean:
                case SpecialType.System_Int32:
                    break; // nop

                case SpecialType.System_Int64:
                    _il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                    _il.EmitOpCode(ILOpCode.Conv_i8, 0);
                    _il.EmitOpCode(negation ? ILOpCode.Ceq : ILOpCode.Cgt_un);
                    return;

                case SpecialType.System_Double:

                    // r8 == 0.0
                    _il.EmitDoubleConstant(0.0);
                    _il.EmitOpCode(ILOpCode.Ceq);

                    if (!negation)
                    {
                        // !<i4>
                        EmitLogicNegation();
                    }

                    return;

                case SpecialType.System_String:
                    // Convert.ToBoolean(string)
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToBoolean_String);
                    break;

                case SpecialType.System_Object:
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToBoolean_Object);
                    break;

                case SpecialType.None:
                    if (from == CoreTypes.PhpValue)
                    {
                        // Convert.ToBoolean(value)
                        EmitCall(ILOpCode.Call, CoreMethods.Operators.ToBoolean_PhpValue);
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
                    else if (from == CoreTypes.PhpArray)
                    {
                        EmitCall(ILOpCode.Callvirt, CoreMethods.PhpArray.ToBoolean);
                        break;
                    }
                    else if (from.IsReferenceType)
                    {
                        goto case SpecialType.System_Object;
                    }

                    goto default;

                default:
                    throw new NotImplementedException($"(bool){from.Name}");
            }

            // !<i4>
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

        public TypeSymbol EmitConvertToPhpValue(TypeSymbol from, TypeRefMask fromHint)
        {
            return EmitConvertToPhpValue(from, fromHint, _il, _moduleBuilder, _diagnostics);
        }

        public static TypeSymbol EmitConvertToPhpValue(TypeSymbol from, TypeRefMask fromHint, ILBuilder il, Emit.PEModuleBuilder module, DiagnosticBag diagnostic)
        {
            Contract.ThrowIfNull(from);

            var compilation = module.Compilation;

            switch (from.SpecialType)
            {
                case SpecialType.System_Boolean:
                    il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_Boolean);
                    break;
                case SpecialType.System_Int32:
                    il.EmitOpCode(ILOpCode.Conv_i8);   // Int32 -> Int64
                    goto case SpecialType.System_Int64; // PhpValue.Create((long)<stack>)
                case SpecialType.System_Int64:
                    il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_Long);
                    break;
                case SpecialType.System_Double:
                    il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_Double);
                    break;
                case SpecialType.System_Void:
                    Emit_PhpValue_Void(il, module, diagnostic);
                    break;
                default:
                    if (from == compilation.CoreTypes.PhpAlias)
                    {
                        il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_PhpAlias)
                            .Expect(compilation.CoreTypes.PhpValue);
                        break;
                    }
                    else if (from == compilation.CoreTypes.PhpValue)
                    {
                        // nop
                        break;
                    }
                    else if (from == compilation.CoreTypes.String)
                    {
                        il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_String)
                            .Expect(compilation.CoreTypes.PhpValue);
                        break;
                    }
                    else if (from == compilation.CoreTypes.PhpString)
                    {
                        il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_PhpString)
                            .Expect(compilation.CoreTypes.PhpValue);
                        break;
                    }
                    else if (from == compilation.CoreTypes.PhpNumber)
                    {
                        il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_PhpNumber)
                            .Expect(compilation.CoreTypes.PhpValue);
                        break;
                    }
                    else if (from == compilation.CoreTypes.PhpArray)
                    {
                        il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_PhpArray)
                            .Expect(compilation.CoreTypes.PhpValue);
                        break;
                    }
                    else if (from == compilation.CoreTypes.IntStringKey)
                    {
                        il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_IntStringKey)
                            .Expect(compilation.CoreTypes.PhpValue);
                        break;
                    }
                    else if (from.IsReferenceType)
                    {
                        il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.FromClass_Object)
                            .Expect(compilation.CoreTypes.PhpValue);
                        break;
                    }
                    else
                    {
                        throw new NotImplementedException($"{from.Name}");
                    }
            }

            //
            return compilation.CoreTypes.PhpValue;
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

            if (from == CoreTypes.PhpValue)
            {
                EmitCall(ILOpCode.Call, CoreMethods.Operators.AsArray_PhpValue);
            }
            else
            {
                throw new NotImplementedException($"(array){from.Name}");
            }
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
                        // Convert.AsArray(value)
                        EmitCall(ILOpCode.Call, CoreMethods.Operators.AsArray_PhpValue).Expect(CoreTypes.PhpArray);
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
                case SpecialType.System_Void:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Boolean:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    if (to == CoreTypes.Object)
                    {
                        from = EmitConvertToPhpValue(from, fromHint);
                        goto default;
                    }
                    else
                    {
                        throw new ArgumentException($"{from.Name} cannot be converted to a class of type {to.Name}!");  // TODO: ErrCode
                    }
                default:
                    if (from == CoreTypes.PhpValue)
                    {
                        // Convert.ToClass( value )
                        EmitCall(ILOpCode.Call, CoreMethods.Operators.ToClass_PhpValue)
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
                EmitConvert(EmitLoadConstant(expr.ConstantValue.Value, to), 0, to);
                return;
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
