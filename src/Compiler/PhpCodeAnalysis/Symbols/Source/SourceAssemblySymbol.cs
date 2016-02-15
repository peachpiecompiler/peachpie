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
    internal sealed class SourceAssemblySymbol : Symbol, IAssemblySymbol
    {
        public override Symbol ContainingSymbol
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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

        public AssemblyIdentity Identity
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsExtern
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsInteractive
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsOverride
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsSealed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsStatic
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsVirtual
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool MightContainExtensionMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<IModuleSymbol> Modules
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ICollection<string> NamespaceNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ICollection<string> TypeNames
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

        public AssemblyMetadata GetMetadata()
        {
            throw new NotImplementedException();
        }

        public INamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            throw new NotImplementedException();
        }

        public bool GivesAccessTo(IAssemblySymbol toAssembly)
        {
            throw new NotImplementedException();
        }

        public INamedTypeSymbol ResolveForwardedType(string fullyQualifiedMetadataName)
        {
            throw new NotImplementedException();
        }
    }
}
