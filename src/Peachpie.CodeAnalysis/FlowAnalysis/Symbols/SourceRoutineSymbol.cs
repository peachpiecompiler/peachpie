using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// 
        /// </summary>
        internal TypeRefMask ResultTypeMask => this.ControlFlowGraph.FlowContext.ReturnType;
    }
}
