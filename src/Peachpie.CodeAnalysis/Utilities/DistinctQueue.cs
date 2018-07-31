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
    internal sealed class DistinctQueue<T>
    {
        readonly object _syncRoot = new object();

        /// <summary>
        /// A set to mark already inserted objects.
        /// </summary>
        readonly HashSet<T> _set;

        /// <summary>
        /// A heap to enable fast insertion and minimum extraction.
        /// </summary>
        readonly PriorityQueue<T> _queue;

        public DistinctQueue(IComparer<T> comparer)
        {
            _queue = new PriorityQueue<T>(comparer);
            _set = new HashSet<T>();
        }

        /// <summary>
        /// Count of items in the queue.
        /// </summary>
        public int Count
        {
            get { return _queue.Count; }
        }

        /// <summary>
        /// Gets value indicating 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(T value)
        {
            lock (_syncRoot)
            {
                return _set.Contains(value);
            }
        }

        /// <summary>
        /// Enqueues item into the queue.
        /// </summary>
        public bool Enqueue(T value)
        {
            Debug.Assert(value != null);

            lock (_syncRoot)
            {
                if (_set.Add(value))
                {
                    _queue.Push(value);
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

                    Debug.Assert(_set.Contains(value));
                    _set.Remove(value);

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
