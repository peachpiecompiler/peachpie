using System;
using System.Collections.Generic;
using System.Text;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Visitor used to traverse CFG and all its operations with infinite recursion prevention.
    /// </summary>
    public abstract class GraphExplorer<TReturn> : GraphWalker<TReturn>
    {
        public int ExploredColor { get; private set; }

        /// <summary>
        /// Create a new instance of <see cref="GraphExplorer{T}"/>, optionally specifying a custom
        /// color for exploration.
        /// </summary>
        /// <param name="exploredColor">Custom color to be used for exploration, specify if
        /// <see cref="VisitCFG(ControlFlowGraph)"/> is not going to be used.</param>
        public GraphExplorer(int? exploredColor = null)
        {
            if (exploredColor.HasValue)
            {
                ExploredColor = exploredColor.Value;
            }
        }

        /// <summary>
        /// Set <see cref="ExploredColor"/> to a new color from <paramref name="x"/> and perform
        /// the exploration.
        /// </summary>
        public sealed override TReturn VisitCFG(ControlFlowGraph x)
        {
            ExploredColor = x.NewColor();
            VisitCFGInternal(x);

            return default;
        }

        protected virtual void VisitCFGInternal(ControlFlowGraph x)
        {
            base.VisitCFG(x);
        }

        protected sealed override TReturn DefaultVisitBlock(BoundBlock x)
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
