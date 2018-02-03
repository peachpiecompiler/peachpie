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

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class BoundBlock
    {
        /// <summary>
        /// Initial block flow state.
        /// Can be <c>null</c> in case there is no flow into the block or the state was released.
        /// </summary>
        internal FlowState FlowState
        {
            get; set;
        }
    }

    partial class ExitBlock
    {
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
