using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    public class GraphRewriter : GraphVisitor<object>
    {
        private Dictionary<BoundBlock, BoundBlock> _updatedBlocks;
        private List<BoundBlock> _possiblyUnreachableBlocks;

        public int ExploredColor { get; private set; }

        #region Helper methods

        private bool IsExplored(BoundBlock x) => x.Tag == ExploredColor;

        private List<T> VisitList<T>(List<T> list) where T : BoundOperation, IPhpOperation
        {
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

        private ImmutableArray<T> VisitImmutableArray<T>(ImmutableArray<T> arr) where T : BoundOperation, IPhpOperation
        {
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

        private ImmutableArray<KeyValuePair<T1, T2>> VisitImmutableArrayPairs<T1, T2>(ImmutableArray<KeyValuePair<T1, T2>> arr)
            where T1 : BoundOperation, IPhpOperation
            where T2 : BoundOperation, IPhpOperation
        {
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

        private T TryGetNewVersion<T>(T block)
            where T : BoundBlock
        {
            if (_updatedBlocks == null || !_updatedBlocks.TryGetValue(block, out var mappedBlock))
            {
                return block;
            }
            else
            {
                return (T)mappedBlock;
            }
        }

        private void MapToNewVersion(BoundBlock oldBlock, BoundBlock newBlock)
        {
            if (_updatedBlocks == null)
            {
                _updatedBlocks = new Dictionary<BoundBlock, BoundBlock>();
            }

            _updatedBlocks.Add(oldBlock, newBlock);
        }

        private BoundBlock MapIfUpdated(BoundBlock original, BoundBlock updated)
        {
            if (original == updated)
            {
                return original;
            }
            else
            {
                MapToNewVersion(original, updated);
                return updated;
            }
        }

        protected void NotePossiblyUnreachable(BoundBlock block)
        {
            if (_possiblyUnreachableBlocks == null)
            {
                _possiblyUnreachableBlocks = new List<BoundBlock>();
            }

            _possiblyUnreachableBlocks.Add(block);
        }

        #endregion

        #region ControlFlowGraph

        public sealed override object VisitCFG(ControlFlowGraph x)
        {
            OnVisitCFG(x);

            ExploredColor = x.NewColor();
            _updatedBlocks = null;

            var updatedStart = (StartBlock)Accept(x.Start);
            var updatedExit = TryGetNewVersion(x.Exit);

            var unreachableBlocks = x.UnreachableBlocks;
            if (_possiblyUnreachableBlocks != null)
            {
                unreachableBlocks = unreachableBlocks.AddRange(_possiblyUnreachableBlocks.Where(b => !IsExplored(b)));
            }

            // TODO: Rescan and fix the whole CFG if _updatedBlocks is not null

            return x.Update(
                updatedStart,
                updatedExit,
                x.Labels,
                x.Yields,
                unreachableBlocks);
        }

        protected virtual void OnVisitCFG(ControlFlowGraph x)
        { }

        #endregion

        #region Graph.Block

        protected sealed override object DefaultVisitBlock(BoundBlock x) => throw new InvalidOperationException();

        public sealed override object VisitCFGBlock(BoundBlock x)
        {
            return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGBlock(x));
        }

        public sealed override object VisitCFGStartBlock(StartBlock x)
        {
            return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGStartBlock(x));
        }

        public sealed override object VisitCFGExitBlock(ExitBlock x)
        {
            return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGExitBlock(x));
        }

        public sealed override object VisitCFGCatchBlock(CatchBlock x)
        {
            return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGCatchBlock(x));
        }

        public sealed override object VisitCFGCaseBlock(CaseBlock x)
        {
            return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGCaseBlock(x));
        }

        public virtual BoundBlock OnVisitCFGBlock(BoundBlock x)
        {
            x.Tag = ExploredColor;
            return x.Update(
                    VisitList(x.Statements),
                    (Edge)Accept(x.NextEdge));
        }

        public virtual StartBlock OnVisitCFGStartBlock(StartBlock x)
        {
            x.Tag = ExploredColor;
            return x.Update(
                    VisitList(x.Statements),
                    (Edge)Accept(x.NextEdge));
        }

        public virtual ExitBlock OnVisitCFGExitBlock(ExitBlock x)
        {
            x.Tag = ExploredColor;
            return x.Update(
                    VisitList(x.Statements),
                    (Edge)Accept(x.NextEdge));
        }

        public virtual CatchBlock OnVisitCFGCatchBlock(CatchBlock x)
        {
            x.Tag = ExploredColor;
            return x.Update(
                    (BoundTypeRef)Accept(x.TypeRef),
                    (BoundVariableRef)Accept(x.Variable),
                    VisitList(x.Statements),
                    (Edge)Accept(x.NextEdge));
        }

        public virtual CaseBlock OnVisitCFGCaseBlock(CaseBlock x)
        {
            x.Tag = ExploredColor;
            return x.Update(
                    x.CaseValue,                // TODO: Visit also the expressions
                    VisitList(x.Statements),
                    (Edge)Accept(x.NextEdge));
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
            return x.Update(
                (BoundBlock)Accept(x.TrueTarget),
                (BoundBlock)Accept(x.FalseTarget),
                (BoundExpression)Accept(x.Condition));
        }

        public override object VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            return x.Update(
                (BoundBlock)Accept(x.BodyBlock),
                VisitImmutableArray(x.CatchBlocks),
                (BoundBlock)Accept(x.FinallyBlock),
                (BoundBlock)Accept(x.NextBlock));
        }

        public override object VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x)
        {
            return x.Update(
                (BoundBlock)Accept(x.Target),
                (BoundExpression)Accept(x.Enumeree),
                x.AreValuesAliased);
        }

        public override object VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            return x.Update(
                (BoundBlock)Accept(x.BodyBlock),
                (BoundBlock)Accept(x.NextBlock),
                (ForeachEnumereeEdge)Accept(x.EnumereeEdge),
                (BoundReferenceExpression)Accept(x.KeyVariable),
                (BoundReferenceExpression)Accept(x.ValueVariable),
                x.MoveSpan);
        }

        public override object VisitCFGSwitchEdge(SwitchEdge x)
        {
            return x.Update(
                (BoundExpression)Accept(x.SwitchValue),
                VisitImmutableArray(x.CaseBlocks),
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

        public override object VisitLiteral(BoundLiteral x)
        {
            return x;
        }

        public override object VisitArgument(BoundArgument x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Value),
                x.ArgumentKind);
        }

        public override object VisitTypeRef(BoundTypeRef x)
        {
            return x.Update(
                (BoundExpression)Accept(x.TypeExpression),
                x.TypeRef,
                x.ObjectTypeInfoSemantic,
                x.HasClassNameRestriction); ;
        }

        public override object VisitMultipleTypeRef(BoundMultipleTypeRef x)
        {
            var typeRefs = VisitImmutableArray(x.BoundTypes);
            if (typeRefs == x.BoundTypes)
            {
                return x;
            }
            else if (typeRefs.Length == 1)
            {
                return typeRefs[0];
            }
            else
            {
                return x.Update(
                    typeRefs,
                    x.TypeRef,
                    x.ObjectTypeInfoSemantic,
                    x.HasClassNameRestriction);
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
                VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override object VisitInstanceFunctionCall(BoundInstanceFunctionCall x)
        {
            return x.Update(
                (BoundExpression)Accept(x.Instance),
                (BoundRoutineName)Accept(x.Name),
                VisitImmutableArray(x.ArgumentsInSourceOrder));
        }

        public override object VisitStaticFunctionCall(BoundStaticFunctionCall x)
        {
            return x.Update(
                (BoundTypeRef)Accept(x.TypeRef),
                (BoundRoutineName)Accept(x.Name),
                VisitImmutableArray(x.ArgumentsInSourceOrder));
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
                VisitImmutableArray(x.ArgumentsInSourceOrder));
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
                (BoundExpression)Accept(x.Value));
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
            // TODO: Find out whether is this called at all and decide how to handle NextEdge

            //// TODO: Return a new block if any change was made (after this class is turned into GraphRewriter)
            //for (int i = 0; i < x.Statements.Count; i++)
            //{
            //    x.Statements[i] = (BoundStatement)Accept(x.Statements[i]);
            //}

            return x;
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
