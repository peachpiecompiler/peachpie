using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.Symbols
{
    internal partial class SourceNamespaceSymbol : NamespaceSymbol
    {
        readonly SourceModuleSymbol _sourceModule;
        readonly string _name;

        public SourceNamespaceSymbol(SourceModuleSymbol module, NamespaceDecl ns)
        {
            _sourceModule = module;
            _name = ns.QualifiedName.QualifiedName.ClrName();
        }

        internal override PhpCompilation DeclaringCompilation => _sourceModule.DeclaringCompilation;

        public override PhpCompilation ContainingCompilation => _sourceModule.DeclaringCompilation;

        public override void Accept(SymbolVisitor visitor) => visitor.VisitNamespace(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitNamespace(this);

        public override AssemblySymbol ContainingAssembly => _sourceModule.ContainingAssembly;

        internal override ModuleSymbol ContainingModule => _sourceModule;

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

        public override ImmutableArray<Symbol> GetMembersByPhpName(string name)
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

    internal class SourceGlobalNamespaceSymbol : NamespaceSymbol
    {
        readonly SourceModuleSymbol _sourceModule;

        public SourceGlobalNamespaceSymbol(SourceModuleSymbol module)
        {
            _sourceModule = module;
        }

        internal override PhpCompilation DeclaringCompilation => _sourceModule.DeclaringCompilation;

        public override PhpCompilation ContainingCompilation => _sourceModule.DeclaringCompilation;

        public override void Accept(SymbolVisitor visitor) => visitor.VisitNamespace(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitNamespace(this);

        public override AssemblySymbol ContainingAssembly => _sourceModule.ContainingAssembly;

        internal override ModuleSymbol ContainingModule => _sourceModule;

        public override Symbol ContainingSymbol => _sourceModule;

        public override string Name => string.Empty;

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
            //var table = _sourceModule.SymbolTables;
            //return table.GetFunctions().Cast<Symbol>().Concat(table.GetTypes()).AsImmutable();
            throw new NotImplementedException();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Symbol> GetMembersByPhpName(string name)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return _sourceModule.SymbolCollection.GetTypes().Cast<NamedTypeSymbol>().AsImmutable();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            var x = _sourceModule.SymbolCollection.GetType(NameUtils.MakeQualifiedName(name, true));
            if (x != null)
            {
                if (x.IsErrorType())
                {
                    var candidates = ((ErrorTypeSymbol)x).CandidateSymbols;
                    if (candidates.Length != 0)
                    {
                        return candidates.OfType<NamedTypeSymbol>().AsImmutable();
                    }
                }
                else
                {
                    return ImmutableArray.Create(x);
                }
            }

            return ImmutableArray<NamedTypeSymbol>.Empty;
        }
    }
}
