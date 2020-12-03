using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceRoutineSymbol
    {
        /// <summary>
        /// Routine <see cref="TypeRefContext"/> instance.
        /// </summary>
        internal TypeRefContext TypeRefContext
        {
            get
            {
                if (_typeCtx == null)
                {
                    Interlocked.CompareExchange(ref _typeCtx, CreateTypeRefContext(), null);
                }
                return _typeCtx;
            }
        }

        TypeRefContext _typeCtx;

        /// <summary>
        /// Routine flags lazily collected during code analysis.
        /// </summary>
        internal RoutineFlags Flags { get; set; }

        /// <summary>
        /// Gets so far type-analysed routine result type.
        /// </summary>
        internal TypeRefMask ResultTypeMask
        {
            get
            {
                var cfg = this.ControlFlowGraph;
                return (cfg != null)
                    ? cfg.FlowContext.ReturnType    // might be void if not analysed yet
                    : TypeRefMask.AnyType;
            }
        }

        /// <summary>
        /// Marks whether the exit block or any block with a return statement was already processed at least once.
        /// </summary>
        internal bool IsReturnAnalysed
        {
            get => (Flags & RoutineFlags.IsReturnAnalysed) != 0;
            set
            {
                if (value)
                    Flags |= RoutineFlags.IsReturnAnalysed;
                else
                    Flags &= ~RoutineFlags.IsReturnAnalysed;
            }
        }
    }
}
