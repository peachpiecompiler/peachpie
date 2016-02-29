using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.Syntax.AST;

namespace Pchp.CodeAnalysis.Symbols
{
    internal partial class SourceNamespaceSymbol : NamespaceSymbol
    {
        readonly SourceModuleSymbol _sourceModule;
        readonly string _name;

        public SourceNamespaceSymbol(SourceModuleSymbol module, NamespaceDecl ns)
        {
            _sourceModule = module;
            _name = ns.QualifiedName.ClrName();
        }

        internal override PhpCompilation DeclaringCompilation => _sourceModule.DeclaringCompilation;

        public override PhpCompilation ContainingCompilation => _sourceModule.DeclaringCompilation;

        public override void Accept(SymbolVisitor visitor) => visitor.VisitNamespace(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitNamespace(this);

        public override AssemblySymbol ContainingAssembly => _sourceModule.ContainingAssembly;

        internal override IModuleSymbol ContainingModule => _sourceModule;

        public override Symbol ContainingSymbol => _sourceModule;

        public override string Name => _name;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
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

        public override ImmutableArray<Symbol> GetMembers()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            throw new NotImplementedException();
        }
    }
}
