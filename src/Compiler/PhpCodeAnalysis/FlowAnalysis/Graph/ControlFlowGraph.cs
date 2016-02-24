using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    partial class ControlFlowGraph
    {
        /// <summary>
        /// Gets flow analysis context for this CFG.
        /// </summary>
        /// <remarks>CFG has to be analysed prior to getting this property.</remarks>
        public FlowContext FlowContext => this.Start.FlowState.FlowContext;

        /// <summary>
        /// Gets local variable type.
        /// </summary>
        /// <remarks>CFG has to be analysed prior to getting this property.</remarks>
        public TypeRefMask GetLocalTypeMask(ILocalSymbol local) => this.FlowContext.GetVarType(local.Name);

        /// <summary>
        /// Gets type of return value within this CFG.
        /// </summary>
        /// <remarks>CFG has to be analysed prior to getting this property.</remarks>
        public TypeRefMask GetReturnTypeMask => this.Exit.FlowState.GetReturnType();
    }
}
