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
        internal void EmitPhpNumberAddr() => EmitStructAddr(CoreTypes.PhpNumber);

        /// <summary>
        /// Copies <c>PhpValue</c> into a temp variable and loads its address.
        /// </summary>
        internal void EmitPhpValueAddr() => EmitStructAddr(CoreTypes.PhpValue);

        /// <summary>
        /// Copies a value type from the top of evaluation stack into a temporary variable and loads its address.
        /// </summary>
        private void EmitStructAddr(TypeSymbol t)
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
                Emit_PhpAlias_GetValueAddr();
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
                        // (bool)value
                        EmitCall(ILOpCode.Call, CoreMethods.Operators.ToBoolean_PhpValue);
                        break;
                    }
                    else if (from == CoreTypes.PhpNumber)
                    {
                        EmitPhpNumberAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToBoolean);
                        break;
                    }
                    else if (from.IsOfType(CoreTypes.IPhpConvertible))
                    {
                        // (IPhpConvertible).ToBoolean()
                        if (CanBeNull(fromHint))
                        {
                            // Template: <value> != null && <value>.ToBoolean()
                            EmitCall(ILOpCode.Call, CoreMethods.Operators.ToBoolean_IPhpConvertible);
                        }
                        else
                        {
                            // Template: <value>.ToBoolean()
                            EmitCall(ILOpCode.Callvirt, CoreMethods.IPhpConvertible.ToBoolean)
                                .Expect(SpecialType.System_Boolean);
                        }
                        break;
                    }
                    //else if (from == CoreTypes.PhpString)
                    //{
                    //    EmitCall(ILOpCode.Call, CoreMethods.PhpString.ToBoolean);
                    //    break;
                    //}
                    //else if (from.IsOfType(CoreTypes.IPhpArray))
                    //{
                    //    // TODO: != null && .Count != 0
                    //    // IPhpArray.Count != 0
                    //    EmitCall(ILOpCode.Callvirt, CoreMethods.IPhpArray.get_Count);
                    //    _il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                    //    _il.EmitOpCode(negation ? ILOpCode.Ceq : ILOpCode.Cgt_un);
                    //    return; // negation handled
                    //}
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
                        if (negation) this.EmitLogicNegation();
                        return;
                    }
                    else if (place.TypeOpt == CoreTypes.PhpValue)
                    {
                        // < place >.ToBoolean()
                        place.EmitLoadAddress(_il);
                        EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToBoolean);
                        if (negation) this.EmitLogicNegation();
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
                case SpecialType.System_String:
                    il.EmitCall(module, diagnostic, ILOpCode.Call, compilation.CoreMethods.PhpValue.Create_String)
                        .Expect(compilation.CoreTypes.PhpValue);
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
                    else if (from.IsOfType(compilation.CoreTypes.PhpArray))
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

        public void EmitConvertToIntStringKey(TypeSymbol from, TypeRefMask fromHint)
        {
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            switch (from.SpecialType)
            {
                case SpecialType.System_Int64:
                    _il.EmitOpCode(ILOpCode.Conv_i4);   // i8 -> i4
                    goto case SpecialType.System_Int32;
                case SpecialType.System_Int32:
                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_int);
                    break;
                case SpecialType.System_String:
                    EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.IntStringKey_string);
                    break;
                default:
                    EmitConvertToPhpValue(from, 0);
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToIntStringKey_PhpValue);
                    break;
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
                    else if (from == CoreTypes.PhpValue)
                    {
                        EmitCall(ILOpCode.Call, CoreMethods.Operators.ToNumber_PhpValue);
                        break;
                    }
                    else
                    {
                        throw new NotImplementedException($"{from} -> PhpNumber");
                    }
            }
        }

        public void EmitConvertToInt(TypeSymbol from, TypeRefMask fromHint)
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
                    return;

                default:

                    if (from.IsOfType(CoreTypes.IPhpArray))
                    {
                        // IPhpArray.Count
                        EmitCall(ILOpCode.Callvirt, CoreMethods.IPhpArray.Count.Getter);
                    }
                    else
                    {
                        EmitConvertToLong(from, 0);
                        _il.EmitOpCode(ILOpCode.Conv_i4);   // Int64 -> Int32
                    }
                    return;
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
                case SpecialType.System_Boolean:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // bool -> Int64
                    return;

                case SpecialType.System_Int32:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // Int32 -> Int64
                    return;

                case SpecialType.System_Int64:
                    // nop
                    return;

                case SpecialType.System_Double:
                    _il.EmitOpCode(ILOpCode.Conv_i8);   // double -> int64
                    break;

                case SpecialType.System_String:
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToLong_String)
                        .Expect(SpecialType.System_Int64);
                    break;

                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        EmitPhpNumberAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToLong);
                        return;
                    }
                    else if (from.IsOfType(CoreTypes.IPhpArray))
                    {
                        // (long)IPhpArray.Count
                        EmitCall(ILOpCode.Callvirt, CoreMethods.IPhpArray.Count.Getter);
                        _il.EmitOpCode(ILOpCode.Conv_i8);   // Int32 -> Int64
                        return;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        EmitCall(ILOpCode.Call, CoreMethods.Operators.ToLong_PhpValue);
                        return;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        public TypeSymbol EmitConvertToDouble(TypeSymbol from, TypeRefMask fromHint)
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

                case SpecialType.System_Single:
                    _il.EmitOpCode(ILOpCode.Conv_r8);   // float -> Double
                    return dtype;

                case SpecialType.System_Double:
                    // nop
                    return dtype;

                case SpecialType.System_String:
                    return EmitCall(ILOpCode.Call, CoreMethods.Operators.ToDouble_String)
                        .Expect(SpecialType.System_Double);

                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        EmitPhpNumberAddr();
                        EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToDouble);
                        return dtype;
                    }
                    else if (from.IsOfType(CoreTypes.IPhpArray))
                    {
                        // (double)IPhpArray.Count
                        EmitCall(ILOpCode.Callvirt, CoreMethods.IPhpArray.Count.Getter);
                        _il.EmitOpCode(ILOpCode.Conv_r8);   // Int32 -> Double
                        return dtype;
                    }
                    else if (from == CoreTypes.PhpValue)
                    {
                        return EmitCall(ILOpCode.Call, CoreMethods.Operators.ToDouble_PhpValue);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        /// <summary>
        /// In case there is <c>Int32</c> or <c>bool</c> on the top of evaluation stack,
        /// converts it to <c>Int64</c>.
        /// </summary>
        /// <param name="stack">New type on top of stack.</param>
        /// <returns></returns>
        internal TypeSymbol EmitConvertIntToLong(TypeSymbol stack)
        {
            if (stack.SpecialType == SpecialType.System_Int32 ||
                stack.SpecialType == SpecialType.System_Boolean)
            {
                _il.EmitOpCode(ILOpCode.Conv_i8);    // int|bool -> long
                stack = this.CoreTypes.Long;
            }

            return stack;
        }

        /// <summary>
        /// In case there is <c>string</c> or <c>PhpString</c> on the top of evaluation stack,
        /// converts it to <c>PhpNumber</c>.
        /// </summary>
        /// <returns>New type on top of stack.</returns>
        internal TypeSymbol EmitConvertStringToNumber(TypeSymbol stack)
        {
            if (stack.SpecialType == SpecialType.System_String)
            {
                return EmitCall(ILOpCode.Call, CoreMethods.Operators.ToNumber_String)
                    .Expect(CoreTypes.PhpNumber);
            }

            if (stack == CoreTypes.PhpString)
            {
                return EmitCall(ILOpCode.Callvirt, CoreMethods.PhpString.ToNumber)
                    .Expect(CoreTypes.PhpNumber);
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
                    if (place.TypeOpt == CoreTypes.PhpNumber)
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
                case SpecialType.System_Void:
                    Builder.EmitStringConstant(string.Empty);
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
                    //else if (from.IsOfType(CoreTypes.IPhpConvertible))
                    //{
                    //    // Template: ((IPhpConvertible)STACK).ToStringOrThrow(ctx)
                    //    EmitCastClass(from, CoreTypes.IPhpConvertible);
                    //    EmitLoadContext();
                    //    EmitCall(ILOpCode.Callvirt, CoreMethods.IPhpConvertible.ToStringOrThrow_Context)
                    //        .Expect(SpecialType.System_String);
                    //    break;
                    //}
                    else if (from.IsReferenceType)
                    {
                        // Template: STACK.ToString()
                        EmitCall(ILOpCode.Callvirt, (MethodSymbol)DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Object__ToString))
                            .Expect(SpecialType.System_String);
                        break;
                    }
                    throw new NotImplementedException($"(string){from}");
            }
        }

        /// <summary>
        /// Emits conversion to <c>PhpString</c> (aka writable string).
        /// </summary>
        public void EmitConvertToPhpString(TypeSymbol from, TypeRefMask fromHint)
        {
            Contract.ThrowIfNull(from);

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                Emit_PhpAlias_GetValue();
                from = CoreTypes.PhpValue;
            }

            from = EmitSpecialize(from, fromHint);

            if (from == CoreTypes.PhpString)
            {
                return;
            }
            else if (from.SpecialType == SpecialType.System_Void)
            {
                EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpString);
            }
            else if (from == CoreTypes.PhpValue)
            {
                EmitLoadContext();  // Context
                EmitCall(ILOpCode.Call, CoreMethods.Operators.ToPhpString_PhpValue_Context)
                    .Expect(CoreTypes.PhpString);
            }
            else
            {
                // new PhpString(string)
                EmitConvertToString(from, fromHint);
                EmitCall(ILOpCode.Newobj, CoreMethods.Ctors.PhpString_string);
            }
        }

        /// <summary>
        /// Emits conversion to <c>PhpArray</c>.
        /// </summary>
        public TypeSymbol EmitConvertToPhpArray(TypeSymbol from, TypeRefMask fromHint)
        {
            if (from.IsOfType(CoreTypes.PhpArray))
            {
                return from;
            }
            else if (from == CoreTypes.PhpAlias)
            {
                // Template: <PhpAlias>.Value.ToArray()
                this.Emit_PhpAlias_GetValueAddr();
                return this.EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToArray);
            }
            else if (   // TODO: helper method for builtin types
                from.SpecialType != SpecialType.None ||
                from.IsOfType(CoreTypes.PhpResource) || from == CoreTypes.PhpNumber || from == CoreTypes.PhpString)
            {
                EmitConvertToPhpValue(from, fromHint);
                return EmitCall(ILOpCode.Call, CoreMethods.PhpArray.New_PhpValue);
            }
            else
            {
                // Template: ToArray((PhpValue)<from>)
                EmitConvert(from, 0, CoreTypes.PhpValue);
                return EmitCall(ILOpCode.Call, CoreMethods.Operators.ToArray_PhpValue);
            }
        }

        /// <summary>
        /// Emits conversion "As PhpArray", resulting in instance of <c>PhpArray</c> or <c>NULL</c> on stack.
        /// </summary>
        public TypeSymbol EmitAsPhpArray(TypeSymbol from)
        {
            if (from == CoreTypes.PhpAlias)
            {
                // <alias>.Value.Object
                Emit_PhpAlias_GetValueAddr();
                from = EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Object.Getter);
            }
            else if (from == CoreTypes.PhpValue)
            {
                // AsArray(<value>)
                return EmitCall(ILOpCode.Call, CoreMethods.Operators.AsArray_PhpValue);
            }

            //

            if (from.IsReferenceType)
            {
                // <stack> as PhpArray
                _il.EmitOpCode(ILOpCode.Isinst);
                EmitSymbolToken(CoreTypes.PhpArray, null);
            }
            else
            {
                EmitPop(from);
                _il.EmitNullConstant();
            }

            //

            return CoreTypes.PhpArray;
        }

        /// <summary>
        /// Emits conversion "as object" keeping a reference type on stack or <c>null</c>.
        /// </summary>
        public TypeSymbol EmitAsObject(TypeSymbol from)
        {
            bool isnull;
            return EmitAsObject(from, out isnull);
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
            Debug.Assert(to.IsReferenceType);   // TODO: structs other than primitive types
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
                        this.EmitLoadContext();                 // Context
                        EmitCall(ILOpCode.Call, CoreMethods.PhpString.ToBytes_Context)
                            .Expect(to);  // ToBytes()
                        return;
                    }

                    throw new NotImplementedException($"Conversion from {from.Name} to {arrt.ElementType.Name}[] is not implemented.");
                }

                throw new NotImplementedException($"Conversion from {from.Name} to array {to.Name} is not implemented.");
            }

            // dereference
            if (from == CoreTypes.PhpAlias)
            {
                // <alias>.Value.AsObject() : object
                Emit_PhpAlias_GetValueAddr();
                from = EmitCall(ILOpCode.Call, CoreMethods.PhpValue.AsObject)
                    .Expect(SpecialType.System_Object);
            }

            if (from == to)
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
                    if (from == CoreTypes.PhpValue)
                    {
                        if (IsClassOnly(fromHint))
                        {
                            // <value>.Object
                            EmitPhpValueAddr();
                            from = EmitCall(ILOpCode.Call, CoreMethods.PhpValue.Object.Getter)
                                .Expect(SpecialType.System_Object);
                        }
                        else
                        {
                            // Convert.ToClass( value )
                            from = EmitCall(ILOpCode.Call, CoreMethods.Operators.AsObject_PhpValue)
                                .Expect(SpecialType.System_Object);
                        }
                    }
                    else if (
                        from.IsOfType(CoreTypes.PhpArray) ||
                        from == CoreTypes.PhpString ||
                        from.IsValueType)
                    {
                        // null
                        EmitPop(from);
                        _il.EmitNullConstant();
                        return;
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
                if (place != null && place.TypeOpt != to)
                {
                    var type = TryEmitVariableSpecialize(place, expr.TypeRefMask);
                    if (type != null)
                    {
                        EmitConvert(type, 0, to);
                        return;
                    }
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

        void EmitConvertToEnum(TypeSymbol from, NamedTypeSymbol to)
        {
            Debug.Assert(to.IsEnumType());

            switch (from.SpecialType)
            {
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    break;
                default:
                    EmitConvert(from, 0, from = CoreTypes.Long);
                    break;
            }

            _il.EmitNumericConversion(from.PrimitiveTypeCode, to.EnumUnderlyingType.PrimitiveTypeCode, false);
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

            Debug.Assert(!to.IsErrorType(), "Conversion to an error type.");

            // conversion is not needed:
            if (from.SpecialType == to.SpecialType &&
                (from == to || (to.SpecialType != SpecialType.System_Object && from.IsOfType(to))))
            {
                return;
            }

            //
            from = EmitSpecialize(from, fromHint);

            // conversion is not needed:
            if (from.SpecialType == to.SpecialType &&
                (from == to || (to.SpecialType != SpecialType.System_Object && from.IsOfType(to))))
            {
                return;
            }

            // specialized conversions:
            switch (to.SpecialType)
            {
                case SpecialType.System_Void:
                    EmitPop(from);
                    return;
                case SpecialType.System_Boolean:
                    EmitConvertToBool(from, fromHint);
                    return;
                case SpecialType.System_Int32:
                    EmitConvertToInt(from, fromHint);
                    return;
                case SpecialType.System_Int64:
                    EmitConvertToLong(from, fromHint);
                    return;
                case SpecialType.System_Single:
                    EmitConvertToDouble(from, fromHint);
                    _il.EmitOpCode(ILOpCode.Conv_r4);
                    return;
                case SpecialType.System_Double:
                    EmitConvertToDouble(from, fromHint);
                    return;
                case SpecialType.System_String:
                    EmitConvertToString(from, fromHint);
                    return;

                case SpecialType.System_Char:
                    // Template: Convert.ToChar( (string)from )
                    EmitConvertToString(from, fromHint);
                    EmitCall(ILOpCode.Call, CoreMethods.Operators.ToChar_String);
                    return;

                default:
                    if (to == CoreTypes.PhpValue)
                    {
                        EmitConvertToPhpValue(from, fromHint);
                    }
                    else if (to == CoreTypes.PhpAlias)
                    {
                        if (from != CoreTypes.PhpValue)
                        {
                            if (from != CoreTypes.PhpAlias)
                            {
                                // Template: new PhpAlias((PhpValue))
                                EmitConvertToPhpValue(from, fromHint);
                                Emit_PhpValue_MakeAlias();
                            }
                        }
                        else
                        {
                            // Template: <STACK>.EnsureAlias()    // keeps already aliased value
                            EmitPhpValueAddr();
                            EmitCall(ILOpCode.Call, CoreMethods.PhpValue.EnsureAlias);
                        }
                    }
                    else if (to == CoreTypes.PhpNumber)
                    {
                        EmitConvertToPhpNumber(from, fromHint);
                    }
                    else if (to.IsReferenceType)
                    {
                        if (to == CoreTypes.PhpArray || to == CoreTypes.IPhpArray || to == CoreTypes.IPhpEnumerable || to == CoreTypes.PhpHashtable)
                        {
                            // -> PhpArray
                            // TODO: try unwrap "value.Object as T"
                            EmitConvertToPhpArray(from, fromHint);
                        }
                        else if (to == CoreTypes.PhpString)
                        {
                            // -> PhpString
                            EmitConvertToPhpString(from, fromHint);
                        }
                        else
                        {
                            // -> Object, PhpResource
                            EmitConvertToClass(from, fromHint, to);
                        }
                    }
                    else if (to.IsEnumType())
                    {
                        EmitConvertToEnum(from, (NamedTypeSymbol)to);
                    }
                    else if (to == CoreTypes.IntStringKey)
                    {
                        EmitConvertToIntStringKey(from, fromHint);
                    }
                    else
                    {
                        break;  // NotImplementedException
                    }
                    return; // Handled
            }

            //
            throw new NotImplementedException($"{to}");
        }

        /// <summary>
        /// Converts PHP value to an object in PHP manner.
        /// Implements PHP object cast operator.
        /// </summary>
        public TypeSymbol EmitCastToObject(BoundExpression expr)
        {
            Contract.ThrowIfNull(expr);

            var from = Emit(expr);

            switch (from.SpecialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Boolean:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    from = EmitConvertToPhpValue(from, expr.TypeRefMask);
                    goto default;

                default:
                    if (from == CoreTypes.PhpNumber)
                    {
                        // Object
                        EmitPhpNumberAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpNumber.ToClass)
                            .Expect(SpecialType.System_Object);
                    }

                    if (from == CoreTypes.PhpAlias)
                    {
                        // Template: <alias>.Value.ToClass()
                        Emit_PhpAlias_GetValueAddr();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpValue.ToClass)
                            .Expect(SpecialType.System_Object);
                    }

                    if (from.IsOfType(CoreTypes.PhpArray))
                    {
                        // PhpArray.ToClass();
                        return EmitCall(ILOpCode.Call, CoreMethods.PhpArray.ToClass);
                    }

                    if (from.IsOfType(CoreTypes.IPhpArray))
                    {
                        // Convert.ToClass(IPhpArray)
                        return EmitCall(ILOpCode.Call, CoreMethods.Operators.ToClass_IPhpArray);
                    }

                    if (from.IsReferenceType &&
                        from != CoreTypes.PhpString &&
                        !from.IsOfType(CoreTypes.PhpResource))
                    {
                        Debug.Assert(from != CoreTypes.PhpAlias);
                        return from;
                    }

                    // <PhpValue>.ToClass()

                    EmitConvertToPhpValue(from, expr.TypeRefMask);

                    // Convert.ToClass( value )
                    return EmitCall(ILOpCode.Call, CoreMethods.Operators.ToClass_PhpValue)
                        .Expect(SpecialType.System_Object);
            }
        }
    }
}
