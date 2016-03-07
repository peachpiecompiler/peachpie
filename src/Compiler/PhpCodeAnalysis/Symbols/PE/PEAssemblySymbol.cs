using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents an assembly imported from a PE.
    /// </summary>
    internal sealed class PEAssemblySymbol : NonMissingAssemblySymbol
    {
        /// <summary>
        /// An Assembly object providing metadata for the assembly.
        /// </summary>
        readonly PEAssembly _assembly;

        /// <summary>
        /// The list of contained PEModuleSymbol objects.
        /// The list doesn't use type ReadOnlyCollection(Of PEModuleSymbol) so that we
        /// can return it from Modules property as is.
        /// </summary>
        readonly ImmutableArray<ModuleSymbol> _modules;

        /// <summary>
        /// Assembly is /l-ed by compilation that is using it as a reference.
        /// </summary>
        readonly bool _isLinked;

        /// <summary>
        /// Whether this assembly is the COR library.
        /// </summary>
        readonly SpecialAssembly _specialAssembly;

        /// <summary>
        /// A DocumentationProvider that provides XML documentation comments for this assembly.
        /// </summary>
        readonly DocumentationProvider _documentationProvider;
        
        internal PEAssembly PEAssembly => _assembly;

        public override AssemblyIdentity Identity => _assembly.Identity;

        public override ImmutableArray<ModuleSymbol> Modules => _modules;

        public override INamespaceSymbol GlobalNamespace => PrimaryModule.GlobalNamespace;

        internal PEModuleSymbol PrimaryModule => (PEModuleSymbol)_modules[0];

        internal override PhpCompilation DeclaringCompilation => null;

        public override AssemblyMetadata GetMetadata() => _assembly.GetNonDisposableMetadata();

        internal override ImmutableArray<byte> PublicKey => this.Identity.PublicKey;

        public override bool IsCorLibrary => _specialAssembly == SpecialAssembly.CorLibrary;

        public override bool IsPchpCorLibrary => _specialAssembly == SpecialAssembly.PchpCorLibrary;

        internal PEAssemblySymbol(PEAssembly assembly, DocumentationProvider documentationProvider, bool isLinked, MetadataImportOptions importOptions)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(documentationProvider != null);

            _assembly = assembly;
            _documentationProvider = documentationProvider;

            var modules = new ModuleSymbol[assembly.Modules.Length];

            for (int i = 0; i < assembly.Modules.Length; i++)
            {
                modules[i] = new PEModuleSymbol(this, assembly.Modules[i], importOptions, i);
            }

            _modules = modules.AsImmutableOrNull();
            _isLinked = isLinked;

            if (assembly.AssemblyReferences.Length == 0 && assembly.DeclaresTheObjectClass)
                _specialAssembly = SpecialAssembly.CorLibrary;
            else if (assembly.Identity.Name == "pchpcor")
                _specialAssembly = SpecialAssembly.PchpCorLibrary;
        }

        internal static PEAssemblySymbol Create(PortableExecutableReference reference)
        {
            var data = (AssemblyMetadata)reference.GetMetadata();
            //var data = AssemblyMetadata.CreateFromFile(reference.FilePath);
            var ass = data.GetAssembly();

            return new PEAssemblySymbol(ass, DocumentationProvider.Default, true, MetadataImportOptions.Public);
        }

        /// <summary>
        /// Look up the assembly to which the given metadata type is forwarded.
        /// </summary>
        /// <param name="emittedName"></param>
        /// <returns>
        /// The assembly to which the given type is forwarded or null, if there isn't one.
        /// </returns>
        /// <remarks>
        /// The returned assembly may also forward the type.
        /// </remarks>
        internal AssemblySymbol LookupAssemblyForForwardedMetadataType(ref MetadataTypeName emittedName)
        {
            // Look in the type forwarders of the primary module of this assembly, clr does not honor type forwarder
            // in non-primary modules.

            // Examine the type forwarders, but only from the primary module.
            return this.PrimaryModule.GetAssemblyForForwardedType(ref emittedName);
        }

        internal override NamedTypeSymbol TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies)
        {
            // Check if it is a forwarded type.
            var forwardedToAssembly = LookupAssemblyForForwardedMetadataType(ref emittedName);
            if ((object)forwardedToAssembly != null)
            {
                // Don't bother to check the forwarded-to assembly if we've already seen it.
                if (visitedAssemblies != null && visitedAssemblies.Contains(forwardedToAssembly))
                {
                    return CreateCycleInTypeForwarderErrorTypeSymbol(ref emittedName);
                }
                else
                {
                    visitedAssemblies = new ConsList<AssemblySymbol>(this, visitedAssemblies ?? ConsList<AssemblySymbol>.Empty);
                    return forwardedToAssembly.LookupTopLevelMetadataTypeWithCycleDetection(ref emittedName, visitedAssemblies, digThroughForwardedTypes: true);
                }
            }

            return null;
        }
    }
}
