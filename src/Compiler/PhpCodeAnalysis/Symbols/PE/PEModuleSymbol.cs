using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed partial class PEModuleSymbol : ModuleSymbol
    {
        /// <summary>
        /// Owning AssemblySymbol. This can be a PEAssemblySymbol or a SourceAssemblySymbol.
        /// </summary>
        readonly PEAssemblySymbol _assembly;

        private readonly int _ordinal;

        /// <summary>
        /// A Module object providing metadata.
        /// </summary>
        readonly PEModule _module;

        ///// <summary>
        ///// Global namespace.
        ///// </summary>
        readonly PENamespaceSymbol _namespace;
        
        public PEModuleSymbol(PEAssemblySymbol assembly, PEModule module, MetadataImportOptions importOptions, int ordinal)
        {
            _assembly = assembly;
            _module = module;
            _ordinal = ordinal;
            this.ImportOptions = importOptions;
            _namespace = new PEGlobalNamespaceSymbol(this);
        }

        internal readonly MetadataImportOptions ImportOptions;

        public PEModule Module => _module;

        public override INamespaceSymbol GlobalNamespace => _namespace;

        public override string Name => _module.Name;

        //private static EntityHandle Token
        //{
        //    get
        //    {
        //        return EntityHandle.ModuleDefinition;
        //    }
        //}

        public override IAssemblySymbol ContainingAssembly => _assembly;

        public override Symbol ContainingSymbol => _assembly;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return ObsoleteAttributeData.Uninitialized; // throw new NotImplementedException();
            }
        }
    }
}
