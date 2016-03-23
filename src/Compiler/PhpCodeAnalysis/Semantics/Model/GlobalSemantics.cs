using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Semantics.Model
{
    internal class GlobalSemantics : ISemanticModel
    {
        #region Fields

        readonly PhpCompilation _compilation;

        ImmutableArray<NamedTypeSymbol> _lazyExtensionContainers;

        #endregion

        public GlobalSemantics(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
        }

        ImmutableArray<NamedTypeSymbol> ExtensionContainers
        {
            get
            {
                if (_lazyExtensionContainers.IsDefault)
                {
                    _lazyExtensionContainers = _compilation.GetBoundReferenceManager()
                        .ExplicitReferencesSymbols.OfType<PEAssemblySymbol>().Where(s => s.IsExtensionLibrary)
                        .SelectMany(r => r.ExtensionContainers)
                        .ToImmutableArray();
                }

                return _lazyExtensionContainers;
            }
        }

        #region ISemanticModel

        public ISemanticModel Next => _compilation.SourceSymbolTables;

        public INamedTypeSymbol GetType(QualifiedName name)
        {
            return Next.GetType(name);

            // TODO: library types
        }

        public SourceFileSymbol GetFile(string relativePath)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ISemanticFunction> ResolveFunction(QualifiedName name)
        {
            var result =
                // library functions
                ExtensionContainers.SelectMany(r => r.GetMembers(name.ClrName()).OfType<ISemanticFunction>())
                // source functions
                .Concat(Next.ResolveFunction(name));

            return result;
        }

        public bool IsAssignableFrom(QualifiedName qname, INamedTypeSymbol from)
        {
            throw new NotImplementedException();
        }

        public bool IsSpecialParameter(ParameterSymbol p)
        {
            return p.Type == _compilation.CoreTypes.Context;
        }

        #endregion
    }
}
