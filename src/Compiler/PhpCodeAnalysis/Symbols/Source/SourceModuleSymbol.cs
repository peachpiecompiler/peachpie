using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class SourceModuleSymbol : ModuleSymbol, IModuleSymbol
    {
        readonly SourceAssemblySymbol _sourceAssembly;
        readonly string _name;
        readonly SourceDeclarations _tables;
        readonly NamespaceSymbol _ns;

        /// <summary>
        /// Tables of all source symbols to be compiled within the source module.
        /// </summary>
        public SourceDeclarations SymbolTables => _tables;

        public SourceModuleSymbol(SourceAssemblySymbol sourceAssembly, SourceDeclarations tables, string name)
        {
            _sourceAssembly = sourceAssembly;
            _name = name;
            _tables = tables;
            _ns = new SourceGlobalNamespaceSymbol(this);
        }

        public override string Name => _name;

        public override Symbol ContainingSymbol => _sourceAssembly;

        public override NamespaceSymbol GlobalNamespace => _ns;

        internal SourceAssemblySymbol SourceAssemblySymbol => _sourceAssembly;

        public override AssemblySymbol ContainingAssembly => _sourceAssembly;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override PhpCompilation DeclaringCompilation => _sourceAssembly.DeclaringCompilation;

        /// <summary>
        /// Lookup a top level type referenced from metadata, names should be
        /// compared case-sensitively.
        /// </summary>
        /// <param name="emittedName">
        /// Full type name, possibly with generic name mangling.
        /// </param>
        /// <returns>
        /// Symbol for the type, or MissingMetadataSymbol if the type isn't found.
        /// </returns>
        /// <remarks></remarks>
        internal sealed override NamedTypeSymbol LookupTopLevelMetadataType(ref MetadataTypeName emittedName)
        {
            NamedTypeSymbol result;
            NamespaceSymbol scope = this.GlobalNamespace; //.LookupNestedNamespace(emittedName.NamespaceSegments);

            if ((object)scope == null)
            {
                // We failed to locate the namespace
                throw new NotImplementedException();
                //result = new MissingMetadataTypeSymbol.TopLevel(this, ref emittedName);
            }
            else
            {
                result = scope.LookupMetadataType(ref emittedName);
            }

            Debug.Assert((object)result != null);
            return result;
        }
    }
}
