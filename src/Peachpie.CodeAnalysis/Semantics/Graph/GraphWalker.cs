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
    public abstract class GraphWalker<T> : SingleBlockWalker<T>
    {
        #region ControlFlowGraph

        public override T VisitCFG(ControlFlowGraph x)
        {
            x.Start.Accept(this);

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
            base.VisitCFGConditionalEdge(x);

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
            base.VisitCFGForeachEnumereeEdge(x);

            x.NextBlock.Accept(this);

            return default;
        }

        public override T VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            base.VisitCFGForeachMoveNextEdge(x);

            x.BodyBlock.Accept(this);
            x.NextBlock.Accept(this);

            return default;
        }

        public override T VisitCFGSwitchEdge(SwitchEdge x)
        {
            base.VisitCFGSwitchEdge(x);

            //
            var arr = x.CaseBlocks;
            for (int i = 0; i < arr.Length; i++)
                arr[i].Accept(this);

            return default;
        }

        #endregion
    }
}
