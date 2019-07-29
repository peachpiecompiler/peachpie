using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    /// <summary>
    /// Stores certain types of transformations in parallel fashion and performs them serially afterwards.
    /// </summary>
    internal class DelayedTransformations
    {
        /// <summary>
        /// Routines with unreachable declarations.
        /// </summary>
        public ConcurrentBag<SourceRoutineSymbol> UnreachableRoutines { get; } = new ConcurrentBag<SourceRoutineSymbol>();

        /// <summary>
        /// Types with unreachable declarations.
        /// </summary>
        public ConcurrentBag<SourceTypeSymbol> UnreachableTypes { get; } = new ConcurrentBag<SourceTypeSymbol>();

        /// <summary>
        /// Functions that were declared conditionally but analysis marked them as unconditional.
        /// </summary>
        public ConcurrentBag<SourceFunctionSymbol> FunctionsMarkedAsUnconditional { get; } = new ConcurrentBag<SourceFunctionSymbol>();

        public bool Apply()
        {
            bool changed = false;

            foreach (var routine in UnreachableRoutines)
            {
                if (!routine.IsUnreachable)
                {
                    routine.Flags |= RoutineFlags.IsUnreachable;
                    changed = true;
                }
            }

            foreach (var type in UnreachableTypes)
            {
                if (!type.IsMarkedUnreachable)
                {
                    type.IsMarkedUnreachable = true;
                    changed = true;
                }
            }

            foreach (var f in FunctionsMarkedAsUnconditional)
            {
                if (f.IsConditional && !f.IsUnreachable)
                {
                    f.IsConditional = false;
                    changed = true;
                }
            }

            //
            return changed;
        }
    }
}
