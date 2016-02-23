using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Queue of work items to do.
    /// </summary>
    internal class Worklist<T>
    {
        readonly object _syncRoot = new object();

        /// <summary>
        /// Action performed on blocks.
        /// </summary>
        readonly Action<T> _analyzer;

        //public event EventHandler MethodDone;
        
        ///// <summary>
        ///// Set of blocks being analyzed.
        ///// Used for recursion prevention.
        ///// </summary>
        //readonly HashSet<T> _pending;

        /// <summary>
        /// List of blocks to be processed.
        /// </summary>
        readonly DistinctQueue<T> _queue = new DistinctQueue<T>();

        public Worklist(Action<T> analyzer)
        {
            Contract.ThrowIfNull(analyzer);

            _analyzer = analyzer;
        }

        /// <summary>
        /// Adds block to the queue.
        /// </summary>
        public void Enqueue(T block)
        {
            _queue.Enqueue(block);
        }

        /// <summary>
        /// Processes all tasks until the queue is not empty.
        /// </summary>
        public void DoAll()
        {
            for (; DoNext();) ;
        }

        /// <summary>
        /// Pop next item from the queue and process it.
        /// </summary>
        /// <returns><c>true</c> if there was an item, otherwise <c>false</c>.</returns>
        public bool DoNext()
        {
            T block;
            if (!_queue.TryDequeue(out block))
                return false;

            //
            _analyzer(block);

            //
            return true;
        }
    }
}
