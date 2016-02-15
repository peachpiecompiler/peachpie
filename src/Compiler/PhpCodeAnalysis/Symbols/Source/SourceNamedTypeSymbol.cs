using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.Syntax;
using Pchp.Syntax.AST;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class SourceNamedTypeSymbol : NamedTypeSymbol
    {
        readonly TypeDecl _syntax;
        readonly SourceModuleSymbol _sourceModule;

        public SourceNamedTypeSymbol(SourceModuleSymbol module, TypeDecl syntax)
        {
            _syntax = syntax;
            _sourceModule = module;
        }

        internal override IModuleSymbol ContainingModule => _sourceModule;

        public override Symbol ContainingSymbol => _sourceModule;

        public override string Name => _syntax.Name.Value;

        public override TypeKind TypeKind => TypeKind.Class;

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.Public;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override bool IsAbstract => (_syntax.MemberAttributes & PhpMemberAttributes.Abstract) != 0;

        public override bool IsSealed => (_syntax.MemberAttributes & PhpMemberAttributes.Final) != 0;

        public override bool IsStatic => (_syntax.MemberAttributes & PhpMemberAttributes.Static) != 0;

        public override SymbolKind Kind => SymbolKind.NamedType;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override TypeLayout Layout => default(TypeLayout);

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return null;
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray<Symbol>.Empty;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return ImmutableArray<Symbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            yield break;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }
    }
}
