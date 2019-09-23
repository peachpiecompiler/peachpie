using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Visitor used to traverse a single block and its adjacent edge.
    /// </summary>
    public abstract class SingleBlockWalker<T> : GraphWalker<T>
    {
        public override T VisitCFGSimpleEdge(SimpleEdge x)
        {
            return default;
        }

        public override T VisitCFGConditionalEdge(ConditionalEdge x)
        {
            Accept(x.Condition);

            return default;
        }

        public override T VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            return default;
        }

        public override T VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x)
        {
            Accept(x.Enumeree);

            return default;
        }

        public override T VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            Accept(x.ValueVariable);
            Accept(x.KeyVariable);

            return default;
        }

        public override T VisitCFGSwitchEdge(SwitchEdge x)
        {
            Accept(x.SwitchValue);

            return default;
        }
    }
}
