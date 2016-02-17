using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;

namespace Pchp.CodeAnalysis.Symbols
{
    internal class AssemblySymbol : Symbol, IAssemblySymbol
    {
        public override Symbol ContainingSymbol => null;

        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual INamespaceSymbol GlobalNamespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual AssemblyIdentity Identity { get { throw new NotImplementedException(); } }

        /// <summary>
        /// Gets COR library assembly symbol. Valid for source assembly.
        /// </summary>
        public virtual AssemblySymbol CorLibrary { get { throw new InvalidOperationException(); } }

        public virtual bool IsCorLibrary => false;

        public override bool IsAbstract => false;

        public override bool IsExtern => false;

        public virtual bool IsInteractive => false;

        public override bool IsOverride => false;

        public override bool IsSealed => false;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override SymbolKind Kind => SymbolKind.Assembly;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool MightContainExtensionMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ImmutableArray<IModuleSymbol> Modules
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IEnumerable<IModuleSymbol> IAssemblySymbol.Modules
        {
            get
            {
                return this.Modules;
            }
        }

        public virtual ICollection<string> NamespaceNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ICollection<string> TypeNames
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
                throw new NotImplementedException();
            }
        }

        internal virtual ImmutableArray<byte> PublicKey { get { throw new NotSupportedException(); } }

        public virtual AssemblyMetadata GetMetadata()
        {
            throw new NotImplementedException();
        }

        public virtual INamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            throw new NotImplementedException();
        }

        public virtual bool GivesAccessTo(IAssemblySymbol toAssembly)
        {
            throw new NotImplementedException();
        }

        public INamedTypeSymbol ResolveForwardedType(string fullyQualifiedMetadataName)
        {
            throw new NotImplementedException();
        }
    }
}
