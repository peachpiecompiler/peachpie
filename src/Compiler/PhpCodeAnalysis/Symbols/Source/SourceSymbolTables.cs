using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Pchp.Syntax;
using System.Diagnostics;
using Pchp.Syntax.AST;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents declarations within given source trees.
    /// </summary>
    internal class SourceSymbolTables : ISymbolTables
    {
        #region PopulatorVisitor

        sealed class PopulatorVisitor : TreeVisitor
        {
            readonly SourceSymbolTables _tables;
            readonly PhpCompilation _compilation;
            
            public PopulatorVisitor(PhpCompilation compilation, SourceSymbolTables tables)
            {
                _tables = tables;
                _compilation = compilation;
            }

            public void VisitSourceUnit(SourceUnit unit)
            {
                if (unit != null && unit.Ast != null)
                {
                    _tables._files.Add(unit.FilePath, unit.Ast);

                    VisitGlobalCode(unit.Ast);
                }
            }

            public override void VisitFunctionDecl(FunctionDecl x)
            {
                _tables._functions.Add(NameUtils.MakeQualifiedName(x.Name, x.Namespace), new SourceFunctionSymbol(_compilation, x));
            }

            public override void VisitTypeDecl(TypeDecl x)
            {
                _tables._types.Add(x.MakeQualifiedName(), new SourceNamedTypeSymbol(_compilation, x));
            }
        }

        #endregion

        readonly Dictionary<QualifiedName, INamedTypeSymbol> _types = new Dictionary<QualifiedName, INamedTypeSymbol>();
        readonly Dictionary<QualifiedName, IMethodSymbol> _functions = new Dictionary<QualifiedName, IMethodSymbol>();
        readonly Dictionary<string, GlobalCode> _files = new Dictionary<string, GlobalCode>(StringComparer.OrdinalIgnoreCase);
        
        public SourceSymbolTables()
        {
            
        }

        internal void PopulateTables(PhpCompilation compilation, IEnumerable<SourceUnit>/**/trees)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(trees);

            var visitor = new PopulatorVisitor(compilation, this);
            trees.Foreach(visitor.VisitSourceUnit);
        }

        #region ISymbolTables

        IMethodSymbol ISymbolTables.GetFunction(QualifiedName name) => _functions.TryGetOrDefault(name);

        IEnumerable<IMethodSymbol> ISymbolTables.GetFunctions() => _functions.Values;

        INamedTypeSymbol ISymbolTables.GetType(QualifiedName name) => _types.TryGetOrDefault(name);

        IEnumerable<INamedTypeSymbol> ISymbolTables.GetTypes() => _types.Values;

        #endregion
    }
}
