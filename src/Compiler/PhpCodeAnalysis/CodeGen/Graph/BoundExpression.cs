using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax.AST;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
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
        internal virtual TypeSymbol Emit(CodeGenerator il)
        {
            throw ExceptionUtilities.UnexpectedValue(this.GetType().FullName);
        }
    }

    partial class BoundBinaryEx
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            Debug.Assert(this.Access == AccessType.Read || this.Access == AccessType.None);

            TypeSymbol returned_type;

            if (UsesOperatorMethod)
            {
                throw new NotImplementedException();    // call this.Operator(Left, Right)
            }

            switch (this.Operation)
            {
                #region Arithmetic Operations

                case Operations.Add:
                    returned_type = EmitAdd(il, Left, Right);
                    break;

                case Operations.Sub:
                    //Template: "x - y"        Operators.Subtract(x,y) [overloads]
                    returned_type = EmitSub(il, Left, Right);
                    break;

                case Operations.Div:
                    //Template: "x / y"
                    returned_type = EmitDivision(il);
                    break;

                case Operations.Mul:
                    //Template: "x * y"
                    returned_type = EmitMultiply(il);
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
                    returned_type = EmitBinaryBooleanOperation(il, true);
                    break;

                case Operations.Or:
                    returned_type = EmitBinaryBooleanOperation(il, false);
                    break;

                case Operations.Xor:
                    returned_type = EmitBinaryXor(il);
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
                    EmitEquality(il);
                    returned_type = il.CoreTypes.Boolean;
                    break;

                case Operations.NotEqual:
                    EmitEquality(il);
                    il.EmitLogicNegation();
                    returned_type = il.CoreTypes.Boolean;
                    break;

                case Operations.GreaterThan:
                    returned_type = EmitComparison(il, false);
                    break;

                case Operations.LessThan:
                    returned_type = EmitComparison(il, true);
                    break;

                case Operations.GreaterThanOrEqual:
                    // template: !(LessThan)
                    returned_type = EmitComparison(il, true);
                    il.EmitLogicNegation();
                    break;

                case Operations.LessThanOrEqual:
                    // template: !(GreaterThan)
                    returned_type = EmitComparison(il, false);
                    il.EmitLogicNegation();
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
            switch (Access)
            {
                case AccessType.Read:
                    // Result is read, do nothing.
                    Debug.Assert(returned_type.SpecialType != SpecialType.System_Void);
                    break;

                case AccessType.None:
                    // Result is not read, pop the result
                    il.EmitPop(returned_type);
                    returned_type = il.CoreTypes.Void;
                    break;
            }

            //
            return returned_type;
        }

        /// <summary>
        /// Emits <c>+</c> operator suitable for actual operands.
        /// </summary>
        internal static TypeSymbol EmitAdd(CodeGenerator gen, BoundExpression Left, BoundExpression Right)
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
                        il.EmitOpCode(ILOpCode.Mul);
                        return xtype;   // r8
                    }
                    throw new NotImplementedException();
                case SpecialType.System_Int64:
                    ytype = gen.EmitConvertIntToLong(gen.Emit(Right));
                    if (ytype.SpecialType == SpecialType.System_Int64)
                    {
                        return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Mul_long_long)
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
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            Debug.Assert(Access == AccessType.Read || Access == AccessType.None);

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
                    il.EmitConvertToBool(this.Operand, true);
                    returned_type = il.CoreTypes.Boolean;
                    break;

                case Operations.Minus:
                    //Template: "-x"  Operators.Minus(x)
                    returned_type = EmitMinus(il);
                    break;

                case Operations.ObjectCast:
                    //Template: "(object)x"   Convert.ObjectToDObject(x,ScriptContext)
                    //codeGenerator.EmitBoxing(node.Expr.Emit(codeGenerator));
                    //codeGenerator.EmitLoadScriptContext();
                    //il.Emit(OpCodes.Call, Methods.Convert.ObjectToDObject);
                    //returned_typecode = PhpTypeCode.Object;
                    //break;
                    throw new NotImplementedException();

                case Operations.Plus:
                    //Template: "+x"  Operators.Plus(x)
                    //codeGenerator.EmitBoxing(node.Expr.Emit(codeGenerator));
                    //il.Emit(OpCodes.Call, Methods.Operators.Plus);
                    //returned_typecode = PhpTypeCode.Object;
                    //break;
                    throw new NotImplementedException();

                case Operations.Print:
                    il.EmitEcho(this.Operand);

                    if (Access == AccessType.Read)
                    {
                        // Always returns 1
                        il.Builder.EmitLongConstant(1);
                        returned_type = il.CoreTypes.Long;
                    }
                    else
                    {
                        // nobody reads the result anyway
                        returned_type = il.CoreTypes.Void;
                    }
                    break;

                case Operations.BoolCast:
                    //Template: "(bool)x"     Convert.ObjectToBoolean(x)
                    il.EmitConvert(this.Operand, il.CoreTypes.Boolean);
                    returned_type = il.CoreTypes.Boolean;
                    break;

                case Operations.Int8Cast:
                case Operations.Int16Cast:
                case Operations.Int32Cast:
                case Operations.UInt8Cast:
                case Operations.UInt16Cast:

                case Operations.UInt64Cast:
                case Operations.UInt32Cast:
                case Operations.Int64Cast:

                    il.EmitConvert(this.Operand, il.CoreTypes.Long);
                    returned_type = il.CoreTypes.Long;
                    break;

                case Operations.DecimalCast:
                case Operations.DoubleCast:
                case Operations.FloatCast:

                    il.EmitConvert(this.Operand, il.CoreTypes.Double);
                    returned_type = il.CoreTypes.Double;
                    break;

                case Operations.UnicodeCast: // TODO
                case Operations.StringCast:
                    //if ((returned_typecode = node.Expr.Emit(codeGenerator)) != PhpTypeCode.String)
                    //{
                    //    codeGenerator.EmitBoxing(returned_typecode);
                    //    //codeGenerator.EmitLoadClassContext();
                    //    il.Emit(OpCodes.Call, Methods.Convert.ObjectToString);
                    //    returned_typecode = PhpTypeCode.String;
                    //}
                    //break;
                    throw new NotImplementedException();

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

            switch (Access)
            {
                case AccessType.Read:
                    Debug.Assert(returned_type.SpecialType != SpecialType.System_Void);
                    // do nothing
                    break;
                case AccessType.None:
                    // pop operation's result value from stack
                    il.EmitPop(returned_type);
                    returned_type = il.CoreTypes.Void;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(Access);
            }

            return returned_type;
        }

        TypeSymbol EmitMinus(CodeGenerator gen)
        {
            var il = gen.Builder;
            var t = gen.Emit(this.Operand);

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
                    return gen.CoreTypes.Long;
                case SpecialType.System_Int64:
                    // PhpNumber.Minus(i8) : number
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Negation_long)
                            .Expect(gen.CoreTypes.PhpNumber);
                default:
                    if (t == gen.CoreTypes.PhpNumber)
                    {
                        return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Negation)
                            .Expect(t);
                    }

                    throw new NotImplementedException();
            }
        }
    }

    partial class BoundLiteral
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            Debug.Assert(this.Access == AccessType.Read || this.Access == AccessType.None);

            // do nothing
            if (this.Access == AccessType.None)
            {
                return il.CoreTypes.Void;
            }

            // push value onto the evaluation stack
            if (!ConstantValue.HasValue)
                throw new InvalidOperationException();

            // TOOD: use ConstantValue

            var value = ConstantValue.Value;
            if (value == null)
            {
                il.Builder.EmitNullConstant();
                return il.CoreTypes.Object;
            }
            else
            {
                if (value is int)
                {
                    il.Builder.EmitIntConstant((int)value);
                    return il.CoreTypes.Int32;
                }
                else if (value is long)
                {
                    il.Builder.EmitLongConstant((long)value);
                    return il.CoreTypes.Long;
                }
                else if (value is string)
                {
                    il.Builder.EmitStringConstant((string)value);
                    return il.CoreTypes.String;
                }
                else if (value is bool)
                {
                    il.Builder.EmitBoolConstant((bool)value);
                    return il.CoreTypes.Boolean;
                }
                else if (value is double)
                {
                    il.Builder.EmitDoubleConstant((double)value);
                    return il.CoreTypes.Double;
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
        /// Gets <see cref="IPlace"/> providing load and store operations.
        /// </summary>
        internal abstract IPlace GetPlace(CodeGenerator il);
    }

    partial class BoundVariableRef
    {
        internal override IPlace GetPlace(CodeGenerator il) => this.Variable.GetPlace(il.Builder);

        internal override TypeSymbol Emit(CodeGenerator il)
        {
            if (this.Variable == null)
                throw new InvalidOperationException(); // variable was not resolved

            if (Access == AccessType.None)
            {
                // do nothing
                return il.CoreTypes.Void;
            }

            //
            return EmitLoad(il);
        }

        internal TypeSymbol EmitLoad(CodeGenerator il)
        {
            return il.EmitLoad(this.Variable);
        }
    }

    partial class BoundFunctionCall
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            var method = this.TargetMethod;

            Debug.Assert(method != null);

            if (method == null)
                throw new InvalidOperationException();  // function call has to be analyzed first

            Debug.Assert(method.IsStatic);
            Debug.Assert(method.Arity == 0);

            // TODO: emit check the routine is declared; options:
            // 1. disable checks in release for better performance
            // 2. autoload script containing routine declaration
            // 3. throw if routine is not declared

            //
            foreach (var p in method.Parameters)
            {
                Debug.Assert(p.Type.SpecialType != SpecialType.System_Void);

                var arg = (BoundArgument)this.ArgumentMatchingParameter(p);
                if (arg != null)
                {
                    il.EmitConvert(arg.Value, p.Type);
                }
                else
                {
                    // special parameter
                    if (p.Type == il.CoreTypes.Context)
                    {
                        il.EmitLoadContext();
                    }
                    else
                    {
                        // load default value
                        il.EmitLoadDefaultValue(p.Type, 0);
                    }
                }
            }

            Debug.Assert(method.Parameters.Length == method.ParameterCount);

            //
            return il.EmitCall(ILOpCode.Call, method);
        }
    }

    partial class BoundEcho
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            Debug.Assert(Access == AccessType.None);

            foreach (var arg in _arguments)
            {
                il.EmitEcho(arg.Value);
            }

            return il.CoreTypes.Void;
        }
    }

    partial class BoundAssignEx
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            var target_place = this.Target.GetPlace(il);
            Debug.Assert(target_place != null);
            Debug.Assert(target_place.Type != null && target_place.Type.SpecialType != SpecialType.System_Void);

            // T tmp; // in case access is Read
            var t = target_place.Type;
            LocalDefinition tmp = null;

            // <target> = <value>
            target_place.EmitStorePrepare(il.Builder);
            il.EmitConvert(this.Value, t);

            if (this.Access != AccessType.None)
            {
                switch (this.Access)
                {
                    case AccessType.Read:
                        tmp = il.GetTemporaryLocal(t, false);
                        il.Builder.EmitOpCode(ILOpCode.Dup);
                        il.Builder.EmitLocalStore(tmp);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.Access);
                }
            }

            target_place.EmitStore(il.Builder);

            //
            switch (this.Access)
            {
                case AccessType.None:
                    t = il.CoreTypes.Void;
                    break;
                case AccessType.Read:
                    il.Builder.EmitLocalLoad(tmp);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(this.Access);
            }

            if (tmp != null)
            {
                il.ReturnTemporaryLocal(tmp);
            }

            //
            return t;
        }
    }

    partial class BoundCompoundAssignEx
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            throw new NotSupportedException();  // transform to BoundAssignEx with BoundBinaryEx as its Value
        }
    }

    partial class BoundIncDecEx
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            Debug.Assert(this.Access == AccessType.None || this.Access == AccessType.Read);

            if (this.UsesOperatorMethod)
            {
                throw new NotImplementedException();
            }

            var targetPlace = this.Target.GetPlace(il);
            var read = this.Access == AccessType.Read;

            // Postfix (i++, i--)
            if (this.IncrementKind == UnaryOperationKind.OperatorPostfixIncrement)
            {
                if (read)
                    throw new NotImplementedException();

                targetPlace.EmitStorePrepare(il.Builder);
                TypeSymbol result;
                if (targetPlace.Type == il.CoreTypes.Long)    // ++ won't overflow
                {
                    Debug.Assert(il.IsLongOnly(this.TypeRefMask));
                    il.EmitConvert(this.Target, il.CoreTypes.Long);
                    il.Builder.EmitLongConstant(1L);
                    il.Builder.EmitOpCode(ILOpCode.Add);
                    result = il.CoreTypes.Long;
                }
                else
                {
                    result = BoundBinaryEx.EmitAdd(il, this.Target, this.Value);
                }
                il.EmitConvert(result, this.TypeRefMask, targetPlace.Type);
                targetPlace.EmitStore(il.Builder);

                //
                return il.CoreTypes.Void;
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
                targetPlace.EmitStorePrepare(il.Builder);
                var result = BoundBinaryEx.EmitAdd(il, this.Target, this.Value);
                il.EmitConvert(result, this.TypeRefMask, targetPlace.Type);

                if (read)
                    il.Builder.EmitOpCode(ILOpCode.Dup);

                targetPlace.EmitStore(il.Builder);

                //
                if (read)
                    return targetPlace.Type;
                else
                    return il.CoreTypes.Void;
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
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            var result_type = il.DeclaringCompilation.GetTypeFromTypeRef(il.Routine, this.TypeRefMask);

            if (this.IfTrue != null)
            {
                object trueLbl = new object();
                object endLbl = new object();

                // Cond ? True : False
                il.EmitConvertToBool(this.Condition);   // i4
                il.Builder.EmitBranch(ILOpCode.Brtrue, trueLbl);

                // false:
                il.EmitConvert(this.IfFalse, result_type);
                il.Builder.EmitBranch(ILOpCode.Br, endLbl);
                il.Builder.AdjustStack(-1); // workarounds assert in ILBuilder.MarkLabel, we're doing something wrong with ILBuilder
                // trueLbl:
                il.Builder.MarkLabel(trueLbl);
                il.EmitConvert(this.IfTrue, result_type);

                // endLbl:
                il.Builder.MarkLabel(endLbl);
            }
            else
            {
                object trueLbl = new object();
                object endLbl = new object();

                // Cond ?: False

                // <stack> = <cond_var> = Cond
                var cond_type = il.Emit(this.Condition);
                var cond_var = il.GetTemporaryLocal(cond_type);
                il.Builder.EmitOpCode(ILOpCode.Dup);
                il.Builder.EmitLocalStore(cond_var);

                il.EmitConvertToBool(cond_type, this.Condition.TypeRefMask);
                il.Builder.EmitBranch(ILOpCode.Brtrue, trueLbl);

                // false:
                il.EmitConvert(this.IfFalse, result_type);
                il.Builder.EmitBranch(ILOpCode.Br, endLbl);
                il.Builder.AdjustStack(-1); // workarounds assert in ILBuilder.MarkLabel, we're doing something wrong with ILBuilder

                // trueLbl:
                il.Builder.MarkLabel(trueLbl);
                il.Builder.EmitLocalLoad(cond_var);
                il.EmitConvert(cond_type, this.Condition.TypeRefMask, result_type);
                
                // endLbl:
                il.Builder.MarkLabel(endLbl);

                //
                il.ReturnTemporaryLocal(cond_var);
            }

            //
            if (Access == AccessType.None)
            {
                il.EmitPop(result_type);
                result_type = il.CoreTypes.Void;
            }

            //
            return result_type;
        }
    }
}
