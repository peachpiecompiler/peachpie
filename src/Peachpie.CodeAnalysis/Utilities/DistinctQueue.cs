using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.Semantics.Graph;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Utilities
{
    /// <summary>
    /// Represents priority queue where items are enqueued just once and queue can be accessed in parallel.
    /// </summary>
    internal sealed class DistinctQueue<T> where T : BoundBlock
    {
        readonly object _syncRoot = new object();

        /// <summary>
        /// A heap to enable fast insertion and minimum extraction.
        /// </summary>
        readonly PriorityQueue<T> _queue;

        public DistinctQueue(IComparer<T> comparer)
        {
            _queue = new PriorityQueue<T>(comparer);
        }

        /// <summary>
        /// Count of items in the queue.
        /// </summary>
        public int Count
        {
            get { return _queue.Count; }
        }

        /// <summary>
        /// Enqueues item into the queue.
        /// </summary>
        public bool Enqueue(T value)
        {
            Debug.Assert(value != null);

            lock (_syncRoot)
            {
                if (!value.IsEnqueued)
                {
                    _queue.Push(value);
                    value.IsEnqueued = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Dequeues item from the queue.
        /// </summary>
        public bool TryDequeue(out T value)
        {
            lock (_syncRoot)
            {
                if (_queue.Count != 0)
                {
                    value = _queue.Top;
                    _queue.Pop();

                    Debug.Assert(value.IsEnqueued);
                    value.IsEnqueued = false;

                    return true;
                }
                else
                {
                    value = default(T);
                    return false;
                }
            }
        }
    }
}
