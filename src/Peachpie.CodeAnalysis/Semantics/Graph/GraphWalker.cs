using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.Semantics.TypeRef;
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
    public abstract class GraphWalker<T> : GraphVisitor<T>
    {
        #region ControlFlowGraph

        public override T VisitCFG(ControlFlowGraph x)
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
        protected override T DefaultVisitBlock(BoundBlock x)
        {
            VisitCFGBlockStatements(x);

            AcceptEdge(x, x.NextEdge);

            return default;
        }

        protected virtual T AcceptEdge(BoundBlock fromBlock, Edge edge)
        {
            return edge != null ? edge.Accept(this) : default;
        }

        public override T VisitCFGBlock(BoundBlock x)
        {
            DefaultVisitBlock(x);

            return default;
        }

        public override T VisitCFGExitBlock(ExitBlock x)
        {
            VisitCFGBlock(x);

            return default;
        }

        public override T VisitCFGCatchBlock(CatchBlock x)
        {
            Accept(x.TypeRef);
            Accept(x.Variable);

            DefaultVisitBlock(x);

            return default;
        }

        public override T VisitCFGCaseBlock(CaseBlock x)
        {
            if (!x.CaseValue.IsOnlyBoundElement) { VisitCFGBlock(x.CaseValue.PreBoundBlockFirst); }
            if (!x.CaseValue.IsEmpty) { Accept(x.CaseValue.BoundElement); }

            DefaultVisitBlock(x);

            return default;
        }

        #endregion

        #region Graph.Edge

        public override T VisitCFGSimpleEdge(SimpleEdge x)
        {
            Debug.Assert(x.NextBlock != null);
            x.NextBlock.Accept(this);

            DefaultVisitEdge(x);

            return default;
        }

        public override T VisitCFGConditionalEdge(ConditionalEdge x)
        {
            Accept(x.Condition);

            x.TrueTarget.Accept(this);
            x.FalseTarget.Accept(this);

            return default;
        }

        public override T VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            x.BodyBlock.Accept(this);

            foreach (var c in x.CatchBlocks)
                c.Accept(this);

            if (x.FinallyBlock != null)
                x.FinallyBlock.Accept(this);

            return default;
        }

        public override T VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x)
        {
            Accept(x.Enumeree);

            x.NextBlock.Accept(this);

            return default;
        }

        public override T VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            Accept(x.ValueVariable);
            Accept(x.KeyVariable);

            x.BodyBlock.Accept(this);
            x.NextBlock.Accept(this);

            return default;
        }

        public override T VisitCFGSwitchEdge(SwitchEdge x)
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

        protected override T VisitRoutineCall(BoundRoutineCall x)
        {
            if (x.TypeArguments.IsDefaultOrEmpty == false)
            {
                for (int i = 0; i < x.TypeArguments.Length; i++)
                {
                    x.TypeArguments[i].Accept(this);
                }
            }

            var args = x.ArgumentsInSourceOrder;
            for (int i = 0; i < args.Length; i++)
            {
                VisitArgument(args[i]);
            }

            return default;
        }

        public override T VisitLiteral(BoundLiteral x)
        {
            //VisitLiteralExpression(x);

            return default;
        }

        public override T VisitCopyValue(BoundCopyValue x)
        {
            Accept(x.Expression);

            return default;
        }

        public override T VisitArgument(BoundArgument x)
        {
            Accept(x.Value);

            return default;
        }

        internal override T VisitTypeRef(BoundTypeRef x)
        {
            return base.VisitTypeRef(x);
        }

        internal override T VisitIndirectTypeRef(BoundIndirectTypeRef x)
        {
            Accept(x.TypeExpression);
            return base.VisitIndirectTypeRef(x);
        }

        internal override T VisitMultipleTypeRef(BoundMultipleTypeRef x)
        {
            Debug.Assert(x != null);
            Debug.Assert(x.TypeRefs.Length > 1);

            for (int i = 0; i < x.TypeRefs.Length; i++)
            {
                x.TypeRefs[i].Accept(this);
            }

            return default;
        }

        public override T VisitRoutineName(BoundRoutineName x)
        {
            Accept(x.NameExpression);

            return default;
        }

        public override T VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            Accept(x.Name);
            VisitRoutineCall(x);

            return default;
        }

        public override T VisitInstanceFunctionCall(BoundInstanceFunctionCall x)
        {
            Accept(x.Instance);
            Accept(x.Name);
            VisitRoutineCall(x);

            return default;
        }

        public override T VisitStaticFunctionCall(BoundStaticFunctionCall x)
        {
            Accept(x.TypeRef);
            Accept(x.Name);
            VisitRoutineCall(x);

            return default;
        }

        public override T VisitEcho(BoundEcho x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override T VisitConcat(BoundConcatEx x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override T VisitNew(BoundNewEx x)
        {
            Accept(x.TypeRef);
            VisitRoutineCall(x);

            return default;
        }

        public override T VisitInclude(BoundIncludeEx x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override T VisitExit(BoundExitEx x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override T VisitAssert(BoundAssertEx x)
        {
            VisitRoutineCall(x);

            return default;
        }

        public override T VisitBinaryExpression(BoundBinaryEx x)
        {
            Accept(x.Left);
            Accept(x.Right);

            return default;
        }

        public override T VisitUnaryExpression(BoundUnaryEx x)
        {
            Accept(x.Operand);

            return default;
        }

        public override T VisitConversion(BoundConversionEx x)
        {
            Accept(x.Operand);
            Accept(x.TargetType);

            return default;
        }

        public override T VisitIncDec(BoundIncDecEx x)
        {
            Accept(x.Target);
            Accept(x.Value);

            return default;
        }

        public override T VisitConditional(BoundConditionalEx x)
        {
            Accept(x.Condition);
            Accept(x.IfTrue);
            Accept(x.IfFalse);

            return default;
        }

        public override T VisitAssign(BoundAssignEx x)
        {
            Accept(x.Target);
            Accept(x.Value);

            return default;
        }

        public override T VisitCompoundAssign(BoundCompoundAssignEx x)
        {
            Accept(x.Target);
            Accept(x.Value);

            return default;
        }

        public override T VisitVariableName(BoundVariableName x)
        {
            Accept(x.NameExpression);

            return default;
        }

        public override T VisitVariableRef(BoundVariableRef x)
        {
            Accept(x.Name);

            return default;
        }

        public override T VisitTemporalVariableRef(BoundTemporalVariableRef x)
        {
            // BoundSynthesizedVariableRef is based solely on BoundVariableRef so far 
            VisitVariableRef(x);

            return default;
        }

        public override T VisitList(BoundListEx x)
        {
            x.Items.ForEach(pair =>
            {
                Accept(pair.Key);
                Accept(pair.Value);
            });

            return default;
        }

        public override T VisitFieldRef(BoundFieldRef x)
        {
            Accept(x.ContainingType);
            Accept(x.Instance);
            Accept(x.FieldName);

            return default;
        }

        public override T VisitArray(BoundArrayEx x)
        {
            x.Items.ForEach(pair =>
            {
                Accept(pair.Key);
                Accept(pair.Value);
            });

            return default;
        }

        public override T VisitArrayItem(BoundArrayItemEx x)
        {
            Accept(x.Array);
            Accept(x.Index);

            return default;
        }

        public override T VisitArrayItemOrd(BoundArrayItemOrdEx x)
        {
            Accept(x.Array);
            Accept(x.Index);

            return default;
        }

        public override T VisitInstanceOf(BoundInstanceOfEx x)
        {
            Accept(x.Operand);
            Accept(x.AsType);

            return default;
        }

        public override T VisitGlobalConstUse(BoundGlobalConst x)
        {
            return default;
        }

        public override T VisitGlobalConstDecl(BoundGlobalConstDeclStatement x)
        {
            Accept(x.Value);

            return default;
        }

        public override T VisitPseudoConstUse(BoundPseudoConst x)
        {
            return default;
        }

        public override T VisitPseudoClassConstUse(BoundPseudoClassConst x)
        {
            Accept(x.TargetType);

            return default;
        }

        public override T VisitIsEmpty(BoundIsEmptyEx x)
        {
            Accept(x.Operand);

            return default;
        }

        public override T VisitIsSet(BoundIsSetEx x)
        {
            Accept(x.VarReference);

            return default;
        }

        public override T VisitOffsetExists(BoundOffsetExists x)
        {
            Accept(x.Receiver);
            Accept(x.Index);

            return default;
        }

        public override T VisitTryGetItem(BoundTryGetItem x)
        {
            Accept(x.Array);
            Accept(x.Index);
            Accept(x.Fallback);

            return default;
        }

        public override T VisitLambda(BoundLambda x)
        {
            return default;
        }

        public override T VisitEval(BoundEvalEx x)
        {
            Accept(x.CodeExpression);

            return default;
        }


        public override T VisitYieldEx(BoundYieldEx boundYieldEx)
        {
            return default;
        }

        public override T VisitYieldFromEx(BoundYieldFromEx x)
        {
            Accept(x.Operand);

            return default;
        }

        #endregion

        #region Statements

        public override T VisitUnset(BoundUnset x)
        {
            Accept(x.Variable);

            return default;
        }

        public override T VisitEmptyStatement(BoundEmptyStatement x)
        {
            return default;
        }

        public override T VisitBlockStatement(Graph.BoundBlock x)
        {
            Debug.Assert(x.NextEdge == null);

            for (int i = 0; i < x.Statements.Count; i++)
            {
                Accept(x.Statements[i]);
            }

            return default;
        }

        public override T VisitExpressionStatement(BoundExpressionStatement x)
        {
            Accept(x.Expression);

            return default;
        }

        public override T VisitReturn(BoundReturnStatement x)
        {
            Accept(x.Returned);

            return default;
        }

        public override T VisitThrow(BoundThrowStatement x)
        {
            Accept(x.Thrown);

            return default;
        }

        public override T VisitFunctionDeclaration(BoundFunctionDeclStatement x)
        {
            return default;
        }

        public override T VisitTypeDeclaration(BoundTypeDeclStatement x)
        {
            return default;
        }

        public override T VisitGlobalStatement(BoundGlobalVariableStatement x)
        {
            Accept(x.Variable);

            return default;
        }

        public override T VisitStaticStatement(BoundStaticVariableStatement x)
        {
            return default;
        }

        public override T VisitYieldStatement(BoundYieldStatement boundYieldStatement)
        {
            Accept(boundYieldStatement.YieldedValue);
            Accept(boundYieldStatement.YieldedKey);

            return default;
        }

        public override T VisitDeclareStatement(BoundDeclareStatement x)
        {
            return default;
        }

        #endregion
    }
}
