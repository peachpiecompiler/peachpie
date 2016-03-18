using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
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
            throw new NotImplementedException();
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
                    returned_type = EmitAdd(il);
                    break;

                case Operations.Sub:
                    //Template: "x - y"        Operators.Subtract(x,y) [overloads]

                    //lo_typecode = node.LeftExpr.Emit(codeGenerator);
                    //switch (lo_typecode)
                    //{
                    //    case PhpTypeCode.Integer:
                    //        codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                    //        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Int32_Object);
                    //        break;
                    //    case PhpTypeCode.Double:
                    //        switch (ro_typecode = node.RightExpr.Emit(codeGenerator))
                    //        {
                    //            case PhpTypeCode.Integer:
                    //                codeGenerator.IL.Emit(OpCodes.Conv_R8);
                    //                goto case PhpTypeCode.Double;   // fallback:
                    //            case PhpTypeCode.Double:
                    //                codeGenerator.IL.Emit(OpCodes.Sub);
                    //                returned_typecode = PhpTypeCode.Double;
                    //                break;
                    //            default:
                    //                codeGenerator.EmitBoxing(ro_typecode);
                    //                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Double_Object);
                    //                break;
                    //        }

                    //        break;
                    //    default:
                    //        codeGenerator.EmitBoxing(lo_typecode);
                    //        ro_typecode = node.RightExpr.Emit(codeGenerator);
                    //        if (ro_typecode == PhpTypeCode.Integer)
                    //        {
                    //            returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Object_Int);
                    //        }
                    //        else
                    //        {
                    //            codeGenerator.EmitBoxing(ro_typecode);
                    //            returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Object_Object);
                    //        }
                    //        break;
                    //}
                    //break;
                    throw new NotImplementedException();

                case Operations.Div:
                    //Template: "x / y"   Operators.Divide(x,y)

                    //lo_typecode = node.LeftExpr.Emit(codeGenerator);
                    //switch (lo_typecode)
                    //{
                    //    case PhpTypeCode.Integer:
                    //        codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                    //        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Int32_Object);
                    //        break;

                    //    case PhpTypeCode.Double:
                    //        switch (ro_typecode = node.RightExpr.Emit(codeGenerator))
                    //        {
                    //            case PhpTypeCode.Double:
                    //                codeGenerator.IL.Emit(OpCodes.Div);
                    //                returned_typecode = PhpTypeCode.Double;
                    //                break;
                    //            default:
                    //                codeGenerator.EmitBoxing(ro_typecode);
                    //                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Double_Object);
                    //                break;
                    //        }
                    //        break;

                    //    default:
                    //        codeGenerator.EmitBoxing(lo_typecode);
                    //        ro_typecode = node.RightExpr.Emit(codeGenerator);

                    //        switch (ro_typecode)
                    //        {
                    //            case PhpTypeCode.Integer:
                    //                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Int32);
                    //                break;

                    //            case PhpTypeCode.Double:
                    //                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Double);
                    //                break;

                    //            default:
                    //                codeGenerator.EmitBoxing(ro_typecode);
                    //                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Object);
                    //                break;
                    //        }
                    //        break;
                    //}
                    //break;
                    throw new NotImplementedException();

                case Operations.Mul:
                    //switch (lo_typecode = node.LeftExpr.Emit(codeGenerator))
                    //{
                    //    case PhpTypeCode.Double:
                    //        // "x * (double)y"
                    //        // Operators.Multiply((double)x,(object)y)

                    //        switch (ro_typecode = node.RightExpr.Emit(codeGenerator))
                    //        {
                    //            case PhpTypeCode.Integer:
                    //                codeGenerator.IL.Emit(OpCodes.Conv_R8);
                    //                goto case PhpTypeCode.Double;   // fallback:
                    //            case PhpTypeCode.Double:
                    //                codeGenerator.IL.Emit(OpCodes.Mul);
                    //                returned_typecode = PhpTypeCode.Double;
                    //                break;
                    //            default:
                    //                codeGenerator.EmitBoxing(ro_typecode);
                    //                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Double_Object);
                    //                break;
                    //        }

                    //        break;
                    //    default:
                    //        //Template: "x * y"  Operators.Multiply((object)x,y) [overloads]
                    //        codeGenerator.EmitBoxing(lo_typecode);

                    //        ro_typecode = node.RightExpr.Emit(codeGenerator);
                    //        switch (ro_typecode)
                    //        {
                    //            case PhpTypeCode.Integer:
                    //                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Int32);
                    //                break;

                    //            case PhpTypeCode.Double:
                    //                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Double);
                    //                break;

                    //            default:
                    //                codeGenerator.EmitBoxing(ro_typecode);
                    //                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Object);
                    //                break;
                    //        }
                    //        break;
                    //}
                    //break;
                    throw new NotImplementedException();

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

                    //// LOAD PhpComparer.Default.Compare > 0;
                    //EmitComparison(node, codeGenerator, false);
                    //codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    //codeGenerator.IL.Emit(OpCodes.Cgt);

                    //returned_typecode = PhpTypeCode.Boolean;
                    //break;
                    throw new NotImplementedException();

                case Operations.LessThan:

                    //// LOAD PhpComparer.Default.Compare < 0;
                    //EmitComparison(node, codeGenerator, false);
                    //codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    //codeGenerator.IL.Emit(OpCodes.Clt);

                    //returned_typecode = PhpTypeCode.Boolean;
                    //break;
                    throw new NotImplementedException();

                case Operations.GreaterThanOrEqual:

                    //// LOAD PhpComparer.Default.Compare >= 0 (not less than)
                    //EmitComparison(node, codeGenerator, false);
                    //codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    //codeGenerator.IL.Emit(OpCodes.Clt);
                    //codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    //codeGenerator.IL.Emit(OpCodes.Ceq);

                    //returned_typecode = PhpTypeCode.Boolean;
                    //break;
                    throw new NotImplementedException();

                case Operations.LessThanOrEqual:

                    //// LOAD PhpComparer.Default.Compare >= 0 (not greater than)
                    //EmitComparison(node, codeGenerator, false);
                    //codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    //codeGenerator.IL.Emit(OpCodes.Cgt);
                    //codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                    //codeGenerator.IL.Emit(OpCodes.Ceq);

                    //returned_typecode = PhpTypeCode.Boolean;
                    //break;
                    throw new NotImplementedException();

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
        TypeSymbol EmitAdd(CodeGenerator gen)
        {
            // Template: x + y : Operators.Add(x,y) [overloads]

            var il = gen.Builder;

            var xtype = gen.Emit(Left);
            var ytype = gen.Emit(Right);

            //
            if (xtype == gen.CoreTypes.PhpNumber)
            {
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
                else if (
                    ytype.SpecialType == SpecialType.System_Int64 ||
                    ytype.SpecialType == SpecialType.System_Int32 ||
                    ytype.SpecialType == SpecialType.System_Boolean)
                {
                    if (ytype.SpecialType != SpecialType.System_Int64)
                    {
                        il.EmitOpCode(ILOpCode.Conv_i8);    // bool|int -> long
                    }

                    // number + long : number
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_number_long)
                        .Expect(gen.CoreTypes.PhpNumber);
                }

                //
                throw new NotImplementedException();
            }
            else if (xtype.SpecialType == SpecialType.System_Double)
            {
                if (ytype.SpecialType == SpecialType.System_Boolean ||
                    ytype.SpecialType == SpecialType.System_Int32 ||
                    ytype.SpecialType == SpecialType.System_Int64 ||
                    ytype.SpecialType == SpecialType.System_Double)
                {
                    if (ytype.SpecialType != SpecialType.System_Double)
                    {
                        il.EmitOpCode(ILOpCode.Conv_r8);    // bool|int|long -> double
                    }

                    // r8 + r8 : r8
                    il.EmitOpCode(ILOpCode.Add);
                    return gen.CoreTypes.Double;
                }
                else if (ytype == gen.CoreTypes.PhpNumber)
                {
                    // r8 + number : r8
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_double_number)
                        .Expect(SpecialType.System_Double);
                }

                //
                throw new NotImplementedException();
            }
            else if (xtype.SpecialType == SpecialType.System_Int64)
            {
                if (ytype == gen.CoreTypes.PhpNumber)
                {
                    // i8 + number : number
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_long_number)
                        .Expect(gen.CoreTypes.PhpNumber);
                }
                else if (ytype.SpecialType == SpecialType.System_Double)
                {
                    // i8 + r8 : r8
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_long_double)
                        .Expect(SpecialType.System_Double);
                }
                else if (
                    ytype.SpecialType == SpecialType.System_Int64 ||
                    ytype.SpecialType == SpecialType.System_Int32 ||
                    ytype.SpecialType == SpecialType.System_Boolean)
                {
                    if (ytype.SpecialType != SpecialType.System_Int64)
                    {
                        il.EmitOpCode(ILOpCode.Conv_i8);    // int|bool -> long
                    }

                    // i8 + i8 : number
                    return gen.EmitCall(ILOpCode.Call, gen.CoreMethods.PhpNumber.Add_long_long)
                        .Expect(gen.CoreTypes.PhpNumber);
                }

                //
                throw new NotImplementedException();
            }

            //
            throw new NotImplementedException();
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
            var resultvar = gen.GetTemporaryLocal(boolean);   // block the tempoarary variable as little as possible
            il.EmitLocalStore(resultvar);

            // GOTO end;
            il.EmitBranch(ILOpCode.Br, end_label);

            // partial_eval:
            il.MarkLabel(partial_eval_label);
            il.EmitOpCode(isAnd ? ILOpCode.Ldc_i4_0 : ILOpCode.Ldc_i4_1, 1);
            il.EmitLocalStore(resultvar);

            // end:
            il.MarkLabel(end_label);

            // LOAD <RESULT>
            il.EmitLocalLoad(resultvar);
            gen.ReturnTemporaryLocal(resultvar);

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
                    //switch (o_typecode = node.Expr.Emit(codeGenerator))
                    //{
                    //    case PhpTypeCode.Double:
                    //        il.Emit(OpCodes.Neg);
                    //        returned_typecode = PhpTypeCode.Double;
                    //        break;
                    //    default:
                    //        codeGenerator.EmitBoxing(o_typecode);
                    //        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Minus);
                    //        break;
                    //}
                    //break;
                    throw new NotImplementedException();

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

                    // Always returns 1
                    il.Builder.EmitLongConstant(1);
                    returned_type = il.CoreTypes.Long;
                    break;

                case Operations.BoolCast:
                    //Template: "(bool)x"     Convert.ObjectToBoolean(x)
                    il.EmitConvertToBool(this.Operand);
                    returned_type = il.CoreTypes.Boolean;
                    break;

                case Operations.Int8Cast:
                case Operations.Int16Cast:
                case Operations.Int32Cast:
                case Operations.UInt8Cast:
                case Operations.UInt16Cast:
                    // CALL int Convert.ObjectToInteger(<node.Expr>)
                    //o_typecode = node.Expr.Emit(codeGenerator);
                    //if (o_typecode != PhpTypeCode.Integer)
                    //{
                    //    codeGenerator.EmitBoxing(o_typecode);
                    //    il.Emit(OpCodes.Call, Methods.Convert.ObjectToInteger);
                    //}

                    //// CONV for unsigned:
                    //switch (node.Operation)
                    //{
                    //    case Operations.UInt8Cast: il.Emit(OpCodes.Conv_U1); il.Emit(OpCodes.Conv_I4); break;
                    //    case Operations.UInt16Cast: il.Emit(OpCodes.Conv_U2); il.Emit(OpCodes.Conv_I4); break;
                    //}

                    //returned_typecode = PhpTypeCode.Integer;
                    //break;
                    throw new NotImplementedException();

                case Operations.UInt64Cast:
                case Operations.UInt32Cast:
                case Operations.Int64Cast:
                    // CALL long Convert.ObjectToLongInteger(<node.Expr>)
                    //o_typecode = node.Expr.Emit(codeGenerator);
                    //if (o_typecode != PhpTypeCode.LongInteger)
                    //{
                    //    codeGenerator.EmitBoxing(o_typecode);
                    //    il.Emit(OpCodes.Call, Methods.Convert.ObjectToLongInteger);
                    //}

                    //// CONV for unsigned:
                    //switch (node.Operation)
                    //{
                    //    case Operations.UInt32Cast: il.Emit(OpCodes.Conv_U4); il.Emit(OpCodes.Conv_I8); break;
                    //    case Operations.UInt64Cast: il.Emit(OpCodes.Conv_U8); il.Emit(OpCodes.Conv_I8); break;
                    //}

                    //returned_typecode = PhpTypeCode.LongInteger;
                    //break;
                    throw new NotImplementedException();

                case Operations.DecimalCast:
                case Operations.DoubleCast:
                case Operations.FloatCast:
                    // CALL double Convert.ObjectToDouble(<node.Expr>)
                    //o_typecode = node.Expr.Emit(codeGenerator);
                    //if (o_typecode != PhpTypeCode.Double)
                    //{
                    //    codeGenerator.EmitBoxing(o_typecode);
                    //    il.Emit(OpCodes.Call, Methods.Convert.ObjectToDouble);
                    //}
                    //returned_typecode = PhpTypeCode.Double;
                    //break;
                    throw new NotImplementedException();

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
            }

            return returned_type;
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

            if (Access == AccessType.Read)
            {
                return il.EmitLoad(this.Variable);
            }
            else if (Access == AccessType.None)
            {
                // do nothing
                return il.CoreTypes.Void;
            }

            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }

    partial class BoundIncDecEx
    {
        internal override TypeSymbol Emit(CodeGenerator il)
        {
            throw new NotImplementedException();
        }
    }
}
