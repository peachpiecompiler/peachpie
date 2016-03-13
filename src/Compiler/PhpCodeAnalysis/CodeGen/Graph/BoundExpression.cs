using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Symbols;
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

            switch (this.BinaryOperationKind)
            {
                #region Boolean and Bitwise Operations

                case BinaryKind.OperatorConditionalAnd:
                    returned_type = EmitBinaryBooleanOperation(il, true);
                    break;

                case BinaryKind.OperatorConditionalOr:
                    returned_type = EmitBinaryBooleanOperation(il, false);
                    break;

                case (BinaryKind)BinaryPhpOperationKind.OperatorConditionalXor:
                    returned_type = EmitBinaryXor(il);
                    break;

                case BinaryKind.OperatorAnd:
                    //returned_typecode = EmitBitOperation(node, codeGenerator, Operators.BitOp.And);
                    //break;

                case BinaryKind.OperatorOr:
                    //returned_typecode = EmitBitOperation(node, codeGenerator, Operators.BitOp.Or);
                    //break;

                case BinaryKind.OperatorExclusiveOr:
                    //returned_typecode = EmitBitOperation(node, codeGenerator, Operators.BitOp.Xor);
                    //break;

                #endregion

                default:
                    throw new NotImplementedException();
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
        /// Emits binary boolean operation (AND or OR).
        /// </summary>
        /// <param name="codeGenerator">A code generator.</param>
        /// <param name="isAnd">Whether to emit AND, otherwise OR.</param>
        /// <returns>A type code of the result.</returns>
        private TypeSymbol EmitBinaryBooleanOperation(CodeGenerator codeGenerator, bool isAnd)
        {
            var boolean = codeGenerator.CoreTypes.Boolean;  // typeof(bool)

            var il = codeGenerator.Builder;
            var partial_eval_label = new object();
            var end_label = new object();
           
            // IF [!]<(bool) Left> THEN GOTO partial_eval;
            codeGenerator.EmitConvertToBool(Left);
            il.EmitBranch(isAnd ? ILOpCode.Brfalse : ILOpCode.Brtrue, partial_eval_label);

            // <RESULT> = <(bool) Right>;
            codeGenerator.EmitConvertToBool(Right);
            var resultvar = codeGenerator.GetTemporaryLocal(boolean);   // block the tempoarary variable as little as possible
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
            codeGenerator.ReturnTemporaryLocal(resultvar);

            //
            return boolean;
        }

        /// <summary>
        /// Emits binary operation XOR.
        /// </summary>
        private TypeSymbol EmitBinaryXor(CodeGenerator codeGenerator)
        {
            // LOAD <(bool) leftSon> == <(bool) rightSon>;
            codeGenerator.EmitConvertToBool(Left);
            codeGenerator.EmitConvertToBool(Right);
            codeGenerator.EmitOpCode(ILOpCode.Ceq);

            codeGenerator.EmitOpCode(ILOpCode.Ldc_i4_0);
            codeGenerator.EmitOpCode(ILOpCode.Ceq);

            return codeGenerator.CoreTypes.Boolean;
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
                    il.Builder.EmitLongConstant((int)value);
                    return il.CoreTypes.Long;
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
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }

    partial class BoundVariableRef
    {
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
}
