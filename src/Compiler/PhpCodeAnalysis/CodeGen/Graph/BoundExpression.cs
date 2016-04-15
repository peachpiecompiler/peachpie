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
                    returned_type = (cg.IsLongOnly(this.TypeRefMask)) ? cg.CoreTypes.Long.Symbol : null;
                    returned_type = EmitAdd(cg, Left, Right, returned_type);
                    break;

                case Operations.Sub:
                    //Template: "x - y"        Operators.Subtract(x,y) [overloads]
                    returned_type = EmitSub(cg, Left, Right);
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
        internal static TypeSymbol EmitAdd(CodeGenerator gen, BoundExpression Left, BoundExpression Right, TypeSymbol resultTypeOpt = null)
        {
            // Template: x + y : Operators.Add(x,y) [overloads]

            var il = gen.Builder;

            var xtype = gen.Emit(Left);
            xtype = gen.EmitConvertIntToLong(xtype);    // int|bool -> long

            //
            if (xtype == gen.CoreTypes.PhpNumber)
            {
                var ytype = gen.EmitConvertIntToLong(gen.Emit(Right));  // int|bool -> long

                if (ytype == gen.CoreTypes.PhpNumber)
                {
                    // number + number : number
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_number_number)
                        .Expect(gen.CoreTypes.PhpNumber);
                }
                else if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // number + r8 : r8
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_number_double)
                        .Expect(SpecialType.System_Double);
                }
                else if (ytype.SpecialType == SpecialType.System_Int64)
                {
                    // number + long : number
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_number_long)
                        .Expect(gen.CoreTypes.PhpNumber);
                }

                //
                throw new NotImplementedException();
            }
            else if (xtype.SpecialType == SpecialType.System_Double)
            {
                var ytype = gen.EmitConvertNumberToDouble(Right); // bool|int|long|number -> double

                if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // r8 + r8 : r8
                    il.EmitOpCode(ILOpCode.Add);
                    return gen.CoreTypes.Double;
                }

                //
                throw new NotImplementedException();
            }
            else if (xtype.SpecialType == SpecialType.System_Int64)
            {
                var ytype = gen.EmitConvertIntToLong(gen.Emit(Right));    // int|bool -> long

                if (ytype.SpecialType == SpecialType.System_Int64)
                {
                    if (resultTypeOpt != null)
                    {
                        if (resultTypeOpt.SpecialType == SpecialType.System_Int64)
                        {
                            // (long)(i8 + i8 : number)
                            il.EmitOpCode(ILOpCode.Add);
                            return gen.CoreTypes.Long;
                        }
                    }

                    // i8 + i8 : number
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_long_long)
                        .Expect(gen.CoreTypes.PhpNumber);
                }
                else if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // i8 + r8 : r8
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_long_double)
                        .Expect(SpecialType.System_Double);
                }
                else if (ytype == gen.CoreTypes.PhpNumber)
                {
                    // i8 + number : number
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_long_number)
                        .Expect(gen.CoreTypes.PhpNumber);
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
        internal static TypeSymbol EmitSub(CodeGenerator gen, BoundExpression Left, BoundExpression Right)
        {
            var il = gen.Builder;

            var xtype = gen.Emit(Left);
            xtype = gen.EmitConvertIntToLong(xtype);    // int|bool -> int64
            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Int64:
                    ytype = gen.EmitConvertIntToLong(gen.Emit(Right));
                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // i8 - i8 : number
                        return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Subtract_long_long)
                            .Expect(gen.CoreTypes.PhpNumber);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // i8 - r8 : r8
                        return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Subtract_long_double)
                            .Expect(gen.CoreTypes.Double);
                    }
                    else if (ytype == gen.CoreTypes.PhpNumber)
                    {
                        // i8 - number : number
                        return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Subtract_long_number)
                            .Expect(gen.CoreTypes.PhpNumber);
                    }
                    throw new NotImplementedException();
                case SpecialType.System_Double:
                    ytype = gen.EmitConvertNumberToDouble(Right); // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 - r8 : r8
                        il.EmitOpCode(ILOpCode.Sub);
                        return gen.CoreTypes.Double;
                    }
                    throw new NotImplementedException();
                default:
                    if (xtype == gen.CoreTypes.PhpNumber)
                    {
                        ytype = gen.EmitConvertIntToLong(gen.Emit(Right));
                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // number - long : number
                            return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Subtract_number_long)
                                .Expect(gen.CoreTypes.PhpNumber);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            // number - double : double
                            return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Subtract_number_double)
                                .Expect(SpecialType.System_Double);
                        }
                        else if (ytype == gen.CoreTypes.PhpNumber)
                        {
                            // number - number : number
                            return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Subtract_number_number)
                                .Expect(gen.CoreTypes.PhpNumber);
                        }
                    }
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Emits binary boolean operation (AND or OR).
        /// </summary>
        /// <param name="gen">A code generator.</param>
        /// <param name="isAnd">Whether to emit AND, otherwise OR.</param>
        /// <returns>A type code of the result.</returns>
        TypeSymbol EmitBinaryBooleanOperation(CodeGenerator gen, bool isAnd)
        {
            var boolean = gen.CoreTypes.Boolean;  // typeof(bool)

            var il = gen.Builder;
            var partial_eval_label = new object();
            var end_label = new object();

            // IF [!]<(bool) Left> THEN GOTO partial_eval;
            gen.EmitConvertToBool(Left);
            il.EmitBranch(isAnd ? ILOpCode.Brfalse : ILOpCode.Brtrue, partial_eval_label);

            // <RESULT> = <(bool) Right>;
            gen.EmitConvertToBool(Right);

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
        TypeSymbol EmitBinaryXor(CodeGenerator gen)
        {
            // LOAD <(bool) leftSon> == <(bool) rightSon>;
            gen.EmitConvertToBool(Left);
            gen.EmitConvertToBool(Right);
            gen.EmitOpCode(ILOpCode.Ceq);

            gen.EmitOpCode(ILOpCode.Ldc_i4_0);
            gen.EmitOpCode(ILOpCode.Ceq);

            return gen.CoreTypes.Boolean;
        }

        /// <summary>
        /// Emits check for values equality.
        /// </summary>
        TypeSymbol EmitEquality(CodeGenerator gen)
        {
            // x == y

            var xtype = gen.Emit(Left);
            if (xtype.SpecialType == SpecialType.System_Double)
            {
                gen.EmitConvertToDouble(gen.Emit(Right), Right.TypeRefMask);    // TODO: only value types, otherwise fallback to generic CompareOp(double, object)
                gen.Builder.EmitOpCode(ILOpCode.Ceq);
            }
            else if (xtype == gen.CoreTypes.PhpNumber)
            {
                gen.EmitConvertToPhpNumber(gen.Emit(Right), Right.TypeRefMask); // TODO: only value types, otherwise fallback to generic CompareOp(double, object)
                gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Eq_number_number)
                    .Expect(SpecialType.System_Boolean);
            }
            else
            {
                throw new NotImplementedException();
            }

            //
            return gen.CoreTypes.Boolean;
        }

        /// <summary>
        /// Emits comparison operator pushing <c>bool</c> (<c>i4</c> of value <c>0</c> or <c>1</c>) onto the evaluation stack.
        /// </summary>
        /// <param name="gen">Code generator helper.</param>
        /// <param name="lt">True for <c>clt</c> (less than) otherwise <c>cgt</c> (greater than).</param>
        /// <returns>Resulting type code pushed onto the top of evaliuation stack.</returns>
        TypeSymbol EmitComparison(CodeGenerator gen, bool lt)
        {
            var il = gen.Builder;

            var xtype = gen.Emit(Left);
            var ytype = gen.Emit(Right);

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
                    gen.EmitConvertToDouble(ytype, Right.TypeRefMask);
                    il.EmitOpCode(lt ? ILOpCode.Clt : ILOpCode.Cgt);
                    break;

                default:
                    if (xtype == gen.CoreTypes.PhpNumber)
                    {
                        ytype = gen.EmitConvertIntToLong(ytype);    // bool|int -> long
                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            // number <> long
                            return gen.EmitCall(ILOpCode.Call, lt
                                ? gen.CoreMethods.PhpNumber.lt_number_long
                                : gen.CoreMethods.PhpNumber.gt_number_long)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            // number <> double
                            return gen.EmitCall(ILOpCode.Call, lt
                                ? gen.CoreMethods.PhpNumber.lt_number_double
                                : gen.CoreMethods.PhpNumber.gt_number_double)
                                .Expect(SpecialType.System_Boolean);
                        }
                        else // TODO: only if convertable to number for sure
                        {
                            gen.EmitConvertToPhpNumber(ytype, Right.TypeRefMask);
                            // number <> number
                            return gen.EmitCall(ILOpCode.Call, lt
                                ? gen.CoreMethods.PhpNumber.lt_number_number
                                : gen.CoreMethods.PhpNumber.gt_number_number)
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
            return gen.CoreTypes.Boolean;
        }

        /// <summary>
        /// Emits <c>*</c> operation.
        /// </summary>
        TypeSymbol EmitMultiply(CodeGenerator gen)
        {
            var il = gen.Builder;

            var xtype = gen.Emit(Left);
            xtype = gen.EmitConvertIntToLong(xtype);    // int|bool -> int64

            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Double:
                    ytype = gen.EmitConvertNumberToDouble(Right); // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // r8 * r8 : r8
                        il.EmitOpCode(ILOpCode.Mul);
                        return xtype;   // r8
                    }
                    throw new NotImplementedException();
                case SpecialType.System_Int64:
                    ytype = gen.EmitConvertIntToLong(gen.Emit(Right));
                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        // i8 * i8 : number
                        return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Mul_long_long)
                                .Expect(gen.CoreTypes.PhpNumber);
                    }
                    else if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        // i8 * r8 : r8
                        return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Mul_long_double)
                                .Expect(SpecialType.System_Double);
                    }
                    else if (ytype == gen.CoreTypes.PhpNumber)
                    {
                        // i8 * number : number
                        return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Mul_long_number)
                                .Expect(gen.CoreTypes.PhpNumber);
                    }
                    throw new NotImplementedException();
                default:
                    if (xtype == gen.CoreTypes.PhpNumber)
                    {
                        ytype = gen.EmitConvertIntToLong(gen.Emit(Right));

                        if (ytype.SpecialType == SpecialType.System_Int64)
                        {
                            return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Mul_number_long)
                                .Expect(gen.CoreTypes.PhpNumber);
                        }
                        else if (ytype.SpecialType == SpecialType.System_Double)
                        {
                            return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Mul_number_double)
                                .Expect(gen.CoreTypes.Double);
                        }
                        else
                        {
                            // number * number : number
                            gen.EmitConvertToPhpNumber(ytype, Right.TypeRefMask);
                            return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Mul_number_number)
                                .Expect(gen.CoreTypes.PhpNumber);
                        }
                    }
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Emits <c>/</c> operator.
        /// </summary>
        TypeSymbol EmitDivision(CodeGenerator gen)
        {
            var il = gen.Builder;

            var xtype = gen.Emit(Left);
            xtype = gen.EmitConvertIntToLong(xtype);    // int|bool -> int64
            TypeSymbol ytype;

            switch (xtype.SpecialType)
            {
                case SpecialType.System_Double:
                    ytype = gen.EmitConvertNumberToDouble(Right); // bool|int|long|number -> double
                    if (ytype.SpecialType == SpecialType.System_Double)
                    {
                        il.EmitOpCode(ILOpCode.Div);
                        return xtype;   // r8
                    }

                    throw new NotImplementedException();
                case SpecialType.System_Int64:
                    ytype = gen.EmitConvertIntToLong(gen.Emit(Right));  // bool|int -> long
                    if (ytype == gen.CoreTypes.PhpNumber)
                    {
                        // long / number : number
                        return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Division_long_number)
                            .Expect(gen.CoreTypes.PhpNumber);
                    }
                    throw new NotImplementedException();
                default:
                    if (xtype == gen.CoreTypes.PhpNumber)
                    {
                        ytype = gen.EmitConvertIntToLong(gen.Emit(Right));  // bool|int -> long
                        if (ytype == gen.CoreTypes.PhpNumber)
                        {
                            // nmumber / number : number
                            return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Division_number_number)
                                .Expect(gen.CoreTypes.PhpNumber);
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
        internal override TypeSymbol Emit(CodeGenerator cxg)
        {
            Debug.Assert(this.Access.IsRead || Access.IsNone);

            // do nothing
            if (this.Access.IsNone)
            {
                return cxg.CoreTypes.Void;
            }

            // push value onto the evaluation stack
            if (!ConstantValue.HasValue)
                throw new InvalidOperationException();

            // TOOD: use ConstantValue

            var value = ConstantValue.Value;
            if (value == null)
            {
                cxg.Builder.EmitNullConstant();
                return cxg.CoreTypes.Object;
            }
            else
            {
                if (value is int)
                {
                    cxg.Builder.EmitIntConstant((int)value);
                    return cxg.CoreTypes.Int32;
                }
                else if (value is long)
                {
                    cxg.Builder.EmitLongConstant((long)value);
                    return cxg.CoreTypes.Long;
                }
                else if (value is string)
                {
                    cxg.Builder.EmitStringConstant((string)value);
                    return cxg.CoreTypes.String;
                }
                else if (value is bool)
                {
                    cxg.Builder.EmitBoolConstant((bool)value);
                    return cxg.CoreTypes.Boolean;
                }
                else if (value is double)
                {
                    cxg.Builder.EmitDoubleConstant((double)value);
                    return cxg.CoreTypes.Double;
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
        /// Gets <see cref="IBoundPlace"/> providing load and store operations.
        /// </summary>
        internal abstract IBoundPlace BindPlace(CodeGenerator cg);

        internal abstract IPlace Place(ILBuilder il);
    }

    partial class BoundVariableRef
    {
        internal override IBoundPlace BindPlace(CodeGenerator cg) => this.Variable.BindPlace(cg.Builder);

        internal override IPlace Place(ILBuilder il) => this.Variable.Place(il);

        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            if (this.Variable == null)
                throw new InvalidOperationException(); // variable was not resolved

            if (Access.IsNone)
            {
                // do nothing
                return cg.CoreTypes.Void;
            }

            //
            return EmitLoad(cg);
        }

        internal TypeSymbol EmitLoad(CodeGenerator cg)
        {
            return cg.EmitLoad(this.Variable);
        }
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

                var callsitetype = cg.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite);    // temporary, we will change to specific generic once we know
                var callsitetype_generic = cg.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite_T);    // temporary, we will change to specific generic once we know
                var callsite_create_generic = cg.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CallSite_T__Create);
                var target = (FieldSymbol)cg.DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CallSite_T__Target);
                target = new SubstitutedFieldSymbol(callsitetype_generic, target); // AsMember // we'll change containing type later once we know

                var container = (IWithSynthesized)cg.Routine.ContainingType;
                var fld = container.CreateSynthesizedField(callsitetype, "__'" + this.Name + "'" + (this.GetHashCode() % 128).ToString("x"), Accessibility.Private, true);
                var cctor = cg.Module.GetStaticCtorBuilder(cg.Routine.ContainingType);

                var callsiteargs = new List<TypeSymbol>(1 + _arguments.Length);
                var return_type = this.Access.IsRead ? cg.CoreTypes.PhpValue.Symbol : cg.CoreTypes.Void.Symbol;

                // callsite
                var fldPlace = new FieldPlace(null, fld);

                // callsite.Target
                fldPlace.EmitLoad(cg.Builder);
                cg.Builder.EmitOpCode(ILOpCode.Ldfld);
                cg.EmitSymbolToken(target, null);
                
                // (callsite, instance, ctx, ...)
                fldPlace.EmitLoad(cg.Builder);
                cg.Emit(this.Instance);   // instance

                callsiteargs.Add(cg.EmitLoadContext());     // ctx

                foreach (var a in _arguments)
                {
                    callsiteargs.Add(cg.Emit(a.Value));
                }

                //
                var functype = cg.Factory.GetCallSiteDelegateType(
                    this.Instance.ResultType, RefKind.None,
                    callsiteargs.AsImmutable(),
                    default(ImmutableArray<RefKind>),
                    null,
                    return_type);

                callsitetype = callsitetype_generic.Construct(functype);
                ((SubstitutedFieldSymbol)target).SetContainingType((SubstitutedNamedTypeSymbol)callsitetype);

                fld.SetFieldType(callsitetype);

                // Target()
                var invoke = functype.DelegateInvokeMethod;
                cg.EmitCall(ILOpCode.Callvirt, invoke);

                // static .cctor {

                // fld = CallSite<T>.Create( CallMethodBinder.Create( name, currentclass, returntype, generics ) )

                fldPlace.EmitStorePrepare(cctor);

                cctor.EmitStringConstant(this.Name.Value);
                cctor.EmitLoadToken(cg.Module, cg.Diagnostics, cg.Routine.ContainingType, null);
                cctor.EmitLoadToken(cg.Module, cg.Diagnostics, return_type, null);
                cctor.EmitIntConstant(0);
                cctor.EmitCall(cg.Module, cg.Diagnostics, ILOpCode.Call, cg.CoreMethods.CallMethodBinder.Create);

                cctor.EmitCall(cg.Module, cg.Diagnostics, ILOpCode.Call, (MethodSymbol)callsite_create_generic.SymbolAsMember(callsitetype));

                fldPlace.EmitStore(cctor);

                // }

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
                    var value = ((BoundExpression)expr).ConstantValue.Value;
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
            LocalDefinition tmp = null;

            // <target> = <value>
            target_place.EmitPrepare(cg);
            if (t_value != null) cg.EmitConvert(this.Value, t_value);
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
            throw new NotSupportedException();  // transform to BoundAssignEx with BoundBinaryEx as its Value
        }
    }

    partial class BoundIncDecEx
    {
        internal override TypeSymbol Emit(CodeGenerator cg)
        {
            Debug.Assert(this.Access.IsNone || Access.IsRead);

            if (this.UsesOperatorMethod)
            {
                throw new NotImplementedException();
            }

            var targetPlace = this.Target.BindPlace(cg);
            var t_value = targetPlace.Type;
            var read = this.Access.IsRead;

            // Postfix (i++, i--)
            if (this.IncrementKind == UnaryOperationKind.OperatorPostfixIncrement)
            {
                if (read)
                    throw new NotImplementedException();

                targetPlace.EmitPrepare(cg);
                var result = BoundBinaryEx.EmitAdd(cg, this.Target, this.Value, t_value);
                cg.EmitConvert(result, this.TypeRefMask, t_value);
                targetPlace.EmitStore(cg, t_value);

                //
                return cg.CoreTypes.Void;
            }
            else if (this.IncrementKind == UnaryOperationKind.OperatorPostfixDecrement)
            {
                if (read)
                    throw new NotImplementedException();

                throw new NotImplementedException();
            }
            // Prefix (++i, --i)
            if (this.IncrementKind == UnaryOperationKind.OperatorPrefixIncrement)
            {
                targetPlace.EmitPrepare(cg);
                var result = BoundBinaryEx.EmitAdd(cg, this.Target, this.Value, t_value);
                cg.EmitConvert(result, this.TypeRefMask, t_value);

                if (read)
                    cg.Builder.EmitOpCode(ILOpCode.Dup);

                targetPlace.EmitStore(cg, t_value);

                //
                if (read)
                    return t_value;
                else
                    return cg.CoreTypes.Void;
            }
            else if (this.IncrementKind == UnaryOperationKind.OperatorPrefixDecrement)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(this.IncrementKind);
            }
        }
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
}
