using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class SourceRoutineSymbol
    {
        /// <summary>
        /// Routine <see cref="TypeRefContext"/> instance.
        /// </summary>
        internal TypeRefContext TypeRefContext => _typeCtx ?? (_typeCtx = CreateTypeRefContext());

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
    }
}
