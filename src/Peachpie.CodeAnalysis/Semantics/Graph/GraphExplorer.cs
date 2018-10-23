using System;
using System.Collections.Generic;
using System.Text;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Visitor used to traverse CFG and all its operations with infinite recursion prevention.
    /// </summary>
    public abstract class GraphExplorer<T> : GraphWalker<T>
    {
        public int ExploredColor { get; private set; }

        public sealed override T VisitCFG(ControlFlowGraph x)
        {
            ExploredColor = x.NewColor();
            VisitCFGInternal(x);

            return default;
        }

        protected virtual void VisitCFGInternal(ControlFlowGraph x)
        {
            base.VisitCFG(x);
        }

        protected sealed override T DefaultVisitBlock(BoundBlock x)
        {
            if (x.Tag != ExploredColor)
            {
                x.Tag = ExploredColor;
                DefaultVisitUnexploredBlock(x);
            }

            return default;
        }

        protected virtual void DefaultVisitUnexploredBlock(BoundBlock x)
        {
            base.DefaultVisitBlock(x);
        }
    }
}
