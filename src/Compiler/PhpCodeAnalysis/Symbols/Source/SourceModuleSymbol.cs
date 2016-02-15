using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class SourceModuleSymbol : Symbol, IModuleSymbol
    {
        readonly SourceAssemblySymbol _sourceAssembly;
        readonly string _name;

        public SourceModuleSymbol(SourceAssemblySymbol sourceAssembly, string name)
        {
            _sourceAssembly = sourceAssembly;
            _name = name;
        }

        public override string Name => _name;

        public override Symbol ContainingSymbol => _sourceAssembly;

        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public INamespaceSymbol GlobalNamespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsExtern
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsOverride => false;

        public override bool IsSealed => false;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override SymbolKind Kind => SymbolKind.NetModule;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ImmutableArray<AssemblyIdentity> ReferencedAssemblies
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ImmutableArray<IAssemblySymbol> ReferencedAssemblySymbols
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

        public ModuleMetadata GetMetadata()
        {
            throw new NotImplementedException();
        }

        public INamespaceSymbol GetModuleNamespace(INamespaceSymbol namespaceSymbol)
        {
            throw new NotImplementedException();
        }
    }
}
