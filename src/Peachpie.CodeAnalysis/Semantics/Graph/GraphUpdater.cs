using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Pchp.CodeAnalysis.Semantics.TypeRef;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Helper class for <see cref="ControlFlowGraph"/> update. Calls Update on each block, edge, statement
    /// and expression in a CFG (but <see cref="VisitCFG(ControlFlowGraph)"/> not supported).
    /// Doesn't contain any infinite recursion protection.
    /// </summary>
    public abstract class GraphUpdater : GraphVisitor<object>
    {
        #region Helper methods

        protected List<T> VisitList<T>(List<T> list) where T : BoundOperation, IPhpOperation
        {
            if (list == null || list.Count == 0)
            {
                return list;
            }

            List<T> alternate = null;
            for (int i = 0; i < list.Count; i++)
            {
                var orig = list[i];
                var visited = orig?.Accept(this);

                if (visited != orig)
                {
                    if (alternate == null)
                    {
                        alternate = new List<T>(list);
                    }
                    alternate[i] = (T)visited;
                }
            }

            return alternate ?? list;
        }

        protected ImmutableArray<T> VisitImmutableArray<T>(ImmutableArray<T> arr) where T : class, IPhpOperation
        {
            if (arr.IsDefaultOrEmpty)
            {
                return arr;
            }

            ImmutableArray<T>.Builder alternate = null;
            for (int i = 0; i < arr.Length; i++)
            {
                var orig = arr[i];
                var visited = orig?.Accept(this);

                if (visited != orig)
                {
                    if (alternate == null)
                    {
                        alternate = arr.ToBuilder();
                    }
                    alternate[i] = (T)visited;
                }
            }

            return alternate?.MoveToImmutable() ?? arr;
        }

        protected ImmutableArray<T> VisitBlockImmutableArray<T>(ImmutableArray<T> arr) where T : BoundBlock
        {
            if (arr.IsDefaultOrEmpty)
            {
                return arr;
            }

            ImmutableArray<T>.Builder alternate = null;
            for (int i = 0; i < arr.Length; i++)
            {
                var orig = arr[i];
                var visited = orig?.Accept(this);

                if (visited != orig)
                {
                    if (alternate == null)
                    {
                        alternate = arr.ToBuilder();
                    }
                    alternate[i] = (T)visited;
                }
            }

            return alternate?.MoveToImmutable() ?? arr;
        }

        protected ImmutableArray<KeyValuePair<T1, T2>> VisitImmutableArrayPairs<T1, T2>(ImmutableArray<KeyValuePair<T1, T2>> arr)
            where T1 : BoundOperation, IPhpOperation
            where T2 : BoundOperation, IPhpOperation
        {
            if (arr.IsDefaultOrEmpty)
            {
                return arr;
            }

            ImmutableArray<KeyValuePair<T1, T2>>.Builder alternate = null;
            for (int i = 0; i < arr.Length; i++)
            {
                var orig = arr[i];
                var visitedKey = orig.Key?.Accept(this);
                var visitedValue = orig.Value?.Accept(this);

                if (visitedKey != orig.Key || visitedValue != orig.Value)
                {
                    if (alternate == null)
                    {
                        alternate = arr.ToBuilder();
                    }
                    alternate[i] = new KeyValuePair<T1, T2>((T1)visitedKey, (T2)visitedValue);
                }
            }

            return alternate?.MoveToImmutable() ?? arr;
        }

        #endregion

        #region ControlFlowGraph

        /// <summary>
        /// Not supported, use <see cref="GraphRewriter"/> instead.
        /// </summary>
        public override object VisitCFG(ControlFlowGraph x)
        {
            throw new NotSupportedException("Use GraphRewriter to correctly update whole CFG.");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets value indicating the current block is in a conditional scope.
        /// </summary>
        protected bool IsConditional { get; private set; } = false;

        #endregion

        #region Graph.Block

        protected override object DefaultVisitBlock(BoundBlock x) => throw new InvalidOperationException();

        protected virtual Edge AcceptEdge(BoundBlock from, Edge edge)
        {
            return (Edge)edge?.Accept(this);
        }

        public override object VisitCFGBlock(BoundBlock x)
        {
            return x.Update(
                    VisitList(x.Statements),
                    AcceptEdge(x, x.NextEdge));
        }

        public override object VisitCFGStartBlock(StartBlock x)
        {
            return x.Update(
                    VisitList(x.Statements),
                    AcceptEdge(x, x.NextEdge));
        }

        public override object VisitCFGExitBlock(ExitBlock x)
        {
            Debug.Assert(x.NextEdge == null);

            return x.Update(VisitList(x.Statements));
        }

        public override object VisitCFGCatchBlock(CatchBlock x)
        {
            return x.Update(
                    (BoundTypeRef)Accept(x.TypeRef),
                    (BoundVariableRef)Accept(x.Variable),
                    VisitList(x.Statements),
                    AcceptEdge(x, x.NextEdge));
        }

        public override object VisitCFGCaseBlock(CaseBlock x)
        {
            return x.Update(
                    x.CaseValue,                // TODO: Visit also the expressions
                    VisitList(x.Statements),
                    AcceptEdge(x, x.NextEdge));
        }

        #endregion

        #region Graph.Edge

        protected override object DefaultVisitEdge(Edge x) => throw new InvalidOperationException();

        public override object VisitCFGSimpleEdge(SimpleEdge x)
        {
            return x.Update((BoundBlock)Accept(x.Target));
        }

        public override object VisitCFGLeaveEdge(LeaveEdge x)
        {
            return x.Update((BoundBlock)Accept(x.Target));
        }

        public override object VisitCFGConditionalEdge(ConditionalEdge x)
        {
            IsConditional = true;

            return x.Update(
                (BoundBlock)Accept(x.TrueTarget),
                (BoundBlock)Accept(x.FalseTarget),
                (BoundExpression)Accept(x.Condition));
        }

        public override object VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            IsConditional = true;

            return x.Update(
                (BoundBlock)Accept(x.BodyBlock),
                VisitBlockImmutableArray(x.CatchBlocks),
                (BoundBlock)Accept(x.FinallyBlock),
                (BoundBlock)Accept(x.NextBlock));
        }

        public override object VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x)
        {
            IsConditional = true;

            var updated = x.Update(
                (BoundBlock)Accept(x.Target),
                (BoundExpression)Accept(x.Enumeree),
                x.AreValuesAliased);

            if (updated != x)
            {
                // Fix reference from the following ForEachMoveNextEdge
                var moveNext = (ForeachMoveNextEdge)updated.NextBlock.NextEdge;
                Debug.Assert(moveNext.EnumereeEdge == x);
                updated.NextBlock.NextEdge = moveNext.Update(
                    moveNext.BodyBlock,
                    moveNext.NextBlock,
                    updated,
                    moveNext.KeyVariable,
                    moveNext.ValueVariable,
                    moveNext.MoveNextSpan);
            }

            return updated;
        }

        public override object VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            IsConditional = true;

            return x.Update(
                (BoundBlock)Accept(x.BodyBlock),
                (BoundBlock)Accept(x.NextBlock),
                x.EnumereeEdge,                                     // It updates this reference in its visit instead
                (BoundReferenceExpression)Accept(x.KeyVariable),
                (BoundReferenceExpression)Accept(x.ValueVariable),
                x.MoveNextSpan);
        }

        public override object VisitCFGSwitchEdge(SwitchEdge x)
        {
            IsConditional = true;

            return x.Update(
                (BoundExpression)Accept(x.SwitchValue),
                VisitBlockImmutableArray(x.CaseBlocks),
                (BoundBlock)Accept(x.NextBlock));
        }

        #endregion

        #region Expressions

        protected override object DefaultVisitOperation(BoundOperation x)
        {
            return x;
        }

        protected override object VisitRoutineCall(BoundRoutineCall x)
        {
            // It must be updated in the visits of non-abstract subclassess
            return x;
        }

        public override object VisitCopyValue(BoundCopyValue x)
        {
            return x.Update((BoundExpression)Accept(x.Expression));
        }

        public override object VisitArgument(BoundArgument x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Value),
                x.ArgumentKind);
        }
        internal override object VisitIndirectTypeRef(BoundIndirectTypeRef x)
        {
            return x.Update(
                (BoundExpression)Accept(x.TypeExpression),
                x.ObjectTypeInfoSemantic);
        }

        internal override object VisitMultipleTypeRef(BoundMultipleTypeRef x)
        {
            var typeRefs = VisitImmutableArray(x.TypeRefs);
            if (typeRefs.Length == 1)
            {
                // reduce // CONSIDER: here?
                return typeRefs[0];
            }
            else
            {
                return x.Update(typeRefs);
            }
        }

        public override object VisitRoutineName(BoundRoutineName x)
        {
            return x.Update(
                x.NameValue,
                (BoundExpression)Accept(x.NameExpression));
        }

        public override object VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            return x.Update(
                (BoundRoutineName)Accept(x.Name),
                x.NameOpt,
                VisitImmutableArray(x.ArgumentsInSourceOrder),
                VisitImmutableArray(x.TypeArguments));
        }

        public override object VisitInstanceFunctionCall(BoundInstanceFunctionCall x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Instance),
                (BoundRoutineName)Accept(x.Name),
                VisitImmutableArray(x.ArgumentsInSourceOrder),
                VisitImmutableArray(x.TypeArguments));
        }

        public override object VisitStaticFunctionCall(BoundStaticFunctionCall x)
        {
            return x.Update(
                (BoundTypeRef)Accept(x.TypeRef),
                (BoundRoutineName)Accept(x.Name),
                VisitImmutableArray(x.ArgumentsInSourceOrder),
                VisitImmutableArray(x.TypeArguments));
        }

        public override object VisitEcho(BoundEcho x)
        {
            return x.Update(VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override object VisitConcat(BoundConcatEx x)
        {
            return x.Update(VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override object VisitNew(BoundNewEx x)
        {
            return x.Update(
                (BoundTypeRef)Accept(x.TypeRef),
                VisitImmutableArray(x.ArgumentsInSourceOrder),
                VisitImmutableArray(x.TypeArguments));
        }

        public override object VisitInclude(BoundIncludeEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.ArgumentsInSourceOrder[0].Value),
                x.InclusionType);
        }

        public override object VisitExit(BoundExitEx x)
        {
            return x.Update(VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override object VisitAssert(BoundAssertEx x)
        {
            return x.Update(VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override object VisitBinaryExpression(BoundBinaryEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Left),
                (BoundExpression)Accept(x.Right),
                x.Operation);
        }

        public override object VisitUnaryExpression(BoundUnaryEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Operand),
                x.Operation);
        }

        public override object VisitConversion(BoundConversionEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Operand),
                (BoundTypeRef)Accept(x.TargetType));
        }

        public override object VisitIncDec(BoundIncDecEx x)
        {
            return x.Update(
                (BoundReferenceExpression)Accept(x.Target),
                x.IsIncrement,
                x.IsPostfix);
        }

        public override object VisitConditional(BoundConditionalEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Condition),
                (BoundExpression)Accept(x.IfTrue),
                (BoundExpression)Accept(x.IfFalse));
        }

        public override object VisitAssign(BoundAssignEx x)
        {
            return x.Update(
                (BoundReferenceExpression)Accept(x.Target),
                (BoundExpression)Accept(x.Value));
        }

        public override object VisitCompoundAssign(BoundCompoundAssignEx x)
        {
            return x.Update(
                (BoundReferenceExpression)Accept(x.Target),
                (BoundExpression)Accept(x.Value),
                x.Operation);
        }

        public override object VisitVariableName(BoundVariableName x)
        {
            return x.Update(
                x.NameValue,
                (BoundExpression)Accept(x.NameExpression));
        }

        public override object VisitVariableRef(BoundVariableRef x)
        {
            return x.Update((BoundVariableName)Accept(x.Name));
        }

        public override object VisitTemporalVariableRef(BoundTemporalVariableRef x)
        {
            Debug.Assert(x.Name.IsDirect);
            return x;
        }

        public override object VisitList(BoundListEx x)
        {
            return x.Update(VisitImmutableArrayPairs(x.Items));
        }

        public override object VisitFieldRef(BoundFieldRef x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Instance),
                (BoundTypeRef)Accept(x.ContainingType),
                (BoundVariableName)Accept(x.FieldName));
        }

        public override object VisitArray(BoundArrayEx x)
        {
            return x.Update(VisitImmutableArrayPairs(x.Items));
        }

        public override object VisitArrayItem(BoundArrayItemEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Array),
                (BoundExpression)Accept(x.Index));
        }

        public override object VisitArrayItemOrd(BoundArrayItemOrdEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Array),
                (BoundExpression)Accept(x.Index));
        }

        public override object VisitInstanceOf(BoundInstanceOfEx x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Operand),
                (BoundTypeRef)Accept(x.AsType));
        }

        public override object VisitGlobalConstUse(BoundGlobalConst x)
        {
            return x;
        }

        public override object VisitGlobalConstDecl(BoundGlobalConstDeclStatement x)
        {
            return x.Update(
                x.Name,
                (BoundExpression)Accept(x.Value));
        }

        public override object VisitPseudoConstUse(BoundPseudoConst x)
        {
            return x;
        }

        public override object VisitPseudoClassConstUse(BoundPseudoClassConst x)
        {
            return x.Update(
                (BoundTypeRef)Accept(x.TargetType),
                x.ConstType);
        }

        public override object VisitIsEmpty(BoundIsEmptyEx x)
        {
            return x.Update((BoundExpression)Accept(x.Operand));
        }

        public override object VisitIsSet(BoundIsSetEx x)
        {
            return x.Update((BoundReferenceExpression)Accept(x.VarReference));
        }

        public override object VisitOffsetExists(BoundOffsetExists x)
        {
            return x.Update((BoundExpression)Accept(x.Receiver), (BoundExpression)Accept(x.Index));
        }

        public override object VisitTryGetItem(BoundTryGetItem x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Array),
                (BoundExpression)Accept(x.Index),
                (BoundExpression)Accept(x.Fallback));
        }

        public override object VisitLambda(BoundLambda x)
        {
            return x.Update(VisitImmutableArray(x.UseVars));
        }

        public override object VisitEval(BoundEvalEx x)
        {
            return x.Update((BoundExpression)Accept(x.CodeExpression));
        }


        public override object VisitYieldEx(BoundYieldEx x)
        {
            return x;
        }

        public override object VisitYieldFromEx(BoundYieldFromEx x)
        {
            return x.Update((BoundExpression)Accept(x.Operand));
        }

        #endregion

        #region Statements

        public override object VisitUnset(BoundUnset x)
        {
            return x.Update((BoundReferenceExpression)Accept(x.Variable));
        }

        public override object VisitEmptyStatement(BoundEmptyStatement x)
        {
            return x;
        }

        public override object VisitBlockStatement(Graph.BoundBlock x)
        {
            Debug.Assert(x.NextEdge == null);

            return x.Update(
                VisitList(x.Statements),
                x.NextEdge);
        }

        public override object VisitExpressionStatement(BoundExpressionStatement x)
        {
            return x.Update((BoundExpression)Accept(x.Expression));
        }

        public override object VisitReturn(BoundReturnStatement x)
        {
            return x.Update((BoundExpression)Accept(x.Returned));
        }

        public override object VisitThrow(BoundThrowStatement x)
        {
            return x.Update((BoundExpression)x.Thrown);
        }

        public override object VisitFunctionDeclaration(BoundFunctionDeclStatement x)
        {
            return x;
        }

        public override object VisitTypeDeclaration(BoundTypeDeclStatement x)
        {
            return x;
        }

        public override object VisitGlobalStatement(BoundGlobalVariableStatement x)
        {
            return x.Update((BoundVariableRef)Accept(x.Variable));
        }

        public override object VisitStaticStatement(BoundStaticVariableStatement x)
        {
            return x;
        }

        public override object VisitYieldStatement(BoundYieldStatement x)
        {
            return x.Update(
                x.YieldIndex,
                (BoundExpression)Accept(x.YieldedValue),
                (BoundExpression)Accept(x.YieldedKey));
        }

        public override object VisitDeclareStatement(BoundDeclareStatement x)
        {
            return x;
        }

        #endregion
    }
}
