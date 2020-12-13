using Devsense.PHP.Syntax;
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
        public bool HasFlowState => this.Start.FlowState != null;

        /// <summary>
        /// Gets flow analysis context for this CFG.
        /// </summary>
        /// <remarks>CFG has to be analysed prior to getting this property.</remarks>
        public FlowContext FlowContext => this.Start.FlowState?.FlowContext;

        /// <summary>
        /// Gets possible types of a local variable.
        /// </summary>
        /// <remarks>CFG has to be analysed prior to getting this property.</remarks>
        public TypeRefMask GetLocalTypeMask(string varname) => this.FlowContext.GetVarType(new VariableName(varname));

        ///// <summary>
        ///// Gets type of return value within this CFG.
        ///// </summary>
        ///// <remarks>CFG has to be analysed prior to getting this property.</remarks>
        //public TypeRefMask ReturnTypeMask => (this.Exit.FlowState ?? this.Start.FlowState).GetReturnType(); // (this.Exit.FlowState != null) ? this.Exit.FlowState.GetReturnType() : default(TypeRefMask);
    }
}
