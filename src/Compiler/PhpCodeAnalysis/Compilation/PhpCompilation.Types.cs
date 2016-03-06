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
        internal NamedTypeSymbol GetWellKnownType(WellKnownType id)
        {
            var name = id.GetMetadataName();
            if (name != null && this.CorLibrary != null)
            {
                return (NamedTypeSymbol)this.CorLibrary.GlobalNamespace.GetTypeMembers(name).FirstOrDefault();
            }

            return null;
        }

        protected override INamedTypeSymbol CommonGetSpecialType(SpecialType specialType)
        {
            var name = SpecialTypes.GetMetadataName(specialType);
            if (name != null && this.CorLibrary != null)
            {
                return this.CorLibrary.GlobalNamespace.GetTypeMembers(name).FirstOrDefault();
            }

            return null;
        }

        IEnumerable<IAssemblySymbol> ProbingAssemblies
        {
            get
            {
                foreach (var pair in CommonGetBoundReferenceManager().GetReferencedAssemblies())
                    yield return pair.Value;

                yield return this.SourceAssembly;
            }
        }

        protected override INamedTypeSymbol CommonGetTypeByMetadataName(string metadataName)
        {
            return ProbingAssemblies
                    .SelectMany(a => a.GlobalNamespace.GetTypeMembers(metadataName))
                    .FirstOrDefault();
        }

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
