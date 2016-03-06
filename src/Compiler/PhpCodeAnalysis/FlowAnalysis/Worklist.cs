using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
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
    internal class Worklist<T> where T : BoundBlock
    {
        readonly object _syncRoot = new object();

        /// <summary>
        /// Action performed on bound operations.
        /// </summary>
        readonly List<GraphVisitor> _analyzers = new List<GraphVisitor>();  // TODO: Analysis instead of GraphVisitor

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

        /// <summary>
        /// Adds an analysis driver into the list of analyzers to be performed on bound operations.
        /// </summary>
        internal void AddAnalysis(GraphVisitor analyzer)
        {
            _analyzers.Add(analyzer);
        }

        public Worklist()
        {
            
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
            _analyzers.ForEach(visitor => visitor.VisitCFGBlock(block));

            //
            return true;
        }
    }
}
