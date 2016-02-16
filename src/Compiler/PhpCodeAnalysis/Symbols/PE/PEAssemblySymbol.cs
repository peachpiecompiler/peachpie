using Microsoft.CodeAnalysis;
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
    internal sealed class PEAssemblySymbol : AssemblySymbol
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
        readonly ImmutableArray<IModuleSymbol> _modules;

        /// <summary>
        /// Assembly is /l-ed by compilation that is using it as a reference.
        /// </summary>
        readonly bool _isLinked;

        /// <summary>
        /// A DocumentationProvider that provides XML documentation comments for this assembly.
        /// </summary>
        readonly DocumentationProvider _documentationProvider;

        internal PEAssembly PEAssembly => _assembly;

        public override AssemblyIdentity Identity => _assembly.Identity;

        public override ImmutableArray<IModuleSymbol> Modules => _modules;

        //internal PEModuleSymbol PrimaryModule => (PEModuleSymbol)_modules[0];

        internal override PhpCompilation DeclaringCompilation => null;

        public override AssemblyMetadata GetMetadata() => _assembly.GetNonDisposableMetadata();

        internal override ImmutableArray<byte> PublicKey => this.Identity.PublicKey;

        internal PEAssemblySymbol(PEAssembly assembly, DocumentationProvider documentationProvider, bool isLinked, MetadataImportOptions importOptions)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(documentationProvider != null);

            _assembly = assembly;
            _documentationProvider = documentationProvider;

            var modules = new IModuleSymbol[assembly.Modules.Length];

            for (int i = 0; i < assembly.Modules.Length; i++)
            {
                modules[i] = new PEModuleSymbol(this, assembly.Modules[i], importOptions, i);
            }

            _modules = modules.AsImmutableOrNull();
            _isLinked = isLinked;
        }

        internal static PEAssemblySymbol CreateFromFile(string path)
        {
            var data = AssemblyMetadata.CreateFromFile(path);
            var ass = data.GetAssembly();

            return new PEAssemblySymbol(ass, DocumentationProvider.Default, true, MetadataImportOptions.Public);
        }
    }
}
