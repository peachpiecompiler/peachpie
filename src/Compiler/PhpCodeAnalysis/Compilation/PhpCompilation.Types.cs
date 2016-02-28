using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis
{
    partial class PhpCompilation
    {
        /// <summary>
        /// Resolves <see cref="INamedTypeSymbol"/> best fitting given type mask.
        /// </summary>
        internal INamedTypeSymbol GetTypeFromTypeRef(TypeRefContext typeCtx, TypeRefMask typeMask, bool isRef)
        {
            // TODO: return { namedtype, includes subclasses (for vcall) }
            // TODO: determine best fitting CLR type
            return this.GetSpecialType(SpecialType.System_Object);
        }

        /// <summary>
        /// Resolves <see cref="INamedTypeSymbol"/> best fitting given type mask.
        /// </summary>
        internal INamedTypeSymbol GetTypeFromTypeRef(SourceRoutineSymbol routine, TypeRefMask typeMask, bool isRef)
        {
            if (routine.ControlFlowGraph.HasFlowState)
            {
                var ctx = routine.ControlFlowGraph.FlowContext;
                return this.GetTypeFromTypeRef(ctx.TypeRefContext, typeMask, false);
            }

            throw new InvalidOperationException();
        }
    }
}
