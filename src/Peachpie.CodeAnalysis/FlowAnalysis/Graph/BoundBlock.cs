using Pchp.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Peachpie.CodeAnalysis.Utilities;
using System.Collections.Concurrent;
using Pchp.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class BoundBlock
    {
        /// <summary>
        /// Initial block flow state.
        /// Can be <c>null</c> in case there is no flow into the block, the state was released, or
        /// doesn't match the current version of the analysis.
        /// </summary>
        internal FlowState FlowState
        {
            get
            {
                return (_flowState != null && _flowState.Version == _flowState.FlowContext.Version) ? _flowState : null;
            }
            set
            {
                _flowState = value;
            }
        }
        FlowState _flowState;

        /// <summary>
        /// Whether to reanalyse this block regardless of the flow state convergence.
        /// </summary>
        internal virtual bool ForceRepeatedAnalysis => false;

        /// <summary>
        /// Comparer to sort the blocks in the ascending order of <see cref="Ordinal"/>.
        /// </summary>
        internal sealed class OrdinalComparer : IComparer<BoundBlock>, IEqualityComparer<BoundBlock>
        {
            int IEqualityComparer<BoundBlock>.GetHashCode(BoundBlock obj) => obj.GetHashCode();

            bool IEqualityComparer<BoundBlock>.Equals(BoundBlock x, BoundBlock y) => object.ReferenceEquals(x, y);

            int IComparer<BoundBlock>.Compare(BoundBlock x, BoundBlock y) => x.Ordinal - y.Ordinal;
        }
    }

    partial class ExitBlock
    {
        /// <summary>
        /// ExitBlock propagates the return type (which is not a part of a flow state) to all the callers.
        /// Therefore, it must be reanalysed every time it is encountered, even if the flow state didn't change.
        /// 
        /// TODO: Consider marking the return type as dirty and return true only in that case.
        /// </summary>
        internal override bool ForceRepeatedAnalysis => true;

        #region Callers // TODO: EdgeToCallers

        /// <summary>
        /// Subscribe a block to be analysed when the exit block is reached and the routine return value changes.
        /// </summary>
        internal void Subscribe(BoundBlock x)
        {
            if (_subscribers == null)
            {
                lock (this)
                {
                    _subscribers = _subscribers ?? new HashSet<BoundBlock>();
                }
            }

            lock (_subscribers)
            {
                _subscribers.Add(x);
            }
        }

        internal ICollection<BoundBlock> Subscribers => (ICollection<BoundBlock>)_subscribers ?? Array.Empty<BoundBlock>();

        /// <summary>
        /// Set of blocks making call to this routine (callers) (may be from another CFG) waiting for return type of this routine.
        /// </summary>
        HashSet<BoundBlock> _subscribers;

        /// <summary>
        /// Return type last seen by subscribers.
        /// </summary>
        internal TypeRefMask _lastReturnTypeMask = 0;

        #endregion
    }
}
