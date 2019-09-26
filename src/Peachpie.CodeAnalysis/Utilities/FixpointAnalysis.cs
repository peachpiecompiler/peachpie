using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Utilities;

namespace Peachpie.CodeAnalysis.Utilities
{
    /// <summary>
    /// Interface to define custom intraprocedural fixpoint analyses.
    /// </summary>
    /// <typeparam name="TState">Flow state to propagate throughout the graph.</typeparam>
    internal interface IFixPointAnalysisContext<TState>
    {
        TState GetInitialState();

        TState ProcessBlock(BoundBlock block, TState state);

        TState MergeStates(TState x, TState y);

        bool StatesEqual(TState x, TState y);
    }

    /// <summary>
    /// Class to enable a simple intraprocedural analysis.
    /// </summary>
    /// <typeparam name="TContext">The logic of the particular analysis.</typeparam>
    /// <typeparam name="TState">Flow state to propagate throughout the graph.</typeparam>
    internal class FixPointAnalysis<TContext, TState>
        where TContext : IFixPointAnalysisContext<TState>
    {
        private class BlockAnalysis
        {
            public TState Before;
            public TState After;
        }

        TContext _context;
        private readonly Dictionary<BoundBlock, BlockAnalysis> _results;    // TODO: Consider making ordinals continuous and using array
        private readonly DistinctQueue<BoundBlock> _worklist;               // TODO: Use a simple PriorityQueue (mark insertion in BlockAnalysis?)

        public FixPointAnalysis(TContext context, SourceRoutineSymbol routine)
        {
            _context = context;

            _results = new Dictionary<BoundBlock, BlockAnalysis>();
            _worklist = new DistinctQueue<BoundBlock>(new BoundBlock.OrdinalComparer());

            var startBlock = routine.ControlFlowGraph.Start;
            var startAnalysis = new BlockAnalysis { Before = _context.GetInitialState(), After = default };
            _results.Add(startBlock, startAnalysis);
            _worklist.Enqueue(startBlock);
        }

        public void Run()
        {
            while (_worklist.TryDequeue(out var block))
            {
                var analysis = _results[block];
                var after = _context.ProcessBlock(block, analysis.Before);
                if (!_context.StatesEqual(analysis.After, after))
                {
                    analysis.After = after;
                    foreach (var nextBlock in block.NextEdge?.Targets ?? Enumerable.Empty<BoundBlock>())
                    {
                        var nextAnalysis = EnsureResult(nextBlock);

                        var merged = _context.MergeStates(after, nextAnalysis.Before);

                        if (!_context.StatesEqual(nextAnalysis.Before, merged))
                        {
                            nextAnalysis.Before = merged;
                            _worklist.Enqueue(nextBlock);
                        }
                    }
                }
            }
        }

        private BlockAnalysis EnsureResult(BoundBlock block)
        {
            if (_results.TryGetValue(block, out var result))
            {
                return result;
            }
            else
            {
                result = new BlockAnalysis();
                _results[block] = result;
                return result;
            }
        }

        public TState GetResult(BoundBlock block)
        {
            if (_results.TryGetValue(block, out var value))
            {
                return value.After;
            }
            else
            {
                return default;
            }
        }
    }
}
