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
    internal class SourceDeclarations
    {
        #region PopulatorVisitor

        sealed class PopulatorVisitor : TreeVisitor
        {
            readonly SourceDeclarations _tables;
            readonly PhpCompilation _compilation;

            SourceFileSymbol _currentFile;
            
            public PopulatorVisitor(PhpCompilation compilation, SourceDeclarations tables)
            {
                _tables = tables;
                _compilation = compilation;
            }

            public void VisitSourceUnit(SourceUnit unit)
            {
                if (unit != null && unit.Ast != null)
                {
                    _currentFile = new SourceFileSymbol(_compilation, unit.Ast, _tables._files.Count);
                    _tables._files.Add(unit.FilePath, _currentFile);

                    VisitGlobalCode(unit.Ast);

                    _currentFile = null;
                }
            }

            public override void VisitFunctionDecl(FunctionDecl x)
            {
                _tables._functions.Add(NameUtils.MakeQualifiedName(x.Name, x.Namespace), new SourceFunctionSymbol(_currentFile, x));
            }

            public override void VisitTypeDecl(TypeDecl x)
            {
                _tables._types.Add(x.MakeQualifiedName(), new SourceNamedTypeSymbol(_currentFile, x));
            }
        }

        #endregion

        readonly Dictionary<QualifiedName, SourceNamedTypeSymbol> _types = new Dictionary<QualifiedName, SourceNamedTypeSymbol>();
        readonly Dictionary<QualifiedName, SourceRoutineSymbol> _functions = new Dictionary<QualifiedName, SourceRoutineSymbol>();
        readonly Dictionary<string, SourceFileSymbol> _files = new Dictionary<string, SourceFileSymbol>(StringComparer.OrdinalIgnoreCase);
        
        public SourceDeclarations()
        {
            
        }

        internal void PopulateTables(PhpCompilation compilation, IEnumerable<SourceUnit>/**/trees)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(trees);

            var visitor = new PopulatorVisitor(compilation, this);
            trees.ForEach(visitor.VisitSourceUnit);
        }

        public SourceFileSymbol GetFile(string fname) => _files.TryGetOrDefault(fname);

        public IEnumerable<SourceFileSymbol> GetFiles() => _files.Values;

        public MethodSymbol GetFunction(QualifiedName name) => _functions.TryGetOrDefault(name);

        public IEnumerable<MethodSymbol> GetFunctions() => _functions.Values;

        public NamedTypeSymbol GetType(QualifiedName name) => _types.TryGetOrDefault(name);

        public IEnumerable<NamedTypeSymbol> GetTypes() => _types.Values;
    }
}
