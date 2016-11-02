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

        readonly HashSet<T> _set = new HashSet<T>();
        readonly Queue<T> _queue = new Queue<T>();

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
                if (_set.Add(value))
                {
                    _queue.Enqueue(value);
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
                    value = _queue.Dequeue();
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
