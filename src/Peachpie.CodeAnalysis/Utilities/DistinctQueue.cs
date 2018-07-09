using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Utilities
{
    /// <summary>
    /// Represents queue where items are enqueued just once and queue can be accessed in parralel.
    /// </summary>
    internal sealed class DistinctQueue<T>
    {
        readonly object _syncRoot = new object();

        // TODO: Use a heap (with possible duplicate key values) instead
        readonly SortedSet<T> _queue;

        public DistinctQueue(IComparer<T> comparer)
        {
            _queue = new SortedSet<T>(comparer);
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
                return _queue.Add(value);
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
                    value = _queue.First();
                    _queue.Remove(value);
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
