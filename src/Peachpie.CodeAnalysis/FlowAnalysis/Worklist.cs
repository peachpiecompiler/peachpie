using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
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
        readonly DistinctQueue<T> _queue = new DistinctQueue<T>(new BoundBlockComparer());

        readonly CallGraph _callGraph = new CallGraph();

        /// <summary>
        /// List of blocks that need to be processed, but the methods they call haven't been processed yet.
        /// </summary>
        readonly ConcurrentDictionary<T, object> _dirtyCallBlocks = new ConcurrentDictionary<T, object>();

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
        }

        /// <summary>
        /// Processes all tasks until the queue is not empty.
        /// </summary>
        public void DoAll(bool concurrent = false)
        {
            // deque batch of blocks and analyse them in parallel
            var todo = new T[256];
            int n;
            
            while (true)
            {
                n = Dequeue(todo);
                if (n == 0)
                {
                    if (_dirtyCallBlocks.IsEmpty)
                    {
                        break;
                    }
                    else
                    {
                        // Process also the call blocks that weren't analysed due to circular dependencies
                        // TODO: Consider using something more advanced such as cycle detection
                        _dirtyCallBlocks.ForEach(kvp => Enqueue(kvp.Key));
                        continue;
                    }
                }

                if (concurrent)
                {
                    Parallel.For(0, n, (i) => Process(todo[i]));
                }
                else
                {
                    for (int i = 0; i < n; i++)
                    {
                        Process(todo[i]);
                    }
                }
            }
        }

        int Dequeue(T[] blocks)
        {
            int n = 0;
            while (n < blocks.Length && _queue.TryDequeue(out T block))
            {
                blocks[n++] = block;
            }

            return n;
        }

        sealed class BoundBlockComparer : IComparer<BoundBlock>
        {
            int IComparer<BoundBlock>.Compare(BoundBlock x, BoundBlock y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                int ordRes = x.Ordinal - y.Ordinal;
                if (ordRes != 0)
                {
                    return ordRes;
                }

                // TODO: Skip the following part (returning 0) when a heap with possible duplicate key values is used

                // We must ensure that two blocks from two different routines are not considered equal
                int hashRes = x.GetHashCode() - y.GetHashCode();
                if (hashRes != 0)
                {
                    return hashRes;
                }

                Debug.Assert(x.FlowState != null && y.FlowState != null);
                Debug.Assert(x.FlowState.FlowContext != y.FlowState.FlowContext);
                int stateHashRes = x.FlowState.FlowContext.GetHashCode() - y.FlowState.FlowContext.GetHashCode();
                if (stateHashRes != 0)
                {
                    return stateHashRes;
                }

                throw new NotImplementedException();
            }
        }
    }
}
