using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.CodeGen;
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
using BinaryKind = Microsoft.CodeAnalysis.Semantics.BinaryOperationKind;

namespace Pchp.CodeAnalysis.Semantics
{
    partial class BoundExpression
    {
        /// <summary>
        /// Emits the expression with its bound access.
        /// Only Read or None access is possible. Write access has to be handled separately.
        /// </summary>
        /// <param name="cg">Associated code generator.</param>
        /// <returns>The type of expression emitted on top of the evaluation stack.</returns>
        internal virtual TypeSymbol Emit(CodeGenerator cg)
        {
            throw ExceptionUtilities.UnexpectedValue(this.GetType().FullName);
        }
    }

    partial class BoundBinaryEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(this.Access.IsRead || this.Access.IsNone);

            //
            TypeSymbol returned_type;

            if (UsesOperatorMethod)
            {
                throw new NotImplementedException();    // call this.Operator(Left, Right)
            }

            switch (this.Operation)
            {
                #region Arithmetic Operations

                case Operations.Add:
                    returned_type = (cg.IsLongOnly(this.TypeRefMask)) ? cg.CoreTypes.Long.Symbol : this.Access.TargetType;
                    returned_type = EmitAdd(cg, Left, Right, returned_type);
                    break;

                case Operations.Sub:
                    //Template: "x - y"        Operators.Subtract(x,y) [overloads]
                    returned_type = EmitSub(cg, Left, Right, this.Access.TargetType);
                    break;

                case Operations.Div:
                    //Template: "x / y"
                    returned_type = EmitDivision(cg);
                    break;

                case Operations.Mul:
                    //Template: "x * y"
                    returned_type = EmitMultiply(cg);
                    break;

                case Operations.Pow:
                    //Template: "x ** y"
                    returned_type = EmitPow(cg);
                    break;

                case Operations.Mod:
                    // x % y
                    returned_type = EmitRemainder(cg, Left, Right);
                    break;

                case Operations.ShiftLeft:
                    //Template: x << y : long
                    returned_type = EmitShift(cg, Left, Right, ILOpCode.Shl);
                    break;

                case Operations.ShiftRight:
                    //Template: x >> y : long
                    returned_type = EmitShift(cg, Left, Right, ILOpCode.Shr);
                    break;

                #endregion

                #region Boolean and Bitwise Operations

                case Operations.And:
                    returned_type = EmitBinaryBooleanOperation(cg, true);
                    break;

                case Operations.Or:
                    returned_type = EmitBinaryBooleanOperation(cg, false);
                    break;

                case Operations.Xor:
                    returned_type = EmitBinaryXor(cg);
                    break;

                case Operations.BitAnd:
                    returned_type = EmitBitAnd(cg, Left, Right);
                    break;

                case Operations.BitOr:
                    returned_type = EmitBitOr(cg, Left, Right);
                    break;

                case Operations.BitXor:
                    returned_type = EmitBitXor(cg, Left, Right);
                    break;

                #endregion

                #region Comparing Operations

                case Operations.Equal:
                    returned_type = EmitEquality(cg);
                    break;

                case Operations.NotEqual:
                    EmitEquality(cg);
                    cg.EmitLogicNegation();
                    returned_type = cg.CoreTypes.Boolean;
                    break;

                case Operations.GreaterThan:
                    returned_type = EmitLtGt(cg, false);
                    break;

                case Operations.LessThan:
                    returned_type = EmitLtGt(cg, true);
                    break;

                case Operations.GreaterThanOrEqual:
                    // template: !(LessThan)
                    returned_type = EmitLtGt(cg, true);
                    cg.EmitLogicNegation();
                    break;

                case Operations.LessThanOrEqual:
                    // template: !(GreaterThan)
                    returned_type = EmitLtGt(cg, false);
                    cg.EmitLogicNegation();
                    break;

                case Operations.Identical:

                    // Left === Right
                    returned_type = EmitStrictEquality(cg);
                    break;

                case Operations.NotIdentical:

                    // ! (Left === Right)
                    returned_type = EmitStrictEquality(cg);
                    cg.EmitLogicNegation();
                    break;

                #endregion

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            //
            if (Access.IsNone)
            {
                // Result is not read, pop the result
                cg.EmitPop(returned_type);
                returned_type = cg.CoreTypes.Void;
            }
            else if (Access.IsRead)
            {
                Debug.Assert(returned_type.SpecialType != SpecialType.System_Void);
            }

            //
            return returned_type;
        }

        /// <summary>
        /// Emits <c>+</c> operator suitable for actual operands.
        /// </summary>
        private static TypeSymbol EmitAdd(CodeGenerator cg, BoundExpression left, BoundExpression right, TypeSymbol resultTypeOpt = null)
        {
            // Template: x + y
            return EmitAdd(cg, cg.Emit(left), right, resultTypeOpt);
        }

        /// <summary>
        /// Emits <c>+</c> operator suitable for actual operands.
        /// </summary>
        internal static TypeSymbol EmitAdd(CodeGenerator cg, TypeSymbol xtype, BoundExpression Right, TypeSymbol resultTypeOpt = null)
        {
            var il = cg.Builder;

            xtype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(xtype));    // int|bool -> long, string -> number

            if (xtype == cg.CoreTypes.PhpAlias)
            {
                // <PhpAlias>.Value
                xtype = cg.Emit_PhpAlias_GetValue();
            }

            //
            if (xtype == cg.CoreTypes.PhpNumber)
            {
                var ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(Right)));  // int|bool -> long, string -> number

                if (ytype == cg.CoreTypes.PhpNumber)
                {
                    // number + number : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_number_number)
                        .Expect(cg.CoreTypes.PhpNumber);
                }
                else if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // number + r8 : r8
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_number_double)
                        .Expect(SpecialType.System_Double);
                }
                else if (ytype.SpecialType == SpecialType.System_Int64)
                {
                    // number + long : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_number_long)
                        .Expect(cg.CoreTypes.PhpNumber);
                }
                else if (ytype == cg.CoreTypes.PhpValue)
                {
                    // number + value : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_number_value)
                        .Expect(cg.CoreTypes.PhpNumber);
                }

                //
                throw new NotImplementedException($"Add(number, {ytype.Name})");
            }
            else if (xtype.SpecialType == SpecialType.System_Double)
            {
                var ytype = cg.EmitConvertStringToNumber(cg.EmitConvertNumberToDouble(Right)); // bool|int|long|number -> double, string -> number

                if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // r8 + r8 : r8
                    il.EmitOpCode(ILOpCode.Add);
                    return cg.CoreTypes.Double;
                }
                else if (ytype == cg.CoreTypes.PhpValue)
                {
                    // r8 + value : r8
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_double_value)
                        .Expect(SpecialType.System_Double);
                }

                //
                throw new NotImplementedException($"Add(double, {ytype.Name})");
            }
            else if (xtype.SpecialType == SpecialType.System_Int64)
            {
                var ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(Right)));    // int|bool -> long, string -> number

                if (ytype.SpecialType == SpecialType.System_Int64)
                {
                    if (resultTypeOpt != null)
                    {
                        if (resultTypeOpt.SpecialType == SpecialType.System_Int64)
                        {
                            // (long)(i8 + i8 : number)
                            il.EmitOpCode(ILOpCode.Add);
                            return cg.CoreTypes.Long;
                        }
                    }

                    // i8 + i8 : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_long_long)
                        .Expect(cg.CoreTypes.PhpNumber);
                }
                else if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // i8 + r8 : r8
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_long_double)
                        .Expect(SpecialType.System_Double);
                }
                else if (ytype == cg.CoreTypes.PhpNumber)
                {
                    // i8 + number : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_long_number)
                        .Expect(cg.CoreTypes.PhpNumber);
                }
                else if (ytype == cg.CoreTypes.PhpValue)
                {
                    // i8 + value : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_long_value)
                        .Expect(cg.CoreTypes.PhpNumber);
                }

                //
                throw new NotImplementedException($"Add(int64, {ytype.Name})");
            }
            else if (xtype == cg.CoreTypes.PhpArray)
            {
                var ytype = cg.Emit(Right);
                if (ytype == cg.CoreTypes.PhpArray)
                {
                    // PhpArray.Union(array, array) : PhpArray
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Union_PhpArray_PhpArray)
                        .Expect(cg.CoreTypes.PhpArray);
                }

                if (ytype == cg.CoreTypes.PhpValue)
                {
                    // array + value
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_array_value);
                }

                //
                throw new NotImplementedException($"Add(PhpArray, {ytype.Name})");
            }
            else if (xtype == cg.CoreTypes.PhpValue)
            {
                var ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(Right)));    // int|bool -> long, string -> number

                if (ytype.SpecialType == SpecialType.System_Int64)
                {
                    // value + i8 : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_value_long)
                        .Expect(cg.CoreTypes.PhpNumber);
                }
                else if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // value + r8 : r8
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_value_double)
                        .Expect(SpecialType.System_Double);
                }
                else if (ytype == cg.CoreTypes.PhpArray)
                {
                    // value + array
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_value_array);
                }
                else if (ytype == cg.CoreTypes.PhpNumber)
                {
                    // value + number : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_value_number)
                        .Expect(cg.CoreTypes.PhpNumber);
                }
                else if (ytype == cg.CoreTypes.PhpValue)
                {
                    // value + value : value
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_value_value)
                        .Expect(cg.CoreTypes.PhpValue);
                }

                //
                throw new NotImplementedException($"Add(PhpValue, {ytype.Name})");
            }

            //
            throw new NotImplementedException($"Add({xtype.Name}, ...)");
        }

        /// <summary>
        /// Emits subtraction operator.
        /// </summary>
        internal static TypeSymbol EmitSub(CodeGenerator cg, BoundExpression left, BoundExpression right, TypeSymbol resultTypeOpt = null)
        {
            return EmitSub(cg, cg.Emit(left), right, resultTypeOpt);
        }

        /// <summary>
        /// Emits subtraction operator.
        /// </summary>
        internal static TypeSymbol EmitSub(CodeGenerator cg, TypeSymbol xtype, BoundExpression right, TypeSymbol resultTypeOpt = null)
        {
            var il = cg.Builder;

            xtype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(xtype));    // int|bool -> int64, string -> number
            TypeSymbol ytype;

            if (xtype == cg.CoreTypes.PhpAlias)
            {
                // <PhpAlias>.Value
                xtype = cg.Emit_PhpAlias_GetValue();
            }

            //
            switch (xtype.SpecialType)
            {
                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(right)));
                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // i8 - i8 : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_long_long)
                            .Expect(cg.CoreTypes.PhpNumber);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // i8 - r8 : r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_long_double)
                            .Expect(cg.CoreTypes.Double);
                    }
                    else if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // i8 - number : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_long_number)
                            .Expect(cg.CoreTypes.PhpNumber);
                    }
                    else
                    {
                        ytype = cg.EmitConvertToPhpValue(ytype, 0);
                        // i8 - value : value
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_long_value)
                            .Expect(cg.CoreTypes.PhpNumber);
                    }

                case SpecialType.System_Double:
                    ytype = cg.EmitConvertStringToNumber(cg.EmitConvertNumberToDouble(right)); // bool|int|long|number -> double, string -> number
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 - r8 : r8
                        il.EmitOpCode(ILOpCode.Sub);
                        return cg.CoreTypes.Double;
                    }
                    throw new NotImplementedException($"Sub(double, {ytype.Name})");

                case SpecialType.System_String:
                    xtype = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.ToNumber_String)
                        .Expect(cg.CoreTypes.PhpNumber);
                    goto default;

                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(right)));
                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // number - i8 : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_number_long)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            // number - r8 : double
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_number_double)
                                .Expect(SpecialType.System_Double);
                        }
                        else if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // number - number : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_number_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype == cg.CoreTypes.PhpValue)
                        {
                            // number - value : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_number_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }

                        throw new NotImplementedException($"Sub(PhpNumber, {ytype.Name})");
                    }
                    else if (xtype == cg.CoreTypes.PhpValue)
                    {
                        ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(right)));

                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // value - i8 : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_value_long)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            // value - r8 : r8
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_value_double)
                                .Expect(SpecialType.System_Double);
                        }
                        else if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // value - number : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_value_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype == cg.CoreTypes.PhpValue)
                        {
                            // value - value : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_value_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }

                        throw new NotImplementedException($"Sub(PhpValue, {ytype.Name})");
                    }

                    throw new NotImplementedException($"Sub({xtype.Name},...)");
            }
        }

        internal static TypeSymbol EmitBitAnd(CodeGenerator cg, BoundExpression left, BoundExpression right)
        {
            // most common cases:
            if (cg.IsLongOnly(left.TypeRefMask) || cg.IsLongOnly(right.TypeRefMask))
            {
                // i64 | i64 : i64
                cg.EmitConvert(left, cg.CoreTypes.Long);
                cg.EmitConvert(right, cg.CoreTypes.Long);
                cg.Builder.EmitOpCode(ILOpCode.And);
                return cg.CoreTypes.Long;
            }

            // TODO: IF cg.IsStringOnly(left.TypeRefMask) && cg.IsStringOnly(Right.TypeRefMask)

            //
            return EmitBitAnd(cg, cg.Emit(left), right);
        }

        internal static TypeSymbol EmitBitAnd(CodeGenerator cg, TypeSymbol xtype, BoundExpression right)
        {
            switch (xtype.SpecialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_Int32:
                case SpecialType.System_Boolean:
                case SpecialType.System_Double:
                    cg.EmitConvert(xtype, 0, cg.CoreTypes.Long);
                    goto case SpecialType.System_Int64;

                case SpecialType.System_Int64:
                    cg.EmitConvert(right, cg.CoreTypes.Long);
                    cg.Builder.EmitOpCode(ILOpCode.And);
                    return cg.CoreTypes.Long;

                default:
                    if (right.ResultType != null && right.ResultType.SpecialType != SpecialType.System_String)
                    {
                        // value | !string -> long | long -> long
                        cg.EmitConvert(xtype, 0, cg.CoreTypes.Long);
                        goto case SpecialType.System_Int64;
                    }

                    cg.EmitConvert(xtype, 0, cg.CoreTypes.PhpValue);
                    cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BitwiseAnd_PhpValue_PhpValue)
                        .Expect(cg.CoreTypes.PhpValue);
            }
        }

        internal static TypeSymbol EmitBitOr(CodeGenerator cg, BoundExpression left, BoundExpression right)
        {
            // most common cases:
            if (cg.IsLongOnly(left.TypeRefMask) || cg.IsLongOnly(right.TypeRefMask))
            {
                // i64 | i64 : i64
                cg.EmitConvert(left, cg.CoreTypes.Long);
                cg.EmitConvert(right, cg.CoreTypes.Long);
                cg.Builder.EmitOpCode(ILOpCode.Or);
                return cg.CoreTypes.Long;
            }

            // TODO: IF cg.IsStringOnly(left.TypeRefMask) && cg.IsStringOnly(Right.TypeRefMask)

            //
            return EmitBitOr(cg, cg.Emit(left), right);
        }

        internal static TypeSymbol EmitBitOr(CodeGenerator cg, TypeSymbol xtype, BoundExpression right)
        {
            switch (xtype.SpecialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_Int32:
                case SpecialType.System_Boolean:
                case SpecialType.System_Double:
                    cg.EmitConvert(xtype, 0, cg.CoreTypes.Long);
                    goto case SpecialType.System_Int64;

                case SpecialType.System_Int64:
                    cg.EmitConvert(right, cg.CoreTypes.Long);
                    cg.Builder.EmitOpCode(ILOpCode.Or);
                    return cg.CoreTypes.Long;

                default:
                    if (right.ResultType != null && right.ResultType.SpecialType != SpecialType.System_String)
                    {
                        // value | !string -> long | long -> long
                        cg.EmitConvert(xtype, 0, cg.CoreTypes.Long);
                        goto case SpecialType.System_Int64;
                    }

                    cg.EmitConvert(xtype, 0, cg.CoreTypes.PhpValue);
                    cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BitwiseOr_PhpValue_PhpValue)
                        .Expect(cg.CoreTypes.PhpValue);
            }
        }

        internal static TypeSymbol EmitBitXor(CodeGenerator cg, BoundExpression left, BoundExpression right)
        {
            // most common cases:
            if (cg.IsLongOnly(left.TypeRefMask) || cg.IsLongOnly(right.TypeRefMask))
            {
                // i64 | i64 : i64
                cg.EmitConvert(left, cg.CoreTypes.Long);
                cg.EmitConvert(right, cg.CoreTypes.Long);
                cg.Builder.EmitOpCode(ILOpCode.Xor);
                return cg.CoreTypes.Long;
            }

            // TODO: IF cg.IsStringOnly(left.TypeRefMask) && cg.IsStringOnly(Right.TypeRefMask)

            //
            return EmitBitXor(cg, cg.Emit(left), right);
        }

        internal static TypeSymbol EmitBitXor(CodeGenerator cg, TypeSymbol xtype, BoundExpression right)
        {
            switch (xtype.SpecialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_Int32:
                case SpecialType.System_Boolean:
                case SpecialType.System_Double:
                    cg.EmitConvert(xtype, 0, cg.CoreTypes.Long);
                    goto case SpecialType.System_Int64;

                case SpecialType.System_Int64:
                    cg.EmitConvert(right, cg.CoreTypes.Long);
                    cg.Builder.EmitOpCode(ILOpCode.Xor);
                    return cg.CoreTypes.Long;

                default:
                    if (right.ResultType != null && right.ResultType.SpecialType != SpecialType.System_String)
                    {
                        // value | !string -> long | long -> long
                        cg.EmitConvert(xtype, 0, cg.CoreTypes.Long);
                        goto case SpecialType.System_Int64;
                    }

                    cg.EmitConvert(xtype, 0, cg.CoreTypes.PhpValue);
                    cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BitwiseXor_PhpValue_PhpValue)
                        .Expect(cg.CoreTypes.PhpValue);
            }
        }

        internal static TypeSymbol EmitRemainder(CodeGenerator cg, BoundExpression left, BoundExpression right)
        {
            return EmitRemainder(cg, cg.Emit(left), right);
        }

        internal static TypeSymbol EmitRemainder(CodeGenerator cg, TypeSymbol xtype, BoundExpression right)
        {
            switch (xtype.SpecialType)
            {
                case SpecialType.System_Int32:
                case SpecialType.System_Double:
                    cg.Builder.EmitOpCode(ILOpCode.Conv_i8);    // cast to long
                    xtype = cg.CoreTypes.Long;
                    goto case SpecialType.System_Int64;

                case SpecialType.System_Int64:
                    if (cg.IsNumberOnly(right.TypeRefMask))
                    {
                        // long & long
                        cg.EmitConvert(right, cg.CoreTypes.Long);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mod_long_long);
                    }
                    else
                    {
                        // long % value
                        cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mod_long_value);
                    }

                default:

                    cg.EmitConvert(xtype, 0, cg.CoreTypes.PhpValue);

                    if (cg.IsNumberOnly(right.TypeRefMask))
                    {
                        // value % long
                        cg.EmitConvert(right, cg.CoreTypes.Long);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mod_value_long);
                    }
                    else
                    {
                        // value % value
                        cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mod_value_value);
                    }

            }
        }

        internal static TypeSymbol EmitShift(CodeGenerator cg, BoundExpression left, BoundExpression right, ILOpCode op)
        {
            cg.EmitConvert(left, cg.CoreTypes.Long);
            return EmitShift(cg, cg.CoreTypes.Long, right, op);
        }

        internal static TypeSymbol EmitShift(CodeGenerator cg, TypeSymbol xtype, BoundExpression right, ILOpCode op)
        {
            Debug.Assert(op == ILOpCode.Shl || op == ILOpCode.Shr);
            cg.EmitConvert(xtype, 0, cg.CoreTypes.Long);
            cg.EmitConvert(right, cg.CoreTypes.Int32);
            cg.Builder.EmitOpCode(op);

            return cg.CoreTypes.Long;
        }

        /// <summary>
        /// Emits binary boolean operation (AND or OR).
        /// </summary>
        /// <param name="cg">A code generator.</param>
        /// <param name="isAnd">Whether to emit AND, otherwise OR.</param>
        /// <returns>A type code of the result.</returns>
        TypeSymbol EmitBinaryBooleanOperation(CodeGenerator cg, bool isAnd)
        {
            var boolean = cg.CoreTypes.Boolean;  // typeof(bool)

            var il = cg.Builder;
            var partial_eval_label = new NamedLabel("<partial_eval>" + this.GetHashCode().ToString("X"));
            var end_label = new NamedLabel("<end>" + this.GetHashCode().ToString("X"));

            // IF [!]<(bool) Left> THEN GOTO partial_eval;
            cg.EmitConvert(Left, cg.CoreTypes.Boolean);
            il.EmitBranch(isAnd ? ILOpCode.Brfalse : ILOpCode.Brtrue, partial_eval_label);

            // <RESULT> = <(bool) Right>;
            cg.EmitConvert(Right, cg.CoreTypes.Boolean);

            // GOTO end;
            il.EmitBranch(ILOpCode.Br, end_label);
            il.AdjustStack(-1); // workarounds assert in ILBuilder.MarkLabel, we're doing something wrong with ILBuilder

            // partial_eval:
            il.MarkLabel(partial_eval_label);
            il.EmitOpCode(isAnd ? ILOpCode.Ldc_i4_0 : ILOpCode.Ldc_i4_1, 1);

            // end:
            il.MarkLabel(end_label);

            //
            return boolean;
        }

        /// <summary>
        /// Emits binary operation XOR.
        /// </summary>
        TypeSymbol EmitBinaryXor(CodeGenerator cg)
        {
            // LOAD <(bool) leftSon> == <(bool) rightSon>;
            cg.EmitConvert(Left, cg.CoreTypes.Boolean);
            cg.EmitConvert(Right, cg.CoreTypes.Boolean);
            cg.EmitOpCode(ILOpCode.Ceq);

            cg.EmitOpCode(ILOpCode.Ldc_i4_0);
            cg.EmitOpCode(ILOpCode.Ceq);

            return cg.CoreTypes.Boolean;
        }

        /// <summary>
        /// Emits check for values equality.
        /// Lefts <c>bool</c> on top of evaluation stack.
        /// </summary>
        TypeSymbol EmitEquality(CodeGenerator cg)
        {
            // x == y
            return EmitEquality(cg, Left, Right);
        }

        /// <summary>
        /// Emits check for values equality.
        /// Lefts <c>bool</c> on top of evaluation stack.
        /// </summary>
        internal static TypeSymbol EmitEquality(CodeGenerator cg, BoundExpression left, BoundExpression right)
        {
            if (left.ConstantValue.HasValue && left.ConstantValue.Value == null)
            {
                // null == right
                return EmitEqualityToNull(cg, right);
            }
            else if (right.ConstantValue.HasValue && right.ConstantValue.Value == null)
            {
                // left == null
                return EmitEqualityToNull(cg, left);
            }
            else
            {
                // left == right
                return EmitEquality(cg, cg.Emit(left), right);
            }
        }

        static TypeSymbol EmitEqualityToNull(CodeGenerator cg, BoundExpression expr)
        {
            // Template: <expr> == null

            var il = cg.Builder;
            var t = cg.Emit(expr);

            //
            switch (t.SpecialType)
            {
                case SpecialType.System_Object:
                    // object == null
                    il.EmitNullConstant();
                    il.EmitOpCode(ILOpCode.Ceq);
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Double:
                    // r8 == 0
                    il.EmitDoubleConstant(0.0);
                    il.EmitOpCode(ILOpCode.Ceq);
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Int32:
                    // i4 == 0
                    cg.EmitLogicNegation();
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Int64:
                    // i8 == 0
                    il.EmitLongConstant(0);
                    il.EmitOpCode(ILOpCode.Ceq);
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_String:
                    // string.IsNullOrEmpty(string)
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.IsNullOrEmpty_String);
                    return cg.CoreTypes.Boolean;

                default:
                    if (t == cg.CoreTypes.PhpNumber)
                    {
                        // number == 0L
                        il.EmitLongConstant(0);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Eq_number_long)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (t == cg.CoreTypes.PhpAlias)
                    {
                        // LOAD <PhpAlias>.Value
                        cg.Emit_PhpAlias_GetValue();
                    }
                    else
                    {
                        // LOAD <PhpValue>
                        cg.EmitConvert(t, 0, cg.CoreTypes.PhpValue);
                    }

                    // CeqNull(<value>)
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.CeqNull_value)
                        .Expect(SpecialType.System_Boolean);
            }
        }

        /// <summary>
        /// Emits check for values equality.
        /// Lefts <c>bool</c> on top of evaluation stack.
        /// </summary>
        internal static TypeSymbol EmitEquality(CodeGenerator cg, TypeSymbol xtype, BoundExpression right)
        {
            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Boolean:

                    // bool == y.ToBoolean()
                    cg.EmitConvert(right, cg.CoreTypes.Boolean);
                    cg.Builder.EmitOpCode(ILOpCode.Ceq);

                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Int32:
                    // i4 -> i8
                    cg.Builder.EmitOpCode(ILOpCode.Conv_i8);
                    goto case SpecialType.System_Int64;

                case SpecialType.System_Int64:

                    ytype = cg.Emit(right);

                    //
                    if (ytype.SpecialType == SpecialType.System_Int32)
                    {
                        cg.Builder.EmitOpCode(ILOpCode.Conv_i8);    // i4 -> i8
                        ytype = cg.CoreTypes.Long;
                    }

                    //
                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // i8 == i8
                        cg.Builder.EmitOpCode(ILOpCode.Ceq);
                        return cg.CoreTypes.Boolean;
                    }
                    else if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // i8 == r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Ceq_long_double)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Boolean)
                    {
                        // i8 == bool
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Ceq_long_bool)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (ytype.SpecialType == SpecialType.System_String)
                    {
                        // i8 == string
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Ceq_long_string)
                            .Expect(SpecialType.System_Boolean);
                    }

                    // value
                    ytype = cg.EmitConvertToPhpValue(ytype, 0);

                    // compare(i8, value) == 0
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_long_value);
                    cg.EmitLogicNegation();

                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Double:

                    ytype = cg.EmitConvertNumberToDouble(right);  // bool|long|int -> double

                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 == r8
                        cg.Builder.EmitOpCode(ILOpCode.Ceq);
                        return cg.CoreTypes.Boolean;
                    }
                    else if (ytype.SpecialType == SpecialType.System_String)
                    {
                        // r8 == string
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Ceq_double_string)
                            .Expect(SpecialType.System_Boolean);
                    }

                    // value
                    ytype = cg.EmitConvertToPhpValue(ytype, 0);

                    // compare(double, value) == 0
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_double_value);
                    cg.EmitLogicNegation();

                    return cg.CoreTypes.Boolean;

                case SpecialType.System_String:

                    ytype = cg.Emit(right);

                    if (ytype.SpecialType == SpecialType.System_Int32)
                    {
                        // i4 -> i8
                        cg.Builder.EmitOpCode(ILOpCode.Conv_i8);
                        ytype = cg.CoreTypes.Long;
                    }

                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // string == i8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Ceq_string_long)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Boolean)
                    {
                        // string == bool
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Ceq_string_bool)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // string == r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Ceq_string_double)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (ytype.SpecialType == SpecialType.System_String)
                    {
                        // compare(string, string) == 0
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_string_string).Expect(SpecialType.System_Int32);
                        cg.EmitLogicNegation();
                        return cg.CoreTypes.Boolean;
                    }

                    // value
                    ytype = cg.EmitConvertToPhpValue(ytype, 0);

                    // compare(string, value) == 0
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_string_value);
                    cg.EmitLogicNegation();
                    return cg.CoreTypes.Boolean;

                //case SpecialType.System_Object:
                //    goto default;

                default:

                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertIntToLong(cg.Emit(right));
                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // number == i8
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Eq_number_long)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            // number == r8
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Eq_number_double)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // number == number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Eq_number_number)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else
                        {
                            ytype = cg.EmitConvertToPhpValue(ytype, 0);
                            // number == value
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Eq_number_PhpValue)
                                .Expect(SpecialType.System_Boolean);
                        }
                    }
                    else
                    {
                        // TODO: xtype: PhpArray, ...

                        xtype = cg.EmitConvertToPhpValue(xtype, 0);

                        // TODO: overloads for type of <right>

                        ytype = cg.EmitConvertToPhpValue(cg.Emit(right), right.TypeRefMask);

                        // value == value
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Eq_PhpValue_PhpValue)
                            .Expect(SpecialType.System_Boolean);
                    }
            }
        }

        TypeSymbol EmitStrictEquality(CodeGenerator cg)
            => EmitStrictEquality(cg, Left, Right);

        internal static TypeSymbol EmitStrictEquality(CodeGenerator cg, BoundExpression left, BoundExpression right)
            => EmitStrictEquality(cg, cg.Emit(left), right);

        internal static TypeSymbol EmitStrictEquality(CodeGenerator cg, TypeSymbol xtype, BoundExpression right)
        {
            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Boolean:
                    ytype = cg.Emit(right);
                    if (ytype.SpecialType == SpecialType.System_Boolean)
                    {
                        // bool == bool
                        cg.Builder.EmitOpCode(ILOpCode.Ceq);
                        return cg.CoreTypes.Boolean;
                    }
                    else if (
                        ytype.SpecialType == SpecialType.System_Double ||
                        ytype.SpecialType == SpecialType.System_Int32 ||
                        ytype.SpecialType == SpecialType.System_Int64 ||
                        ytype.SpecialType == SpecialType.System_String ||
                        ytype.IsOfType(cg.CoreTypes.IPhpArray) ||
                        ytype == cg.CoreTypes.PhpString ||
                        ytype == cg.CoreTypes.Object)
                    {
                        // bool == something else => false
                        cg.EmitPop(ytype);
                        cg.EmitPop(xtype);
                        cg.Builder.EmitBoolConstant(false);
                        return cg.CoreTypes.Boolean;
                    }
                    else
                    {
                        // bool == PhpValue
                        cg.EmitConvertToPhpValue(ytype, 0);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_bool_PhpValue)
                            .Expect(SpecialType.System_Boolean);
                    }

                case SpecialType.System_Int32:
                    cg.Builder.EmitOpCode(ILOpCode.Conv_i8);    // i4 -> i8
                    goto case SpecialType.System_Int64;

                case SpecialType.System_Int64:
                    ytype = cg.Emit(right);
                    if (ytype.SpecialType == SpecialType.System_Int32)
                    {
                        cg.Builder.EmitOpCode(ILOpCode.Conv_i8);    // i4 -> i8
                        ytype = cg.CoreTypes.Long;
                    }

                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // i8 == i8
                        cg.Builder.EmitOpCode(ILOpCode.Ceq);
                        return cg.CoreTypes.Boolean;
                    }
                    else if (
                        ytype.SpecialType == SpecialType.System_Boolean ||
                        ytype.SpecialType == SpecialType.System_String ||
                        ytype.SpecialType == SpecialType.System_Double ||
                        ytype.IsOfType(cg.CoreTypes.IPhpArray) ||
                        ytype == cg.CoreTypes.Object ||
                        ytype == cg.CoreTypes.PhpString)
                    {
                        // i8 == something else => false
                        cg.EmitPop(ytype);
                        cg.EmitPop(xtype);
                        cg.Builder.EmitBoolConstant(false);
                        return cg.CoreTypes.Boolean;
                    }
                    else
                    {
                        // i8 == PhpValue
                        cg.EmitConvertToPhpValue(ytype, 0);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_long_PhpValue)
                            .Expect(SpecialType.System_Boolean);
                    }

                case SpecialType.System_Double:
                    ytype = cg.Emit(right);
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 == r8
                        cg.Builder.EmitOpCode(ILOpCode.Ceq);
                        return cg.CoreTypes.Boolean;
                    }
                    else if (
                        ytype.SpecialType == SpecialType.System_Boolean ||
                        ytype.SpecialType == SpecialType.System_String ||
                        ytype.SpecialType == SpecialType.System_Int64 ||
                        ytype.SpecialType == SpecialType.System_Int32 ||
                        ytype.IsOfType(cg.CoreTypes.IPhpArray) ||
                        ytype == cg.CoreTypes.Object ||
                        ytype == cg.CoreTypes.PhpString)
                    {
                        // r8 == something else => false
                        cg.EmitPop(ytype);
                        cg.EmitPop(xtype);
                        cg.Builder.EmitBoolConstant(false);
                        return cg.CoreTypes.Boolean;
                    }
                    else
                    {
                        // r8 == PhpValue
                        cg.EmitConvertToPhpValue(ytype, 0);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_double_PhpValue)
                            .Expect(SpecialType.System_Boolean);
                    }

                default:

                    //// === NULL
                    //if (right.ConstantValue.IsNull())
                    //{
                    // TODO 
                    //}

                    // TODO: PhpArray, Object === ...

                    xtype = cg.EmitConvertToPhpValue(xtype, 0);

                    ytype = cg.Emit(right);

                    if (ytype.SpecialType == SpecialType.System_Boolean)
                    {
                        // PhpValue == bool
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_PhpValue_bool)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else
                    {
                        ytype = cg.EmitConvertToPhpValue(ytype, 0);

                        // PhpValue == PhpValue
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_PhpValue_PhpValue)
                            .Expect(SpecialType.System_Boolean);
                    }
            }
        }

        /// <summary>
        /// Emits comparison operator pushing <c>bool</c> (<c>i4</c> of value <c>0</c> or <c>1</c>) onto the evaluation stack.
        /// </summary>
        /// <param name="cg">Code generator helper.</param>
        /// <param name="lt">True for <c>clt</c> (less than) otherwise <c>cgt</c> (greater than).</param>
        /// <returns>Resulting type code pushed onto the top of evaliuation stack.</returns>
        TypeSymbol EmitLtGt(CodeGenerator cg, bool lt)
            => EmitLtGt(cg, cg.Emit(Left), Right, lt);

        /// <summary>
        /// Emits comparison operator pushing <c>bool</c> (<c>i4</c> of value <c>0</c> or <c>1</c>) onto the evaluation stack.
        /// </summary>
        /// <returns>Resulting type code pushed onto the top of evaliuation stack.</returns>
        internal static TypeSymbol EmitLtGt(CodeGenerator cg, TypeSymbol xtype, BoundExpression right, bool lt)
        {
            TypeSymbol ytype;
            var il = cg.Builder;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Void:
                    // Operators.CompareNull(value)
                    throw new NotImplementedException();

                case SpecialType.System_Int32:
                    // i4 -> i8
                    il.EmitOpCode(ILOpCode.Conv_i8);
                    goto case SpecialType.System_Int64;

                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertIntToLong(cg.Emit(right));    // bool|int -> long
                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // i8 <> r8
                        return cg.EmitCall(ILOpCode.Call, lt
                            ? cg.CoreMethods.Operators.Clt_long_double
                            : cg.CoreMethods.Operators.Cgt_long_double);
                    }
                    else
                    {
                        ytype = cg.EmitConvertToPhpValue(ytype, 0);

                        // compare(i8, value) <> 0
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_long_value);

                        il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                        il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    }
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Double:
                    ytype = cg.EmitConvertNumberToDouble(right);    // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 <> r8
                        il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    }
                    else
                    {
                        // compare(r8, value)
                        ytype = cg.EmitConvertToPhpValue(ytype, 0);
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_double_value);

                        // <> 0
                        il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                        il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    }
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_String:
                    ytype = cg.Emit(right);
                    if (ytype.SpecialType == SpecialType.System_String)
                    {
                        // compare(string, string)
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_string_string);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // compare(string, long)
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_string_long);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // compare(string, double)
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_string_double);
                    }
                    else
                    {
                        // compare(string, value)
                        ytype = cg.EmitConvertToPhpValue(ytype, 0);
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_string_value);
                    }

                    // <> 0
                    il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                    il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Boolean:

                    cg.EmitConvert(right, cg.CoreTypes.Boolean);
                    ytype = cg.CoreTypes.Boolean;

                    // compare(bool, bool)
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_bool_bool);

                    // <> 0
                    il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                    il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    return cg.CoreTypes.Boolean;

                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertIntToLong(cg.Emit(right));    // bool|int -> long
                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // number <> i8
                            return cg.EmitCall(ILOpCode.Call, lt
                                ? cg.CoreMethods.PhpNumber.lt_number_long
                                : cg.CoreMethods.PhpNumber.gt_number_long)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            // number <> r8
                            return cg.EmitCall(ILOpCode.Call, lt
                                ? cg.CoreMethods.PhpNumber.lt_number_double
                                : cg.CoreMethods.PhpNumber.gt_number_double)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // number <> number
                            return cg.EmitCall(ILOpCode.Call, lt
                                ? cg.CoreMethods.PhpNumber.lt_number_number
                                : cg.CoreMethods.PhpNumber.gt_number_number)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else
                        {
                            ytype = cg.EmitConvertToPhpValue(ytype, 0);

                            // compare(number, value)
                            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_number_value);

                            // <> 0
                            il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);    // +1 on stack
                            il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                            return cg.CoreTypes.Boolean;
                        }
                    }
                    else
                    {
                        xtype = cg.EmitConvertToPhpValue(xtype, 0);
                        ytype = cg.Emit(right);

                        // TODO: if (ytype.SpecialType == SpecialType.System_Boolean) ...
                        // TODO: if (ytype.SpecialType == SpecialType.System_Int64) ...
                        // TODO: if (ytype.SpecialType == SpecialType.System_String) ...
                        // TODO: if (ytype.SpecialType == SpecialType.System_Double) ...

                        // compare(value, value)
                        ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask);
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_value_value);

                        // <> 0
                        il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                        il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                        return cg.CoreTypes.Boolean;
                    }
            }
        }

        /// <summary>
        /// Emits <c>*</c> operation.
        /// </summary>
        TypeSymbol EmitMultiply(CodeGenerator cg)
            => EmitMul(cg, cg.Emit(Left), Right);

        internal static TypeSymbol EmitMul(CodeGenerator cg, TypeSymbol xtype, BoundExpression right, TypeSymbol resultTypeOpt = null)
        {
            var il = cg.Builder;

            xtype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(xtype));    // int|bool -> int64, string -> number

            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Double:
                    ytype = cg.EmitConvertNumberToDouble(right); // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 * r8 : r8
                        il.EmitOpCode(ILOpCode.Mul);
                        return xtype;   // r8
                    }
                    else if (ytype == cg.CoreTypes.PhpValue)
                    {
                        // r8 * value : r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_double_value)
                                .Expect(SpecialType.System_Double);
                    }
                    //
                    throw new NotImplementedException($"Mul(double, {ytype.Name})");
                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(right)));
                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // i8 * i8 : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_long_long)
                                .Expect(cg.CoreTypes.PhpNumber);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // i8 * r8 : r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_long_double)
                                .Expect(SpecialType.System_Double);
                    }
                    else if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // i8 * number : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_long_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                    }
                    else if (ytype == cg.CoreTypes.PhpValue)
                    {
                        // i8 * value : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_long_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                    }
                    //
                    throw new NotImplementedException($"Mul(int64, {ytype.Name})");
                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(right)));

                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_number_long)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_number_double)
                                .Expect(cg.CoreTypes.Double);
                        }
                        else if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // number * number : number
                            cg.EmitConvertToPhpNumber(ytype, right.TypeRefMask);
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_number_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype == cg.CoreTypes.PhpValue)
                        {
                            // number * value : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_number_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else
                        {
                            // TODO: unconvertible

                            // number * number : number
                            cg.EmitConvertToPhpNumber(ytype, right.TypeRefMask);
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_number_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        //
                        throw new NotImplementedException($"Mul(PhpNumber, {ytype.Name})");
                    }
                    else if (xtype == cg.CoreTypes.PhpValue)
                    {
                        ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(right)));    // bool|int -> long, string -> number
                        if (ytype == cg.CoreTypes.PhpValue)
                        {
                            // value * value : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_value_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // value * number : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_value_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype == cg.CoreTypes.Long)
                        {
                            // value * i8 : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_value_long)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype == cg.CoreTypes.Double)
                        {
                            // value * r8 : double
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_value_double)
                                .Expect(SpecialType.System_Double);
                        }
                        //
                        throw new NotImplementedException($"Mul(PhpValue, {ytype.Name})");
                    }

                    //
                    throw new NotImplementedException($"Mul({xtype.Name}, ...)");
            }
        }

        /// <summary>
        /// Emits <c>/</c> operator.
        /// </summary>
        TypeSymbol EmitDivision(CodeGenerator cg)
            => EmitDiv(cg, cg.Emit(Left), Right);

        internal static TypeSymbol EmitDiv(CodeGenerator cg, TypeSymbol xtype, BoundExpression right, TypeSymbol resultTypeOpt = null)
        {
            var il = cg.Builder;

            xtype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(xtype));    // int|bool -> int64, string -> number
            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Double:
                    ytype = cg.EmitConvertNumberToDouble(right); // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        il.EmitOpCode(ILOpCode.Div);
                        return xtype;   // r8
                    }

                    // double / value : double
                    cg.EmitConvertToPhpValue(ytype, 0);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Div_double_PhpValue);

                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertIntToLong(cg.Emit(right));  // bool|int -> long
                    if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // long / number : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Division_long_number)
                            .Expect(cg.CoreTypes.PhpNumber);
                    }

                    // long / value : number
                    cg.EmitConvertToPhpValue(ytype, 0);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Div_long_PhpValue);

                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertStringToNumber(cg.EmitConvertIntToLong(cg.Emit(right)));  // bool|int -> long, string -> number
                        if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // number / number : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Division_number_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // number / i8 : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Division_number_long);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            // number / r8 : r8
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Division_number_double);
                        }
                        else if (ytype == cg.CoreTypes.PhpValue)
                        {
                            // number / value : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Division_number_value);
                        }

                        //
                        throw new NotImplementedException($"Div(number, {ytype.Name})");
                    }

                    // x -> PhpValue
                    xtype = cg.EmitConvertToPhpValue(xtype, 0);
                    cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                    ytype = cg.CoreTypes.PhpValue;

                    // value / value : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Div_PhpValue_PhpValue);
            }
        }

        /// <summary>
        /// Emits <c>pow</c> operator.
        /// </summary>
        TypeSymbol EmitPow(CodeGenerator cg)
        {
            return EmitPow(cg, cg.Emit(Left), Left.TypeRefMask, Right);
        }

        internal static TypeSymbol EmitPow(CodeGenerator cg, TypeSymbol xtype, FlowAnalysis.TypeRefMask xtype_hint, BoundExpression right)
        {
            var il = cg.Builder;

            TypeSymbol ytype;
            xtype = cg.EmitConvertIntToLong(xtype);    // int|bool -> long

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertIntToLong(cg.Emit(right));    // int|bool -> long

                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // i8 ** i8 : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_long_long);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // i8 ** r8 : r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_long_double);
                    }
                    else if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // i8 ** number : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_long_number);
                    }
                    // y -> PhpValue
                    cg.EmitConvert(ytype, right.TypeRefMask, cg.CoreTypes.PhpValue);
                    ytype = cg.CoreTypes.PhpValue;

                    // i8 ** value : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_long_value);

                case SpecialType.System_Double:
                    ytype = cg.EmitConvertNumberToDouble(right);    // int|bool|long|number -> double

                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 ** r8 : r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_double_double);
                    }
                    // y -> PhpValue
                    cg.EmitConvert(ytype, right.TypeRefMask, cg.CoreTypes.PhpValue);
                    ytype = cg.CoreTypes.PhpValue;

                    // r8 ** value : r8
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_double_value);

                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertIntToLong(cg.Emit(right));    // int|bool -> long
                        if (ytype == cg.CoreTypes.Double)
                        {
                            // number ** r8 : r8
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_number_double);
                        }

                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // y -> number
                            ytype = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Create_Long);
                        }

                        if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // number ** number : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_number_number);
                        }

                        // y -> PhpValue
                        ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask);

                        // number ** value : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_number_value);
                    }

                    // x -> PhpValue
                    xtype = cg.EmitConvertToPhpValue(xtype, xtype_hint);
                    cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                    ytype = cg.CoreTypes.PhpValue;

                    // value ** value : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_value_value);
            }
        }
    }

    partial class BoundUnaryEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(Access.IsRead || Access.IsNone);

            TypeSymbol returned_type;

            switch (this.Operation)
            {
                case Operations.AtSign:
                    // special arrangement
                    // Template:
                    //		context.DisableErrorReporting();
                    //		s;
                    //		context.EnableErrorReporting();
                    returned_type = cg.EmitWithDisabledErrorReporting(Operand);
                    break;

                case Operations.BitNegation:
                    //Template: "~x" Operators.BitNot(x)                                     
                    returned_type = EmitBitNot(cg);
                    break;

                case Operations.Clone:
                    // Template: clone x
                    returned_type = EmitClone(cg);
                    break;

                case Operations.LogicNegation:
                    //Template: !(bool)(x);                              
                    cg.EmitConvertToBool(this.Operand, true);
                    returned_type = cg.CoreTypes.Boolean;
                    break;

                case Operations.Minus:
                    //Template: "-x"
                    returned_type = EmitMinus(cg);
                    break;

                case Operations.Plus:
                    //Template: "+x"
                    returned_type = EmitPlus(cg);
                    break;

                case Operations.ObjectCast:
                    //Template: "(object)x"
                    cg.EmitConvert(this.Operand, cg.CoreTypes.Object);
                    returned_type = cg.CoreTypes.Object;
                    break;

                case Operations.Print:
                    cg.EmitEcho(this.Operand);

                    if (Access.IsRead)
                    {
                        // Always returns 1
                        cg.Builder.EmitLongConstant(1);
                        returned_type = cg.CoreTypes.Long;
                    }
                    else
                    {
                        // nobody reads the result anyway
                        returned_type = cg.CoreTypes.Void;
                    }
                    break;

                case Operations.BoolCast:
                    //Template: "(bool)x"
                    cg.EmitConvert(this.Operand, cg.CoreTypes.Boolean);
                    returned_type = cg.CoreTypes.Boolean;
                    break;

                case Operations.Int8Cast:
                case Operations.Int16Cast:
                case Operations.Int32Cast:
                case Operations.UInt8Cast:
                case Operations.UInt16Cast:

                case Operations.UInt64Cast:
                case Operations.UInt32Cast:
                case Operations.Int64Cast:

                    cg.EmitConvert(this.Operand, cg.CoreTypes.Long);
                    returned_type = cg.CoreTypes.Long;
                    break;

                case Operations.DecimalCast:
                case Operations.DoubleCast:
                case Operations.FloatCast:

                    cg.EmitConvert(this.Operand, cg.CoreTypes.Double);
                    returned_type = cg.CoreTypes.Double;
                    break;

                case Operations.UnicodeCast: // TODO
                case Operations.StringCast:
                    // (string)x
                    cg.EmitConvert(this.Operand, cg.CoreTypes.String);  // TODO: to String or PhpString ? to not corrupt single-byte string
                    return cg.CoreTypes.String;

                case Operations.BinaryCast:
                    //if ((returned_typecode = node.Expr.Emit(codeGenerator)) != PhpTypeCode.PhpBytes)
                    //{
                    //    codeGenerator.EmitBoxing(returned_typecode);
                    //    //codeGenerator.EmitLoadClassContext();
                    //    il.Emit(OpCodes.Call, Methods.Convert.ObjectToPhpBytes);
                    //    returned_typecode = PhpTypeCode.PhpBytes;
                    //}
                    //break;
                    throw new NotImplementedException();

                case Operations.ArrayCast:
                    //Template: "(array)x"
                    cg.EmitConvert(this.Operand, cg.CoreTypes.PhpArray);    // TODO: EmitArrayCast()
                    returned_type = cg.CoreTypes.PhpArray;
                    break;

                case Operations.UnsetCast:
                    // Template: "(unset)x"  null

                    cg.EmitPop(cg.Emit(this.Operand));

                    if (this.Access.IsRead)
                    {
                        cg.Builder.EmitNullConstant();
                        returned_type = cg.CoreTypes.Object;
                    }
                    else
                    {
                        returned_type = cg.CoreTypes.Void;
                    }
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            //
            if (Access.IsNone)
            {
                // Result is not read, pop the result
                cg.EmitPop(returned_type);
                returned_type = cg.CoreTypes.Void;
            }
            else if (Access.IsRead)
            {
                Debug.Assert(returned_type.SpecialType != SpecialType.System_Void);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(Access);
            }

            return returned_type;
        }

        TypeSymbol EmitMinus(CodeGenerator cg)
        {
            // Template: 0L - Operand

            var il = cg.Builder;
            var t = cg.Emit(this.Operand);

            switch (t.SpecialType)
            {
                case SpecialType.System_Double:
                    // -r8
                    il.EmitOpCode(ILOpCode.Neg);
                    return t;
                case SpecialType.System_Int32:
                    // -(i8)i4
                    il.EmitOpCode(ILOpCode.Conv_i8);    // i4 -> i8
                    il.EmitOpCode(ILOpCode.Neg);        // result will fit into long for sure
                    return cg.CoreTypes.Long;
                case SpecialType.System_Int64:
                    // PhpNumber.Minus(i8) : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Negation_long)
                            .Expect(cg.CoreTypes.PhpNumber);
                default:
                    if (t != cg.CoreTypes.PhpNumber)
                    {
                        cg.EmitConvertToPhpNumber(t, 0);
                    }

                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Negation)
                        .Expect(cg.CoreTypes.PhpNumber);
            }
        }

        TypeSymbol EmitPlus(CodeGenerator cg)
        {
            // Template: 0L + Operand

            // convert value to a number

            var il = cg.Builder;
            var t = cg.Emit(this.Operand);

            switch (t.SpecialType)
            {
                case SpecialType.System_Double:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                    return t;
                case SpecialType.System_Boolean:
                    // (long)(int)bool
                    il.EmitOpCode(ILOpCode.Conv_i4);
                    il.EmitOpCode(ILOpCode.Conv_i8);
                    return cg.CoreTypes.Long;
                default:
                    if (t != cg.CoreTypes.PhpNumber)
                    {
                        cg.EmitConvertToPhpNumber(t, 0);
                    }

                    return cg.CoreTypes.PhpNumber;
            }
        }

        TypeSymbol EmitBitNot(CodeGenerator cg)
        {
            var il = cg.Builder;
            var t = cg.Emit(this.Operand);

            switch (t.SpecialType)
            {
                case SpecialType.System_Double:
                case SpecialType.System_Int32:
                    // r8|i4 -> i8
                    il.EmitOpCode(ILOpCode.Conv_i8);
                    goto case SpecialType.System_Int64;

                case SpecialType.System_Int64:
                    il.EmitOpCode(ILOpCode.Not);    // ~i64 : i64
                    return cg.CoreTypes.Long;

                case SpecialType.System_Boolean:
                    throw new NotImplementedException();    // Err
                default:
                    if (t == cg.CoreTypes.PhpArray)
                    {
                        // ERR
                    }

                    // ~ PhpValue
                    cg.EmitConvert(t, Operand.TypeRefMask, cg.CoreTypes.PhpValue);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BitwiseNot_PhpValue);
            }
        }

        TypeSymbol EmitClone(CodeGenerator cg)
        {
            // Template clone(Context, Object)
            cg.EmitLoadContext();
            var t = cg.EmitAsObject(cg.Emit(this.Operand));

            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Clone_Context_Object)
                .Expect(SpecialType.System_Object);

            //
            return t;
        }
    }

    partial class BoundLiteral
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(this.Access.IsRead || Access.IsNone);

            // do nothing
            if (this.Access.IsNone)
            {
                return cg.CoreTypes.Void;
            }

            // push value onto the evaluation stack

            Debug.Assert(ConstantValue.HasValue);
            return cg.EmitLoadConstant(ConstantValue.Value, this.Access.TargetType);
        }
    }

    partial class BoundReferenceExpression
    {
        /// <summary>
        /// Gets <see cref="IBoundReference"/> providing load and store operations.
        /// </summary>
        internal abstract IBoundReference BindPlace(CodeGenerator cg);

        internal abstract IPlace Place(ILBuilder il);

        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(this.Access.IsRead || this.Access.IsNone);

            if (Access.IsNone)
            {
                // do nothing
                return cg.CoreTypes.Void;
            }

            if (ConstantValue.HasValue)
            {
                return cg.EmitLoadConstant(ConstantValue.Value, this.Access.TargetType);
            }

            var place = this.BindPlace(cg);
            place.EmitLoadPrepare(cg);
            return place.EmitLoad(cg);
        }
    }

    partial class BoundVariableRef
    {
        internal override IBoundReference BindPlace(CodeGenerator cg) => this.Variable.BindPlace(cg.Builder, this.Access, this.TypeRefMask);

        internal override IPlace Place(ILBuilder il) => this.Variable.Place(il);
    }

    partial class BoundListEx : IBoundReference
    {
        internal override IBoundReference BindPlace(CodeGenerator cg) => this;

        internal override IPlace Place(ILBuilder il) => null;

        #region IBoundReference

        TypeSymbol IBoundReference.TypeOpt => null;

        void IBoundReference.EmitLoadPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            throw new InvalidOperationException();
        }

        TypeSymbol IBoundReference.EmitLoad(CodeGenerator cg)
        {
            throw new InvalidOperationException();
        }

        void IBoundReference.EmitStorePrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            // nop
        }

        void IBoundReference.EmitStore(CodeGenerator cg, TypeSymbol valueType)
        {
            var rtype = cg.EmitAsPhpArray(valueType);

            var tmp = cg.GetTemporaryLocal(rtype);
            cg.Builder.EmitLocalStore(tmp);

            // Template: if (<tmp> != null) { ... }
            var lblnull = new NamedLabel("<tmp> == null");
            var lblend = new NamedLabel("<list> end");
            cg.Builder.EmitLocalLoad(tmp);
            cg.Builder.EmitBranch(ILOpCode.Brfalse, lblnull);

            // NOTE: since PHP7, variables are assigned from left to right
            var vars = this.Variables;
            for (int i = 0; i < vars.Length; i++)
            {
                var target = vars[i];
                if (target == null)
                    continue;

                // Template: <vars[i]> = <tmp>[i]

                var boundtarget = target.BindPlace(cg);
                boundtarget.EmitStorePrepare(cg);

                // LOAD IPhpArray.GetItemValue(IntStringKey{i})
                cg.Builder.EmitLocalLoad(tmp);
                cg.EmitIntStringKey(i);
                var itemtype = cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.GetItemValue_IntStringKey);

                // STORE vars[i]
                boundtarget.EmitStore(cg, itemtype);
            }

            cg.Builder.EmitBranch(ILOpCode.Br, lblend);

            //
            cg.ReturnTemporaryLocal(tmp);

            // Template: <vars[i]> = NULL
            cg.Builder.MarkLabel(lblnull);
            for (int i = 0; i < vars.Length; i++)
            {
                var target = vars[i];
                if (target == null)
                    continue;

                // Template: <vars[i]> = NULL

                var boundtarget = target.BindPlace(cg);
                boundtarget.EmitStorePrepare(cg);

                // LOAD default<T> // = NULL
                var t = boundtarget.TypeOpt ?? cg.CoreTypes.PhpValue;
                cg.EmitLoadDefault(t, 0);

                // STORE vars[i]
                boundtarget.EmitStore(cg, t);
            }

            //
            cg.Builder.MarkLabel(lblend);

        }

        #endregion
    }

    partial class BoundFieldRef
    {
        internal IBoundReference BoundReference { get; set; }

        IFieldSymbol FieldSymbolOpt => (BoundReference as BoundFieldPlace)?.Field;

        internal override IBoundReference BindPlace(CodeGenerator cg)
        {
            // TODO: constant bound reference
            Debug.Assert(BoundReference != null, "BoundReference was not set!");
            return BoundReference;
        }

        internal override IPlace Place(ILBuilder il)
        {
            var fldplace = BoundReference as BoundFieldPlace;
            if (fldplace != null)
            {
                Debug.Assert(fldplace.Field != null);

                var instanceplace = (fldplace.Instance as BoundReferenceExpression)?.Place(il);
                if ((fldplace.Field.IsStatic) ||
                    (instanceplace != null && instanceplace.TypeOpt != null && instanceplace.TypeOpt.IsOfType(fldplace.Field.ContainingType)))
                {
                    return new FieldPlace(instanceplace, fldplace.Field);
                }
            }

            return null;
        }
    }

    partial class BoundRoutineCall
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            return !TargetMethod.IsErrorMethod()
                ? EmitDirectCall(cg, IsVirtualCall ? ILOpCode.Callvirt : ILOpCode.Call, TargetMethod, LateStaticTypeRef)
                : EmitCallsiteCall(cg);
        }

        internal virtual TypeSymbol EmitDirectCall(CodeGenerator cg, ILOpCode opcode, MethodSymbol method, BoundTypeRef staticType = null)
        {
            // TODO: emit check the routine is declared
            // <ctx>.AssertFunctionDeclared

            var arguments = _arguments.Select(a => a.Value).ToImmutableArray();

            return (this.ResultType = cg.EmitMethodAccess(cg.EmitCall(opcode, method, this.Instance, arguments, staticType), method, Access));
        }

        protected virtual string CallsiteName => null;
        protected virtual BoundExpression RoutineNameExpr => null;
        protected virtual BoundTypeRef RoutineTypeRef => null;

        /// <summary>Type reference to the static type. The containing type of called routine, e.g. <c>THE_TYPE::foo()</c>. Used for direct method call requiring late static type..</summary>
        protected virtual BoundTypeRef LateStaticTypeRef => null;
        protected virtual bool IsVirtualCall => true;

        /// <summary>
        /// Optional. Emits instance on which the method is invoked.
        /// In case of instance function call, it is the instance expression,
        /// in case of static method, it is reference to <c>$this</c> which may be needed in some cases.
        /// </summary>
        /// <returns>Type left on stack. Can be <c>null</c> if no target was emitted.</returns>
        internal virtual TypeSymbol EmitTarget(CodeGenerator cg)
        {
            if (Instance != null)
            {
                return cg.Emit(Instance);
            }
            else
            {
                return null;
            }
        }

        internal virtual TypeSymbol EmitCallsiteCall(CodeGenerator cg)
        {
            // callsite

            var nameOpt = this.CallsiteName;
            var callsite = cg.Factory.StartCallSite("call_" + nameOpt);

            var callsiteargs = new List<TypeSymbol>(_arguments.Length);
            var return_type = this.Access.IsRead
                    ? this.Access.IsReadRef
                        ? cg.CoreTypes.PhpAlias.Symbol
                        : (this.Access.TargetType ?? cg.CoreTypes.PhpValue.Symbol)
                    : cg.CoreTypes.Void.Symbol;

            // callsite
            var fldPlace = callsite.Place;

            // LOAD callsite.Target
            callsite.EmitLoadTarget(cg.Builder);

            // LOAD callsite arguments

            // (callsite, [target], ctx, [name], ...)
            fldPlace.EmitLoad(cg.Builder);

            callsiteargs.Add(cg.EmitLoadContext());     // ctx

            var t = EmitTarget(cg);
            if (t != null)
            {
                callsiteargs.Add(t);   // instance
            }

            if (RoutineTypeRef != null)
            {
                callsiteargs.Add(RoutineTypeRef.EmitLoadTypeInfo(cg, true));   // PhpTypeInfo
            }

            if (RoutineNameExpr != null)
            {
                callsiteargs.Add(cg.Emit(RoutineNameExpr));   // name
            }

            foreach (var a in _arguments)
            {
                callsiteargs.Add(cg.Emit(a.Value));
            }

            // Target()
            var functype = cg.Factory.GetCallSiteDelegateType(
                null, RefKind.None,
                callsiteargs.AsImmutable(),
                default(ImmutableArray<RefKind>),
                null,
                return_type);

            cg.EmitCall(ILOpCode.Callvirt, functype.DelegateInvokeMethod);

            // Create CallSite ...
            callsite.Construct(functype, cctor_cg => BuildCallsiteCreate(cctor_cg, return_type));

            //
            return return_type;
        }

        internal virtual void BuildCallsiteCreate(CodeGenerator cg, TypeSymbol returntype) { throw new InvalidOperationException(); }
    }

    partial class BoundGlobalFunctionCall
    {
        protected override string CallsiteName => _name.IsDirect ? _name.NameValue.ToString() : null;
        protected override BoundExpression RoutineNameExpr => _name.NameExpression;
        protected override bool IsVirtualCall => false;

        internal override TypeSymbol EmitCallsiteCall(CodeGenerator cg)
        {
            if (_name.IsDirect)
            {
                return base.EmitCallsiteCall(cg);
            }
            else
            {
                Debug.Assert(_name.NameExpression != null);

                // faster to emit PhpCallback.Invoke

                // NameExpression.AsCallback().Invoke(Context, PhpValue[])

                cg.EmitConvert(_name.NameExpression, cg.CoreTypes.IPhpCallable);    // (IPhpCallable)Name
                cg.EmitLoadContext();       // Context
                cg.Emit_NewArray(cg.CoreTypes.PhpValue, _arguments.Select(a => a.Value).ToArray()); // PhpValue[]

                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreTypes.IPhpCallable.Symbol.LookupMember<MethodSymbol>("Invoke"));
            }
        }

        internal override void BuildCallsiteCreate(CodeGenerator cg, TypeSymbol returntype)
        {
            cg.Builder.EmitStringConstant(CallsiteName);
            cg.Builder.EmitStringConstant(_nameOpt.HasValue ? _nameOpt.Value.ToString() : null);
            cg.EmitLoadToken(returntype, null);
            cg.Builder.EmitIntConstant(0);
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.CallBinderFactory_Function);
        }
    }

    partial class BoundInstanceFunctionCall
    {
        protected override string CallsiteName => _name.IsDirect ? _name.NameValue.ToString() : null;
        protected override BoundExpression RoutineNameExpr => _name.NameExpression;

        internal override void BuildCallsiteCreate(CodeGenerator cg, TypeSymbol returntype)
        {
            cg.Builder.EmitStringConstant(CallsiteName);        // name
            cg.EmitLoadToken(cg.CallerType, null);              // class context
            cg.EmitLoadToken(returntype, null);                 // return type
            cg.Builder.EmitIntConstant(0);                      // generic params count
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.CallBinderFactory_InstanceFunction);
        }
    }

    partial class BoundStaticFunctionCall
    {
        protected override string CallsiteName => _name.IsDirect ? _name.NameValue.ToString() : null;
        protected override BoundExpression RoutineNameExpr => _name.NameExpression;
        protected override BoundTypeRef RoutineTypeRef => (_typeRef.ResolvedType == null) ? _typeRef : null;    // in case the type has to be resolved in runtime and passed to callsite
        protected override BoundTypeRef LateStaticTypeRef => _typeRef;  // used for direct routine call requiring late static type
        protected override bool IsVirtualCall => false;

        internal override TypeSymbol EmitTarget(CodeGenerator cg)
        {
            return cg.EmitThisOrNull();
        }

        internal override void BuildCallsiteCreate(CodeGenerator cg, TypeSymbol returntype)
        {
            cg.EmitLoadToken(_typeRef.ResolvedType, null);      // type
            cg.Builder.EmitStringConstant(CallsiteName);        // name
            cg.EmitLoadToken(cg.CallerType, null);              // class context
            cg.EmitLoadToken(returntype, null);                 // return type
            cg.Builder.EmitIntConstant(0);                      // generic params count
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.CallBinderFactory_StaticFunction);
        }
    }

    partial class BoundNewEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            if (!TargetMethod.IsErrorMethod())
            {
                return EmitDirectCall(cg, ILOpCode.Newobj, TargetMethod);
            }
            else
            {
                if (_typeref.ResolvedType != null)
                {
                    // context.Create<T>(caller, params)
                    var create_t = cg.CoreTypes.Context.Symbol.GetMembers("Create")
                        .OfType<MethodSymbol>()
                        .Where(s => s.Arity == 1 && s.ParameterCount == 2 &&
                            s.Parameters[1].IsParams &&
                            SpecialParameterSymbol.IsCallerClassParameter(s.Parameters[0]))
                        .Single()
                        .Construct(_typeref.ResolvedType);

                    cg.EmitLoadContext();               // Context
                    cg.EmitCallerRuntimeTypeHandle();   // RuntimeTypeHandle
                    cg.Emit_NewArray(cg.CoreTypes.PhpValue, _arguments.Select(a => a.Value).ToArray());  // PhpValue[]

                    return cg.EmitCall(ILOpCode.Call, create_t);
                }
                else
                {
                    // ctx.Create(caller, PhpTypeInfo, params)
                    var create = cg.CoreTypes.Context.Symbol.GetMembers("Create")
                        .OfType<MethodSymbol>()
                        .Where(s => s.Arity == 0 && s.ParameterCount == 3 &&
                            s.Parameters[1].Type == cg.CoreTypes.PhpTypeInfo &&
                            s.Parameters[2].IsParams && ((ArrayTypeSymbol)s.Parameters[2].Type).ElementType == cg.CoreTypes.PhpValue &&
                            SpecialParameterSymbol.IsCallerClassParameter(s.Parameters[0]))
                        .Single();

                    cg.EmitLoadContext();                   // Context
                    cg.EmitCallerRuntimeTypeHandle();       // RuntimeTypeHandle
                    _typeref.EmitLoadTypeInfo(cg, true);    // PhpTypeInfo
                    cg.Emit_NewArray(cg.CoreTypes.PhpValue, _arguments.Select(a => a.Value).ToArray());  // PhpValue[]

                    return cg.EmitCall(ILOpCode.Call, create);
                }
            }
        }
    }

    partial class BoundEcho
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(Access.IsNone);

            _arguments
                .Select(a => a.Value)
                .ForEach(cg.EmitEcho);

            return cg.CoreTypes.Void;
        }
    }

    partial class BoundConcatEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            // new PhpString()
            cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpString);

            // TODO: overload for 2, 3, 4 parameters directly

            // <PhpString>.Append(<expr>)
            foreach (var x in this.ArgumentsInSourceOrder)
            {
                var expr = x.Value;
                if (IsEmpty(expr))
                    continue;

                //
                cg.Builder.EmitOpCode(ILOpCode.Dup);    // PhpString

                var t = cg.Emit(expr);
                if (t == cg.CoreTypes.PhpString)
                {
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpString.Append_PhpString);
                }
                else
                {
                    // TODO: PhpValue -> PhpString (instead of String)

                    cg.EmitConvert(t, 0, cg.CoreTypes.String);
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpString.Append_String);
                }

                //
                cg.Builder.EmitOpCode(ILOpCode.Nop);
            }

            //
            return cg.CoreTypes.PhpString;
        }

        bool IsEmpty(BoundExpression x)
        {
            if (x.ConstantValue.HasValue)
            {
                var value = x.ConstantValue.Value;
                if (value == null)
                    return true;

                if (value is string && ((string)value).Length == 0)
                    return true;

                if (value is bool && ((bool)value) == false)
                    return true;
            }

            return false;
        }
    }

    partial class BoundIncludeEx
    {
        /// <summary>
        /// True for <c>include_once</c> or <c>require_once</c>.
        /// </summary>
        public bool IsOnceSemantic => this.InclusionType == InclusionTypes.IncludeOnce || this.InclusionType == InclusionTypes.RequireOnce;

        /// <summary>
        /// True for <c>require</c> or <c>require_once</c>.
        /// </summary>
        public bool IsRequireSemantic => this.InclusionType == InclusionTypes.Require || this.InclusionType == InclusionTypes.RequireOnce;

        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            TypeSymbol result;
            var isvoid = this.Access.IsNone;

            Debug.Assert(_arguments.Length == 1);
            Debug.Assert(_arguments[0].Value.Access.IsRead);
            Debug.Assert(Access.IsRead || Access.IsNone);

            var method = this.Target;
            if (method != null) // => IsResolved
            {
                // emit condition for include_once/require_once
                if (IsOnceSemantic)
                {
                    var tscript = method.ContainingType;

                    result = isvoid
                        ? cg.CoreTypes.Void.Symbol
                        : cg.DeclaringCompilation.GetTypeFromTypeRef(cg.Routine.TypeRefContext, this.TypeRefMask);

                    // Template: (<ctx>.CheckIncludeOnce<TScript>()) ? <Main>() : TRUE
                    // Template<isvoid>: if (<ctx>.CheckIncludeOnce<TScript>()) <Main>()
                    var falseLabel = new object();
                    var endLabel = new object();

                    cg.EmitLoadContext();
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.CheckIncludeOnce_TScript.Symbol.Construct(tscript));

                    cg.Builder.EmitBranch(ILOpCode.Brfalse, falseLabel);

                    // ? (PhpValue)<Main>(...)
                    cg.EmitCallMain(method);
                    if (isvoid)
                    {
                        cg.EmitPop(method.ReturnType);
                    }
                    else
                    {
                        cg.EmitConvert(method.ReturnType, 0, result);
                    }
                    cg.Builder.EmitBranch(ILOpCode.Br, endLabel);

                    if (!isvoid)
                    {
                        cg.Builder.AdjustStack(-1); // workarounds assert in ILBuilder.MarkLabel, we're doing something wrong with ILBuilder
                    }

                    // : PhpValue.Create(true)
                    cg.Builder.MarkLabel(falseLabel);
                    if (!isvoid)
                    {
                        cg.Builder.EmitBoolConstant(true);
                        cg.EmitConvert(cg.CoreTypes.Boolean, 0, result);
                    }

                    //
                    cg.Builder.MarkLabel(endLabel);
                }
                else
                {
                    // <Main>
                    result = cg.EmitCallMain(method);
                }
            }
            else
            {
                Debug.Assert(cg.LocalsPlaceOpt != null);

                // Template: <ctx>.Include(dir, path, locals, @this, bool once = false, bool throwOnError = false)
                cg.EmitLoadContext();
                cg.Builder.EmitStringConstant(cg.Routine.ContainingFile.DirectoryRelativePath);
                cg.EmitConvert(_arguments[0].Value, cg.CoreTypes.String);
                cg.LocalsPlaceOpt.EmitLoad(cg.Builder); // scope of local variables, corresponds to $GLOBALS in global scope.
                cg.EmitThisOrNull();    // $this
                cg.Builder.EmitBoolConstant(IsOnceSemantic);
                cg.Builder.EmitBoolConstant(IsRequireSemantic);
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.Include_string_string_PhpArray_object_bool_bool);
            }

            //
            return result;
        }
    }

    partial class BoundLambda
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            if (this.BoundLambdaMethod == null)
            {
                throw new InvalidOperationException();
            }

            // Template: BuildClosure(BoundLambdaMethod.EnsureRoutineInfoField(), [this, use1, use2, ...], [p1, p2, ...])

            var idxfld = this.BoundLambdaMethod.EnsureRoutineInfoField(cg.Module);
            new FieldPlace(null, idxfld).EmitLoad(cg.Builder);

            EmitParametersArray(cg);
            EmitUseArray(cg);

            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BuildClosure_RoutineInfo_PhpArray_PhpArray);
        }

        void EmitUseArray(CodeGenerator cg)
        {
            var count = (BoundLambdaMethod.UseThis ? 1 : 0) + UseVars.Length;
            if (count != 0)
            {
                // new PhpArray(<count>)
                cg.Builder.EmitIntConstant(count);
                cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpArray_int);

                //
                if (BoundLambdaMethod.UseThis)
                {
                    // <stack>.Add("this", this)
                    cg.Builder.EmitOpCode(ILOpCode.Dup);
                    cg.EmitIntStringKey(VariableName.ThisVariableName.Value);
                    cg.EmitConvertToPhpValue(cg.EmitThisOrNull(), 0);
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_IntStringKey_PhpValue);
                }

                // uses
                foreach (var u in UseVars)
                {
                    // <stack>.SetItemValue|SetItemAlias(name, value)
                    cg.Builder.EmitOpCode(ILOpCode.Dup);
                    cg.EmitIntStringKey(u.Parameter.Name);

                    if (u.Value.Access.IsReadRef)
                    {
                        // PhpValue.Create( PhpAlias )
                        cg.Emit(u.Value).Expect(cg.CoreTypes.PhpAlias); // PhpAlias
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Create_PhpAlias);    // PhpValue.Create
                    }
                    else
                    {
                        // PhpValue
                        cg.EmitConvert(u.Value, cg.CoreTypes.PhpValue);
                    }

                    // Add(name, value)
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_IntStringKey_PhpValue);
                }
            }
            else
            {
                cg.Emit_PhpArray_Empty();
            }
        }

        void EmitParametersArray(CodeGenerator cg)
        {
            var ps = ((LambdaFunctionExpr)PhpSyntax).Signature.FormalParams;
            if (ps != null && ps.Length != 0)
            {
                // new PhpArray(<count>){ ... }
                cg.Builder.EmitIntConstant(ps.Length);
                cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpArray_int);

                foreach (var p in ps)
                {
                    var keyname = "$" + p.Name.Name.Value;
                    if (p.PassedByRef) keyname = "&" + keyname;
                    var value = (p.InitValue != null) ? "<optional>" : "<required>";

                    // <stack>.SetItemValue("&$name", "<optional>"|"<required>")
                    cg.Builder.EmitOpCode(ILOpCode.Dup);
                    cg.EmitIntStringKey(keyname);
                    cg.Builder.EmitStringConstant(value);
                    cg.EmitConvertToPhpValue(cg.CoreTypes.String, 0);
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_IntStringKey_PhpValue);
                }
            }
            else
            {
                // PhpArray.Empty
                cg.Emit_PhpArray_Empty();
            }
        }
    }

    partial class BoundEvalEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(cg.LocalsPlaceOpt != null);

            // get location of evaluated code
            var filepath = cg.Routine.ContainingFile.RelativeFilePath;
            int line, col;
            var unit = this.PhpSyntax.ContainingSourceUnit;
            unit.GetLineColumnFromPosition(this.CodeExpression.PhpSyntax.Span.Start, out line, out col);

            // Template: Operators.Eval(ctx, locals, @this, code, currentpath, line, column)
            cg.EmitLoadContext();
            cg.LocalsPlaceOpt.EmitLoad(cg.Builder);
            cg.EmitThisOrNull();
            cg.EmitConvert(this.CodeExpression, cg.CoreTypes.String);   // (string)code
            cg.Builder.EmitStringConstant(filepath);    // currentpath
            cg.Builder.EmitIntConstant(line);           // line
            cg.Builder.EmitIntConstant(col);            // column

            // Eval( ... )
            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Eval_Context_PhpArray_object_string_string_int_int);
        }
    }

    partial class BoundExitEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            MethodSymbol ctorsymbol;

            if (_arguments.Length == 0)
            {
                // <ctx>.Exit();
                ctorsymbol = cg.CoreMethods.Ctors.ScriptDiedException;
            }
            else
            {
                // LOAD <status>
                var t = cg.Emit(_arguments[0].Value);

                switch (t.SpecialType)
                {
                    case SpecialType.System_Int32:
                        cg.Builder.EmitOpCode(ILOpCode.Conv_i8);    // i4 -> i8
                        goto case SpecialType.System_Int64;

                    case SpecialType.System_Int64:
                        ctorsymbol = cg.CoreMethods.Ctors.ScriptDiedException_Long;
                        break;

                    default:
                        cg.EmitConvertToPhpValue(t, 0);
                        ctorsymbol = cg.CoreMethods.Ctors.ScriptDiedException_PhpValue;
                        break;
                }
            }

            //
            cg.EmitCall(ILOpCode.Newobj, ctorsymbol);
            cg.Builder.EmitThrow(false);

            //
            return cg.CoreTypes.Void;
        }
    }

    partial class BoundAssignEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var target_place = this.Target.BindPlace(cg);
            Debug.Assert(target_place != null);
            Debug.Assert(target_place.TypeOpt == null || target_place.TypeOpt.SpecialType != SpecialType.System_Void);

            // T tmp; // in case access is Read
            var t_value = target_place.TypeOpt;
            if (t_value == cg.CoreTypes.PhpAlias || t_value == cg.CoreTypes.PhpValue)
                t_value = null; // no inplace conversion

            LocalDefinition tmp = null;

            // <target> = <value>
            target_place.EmitStorePrepare(cg);

            // TODO: load value & dereference eventually
            if (t_value != null && !this.Value.Access.IsReadRef)
            {
                cg.EmitConvert(this.Value, t_value);   // TODO: do not convert here yet
            }
            else
            {
                t_value = cg.Emit(this.Value);
            }

            if (t_value.SpecialType == SpecialType.System_Void)
            {
                // default<T>
                t_value = target_place.TypeOpt ?? cg.CoreTypes.PhpValue; // T of PhpValue
                cg.EmitLoadDefault(t_value, 0);
            }

            //
            if (Access.IsNone)
            {
            }
            else if (Access.IsRead)
            {
                tmp = cg.GetTemporaryLocal(t_value, false);
                cg.Builder.EmitOpCode(ILOpCode.Dup);
                cg.Builder.EmitLocalStore(tmp);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(Access);
            }

            target_place.EmitStore(cg, t_value);

            //
            if (Access.IsNone)
            {
                t_value = cg.CoreTypes.Void;
            }
            else if (Access.IsRead)
            {
                cg.Builder.EmitLocalLoad(tmp);

                if (Access.IsReadCopy)
                {
                    // DeepCopy(<tmp>)
                    t_value = cg.EmitDeepCopy(t_value, this.Value.TypeRefMask);
                }
            }

            if (tmp != null)
            {
                cg.ReturnTemporaryLocal(tmp);
            }

            //
            return t_value;
        }
    }

    partial class BoundCompoundAssignEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(Access.IsRead || Access.IsNone);

            // target X= value;

            var target_place = this.Target.BindPlace(cg);
            Debug.Assert(target_place != null);
            Debug.Assert(target_place.TypeOpt == null || target_place.TypeOpt.SpecialType != SpecialType.System_Void);

            // helper class maintaining reference to already evaluated instance of the eventual chain
            using (var instance_holder = new InstanceCacheHolder())
            {
                // <target> = <target> X <value>
                target_place.EmitStorePrepare(cg, instance_holder);

                //
                target_place.EmitLoadPrepare(cg, instance_holder);
            }

            var xtype = target_place.EmitLoad(cg);  // type of left value operand

            TypeSymbol result_type;

            switch (this.Operation)
            {
                case Operations.AssignAdd:
                    result_type = BoundBinaryEx.EmitAdd(cg, xtype, Value, target_place.TypeOpt);
                    break;
                case Operations.AssignAppend:
                    result_type = EmitAppend(cg, xtype, Value);
                    break;
                ////case Operations.AssignPrepend:
                ////    break;
                case Operations.AssignDiv:
                    result_type = BoundBinaryEx.EmitDiv(cg, xtype, Value, target_place.TypeOpt);
                    break;
                //case Operations.AssignMod:
                //    binaryop = Operations.Mod;
                //    break;
                case Operations.AssignMul:
                    result_type = BoundBinaryEx.EmitMul(cg, xtype, Value, target_place.TypeOpt);
                    break;
                case Operations.AssignAnd:
                    result_type = BoundBinaryEx.EmitBitAnd(cg, xtype, Value);
                    break;
                case Operations.AssignOr:
                    result_type = BoundBinaryEx.EmitBitOr(cg, xtype, Value);
                    break;
                case Operations.AssignXor:
                    result_type = BoundBinaryEx.EmitBitXor(cg, xtype, Value);
                    break;
                case Operations.AssignPow:
                    result_type = BoundBinaryEx.EmitPow(cg, xtype, /*this.Target.TypeRefMask*/0, Value);
                    break;
                case Operations.AssignShiftLeft:
                case Operations.AssignShiftRight:
                    result_type = BoundBinaryEx.EmitShift(cg, xtype, Value, this.Operation == Operations.AssignShiftLeft ? ILOpCode.Shl : ILOpCode.Shr);
                    break;
                case Operations.AssignSub:
                    result_type = BoundBinaryEx.EmitSub(cg, xtype, Value, target_place.TypeOpt);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(this.Operation);
            }

            LocalDefinition tmp = null;

            if (Access.IsRead)
            {
                tmp = cg.GetTemporaryLocal(result_type, false);
                cg.Builder.EmitOpCode(ILOpCode.Dup);
                cg.Builder.EmitLocalStore(tmp);
            }

            target_place.EmitStore(cg, result_type);

            //
            if (Access.IsRead)
            {
                Debug.Assert(tmp != null);
                cg.Builder.EmitLoad(tmp);
                cg.ReturnTemporaryLocal(tmp);
                return result_type;
            }
            else if (Access.IsNone)
            {
                return cg.CoreTypes.Void;
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(this.Access);
            }
        }

        static TypeSymbol EmitAppend(CodeGenerator cg, TypeSymbol xtype, BoundExpression y)
        {
            if (xtype == cg.CoreTypes.PhpString)
            {
                // x.Append(y); return x;
                cg.Builder.EmitOpCode(ILOpCode.Dup);

                cg.EmitConvert(y, cg.CoreTypes.String);
                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpString.Append_String);

                //
                return xtype;
            }
            else
            {
                // concat(x, y)
                cg.EmitConvert(xtype, 0, cg.CoreTypes.String);
                cg.EmitConvert(y, cg.CoreTypes.String);

                return cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpString_string_string);
            }
        }
    }

    partial class BoundIncDecEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(this.Access.IsNone || Access.IsRead);
            Debug.Assert(!this.Access.IsReadRef);
            Debug.Assert(!this.Access.IsWrite);
            Debug.Assert(this.Target.Access.IsRead && this.Target.Access.IsWrite);
            Debug.Assert(this.Value.Access.IsRead);

            Debug.Assert(this.Value is BoundLiteral);

            if (this.UsesOperatorMethod)
            {
                throw new NotImplementedException();
            }

            TypeSymbol result_type = cg.CoreTypes.Void;
            LocalDefinition tempvar = null;    // temporary variable containing result of the expression if needed

            var read = this.Access.IsRead;

            var target_place = this.Target.BindPlace(cg);
            Debug.Assert(target_place != null);

            using (var instance_holder = new InstanceCacheHolder())
            {
                // prepare target for store operation
                target_place.EmitStorePrepare(cg, instance_holder);

                // load target value
                target_place.EmitLoadPrepare(cg, instance_holder);
            }

            var target_load_type = target_place.EmitLoad(cg);

            TypeSymbol op_type;

            if (read && IsPostfix)
            {
                // store value of target
                // <temp> = TARGET
                tempvar = cg.GetTemporaryLocal(target_load_type);
                cg.EmitOpCode(ILOpCode.Dup);
                cg.Builder.EmitLocalStore(tempvar);
            }

            if (IsIncrement)
            {
                op_type = BoundBinaryEx.EmitAdd(cg, target_load_type, this.Value, target_place.TypeOpt);
            }
            else
            {
                Debug.Assert(IsDecrement);
                op_type = BoundBinaryEx.EmitSub(cg, target_load_type, this.Value, target_place.TypeOpt);
            }

            if (read && IsPrefix)
            {
                // store value of result
                // <temp> = TARGET
                tempvar = cg.GetTemporaryLocal(op_type);
                cg.EmitOpCode(ILOpCode.Dup);
                cg.Builder.EmitLocalStore(tempvar);
            }

            //
            target_place.EmitStore(cg, op_type);

            if (read)
            {
                Debug.Assert(tempvar != null);

                // READ <temp>
                cg.Builder.EmitLocalLoad(tempvar);
                result_type = (TypeSymbol)tempvar.Type;

                //
                cg.ReturnTemporaryLocal(tempvar);
                tempvar = null;
            }

            //
            Debug.Assert(tempvar == null);
            Debug.Assert(!read || result_type.SpecialType != SpecialType.System_Void);

            //
            return result_type;
        }

        bool IsPostfix => this.IncrementKind == UnaryOperationKind.OperatorPostfixIncrement || this.IncrementKind == UnaryOperationKind.OperatorPostfixDecrement;
        bool IsPrefix => !IsPostfix;
        bool IsIncrement => this.IncrementKind == UnaryOperationKind.OperatorPostfixIncrement || this.IncrementKind == UnaryOperationKind.OperatorPrefixIncrement;
        bool IsDecrement => this.IncrementKind == UnaryOperationKind.OperatorPostfixDecrement || this.IncrementKind == UnaryOperationKind.OperatorPrefixDecrement;
    }

    partial class BoundConditionalEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var result_type = cg.DeclaringCompilation.GetTypeFromTypeRef(cg.Routine, this.TypeRefMask);

            if (this.IfTrue != null)
            {
                object trueLbl = new object();
                object endLbl = new object();

                // Cond ? True : False
                cg.EmitConvert(this.Condition, cg.CoreTypes.Boolean);   // i4
                cg.Builder.EmitBranch(ILOpCode.Brtrue, trueLbl);

                // false:
                cg.EmitConvert(this.IfFalse, result_type);
                cg.Builder.EmitBranch(ILOpCode.Br, endLbl);
                cg.Builder.AdjustStack(-1); // workarounds assert in ILBuilder.MarkLabel, we're doing something wrong with ILBuilder
                // trueLbl:
                cg.Builder.MarkLabel(trueLbl);
                cg.EmitConvert(this.IfTrue, result_type);

                // endLbl:
                cg.Builder.MarkLabel(endLbl);
            }
            else
            {
                object trueLbl = new object();
                object endLbl = new object();

                // Cond ?: False

                // <stack> = <cond_var> = Cond
                var cond_type = cg.Emit(this.Condition);
                var cond_var = cg.GetTemporaryLocal(cond_type);
                cg.Builder.EmitOpCode(ILOpCode.Dup);
                cg.Builder.EmitLocalStore(cond_var);

                cg.EmitConvertToBool(cond_type, this.Condition.TypeRefMask);
                cg.Builder.EmitBranch(ILOpCode.Brtrue, trueLbl);

                // false:
                cg.EmitConvert(this.IfFalse, result_type);
                cg.Builder.EmitBranch(ILOpCode.Br, endLbl);
                cg.Builder.AdjustStack(-1); // workarounds assert in ILBuilder.MarkLabel, we're doing something wrong with ILBuilder

                // trueLbl:
                cg.Builder.MarkLabel(trueLbl);
                cg.Builder.EmitLocalLoad(cond_var);
                cg.EmitConvert(cond_type, this.Condition.TypeRefMask, result_type);

                // endLbl:
                cg.Builder.MarkLabel(endLbl);

                //
                cg.ReturnTemporaryLocal(cond_var);
            }

            //
            if (Access.IsNone)
            {
                cg.EmitPop(result_type);
                result_type = cg.CoreTypes.Void;
            }

            //
            return result_type;
        }
    }

    partial class BoundArrayEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            if (_items.Length == 0)
            {
                // PhpArray.NewEmpty()
                return cg.Emit_PhpArray_NewEmpty();
            }

            // new PhpArray(count)
            cg.Builder.EmitIntConstant(_items.Length);
            var result = cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpArray_int)
                .Expect(cg.CoreTypes.PhpArray);

            foreach (var x in _items)
            {
                // <PhpArray>
                cg.Builder.EmitOpCode(ILOpCode.Dup);

                // key
                if (x.Key != null)
                {
                    cg.EmitIntStringKey(x.Key);
                }

                // value
                Debug.Assert(x.Value != null);
                cg.EmitConvert(x.Value, cg.CoreTypes.PhpValue);

                if (x.Key != null)
                {
                    // <stack>.Add(key, value)
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_IntStringKey_PhpValue);
                }
                else
                {
                    // <stack>.Add(value) : int
                    cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_PhpValue));
                }
            }

            //
            return result;
        }
    }

    partial class BoundArrayItemEx : IBoundReference
    {
        internal override IBoundReference BindPlace(CodeGenerator cg)
        {
            // resolve result type of place
            _type = Access.IsReadRef
                ? cg.CoreTypes.PhpAlias
                : cg.CoreTypes.PhpValue;

            //
            return this;
        }

        internal override IPlace Place(ILBuilder il)
        {
            // TODO: simple array access in case Array is System.Array and Key is int|long

            return null;
        }

        #region IBoundReference

        TypeSymbol IBoundReference.TypeOpt => _type;
        TypeSymbol _type;

        #region Emitted Array Stack

        /// <summary>
        /// Stack of arrays emitted by <see cref="IBoundReference.EmitLoadPrepare"/> and <see cref="IBoundReference.EmitStorePrepare"/>.
        /// </summary>
        Stack<TypeSymbol> _emittedArrays;

        /// <summary>
        /// <see cref="IBoundReference.EmitLoadPrepare"/> and <see cref="IBoundReference.EmitStorePrepare"/> remembers what was the array type it emitted.
        /// Used by <see cref="PopEmittedArray"/> and <see cref="IBoundReference.EmitLoad"/> or <see cref="IBoundReference.EmitStore"/> to emit specific operator.
        /// </summary>
        void PushEmittedArray(TypeSymbol t)
        {
            Debug.Assert(t != null);

            if (_emittedArrays == null)
            {
                _emittedArrays = new Stack<TypeSymbol>();
            }

            _emittedArrays.Push(t);
        }

        /// <summary>
        /// Used by <see cref="IBoundReference.EmitLoad"/> and <see cref="IBoundReference.EmitStore"/> to emit specific operator
        /// on a previously emitted array (<see cref="PushEmittedArray"/>).
        /// </summary>
        TypeSymbol PopEmittedArray()
        {
            Debug.Assert(_emittedArrays != null && _emittedArrays.Count != 0);
            var result = _emittedArrays.Pop();
            if (_emittedArrays.Count == 0)
            {
                _emittedArrays = null;   // free
            }

            return result;
        }

        #endregion

        void IBoundReference.EmitLoadPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            // Template: array[index]

            //
            // LOAD Array
            //

            var t = InstanceCacheHolder.EmitInstance(instanceOpt, cg, Array);
            var intstrkey = true;

            // convert {t} to IPhpArray, string, System.Array

            if (t.IsOfType(cg.CoreTypes.IPhpArray))
            {
                // ok; PhpArray, PhpString, object implementing IPhpArray
            }
            else if (t == cg.CoreTypes.PhpValue)
            {
                // ok
            }
            else if (t == cg.CoreTypes.PhpAlias)
            {
                t = cg.Emit_PhpAlias_GetValue();
            }
            else if (t == cg.CoreTypes.String)
            {
                // ok
            }
            else if (t == cg.CoreTypes.Void)
            {
                Debug.Fail("Use of uninitialized value.");  // TODO: Err in analysis, use of uninitialized value
            }
            else if (t.IsArray())   // TODO: IList, IDictionary
            {
                // ok
            }
            else if (t.IsOfType(cg.CoreTypes.ArrayAccess))
            {
                // ok
                intstrkey = false;  // do not convert to IntStringKey
            }
            else
            {
                throw new NotImplementedException($"LOAD {t.Name}[]");    // TODO: emit convert as PhpArray
            }

            if (this.Access.IsRead && this.Access.IsQuiet)  // TODO: analyse if Array can be NULL
            {
                // ?? PhpArray.Empty
                if (cg.CoreTypes.PhpArray.Symbol.IsOfType(t))
                {
                    var lbl_notnull = new NamedLabel("NotNull");
                    cg.Builder.EmitOpCode(ILOpCode.Dup);
                    cg.Builder.EmitBranch(ILOpCode.Brtrue, lbl_notnull);

                    cg.Builder.EmitOpCode(ILOpCode.Pop);
                    cg.EmitCastClass(cg.Emit_PhpArray_Empty(), t);

                    cg.Builder.MarkLabel(lbl_notnull);
                }
            }

            Debug.Assert(
                t.IsOfType(cg.CoreTypes.IPhpArray) ||
                t.SpecialType == SpecialType.System_String ||
                t.IsArray() ||
                t.IsOfType(cg.CoreTypes.ArrayAccess) ||
                t == cg.CoreTypes.PhpValue);
            PushEmittedArray(t);

            //
            // LOAD [Index]
            //

            Debug.Assert(this.Index != null || this.Access.IsEnsure, "Index is required when reading the array item.");

            if (this.Index != null)
            {
                if (intstrkey)
                    cg.EmitIntStringKey(this.Index);    // TODO: save Index into InstanceCacheHolder
                else
                    cg.Emit(this.Index);
            }
        }

        TypeSymbol IBoundReference.EmitLoad(CodeGenerator cg)
        {
            // Template: array[index]

            var arrtype = PopEmittedArray();
            if (arrtype.IsOfType(cg.CoreTypes.IPhpArray))
            {
                var isphparr = (arrtype == cg.CoreTypes.PhpArray);    // whether the target is instance of PhpArray, otherwise it is an IPhpArray and we have to use .callvirt

                if (this.Index == null)
                {
                    Debug.Assert(this.Access.IsEnsure);
                    /*
                     * Template:
                     * <array>.AddValue((PhpValue)(tmp = new <T>));
                     * LOAD tmp;
                     */
                    LocalDefinition tmp;
                    if (Access.EnsureArray)
                    {
                        // tmp = new PhpArray();
                        tmp = cg.GetTemporaryLocal(cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpArray));
                    }
                    else if (Access.EnsureObject)
                    {
                        // tmp = new stdClass();
                        tmp = cg.GetTemporaryLocal(cg.EmitCall(ILOpCode.Newobj, cg.CoreTypes.stdClass.Ctor()));
                    }
                    else if (Access.IsReadRef)
                    {
                        // tmp = new PhpAlias(NULL, 1)
                        cg.Emit_PhpValue_Null();
                        cg.Builder.EmitIntConstant(1);
                        tmp = cg.GetTemporaryLocal(cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpAlias_PhpValue_int));
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(Access);
                    }

                    cg.Builder.EmitOpCode(ILOpCode.Dup);
                    cg.Builder.EmitLocalStore(tmp);

                    var tmp_type = (TypeSymbol)tmp.Type;
                    cg.EmitConvertToPhpValue(tmp_type, 0);

                    if (isphparr) cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_PhpValue));
                    else cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.AddValue_PhpValue);

                    //
                    cg.Builder.EmitLocalLoad(tmp);
                    cg.ReturnTemporaryLocal(tmp);
                    return tmp_type;
                }
                else if (Access.EnsureObject)
                {
                    // <array>.EnsureItemObject(<key>)
                    return isphparr
                        ? cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.EnsureItemObject_IntStringKey)
                        : cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.EnsureItemObject_IntStringKey);
                }
                else if (Access.EnsureArray)
                {
                    return isphparr
                        ? cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.EnsureItemArray_IntStringKey)
                        : cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.EnsureItemArray_IntStringKey);
                }
                else if (Access.IsReadRef)
                {
                    Debug.Assert(this.Array.Access.EnsureArray);

                    return isphparr
                        ? cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.EnsureItemAlias_IntStringKey)
                        : cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.EnsureItemAlias_IntStringKey);
                }
                else
                {
                    Debug.Assert(Access.IsRead);
                    var t = isphparr
                        ? cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.GetItemValue_IntStringKey)
                        : cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.GetItemValue_IntStringKey);

                    if (Access.IsReadCopy)
                    {
                        t = cg.EmitReadCopy(Access.TargetType, t);
                    }

                    return t;
                }
            }
            else if (arrtype.SpecialType == SpecialType.System_String)
            {
                if (Access.EnsureObject || Access.EnsureArray || Access.IsReadRef)
                {
                    // null
                    throw new InvalidOperationException();
                }
                else
                {
                    Debug.Assert(Access.IsRead);
                    // GetItemValue(string, IntStringKey)
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetItemValue_String_IntStringKey);
                }
            }
            else if (arrtype == cg.CoreTypes.PhpValue)
            {
                if (Access.EnsureObject || Access.EnsureArray)
                {
                    // null
                    throw new InvalidOperationException();
                }
                else if (Access.IsReadRef)
                {
                    Debug.WriteLine("TODO: we need reference to PhpValue so we can modifiy its content! This is not compatible with behavior of = &$null[0].");
                    // PhpValue.GetItemRef(IntStringKey, bool)
                    cg.Builder.EmitBoolConstant(Access.IsQuiet);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureItemAlias_PhpValue_IntStringKey_Bool);
                }
                else
                {
                    Debug.Assert(Access.IsRead);
                    // PhpValue.GetItemValue(IntStringKey, bool)
                    cg.Builder.EmitBoolConstant(Access.IsQuiet);
                    var t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetItemValue_PhpValue_IntStringKey_Bool);

                    if (Access.IsReadCopy)
                    {
                        t = cg.EmitReadCopy(Access.TargetType, t);
                    }

                    return t;
                }
            }
            else if (arrtype.IsOfType(cg.CoreTypes.ArrayAccess))
            {
                // Template: ArrayAccess.offsetGet(<index>)
                cg.EmitConvert(this.Index.ResultType, this.Index.TypeRefMask, cg.CoreTypes.PhpValue);
                var t = cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Operators.offsetGet_ArrayAccess_PhpValue);

                if (Access.IsReadCopy)
                {
                    t = cg.EmitReadCopy(Access.TargetType, t);
                }

                return t;
            }
            else if (arrtype.SpecialType == SpecialType.System_Void)
            {
                // array item on an uninitialized value
                // void[key] -> void
                cg.EmitPop(cg.CoreTypes.IntStringKey);
                return cg.Emit_PhpValue_Void();
            }
            else
            {
                throw new NotImplementedException($"LOAD {arrtype.Name}[]");
            }
        }

        void IBoundReference.EmitStorePrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            // Template: array[index]

            //
            // ENSURE Array
            //

            var t = InstanceCacheHolder.EmitInstance(instanceOpt, cg, Array);

            if (t.IsOfType(cg.CoreTypes.IPhpArray))
            {
                // ok
            }
            else if (t == cg.CoreTypes.PhpAlias)
            {
                // PhpAlias.EnsureArray
                t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.EnsureArray);
            }
            else if (this.Array.Access.EnsureArray)
            {
                // Array had to be ensured already
                throw new NotImplementedException($"(ensure) STORE {t.Name}[]");
            }
            else if (this.Array.Access.IsQuiet) // semantics of isempty, unset; otherwise in store operation we should EnsureArray
            {
                // WRITE semantics, without need of ensuring the underlaying value

                if (t == cg.CoreTypes.PhpValue)
                {
                    t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.ToArray_PhpValue);
                }
                else if (t == cg.CoreTypes.String)
                {
                    // new PhpString(string)
                    t = cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpString_string);
                }
                else if (t == cg.CoreTypes.Void)
                {
                    // TODO: uninitialized value, report error
                    Debug.WriteLine("Use of uninitialized value.");
                    t = cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpString);
                }
                else
                {
                    throw new NotImplementedException($"(quiet) STORE {t.Name}[]");    // TODO: emit convert as PhpArray
                }
            }
            else
            {
                throw new NotImplementedException($"STORE {t.Name}[]");    // TODO: emit convert as PhpArray
            }

            Debug.Assert(t.IsOfType(cg.CoreTypes.IPhpArray));
            PushEmittedArray(t);

            //
            // LOAD [Index]
            //

            if (this.Index != null)
            {
                cg.EmitIntStringKey(this.Index);    // TODO: save Index into InstanceCacheHolder
            }
        }

        void IBoundReference.EmitStore(CodeGenerator cg, TypeSymbol valueType)
        {
            // Template: array[index]

            var arrtype = PopEmittedArray();
            if (arrtype.IsOfType(cg.CoreTypes.IPhpArray))
            {
                var isphparr = (arrtype == cg.CoreTypes.PhpArray);    // whether the target is instance of PhpArray, otherwise it is an IPhpArray and we have to use .callvirt

                if (Access.IsWriteRef)
                {
                    // PhpAlias
                    if (valueType != cg.CoreTypes.PhpAlias)
                    {
                        cg.EmitConvertToPhpValue(valueType, 0);
                        cg.Emit_PhpValue_MakeAlias();
                    }

                    // .SetItemAlias(key, alias) or .AddValue(PhpValue.Create(alias))
                    if (this.Index != null)
                    {
                        if (isphparr)
                            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.SetItemAlias_IntStringKey_PhpAlias);
                        else
                            cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.SetItemAlias_IntStringKey_PhpAlias);
                    }
                    else
                    {
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Create_PhpAlias);

                        if (isphparr)
                            cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_PhpValue));
                        else
                            cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.AddValue_PhpValue);
                    }
                }
                else if (Access.IsUnset)
                {
                    if (this.Index == null)
                        throw new InvalidOperationException();

                    // .RemoveKey(key)
                    if (isphparr)
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.RemoveKey_IntStringKey);
                    else
                        cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.RemoveKey_IntStringKey);
                }
                else
                {
                    Debug.Assert(Access.IsWrite);

                    cg.EmitConvertToPhpValue(valueType, 0);

                    // .SetItemValue(key, value) or .AddValue(value)
                    if (this.Index != null)
                    {
                        if (isphparr)
                            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.SetItemValue_IntStringKey_PhpValue);
                        else
                            cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.SetItemValue_IntStringKey_PhpValue);
                    }
                    else
                    {
                        if (isphparr)
                            cg.EmitPop(cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Add_PhpValue));
                        else
                            cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.AddValue_PhpValue);
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"STORE {arrtype.Name}[]");
            }
        }

        #endregion
    }

    partial class BoundInstanceOfEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(Access.IsRead || Access.IsNone);

            var type = cg.Emit(Operand);

            //
            if (Access.IsNone)
            {
                cg.EmitPop(type);
                return cg.CoreTypes.Void;
            }

            bool isnull;
            type = cg.EmitAsObject(type, out isnull);
            Debug.Assert(type.IsReferenceType);

            //
            if (AsType.ResolvedType != null)
            {
                if (!isnull)
                {
                    // Template: value is T : object
                    cg.Builder.EmitOpCode(ILOpCode.Isinst);
                    cg.EmitSymbolToken(AsType.ResolvedType, null);

                    // object != null
                    cg.Builder.EmitNullConstant(); // .ldnull
                    cg.Builder.EmitOpCode(ILOpCode.Cgt_un); // .cgt.un
                }
                else
                {
                    cg.EmitPop(type);   // Operand is never an object instance

                    // FALSE
                    cg.Builder.EmitBoolConstant(false);
                }

                //
                return cg.CoreTypes.Boolean;
            }
            else
            {
                AsType.EmitLoadTypeInfo(cg, false);

                // Template: Operators.IsInstanceOf(value, type);
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.IsInstanceOf_Object_PhpTypeInfo);
            }

            throw new NotImplementedException();
        }
    }

    partial class BoundPseudoConst
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            switch (this.Type)
            {
                case PseudoConstUse.Types.File:

                    // <ctx>.RootPath + RelativePath
                    cg.EmitLoadContext();
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.RootPath.Getter);

                    cg.Builder.EmitStringConstant("/" + cg.Routine.ContainingFile.RelativeFilePath);

                    // TODO: normalize slashes
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Concat_String_String)
                        .Expect(SpecialType.System_String);

                case PseudoConstUse.Types.Dir:

                    // <ctx>.RootPath + RelativeDirectory
                    cg.EmitLoadContext();
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.RootPath.Getter);

                    var relative_dir = cg.Routine.ContainingFile.DirectoryRelativePath;
                    if (relative_dir.Length != 0) relative_dir = "/" + relative_dir;

                    cg.Builder.EmitStringConstant(relative_dir);

                    // TODO: normalize slashes
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Concat_String_String)
                        .Expect(SpecialType.System_String);

                default:

                    // the other pseudoconstants should be resolved by flow analysis
                    throw ExceptionUtilities.Unreachable;
            }
        }
    }

    partial class BoundPseudoClassConst
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            switch (this.Type)
            {
                case PseudoClassConstUse.Types.Class:
                    this.TargetType.EmitClassName(cg);
                    return cg.CoreTypes.String;

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.Type);
            }
        }
    }

    partial class BoundGlobalConst
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(!Access.IsWrite);

            if (this.Access.IsNone)
            {
                return cg.CoreTypes.Void;
            }

            if (this.ConstantValue.HasValue)
            {
                return cg.EmitLoadConstant(this.ConstantValue.Value, this.Access.TargetType);
            }

            if (_boundExpressionOpt != null)
            {
                _boundExpressionOpt.EmitLoadPrepare(cg);
                return _boundExpressionOpt.EmitLoad(cg);
            }

            var idxfield = cg.Module.SynthesizedManager
                .GetOrCreateSynthesizedField(cg.Module.ScriptType, cg.CoreTypes.Int32, $"[const]{this.Name}", Accessibility.Internal, true, false);

            // <ctx>.GetConstant(<name>, ref <Index of constant>)
            cg.EmitLoadContext();
            cg.Builder.EmitStringConstant(this.Name);
            cg.EmitFieldAddress(idxfield);
            return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.GetConstant_string_int32)
                .Expect(cg.CoreTypes.PhpValue);
        }
    }

    partial class BoundIsEmptyEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            return Emit(cg, cg.Emit(this.Operand));
        }

        private static TypeSymbol Emit(CodeGenerator cg, TypeSymbol t)
        {
            var il = cg.Builder;

            //
            switch (t.SpecialType)
            {
                case SpecialType.System_Object:
                    // object == null
                    il.EmitNullConstant();
                    il.EmitOpCode(ILOpCode.Ceq);
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Double:
                    // r8 == 0
                    il.EmitDoubleConstant(0.0);
                    il.EmitOpCode(ILOpCode.Ceq);
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Boolean:
                case SpecialType.System_Int32:
                    // i4 == 0
                    cg.EmitLogicNegation();
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Int64:
                    // i8 == 0
                    il.EmitLongConstant(0);
                    il.EmitOpCode(ILOpCode.Ceq);
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_String:
                    // !ToBoolean(string)
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.ToBoolean_String);
                    cg.EmitLogicNegation();
                    return cg.CoreTypes.Boolean;

                default:
                    if (t == cg.CoreTypes.PhpNumber)
                    {
                        // number == 0L
                        il.EmitLongConstant(0);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Eq_number_long)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (t == cg.CoreTypes.PhpAlias)
                    {
                        // <PhpAlias>.Value.get_IsEmpty()
                        cg.Emit_PhpAlias_GetValueAddr();
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.IsEmpty.Getter)
                            .Expect(SpecialType.System_Boolean);
                    }

                    // (value).IsEmpty
                    cg.EmitConvert(t, 0, cg.CoreTypes.PhpValue);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.IsEmpty_PhpValue)
                        .Expect(SpecialType.System_Boolean);
            }
        }
    }

    partial class BoundIsSetEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var end_label = new object();

            var vars = this.VarReferences;
            for (int i = 0; i < vars.Length; i++)
            {
                if (i > 0)
                {
                    cg.Builder.EmitOpCode(ILOpCode.Pop);
                }

                var t = cg.Emit(vars[i]);

                // t.IsSet
                if (t == cg.CoreTypes.PhpValue)
                {
                    // IsSet(value)
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.IsSet_PhpValue);
                }
                else if (t.IsReferenceType)
                {
                    // object != null
                    cg.Builder.EmitNullConstant(); // .ldnull
                    cg.Builder.EmitOpCode(ILOpCode.Cgt_un); // .cgt.un
                }
                else
                {
                    // value type => true
                    cg.EmitPop(t);
                    cg.Builder.EmitBoolConstant(true);
                }

                if (i + 1 < vars.Length)
                {
                    // if (result == false) goto end_label;
                    cg.Builder.EmitOpCode(ILOpCode.Dup);
                    cg.Builder.EmitBranch(ILOpCode.Brfalse, end_label);
                }
            }

            //
            cg.Builder.MarkLabel(end_label);

            //
            return cg.CoreTypes.Boolean;
        }
    }
}
