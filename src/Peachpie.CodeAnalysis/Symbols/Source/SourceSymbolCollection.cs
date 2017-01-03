using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Pchp.CodeAnalysis.Semantics;
using Roslyn.Utilities;
using Pchp.CodeAnalysis.Utilities;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Collection of source symbols.
    /// </summary>
    internal class SourceSymbolCollection // SourceProject
    {
        #region BinderVisitor

        sealed class BinderVisitor : TreeVisitor
        {
            readonly SourceSymbolCollection _tables;
            readonly PhpCompilation _compilation;

            SourceFileSymbol _currentFile;

            public BinderVisitor(PhpCompilation compilation, SourceSymbolCollection tables)
            {
                _tables = tables;
                _compilation = compilation;
            }

            public override void VisitGlobalCode(GlobalCode x)
            {
                _currentFile = new SourceFileSymbol(_compilation, x);
                _tables._files[_currentFile.RelativeFilePath] = _currentFile;

                if (_tables.FirstScript == null)
                {
                    _tables.FirstScript = _currentFile;
                }

                base.VisitGlobalCode(x);

                _currentFile = null;
            }

            public override void VisitFunctionDecl(FunctionDecl x)
            {
                var routine = new SourceFunctionSymbol(_currentFile, x);

                x.SetProperty(routine); // remember bound function symbol
                _currentFile.AddFunction(routine);

                //
                base.VisitFunctionDecl(x);
            }

            public override void VisitTypeDecl(TypeDecl x)
            {
                var type = new SourceTypeSymbol(_currentFile, x);

                x.SetProperty(type);    // remember bound type symbol
                _currentFile.ContainedTypes.Add(type);

                //
                base.VisitTypeDecl(x);
            }
        }

        #endregion

        #region SymbolsCache

        sealed class SymbolsCache<TKey, TSymbol>
        {
            int _cacheVersion = -1;
            MultiDictionary<TKey, TSymbol> _cacheDict;
            List<TSymbol> _cacheAll;

            readonly SourceSymbolCollection _table;
            readonly Func<SourceFileSymbol, IEnumerable<TSymbol>> _getter;
            readonly Func<TSymbol, TKey> _key;
            readonly Func<TSymbol, bool> _isVisible;

            public SymbolsCache(SourceSymbolCollection table,
                Func<SourceFileSymbol, IEnumerable<TSymbol>> getter,
                Func<TSymbol, TKey> key,
                Func<TSymbol, bool> isVisible)
            {
                _table = table;
                _getter = getter;
                _key = key;
                _isVisible = isVisible;
            }

            void EnsureUpdated()
            {
                if (_table._version != _cacheVersion)
                {
                    _cacheAll = new List<TSymbol>();
                    _cacheDict = new MultiDictionary<TKey, TSymbol>();
                    _cacheVersion = _table._version;

                    foreach (var f in _table._files.Values)
                    {
                        var symbols = _getter(f);
                        _cacheAll.AddRange(symbols);
                        
                        foreach (var visible in symbols.Where(_isVisible))
                        {
                            _cacheDict.Add(_key(visible), visible);
                        }
                    }
                }
            }

            /// <summary>
            /// All symbols, both visible or not.
            /// </summary>
            public IEnumerable<TSymbol> Symbols
            {
                get
                {
                    EnsureUpdated();
                    return _cacheAll;
                }
            }

            /// <summary>
            /// Gets single visible symbol or null.
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public TSymbol SingleOrNull(TKey key)
            {
                var values = this[key];
                return values.Count == 1 ? values.Single() : default(TSymbol);
            }

            /// <summary>
            /// Gets all visible symbols.
            /// </summary>
            public MultiDictionary<TKey, TSymbol>.ValueSet this[TKey key]
            {
                get
                {
                    EnsureUpdated();
                    return _cacheDict[key];
                }
            }
        }

        #endregion

        /// <summary>
        /// Collection version, increased when a syntax tree is added or removed.
        /// </summary>
        public int Version => _version;
        int _version = 0;

        /// <summary>
        /// Gets reference to containing compilation object.
        /// </summary>
        public PhpCompilation Compilation => _compilation;
        readonly PhpCompilation _compilation;

        /// <summary>
        /// Set of files.
        /// </summary>
        readonly Dictionary<string, SourceFileSymbol> _files = new Dictionary<string, SourceFileSymbol>(StringComparer.Ordinal);

        readonly SymbolsCache<QualifiedName, SourceTypeSymbol> _types;
        readonly SymbolsCache<QualifiedName, SourceFunctionSymbol> _functions;

        /// <summary>
        /// First script added to the collection.
        /// Used as a default entry script.
        /// </summary>
        public SourceFileSymbol FirstScript { get; private set; }

        public SourceSymbolCollection(PhpCompilation/*!*/compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;

            _types = new SymbolsCache<QualifiedName, SourceTypeSymbol>(this, f => f.ContainedTypes, t => t.MakeQualifiedName(), t => !t.IsConditional);
            _functions = new SymbolsCache<QualifiedName, SourceFunctionSymbol>(this, f => f.Functions, f => f.QualifiedName, f => !f.IsConditional);
        }

        public void AddSyntaxTreeRange(IEnumerable<SourceUnit> tree)
        {
            tree.ForEach(AddSyntaxTree);
        }

        public void AddSyntaxTree(SourceUnit tree)
        {
            Contract.ThrowIfNull(tree);

            new BinderVisitor(_compilation, this).VisitGlobalCode(tree.Ast);

            _version++;
        }

        public bool RemoveSyntaxTree(string fname)
        {
            var relative = PhpFileUtilities.GetRelativePath(fname, _compilation.Options.BaseDirectory);
            if (_files.Remove(relative))
            {
                _version++;

                return true;
            }

            return false;
        }

        public SourceFileSymbol GetFile(string fname) => _files.TryGetOrDefault(fname);

        public IEnumerable<SourceFileSymbol> GetFiles() => _files.Values;

        public MethodSymbol GetFunction(QualifiedName name) => _functions.SingleOrNull(name);

        public IEnumerable<MethodSymbol> GetFunctions(QualifiedName name) => _functions[name];

        public IEnumerable<SourceFunctionSymbol> GetFunctions() => _functions.Symbols;

        /// <summary>
        /// Gets enumeration of all routines (global code, functions and methods) in source code.
        /// </summary>
        public IEnumerable<SourceRoutineSymbol> AllRoutines    // all functions + global code + methods
        {
            get
            {
                var funcs = GetFunctions().Cast<SourceRoutineSymbol>();
                var mains = _files.Values.Select(f => f.MainMethod);
                var methods = GetTypes().SelectMany(f => f.GetMembers().OfType<SourceRoutineSymbol>());

                //
                return funcs.Concat(mains).Concat(methods);
            }
        }

        public NamedTypeSymbol GetType(QualifiedName name) => _types.SingleOrNull(name);

        public IEnumerable<SourceTypeSymbol> GetTypes() => _types.Symbols;
    }
}
