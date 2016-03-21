using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax;

namespace Pchp.CodeAnalysis.Semantics.Model
{
    internal class GlobalSemantics : ISemanticModel
    {
        #region Fields

        readonly PhpCompilation _compilation;

        #endregion

        public GlobalSemantics(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
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
            return Next.ResolveFunction(name);

            // TODO: library functions
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
