using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Semantics
{
    public class PhpOperationVisitor // : OperationVisitor
    {
        /// <summary>Visits given operation.</summary>
        protected void Accept(IPhpOperation x) => x?.Accept(this);

        #region Expressions

        protected virtual void VisitRoutineCall(BoundRoutineCall x)
        {
            x.ArgumentsInSourceOrder.ForEach(VisitArgument);
        }

        public virtual void VisitLiteral(BoundLiteral x)
        {
            // VisitLiteralExpression(x);
        }

        public virtual void VisitArgument(BoundArgument x)
        {
            Accept(x.Value);
        }

        public virtual void VisitTypeRef(BoundTypeRef x)
        {
            if (x != null)
            {
                Accept(x.TypeExpression);
            }
        }

        public virtual void VisitMultipleTypeRef(BoundMultipleTypeRef x)
        {
            Debug.Assert(x != null);
            Debug.Assert(x.TypeExpression == null);
            Debug.Assert(x.BoundTypes.Length > 1);

            for (int i = 0; i < x.BoundTypes.Length; i++)
            {
                x.BoundTypes[i].Accept(this);
            }
        }

        public virtual void VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            VisitRoutineCall(x);
        }

        public virtual void VisitInstanceFunctionCall(BoundInstanceFunctionCall x)
        {
            Accept(x.Instance);
            VisitRoutineCall(x);
        }

        public virtual void VisitStaticFunctionCall(BoundStaticFunctionCall x)
        {
            VisitTypeRef(x.TypeRef);
            VisitRoutineCall(x);
        }

        public virtual void VisitEcho(BoundEcho x)
        {
            VisitRoutineCall(x);
        }

        public virtual void VisitConcat(BoundConcatEx x)
        {
            VisitRoutineCall(x);
        }

        public virtual void VisitNew(BoundNewEx x)
        {
            VisitTypeRef(x.TypeRef);
            VisitRoutineCall(x);
        }

        public virtual void VisitInclude(BoundIncludeEx x)
        {
            VisitRoutineCall(x);
        }

        public virtual void VisitExit(BoundExitEx x)
        {
            VisitRoutineCall(x);
        }

        public virtual void VisitAssert(BoundAssertEx x)
        {
            VisitRoutineCall(x);
        }

        public virtual void VisitBinaryExpression(BoundBinaryEx x)
        {
            Accept(x.Left);
            Accept(x.Right);
        }

        public virtual void VisitUnaryExpression(BoundUnaryEx x)
        {
            Accept(x.Operand);
        }

        public virtual void VisitIncDec(BoundIncDecEx x)
        {
            Accept(x.Target);
            Accept(x.Value);
        }

        public virtual void VisitConditional(BoundConditionalEx x)
        {
            Accept(x.Condition);
            Accept(x.IfTrue);
            Accept(x.IfFalse);
        }

        public virtual void VisitAssign(BoundAssignEx x)
        {
            Accept(x.Target);
            Accept(x.Value);
        }

        public virtual void VisitCompoundAssign(BoundCompoundAssignEx x)
        {
            Accept(x.Target);
            Accept(x.Value);
        }

        public virtual void VisitVariableRef(BoundVariableRef x)
        {

        }

        public virtual void VisitTemporalVariableRef(BoundTemporalVariableRef x)
        {
            // BoundSynthesizedVariableRef is based solely on BoundVariableRef so far 
            VisitVariableRef(x);
        }

        public virtual void VisitList(BoundListEx x)
        {
            x.Items.ForEach(pair =>
            {
                Accept(pair.Key);
                Accept(pair.Value);
            });
        }

        public virtual void VisitFieldRef(BoundFieldRef x)
        {
            VisitTypeRef(x.ContainingType); 
            Accept(x.Instance);
            Accept(x.FieldName.NameExpression);
        }

        public virtual void VisitArray(BoundArrayEx x)
        {
            x.Items.ForEach(pair =>
            {
                Accept(pair.Key);
                Accept(pair.Value);
            });
        }

        public virtual void VisitArrayItem(BoundArrayItemEx x)
        {
            Accept(x.Array);
            Accept(x.Index);
        }

        public virtual void VisitInstanceOf(BoundInstanceOfEx x)
        {
            Accept(x.Operand);
            VisitTypeRef(x.AsType);
        }

        public virtual void VisitGlobalConstUse(BoundGlobalConst x)
        {

        }

        public virtual void VisitGlobalConstDecl(BoundGlobalConstDeclStatement x)
        {
            Accept(x.Value);
        }

        public virtual void VisitPseudoConstUse(BoundPseudoConst x)
        {

        }

        public virtual void VisitPseudoClassConstUse(BoundPseudoClassConst x)
        {
            VisitTypeRef(x.TargetType);
        }

        public virtual void VisitIsEmpty(BoundIsEmptyEx x)
        {
            Accept(x.Operand);
        }

        public virtual void VisitIsSet(BoundIsSetEx x)
        {
            Accept(x.VarReference);
        }

        public virtual void VisitLambda(BoundLambda x)
        {

        }

        public virtual void VisitEval(BoundEvalEx x)
        {
            Accept(x.CodeExpression);
        }


        public virtual void VisitYieldEx(BoundYieldEx boundYieldEx)
        {

        }

        public virtual void VisitYieldFromEx(BoundYieldFromEx x)
        {
            Accept(x.Operand);
        }

        #endregion

        #region Statements

        public virtual void VisitUnset(BoundUnset x)
        {
            Accept(x.Variable);
        }

        public virtual void VisitEmptyStatement(BoundEmptyStatement x)
        {
            
        }

        public virtual void VisitBlockStatement(Graph.BoundBlock x)
        {
            for (int i = 0; i < x.Statements.Count; i++)
            {
                Accept(x.Statements[i]);
            }
        }

        public virtual void VisitExpressionStatement(BoundExpressionStatement x)
        {
            Accept(x.Expression);
        }

        public virtual void VisitReturn(BoundReturnStatement x)
        {
            Accept(x.Returned);
        }

        public virtual void VisitThrow(BoundThrowStatement x)
        {
            Accept(x.Thrown);
        }

        public virtual void VisitFunctionDeclaration(BoundFunctionDeclStatement x)
        {
            
        }

        public virtual void VisitTypeDeclaration(BoundTypeDeclStatement x)
        {
            
        }

        public virtual void VisitGlobalStatement(BoundGlobalVariableStatement x)
        {
            Accept(x.Variable);
        }

        public virtual void VisitStaticStatement(BoundStaticVariableStatement x)
        {
            
        }

        public virtual void VisitYieldStatement(BoundYieldStatement boundYieldStatement)
        {
            Accept(boundYieldStatement.YieldedValue);
            Accept(boundYieldStatement.YieldedKey);
        }

        public virtual void VisitDeclareStatement(BoundDeclareStatement x)
        {
        }

        #endregion
    }

    public class PhpOperationVisitor<TResult>
    {
        /// <summary>Visits given operation.</summary>
        protected TResult Accept(IPhpOperation x) => (x != null) ? x.Accept(this) : default;

        #region Expressions

        protected virtual TResult DefaultVisitOperation(BoundOperation x) => default;

        protected virtual TResult VisitRoutineCall(BoundRoutineCall x) => DefaultVisitOperation(x);

        public virtual TResult VisitLiteral(BoundLiteral x) => DefaultVisitOperation(x);

        public virtual TResult VisitArgument(BoundArgument x) => DefaultVisitOperation(x);

        public virtual TResult VisitTypeRef(BoundTypeRef x) => default;

        public virtual TResult VisitMultipleTypeRef(BoundMultipleTypeRef x) => default;

        public virtual TResult VisitGlobalFunctionCall(BoundGlobalFunctionCall x) => VisitRoutineCall(x);

        public virtual TResult VisitInstanceFunctionCall(BoundInstanceFunctionCall x) => VisitRoutineCall(x);

        public virtual TResult VisitStaticFunctionCall(BoundStaticFunctionCall x) => VisitRoutineCall(x);

        public virtual TResult VisitEcho(BoundEcho x) => VisitRoutineCall(x);

        public virtual TResult VisitConcat(BoundConcatEx x) => VisitRoutineCall(x);

        public virtual TResult VisitNew(BoundNewEx x) => VisitRoutineCall(x);

        public virtual TResult VisitInclude(BoundIncludeEx x) => VisitRoutineCall(x);

        public virtual TResult VisitExit(BoundExitEx x) => VisitRoutineCall(x);

        public virtual TResult VisitAssert(BoundAssertEx x) => VisitRoutineCall(x);

        public virtual TResult VisitBinaryExpression(BoundBinaryEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitUnaryExpression(BoundUnaryEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitIncDec(BoundIncDecEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitConditional(BoundConditionalEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitAssign(BoundAssignEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitCompoundAssign(BoundCompoundAssignEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitVariableRef(BoundVariableRef x) => DefaultVisitOperation(x);

        public virtual TResult VisitTemporalVariableRef(BoundTemporalVariableRef x) => DefaultVisitOperation(x);

        public virtual TResult VisitList(BoundListEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitFieldRef(BoundFieldRef x) => DefaultVisitOperation(x);

        public virtual TResult VisitArray(BoundArrayEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitArrayItem(BoundArrayItemEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitInstanceOf(BoundInstanceOfEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitGlobalConstUse(BoundGlobalConst x) => DefaultVisitOperation(x);

        public virtual TResult VisitGlobalConstDecl(BoundGlobalConstDeclStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitPseudoConstUse(BoundPseudoConst x) => DefaultVisitOperation(x);

        public virtual TResult VisitPseudoClassConstUse(BoundPseudoClassConst x) => DefaultVisitOperation(x);

        public virtual TResult VisitIsEmpty(BoundIsEmptyEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitIsSet(BoundIsSetEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitLambda(BoundLambda x) => DefaultVisitOperation(x);

        public virtual TResult VisitEval(BoundEvalEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitYieldEx(BoundYieldEx x) => DefaultVisitOperation(x);

        public virtual TResult VisitYieldFromEx(BoundYieldFromEx x) => DefaultVisitOperation(x);

        #endregion

        #region Statements

        public virtual TResult VisitUnset(BoundUnset x) => DefaultVisitOperation(x);

        public virtual TResult VisitEmptyStatement(BoundEmptyStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitBlockStatement(Graph.BoundBlock x) => DefaultVisitOperation(x);

        public virtual TResult VisitExpressionStatement(BoundExpressionStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitReturn(BoundReturnStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitThrow(BoundThrowStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitFunctionDeclaration(BoundFunctionDeclStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitTypeDeclaration(BoundTypeDeclStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitGlobalStatement(BoundGlobalVariableStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitStaticStatement(BoundStaticVariableStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitYieldStatement(BoundYieldStatement x) => DefaultVisitOperation(x);

        public virtual TResult VisitDeclareStatement(BoundDeclareStatement x) => DefaultVisitOperation(x);

        #endregion
    }
}
