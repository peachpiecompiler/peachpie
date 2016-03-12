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
        #region CoreTypes, CoreMethods, Merging

        /// <summary>
        /// Well known types associated with this compilation.
        /// </summary>
        public CoreTypes CoreTypes => _coreTypes;
        readonly CoreTypes _coreTypes;

        /// <summary>
        /// Well known methods associated with this compilation.
        /// </summary>
        public CoreMethods CoreMethods => _coreMethods;
        readonly CoreMethods _coreMethods;

        public CoreType Merge(CoreType first, CoreType second)
        {
            Contract.ThrowIfNull(first);
            Contract.ThrowIfNull(second);

            if (first == second)
                return first;

            //return CoreTypes.obj
            throw new NotImplementedException();
        }

        #endregion

        internal NamedTypeSymbol GetWellKnownType(WellKnownType id)
        {
            var name = id.GetMetadataName();
            if (name != null && this.CorLibrary != null)
            {
                return this.CorLibrary.GetTypeByMetadataName(name);
            }

            return null;
        }

        protected override INamedTypeSymbol CommonGetSpecialType(SpecialType specialType)
        {
            return this.CorLibrary.GetSpecialType(specialType);
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
                    .Select(a => a.GetTypeByMetadataName(metadataName))
                    .Where(a => a != null)
                    .FirstOrDefault();
        }

        /// <summary>
        /// Resolves <see cref="TypeSymbol"/> best fitting given type mask.
        /// </summary>
        internal NamedTypeSymbol GetTypeFromTypeRef(TypeRefContext typeCtx, TypeRefMask typeMask)
        {
            if (!typeMask.IsAnyType)
            {
                if (typeMask.IsRef)
                {
                    // return CoreTypes.PhpAlias;
                    throw new NotImplementedException();
                }

                var types = typeCtx.GetTypes(typeMask);

                // TODO: determine best fitting CLR type based on defined PHP types hierarchy
            }

            //
            return CoreTypes.Object;
        }

        /// <summary>
        /// Resolves <see cref="INamedTypeSymbol"/> best fitting given type mask.
        /// </summary>
        internal NamedTypeSymbol GetTypeFromTypeRef(SourceRoutineSymbol routine, TypeRefMask typeMask)
        {
            if (routine.ControlFlowGraph.HasFlowState)
            {
                var ctx = routine.ControlFlowGraph.FlowContext;
                return this.GetTypeFromTypeRef(ctx.TypeRefContext, typeMask);
            }

            throw new InvalidOperationException();
        }
    }
}
