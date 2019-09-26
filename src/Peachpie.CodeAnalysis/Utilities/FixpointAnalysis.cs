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
        private struct BlockAnalysis
        {
            public bool InWorklist;
            public TState Before;
            public TState After;
        }

        TContext _context;
        private readonly BlockAnalysis[] _results;    // TODO: Make ordinals continuous to save memory
        private readonly PriorityQueue<BoundBlock> _worklist;

        public FixPointAnalysis(TContext context, SourceRoutineSymbol routine)
        {
            _context = context;

            var cfg = routine.ControlFlowGraph;
            _results = new BlockAnalysis[cfg.Exit.Ordinal + 1];
            _worklist = new PriorityQueue<BoundBlock>(new BoundBlock.OrdinalComparer());

            var startBlock = cfg.Start;
            _worklist.Push(startBlock);

            _results[startBlock.Ordinal] = new BlockAnalysis { InWorklist = true, Before = _context.GetInitialState(), After = default };
        }

        public void Run()
        {
            while (_worklist.Count > 0)
            {
                var block = _worklist.Top;
                _worklist.Pop();

                ref var analysis = ref _results[block.Ordinal];
                analysis.InWorklist = false;

                var after = _context.ProcessBlock(block, analysis.Before);
                if (!_context.StatesEqual(analysis.After, after))
                {
                    analysis.After = after;
                    foreach (var nextBlock in block.NextEdge?.Targets ?? Enumerable.Empty<BoundBlock>())
                    {
                        ref var nextAnalysis = ref _results[nextBlock.Ordinal];

                        var merged = _context.MergeStates(after, nextAnalysis.Before);

                        if (!_context.StatesEqual(nextAnalysis.Before, merged))
                        {
                            nextAnalysis.Before = merged;

                            if (!nextAnalysis.InWorklist)
                                _worklist.Push(nextBlock);
                        }
                    }
                }
            }
        }

        public TState GetResult(BoundBlock block)
        {
            if (_results[block.Ordinal] is var value)
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
