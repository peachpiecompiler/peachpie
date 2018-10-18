using System;
using System.Collections.Generic;
using System.Text;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Base visitor for control flow graphs.
    /// </summary>
    /// <typeparam name="TResult">Return type of all the Visit operations, use <see cref="EmptyStruct"/> if none.</typeparam>
    public abstract class GraphVisitor<TResult> : PhpOperationVisitor<TResult>
    {
        #region ControlFlowGraph

        public virtual TResult VisitCFG(ControlFlowGraph x) => default;

        #endregion

        #region Graph.Block

        protected virtual TResult DefaultVisitBlock(BoundBlock x) => default;

        public virtual TResult VisitCFGBlock(BoundBlock x) => DefaultVisitBlock(x);

        public virtual TResult VisitCFGExitBlock(ExitBlock x) => DefaultVisitBlock(x);

        public virtual TResult VisitCFGCatchBlock(CatchBlock x) => DefaultVisitBlock(x);

        public virtual TResult VisitCFGCaseBlock(CaseBlock x) => DefaultVisitBlock(x);

        #endregion

        #region Graph.Edge

        protected virtual TResult DefaultVisitEdge(Edge x) => default;

        public virtual TResult VisitCFGSimpleEdge(SimpleEdge x) => DefaultVisitEdge(x);

        public virtual TResult VisitCFGConditionalEdge(ConditionalEdge x) => DefaultVisitEdge(x);

        public virtual TResult VisitCFGTryCatchEdge(TryCatchEdge x) => DefaultVisitEdge(x);

        public virtual TResult VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x) => DefaultVisitEdge(x);

        public virtual TResult VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x) => DefaultVisitEdge(x);

        public virtual TResult VisitCFGSwitchEdge(SwitchEdge x) => DefaultVisitEdge(x);

        #endregion
    }
}
