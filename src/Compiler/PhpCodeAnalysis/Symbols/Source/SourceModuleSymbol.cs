using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class SourceModuleSymbol : ModuleSymbol, IModuleSymbol
    {
        readonly SourceAssemblySymbol _sourceAssembly;
        readonly string _name;
        readonly SourceDeclarations _tables;

        /// <summary>
        /// Tables of all source symbols to be compiled within the source module.
        /// </summary>
        public SourceDeclarations SymbolTables => _tables;

        public SourceModuleSymbol(SourceAssemblySymbol sourceAssembly, SourceDeclarations tables, string name)
        {
            _sourceAssembly = sourceAssembly;
            _name = name;
            _tables = tables;
        }

        public override string Name => _name;

        public override Symbol ContainingSymbol => _sourceAssembly;

        internal SourceAssemblySymbol SourceAssemblySymbol => _sourceAssembly;

        public override IAssemblySymbol ContainingAssembly => _sourceAssembly;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override PhpCompilation DeclaringCompilation => _sourceAssembly.DeclaringCompilation;

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
