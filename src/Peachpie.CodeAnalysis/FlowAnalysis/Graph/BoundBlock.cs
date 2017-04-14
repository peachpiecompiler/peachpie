using Pchp.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Peachpie.CodeAnalysis.Utilities;

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

        /// <summary>
        /// Updates flow state of incoming edge and gets merged states of all incoming edges.
        /// </summary>
        /// <param name="edgeLabel">Incoming edge label. Can be anything identifying the edge except <c>null</c>.</param>
        /// <param name="state">Flow state of the incoming edge.</param>
        /// <returns>Merged initial block state.</returns>
        internal FlowState UpdateIncomingFlowState(object edgeLabel, FlowState state)
        {
            Debug.Assert(edgeLabel != null, $"{nameof(edgeLabel)} is null");
            Debug.Assert(state != null, $"{nameof(state)} is null");

            if (_incommingFlowStates == null)
            {
                _incommingFlowStates = new Dictionary<object, FlowState>(ReferenceEqualityComparer.Default);
            }

            // update incoming flow state
            _incommingFlowStates[edgeLabel] = state.Clone();

            // merge states
            FlowState result = null;
            foreach (var s in _incommingFlowStates)
            {
                result = (result != null) ? result.Merge(s.Value) : s.Value;
            }

            Debug.Assert(result != null, $"{nameof(result)} is null");
            return result;
        }

        Dictionary<object, FlowState> _incommingFlowStates;
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
                _subscribers = new HashSet<BoundBlock>();

            _subscribers.Add(x);
        }

        internal IEnumerable<BoundBlock> Subscribers => (IEnumerable<BoundBlock>)_subscribers ?? ImmutableArray<BoundBlock>.Empty;

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
