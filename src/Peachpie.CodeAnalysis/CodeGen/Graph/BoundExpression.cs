using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.CodeGen;
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
                    {
                        bool negation = false;
                        returned_type = EmitEquality(cg, ref negation);

                        Debug.Assert(negation == false);
                    }
                    break;

                case Operations.NotEqual:
                    {
                        bool negation = true;
                        EmitEquality(cg, ref negation);
                        returned_type = cg.CoreTypes.Boolean;

                        if (negation)
                        {
                            cg.EmitLogicNegation();
                        }
                    }
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

                case Operations.Coalesce:
                    returned_type = EmitCoalesce(cg);
                    break;

                case Operations.Spaceship:
                    returned_type = EmitSpaceship(cg);
                    break;

                default:
                    throw cg.NotImplementedException(message: $"BinaryEx {this.Operation} is not implemented.", op: this);
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
        internal static TypeSymbol EmitAdd(CodeGenerator cg, TypeSymbol xtype, BoundExpression right, TypeSymbol resultTypeOpt = null)
        {
            var il = cg.Builder;

            xtype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(xtype));    // int|bool -> long, string -> number
            cg.EmitPhpAliasDereference(ref xtype); // alias -> value

            //
            if (xtype == cg.CoreTypes.PhpNumber)
            {
                var ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));  // int|bool -> long, string -> number
                cg.EmitPhpAliasDereference(ref ytype); // alias -> value

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
                else
                {
                    // number + value : number
                    if (ytype != cg.CoreTypes.PhpValue) { ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_number_value)
                        .Expect(cg.CoreTypes.PhpNumber);
                }

                //
                throw cg.NotImplementedException($"Add(number, {ytype.Name})", right);
            }
            else if (xtype.SpecialType == SpecialType.System_Double)
            {
                var ytype = cg.EmitConvertStringToPhpNumber(cg.EmitExprConvertNumberToDouble(right)); // bool|int|long|number -> double, string -> number

                if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // r8 + r8 : r8
                    il.EmitOpCode(ILOpCode.Add);
                    return cg.CoreTypes.Double;
                }
                else
                {
                    // r8 + value : r8
                    if (ytype != cg.CoreTypes.PhpValue) { ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_double_value)
                        .Expect(SpecialType.System_Double);
                }

                //
                throw cg.NotImplementedException($"Add(double, {ytype.Name})", right);
            }
            else if (xtype.SpecialType == SpecialType.System_Int64)
            {
                var ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));    // int|bool -> long, string -> number
                cg.EmitPhpAliasDereference(ref ytype); // alias -> value

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
                else
                {
                    // i8 + value : number
                    if (ytype != cg.CoreTypes.PhpValue) { ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_long_value)
                        .Expect(cg.CoreTypes.PhpNumber);
                }

                //
                throw cg.NotImplementedException($"Add(int64, {ytype.Name})", right);
            }
            else if (xtype == cg.CoreTypes.PhpArray)
            {
                var ytype = cg.Emit(right);
                cg.EmitPhpAliasDereference(ref ytype); // alias -> value

                if (ytype == cg.CoreTypes.PhpArray)
                {
                    // PhpArray.Union(array, array) : PhpArray
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.Union_PhpArray_PhpArray)
                        .Expect(cg.CoreTypes.PhpArray);
                }
                else
                {
                    // array + value
                    if (ytype != cg.CoreTypes.PhpValue) { ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_array_value);
                }

                //
                throw cg.NotImplementedException($"Add(PhpArray, {ytype.Name})", right);
            }
            else if (xtype == cg.CoreTypes.PhpValue)
            {
                var ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));    // int|bool -> long, string -> number
                cg.EmitPhpAliasDereference(ref ytype); // alias -> value

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
                else
                {
                    // value + value : value
                    if (ytype != cg.CoreTypes.PhpValue) { ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_value_value)
                        .Expect(cg.CoreTypes.PhpValue);
                }

                //
                throw cg.NotImplementedException($"Add(PhpValue, {ytype.Name})", right);
            }
            else
            {
                // x -> PhpValue
                if (xtype != cg.CoreTypes.PhpValue) { xtype = cg.EmitConvertToPhpValue(xtype, right.TypeRefMask); }

                // y -> PhpValue
                cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                var ytype = cg.CoreTypes.PhpValue;

                // value / value : number
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_value_value)
                    .Expect(cg.CoreTypes.PhpValue);


            }

            //
            throw cg.NotImplementedException($"Add({xtype.Name}, ...)", right);
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

            xtype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(xtype));    // int|bool -> int64, string -> number
            TypeSymbol ytype;

            cg.EmitPhpAliasDereference(ref xtype); // alias -> value

            //
            switch (xtype.SpecialType)
            {
                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));
                    cg.EmitPhpAliasDereference(ref ytype); // alias -> value
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
                        // i8 - value : number
                        if (ytype != cg.CoreTypes.PhpValue) { ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_long_value)
                            .Expect(cg.CoreTypes.PhpNumber);
                    }

                case SpecialType.System_Double:
                    ytype = cg.EmitConvertStringToPhpNumber(cg.EmitExprConvertNumberToDouble(right)); // bool|int|long|number -> double, string -> number
                    cg.EmitPhpAliasDereference(ref ytype); // alias -> value
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 - r8 : r8
                        il.EmitOpCode(ILOpCode.Sub);
                        return cg.CoreTypes.Double;
                    }
                    else
                    {
                        // r8 - value : double
                        if (ytype != cg.CoreTypes.PhpValue) { ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_double_value)
                                .Expect(cg.CoreTypes.Double);

                    }

                case SpecialType.System_String:
                    xtype = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.ToNumber_String)
                        .Expect(cg.CoreTypes.PhpNumber);
                    goto default;

                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));
                        cg.EmitPhpAliasDereference(ref ytype); // alias -> value
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
                        else
                        {
                            // number - value : number
                            if (ytype != cg.CoreTypes.PhpValue) { ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_number_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }

                        throw cg.NotImplementedException($"Sub(PhpNumber, {ytype.Name})", right);
                    }
                    else if (xtype == cg.CoreTypes.PhpValue)
                    {
                        ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));
                        cg.EmitPhpAliasDereference(ref ytype); // alias -> value

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
                        else
                        {
                            // value - value : number
                            if (ytype != cg.CoreTypes.PhpValue) { ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_value_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }

                        throw cg.NotImplementedException($"Sub(PhpValue, {ytype.Name})", right);
                    }
                    else
                    {
                        // x -> PhpValue
                        if (xtype != cg.CoreTypes.PhpValue) { xtype = cg.EmitConvertToPhpValue(xtype, right.TypeRefMask); }

                        // y -> PhpValue
                        cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                        ytype = cg.CoreTypes.PhpValue;

                        // value / value : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_value_value)
                            .Expect(cg.CoreTypes.PhpNumber);

                    }

                    throw cg.NotImplementedException($"Sub({xtype.Name},...)", right);
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
                    else
                    {
                        cg.EmitConvert(xtype, 0, cg.CoreTypes.PhpValue);
                        cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BitwiseAnd_PhpValue_PhpValue)
                            .Expect(cg.CoreTypes.PhpValue);

                    }
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
                    else
                    {
                        cg.EmitConvert(xtype, 0, cg.CoreTypes.PhpValue);
                        cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BitwiseOr_PhpValue_PhpValue)
                            .Expect(cg.CoreTypes.PhpValue);

                    }
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
                    else
                    {
                        cg.EmitConvert(xtype, 0, cg.CoreTypes.PhpValue);
                        cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BitwiseXor_PhpValue_PhpValue)
                            .Expect(cg.CoreTypes.PhpValue);
                    }

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

        /// <summary>Emits <c>??</c> operator and returns the result type.</summary>
        TypeSymbol EmitCoalesce(CodeGenerator cg) => EmitCoalesce(cg, cg.Emit(this.Left), this.Left.TypeRefMask, this.Right);

        internal static TypeSymbol EmitCoalesce(CodeGenerator cg, TypeSymbol left_type, FlowAnalysis.TypeRefMask left_type_mask, BoundExpression right)
        {
            // Left ?? Right

            if (!cg.CanBeNull(left_type)) // in case we truly believe in our type analysis: || !cg.CanBeNull(this.Left.TypeRefMask))
            {
                return left_type;
            }

            object trueLbl = new object();
            object endLbl = new object();

            // <stack> = <left_var> = Left
            var left_var = cg.GetTemporaryLocal(left_type);
            cg.Builder.EmitOpCode(ILOpCode.Dup);
            cg.Builder.EmitLocalStore(left_var);

            cg.EmitNotNull(left_type, left_type_mask);
            cg.Builder.EmitBranch(ILOpCode.Brtrue, trueLbl);

            // false:
            var right_type = cg.Emit(right);
            var result_type = cg.DeclaringCompilation.Merge(left_type, right_type);
            cg.EmitConvert(right_type, right.TypeRefMask, result_type);
            cg.Builder.EmitBranch(ILOpCode.Br, endLbl);
            cg.Builder.AdjustStack(-1);

            // trueLbl:
            cg.Builder.MarkLabel(trueLbl);
            cg.Builder.EmitLocalLoad(left_var);
            cg.EmitConvert(left_type, left_type_mask, result_type);

            // endLbl:
            cg.Builder.MarkLabel(endLbl);

            //
            cg.ReturnTemporaryLocal(left_var);

            //
            return result_type;
        }

        /// <summary>Emits the spaceship `&lt;=&gt;` operation.</summary>
        TypeSymbol EmitSpaceship(CodeGenerator cg)
        {
            // TODO: return strictly -1, 0, +1 (.NET compare operation returns number in range: < 0, 0, > 0
            // TODO: optimize for specific type of operands (mostly string, long)

            cg.EmitConvertToPhpValue(Left);
            cg.EmitConvertToPhpValue(Right);

            // Template: Comparison.Compare( <Left>, <Right> ) : i4

            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_value_value)
                .Expect(SpecialType.System_Int32);
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
            il.AdjustStack(-1);

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
        TypeSymbol EmitEquality(CodeGenerator cg, ref bool negation)
        {
            // x == y
            return EmitEquality(cg, Left, Right, ref negation);
        }

        /// <summary>
        /// Emits check for values equality.
        /// Lefts <c>bool</c> on top of evaluation stack.
        /// </summary>
        internal static TypeSymbol EmitEquality(CodeGenerator cg, TypeSymbol xtype, BoundExpression right)
        {
            bool negation = false;  // unused
            return EmitEquality(cg, xtype, right, ref negation);
        }

        /// <summary>
        /// Emits check for values equality.
        /// Lefts <c>bool</c> on top of evaluation stack.
        /// </summary>
        internal static TypeSymbol EmitEquality(CodeGenerator cg, BoundExpression left, BoundExpression right, ref bool negation)
        {
            if (left.ConstantValue.IsNull())
            {
                // null == right
                return EmitEqualityToNull(cg, right, ref negation);
            }
            else if (right.ConstantValue.IsNull())
            {
                // left == null
                return EmitEqualityToNull(cg, left, ref negation);
            }
            else
            {
                // left == right
                return EmitEquality(cg, cg.Emit(left), right, ref negation);
            }
        }

        static TypeSymbol EmitEqualityToNull(CodeGenerator cg, BoundExpression expr, ref bool negation)
        {
            // Template: <expr> == null

            var il = cg.Builder;
            var t = cg.Emit(expr);

            //
            switch (t.SpecialType)
            {
                case SpecialType.System_Object:
                    il.EmitNullConstant();
                    if (negation)
                    {
                        // object != null
                        il.EmitOpCode(ILOpCode.Cgt_un);
                        negation = false;   // handled
                    }
                    else
                    {
                        // object == null
                        il.EmitOpCode(ILOpCode.Ceq);
                    }
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
                    if (negation)
                    {
                        il.EmitOpCode(ILOpCode.Cgt_un);
                        negation = false; // handled
                    }
                    else
                    {
                        il.EmitOpCode(ILOpCode.Ceq);
                    }
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
        internal static TypeSymbol EmitEquality(CodeGenerator cg, TypeSymbol xtype, BoundExpression right, ref bool negation)
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
                    else if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // i8 == number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Eq_long_number)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else
                    {
                        // value
                        ytype = cg.EmitConvertToPhpValue(ytype, 0);

                        // compare(i8, value) == 0
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_long_value);
                        cg.EmitLogicNegation();

                        return cg.CoreTypes.Boolean;
                    }


                case SpecialType.System_Double:

                    ytype = cg.EmitExprConvertNumberToDouble(right);  // bool|long|int -> double

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
                    else
                    {
                        // value
                        ytype = cg.EmitConvertToPhpValue(ytype, 0);

                        // compare(double, value) == 0
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_double_value);
                        cg.EmitLogicNegation();

                        return cg.CoreTypes.Boolean;
                    }


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
                        // Ceq(string, string)
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Ceq_string_string)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else
                    {
                        // value
                        ytype = cg.EmitConvertToPhpValue(ytype, 0);

                        if (negation)
                        {
                            // string != value
                            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Ineq_String_PhpValue)
                                .Expect(SpecialType.System_Boolean);
                            negation = false;   // handled
                        }
                        else
                        {
                            // string == value
                            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Eq_String_PhpValue)
                                .Expect(SpecialType.System_Boolean);
                        }
                        return cg.CoreTypes.Boolean;
                    }

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

                        ytype = cg.Emit(right);
                        switch (ytype.SpecialType)
                        {
                            case SpecialType.System_String:
                                if (negation)
                                {
                                    negation = false;   // handled
                                    // value == string
                                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Ineq_PhpValue_String)
                                        .Expect(SpecialType.System_Boolean);
                                }
                                else
                                {
                                    // value == string
                                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Eq_PhpValue_String)
                                        .Expect(SpecialType.System_Boolean);
                                }

                            // TODO: more types on right

                            default:
                                ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask);

                                if (negation)
                                {
                                    negation = false; // handled
                                    // value == value
                                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Ineq_PhpValue_PhpValue)
                                        .Expect(SpecialType.System_Boolean);
                                }
                                else
                                {
                                    // value == value
                                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Eq_PhpValue_PhpValue)
                                        .Expect(SpecialType.System_Boolean);
                                }
                        }
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
                        if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
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
                        // i8 === i8
                        cg.Builder.EmitOpCode(ILOpCode.Ceq);
                        return cg.CoreTypes.Boolean;
                    }
                    else if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // i8 === number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_long_PhpNumber)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (
                        ytype.SpecialType == SpecialType.System_Boolean ||
                        ytype.SpecialType == SpecialType.System_String ||
                        ytype.SpecialType == SpecialType.System_Double ||
                        ytype.IsOfType(cg.CoreTypes.IPhpArray) ||
                        ytype == cg.CoreTypes.Object ||
                        ytype == cg.CoreTypes.PhpString)
                    {
                        // i8 === something else => false
                        cg.EmitPop(ytype);
                        cg.EmitPop(xtype);
                        cg.Builder.EmitBoolConstant(false);
                        return cg.CoreTypes.Boolean;
                    }
                    else
                    {
                        // i8 === PhpValue
                        if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
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
                    else if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // r8 === number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_double_PhpNumber)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (
                        ytype.SpecialType == SpecialType.System_Boolean ||
                        ytype.SpecialType == SpecialType.System_Int32 ||
                        ytype.SpecialType == SpecialType.System_Int64 ||
                        ytype.SpecialType == SpecialType.System_String ||
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
                        if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_double_PhpValue)
                            .Expect(SpecialType.System_Boolean);
                    }

                case SpecialType.System_String:
                    // string === RValue
                    ytype = cg.Emit(right);
                    if (ytype.SpecialType == SpecialType.System_String)
                    {
                        // string === string
                        return cg.EmitCall(ILOpCode.Call, (MethodSymbol)cg.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_String__op_Equality));
                    }
                    else if (
                        ytype.SpecialType == SpecialType.System_Boolean ||
                        ytype.SpecialType == SpecialType.System_Int32 ||
                        ytype.SpecialType == SpecialType.System_Int64 ||
                        ytype.SpecialType == SpecialType.System_Double ||
                        ytype.IsOfType(cg.CoreTypes.IPhpArray))
                    {
                        // string == something else => false
                        cg.EmitPop(ytype);
                        cg.EmitPop(xtype);
                        cg.Builder.EmitBoolConstant(false);
                        return cg.CoreTypes.Boolean;
                    }
                    else if (ytype.SpecialType == SpecialType.System_Object && right.ConstantValue.IsNull())
                    {
                        // comparison to NULL
                        // string === NULL
                        // Template: ReferenceEquals( string, object )
                        return cg.EmitCall(ILOpCode.Call, (MethodSymbol)cg.DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Object__ReferenceEquals))
                            .Expect(SpecialType.System_Boolean);
                    }
                    else
                    {
                        // string === value
                        ytype = cg.EmitConvertToPhpValue(ytype, 0);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_string_PhpValue)
                            .Expect(SpecialType.System_Boolean);
                    }

                default:

                    // === NULL
                    if (right.ConstantValue.IsNull())
                    {
                        if (xtype.IsReferenceType && xtype != cg.CoreTypes.PhpAlias)
                        {
                            // Template: <STACK> == null
                            cg.Builder.EmitNullConstant();
                            cg.Builder.EmitOpCode(ILOpCode.Ceq);
                            return cg.CoreTypes.Boolean;
                        }

                        // StrictCeqNull( <VALUE> )
                        xtype = cg.EmitConvertToPhpValue(xtype, 0);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeqNull_PhpValue)
                            .Expect(SpecialType.System_Boolean);
                    }

                    // TODO: PhpArray, Object === ...

                    xtype = cg.EmitConvertToPhpValue(xtype, 0);
                    ytype = cg.Emit(right);

                    if (ytype.SpecialType == SpecialType.System_Boolean)
                    {
                        // PhpValue == bool
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_PhpValue_bool)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else if (ytype.SpecialType == SpecialType.System_String)
                    {
                        // value === string
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.StrictCeq_PhpValue_string)
                            .Expect(SpecialType.System_Boolean);
                    }
                    else
                    {
                        if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }

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
        /// <returns>Resulting type code pushed onto the top of evaluation stack.</returns>
        internal static TypeSymbol EmitLtGt(CodeGenerator cg, TypeSymbol xtype, BoundExpression right, bool lt)
        {
            TypeSymbol ytype;
            var il = cg.Builder;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Void:
                    ytype = cg.Emit(right);    // bool|int -> long

                    // Template: Operators.CompareNull(value)
                    if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.CompareNull_value);

                    // {comparison }<> 0
                    il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                    il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    return cg.CoreTypes.Boolean;

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
                    else if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // i8 <> number
                        return cg.EmitCall(ILOpCode.Call, lt
                            ? cg.CoreMethods.PhpNumber.lt_long_number
                            : cg.CoreMethods.PhpNumber.gt_long_number);
                    }
                    else
                    {
                        if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }

                        // compare(i8, value) <> 0
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_long_value);

                        il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);
                        il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    }
                    return cg.CoreTypes.Boolean;

                case SpecialType.System_Double:
                    ytype = cg.EmitExprConvertNumberToDouble(right);    // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 <> r8
                        il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    }
                    else
                    {
                        // compare(r8, value)
                        if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
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
                        if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
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
                            if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }

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
                        // TODO: if (ytype.SpecialType == SpecialType.System_String) ...
                        // TODO: if (ytype.SpecialType == SpecialType.System_Double) ...

                        if (ytype.SpecialType == SpecialType.System_Int64 || ytype.SpecialType == SpecialType.System_Int32)
                        {
                            // compare(value, i8)
                            ytype = cg.EmitConvertIntToLong(ytype);
                            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_value_long);
                        }
                        else
                        {
                            // compare(value, value)
                            if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Compare_value_value);
                        }

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

            xtype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(xtype));    // int|bool -> int64, string -> number

            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Double:
                    ytype = cg.EmitExprConvertNumberToDouble(right); // bool|int|long|number -> double
                    if (ytype == cg.CoreTypes.PhpAlias)
                    {
                        // PhpAlias -> PhpValue
                        ytype = cg.Emit_PhpAlias_GetValue();
                    }

                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 * r8 : r8
                        il.EmitOpCode(ILOpCode.Mul);
                        return xtype;   // r8
                    }
                    else
                    {
                        // r8 * value : r8
                        if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_double_value)
                                .Expect(SpecialType.System_Double);
                    }
                    //
                    throw cg.NotImplementedException($"Mul(double, {ytype.Name})", right);

                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));
                    if (ytype == cg.CoreTypes.PhpAlias)
                    {
                        // PhpAlias -> PhpValue
                        ytype = cg.Emit_PhpAlias_GetValue();
                    }

                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // i8 * i8 : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_long_long)
                                .Expect(cg.CoreTypes.PhpNumber);
                    }
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // i8 * r8 : r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_long_double)
                                .Expect(SpecialType.System_Double);
                    }
                    if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // i8 * number : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_long_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                    }
                    else
                    {
                        // i8 * value : number
                        if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_long_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                    }
                    //
                    throw cg.NotImplementedException($"Mul(int64, {ytype.Name})", right);

                default:

                    if (xtype == cg.CoreTypes.PhpAlias)
                    {
                        // dereference:
                        xtype = cg.Emit_PhpAlias_GetValue();
                    }

                    //

                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));

                        if (ytype == cg.CoreTypes.PhpAlias)
                        {
                            // PhpAlias -> PhpValue
                            ytype = cg.Emit_PhpAlias_GetValue();
                        }

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
                        else
                        {
                            // number * value : number
                            if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_number_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }

                        //
                        throw cg.NotImplementedException($"Mul(PhpNumber, {ytype.Name})", right);
                    }
                    else if (xtype == cg.CoreTypes.PhpValue)
                    {
                        ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));    // bool|int -> long, string -> number
                        if (ytype == cg.CoreTypes.PhpAlias)
                        {
                            // PhpAlias -> PhpValue
                            ytype = cg.Emit_PhpAlias_GetValue();
                        }

                        if (ytype == cg.CoreTypes.Long)
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
                        else if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // value * number : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_value_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else
                        {
                            // value * value : number
                            if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_value_value)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        //
                        throw cg.NotImplementedException($"Mul(PhpValue, {ytype.Name})", right);
                    }
                    else
                    {
                        // x -> PhpValue
                        if (xtype != cg.CoreTypes.PhpValue) { xtype = cg.EmitConvertToPhpValue(xtype, right.TypeRefMask); }

                        // y -> PhpValue
                        cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                        ytype = cg.CoreTypes.PhpValue;

                        // value / value : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_value_value)
                            .Expect(cg.CoreTypes.PhpNumber);

                    }


                    //
                    throw cg.NotImplementedException($"Mul({xtype.Name}, ...)", right);
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

            xtype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(xtype));    // int|bool -> int64, string -> number

            if (xtype == cg.CoreTypes.PhpAlias)
            {
                // PhpAlias -> PhpValue
                xtype = cg.Emit_PhpAlias_GetValue();
            }

            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Double:
                    ytype = cg.EmitExprConvertNumberToDouble(right); // bool|int|long|number -> double
                    if (ytype == cg.CoreTypes.PhpAlias)
                    {
                        // PhpAlias -> PhpValue
                        ytype = cg.Emit_PhpAlias_GetValue();
                    }

                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        il.EmitOpCode(ILOpCode.Div);
                        return xtype;   // r8
                    }
                    else
                    {
                        // double / value : double
                        cg.EmitConvertToPhpValue(ytype, 0);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Div_double_PhpValue);
                    }


                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertIntToLong(cg.Emit(right));  // bool|int -> long
                    if (ytype == cg.CoreTypes.PhpAlias)
                    {
                        // PhpAlias -> PhpValue
                        ytype = cg.Emit_PhpAlias_GetValue();
                    }

                    if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // long / number : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Division_long_number)
                            .Expect(cg.CoreTypes.PhpNumber);
                    }
                    else
                    {
                        // long / value : number
                        cg.EmitConvertToPhpValue(ytype, 0);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Div_long_PhpValue);
                    }


                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertStringToPhpNumber(cg.EmitConvertIntToLong(cg.Emit(right)));  // bool|int -> long, string -> number
                        if (ytype == cg.CoreTypes.PhpAlias)
                        {
                            // PhpAlias -> PhpValue
                            ytype = cg.Emit_PhpAlias_GetValue();
                        }

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
                        else
                        {
                            // number / value : number
                            if (ytype != cg.CoreTypes.PhpValue) { cg.EmitConvertToPhpValue(ytype, right.TypeRefMask); }
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Division_number_value);
                        }

                        //
                        throw cg.NotImplementedException($"Div(number, {ytype.Name})", right);
                    }
                    else
                    {
                        // x -> PhpValue
                        if (xtype != cg.CoreTypes.PhpValue) { xtype = cg.EmitConvertToPhpValue(xtype, right.TypeRefMask); }

                        // y -> PhpValue
                        cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                        ytype = cg.CoreTypes.PhpValue;

                        // value / value : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Div_PhpValue_PhpValue);

                    }

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
                    else
                    {
                        // y -> PhpValue
                        cg.EmitConvert(ytype, right.TypeRefMask, cg.CoreTypes.PhpValue);
                        ytype = cg.CoreTypes.PhpValue;

                        // i8 ** value : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_long_value);
                    }

                case SpecialType.System_Double:
                    ytype = cg.EmitExprConvertNumberToDouble(right);    // int|bool|long|number -> double

                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 ** r8 : r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_double_double);
                    }
                    else
                    {
                        // y -> PhpValue
                        cg.EmitConvert(ytype, right.TypeRefMask, cg.CoreTypes.PhpValue);
                        ytype = cg.CoreTypes.PhpValue;

                        // r8 ** value : r8
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_double_value);
                    }

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
                        else
                        {
                            // y -> PhpValue
                            ytype = cg.EmitConvertToPhpValue(ytype, right.TypeRefMask);

                            // number ** value : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_number_value);
                        }
                    }
                    else
                    {
                        // x -> PhpValue
                        xtype = cg.EmitConvertToPhpValue(xtype, xtype_hint);

                        // y -> PhpValue
                        cg.EmitConvert(right, cg.CoreTypes.PhpValue);
                        ytype = cg.CoreTypes.PhpValue;

                        // value ** value : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Pow_value_value);
                    }

            }
        }
    }

    partial class BoundUnaryEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(Access.IsRead || Access.IsNone, "Access cannot be " + Access.ToString());

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
                    cg.EmitConvertToBool(this.Operand);
                    cg.EmitLogicNegation();
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
                // Debug.Assert(returned_type.SpecialType != SpecialType.System_Void, "returns void, operation: " + this.Operation.ToString() + ", file: " + cg.ContainingFile.RelativeFilePath);
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
                    throw new NotImplementedException();    // ERR
                default:
                    if (t == cg.CoreTypes.PhpArray)
                    {
                        throw new NotImplementedException(); // ERR
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

    partial class BoundConversionEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            // emit explicit 'cast' operation

            var t = cg.Emit(this.Operand);

            var target = (TypeSymbol)this.TargetType.ResolveTypeSymbol(cg.DeclaringCompilation);
            if (target.IsErrorTypeOrNull())
            {
                throw cg.NotImplementedException(op: this);
            }

            var conv = cg.DeclaringCompilation.ClassifyExplicitConversion(t, target);
            if (conv.Exists == false)
            {

            }

            cg.EmitConversion(conv, t, target);

            //

            if (Access.IsNone)
            {
                cg.EmitPop(target);
                return cg.CoreTypes.Void;
            }

            return target;
        }
    }

    partial class BoundCallableConvert
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var m = TargetCallable as MethodSymbol;

            if ((m != null && m.IsValidMethod()) ||
                (m is AmbiguousMethodSymbol a && a.IsOverloadable && a.Ambiguities.Length != 0))
            {
                if (m.IsStatic)
                {
                    return m.EmitLoadRoutineInfo(cg);
                }
                else
                {
                    Debug.Assert(Receiver != null);

                    // PhpCallback.Create((object)Receiver, methodRoutineInfo)
                    cg.EmitConvert(Receiver, cg.CoreTypes.Object);
                    m.EmitLoadRoutineInfo(cg);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BindTargetToMethod_Object_RoutineInfo)
                        .Expect(cg.CoreTypes.IPhpCallable);
                }
            }

            // generic conversion to IPhpCallable:
            var target = (TypeSymbol)this.TargetType.ResolveTypeSymbol(cg.DeclaringCompilation);    // always IPhpCallable

            // 
            cg.EmitConvert(this.Operand, target);
            return target;
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

    partial class BoundCopyValue
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var expr = this.Expression;
            var t = cg.Emit(expr);

            if (expr.IsDeeplyCopied)
            {
                // dereference
                if (expr.TypeRefMask.IsRef)
                {
                    t = cg.EmitDereference(t);
                }

                // copy
                t = cg.EmitDeepCopy(t, expr.TypeRefMask);
            }

            //
            return t;
        }
    }

    partial class BoundReferenceExpression
    {
        /// <summary>
        /// Gets <see cref="IVariableReference"/> providing load and store operations.
        /// </summary>
        internal abstract IVariableReference BindPlace(CodeGenerator cg);

        internal abstract IPlace Place();

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

            return this.BindPlace(cg).EmitLoadValue(cg, Access);
        }
    }

    partial class BoundVariableName
    {
        /// <summary>
        /// Emits the name of variable leaving <c>string</c> on top of evaluation stack.
        /// </summary>
        internal TypeSymbol EmitVariableName(CodeGenerator cg)
        {
            if (this.IsDirect)
            {
                cg.Builder.EmitStringConstant(this.NameValue.Value);
            }
            else
            {
                cg.EmitConvert(this.NameExpression, cg.CoreTypes.String);
            }

            //
            return cg.CoreTypes.String;
        }

        internal void EmitIntStringKey(CodeGenerator cg)
        {
            if (this.IsDirect)
            {
                cg.EmitIntStringKey(this.NameValue.Value);
            }
            else
            {
                cg.EmitIntStringKey(this.NameExpression);
            }
        }
    }

    partial class BoundVariableRef
    {
        internal override IVariableReference BindPlace(CodeGenerator cg) => this.Variable; // .BindPlace(cg.Builder, this.Access, this.BeforeTypeRef);

        internal override IPlace Place() => this.Variable.Place;
    }

    partial class BoundListEx : IVariableReference
    {
        internal override IVariableReference BindPlace(CodeGenerator cg) => this;

        internal override IPlace Place() => null;

        /// <summary>
        /// Emits conversion to <c>IPhpArray</c>.
        /// Emits empty array on top of stack if object cannot be used as array.
        /// </summary>
        static TypeSymbol/*!*/EmitListAccess(CodeGenerator cg, TypeSymbol valueType)
        {
            Debug.Assert(valueType != null);

            if (valueType.IsReferenceType)
            {
                if (valueType.IsOfType(cg.CoreTypes.IPhpArray))
                {
                    return valueType; // keep value on stack
                }

                if (valueType.IsOfType(cg.CoreTypes.ArrayAccess))
                {
                    // Template: EnsureArray( ArrayAccess) : IPhpArray
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureArray_ArrayAccess);
                }
            }

            // Template: Operators: GetListAccess( (PhpValue)value )
            cg.EmitConvertToPhpValue(valueType, 0);
            return cg.EmitCall(ILOpCode.Call, cg.CoreTypes.Operators.Method("GetListAccess", cg.CoreTypes.PhpValue));
        }

        static void EmitItemAssign(CodeGenerator cg, KeyValuePair<BoundExpression, BoundReferenceExpression> item, long index, IPlace arrplace)
        {
            var target = item;
            if (target.Value == null)
            {
                return;
            }

            // Template: <vars[i]> = <tmp>[i]

            var boundtarget = target.Value.BindPlace(cg);
            var lhs = boundtarget.EmitStorePreamble(cg, target.Value.TargetAccess());

            // LOAD IPhpArray.GetItemValue(IntStringKey{i})
            arrplace.EmitLoad(cg.Builder);
            if (target.Key == null)
            {
                cg.EmitIntStringKey(index);
            }
            else
            {
                cg.EmitIntStringKey(target.Key);
            }

            // GetItemVaue
            var itemtype = cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.GetItemValue_IntStringKey);

            // dereference
            itemtype = cg.EmitDereference(itemtype);

            // copy
            itemtype = cg.EmitDeepCopy(itemtype, nullcheck: true);

            // STORE vars[i]
            boundtarget.EmitStore(cg, ref lhs, itemtype, target.Value.Access);
            lhs.Dispose();
        }

        #region IVariableReference

        Symbol IVariableReference.Symbol => null;

        TypeSymbol IVariableReference.Type => null; // throw new NotImplementedException();

        bool IVariableReference.HasAddress => false;

        IPlace IVariableReference.Place => null;

        LhsStack IVariableReference.EmitStorePreamble(CodeGenerator cg, BoundAccess access)
        {
            // nada
            return default;
        }

        void IVariableReference.EmitStore(CodeGenerator cg, ref LhsStack lhs, TypeSymbol stack, BoundAccess access)
        {
            var rtype = EmitListAccess(cg, stack);

            var tmp = cg.GetTemporaryLocal(rtype);
            cg.Builder.EmitLocalStore(tmp);

            var items = this.Items;

            // NOTE: since PHP7, variables are assigned from left to right

            for (int i = 0; i < items.Length; i++)
            {
                EmitItemAssign(cg, items[i], i, new LocalPlace(tmp));
            }

            //
            cg.ReturnTemporaryLocal(tmp);
        }

        TypeSymbol IVariableReference.EmitLoadValue(CodeGenerator cg, ref LhsStack lhsStack, BoundAccess access)
        {
            throw new InvalidOperationException();
        }

        TypeSymbol IVariableReference.EmitLoadAddress(CodeGenerator cg, ref LhsStack lhsStack)
        {
            throw new InvalidOperationException();
        }

        #endregion
    }

    partial class BoundFieldRef
    {
        internal IVariableReference BoundReference { get; set; }

        internal override IVariableReference BindPlace(CodeGenerator cg) => BoundReference;

        internal override IPlace Place() => BoundReference?.Place;
    }

    partial class BoundRoutineCall
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            EmitBeforeCall(cg);

            if (TargetMethod.IsValidMethod())
            {
                Debug.Assert(!TargetMethod.IsUnreachable);
                // the most preferred case when method is known,
                // the method can be called directly
                return EmitDirectCall(cg, IsVirtualCall ? ILOpCode.Callvirt : ILOpCode.Call, TargetMethod, (BoundTypeRef)LateStaticTypeRef);
            }
            else if (TargetMethod is MagicCallMethodSymbol magic && !this.HasArgumentsUnpacking)
            {
                return EmitMagicCall(cg, magic.OriginalMethodName, magic.RealMethod, (BoundTypeRef)LateStaticTypeRef);
            }
            else
            {
                //
                return EmitDynamicCall(cg);
            }
        }

        internal virtual void EmitBeforeCall(CodeGenerator cg)
        {

        }

        /// <summary>
        /// Emits the routine call in case the method symbol couldn't be resolved or it cannot be called directly.
        /// </summary>
        internal virtual TypeSymbol EmitDynamicCall(CodeGenerator cg)
        {
            return EmitCallsiteCall(cg);
        }

        internal TypeSymbol EmitDirectCall(CodeGenerator cg, ILOpCode opcode, MethodSymbol method, BoundTypeRef staticType = null)
        {
            // TODO: in case of a global user routine -> emit check the function is declared
            // <ctx>.AssertFunctionDeclared

            var stacktype = this.HasArgumentsUnpacking
                ? cg.EmitCall_UnpackingArgs(opcode, method, this.Instance, _arguments, staticType)  // call method with respect to argument unpacking
                : cg.EmitCall(opcode, method, this.Instance, _arguments, staticType);               // call method and pass provided arguments as they are

            //
            return (this.ResultType = cg.EmitMethodAccess(stacktype, method, Access));
        }

        /// <summary>
        /// Determines if the target magic method will be called using standard calling convention.
        /// </summary>
        static bool IsClrMagicCall(MethodSymbol method)
        {
            if (method.ContainingType.IsPhpType())
            {
                // defined in PHP source, use PHP calling convention
                return false;
            }

            var parameters = method.Parameters;
            if (parameters.Last().IsParams)
            {
                return true;
            }

            //var nimplicit = parameters.TakeWhile(p => p.IsImplicitlyDeclared).Count();

            //var actualparameters = parameters.Length - nimplicit;
            //if (actualparameters != 2)
            //{
            //    return true;
            //}

            //if (!parameters.Last().Type.Is_PhpArray() &&
            //    !parameters.Last().Type.Is_PhpValue())
            //{
            //    return true;
            //}

            // regular PHP semantic:
            // __call(name, PhpArray arguments)
            return false;
        }

        internal TypeSymbol EmitMagicCall(CodeGenerator cg, string originalMethodName, MethodSymbol method, BoundTypeRef staticType = null)
        {
            // call to __callStatic() or __call()
            Debug.Assert(
                string.Equals(method.Name, Name.SpecialMethodNames.Call.Value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method.Name, Name.SpecialMethodNames.CallStatic.Value, StringComparison.OrdinalIgnoreCase));

            if (this.HasArgumentsUnpacking)
            {
                throw cg.NotImplementedException("__callStatic() with Arguments Unpacking", this);
            }

            ImmutableArray<BoundArgument> realArguments;

            var boundname = BoundArgument.Create(new BoundLiteral(originalMethodName).WithAccess(BoundAccess.Read));

            if (IsClrMagicCall(method))
            {
                // method is CLR method with params => don't pack arguments into phparray and call method normally
                // first argument is the method name:
                realArguments = _arguments.Insert(0, boundname);
            }
            else
            {
                // PHP behavior
                realArguments = ImmutableArray.Create(
                    // $name: string
                    boundname,
                    // $arguments: PhpArray
                    BoundArgument.Create(
                        new BoundArrayEx(
                            _arguments.Select(
                                arg => new KeyValuePair<BoundExpression, BoundExpression>(null, arg.Value)
                            ).ToImmutableArray())
                        .WithAccess(BoundAccess.Read))
                    );
            }

            //
            var opcode = (method.IsVirtual || IsVirtualCall) ? ILOpCode.Callvirt : ILOpCode.Call;

            //
            var stackType = cg.EmitCall(opcode, method, this.Instance, realArguments, staticType);

            return cg.EmitMethodAccess(stackType, method, Access);
        }

        protected virtual bool IsVirtualCall => true;

        /// <summary>Type reference to the static type. The containing type of called routine, e.g. <c>THE_TYPE::foo()</c>. Used for direct method call requiring late static type..</summary>
        protected virtual IBoundTypeRef LateStaticTypeRef => null;

        #region Emit CallSite

        protected virtual bool CallsiteRequiresCallerContext => false;
        protected virtual string CallsiteName => null;
        protected virtual BoundExpression RoutineNameExpr => null;
        protected virtual IBoundTypeRef RoutineTypeRef => null;

        /// <summary>
        /// Optional. Emits instance on which the method is invoked.
        /// In case of instance function call, it is the instance expression,
        /// in case of static method, it is reference to <c>$this</c> which may be needed in some cases.
        /// </summary>
        /// <returns>Type left on stack. Can be <c>null</c> if callsite does not expect a target.</returns>
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

        /// <summary>
        /// Emits <c>System.Type[]</c> of type arguments.
        /// </summary>
        internal TypeSymbol EmitTypeArgumentsArray(CodeGenerator cg)
        {
            var system_type = cg.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type);

            if (this.TypeArguments.IsDefaultOrEmpty)
            {
                // most common case
                return cg.Emit_EmptyArray(system_type);
            }
            else
            {
                return cg.Emit_NewArray(
                    system_type,
                    this.TypeArguments,
                    tref =>
                    {
                        var t = (BoundTypeRef)tref;
                        if (t.ResolvedType.IsErrorTypeOrNull())
                        {
                            throw ExceptionUtilities.NotImplementedException(cg, "type argument has not been resolved", this);
                        }

                        return cg.EmitSystemType(t.ResolvedType);
                    });
            }
        }

        internal TypeSymbol EmitCallsiteCall(CodeGenerator cg)
        {
            // callsite

            var callsite = cg.Factory.StartCallSite("call_" + this.CallsiteName);

            // LOAD callsite.Target
            callsite.EmitLoadTarget();

            // LOAD callsite arguments

            // (callsite, ctx, [target], [name], ...)
            callsite.EmitLoadCallsite();                // callsite
            callsite.EmitTargetInstance(EmitTarget);    // [target]
            callsite.EmitTargetTypeParam(RoutineTypeRef);// [target_type] : PhpTypeInfo
            callsite.EmitLateStaticTypeParam(LateStaticTypeRef);    // [late_static] : PhpTypeInfo
            callsite.EmitNameParam(RoutineNameExpr);    // [name] : string
            callsite.EmitLoadContext();                 // ctx : Context

            if (this.TypeArguments.IsDefaultOrEmpty == false)
            {
                callsite.AddArg(EmitTypeArgumentsArray(cg), false); // typeargs : System.Type[]
            }

            if (CallsiteRequiresCallerContext)
            {
                callsite.EmitCallerTypeParam();         // [class_ctx] : RuntimeTypeHandle
            }

            callsite.EmitArgs(_arguments);              // ...

            // RETURN TYPE:
            var return_type = this.Access.IsRead
                    ? this.Access.IsReadRef ? cg.CoreTypes.PhpAlias.Symbol
                    : this.Access.EnsureArray ? cg.CoreTypes.IPhpArray.Symbol
                    : this.Access.EnsureObject ? cg.CoreTypes.Object.Symbol
                    : (this.Access.TargetType ?? cg.CoreTypes.PhpValue.Symbol)
                : cg.CoreTypes.Void.Symbol;

            // Target()
            var functype = cg.Factory.GetCallSiteDelegateType(
                null, RefKind.None,
                callsite.Arguments,
                callsite.ArgumentsRefKinds,
                null,
                return_type);

            cg.EmitCall(ILOpCode.Callvirt, functype.DelegateInvokeMethod);

            // Create CallSite ...
            callsite.Construct(functype, cctor_cg => BuildCallsiteCreate(cctor_cg, return_type));

            //
            return return_type;
        }

        internal virtual void BuildCallsiteCreate(CodeGenerator cg, TypeSymbol returntype) { throw new InvalidOperationException(); }

        #endregion
    }

    partial class BoundGlobalFunctionCall
    {
        protected override string CallsiteName => _name.IsDirect ? _name.NameValue.ToString() : null;
        protected override BoundExpression RoutineNameExpr => _name.NameExpression;
        protected override bool IsVirtualCall => false;

        internal override TypeSymbol EmitDynamicCall(CodeGenerator cg)
        {
            if (_name.IsDirect)
            {
                return EmitCallsiteCall(cg);
            }
            else
            {
                Debug.Assert(_name.NameExpression != null);

                // better to use PhpCallback.Invoke instead of call sites

                // Template: NameExpression.AsCallback().Invoke(Context, PhpValue[])

                cg.EmitConvert(_name.NameExpression, cg.CoreTypes.IPhpCallable);    // (IPhpCallable)Name
                cg.EmitLoadContext();       // Context
                cg.Emit_ArgumentsIntoArray(_arguments, default(PhpSignatureMask)); // PhpValue[]

                return cg.EmitMethodAccess(
                    stack: cg.EmitCall(ILOpCode.Callvirt, cg.CoreTypes.IPhpCallable.Symbol.LookupMember<MethodSymbol>("Invoke")),
                    method: null,
                    access: this.Access);
            }
        }

        internal override void BuildCallsiteCreate(CodeGenerator cg, TypeSymbol returntype)
        {
            cg.Builder.EmitStringConstant(CallsiteName);    // function name
            cg.Builder.EmitStringConstant(_nameOpt.HasValue ? _nameOpt.Value.ToString() : null);    // fallback function name
            cg.EmitLoadToken(returntype, null);             // return type
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.BinderFactory_Function);
        }
    }

    partial class BoundInstanceFunctionCall
    {
        protected override bool CallsiteRequiresCallerContext => true;
        protected override string CallsiteName => _name.IsDirect ? _name.NameValue.ToString() : null;
        protected override BoundExpression RoutineNameExpr => _name.NameExpression;

        internal override void BuildCallsiteCreate(CodeGenerator cg, TypeSymbol returntype)
        {
            cg.Builder.EmitStringConstant(CallsiteName);        // name
            cg.EmitLoadToken(cg.CallerType, null);              // class context
            cg.EmitLoadToken(returntype, null);                 // return type
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.BinderFactory_InstanceFunction);
        }
    }

    partial class BoundStaticFunctionCall
    {
        protected override bool CallsiteRequiresCallerContext => true;
        protected override string CallsiteName => _name.IsDirect ? _name.NameValue.ToString() : null;
        protected override BoundExpression RoutineNameExpr => _name.NameExpression;
        protected override IBoundTypeRef RoutineTypeRef => _typeRef.ResolvedType.IsErrorTypeOrNull() ? _typeRef : null;    // in case the type has to be resolved in runtime and passed to callsite
        protected override IBoundTypeRef LateStaticTypeRef => _typeRef;  // used for direct routine call requiring late static type
        protected override bool IsVirtualCall => false;

        /// <summary>
        /// Emits current class instance, expected by callsite to resolve instance function called statically.
        /// </summary>
        internal override TypeSymbol EmitTarget(CodeGenerator cg)
        {
            return cg.EmitPhpThisOrNull();
        }

        internal override void BuildCallsiteCreate(CodeGenerator cg, TypeSymbol returntype)
        {
            cg.EmitLoadToken(_typeRef.ResolvedType, null);      // type
            cg.Builder.EmitStringConstant(CallsiteName);        // name
            cg.EmitLoadToken(cg.CallerType, null);              // class context
            cg.EmitLoadToken(returntype, null);                 // return type
            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Dynamic.BinderFactory_StaticFunction);
        }

        internal override void EmitBeforeCall(CodeGenerator cg)
        {
            // ensure type is declared
            if (_typeRef.ResolvedType.IsValidType())
            {
                cg.EmitExpectTypeDeclared(_typeRef.ResolvedType);
            }
        }
    }

    partial class BoundNewEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var t = EmitNewClass(cg);

            // void
            if (Access.IsNone)
            {
                cg.EmitPop(t);
                return cg.CoreTypes.Void;
            }

            // &new // deprecated
            if (Access.IsReadRef)
            {
                // new PhpAlias(PhpValue.FromClass(.newobj))
                cg.EmitConvertToPhpValue(t, 0);
                return cg.Emit_PhpValue_MakeAlias();
            }

            //
            Debug.Assert(Access.IsRead);
            return t;
        }

        private TypeSymbol EmitNewClass(CodeGenerator cg)
        {
            if (!TargetMethod.IsErrorMethodOrNull())
            {
                // when instantiating anonoymous class
                // it has to be declared into the context (right before instantiation)
                if (TargetMethod.ContainingType.IsAnonymousType)
                {
                    // <ctx>.DeclareType<T>()
                    cg.EmitLoadContext();
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Context.DeclareType_T.Symbol.Construct(TargetMethod.ContainingType)).Expect(SpecialType.System_Void);
                }
                else
                {
                    // ensure type is declared
                    cg.EmitExpectTypeDeclared(TargetMethod.ContainingType);
                }

                // Template: new T(args)
                return EmitDirectCall(cg, ILOpCode.Newobj, TargetMethod);
            }
            else
            {
                if (((BoundTypeRef)_typeref).ResolvedType.IsValidType())
                {
                    // ensure type is delcared
                    cg.EmitExpectTypeDeclared(_typeref.Type);

                    // context.Create<T>(caller, params)
                    var create_t = cg.CoreTypes.Context.Symbol.GetMembers("Create")
                        .OfType<MethodSymbol>()
                        .Where(s => s.Arity == 1 && s.ParameterCount == 2 &&
                            s.Parameters[1].IsParams &&
                            SpecialParameterSymbol.IsCallerClassParameter(s.Parameters[0]))
                        .Single()
                        .Construct(_typeref.Type);

                    cg.EmitLoadContext();               // Context
                    cg.EmitCallerTypeHandle();          // RuntimeTypeHandle
                    cg.Emit_ArgumentsIntoArray(_arguments, default);  // PhpValue[]

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

                    cg.EmitLoadContext();               // Context
                    cg.EmitCallerTypeHandle();          // RuntimeTypeHandle
                    _typeref.EmitLoadTypeInfo(cg, true);// PhpTypeInfo
                    cg.Emit_ArgumentsIntoArray(_arguments, default);  // PhpValue[]

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

            var args = ArgumentsInSourceOrder;
            for (int i = 0; i < args.Length; i++)
            {
                cg.EmitEcho(args[i].Value);
            }

            return cg.CoreTypes.Void;
        }
    }

    partial class BoundConcatEx
    {
        static SpecialMember ResolveConcatMethod(int stringargs)
        {
            switch (stringargs)
            {
                case 2: return SpecialMember.System_String__ConcatStringString;
                case 3: return SpecialMember.System_String__ConcatStringStringString;
                case 4: return SpecialMember.System_String__ConcatStringStringStringString;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var args = this.ArgumentsInSourceOrder;

            if (args.Length == 0)
            {
                // ""
                cg.Builder.EmitStringConstant(string.Empty);
                return cg.CoreTypes.String;
            }

            if (args.Length <= 4 && (cg.IsReadonlyStringOnly(this.TypeRefMask) || this.Access.TargetType == cg.CoreTypes.String))
            {
                // Template: System.String.Concat( ... )
                foreach (var x in args)
                {
                    cg.EmitConvert(x.Value, cg.CoreTypes.String);
                }

                //
                if (args.Length == 1)
                {
                    // (string)arg[0]
                    return cg.CoreTypes.String;
                }
                else
                {
                    // String.Concat( (string)0, (string)1, ... );
                    var concat_method = ResolveConcatMethod(args.Length);
                    return cg.EmitCall(ILOpCode.Call, (MethodSymbol)cg.DeclaringCompilation.GetSpecialTypeMember(concat_method))
                        .Expect(SpecialType.System_String);
                }

                throw null;
            }

            if (args.Length == 1)
            {
                // Template: (PhpString)args[0]
                cg.EmitConvert(args[0].Value, cg.CoreTypes.PhpString);
                return cg.CoreTypes.PhpString;
            }

            // Template: new PhpString( new PhpString.Blob() { a1, a2, ..., aN } )

            // new PhpString.Blob()
            cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.Blob);

            // TODO: overload for 2, 3, 4 parameters directly

            // <PhpString>.Append(<expr>)
            foreach (var x in args)
            {
                var expr = x.Value;
                if (IsEmpty(expr))
                {
                    continue;
                }

                //
                cg.Builder.EmitOpCode(ILOpCode.Dup);        // <Blob>
                cg.Emit_PhpStringBlob_Append(expr);// .Append( ... )
            }

            // new PhpString( <Blob> )
            return cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpString_Blob)
                .Expect(cg.CoreTypes.PhpString);
        }

        static bool IsEmpty(BoundExpression x) => x.ConstantValue.HasValue && ExpressionsExtension.IsEmptyStringValue(x.ConstantValue.Value);
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
                        cg.Builder.AdjustStack(-1);
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

                // Template: <ctx>.Include(dir, path, locals, @this, self, bool once = false, bool throwOnError = false)
                cg.EmitLoadContext();
                cg.Builder.EmitStringConstant(cg.ContainingFile.DirectoryRelativePath);
                cg.EmitConvert(_arguments[0].Value, cg.CoreTypes.String);
                cg.LocalsPlaceOpt.EmitLoad(cg.Builder); // scope of local variables, corresponds to $GLOBALS in global scope.
                cg.EmitThisOrNull();    // $this
                cg.EmitCallerTypeHandle();    // self : RuntimeTypeHandle
                cg.Builder.EmitBoolConstant(IsOnceSemantic);
                cg.Builder.EmitBoolConstant(IsRequireSemantic);
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.Include_string_string_PhpArray_object_RuntimeTypeHandle_bool_bool);
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

            // Template: BuildClosure(ctx, BoundLambdaMethod.EnsureRoutineInfoField(), this, scope, statictype, [use1, use2, ...], [p1, p2, ...])

            var idxfld = this.BoundLambdaMethod.EnsureRoutineInfoField(cg.Module);

            cg.EmitLoadContext();           // Context
            idxfld.EmitLoad(cg);            // routine
            EmitThis(cg);                   // $this
            cg.EmitCallerTypeHandle();      // scope
            EmitStaticType(cg);             // statictype : PhpTypeInfo
            EmitParametersArray(cg);        // "parameters"
            EmitUseArray(cg);               // "static"

            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.BuildClosure_Context_IPhpCallable_Object_RuntimeTypeHandle_PhpTypeInfo_PhpArray_PhpArray);
        }

        void EmitThis(CodeGenerator cg)
        {
            cg.EmitPhpThisOrNull();
        }

        void EmitStaticType(CodeGenerator cg)
        {
            if ((cg.Routine.Flags & FlowAnalysis.RoutineFlags.UsesLateStatic) != 0)
            {
                cg.EmitLoadStaticPhpTypeInfo();
            }
            else
            {
                cg.Builder.EmitNullConstant();
            }
        }

        void EmitUseArray(CodeGenerator cg)
        {
            if (UseVars.Length != 0)
            {
                // new PhpArray(<count>)
                cg.Builder.EmitIntConstant(UseVars.Length);
                cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpArray_int);

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
                // TODO: cache singleton

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
            var filepath = cg.ContainingFile.RelativeFilePath;
            int line, col;
            var unit = this.PhpSyntax.ContainingSourceUnit;
            unit.GetLineColumnFromPosition(this.CodeExpression.PhpSyntax.Span.Start, out line, out col);

            // Template: Operators.Eval(ctx, locals, @this, self, code, currentpath, line, column)
            cg.EmitLoadContext();
            cg.LocalsPlaceOpt.EmitLoad(cg.Builder);
            cg.EmitThisOrNull();
            cg.EmitCallerTypeHandle();           // self : RuntimeTypeHandle
            cg.EmitConvert(this.CodeExpression, cg.CoreTypes.String);   // (string)code
            cg.Builder.EmitStringConstant(filepath);    // currentpath
            cg.Builder.EmitIntConstant(line);           // line
            cg.Builder.EmitIntConstant(col);            // column

            // Eval( ... )
            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Eval_Context_PhpArray_object_RuntimeTypeHandle_string_string_int_int);
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

    partial class BoundAssertEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var args = ArgumentsInSourceOrder;
            if (args.Length == 0 ||
                args[0].Value.ConstantValue.EqualsOptional(true.AsOptional()) ||    // ignoring assertion evaluated to true
                cg.IsReadonlyStringOnly(args[0].Value.TypeRefMask))                 // ignoring string assertions
            {
                if (Access.IsNone)
                {
                    // emit nothing
                    return cg.CoreTypes.Void;
                }

                // always passing
                cg.Builder.EmitBoolConstant(true);
            }
            else
            {
                // Template: <ctx>.Assert( condition.ToBoolean(), action )
                cg.EmitLoadContext();

                cg.EmitConvertToBool(args[0].Value);

                if (args.Length > 1)
                {
                    cg.EmitConvertToPhpValue(args[1].Value);
                }
                else
                {
                    cg.Emit_PhpValue_Void();
                }

                // 
                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.Assert_bool_PhpValue)
                    .Expect(SpecialType.System_Boolean);
            }

            //
            return cg.CoreTypes.Boolean;
        }
    }

    partial class BoundAssignEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var target_place = this.Target.BindPlace(cg);

            Debug.Assert(target_place != null);
            Debug.Assert(target_place.Type == null || target_place.Type.SpecialType != SpecialType.System_Void);

            // T tmp; // in case access is Read
            LocalDefinition tmp = null;

            // <target> = <value>
            var lhs = target_place.EmitStorePreamble(cg, Target.TargetAccess());

            var t_value = target_place.Type;
            if (t_value != null &&
                t_value != cg.CoreTypes.PhpValue &&
                t_value != cg.CoreTypes.PhpAlias &&
                !Value.Access.IsReadRef &&
                Access.IsNone)
            {
                // we can convert more efficiently here
                cg.EmitConvert(Value, t_value);
            }
            else
            {
                t_value = cg.Emit(Value);
            }

            if (t_value.SpecialType == SpecialType.System_Void)
            {
                // default<T>
                t_value = target_place.Type ?? cg.CoreTypes.PhpValue; // T of PhpValue
                cg.EmitLoadDefault(t_value, 0);
            }

            //
            if (Access.IsNone)
            {
                // nothing
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

            target_place.EmitStore(cg, ref lhs, t_value, Target.Access);

            lhs.Dispose();

            //
            if (Access.IsNone)
            {
                t_value = cg.CoreTypes.Void;
            }
            else if (Access.IsRead)
            {
                Debug.Assert(tmp != null);
                cg.Builder.EmitLocalLoad(tmp);
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
        /// <summary>
        /// Searches for an occurance of <see cref="SearchForTargetVisitor._target"/>.
        /// </summary>
        class SearchForTargetVisitor : Graph.GraphWalker<VoidStruct>
        {
            readonly BoundReferenceExpression/*!*/_target;

            public bool Found { get; private set; }

            public SearchForTargetVisitor(BoundReferenceExpression target)
            {
                _target = target ?? throw ExceptionUtilities.ArgumentNull();
            }

            public override VoidStruct VisitVariableName(BoundVariableName x)
            {
                if (_target is BoundVariableRef v)
                {
                    Found |= !v.Name.IsDirect || !x.IsDirect || v.Name.NameValue == x.NameValue;
                }

                return default;
            }
        }

        /// <summary>
        /// Determines if <paramref name="target"/> is not referenced within <paramref name="rvalue"/>.
        /// </summary>
        static bool IsSafeToUnroll(BoundReferenceExpression target, BoundExpression rvalue)
        {
            if (rvalue.IsConstant() || rvalue is BoundGlobalConst || rvalue is BoundPseudoConst || (rvalue is BoundFieldRef f && f.IsClassConstant))
            {
                return true;
            }

            var visitor = new SearchForTargetVisitor(target);
            rvalue.Accept(visitor);
            return visitor.Found != true;
        }

        static TypeSymbol EmitAppend(CodeGenerator cg, BoundReferenceExpression target, BoundExpression rvalue, BoundAccess access)
        {
            var target_place = target.BindPlace(cg);

            bool inplace = false;
            var lhs = default(LhsStack);

            if (target_place.HasAddress && target_place.Type != null)
            {
                // we can perform in-place concatenation

                if (target_place.Type == cg.CoreTypes.PhpValue)
                {
                    // Template: Operators.EnsureWritableString(ref PhpValue target).Add( .. )
                    inplace = true;
                    target_place.EmitLoadAddress(cg, ref lhs);  // ref PhpValue
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureWritableString_PhpArrayRef)
                        .Expect(cg.CoreTypes.PhpString_Blob);
                }
                else if (target_place.Type == cg.CoreTypes.PhpAlias)
                {
                    // Template: Operators.EnsureWritableString(ref PhpValue target.Alias.Value).Add( .. )
                    inplace = true;
                    target_place.EmitLoadValue(cg, ref lhs, target.Access);
                    cg.Emit_PhpAlias_GetValueAddr();    // ref PhpValue
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureWritableString_PhpArrayRef)
                        .Expect(cg.CoreTypes.PhpString_Blob);
                }
                else if (target_place.Type == cg.CoreTypes.PhpString)
                {
                    // Template: (target : PhpString).EnsureWritable().Add( .. )
                    inplace = true;
                    target_place.EmitLoadAddress(cg, ref lhs);  // : ref PhpString
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpString.EnsureWritable)
                        .Expect(cg.CoreTypes.PhpString_Blob);
                }
            }

            //

            if (inplace)
            {
                lhs.Dispose();
            }
            else
            {
                // Template: PhpString.AsWritable( ((PhpString)target) ) : Blob
                lhs = target_place.EmitStorePreamble(cg, target.TargetAccess());
                cg.EmitConvertToPhpString(target_place.EmitLoadValue(cg, ref lhs, target.Access), 0);
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpString.AsWritable_PhpString) // Blob
                    .Expect(cg.CoreTypes.PhpString_Blob);
            }

            // STACK: PhpString.Blob

            // Template: .Add( rValue )

            if (access.IsRead || !inplace)
            {
                cg.Builder.EmitOpCode(ILOpCode.Dup);    // 
            }

            // check rValue does not contain lValue!
            // if {rvalue} references {target}, we cannot unroll concat expression

            cg.Emit_PhpStringBlob_Append(rvalue, expandConcat: IsSafeToUnroll(target, rvalue));

            // STACK: 'void' or 'PhpString.Blob'

            if (access.IsRead || !inplace)
            {
                // STACK: PhpString.Blob

                // Template: new PhpString(blob)
                var result_type = cg.EmitCall(ILOpCode.Newobj, cg.CoreMethods.Ctors.PhpString_Blob)
                    .Expect(cg.CoreTypes.PhpString);

                // STACK: PhpString

                LocalDefinition tmp = null;

                if (access.IsRead)
                {
                    tmp = cg.GetTemporaryLocal(result_type, false);
                    cg.Builder.EmitOpCode(ILOpCode.Dup);
                    cg.Builder.EmitLocalStore(tmp);
                }

                if (!inplace)
                {
                    target_place.EmitStore(cg, ref lhs, result_type, target.Access);
                    lhs.Dispose();
                }
                else
                {
                    cg.Builder.EmitOpCode(ILOpCode.Pop);
                }

                // STACK: void

                if (access.IsRead)
                {
                    Debug.Assert(tmp != null);
                    cg.Builder.EmitLoad(tmp);
                    cg.ReturnTemporaryLocal(tmp);
                    return result_type;
                }
            }

            // STACK: void

            if (access.IsNone)
            {
                return cg.CoreTypes.Void;
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(access);
            }
        }

        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(Access.IsRead || Access.IsNone);

            // target X= value;

            if (this.Operation == Operations.AssignAppend)
            {
                return EmitAppend(cg, this.Target, this.Value, this.Access);
            }

            var target_place = this.Target.BindPlace(cg);
            Debug.Assert(target_place != null);
            Debug.Assert(target_place.Type == null || target_place.Type.SpecialType != SpecialType.System_Void);

            // <target> = <target> X <value>
            var lhs = target_place.EmitStorePreamble(cg, Target.TargetAccess());
            var xtype = target_place.EmitLoadValue(cg, ref lhs, Target.Access);

            TypeSymbol result_type;

            switch (this.Operation)
            {
                case Operations.AssignAdd:
                    result_type = BoundBinaryEx.EmitAdd(cg, xtype, Value, target_place.Type);
                    break;
                //case Operations.AssignAppend:
                //    result_type = EmitAppend(cg, xtype, Value);
                //    break;
                ////case Operations.AssignPrepend:
                ////    break;
                case Operations.AssignDiv:
                    result_type = BoundBinaryEx.EmitDiv(cg, xtype, Value, target_place.Type);
                    break;
                case Operations.AssignMod:
                    result_type = BoundBinaryEx.EmitRemainder(cg, xtype, Value);
                    break;
                case Operations.AssignMul:
                    result_type = BoundBinaryEx.EmitMul(cg, xtype, Value, target_place.Type);
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
                    result_type = BoundBinaryEx.EmitSub(cg, xtype, Value, target_place.Type);
                    break;
                case Operations.AssignCoalesce:
                    result_type = BoundBinaryEx.EmitCoalesce(cg, xtype, 0, Value);
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

            target_place.EmitStore(cg, ref lhs, result_type, Target.Access);
            lhs.Dispose();

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

            // prepare target for store operation
            var lhs = target_place.EmitStorePreamble(cg, Target.TargetAccess());

            // load target value
            var target_load_type = target_place.EmitLoadValue(cg, ref lhs, Target.Access);

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
                op_type = BoundBinaryEx.EmitAdd(cg, target_load_type, this.Value, target_place.Type);
            }
            else
            {
                Debug.Assert(IsDecrement);
                op_type = BoundBinaryEx.EmitSub(cg, target_load_type, this.Value, target_place.Type);
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
            target_place.EmitStore(cg, ref lhs, op_type, Target.Access);

            lhs.Dispose();

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

        bool IsPrefix => !IsPostfix;
        bool IsDecrement => !this.IsIncrement;
    }

    partial class BoundConditionalEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            TypeSymbol result_type = cg.DeclaringCompilation.GetTypeFromTypeRef(cg.Routine, this.TypeRefMask);
            bool result_isvoid = result_type.SpecialType == SpecialType.System_Void;

            object trueLbl = new object();
            object endLbl = new object();

            if (this.IfTrue != null)
            {
                // !COND?T:F -> COND?F:T
                bool isnegation = this.Condition.IsLogicNegation(out var negexpr);
                var condition = isnegation ? negexpr : this.Condition;

                // Cond ? True : False
                cg.EmitConvertToBool(condition);   // i4
                cg.Builder.EmitBranch(isnegation ? ILOpCode.Brfalse : ILOpCode.Brtrue, trueLbl);

                // false:
                cg.EmitConvert(this.IfFalse, result_type);
                cg.Builder.EmitBranch(ILOpCode.Br, endLbl);
                if (!result_isvoid) cg.Builder.AdjustStack(-1);

                // trueLbl:
                cg.Builder.MarkLabel(trueLbl);
                cg.EmitConvert(this.IfTrue, result_type);

                // endLbl:
                cg.Builder.MarkLabel(endLbl);
            }
            else
            {
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
                if (!result_isvoid) cg.Builder.AdjustStack(-1);

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
                if (Access.IsNone && !cg.EmitPdbSequencePoints)
                {
                    return cg.CoreTypes.Void;
                }

                // PhpArray.NewEmpty()
                return cg.Emit_PhpArray_NewEmpty();
            }
            else if (cg.IsInCachedArrayExpression || this.RequiresContext)
            {
                // new PhpArray(){ ... }
                return EmitNewPhpArray(cg);
            }
            else // array items do not need Context => they are immutable/literals
            {
                if (Access.IsNone && !cg.EmitPdbSequencePoints)
                {
                    return cg.CoreTypes.Void;
                }

                // static PhpArray field;
                // field ?? (field = new PhpArray(){ ... })
                return EmitCachedPhpArray(cg);
            }
        }

        TypeSymbol EmitNewPhpArray(CodeGenerator cg)
        {
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

        /// <summary>
        /// Caches the array instance into an internal app-static field,
        /// so repetitious creations only uses the existing instance.
        /// </summary>
        TypeSymbol EmitCachedPhpArray(CodeGenerator cg)
        {
            Debug.Assert(cg.IsInCachedArrayExpression == false);

            // static PhpArray arr`;
            var fld = cg.Factory.CreateSynthesizedField(cg.CoreTypes.PhpArray, "<arr>", true);
            var fldplace = new FieldPlace(null, fld, cg.Module);

            // TODO: reuse existing cached PhpArray with the same content

            //
            cg.IsInCachedArrayExpression = true;

            // arr ?? (arr = new PhpArray(){...})
            fldplace.EmitLoad(cg.Builder);
            cg.EmitNullCoalescing((cg_) =>
            {
                fldplace.EmitStorePrepare(cg_.Builder);
                EmitNewPhpArray(cg_);
                cg_.Builder.EmitOpCode(ILOpCode.Dup);
                fldplace.EmitStore(cg_.Builder);
            });

            // .DeepCopy()
            // if (this.Access.IsReadCopy) // unsafe ?
            {
                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.DeepCopy);
            }

            //
            cg.IsInCachedArrayExpression = false;

            //
            return fld.Type;    // ~ PhpArray
        }
    }

    partial class BoundArrayItemEx : IVariableReference
    {
        internal override IVariableReference BindPlace(CodeGenerator cg) => this;

        internal override IPlace Place() => ((IVariableReference)this).Place;

        #region IBoundReference

        Symbol IVariableReference.Symbol => null;

        TypeSymbol IVariableReference.Type => DeclaringCompilation.CoreTypes.PhpValue;

        bool IVariableReference.HasAddress => false;

        IPlace IVariableReference.Place => null;    // TODO: simple array access in case Array is System.Array and Key is int|long

        #region Emitted Array Stack

        /// <summary>
        /// Stack of type of {array,index} emitted by <see cref="IVariableReference.EmitLoadValue"/> and <see cref="IVariableReference.EmitStorePreamble"/>.
        /// </summary>
        Stack<EmittedArrayInfo> _emittedArrays;

        struct EmittedArrayInfo
        {
            public TypeSymbol tArray, tIndex;
        }

        /// <summary>
        /// <see cref="IVariableReference.EmitStorePreamble"/> and <see cref="IVariableReference.EmitStorePreamble"/> remembers what was the array type it emitted.
        /// Used by <see cref="PopEmittedArray"/> and <see cref="IVariableReference.EmitLoadValue"/> or <see cref="IVariableReference.EmitStore"/> to emit specific operator.
        /// </summary>
        void PushEmittedArray(TypeSymbol tArray, TypeSymbol tIndex)
        {
            Debug.Assert(tArray != null);

            if (_emittedArrays == null)
            {
                _emittedArrays = new Stack<EmittedArrayInfo>();
            }

            _emittedArrays.Push(new EmittedArrayInfo() { tArray = tArray, tIndex = tIndex });
        }

        /// <summary>
        /// Used by <see cref="IVariableReference.EmitLoadValue"/> and <see cref="IVariableReference.EmitStore"/> to emit specific operator
        /// on a previously emitted array (<see cref="PushEmittedArray"/>).
        /// </summary>
        EmittedArrayInfo PopEmittedArray()
        {
            Debug.Assert(_emittedArrays != null && _emittedArrays.Count != 0);
            var result = _emittedArrays.Pop();
            if (_emittedArrays.Count == 0)
            {
                _emittedArrays = null;   // free
            }

            return result;
        }

        bool IndexIsSafe()
        {
            var constant = (Index != null) ? Index.ConstantValue : null;
            if (constant.HasValue)
            {
                var value = constant.Value;
                return value is long || value is int || value is string;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Emits <see cref="Index"/> either as <c>PhpValue</c> or <c>IntStringKey</c> if possible safely.
        /// If <see cref="Index"/> is a <c>null</c> reference, nothing is emitted and <c>null</c> is returned by the function.
        /// </summary>
        TypeSymbol EmitLoadIndex(CodeGenerator cg, ref LhsStack lhs, bool safeToUseIntStringKey)
        {
            TypeSymbol tIndex;

            if (this.Index != null)
            {
                if (safeToUseIntStringKey && IndexIsSafe())
                {
                    tIndex = cg.CoreTypes.IntStringKey;
                    cg.EmitIntStringKey(this.Index);
                }
                else
                {
                    tIndex = cg.CoreTypes.PhpValue;
                    cg.EmitConvert(this.Index, tIndex);
                }
            }
            else
            {
                tIndex = null;
            }

            //
            return tIndex;
        }

        #endregion

        void EmitLoadPrepare(CodeGenerator cg, ref LhsStack lhs)
        {
            // Template: array[index]

            bool safeToUseIntStringKey = false;
            bool canBeNull = true;

            //
            // LOAD Array
            //

            var tArray = lhs.EmitReceiver(cg, Array);

            // convert {t} to IPhpArray, string, System.Array

            if (tArray.IsOfType(cg.CoreTypes.IPhpArray))
            {
                // ok; PhpArray, PhpString, object implementing IPhpArray
                safeToUseIntStringKey = true;
            }
            else if (tArray == cg.CoreTypes.PhpValue)
            {
                // ok
            }
            else if (tArray == cg.CoreTypes.PhpAlias)
            {
                tArray = cg.Emit_PhpAlias_GetValue();
            }
            else if (tArray == cg.CoreTypes.String)
            {
                // ok
                safeToUseIntStringKey = true;
            }
            else if (tArray == cg.CoreTypes.PhpString)
            {
                // <PhpString>.AsArray
                tArray = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpString.AsArray_PhpString);
                safeToUseIntStringKey = true;
            }
            else if (tArray == cg.CoreTypes.Void)
            {
                Debug.Fail("Use of uninitialized value.");  // TODO: Err in analysis, use of uninitialized value
            }
            else if (tArray.IsArray())   // TODO: IList, IDictionary
            {
                // ok
            }
            else if (tArray.IsOfType(cg.CoreTypes.ArrayAccess))
            {
                // ok
            }
            else
            {
                // object -> IPhpArray
                tArray = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureArray_Object);
                canBeNull = false;
            }

            if (this.Access.IsRead && this.Access.IsQuiet)  // TODO: analyse if Array can be NULL
            {
                // ?? PhpArray.Empty
                if (cg.CoreTypes.PhpArray.Symbol.IsOfType(tArray) && canBeNull)
                {
                    cg.EmitNullCoalescing((_cg) =>
                    {
                        _cg.EmitCastClass(_cg.Emit_PhpArray_Empty(), tArray);
                    });
                }
            }

            Debug.Assert(
                tArray.IsOfType(cg.CoreTypes.IPhpArray) ||
                tArray.SpecialType == SpecialType.System_String ||
                tArray.IsArray() ||
                tArray.IsOfType(cg.CoreTypes.ArrayAccess) ||
                tArray == cg.CoreTypes.PhpValue);

            //
            // LOAD [Index]
            //

            Debug.Assert(this.Index != null || this.Access.IsEnsure, "Index is required when reading the array item.");

            var tIndex = EmitLoadIndex(cg, ref lhs, safeToUseIntStringKey);

            // remember for EmitLoad
            PushEmittedArray(tArray, tIndex);
        }

        TypeSymbol IVariableReference.EmitLoadValue(CodeGenerator cg, ref LhsStack lhs, BoundAccess access)
        {
            EmitLoadPrepare(cg, ref lhs);

            // Template: array[index]

            var stack = PopEmittedArray();
            if (stack.tArray.IsOfType(cg.CoreTypes.IPhpArray))
            {
                // whether the target is instance of PhpArray, otherwise it is an IPhpArray and we have to use .callvirt and different operators
                var isphparr = (stack.tArray == cg.CoreTypes.PhpArray);

                if (this.Index == null)
                {
                    Debug.Assert(stack.tIndex == null);
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
                    if (stack.tIndex == cg.CoreTypes.IntStringKey)
                    {
                        // <array>.EnsureItemObject(<index>)
                        return isphparr
                            ? cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.EnsureItemObject_IntStringKey)
                            : cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.EnsureItemObject_IntStringKey);
                    }
                    else
                    {
                        Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);
                        // EnsureItemObject(<array>, <index>)
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureItemObject_IPhpArray_PhpValue);
                    }
                }
                else if (Access.EnsureArray)
                {
                    if (stack.tIndex == cg.CoreTypes.IntStringKey)
                    {
                        // <array>.EnsureItemArray(<index>)
                        return isphparr
                            ? cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.EnsureItemArray_IntStringKey)
                            : cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.EnsureItemArray_IntStringKey);
                    }
                    else
                    {
                        Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);
                        // EnsureItemArray(<array>, <index>)
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureItemArray_IPhpArray_PhpValue);
                    }
                }
                else if (Access.IsReadRef)
                {
                    Debug.Assert(this.Array.Access.EnsureArray);

                    if (stack.tIndex == cg.CoreTypes.IntStringKey)
                    {
                        // <array>.EnsureItemAlias(<index>)
                        return isphparr
                            ? cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.EnsureItemAlias_IntStringKey)
                            : cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.EnsureItemAlias_IntStringKey);
                    }
                    else
                    {
                        Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);
                        // EnsureItemAlias(<array>, <index>, quiet)
                        cg.Builder.EmitBoolConstant(Access.IsQuiet);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureItemAlias_IPhpArray_PhpValue_Bool);
                    }
                }
                else
                {
                    Debug.Assert(Access.IsRead);

                    TypeSymbol t;

                    if (stack.tIndex == cg.CoreTypes.IntStringKey)
                    {
                        // <array>.GetItemValue(<index>)
                        t = isphparr
                            ? cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.GetItemValue_IntStringKey)
                            : cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.GetItemValue_IntStringKey);
                    }
                    else
                    {
                        Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);
                        // GetItemValue(<array>, <index>)
                        //cg.Builder.EmitBoolConstant(Access.IsQuiet);
                        t = cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.GetItemValue_PhpValue);
                    }

                    return t;
                }
            }
            else if (stack.tArray.SpecialType == SpecialType.System_String)
            {
                if (Access.EnsureObject || Access.EnsureArray || Access.IsReadRef)
                {
                    // null
                    throw new InvalidOperationException();
                }
                else
                {
                    Debug.Assert(Access.IsRead);
                    if (stack.tIndex == cg.CoreTypes.IntStringKey)
                    {
                        // GetItemValue{OrNull}(string, IntStringKey)
                        return cg.EmitCall(ILOpCode.Call, this.Access.Flags.Isset()
                            ? cg.CoreMethods.Operators.GetItemValueOrNull_String_IntStringKey   // string or null
                            : cg.CoreMethods.Operators.GetItemValue_String_IntStringKey         // string or ""
                            );
                    }
                    else
                    {
                        Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);
                        // GetItemValue(string, PhpValue, bool)
                        cg.Builder.EmitBoolConstant(Access.IsQuiet);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetItemValue_String_PhpValue_Bool);
                    }
                }
            }
            else if (stack.tArray == cg.CoreTypes.PhpValue)
            {
                Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);

                if (Access.EnsureObject || Access.EnsureArray)
                {
                    // null
                    throw new InvalidOperationException();
                }
                else if (Access.IsReadRef)
                {
                    Debug.WriteLine("TODO: we need reference to PhpValue so we can modify its content! This is not compatible with behavior of = &$null[0].");

                    // PhpValue.GetItemRef(index, bool)
                    cg.Builder.EmitBoolConstant(Access.IsQuiet);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureItemAlias_PhpValue_PhpValue_Bool);
                }
                else // IsRead
                {
                    Debug.Assert(Access.IsRead);
                    // PhpValue.GetItemValue(index, bool)
                    cg.Builder.EmitBoolConstant(Access.IsQuiet);
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetItemValue_PhpValue_PhpValue_Bool);
                }
            }
            else if (stack.tArray.IsOfType(cg.CoreTypes.ArrayAccess))
            {
                Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);

                // Template: ArrayAccess.offsetGet(<index>)
                var t = cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Operators.offsetGet_ArrayAccess_PhpValue);

                if (Access.EnsureArray)
                {
                    Debug.Assert(t == cg.CoreTypes.PhpValue);
                    // Template: (ref PhpValue).EnsureArray()
                    cg.EmitPhpValueAddr();
                    t = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureArray_PhpValueRef);
                }

                return t;
            }
            else if (stack.tArray.SpecialType == SpecialType.System_Void)
            {
                // array item on an uninitialized value
                // void[key] -> void
                cg.EmitPop(stack.tIndex);
                return cg.Emit_PhpValue_Void();
            }
            else
            {
                throw cg.NotImplementedException($"LOAD {stack.tArray.Name}[]");
            }
        }

        TypeSymbol IVariableReference.EmitLoadAddress(CodeGenerator cg, ref LhsStack lhs)
        {
            //EmitLoadPrepare(cg, ref lhs);

            //var stack = PopEmittedArray();
            //if (stack.tArray == cg.CoreTypes.PhpArray && stack.tIndex == cg.CoreTypes.IntStringKey)
            //{
            //    // STACK: <PhpArray> <key>

            //    // Template: ref PhpArray.GetItemRef(key)
            //    Debug.Assert(cg.CoreMethods.PhpArray.GetItemRef_IntStringKey.Symbol.ReturnValueIsByRef);
            //    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.GetItemRef_IntStringKey);
            //}
            //else
            //{
            //    throw new NotSupportedException();
            //    //PushEmittedArray(stack.tArray, stack.tIndex);
            //    //return null;    // TODO: IPhpArray if needed
            //}
            throw ExceptionUtilities.Unreachable;
        }

        LhsStack IVariableReference.EmitStorePreamble(CodeGenerator cg, BoundAccess access)
        {
            var lhs = new LhsStack { CodeGenerator = cg, IsEnabled = access.IsRead, };

            //Debug.Assert(this.Array.Access.EnsureArray || this.Array.Access.IsQuiet);

            // Template: array[index]

            bool safeToUseIntStringKey = false;

            //
            // ENSURE Array
            //

            var tArray = lhs.EmitReceiver(cg, Array);
            if (tArray.IsOfType(cg.CoreTypes.IPhpArray))    // PhpArray, PhpString
            {
                // ok
                safeToUseIntStringKey = true;
            }
            else if (tArray.IsOfType(cg.CoreTypes.ArrayAccess)) // ArrayAccess
            {
                // ok
            }
            else if (this.Array.Access.EnsureArray)
            {
                if (tArray == cg.CoreTypes.PhpAlias)
                {
                    // PhpAlias.EnsureArray
                    tArray = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.EnsureArray);
                }
                else
                {
                    // Array should be ensured already
                    throw cg.NotImplementedException($"(ensure) STORE {tArray.Name}[]");
                }
            }
            else if (this.Array.Access.IsQuiet)
            {
                // WRITE semantics, without need of ensuring the underlaying value
                // isempty, unset; otherwise in store operation we should EnsureArray already

                if (tArray == cg.CoreTypes.PhpAlias)
                {
                    // dereference
                    tArray = cg.Emit_PhpAlias_GetValue();
                }

                if (tArray == cg.CoreTypes.PhpValue)
                {
                    Debug.WriteLine("TODO: we need reference to PhpValue so we can modify its content! Won't work with $string[] = ...");

                    cg.EmitPhpValueAddr();

                    tArray = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetArrayAccess_PhpValueRef)
                        .Expect(cg.CoreTypes.IPhpArray);

                    // Template: <STACK> ?? (IPhpArray)PhpArray.Empty
                    cg.EmitNullCoalescing((_cg) =>
                    {
                        _cg.EmitCastClass(_cg.Emit_PhpArray_Empty(), _cg.CoreTypes.IPhpArray);
                    });
                }
                else if (tArray == cg.CoreTypes.String)
                {
                    // Template: (PhpString)string
                    tArray = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpString.implicit_from_string);
                }
                else if (
                    tArray == cg.CoreTypes.Void ||
                    tArray == cg.CoreTypes.Boolean ||
                    tArray == cg.CoreTypes.Long ||
                    tArray == cg.CoreTypes.Double ||
                    tArray.IsOfType(cg.CoreTypes.PhpResource))
                {
                    // TODO: WRN: use value of type '...' as array
                    cg.EmitPop(tArray);
                    tArray = cg.Emit_PhpArray_Empty();
                }
                else if (tArray.IsReferenceType)
                {
                    // null -> PhpArray.Empty
                    if (cg.CanBeNull(Array.TypeRefMask))
                    {
                        // Template: (object)<STACK> ?? PhpArray.Empty
                        cg.EmitCastClass(tArray, cg.CoreTypes.Object);
                        cg.EmitNullCoalescing((_cg) =>
                        {
                            _cg.EmitCastClass(_cg.Emit_PhpArray_Empty(), _cg.CoreTypes.Object);
                        });
                        tArray = cg.CoreTypes.Object;
                    }

                    // EnsureArray(<STACK>) or throw
                    tArray = cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.EnsureArray_Object)
                        .Expect(cg.CoreTypes.IPhpArray);
                }
                else
                {
                    throw cg.NotImplementedException($"(quiet) STORE {tArray.Name}[]");    // TODO: emit convert as PhpArray
                }
            }
            else
            {
                throw cg.NotImplementedException($"STORE {tArray.Name}[]");    // TODO: emit convert as PhpArray
            }

            Debug.Assert(tArray.IsOfType(cg.CoreTypes.IPhpArray) || tArray.IsOfType(cg.CoreTypes.ArrayAccess));

            //
            // LOAD [Index]
            //

            var tIndex = EmitLoadIndex(cg, ref lhs, safeToUseIntStringKey);

            if (tIndex == null && tArray.IsOfType(cg.CoreTypes.ArrayAccess))
            {
                // we need "NULL" key
                Debug.Assert(!safeToUseIntStringKey);
                tIndex = cg.Emit_PhpValue_Null();
            }

            // remember for EmitLoad
            PushEmittedArray(tArray, tIndex);

            //
            return lhs;
        }

        void IVariableReference.EmitStore(CodeGenerator cg, ref LhsStack lhs, TypeSymbol valueType, BoundAccess access)
        {
            // Template: array[index]

            var stack = PopEmittedArray();
            if (stack.tArray.IsOfType(cg.CoreTypes.IPhpArray))
            {
                // whether the target is instance of PhpArray, otherwise it is an IPhpArray and we have to use .callvirt
                var isphparr = (stack.tArray == cg.CoreTypes.PhpArray);

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
                        if (stack.tIndex == cg.CoreTypes.IntStringKey)
                        {
                            if (isphparr)
                                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.SetItemAlias_IntStringKey_PhpAlias);
                            else
                                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.SetItemAlias_IntStringKey_PhpAlias);
                        }
                        else
                        {
                            Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);
                            cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.SetItemAlias_PhpValue_PhpAlias);
                        }
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
                    if (stack.tIndex == cg.CoreTypes.IntStringKey)
                    {
                        if (isphparr)
                            cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.RemoveKey_IntStringKey);
                        else
                            cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.RemoveKey_IntStringKey);
                    }
                    else
                    {
                        Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);
                        cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.RemoveKey_PhpValue);
                    }
                }
                else
                {
                    Debug.Assert(Access.IsWrite);

                    cg.EmitConvertToPhpValue(valueType, 0);

                    // .SetItemValue(key, value) or .AddValue(value)
                    if (this.Index != null)
                    {
                        if (stack.tIndex == cg.CoreTypes.IntStringKey)
                        {
                            if (isphparr)
                                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.SetItemValue_IntStringKey_PhpValue);
                            else
                                cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.SetItemValue_IntStringKey_PhpValue);
                        }
                        else
                        {
                            Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);
                            cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.IPhpArray.SetItemValue_PhpValue_PhpValue);
                        }
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
            else if (stack.tArray.IsOfType(cg.CoreTypes.ArrayAccess))
            {
                if (Access.IsUnset)
                {
                    Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);
                    Debug.Assert(valueType == null);

                    // Template: <STACK>.offsetUnset( key )
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Operators.offsetUnset_ArrayAccess_PhpValue);
                }
                else if (Access.IsWrite)
                {
                    Debug.Assert(stack.tIndex == cg.CoreTypes.PhpValue);

                    // Template: <STACK>.offsetSet( key, value )
                    cg.EmitConvertToPhpValue(valueType, 0);
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Operators.offsetSet_ArrayAccess_PhpValue_PhpValue);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(Access);
                }
            }
            else
            {
                throw cg.NotImplementedException($"STORE {stack.tArray.Name}[]");
            }
        }

        #endregion
    }

    partial class BoundArrayItemOrdEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(!Access.MightChange);
            Debug.Assert(Index != null);

            // Either specialize the call for the string types or fall back to PhpValue
            TypeSymbol arrType;
            MethodSymbol operation;

            var arrTypeMask = Array.TypeRefMask;
            if (arrTypeMask.IsSingleType && !arrTypeMask.IsRef && cg.TypeRefContext.IsAString(arrTypeMask))
            {
                arrType = cg.EmitSpecialize(Array);
            }
            else
            {
                arrType = cg.EmitConvertToPhpValue(Array);
            }

            if (arrType == cg.CoreTypes.String)
            {
                operation = cg.CoreMethods.Operators.GetItemOrdValue_String_Long;
            }
            else if (arrType == cg.CoreTypes.PhpString)
            {
                operation = cg.CoreMethods.Operators.GetItemOrdValue_PhpString_Long;
            }
            else
            {
                Debug.Assert(arrType == cg.CoreTypes.PhpValue);
                operation = cg.CoreMethods.Operators.GetItemOrdValue_PhpValue_Long.Symbol;
            }

            // The index must be integral
            var indexType = cg.EmitSpecialize(Index);
            Debug.Assert(indexType.IsIntegralType());
            cg.EmitConvertIntToLong(indexType);

            return cg.EmitCall(ILOpCode.Call, operation);
        }
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

            type = cg.EmitAsObject(type, out bool isnull);
            Debug.Assert(type.IsReferenceType);

            //
            var tref = (BoundTypeRef)AsType;
            if (tref.ResolvedType.IsValidType())
            {
                if (!isnull)
                {
                    // Template: value is T : object
                    cg.Builder.EmitOpCode(ILOpCode.Isinst);
                    cg.EmitSymbolToken(tref.ResolvedType, null);

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
                tref.EmitLoadTypeInfo(cg, false);

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
            var sourcefile = cg.ContainingFile;

            switch (this.ConstType)
            {
                case PseudoConstUse.Types.File:

                    // <ctx>.RootPath + RelativePath
                    cg.EmitLoadContext();
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.RootPath.Getter);

                    cg.Builder.EmitStringConstant("/" + sourcefile.RelativeFilePath);
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.NormalizePath_string);  // normalize slashes

                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Concat_String_String)
                        .Expect(SpecialType.System_String);

                case PseudoConstUse.Types.Dir:

                    // <ctx>.RootPath + RelativeDirectory
                    cg.EmitLoadContext();
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.RootPath.Getter);

                    var relative_dir = sourcefile.DirectoryRelativePath;
                    if (relative_dir.Length != 0)
                    {
                        cg.Builder.EmitStringConstant("/" + relative_dir);
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.NormalizePath_string);  // normalize slashes

                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.Concat_String_String);
                    }

                    return cg.CoreTypes.String;

                case PseudoConstUse.Types.Class:

                    // resolve name of self in runtime:
                    // Template: (string)Operators.GetSelfOrNull(<self>)?.Name
                    cg.EmitLoadSelf(throwOnError: false);  // GetSelf() : PhpTypeInfo
                    cg.EmitNullCoalescing(
                        () => cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetName_PhpTypeInfo.Getter), // Name : string
                        () => cg.Builder.EmitStringConstant(string.Empty));

                    return cg.CoreTypes.String;

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
            switch (this.ConstType)
            {
                case PseudoClassConstUse.Types.Class:
                    this.TargetType.EmitClassName(cg);
                    return cg.CoreTypes.String;

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.ConstType);
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

            // resolved constant value
            if (this.ConstantValue.HasValue)
            {
                return cg.EmitLoadConstant(this.ConstantValue.Value, this.Access.TargetType);
            }

            // resolved constant symbol
            if (_boundExpressionOpt != null)
            {
                return _boundExpressionOpt.EmitLoadValue(cg, BoundAccess.Read);
            }

            // the constant has to be resolved in runtime,
            // make it easier by caching its internal ID for fast lookup

            // Template: internal static int <const>Name;
            var idxfield = cg.Module.SynthesizedManager.GetGlobalConstantIndexField(Name.ToString());

            Debug.Assert(FallbackName.HasValue == false || Name != FallbackName.Value);

            // Template: Operators.ReadConstant(ctx, name, ref <idxfield> [, fallbackname])
            cg.EmitLoadContext();
            cg.Builder.EmitStringConstant(this.Name.ToString());
            cg.EmitFieldAddress(idxfield);

            if (FallbackName.HasValue)
            {
                // we have to try two possible constant names:
                cg.Builder.EmitStringConstant(this.FallbackName.Value.ToString());
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.ReadConstant_Context_String_Int_String)
                    .Expect(cg.CoreTypes.PhpValue);
            }
            else
            {
                // Operators.ReadConstant(ctx, name, ref <idxfield>)
                return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.ReadConstant_Context_String_Int)
                    .Expect(cg.CoreTypes.PhpValue);
            }
        }
    }

    partial class BoundIsEmptyEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var t = cg.Emit(this.Operand);

            // resolve IsEmpty() operator
            var op = cg.Conversions.ResolveOperator(t, false, new[] { "IsEmpty" }, new[] { cg.CoreTypes.Operators.Symbol }, target: cg.CoreTypes.Boolean);
            if (op != null)
            {
                // TODO: instance method call and possibly NULL => check (VALUE == NULL) || ...

                cg.EmitConversion(new CommonConversion(true, false, false, false, false, op), t, cg.CoreTypes.Boolean);
            }
            else
            {
                var il = cg.Builder;

                //
                switch (t.SpecialType)
                {
                    case SpecialType.System_Object:
                        // object == null
                        il.EmitNullConstant();
                        il.EmitOpCode(ILOpCode.Ceq);
                        break;

                    case SpecialType.System_Double:
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_String:
                        // Template: !(bool)value
                        cg.EmitConvertToBool(t, this.Operand.TypeRefMask);
                        cg.EmitLogicNegation();
                        break;

                    default:

                        // (value).IsEmpty
                        cg.EmitConvert(t, this.Operand.TypeRefMask, cg.CoreTypes.PhpValue);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.IsEmpty_PhpValue)
                            .Expect(SpecialType.System_Boolean);
                }
            }

            //
            return cg.CoreTypes.Boolean;
        }
    }

    partial class BoundIsSetEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var t = cg.Emit(this.VarReference);

            // t.IsSet
            if (t == cg.CoreTypes.PhpAlias)
            {
                // <PhpAlias>.Value
                t = cg.Emit_PhpAlias_GetValue();
            }

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
            else if (t.IsNullableType())
            {
                // Teplate: value.HasValue
                cg.EmitStructAddr(t); // value -> ref value
                cg.EmitCall(ILOpCode.Call, t.LookupMember<PropertySymbol>("HasValue").GetMethod)
                    .Expect(SpecialType.System_Boolean);
            }
            else
            {
                // clean this up ...
                // NOTICE: "IndirectProperty" performs `isset` by itself and returns the result as boolean
                if (t.SpecialType == SpecialType.System_Boolean)
                {
                    if (VarReference is BoundFieldRef boundfld)
                    {
                        if (boundfld.BoundReference is IndirectProperty)
                        {
                            // isset already checked by callsite:
                            return t;
                        }
                    }
                }

                // value type => true
                cg.EmitPop(t);
                cg.Builder.EmitBoolConstant(true);
            }

            //
            return cg.CoreTypes.Boolean;
        }
    }

    partial class BoundOffsetExists
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            // Operators.OffsetExists( Receiver, Index ) : bool

            var arrayType = cg.Emit(Receiver);
            var indexType = cg.Emit(Index);

            //if (arrayType.IsOfType(cg.CoreTypes.ArrayAccess))
            //{
            //    cg.EmitConvert(indexType, 0, cg.CoreTypes.PhpValue);
            //    return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Operators.offsetExists_ArrayAccess_PhpValue);
            //}

            var op = cg.Conversions.ResolveOperator(arrayType, false, new[] { "offsetExists" }, new[] { cg.CoreTypes.Operators.Symbol }, operand: indexType, target: cg.CoreTypes.Boolean);
            if (op != null)
            {
                cg.EmitConversion(new CommonConversion(true, false, false, false, false, op), arrayType, cg.CoreTypes.Boolean, op: indexType);
                return cg.CoreTypes.Boolean;
            }
            else
            {
                throw cg.NotImplementedException($"offsetExists({arrayType}, {indexType})", this);
            }
        }
    }

    partial class BoundTryGetItem
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(!Access.IsEnsure);

            // Either specialize the call for PhpArray (possibly with string index) or fall back to PhpValue

            TypeSymbol arrType, indexType;
            MethodSymbol operation;

            var arrTypeMask = Array.TypeRefMask;
            if (arrTypeMask.IsSingleType && !arrTypeMask.IsRef && cg.TypeRefContext.IsArray(arrTypeMask))
            {
                arrType = cg.EmitSpecialize(Array);
            }
            else
            {
                arrType = cg.EmitConvertToPhpValue(Array);
            }

            var indexTypeMask = Index.TypeRefMask;
            if (arrType == cg.CoreTypes.PhpArray &&
                indexTypeMask.IsSingleType && !indexTypeMask.IsRef && cg.TypeRefContext.IsReadonlyString(indexTypeMask))
            {
                indexType = cg.EmitSpecialize(Index);
            }
            else
            {
                indexType = cg.EmitConvertToPhpValue(Index);
            }

            if (arrType == cg.CoreTypes.PhpArray)
            {
                if (indexType == cg.CoreTypes.String)
                {
                    operation = cg.CoreMethods.Operators.TryGetItemValue_PhpArray_string_PhpValueRef;
                }
                else
                {
                    Debug.Assert(indexType == cg.CoreTypes.PhpValue);
                    operation = cg.CoreMethods.Operators.TryGetItemValue_PhpArray_PhpValue_PhpValueRef;
                }
            }
            else
            {
                Debug.Assert(arrType == cg.CoreTypes.PhpValue);
                Debug.Assert(indexType == cg.CoreTypes.PhpValue);
                operation = cg.CoreMethods.Operators.TryGetItemValue_PhpValue_PhpValue_PhpValueRef;
            }

            // TryGetItemValue(Array, Index, out PhpValue temp) ? temp : Fallback

            object trueLbl = new object();
            object endLbl = new object();

            // call
            var temp = cg.GetTemporaryLocal(cg.CoreTypes.PhpValue);
            cg.Builder.EmitLocalAddress(temp);
            cg.EmitCall(ILOpCode.Call, operation);
            cg.Builder.EmitBranch(ILOpCode.Brtrue, trueLbl);

            // fallback:
            cg.EmitConvertToPhpValue(Fallback);
            cg.Builder.EmitBranch(ILOpCode.Br, endLbl);

            // trueLbl:
            cg.Builder.MarkLabel(trueLbl);
            cg.Builder.EmitLocalLoad(temp);

            // endLbl:
            cg.Builder.MarkLabel(endLbl);

            cg.ReturnTemporaryLocal(temp);

            return cg.CoreTypes.PhpValue;
        }
    }

    partial class BoundYieldEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(cg.GeneratorStateMachineMethod != null);

            if (this.Access.IsNone)
            {
                return cg.CoreTypes.Void;
            }
            else if (this.Access.IsRead)
            {
                // leave result of yield expr. (sent value) on eval stack

                cg.EmitGeneratorInstance();
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.GetGeneratorSentItem_Generator);

                // type of expression result is PHP value (sent value)
                return cg.CoreTypes.PhpValue;
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(this.Access);
            }
        }
    }

    partial class BoundYieldFromEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            if (Access.IsRead)
            {
                var t = cg.EmitAsObject(cg.Emit(Operand), out bool isnull);
                Debug.Assert(t.IsReferenceType);

                if (isnull || (
                    !cg.CoreTypes.Generator.Symbol.IsOfType(t) &&
                    !t.IsOfType(cg.CoreTypes.Generator)))
                {
                    cg.EmitPop(t);
                    cg.Emit_PhpValue_Null();
                }
                else
                {
                    var il = cg.Builder;
                    var lbl_End = new NamedLabel("Generator_Null");

                    if (t != cg.CoreTypes.Generator)
                    {
                        // Template: (Operand as Generator)?.getReturn() : PhpValue
                        cg.Builder.EmitOpCode(ILOpCode.Isinst);
                        cg.EmitSymbolToken(cg.CoreTypes.Generator, null);

                        var lbl_notnull = new NamedLabel("Generator_NotNull");
                        il.EmitOpCode(ILOpCode.Dup);
                        il.EmitBranch(ILOpCode.Brtrue, lbl_notnull);

                        il.EmitOpCode(ILOpCode.Pop);
                        cg.Emit_PhpValue_Null();
                        il.EmitBranch(ILOpCode.Br, lbl_End);

                        il.MarkLabel(lbl_notnull);
                    }

                    // Generator.getReturn() : PhpValue
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreTypes.Generator.Method("getReturn"));

                    il.MarkLabel(lbl_End);
                }

                //
                return cg.CoreTypes.PhpValue;
            }
            else
            {
                return cg.CoreTypes.Void;
            }
        }
    }
}
