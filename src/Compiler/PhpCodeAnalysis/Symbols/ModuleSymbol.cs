using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract partial class ModuleSymbol : Symbol, IModuleSymbol
    {
        internal override IModuleSymbol ContainingModule => null;

        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
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

        public virtual INamespaceSymbol GlobalNamespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ImmutableArray<AssemblyIdentity> ReferencedAssemblies
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ImmutableArray<IAssemblySymbol> ReferencedAssemblySymbols
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ModuleMetadata GetMetadata()
        {
            throw new NotImplementedException();
        }

        public virtual INamespaceSymbol GetModuleNamespace(INamespaceSymbol namespaceSymbol)
        {
            throw new NotImplementedException();
        }
    }
}
