using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Enables to transform a <see cref="ControlFlowGraph"/> in a straightforward manner.
    /// </summary>
    /// <remarks>
    /// The transformation is performed in two phases: updating and repairing. During updating,
    /// virtual Visit* and OnVisit* methods are called on all the nodes, edges and operations
    /// in the CFG. <see cref="ExploredColor"/> is used in the update to mark all the visited
    /// nodes and prevent infinite recursion. Any update of the graph marks all the changed nodes
    /// (their new versions) as <see cref="ChangedColor"/>. If such an update happens, in the
    /// repairing phase the whole graph is traversed again and all the unmodified blocks are cloned,
    /// the edges fixed and all the blocks in the final graph marked as <see cref="RepairedColor"/>.
    /// 
    /// This is done to prevent the graph nodes from pointing to nodes of the older version of the
    /// graph. Possible optimization would be to allow sharing graph parts in acyclic CFGs.
    /// </remarks>
    public class GraphRewriter : GraphUpdater
    {
        private Dictionary<BoundBlock, BoundBlock> _updatedBlocks;
        private List<BoundBlock> _possiblyUnreachableBlocks;
        private bool _isRepairing;

        public int ExploredColor { get; private set; }

        public int ChangedColor { get; private set; }

        public int RepairedColor { get; private set; }

        #region Helper class

        /// <summary>
        /// Finds all yield statements in unreachable blocks (those not yet colored by <see cref="ExploredColor"/>)
        /// of the original graph. The blocks encountered along the way are coloured as a side effect.
        /// </summary>
        private class UnreachableYieldFinder : GraphExplorer<VoidStruct>
        {
            public List<BoundYieldStatement> Yields { get; private set; }

            public UnreachableYieldFinder(int exploredColor) : base(exploredColor)
            { }

            public override VoidStruct VisitYieldStatement(BoundYieldStatement boundYieldStatement)
            {
                if (Yields == null)
                {
                    Yields = new List<BoundYieldStatement>();
                }

                Yields.Add(boundYieldStatement);

                return base.VisitYieldStatement(boundYieldStatement);
            }
        }

        #endregion

        #region Helper methods

        private bool IsExplored(BoundBlock x) => x.Tag >= ExploredColor;

        private bool IsChanged(BoundBlock x) => x.Tag >= ChangedColor;

        private bool IsRepaired(BoundBlock x) => x.Tag == RepairedColor;

        private BoundBlock TryGetNewVersion(BoundBlock block)
        {
            if (_updatedBlocks == null || !_updatedBlocks.TryGetValue(block, out var mappedBlock))
            {
                return block;
            }
            else
            {
                return mappedBlock;
            }
        }

        private void MapToNewVersion(BoundBlock oldBlock, BoundBlock newBlock)
        {
            newBlock.Tag = ChangedColor;

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

        /// <summary>
        /// Inform about a possible unreachability of this block due to a change in the graph.
        /// </summary>
        protected void NotePossiblyUnreachable(BoundBlock block)
        {
            if (_possiblyUnreachableBlocks == null)
            {
                _possiblyUnreachableBlocks = new List<BoundBlock>();
            }

            _possiblyUnreachableBlocks.Add(block);
        }

        private BoundBlock Repair(BoundBlock block)
        {
            Debug.Assert(_isRepairing);

            if (IsRepaired(block))
            {
                return block;
            }

            if (!IsChanged(block))
            {
                if (!_updatedBlocks.TryGetValue(block, out var repaired))
                {
                    repaired = block.Clone();
                    MapToNewVersion(block, repaired);
                }

                block = repaired;
            }

            if (!IsRepaired(block))
            {
                block.Tag = RepairedColor;
                block.NextEdge = (Edge)Accept(block.NextEdge);
            }

            return block;
        }

        #endregion

        #region ControlFlowGraph

        public sealed override object VisitCFG(ControlFlowGraph x)
        {
            OnVisitCFG(x);

            ExploredColor = x.NewColor();
            ChangedColor = x.NewColor();
            RepairedColor = x.NewColor();
            _updatedBlocks = null;

            var updatedStart = (StartBlock)Accept(x.Start);
            var updatedExit = TryGetNewVersion(x.Exit);
            var yields = x.Yields;

            var unreachableBlocks = x.UnreachableBlocks;
            if (_possiblyUnreachableBlocks != null)
            {
                unreachableBlocks = unreachableBlocks.AddRange(_possiblyUnreachableBlocks.Where(b => !IsExplored(b)));

                // Remove any yields found in the unreachable blocks, eventually marking all the original blocks as explored
                if (!yields.IsDefaultOrEmpty && unreachableBlocks != x.UnreachableBlocks)
                {
                    var yieldFinder = new UnreachableYieldFinder(ExploredColor);

                    for (int i = x.UnreachableBlocks.Length; i < unreachableBlocks.Length; i++)
                    {
                        yieldFinder.VisitCFGBlock(unreachableBlocks[i]);
                    }

                    if (yieldFinder.Yields != null)
                    {
                        yields = yields.RemoveRange(yieldFinder.Yields);
                    }
                }
            }

            // Rescan and fix the whole CFG if any blocks were modified
            if (_updatedBlocks != null)
            {
                _isRepairing = true;
                updatedStart = (StartBlock)Accept(updatedStart);
                updatedExit = _updatedBlocks[x.Exit];             // It must have been updated by the repair
            }

            return x.Update(
                updatedStart,
                updatedExit,
                x.Labels,           // Keep them all, they are here only for the diagnostic purposes
                yields,
                unreachableBlocks);
        }

        protected virtual void OnVisitCFG(ControlFlowGraph x)
        { }

        #endregion

        #region Graph.Block

        protected sealed override object DefaultVisitBlock(BoundBlock x) => throw new InvalidOperationException();

        public sealed override object VisitCFGBlock(BoundBlock x)
        {
            if (_isRepairing)
            {
                return Repair(x);
            }
            else
            {
                return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGBlock(x));
            }
        }

        public sealed override object VisitCFGStartBlock(StartBlock x)
        {
            if (_isRepairing)
            {
                return Repair(x);
            }
            else
            {
                return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGStartBlock(x));
            }
        }

        public sealed override object VisitCFGExitBlock(ExitBlock x)
        {
            if (_isRepairing)
            {
                return Repair(x);
            }
            else
            {
                return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGExitBlock(x));
            }
        }

        public sealed override object VisitCFGCatchBlock(CatchBlock x)
        {
            if (_isRepairing)
            {
                return Repair(x);
            }
            else
            {
                return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGCatchBlock(x));
            }
        }

        public sealed override object VisitCFGCaseBlock(CaseBlock x)
        {
            if (_isRepairing)
            {
                return Repair(x);
            }
            else
            {
                return IsExplored(x) ? x : MapIfUpdated(x, OnVisitCFGCaseBlock(x));
            }
        }

        public virtual BoundBlock OnVisitCFGBlock(BoundBlock x)
        {
            x.Tag = ExploredColor;
            return (BoundBlock)base.VisitCFGBlock(x);
        }

        public virtual StartBlock OnVisitCFGStartBlock(StartBlock x)
        {
            x.Tag = ExploredColor;
            return (StartBlock)base.VisitCFGStartBlock(x);
        }

        public virtual ExitBlock OnVisitCFGExitBlock(ExitBlock x)
        {
            Debug.Assert(x.NextEdge == null);

            x.Tag = ExploredColor;
            return (ExitBlock)base.VisitCFGExitBlock(x);
        }

        public virtual CatchBlock OnVisitCFGCatchBlock(CatchBlock x)
        {
            x.Tag = ExploredColor;
            return (CatchBlock)base.VisitCFGCatchBlock(x);
        }

        public virtual CaseBlock OnVisitCFGCaseBlock(CaseBlock x)
        {
            x.Tag = ExploredColor;
            return (CaseBlock)base.VisitCFGCaseBlock(x);
        }

        #endregion
    }
}
