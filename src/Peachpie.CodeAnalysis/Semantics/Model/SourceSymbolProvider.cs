using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using Pchp.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.Model
{
    internal sealed class SourceSymbolProvider : ISymbolProvider
    {
        readonly SourceSymbolCollection _table;

        public PhpCompilation Compilation => _table.Compilation;

        public SourceSymbolProvider(SourceSymbolCollection table)
        {
            Contract.ThrowIfNull(table);
            _table = table;
        }

        public IPhpScriptTypeSymbol ResolveFile(string relativePathNormalized)
        {
            // {relativePathNormalized} is relative to BaseDirectory
            // slashes are normalized '/'

            if (string.IsNullOrEmpty(relativePathNormalized))
            {
                return null;
            }

            // ./ handled by context semantics

            // ../ handled by context semantics

            // TODO: lookup include paths
            // TODO: calling script directory

            // cwd
            return _table.GetFile(relativePathNormalized);
        }

        public INamedTypeSymbol ResolveType(QualifiedName name, Dictionary<QualifiedName, INamedTypeSymbol> resolved) => _table.GetType(name, resolved);

        public IPhpRoutineSymbol ResolveFunction(QualifiedName name)
        {
            return _table.GetFunction(name);
        }

        public IPhpValue ResolveConstant(string name) => null;
    }
}
