using Microsoft.CodeAnalysis.Operations;
using Peachpie.CodeAnalysis.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Visitor used to traverse CFG and all its operations.
    /// </summary>
    /// <remarks>Visitor does not implement infinite recursion prevention.</remarks>
    public abstract class GraphWalker : GraphVisitor<EmptyStruct>
    {
        #region Properties

        protected bool IsEdgeVisitingStopped { get; set; } = false;

        #endregion

        #region ControlFlowGraph

        public override EmptyStruct VisitCFG(ControlFlowGraph x)
        {
            x.Start.Accept(this);

            return default;
        }

        #endregion

        #region Graph.Block

        void VisitCFGBlockStatements(BoundBlock x)
        {
            for (int i = 0; i < x.Statements.Count; i++)
            {
                Accept(x.Statements[i]);
            }
        }

        /// <summary>
        /// Visits block statements and its edge to next block.
        /// </summary>
        protected override EmptyStruct DefaultVisitBlock(BoundBlock x)
        {
            VisitCFGBlockStatements(x);

            if (x.NextEdge != null && !IsEdgeVisitingStopped)
                x.NextEdge.Accept(this);

            return default;
        }

        public override EmptyStruct VisitCFGBlock(BoundBlock x)
        {
            DefaultVisitBlock(x);

            return default;
        }

        public override EmptyStruct VisitCFGExitBlock(ExitBlock x)
        {
            VisitCFGBlock(x);

            return default;
        }

        public override EmptyStruct VisitCFGCatchBlock(CatchBlock x)
        {
            VisitTypeRef(x.TypeRef);
            Accept(x.Variable);

            DefaultVisitBlock(x);

            return default;
        }

        public override EmptyStruct VisitCFGCaseBlock(CaseBlock x)
        {
            if (!x.CaseValue.IsOnlyBoundElement) { VisitCFGBlock(x.CaseValue.PreBoundBlockFirst); }
            if (!x.CaseValue.IsEmpty) { Accept(x.CaseValue.BoundElement); }

            DefaultVisitBlock(x);

            return default;
        }

        #endregion

        #region Graph.Edge

        public override EmptyStruct VisitCFGSimpleEdge(SimpleEdge x)
        {
            Debug.Assert(x.NextBlock != null);
            x.NextBlock.Accept(this);

            DefaultVisitEdge(x);

            return default;
        }

        public override EmptyStruct VisitCFGConditionalEdge(ConditionalEdge x)
        {
            Accept(x.Condition);

            x.TrueTarget.Accept(this);
            x.FalseTarget.Accept(this);

            return default;
        }

        public override EmptyStruct VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            x.BodyBlock.Accept(this);

            foreach (var c in x.CatchBlocks)
                c.Accept(this);

            if (x.FinallyBlock != null)
                x.FinallyBlock.Accept(this);

            return default;
        }

        public override EmptyStruct VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x)
        {
            Accept(x.Enumeree);
            x.NextBlock.Accept(this);

            return default;
        }

        public override EmptyStruct VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            Accept(x.ValueVariable);
            Accept(x.KeyVariable);

            x.BodyBlock.Accept(this);
            x.NextBlock.Accept(this);

            return default;
        }

        public override EmptyStruct VisitCFGSwitchEdge(SwitchEdge x)
        {
            Accept(x.SwitchValue);

            //
            var arr = x.CaseBlocks;
            for (int i = 0; i < arr.Length; i++)
                arr[i].Accept(this);

            return default;
        }

        #endregion


        #region Expressions

        protected override EmptyStruct VisitRoutineCall(BoundRoutineCall x)
        {
            for (int i = 0; i < x.ArgumentsInSourceOrder.Length; i++)
            {
                VisitArgument(x.ArgumentsInSourceOrder[i]);
            }

            return default;
        }

        public override EmptyStruct VisitLiteral(BoundLiteral x)
        {
            //VisitLiteralExpression(x);

            return default;
        }

        public override EmptyStruct VisitArgument(BoundArgument x)
        {
            Accept(x.Value);

            return default;
        }

        public override EmptyStruct VisitTypeRef(BoundTypeRef x)
        {
            if (x != null)
            {
                Accept(x.TypeExpression);
            }

            return default;
        }

        public override EmptyStruct VisitMultipleTypeRef(BoundMultipleTypeRef x)
        {
            Debug.Assert(x != null);
            Debug.Assert(x.TypeExpression == null);
            Debug.Assert(x.BoundTypes.Length > 1);

            for (int i = 0; i < x.BoundTypes.Length; i++)
            {
                x.BoundTypes[i].Accept(this);
            }

            return default;
        }

        public override EmptyStruct VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            VisitRoutineCall(x);
            return default;
        }

        public override EmptyStruct VisitInstanceFunctionCall(BoundInstanceFunctionCall x)
        {
            Accept(x.Instance);
            VisitRoutineCall(x);

            return default;
        }

        public override EmptyStruct VisitStaticFunctionCall(BoundStaticFunctionCall x)
        {
            VisitTypeRef(x.TypeRef);
            VisitRoutineCall(x);

            return default;
        }

        public override EmptyStruct VisitEcho(BoundEcho x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override EmptyStruct VisitConcat(BoundConcatEx x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override EmptyStruct VisitNew(BoundNewEx x)
        {
            VisitTypeRef(x.TypeRef);
            VisitRoutineCall(x);

            return default;
        }

        public override EmptyStruct VisitInclude(BoundIncludeEx x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override EmptyStruct VisitExit(BoundExitEx x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override EmptyStruct VisitAssert(BoundAssertEx x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override EmptyStruct VisitBinaryExpression(BoundBinaryEx x)
        {
            Accept(x.Left);
            Accept(x.Right);

            return default;
        }

        public override EmptyStruct VisitUnaryExpression(BoundUnaryEx x)
        {
            Accept(x.Operand);

            return default;
        }

        public override EmptyStruct VisitIncDec(BoundIncDecEx x)
        {
            Accept(x.Target);
            Accept(x.Value);

            return default;
        }

        public override EmptyStruct VisitConditional(BoundConditionalEx x)
        {
            Accept(x.Condition);
            Accept(x.IfTrue);
            Accept(x.IfFalse);

            return default;
        }

        public override EmptyStruct VisitAssign(BoundAssignEx x)
        {
            Accept(x.Target);
            Accept(x.Value);

            return default;
        }

        public override EmptyStruct VisitCompoundAssign(BoundCompoundAssignEx x)
        {
            Accept(x.Target);
            Accept(x.Value);

            return default;
        }

        public override EmptyStruct VisitVariableRef(BoundVariableRef x)
        {
            return default;
        }

        public override EmptyStruct VisitTemporalVariableRef(BoundTemporalVariableRef x)
        {
            // BoundSynthesizedVariableRef is based solely on BoundVariableRef so far 
            VisitVariableRef(x);

            return default;
        }

        public override EmptyStruct VisitList(BoundListEx x)
        {
            x.Items.ForEach(pair =>
            {
                Accept(pair.Key);
                Accept(pair.Value);
            });

            return default;
        }

        public override EmptyStruct VisitFieldRef(BoundFieldRef x)
        {
            VisitTypeRef(x.ContainingType);
            Accept(x.Instance);
            Accept(x.FieldName.NameExpression);

            return default;
        }

        public override EmptyStruct VisitArray(BoundArrayEx x)
        {
            x.Items.ForEach(pair =>
            {
                Accept(pair.Key);
                Accept(pair.Value);
            });

            return default;
        }

        public override EmptyStruct VisitArrayItem(BoundArrayItemEx x)
        {
            Accept(x.Array);
            Accept(x.Index);

            return default;
        }

        public override EmptyStruct VisitInstanceOf(BoundInstanceOfEx x)
        {
            Accept(x.Operand);
            VisitTypeRef(x.AsType);

            return default;
        }

        public override EmptyStruct VisitGlobalConstUse(BoundGlobalConst x)
        {
            return default;
        }

        public override EmptyStruct VisitGlobalConstDecl(BoundGlobalConstDeclStatement x)
        {
            Accept(x.Value);

            return default;
        }

        public override EmptyStruct VisitPseudoConstUse(BoundPseudoConst x)
        {
            return default;
        }

        public override EmptyStruct VisitPseudoClassConstUse(BoundPseudoClassConst x)
        {
            VisitTypeRef(x.TargetType);

            return default;
        }

        public override EmptyStruct VisitIsEmpty(BoundIsEmptyEx x)
        {
            Accept(x.Operand);

            return default;
        }

        public override EmptyStruct VisitIsSet(BoundIsSetEx x)
        {
            Accept(x.VarReference);

            return default;
        }

        public override EmptyStruct VisitLambda(BoundLambda x)
        {
            return default;
        }

        public override EmptyStruct VisitEval(BoundEvalEx x)
        {
            Accept(x.CodeExpression);

            return default;
        }


        public override EmptyStruct VisitYieldEx(BoundYieldEx boundYieldEx)
        {
            return default;
        }

        public override EmptyStruct VisitYieldFromEx(BoundYieldFromEx x)
        {
            Accept(x.Operand);

            return default;
        }

        #endregion

        #region Statements

        public override EmptyStruct VisitUnset(BoundUnset x)
        {
            Accept(x.Variable);

            return default;
        }

        public override EmptyStruct VisitEmptyStatement(BoundEmptyStatement x)
        {
            return default;
        }

        public override EmptyStruct VisitBlockStatement(Graph.BoundBlock x)
        {
            for (int i = 0; i < x.Statements.Count; i++)
            {
                Accept(x.Statements[i]);
            }

            return default;
        }

        public override EmptyStruct VisitExpressionStatement(BoundExpressionStatement x)
        {
            Accept(x.Expression);

            return default;
        }

        public override EmptyStruct VisitReturn(BoundReturnStatement x)
        {
            Accept(x.Returned);

            return default;
        }

        public override EmptyStruct VisitThrow(BoundThrowStatement x)
        {
            Accept(x.Thrown);

            return default;
        }

        public override EmptyStruct VisitFunctionDeclaration(BoundFunctionDeclStatement x)
        {
            return default;
        }

        public override EmptyStruct VisitTypeDeclaration(BoundTypeDeclStatement x)
        {
            return default;
        }

        public override EmptyStruct VisitGlobalStatement(BoundGlobalVariableStatement x)
        {
            Accept(x.Variable);

            return default;
        }

        public override EmptyStruct VisitStaticStatement(BoundStaticVariableStatement x)
        {
            return default;
        }

        public override EmptyStruct VisitYieldStatement(BoundYieldStatement boundYieldStatement)
        {
            Accept(boundYieldStatement.YieldedValue);
            Accept(boundYieldStatement.YieldedKey);

            return default;
        }

        public override EmptyStruct VisitDeclareStatement(BoundDeclareStatement x)
        {
            return default;
        }

        #endregion
    }
}
