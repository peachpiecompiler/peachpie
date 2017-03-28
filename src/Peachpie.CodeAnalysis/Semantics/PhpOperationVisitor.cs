using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    public class PhpOperationVisitor // : OperationVisitor
    {
        /// <summary>Visits given operation.</summary>
        protected virtual void Accept(IPhpOperation x) => x?.Accept(this);

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
            Accept(x.TypeExpression);
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

        public virtual void VisitList(BoundListEx x)
        {
            x.Variables.ForEach(Accept);
        }

        public virtual void VisitFieldRef(BoundFieldRef x)
        {
            VisitTypeRef(x.ParentType);
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
            x.VarReferences.ForEach(Accept);
        }

        public virtual void VisitLambda(BoundLambda x)
        {

        }

        public virtual void VisitEval(BoundEvalEx x)
        {
            Accept(x.CodeExpression);
        }

        #endregion

        #region Statements

        public virtual void VisitUnset(BoundUnset x)
        {
            x.VarReferences.ForEach(Accept);
        }

        public virtual void VisitEmptyStatement(BoundEmptyStatement x)
        {
            
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
            
        }

        public virtual void VisitStaticStatement(BoundStaticVariableStatement x)
        {
            
        }

        #endregion
    }
}
