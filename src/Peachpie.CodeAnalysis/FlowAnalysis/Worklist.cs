using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Utilities;
using Peachpie.CodeAnalysis.FlowAnalysis.Graph;
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
        readonly DistinctQueue<T> _queue = new DistinctQueue<T>(new BoundBlock.OrdinalComparer());

        readonly CallGraph _callGraph = new CallGraph();

        /// <summary>
        /// Set of blocks that need to be processed, but the methods they call haven't been processed yet.
        /// </summary>
        readonly ConcurrentDictionary<T, object> _dirtyCallBlocks = new ConcurrentDictionary<T, object>();

        /// <summary>
        /// In the case of updating an existing analysis, a map of the currently analysed routines to their previous return types.
        /// Null in the case of a fresh analysis.
        /// </summary>
        Dictionary<SourceRoutineSymbol, TypeRefMask> _currentRoutinesLastReturnTypes;

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
                _dirtyCallBlocks.TryRemove(block, out _);
                _queue.Enqueue(block);
            }
        }

        public bool EnqueueRoutine(IPhpRoutineSymbol routine, T caller, BoundRoutineCall callExpression)
        {
            Contract.ThrowIfNull(routine);

            if (routine.ControlFlowGraph == null)
            {
                var routine2 = routine is SynthesizedMethodSymbol sr
                    ? sr.ForwardedCall
                    : routine.OriginalDefinition as IPhpRoutineSymbol;

                if (routine2 != null && !ReferenceEquals(routine, routine2))
                {
                    return EnqueueRoutine(routine2, caller, callExpression);
                }

                // library (sourceless) function
                return false;
            }

            var sourceRoutine = (SourceRoutineSymbol)routine;

            if (sourceRoutine.SyntaxReturnType != null)
            {
                // we don't have to wait for return type,
                // nor reanalyse itself when routine analyses
                return false;
            }

            _callGraph.AddEdge(caller.FlowState.Routine, sourceRoutine, new CallSite(caller, callExpression));

            // ensure caller is subscribed to routine's ExitBlock
            ((ExitBlock)routine.ControlFlowGraph.Exit).Subscribe(caller);

            // TODO: check if routine has to be reanalyzed => enqueue routine's StartBlock

            // Return whether the routine exit block will certainly be analysed in the future
            return !sourceRoutine.IsReturnAnalysed;
        }

        public void PingReturnUpdate(ExitBlock updatedExit, T callingBlock)
        {
            var caller = callingBlock.FlowState?.Routine;

            // If the update of the analysis is in progress and the caller is not yet analysed (its FlowState is null due to invalidation) or
            // is not within the currently analysed routines, don't enqueue it
            if (callingBlock.FlowState == null ||
                (caller != null && _currentRoutinesLastReturnTypes != null && !_currentRoutinesLastReturnTypes.ContainsKey(caller)))
            {
                return;
            }

            if (caller == null || _callGraph.GetCalleeEdges(caller).All(edge => edge.Callee.IsReturnAnalysed))
            {
                Enqueue(callingBlock);
            }
            else
            {
                _dirtyCallBlocks.TryAdd(callingBlock, null);
            }
        }

        void Process(T block)
        {
            var list = _analyzers;
            for (int i = 0; i < list.Count; i++)
            {
                list[i](block);
            }

            // block.FlowState.Routine.

            //CompilerLogSource.Log.Count("BoundBlockProcessings");
        }

        /// <summary>
        /// Processes all tasks until the queue is not empty.
        /// </summary>
        public void DoAll(bool concurrent = false)
        {
            // Store the current batch and its count
            var todoBlocks = new T[256];

            // Deque a batch of blocks and analyse them in parallel
            while (true)
            {
                var n = Dequeue(todoBlocks);
                if (n != 0)
                {
                    if (concurrent)
                    {
                        Parallel.For(0, n, i => Process(todoBlocks[i]));
                    }
                    else
                    {
                        for (int i = 0; i < n; i++)
                        {
                            Process(todoBlocks[i]);
                        }
                    }
                }
                else
                {
                    if (_dirtyCallBlocks.IsEmpty)
                    {
                        break;
                    }

                    // Process also the call blocks that weren't analysed due to circular dependencies
                    // TODO: Consider using something more advanced such as cycle detection
                    foreach (var dirty in _dirtyCallBlocks)
                    {
                        Enqueue(dirty.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Re-run the analysis for the specified routines. Repeatively propagate the changes of their return types
        /// to their callers, until there are none left.
        /// </summary>
        /// <remarks>
        /// It is expected that the introduced changes don't change the semantics of the program and hence don't
        /// increase the set of possible return types of the particular routines.
        /// </remarks>
        internal void Update(IEnumerable<SourceRoutineSymbol> updatedRoutines, bool concurrent = false)
        {
            // Initialize the currently re-analysed set of methods with the given ones
            _currentRoutinesLastReturnTypes = new Dictionary<SourceRoutineSymbol, TypeRefMask>();
            foreach (var routine in updatedRoutines)
            {
                _currentRoutinesLastReturnTypes.Add(routine, routine.ResultTypeMask);
            }

            do
            {
                foreach (var kvp in _currentRoutinesLastReturnTypes)
                {
                    kvp.Key.ControlFlowGraph.FlowContext.InvalidateAnalysis();
                    Enqueue((T)kvp.Key.ControlFlowGraph.Start);
                }

                // Re-run the analysis with the invalidated routine flow information
                DoAll(concurrent);

                var lastMethods = _currentRoutinesLastReturnTypes;
                _currentRoutinesLastReturnTypes = new Dictionary<SourceRoutineSymbol, TypeRefMask>();

                // Check the changes of the return types and enlist the callers for the next round
                foreach (var kvp in lastMethods)
                {
                    if (kvp.Key.ResultTypeMask != kvp.Value)
                    {
                        // No other types could have been added, only removed (we're making the overapproximation more precise)
                        Debug.Assert(((kvp.Key.ResultTypeMask & ~TypeRefMask.FlagsMask) & ~kvp.Value) == 0);

                        var callers = _callGraph.GetCallerEdges(kvp.Key)
                            .Select(e => e.Caller)
                            .Where(c => !_currentRoutinesLastReturnTypes.ContainsKey(c));    // These were already reanalysed in this phase

                        foreach (var caller in callers)
                        {
                            _currentRoutinesLastReturnTypes.Add(caller, caller.ResultTypeMask);
                        }
                    }
                }
            } while (_currentRoutinesLastReturnTypes.Count > 0);

            _currentRoutinesLastReturnTypes = null;
        }

        /// <summary>
        /// Fills the given array with dequeued blocks from <see cref="_queue"/>./
        /// </summary>
        int Dequeue(T[] todoBlocks)
        {
            // Helper data structures to enable adding only one block per routine to a batch
            var todoContexts = new HashSet<TypeRefContext>();
            List<T> delayedBlocks = null;

            // Insert the blocks with the highest priority to the batch while having at most one block
            // from each routine, delaying the rest
            int n = 0;
            while (n < todoBlocks.Length && _queue.TryDequeue(out var block)) // TODO: TryDequeue() with a predicate so we won't have to maintain {delayedBlocks}
            {
                var typeCtx = block.FlowState.FlowContext.TypeRefContext;

                if (todoContexts.Add(typeCtx))
                {
                    todoBlocks[n++] = block;
                }
                else
                {
                    delayedBlocks ??= new List<T>();
                    delayedBlocks.Add(block);
                }
            }

            // Return the delayed blocks back to the queue to be deenqueued the next time
            if (delayedBlocks != null)
            {
                foreach (var block in delayedBlocks)
                {
                    _queue.Enqueue(block);
                }
            }

            return n;
        }
    }
}
