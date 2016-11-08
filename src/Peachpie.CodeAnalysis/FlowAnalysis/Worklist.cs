using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Queue of work items to do.
    /// </summary>
    [DebuggerDisplay("WorkList<{T}>, Size={_queue.Count}")]
    public class Worklist<T> where T : BoundBlock
    {
        readonly object _syncRoot = new object();

        /// <summary>
        /// Delegate used to process <typeparamref name="T"/>.
        /// </summary>
        public delegate void AnalyzeBlockDelegate(T block);

        /// <summary>
        /// Action performed on bound operations.
        /// </summary>
        readonly List<AnalyzeBlockDelegate> _analyzers = new List<AnalyzeBlockDelegate>();

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

        ///// <summary>
        ///// Adds an analysis driver into the list of analyzers to be performed on bound operations.
        ///// </summary>
        //internal void AddAnalysis(AnalyzeBlockDelegate analyzer)
        //{
        //    _analyzers.Add(analyzer);
        //}

        public Worklist(params AnalyzeBlockDelegate[] analyzers)
        {
            _analyzers.AddRange(analyzers);
        }

        /// <summary>
        /// Adds block to the queue.
        /// </summary>
        public void Enqueue(T block)
        {
            if (block != null)
            {
                _queue.Enqueue(block);
            }
        }

        public bool EnqueueRoutine(IPhpRoutineSymbol routine, T caller, ImmutableArray<BoundExpression> args)
        {
            Contract.ThrowIfNull(routine);

            if (routine.ControlFlowGraph == null)
            {
                // library (sourceless) function
                return false;
            }

            // ensure caller is subscribed to routine's ExitBlock
            ((ExitBlock)routine.ControlFlowGraph.Exit).Subscribe(caller);

            // TODO: check if routine has to be reanalyzed => enqueue routine's StartBlock

            //
            return false;
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
            _analyzers.ForEach(a => a(block));

            //
            return true;
        }
    }
}
