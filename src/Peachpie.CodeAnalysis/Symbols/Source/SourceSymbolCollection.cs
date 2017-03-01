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
            readonly Stack<NamedTypeSymbol> _containerStack = new Stack<NamedTypeSymbol>();

            SourceFileSymbol _currentFile;
            PhpSyntaxTree _syntaxTree;

            public BinderVisitor(PhpCompilation compilation, SourceSymbolCollection tables, PhpSyntaxTree syntaxTree)
            {
                _tables = tables;
                _compilation = compilation;
                _syntaxTree = syntaxTree;
            }

            public override void VisitGlobalCode(GlobalCode x)
            {
                Debug.Assert(_syntaxTree.Source.Ast == x);

                var fsymbol = new SourceFileSymbol(_compilation, _syntaxTree);

                _currentFile = fsymbol;
                _tables._files.Add(fsymbol.RelativeFilePath, fsymbol);
                _tables._ordinalMap.Add(_syntaxTree, _tables._ordinalMap.Count);

                if (_tables.FirstScript == null)
                {
                    _tables.FirstScript = _currentFile;
                }

                _containerStack.Push(fsymbol);

                base.VisitGlobalCode(x);

                _containerStack.Pop();

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
                var type = (x is AnonymousTypeDecl)
                    ? new SourceAnonymousTypeSymbol(_currentFile, (AnonymousTypeDecl)x)
                    : new SourceTypeSymbol(_currentFile, x);

                x.SetProperty(type);    // remember bound type symbol
                _currentFile.ContainedTypes.Add(type);

                //

                _containerStack.Push(type);

                base.VisitTypeDecl(x);

                _containerStack.Pop();
            }

            public override void VisitLambdaFunctionExpr(LambdaFunctionExpr x)
            {
                var container = _containerStack.Peek();
                var lambdasymbol = new SourceLambdaSymbol(x, container, !x.IsStatic);
                Debug.Assert(container is ILambdaContainerSymbol);
                ((ILambdaContainerSymbol)container).AddLambda(lambdasymbol);

                //
                base.VisitLambdaFunctionExpr(x);
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

                    foreach (var f in _table._files.Values)
                    {
                        var symbols = _getter(f);
                        _cacheAll.AddRange(symbols);

                        foreach (var s in symbols)
                        {
                            // add all symbols,
                            // _isVisible may have side effects accessing this incomplete collection
                            _cacheDict.Add(_key(s), s);
                        }
                    }

                    _cacheVersion = _table._version;
                }
            }

            /// <summary>
            /// All symbols, both visible and not visible.
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
            public TSymbol SingleOrDefault(TKey key)
            {
                var single = default(TSymbol);
                var values = this[key];

                int n = 0;

                foreach (var s in values)
                {
                    if (n++ == 0)
                    {
                        single = s;
                    }
                    else
                    {
                        single = default(TSymbol);
                        break;
                    }
                }

                return single;
            }

            public IEnumerable<TSymbol> GetAll(TKey key)
            {
                EnsureUpdated();
                return _cacheDict[key];
            }

            /// <summary>
            /// Gets all visible symbols.
            /// </summary>
            public IEnumerable<TSymbol> this[TKey key]
            {
                get
                {
                    EnsureUpdated();
                    return _cacheDict[key].Where(_isVisible);
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
        readonly Dictionary<SyntaxTree, int> _ordinalMap = new Dictionary<SyntaxTree, int>();

        readonly SymbolsCache<QualifiedName, SourceTypeSymbol> _types;
        readonly SymbolsCache<QualifiedName, SourceFunctionSymbol> _functions;

        /// <summary>
        /// First script added to the collection.
        /// Used as a default entry script.
        /// </summary>
        public SourceFileSymbol FirstScript { get; private set; }

        public IDictionary<SyntaxTree, int> OrdinalMap => _ordinalMap;

        public SourceSymbolCollection(PhpCompilation/*!*/compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;

            _types = new SymbolsCache<QualifiedName, SourceTypeSymbol>(this, f => f.ContainedTypes, t => t.MakeQualifiedName(), t => !t.IsConditional || t.IsAnonymousType);
            _functions = new SymbolsCache<QualifiedName, SourceFunctionSymbol>(this, f => f.Functions, f => f.QualifiedName, f => !f.IsConditional);
        }

        public void AddSyntaxTreeRange(IEnumerable<PhpSyntaxTree> trees)
        {
            trees.ForEach(AddSyntaxTree);
        }

        public void AddSyntaxTree(PhpSyntaxTree tree)
        {
            Contract.ThrowIfNull(tree);

            new BinderVisitor(_compilation, this, tree).VisitGlobalCode(tree.Source.Ast);

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

        /// <summary>
        /// Gets compilation syntax trees.
        /// </summary>
        public IEnumerable<PhpSyntaxTree> SyntaxTrees => _files.Values.Select(f => f.SyntaxTree);

        public IEnumerable<SourceFileSymbol> GetFiles() => _files.Values;

        /// <summary>
        /// Gets function symbol, may return <see cref="ErrorMethodSymbol"/> in case of ambiguity or a missing function.
        /// </summary>
        public MethodSymbol GetFunction(QualifiedName name)
        {
            var fncs = _functions.GetAll(name).AsImmutable();
            if (fncs.Length == 1 && !fncs[0].IsConditional) return fncs[0];
            if (fncs.Length == 0) return new MissingMethodSymbol(name.Name.Value);
            return new AmbiguousMethodSymbol(fncs.AsImmutable<MethodSymbol>(), overloadable: false);
        }

        public IEnumerable<MethodSymbol> GetFunctions(QualifiedName name) => _functions[name];

        public IEnumerable<SourceFunctionSymbol> GetFunctions()
        {
            return _functions.Symbols;
        }

        public IEnumerable<SourceLambdaSymbol> GetLambdas()
        {
            return GetTypes().Cast<ILambdaContainerSymbol>().Concat(_files.Values).SelectMany(c => c.Lambdas);
        }

        /// <summary>
        /// Gets enumeration of all routines (global code, functions and methods) in source code.
        /// </summary>
        public IEnumerable<SourceRoutineSymbol> AllRoutines    // all functions + global code + methods + lambdas
        {
            get
            {
                var funcs = GetFunctions().Cast<SourceRoutineSymbol>();
                var mains = _files.Values.Select(f => f.MainMethod);
                var methods = GetTypes().SelectMany(f => f.GetMembers().OfType<SourceRoutineSymbol>());
                var lambdas = GetLambdas();
                
                //
                return funcs.Concat(mains).Concat(methods).Concat(lambdas);
            }
        }

        public NamedTypeSymbol GetType(QualifiedName name) => _types.SingleOrDefault(name);

        public IEnumerable<SourceTypeSymbol> GetTypes() => _types.Symbols;
    }
}
