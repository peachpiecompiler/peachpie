using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax.AST;
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
                    //codeGenerator.EmitBoxing(node.LeftExpr.Emit(codeGenerator));
                    //codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                    //returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Pow.Object_Object);
                    //break;
                    throw new NotImplementedException();

                case Operations.Mod:
                    //Template: "x % y"        Operators.Remainder(x,y)

                    //codeGenerator.EmitBoxing(node.LeftExpr.Emit(codeGenerator));
                    //ro_typecode = node.RightExpr.Emit(codeGenerator);
                    //switch (ro_typecode)
                    //{
                    //    case PhpTypeCode.Integer:
                    //        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Remainder.Object_Int32);
                    //        break;

                    //    default:
                    //        codeGenerator.EmitBoxing(ro_typecode);
                    //        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Remainder.Object_Object);
                    //        break;
                    //}
                    //break;
                    throw new NotImplementedException();

                case Operations.ShiftLeft:

                    //// LOAD Operators.ShiftLeft(box left, box right);
                    //codeGenerator.EmitBoxing(node.LeftExpr.Emit(codeGenerator));
                    //codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                    //returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.ShiftLeft);
                    //break;
                    throw new NotImplementedException();

                case Operations.ShiftRight:

                    //// LOAD Operators.ShiftRight(box left, box right);
                    //codeGenerator.EmitBoxing(node.LeftExpr.Emit(codeGenerator));
                    //codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                    //returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.ShiftRight);
                    //break;
                    throw new NotImplementedException();

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
                    //returned_typecode = EmitBitOperation(node, codeGenerator, Operators.BitOp.And);
                    //break;
                    throw new NotImplementedException();

                case Operations.BitOr:
                    //returned_typecode = EmitBitOperation(node, codeGenerator, Operators.BitOp.Or);
                    //break;
                    throw new NotImplementedException();

                case Operations.BitXor:
                    //returned_typecode = EmitBitOperation(node, codeGenerator, Operators.BitOp.Xor);
                    //break;
                    throw new NotImplementedException();

                #endregion

                #region Comparing Operations

                case Operations.Equal:
                    EmitEquality(cg);
                    returned_type = cg.CoreTypes.Boolean;
                    break;

                case Operations.NotEqual:
                    EmitEquality(cg);
                    cg.EmitLogicNegation();
                    returned_type = cg.CoreTypes.Boolean;
                    break;

                case Operations.GreaterThan:
                    returned_type = EmitComparison(cg, false);
                    break;

                case Operations.LessThan:
                    returned_type = EmitComparison(cg, true);
                    break;

                case Operations.GreaterThanOrEqual:
                    // template: !(LessThan)
                    returned_type = EmitComparison(cg, true);
                    cg.EmitLogicNegation();
                    break;

                case Operations.LessThanOrEqual:
                    // template: !(GreaterThan)
                    returned_type = EmitComparison(cg, false);
                    cg.EmitLogicNegation();
                    break;

                case Operations.Identical:

                    //// LOAD Operators.StrictEquality(box left,box right);
                    //returned_typecode = EmitStrictEquality(node, codeGenerator);
                    //break;
                    throw new NotImplementedException();

                case Operations.NotIdentical:

                    //// LOAD Operators.StrictEquality(box left,box right) == false;
                    //EmitStrictEquality(node, codeGenerator);

                    //codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    //codeGenerator.IL.Emit(OpCodes.Ceq);

                    //returned_typecode = PhpTypeCode.Boolean;
                    //break;
                    throw new NotImplementedException();

                #endregion

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            //
            switch (Access.Flags)
            {
                case AccessMask.Read:
                    // Result is read, do nothing.
                    Debug.Assert(returned_type.SpecialType != SpecialType.System_Void);
                    break;

                case AccessMask.None:
                    // Result is not read, pop the result
                    cg.EmitPop(returned_type);
                    returned_type = cg.CoreTypes.Void;
                    break;
            }

            //
            return returned_type;
        }

        /// <summary>
        /// Emits <c>+</c> operator suitable for actual operands.
        /// </summary>
        private static TypeSymbol EmitAdd(CodeGenerator cg, BoundExpression Left, BoundExpression Right, TypeSymbol resultTypeOpt = null)
        {
            // Template: x + y
            return EmitAdd(cg, cg.Emit(Left), Right, resultTypeOpt);
        }

        /// <summary>
        /// Emits <c>+</c> operator suitable for actual operands.
        /// </summary>
        internal static TypeSymbol EmitAdd(CodeGenerator cg, TypeSymbol xtype, BoundExpression Right, TypeSymbol resultTypeOpt = null)
        {
            var il = cg.Builder;

            xtype = cg.EmitConvertIntToLong(xtype);    // int|bool -> long

            //
            if (xtype == cg.CoreTypes.PhpNumber)
            {
                var ytype = cg.EmitConvertIntToLong(cg.Emit(Right));  // int|bool -> long

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

                //
                throw new NotImplementedException();
            }
            else if (xtype.SpecialType == SpecialType.System_Double)
            {
                var ytype = cg.EmitConvertNumberToDouble(Right); // bool|int|long|number -> double

                if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // r8 + r8 : r8
                    il.EmitOpCode(ILOpCode.Add);
                    return cg.CoreTypes.Double;
                }

                //
                throw new NotImplementedException();
            }
            else if (xtype.SpecialType == SpecialType.System_Int64)
            {
                var ytype = cg.EmitConvertIntToLong(cg.Emit(Right));    // int|bool -> long

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

                //
                throw new NotImplementedException();
            }
            else if (xtype == cg.CoreTypes.PhpValue)
            {
                var ytype = cg.EmitConvertIntToLong(cg.Emit(Right));    // int|bool -> long

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
                else if (ytype == cg.CoreTypes.PhpNumber)
                {
                    // value + number : number
                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Add_value_number)
                        .Expect(cg.CoreTypes.PhpNumber);
                }

                //
                throw new NotImplementedException();
            }

            //
            throw new NotImplementedException();
        }

        /// <summary>
        /// Emits subtraction operator.
        /// </summary>
        internal static TypeSymbol EmitSub(CodeGenerator cg, BoundExpression Left, BoundExpression Right, TypeSymbol resultTypeOpt = null)
        {
            return EmitSub(cg, cg.Emit(Left), Right, resultTypeOpt);
        }

        /// <summary>
        /// Emits subtraction operator.
        /// </summary>
        internal static TypeSymbol EmitSub(CodeGenerator cg, TypeSymbol xtype, BoundExpression Right, TypeSymbol resultTypeOpt = null)
        {
            var il = cg.Builder;

            xtype = cg.EmitConvertIntToLong(xtype);    // int|bool -> int64
            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertIntToLong(cg.Emit(Right));
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
                    throw new NotImplementedException();
                case SpecialType.System_Double:
                    ytype = cg.EmitConvertNumberToDouble(Right); // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 - r8 : r8
                        il.EmitOpCode(ILOpCode.Sub);
                        return cg.CoreTypes.Double;
                    }
                    throw new NotImplementedException();
                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertIntToLong(cg.Emit(Right));
                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // number - long : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_number_long)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            // number - double : double
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_number_double)
                                .Expect(SpecialType.System_Double);
                        }
                        else if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // number - number : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Subtract_number_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                    }
                    throw new NotImplementedException();
            }
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
            var partial_eval_label = new object();
            var end_label = new object();

            // IF [!]<(bool) Left> THEN GOTO partial_eval;
            cg.EmitConvertToBool(Left);
            il.EmitBranch(isAnd ? ILOpCode.Brfalse : ILOpCode.Brtrue, partial_eval_label);

            // <RESULT> = <(bool) Right>;
            cg.EmitConvertToBool(Right);

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
            cg.EmitConvertToBool(Left);
            cg.EmitConvertToBool(Right);
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
            return EmitEquality(cg, cg.Emit(left), right);
        }

        /// <summary>
        /// Emits check for values equality.
        /// Lefts <c>bool</c> on top of evaluation stack.
        /// </summary>
        internal static TypeSymbol EmitEquality(CodeGenerator cg, TypeSymbol xtype, BoundExpression right)
        {
            if (xtype.SpecialType == SpecialType.System_Double)
            {
                cg.EmitConvert(right, cg.CoreTypes.Double);    // TODO: only value types, otherwise fallback to generic CompareOp(double, object)
                cg.Builder.EmitOpCode(ILOpCode.Ceq);
            }
            else if (xtype.SpecialType == SpecialType.System_Int64)
            {
                cg.EmitConvert(right, cg.CoreTypes.Long);    // TODO: only value types, otherwise fallback to generic CompareOp(double, object)
                cg.Builder.EmitOpCode(ILOpCode.Ceq);
            }
            else if (xtype == cg.CoreTypes.PhpNumber)
            {
                cg.EmitConvert(right, cg.CoreTypes.PhpNumber); // TODO: only value types, otherwise fallback to generic CompareOp(double, object)
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Eq_number_number)
                    .Expect(SpecialType.System_Boolean);
            }
            else
            {
                throw new NotImplementedException();
            }

            //
            return cg.CoreTypes.Boolean;
        }

        /// <summary>
        /// Emits comparison operator pushing <c>bool</c> (<c>i4</c> of value <c>0</c> or <c>1</c>) onto the evaluation stack.
        /// </summary>
        /// <param name="cg">Code generator helper.</param>
        /// <param name="lt">True for <c>clt</c> (less than) otherwise <c>cgt</c> (greater than).</param>
        /// <returns>Resulting type code pushed onto the top of evaliuation stack.</returns>
        TypeSymbol EmitComparison(CodeGenerator cg, bool lt)
        {
            var il = cg.Builder;

            var xtype = cg.Emit(Left);
            var ytype = cg.Emit(Right);

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Int64:
                    if (ytype.SpecialType == SpecialType.System_Int32 ||
                        ytype.SpecialType == SpecialType.System_Int64)
                    {
                        if (ytype.SpecialType != SpecialType.System_Int64)
                            il.EmitOpCode(ILOpCode.Conv_i8);

                        il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                        break;
                    }
                    throw new NotImplementedException();

                case SpecialType.System_Double:
                    cg.EmitConvertToDouble(ytype, Right.TypeRefMask);
                    il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    break;

                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertIntToLong(ytype);    // bool|int -> long
                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // number <> long
                            return cg.EmitCall(ILOpCode.Call, lt
                                ? cg.CoreMethods.PhpNumber.lt_number_long
                                : cg.CoreMethods.PhpNumber.gt_number_long)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            // number <> double
                            return cg.EmitCall(ILOpCode.Call, lt
                                ? cg.CoreMethods.PhpNumber.lt_number_double
                                : cg.CoreMethods.PhpNumber.gt_number_double)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else // TODO: only if convertable to number for sure
                        {
                            cg.EmitConvertToPhpNumber(ytype, Right.TypeRefMask);
                            // number <> number
                            return cg.EmitCall(ILOpCode.Call, lt
                                ? cg.CoreMethods.PhpNumber.lt_number_number
                                : cg.CoreMethods.PhpNumber.gt_number_number)
                                .Expect(SpecialType.System_Boolean);
                        }
                        //else
                        //{
                        //    // Opewrator.Compare(x, y) : int
                        //}

                        //// lt <=> comparison < 0
                        //// gt <=> comparison > 0
                        //il.EmitOpCode(ILOpCode.Ldc_i4_0, 1);    // +1 on stack
                        //il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                        //break;
                    }
                    throw new NotImplementedException();
            }

            // always bool
            return cg.CoreTypes.Boolean;
        }

        /// <summary>
        /// Emits <c>*</c> operation.
        /// </summary>
        TypeSymbol EmitMultiply(CodeGenerator cg)
        {
            var il = cg.Builder;

            var xtype = cg.Emit(Left);
            xtype = cg.EmitConvertIntToLong(xtype);    // int|bool -> int64

            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Double:
                    ytype = cg.EmitConvertNumberToDouble(Right); // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 * r8 : r8
                        il.EmitOpCode(ILOpCode.Mul);
                        return xtype;   // r8
                    }
                    throw new NotImplementedException();
                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertIntToLong(cg.Emit(Right));
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
                    throw new NotImplementedException();
                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertIntToLong(cg.Emit(Right));

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
                        else
                        {
                            // number * number : number
                            cg.EmitConvertToPhpNumber(ytype, Right.TypeRefMask);
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Mul_number_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                    }
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Emits <c>/</c> operator.
        /// </summary>
        TypeSymbol EmitDivision(CodeGenerator cg)
        {
            var il = cg.Builder;

            var xtype = cg.Emit(Left);
            xtype = cg.EmitConvertIntToLong(xtype);    // int|bool -> int64
            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Double:
                    ytype = cg.EmitConvertNumberToDouble(Right); // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        il.EmitOpCode(ILOpCode.Div);
                        return xtype;   // r8
                    }

                    throw new NotImplementedException();
                case SpecialType.System_Int64:
                    ytype = cg.EmitConvertIntToLong(cg.Emit(Right));  // bool|int -> long
                    if (ytype == cg.CoreTypes.PhpNumber)
                    {
                        // long / number : number
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Division_long_number)
                            .Expect(cg.CoreTypes.PhpNumber);
                    }
                    throw new NotImplementedException();
                default:
                    if (xtype == cg.CoreTypes.PhpNumber)
                    {
                        ytype = cg.EmitConvertIntToLong(cg.Emit(Right));  // bool|int -> long
                        if (ytype == cg.CoreTypes.PhpNumber)
                        {
                            // nmumber / number : number
                            return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Division_number_number)
                                .Expect(cg.CoreTypes.PhpNumber);
                        }
                    }
                    throw new NotImplementedException();
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
                    //codeGenerator.EmitLoadScriptContext();
                    //il.Emit(OpCodes.Call, Methods.ScriptContext.DisableErrorReporting);
                    //returned_typecode = node.Expr.Emit(codeGenerator);
                    //codeGenerator.EmitLoadScriptContext();
                    //il.Emit(OpCodes.Call, Methods.ScriptContext.EnableErrorReporting);
                    //break;
                    throw new NotImplementedException();

                case Operations.BitNegation:
                    //Template: "~x" Operators.BitNot(x)                                     
                    //codeGenerator.EmitBoxing(node.Expr.Emit(codeGenerator));
                    //il.Emit(OpCodes.Call, Methods.Operators.BitNot);
                    //returned_typecode = PhpTypeCode.Object;
                    //break;
                    throw new NotImplementedException();

                case Operations.Clone:
                    // Template: clone x        Operators.Clone(x,DTypeDesc,ScriptContext)
                    //codeGenerator.EmitBoxing(node.Expr.Emit(codeGenerator));
                    //codeGenerator.EmitLoadClassContext();
                    //codeGenerator.EmitLoadScriptContext();
                    //il.Emit(OpCodes.Call, Methods.Operators.Clone);
                    //returned_typecode = PhpTypeCode.Object;
                    //break;
                    throw new NotImplementedException();

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
                    //Template: "(array)x"   Convert.ObjectToArray(x)
                    //o_typecode = node.Expr.Emit(codeGenerator);
                    //if (o_typecode != PhpTypeCode.PhpArray)
                    //{
                    //    codeGenerator.EmitBoxing(o_typecode);
                    //    il.Emit(OpCodes.Call, Methods.Convert.ObjectToPhpArray);
                    //}
                    //returned_typecode = PhpTypeCode.PhpArray;
                    //break;
                    throw new NotImplementedException();

                case Operations.UnsetCast:
                    // Template: "(unset)x"  null
                    //il.Emit(OpCodes.Ldnull);
                    //returned_typecode = PhpTypeCode.Object;
                    //break;
                    throw new NotImplementedException();

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            switch (Access.Flags)
            {
                case AccessMask.Read:
                    Debug.Assert(returned_type.SpecialType != SpecialType.System_Void);
                    // do nothing
                    break;
                case AccessMask.None:
                    // pop operation's result value from stack
                    cg.EmitPop(returned_type);
                    returned_type = cg.CoreTypes.Void;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(Access);
            }

            return returned_type;
        }

        TypeSymbol EmitMinus(CodeGenerator cg)
        {
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
                    if (t == cg.CoreTypes.PhpNumber)
                    {
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpNumber.Negation)
                            .Expect(t);
                    }

                    throw new NotImplementedException();
            }
        }

        TypeSymbol EmitPlus(CodeGenerator cg)
        {
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
                    if (t == cg.CoreTypes.PhpNumber)
                    {
                        return t;
                    }

                    // TODO: IPhpConvertible.ToNumber otherwise 0L

                    throw new NotImplementedException();
            }
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
            var value = ConstantValue.Value;
            if (value == null)
            {
                if (this.Access.TargetType == cg.CoreTypes.PhpValue)
                {
                    return cg.Emit_PhpValue_Null();
                }

                cg.Builder.EmitNullConstant();
                return cg.CoreTypes.Object;
            }
            else
            {
                if (value is int)
                {
                    cg.Builder.EmitIntConstant((int)value);
                    return cg.CoreTypes.Int32;
                }
                else if (value is long)
                {
                    cg.Builder.EmitLongConstant((long)value);
                    return cg.CoreTypes.Long;
                }
                else if (value is string)
                {
                    cg.Builder.EmitStringConstant((string)value);
                    return cg.CoreTypes.String;
                }
                else if (value is bool)
                {
                    cg.Builder.EmitBoolConstant((bool)value);
                    return cg.CoreTypes.Boolean;
                }
                else if (value is double)
                {
                    cg.Builder.EmitDoubleConstant((double)value);
                    return cg.CoreTypes.Double;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
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

    partial class BoundFieldRef : IBoundReference
    {
        internal override IBoundReference BindPlace(CodeGenerator cg) => this;

        internal override IPlace Place(ILBuilder il) => null;

        #region IBoundReference

        DynamicOperationFactory.CallSiteData _lazyLoadCallSite = null;
        DynamicOperationFactory.CallSiteData _lazyStoreCallSite = null;

        public bool HasAddress => Field != null;

        /// <summary>
        /// Emits ldfld, stfld, ldflda, ldsfld, stsfld.
        /// </summary>
        /// <param name="cg"></param>
        /// <param name="code">ld* or st* OP code.</param>
        void EmitOpCode(CodeGenerator cg, ILOpCode code)
        {
            Debug.Assert(Field != null);
            cg.Builder.EmitOpCode(code);
            cg.EmitSymbolToken(Field, null);
        }

        void EmitOpCode_Load(CodeGenerator cg)
        {
            EmitOpCode(cg, Field.IsStatic ? ILOpCode.Ldsfld : ILOpCode.Ldfld);
        }

        void EmitOpCode_LoadAddress(CodeGenerator cg)
        {
            EmitOpCode(cg, Field.IsStatic ? ILOpCode.Ldsflda : ILOpCode.Ldflda);
        }

        void EmitOpCode_Store(CodeGenerator cg)
        {
            EmitOpCode(cg, Field.IsStatic ? ILOpCode.Stsfld : ILOpCode.Stfld);
        }

        TypeSymbol IBoundReference.Type => Field?.Type;

        void IBoundReference.EmitLoadPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            if (Field == null)
            {
                if (_lazyLoadCallSite == null)
                    _lazyLoadCallSite = cg.Factory.StartCallSite("get_" + this.Name.Value);

                // callsite.Target callsite
                _lazyLoadCallSite.EmitLoadTarget(cg.Builder);
                _lazyLoadCallSite.Place.EmitLoad(cg.Builder);
            }

            // instance
            InstanceCacheHolder.EmitInstance(instanceOpt, cg, Instance);
        }

        TypeSymbol IBoundReference.EmitLoad(CodeGenerator cg)
        {
            Debug.Assert(Access.IsRead);

            if (Field == null)  // call site
            {
                Debug.Assert(_lazyLoadCallSite != null);
                Debug.Assert(this.Instance.ResultType != null);

                // resolve actual return type
                TypeSymbol return_type;
                if (Access.EnsureObject) return_type = cg.CoreTypes.Object;
                else if (Access.EnsureArray) return_type = cg.CoreTypes.PhpArray;
                else if (Access.IsReadRef) return_type = cg.CoreTypes.PhpAlias;
                else return_type = Access.TargetType ?? cg.CoreTypes.PhpValue;

                // Target()
                var functype = cg.Factory.GetCallSiteDelegateType(
                    this.Instance.ResultType, RefKind.None,
                    ImmutableArray<TypeSymbol>.Empty,
                    default(ImmutableArray<RefKind>),
                    null,
                    return_type);


                cg.EmitCall(ILOpCode.Callvirt, functype.DelegateInvokeMethod);

                //
                _lazyLoadCallSite.Construct(functype, cctor =>
                {
                    // new GetFieldBinder(field_name, context, return, flags)
                    cctor.EmitStringConstant(this.Name.Value);
                    cctor.EmitLoadToken(cg.Module, cg.Diagnostics, cg.Routine.ContainingType, null);
                    cctor.EmitLoadToken(cg.Module, cg.Diagnostics, return_type, null);
                    cctor.EmitIntConstant((int)Access.AccessFlags);
                    cctor.EmitCall(cg.Module, cg.Diagnostics, ILOpCode.Newobj, cg.CoreMethods.Dynamic.GetFieldBinder_ctor);
                });

                //
                return return_type;
            }
            else // direct
            {
                var type = Field.Type;

                if (Field.IsStatic && Instance != null)
                    cg.EmitPop(Instance.ResultType);
                else if (!Field.IsStatic && Instance == null)
                    throw new NotImplementedException();

                // Ensure Object (..->Field->.. =)
                if (Access.EnsureObject)
                {
                    if (type == cg.CoreTypes.PhpAlias)
                    {
                        EmitOpCode_Load(cg);    // PhpAlias
                        cg.EmitLoadContext();   // Context
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.EnsureObject_Context)
                            .Expect(SpecialType.System_Object);
                    }
                    else if (type == cg.CoreTypes.PhpValue)
                    {
                        EmitOpCode_LoadAddress(cg); // &PhpValue
                        cg.EmitLoadContext();       // Context
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.EnsureObject_Context)
                            .Expect(SpecialType.System_Object);
                    }
                    else
                    {
                        if (type.IsReferenceType)
                        {
                            // TODO: ensure it is not null
                            EmitOpCode_Load(cg);
                            return type;
                        }
                        else
                        {
                            // return new stdClass(ctx)
                            throw new NotImplementedException();
                        }
                    }
                }
                // Ensure Array (xxx->Field[] =)
                else if (Access.EnsureArray)
                {
                    if (type == cg.CoreTypes.PhpAlias)
                    {
                        EmitOpCode_Load(cg);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpAlias.EnsureArray)
                            .Expect(cg.CoreTypes.PhpArray);
                    }
                    else if (type == cg.CoreTypes.PhpValue)
                    {
                        EmitOpCode_LoadAddress(cg);
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.EnsureArray)
                                .Expect(cg.CoreTypes.PhpArray);
                    }
                    else if (type == cg.CoreTypes.PhpArray)
                    {
                        // TODO: ensure it is not null
                        EmitOpCode_Load(cg);
                        return type;
                    }

                    throw new NotImplementedException();
                }
                // Ensure Alias (&...->Field)
                else if (Access.IsReadRef)
                {
                    if (type == cg.CoreTypes.PhpAlias)
                    {
                        // TODO: <place>.AddRef()
                        EmitOpCode_Load(cg);
                        return type;
                    }
                    else if (type == cg.CoreTypes.PhpValue)
                    {
                        // return <place>.EnsureAlias()
                        EmitOpCode_LoadAddress(cg); // &PhpValue
                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.EnsureAlias)
                            .Expect(cg.CoreTypes.PhpAlias);
                    }
                    else
                    {
                        Debug.Assert(false, "value cannot be aliased");

                        // new PhpAlias((PhpValue)<place>, 1)
                        EmitOpCode_Load(cg);
                        cg.EmitConvertToPhpValue(type, 0);
                        return cg.Emit_PhpValue_MakeAlias();
                    }
                }
                // Read (...->Field) & Dereference eventually
                else
                {
                    if (type == cg.CoreTypes.PhpAlias)
                    {
                        EmitOpCode_Load(cg);

                        if (Access.TargetType != null)
                        {
                            // convert PhpValue to target type without loading whole value and storing to temporary variable
                            switch (Access.TargetType.SpecialType)
                            {
                                default:
                                    if (Access.TargetType == cg.CoreTypes.PhpArray)
                                    {
                                        // <PhpAlias>.Value.AsArray()
                                        cg.Builder.EmitOpCode(ILOpCode.Ldflda);
                                        cg.EmitSymbolToken(cg.CoreMethods.PhpAlias.Value, null);
                                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.AsArray);
                                    }
                                    break;
                            }
                        }

                        return cg.Emit_PhpAlias_GetValue();
                    }
                    else if (type == cg.CoreTypes.PhpValue)
                    {
                        if (Access.TargetType != null)
                        {
                            // convert PhpValue to target type without loading whole value and storing to temporary variable
                            switch (Access.TargetType.SpecialType)
                            {
                                case SpecialType.System_Double:
                                    EmitOpCode_LoadAddress(cg); // &PhpValue.ToDouble()
                                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.ToDouble);
                                case SpecialType.System_Int64:
                                    EmitOpCode_LoadAddress(cg); // &PhpValue.ToLong()
                                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.ToLong);
                                case SpecialType.System_Boolean:
                                    EmitOpCode_LoadAddress(cg); // &PhpValue.ToBoolean()
                                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.ToBoolean);
                                case SpecialType.System_String:
                                    EmitOpCode_LoadAddress(cg); // &PhpValue.ToString(ctx)
                                    cg.EmitLoadContext();
                                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.ToString_Context);
                                case SpecialType.System_Object:
                                    EmitOpCode_LoadAddress(cg); // &PhpValue.ToClass(ctx)
                                    cg.EmitLoadContext();
                                    return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.ToClass_Context);
                                default:
                                    if (Access.TargetType == cg.CoreTypes.PhpArray)
                                    {
                                        EmitOpCode_LoadAddress(cg); // &PhpValue.AsArray()
                                        return cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.AsArray);
                                    }
                                    break;
                            }
                        }

                        // TODO: dereference if applicable (=> PhpValue.Alias.Value)
                        EmitOpCode_Load(cg);
                        return type;
                    }
                    else
                    {
                        EmitOpCode_Load(cg);
                        return type;
                    }
                }
            }
        }

        void IBoundReference.EmitStorePrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            Debug.Assert(Access.IsWrite);

            if (Field == null)
            {
                if (_lazyStoreCallSite == null)
                    _lazyStoreCallSite = cg.Factory.StartCallSite("set_" + this.Name.Value);

                // callsite.Target callsite
                _lazyStoreCallSite.EmitLoadTarget(cg.Builder);
                _lazyStoreCallSite.Place.EmitLoad(cg.Builder);

                // instance
                InstanceCacheHolder.EmitInstance(instanceOpt, cg, Instance);
            }
            else
            {
                var type = Field.Type;

                // instance
                InstanceCacheHolder.EmitInstance(instanceOpt, cg, Instance);

                //
                if (Access.IsWriteRef)
                {
                    // no need for preparation
                }
                else
                {
                    //
                    if (type == cg.CoreTypes.PhpAlias)
                    {
                        // (PhpAlias)<place>
                        EmitOpCode_Load(cg);    // PhpAlias
                    }
                    else if (type == cg.CoreTypes.PhpValue)
                    {
                        EmitOpCode_LoadAddress(cg); // &PhpValue
                    }
                    else
                    {
                        // no need for preparation
                    }
                }
            }
        }

        void IBoundReference.EmitStore(CodeGenerator cg, TypeSymbol valueType)
        {
            Debug.Assert(Access.IsWrite);

            if (Field == null)
            {
                Debug.Assert(_lazyStoreCallSite != null);
                Debug.Assert(this.Instance.ResultType != null);

                // Target()
                var functype = cg.Factory.GetCallSiteDelegateType(
                    this.Instance.ResultType, RefKind.None,
                    ImmutableArray.Create(valueType),
                    default(ImmutableArray<RefKind>),
                    null,
                    cg.CoreTypes.Void);

                cg.EmitCall(ILOpCode.Callvirt, functype.DelegateInvokeMethod);

                _lazyStoreCallSite.Construct(functype, cctor =>
                {
                    cctor.EmitStringConstant(this.Name.Value);
                    cctor.EmitLoadToken(cg.Module, cg.Diagnostics, cg.Routine.ContainingType, null);
                    cctor.EmitIntConstant((int)Access.AccessFlags);   // flags
                    cctor.EmitCall(cg.Module, cg.Diagnostics, ILOpCode.Newobj, cg.CoreMethods.Dynamic.SetFieldBinder_ctor);
                });
            }
            else
            {
                if (!Field.IsStatic && Instance == null)
                    throw new NotImplementedException();

                var type = Field.Type;

                if (Access.IsWriteRef)
                {
                    if (valueType != cg.CoreTypes.PhpAlias)
                    {
                        Debug.Assert(false, "caller should get aliased value");
                        cg.EmitConvertToPhpValue(valueType, 0);
                        valueType = cg.Emit_PhpValue_MakeAlias();
                    }

                    //
                    if (type == cg.CoreTypes.PhpAlias)
                    {
                        // <place> = <alias>
                        EmitOpCode_Store(cg);
                    }
                    else if (type == cg.CoreTypes.PhpValue)
                    {
                        // <place> = PhpValue.Create(<alias>)
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Create_PhpAlias);
                        EmitOpCode_Store(cg);
                    }
                    else
                    {
                        Debug.Assert(false, "Assigning alias to non-aliasable field.");
                        cg.EmitConvert(valueType, 0, type);
                        EmitOpCode_Store(cg);
                    }
                }
                else
                {
                    //
                    if (type == cg.CoreTypes.PhpAlias)
                    {
                        // <Field>.Value = <value>
                        cg.EmitConvertToPhpValue(valueType, 0);
                        cg.Emit_PhpAlias_SetValue();
                    }
                    else if (type == cg.CoreTypes.PhpValue)
                    {
                        // Operators.SetValue(ref <Field>, (PhpValue)<value>);
                        cg.EmitConvertToPhpValue(valueType, 0);
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.Operators.SetValue_PhpValueRef_PhpValue);
                    }
                    else
                    {
                        cg.EmitConvert(valueType, 0, type);
                        EmitOpCode_Store(cg);
                    }
                }

                // 
                if (Field.IsStatic && Instance != null)
                    cg.EmitPop(Instance.ResultType);
            }
        }

        #endregion
    }

    partial class BoundRoutineCall
    {

    }

    partial class BoundFunctionCall
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var overloads = this.Overloads;
            if (overloads == null)
                throw new InvalidOperationException();  // function call has to be analyzed first

            Debug.Assert(overloads.Candidates.All(c => c.IsStatic));

            // TODO: emit check the routine is declared; options:
            // 1. disable checks in release for better performance
            // 2. autoload script containing routine declaration
            // 3. throw if routine is not declared

            return cg.EmitCall(ILOpCode.Call, null, overloads, _arguments.Select(a => a.Value).ToImmutableArray());
        }
    }

    partial class BoundInstanceMethodCall
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var overloads = this.Overloads;
            if (overloads != null)
            {
                Debug.Assert(overloads.Candidates.All(c => !c.IsStatic));
            }

            // TODO: emit check the containing type is declared; options:
            // 1. disable checks in release for better performance
            // 2. autoload script containing the declaration
            // 3. throw if type is not declared

            if (overloads != null && overloads.IsFinal && overloads.Candidates.Length == 1)
            {
                // direct call
                var method = overloads.Candidates[0];
                return cg.EmitCall(
                    (method.IsOverride && !method.IsMetadataFinal) ? ILOpCode.Callvirt : ILOpCode.Call,
                    method,
                    this.Instance, _arguments.Select(a => a.Value).ToImmutableArray());
            }
            else
            {
                // call site call

                var callsite = cg.Factory.StartCallSite("call_" + this.Name.Value);

                var callsiteargs = new List<TypeSymbol>(1 + _arguments.Length);
                var return_type = this.Access.IsRead
                        ? this.Access.IsReadRef
                            ? cg.CoreTypes.PhpAlias.Symbol
                            : (this.Access.TargetType ?? cg.CoreTypes.PhpValue.Symbol)
                        : cg.CoreTypes.Void.Symbol;

                // callsite
                var fldPlace = callsite.Place;

                // callsite.Target
                callsite.EmitLoadTarget(cg.Builder);

                // (callsite, instance, ctx, ...)
                fldPlace.EmitLoad(cg.Builder);
                cg.Emit(this.Instance);   // instance

                callsiteargs.Add(cg.EmitLoadContext());     // ctx

                foreach (var a in _arguments)
                {
                    callsiteargs.Add(cg.Emit(a.Value));
                }

                //
                Debug.Assert(this.Instance.ResultType != null);

                // Target()
                var functype = cg.Factory.GetCallSiteDelegateType(
                    this.Instance.ResultType, RefKind.None,
                    callsiteargs.AsImmutable(),
                    default(ImmutableArray<RefKind>),
                    null,
                    return_type);

                cg.EmitCall(ILOpCode.Callvirt, functype.DelegateInvokeMethod);

                callsite.Construct(functype, cctor =>
                {
                    cctor.EmitStringConstant(this.Name.Value);
                    cctor.EmitLoadToken(cg.Module, cg.Diagnostics, cg.Routine.ContainingType, null);
                    cctor.EmitLoadToken(cg.Module, cg.Diagnostics, return_type, null);
                    cctor.EmitIntConstant(0);
                    cctor.EmitCall(cg.Module, cg.Diagnostics, ILOpCode.Call, cg.CoreMethods.Dynamic.CallMethodBinder_Create);
                });

                //
                return return_type;
            }
        }
    }

    partial class BoundStMethodCall
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var overloads = this.Overloads;
            if (overloads == null)
                throw new InvalidOperationException();  // function call has to be analyzed first

            Debug.Assert(overloads.Candidates.All(c => c.IsStatic));

            // TODO: emit check the containing type is declared; options:
            // 1. disable checks in release for better performance
            // 2. autoload script containing the declaration
            // 3. throw if type is not declared

            return cg.EmitCall(ILOpCode.Call, null, overloads, _arguments.Select(a => a.Value).ToImmutableArray());
        }
    }

    partial class BoundEcho
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(Access.IsNone);

            foreach (var arg in _arguments)
            {
                cg.EmitEcho(arg.Value);
            }

            return cg.CoreTypes.Void;
        }
    }

    partial class BoundConcatEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            var phpstring = cg.CoreTypes.PhpString;

            // new PhpString(capacity)
            cg.Emit_New_PhpString(CapacityHint());

            // <STACK>.Add(<expr>)
            foreach (var x in this.ArgumentsInSourceOrder)
            {
                var expr = x.Value;
                if (IsEmpty(expr))
                    continue;

                //
                cg.Builder.EmitOpCode(ILOpCode.Dup);    // PhpString

                // TODO: Add overloads for specific types, not System.String only
                cg.EmitConvert(expr, cg.CoreTypes.String);
                cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpString.Append_String);
                cg.Builder.EmitOpCode(ILOpCode.Nop);
            }

            //
            return phpstring;
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

        /// <summary>
        /// Guesses initial string builder capacity.
        /// </summary>
        int CapacityHint()
        {
            int capacity = 0;
            foreach (var x in this.ArgumentsInSourceOrder)
            {
                var expr = x.Value;
                if (IsEmpty(expr))
                    continue;

                if (expr is BoundLiteral)
                {
                    var value = expr.ConstantValue.Value;
                    if (value != null)
                    {
                        capacity += value.ToString().Length;
                    }
                }
                else
                {
                    capacity += 4;
                }
            }
            return capacity;
        }
    }

    partial class BoundNewEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            if (this.Overloads == null || this.ResultType == null || this.ResultType is ErrorTypeSymbol)
                throw new InvalidOperationException();

            if (this.Overloads.IsMethodKindConsistent() != MethodKind.Constructor)
                throw new ArgumentException();

            //
            var type = cg.EmitCall(ILOpCode.Newobj, null, this.Overloads, _arguments.Select(a => a.Value).ToImmutableArray())
                .Expect((TypeSymbol)this.ResultType);

            //
            if (this.Access.IsNone)
            {
                cg.EmitPop(type);
                type = cg.CoreTypes.Void;
            }

            //
            return type;
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
            var t_value = target_place.Type;
            if (t_value == cg.CoreTypes.PhpAlias || t_value == cg.CoreTypes.PhpValue)
                t_value = null; // no inplace conversion

            LocalDefinition tmp = null;

            // <target> = <value>
            target_place.EmitStorePrepare(cg);

            // TODO: load value & dereference eventually
            if (t_value != null) cg.EmitConvert(this.Value, t_value);   // TODO: do not convert here yet
            else t_value = cg.Emit(this.Value);

            switch (this.Access.Flags)
            {
                case AccessMask.Read:
                    tmp = cg.GetTemporaryLocal(t_value, false);
                    cg.Builder.EmitOpCode(ILOpCode.Dup);
                    cg.Builder.EmitLocalStore(tmp);
                    break;
                case AccessMask.None:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(this.Access);
            }

            target_place.EmitStore(cg, t_value);

            //
            switch (this.Access.Flags)
            {
                case AccessMask.None:
                    t_value = cg.CoreTypes.Void;
                    break;
                case AccessMask.Read:
                    cg.Builder.EmitLocalLoad(tmp);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(this.Access);
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
            throw new NotSupportedException();  // TODO
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
            LocalDefinition postfix_temp = null;

            var read = this.Access.IsRead;

            var target_place = this.Target.BindPlace(cg);
            var instance_holder = new InstanceCacheHolder();
            Debug.Assert(target_place != null);

            // prepare target for store operation
            target_place.EmitStorePrepare(cg, instance_holder);

            // load target value
            target_place.EmitLoadPrepare(cg, instance_holder);
            var target_load_type = target_place.EmitLoad(cg);

            TypeSymbol op_type;

            if (read && IsPostfix)
            {
                // store original value of target
                // <temp> = TARGET
                postfix_temp = cg.GetTemporaryLocal(target_load_type);
                cg.EmitOpCode(ILOpCode.Dup);
                cg.Builder.EmitLocalStore(postfix_temp);
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

            if (read)
            {
                if (IsPostfix)
                {
                    // READ <temp>
                    cg.Builder.EmitLocalLoad(postfix_temp);
                    result_type = target_load_type;

                    //
                    cg.ReturnTemporaryLocal(postfix_temp);
                    postfix_temp = null;
                }
                else
                {
                    // dup resulting value
                    // READ (++TARGET OR --TARGET)
                    cg.Builder.EmitOpCode(ILOpCode.Dup);
                    result_type = op_type;
                }
            }

            //
            target_place.EmitStore(cg, op_type);

            //
            instance_holder.Dispose();
            Debug.Assert(postfix_temp == null);
            Debug.Assert(!read || result_type.SpecialType != SpecialType.System_Void);
            
            //
            return result_type;
        }

        bool IsPostfix => this.IncrementKind == UnaryOperationKind.OperatorPostfixIncrement || this.IncrementKind == UnaryOperationKind.OperatorPostfixDecrement;
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
                cg.EmitConvertToBool(this.Condition);   // i4
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

                // value | alias
                Debug.Assert(x.Value != null);

                var byref = x.Value.Access.IsReadRef;
                var valuetype = byref ? cg.CoreTypes.PhpAlias : cg.CoreTypes.PhpValue;
                cg.EmitConvert(x.Value, valuetype);

                if (x.Key != null)
                {
                    if (byref)  // .SetItemAlias( key, PhpAlias )
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.SetItemAlias_IntStringKey_PhpAlias);
                    else   // .SetItemValue( key, PhpValue )
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.SetItemValue_IntStringKey_PhpValue);
                }
                else
                {
                    if (byref)  // PhpValue.Create( PhpAlias )
                        cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Create_PhpAlias);

                    // .AddValue( PhpValue )
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpArray.AddValue_PhpValue);
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
            this.Array.Access = this.Array.Access.WithRead(cg.CoreTypes.PhpArray);
            _type = Access.IsReadRef ? cg.CoreTypes.PhpAlias : cg.CoreTypes.PhpValue;
            return this;
        }

        internal override IPlace Place(ILBuilder il) => null;

        #region IBoundReference

        TypeSymbol IBoundReference.Type => _type;
        TypeSymbol _type;

        void IBoundReference.EmitLoadPrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            // Template: array[index]

            InstanceCacheHolder.EmitInstance(instanceOpt, cg, this.Array);

            if (this.Index == null)
                throw new ArgumentException();

            cg.EmitIntStringKey(this.Index);    // TODO: save Index into InstanceCacheHolder
        }

        TypeSymbol IBoundReference.EmitLoad(CodeGenerator cg)
        {
            // Template: array[index]

            // array on top of stack already
            if (this.Array.ResultType != cg.CoreTypes.PhpArray)
                throw new NotImplementedException();    // TODO: emit convert as PhpArray

            if (Access.EnsureObject)
            {
                // <array>.EnsureItemObject(<key>, ctx)
                cg.EmitLoadContext();
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.EnsureItemObject_IntStringKey_Context);
            }
            else if (Access.EnsureArray)
            {
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.EnsureItemArray_IntStringKey);
            }
            else if (Access.IsReadRef)
            {
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.EnsureItemAlias_IntStringKey);
            }
            else
            {
                Debug.Assert(Access.IsRead);
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.GetItemValue_IntStringKey);
            }
        }

        void IBoundReference.EmitStorePrepare(CodeGenerator cg, InstanceCacheHolder instanceOpt)
        {
            // Template: array[index]

            InstanceCacheHolder.EmitInstance(instanceOpt, cg, this.Array);

            if (this.Index != null)
            {
                cg.EmitIntStringKey(this.Index);    // TODO: save Index into InstanceCacheHolder
            }
        }

        void IBoundReference.EmitStore(CodeGenerator cg, TypeSymbol valueType)
        {
            // Template: array[index]

            if(Access.IsWriteRef)
            {
                // PhpAlias
                if(valueType != cg.CoreTypes.PhpAlias)
                {
                    cg.EmitConvertToPhpValue(valueType, 0);
                    cg.Emit_PhpValue_MakeAlias();
                }

                // .SetItemAlias(key, alias) or .AddValue(PhpValue.Create(alias))
                if (this.Index != null)
                {
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.SetItemAlias_IntStringKey_PhpAlias);
                }
                else
                {
                    cg.EmitCall(ILOpCode.Call, cg.CoreMethods.PhpValue.Create_PhpAlias);
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.AddValue_PhpValue);
                }
            }
            else
            {
                Debug.Assert(Access.IsWrite);

                cg.EmitConvertToPhpValue(valueType, 0);

                // .SetItemValue(key, value) or .AddValue(value)
                if (this.Index != null)
                {
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.SetItemValue_IntStringKey_PhpValue);
                }
                else
                {
                    cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.PhpArray.AddValue_PhpValue);
                }
            }
        }

        #endregion
    }
}
