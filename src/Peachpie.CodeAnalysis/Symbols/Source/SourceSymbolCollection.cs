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
using static Pchp.CodeAnalysis.AstUtils;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Collection of source symbols.
    /// </summary>
    internal class SourceSymbolCollection // SourceProject
    {
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
                    return GetAll(key).Where(_isVisible);
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

            Debug.Assert(tree.Root != null);

            // create file symbol (~ php script containing type)
            var fsymbol = SourceFileSymbol.Create(_compilation, tree);
            if (FirstScript == null) FirstScript = fsymbol;

            // collect type declarations
            foreach (var t in tree.Types)
            {
                var typesymbol = SourceTypeSymbol.Create(fsymbol, t);

                t.SetProperty(typesymbol);    // remember bound type symbol
                fsymbol.ContainedTypes.Add(typesymbol);
            }

            // annotate routines that contain yield
            if (!tree.YieldNodes.IsDefaultOrEmpty)
            {
                var yieldsInRoutines = new Dictionary<LangElement, List<IYieldLikeEx>>();
                foreach (var y in tree.YieldNodes)
                {
                    Debug.Assert(y is IYieldLikeEx);
                    var yield = y as IYieldLikeEx;

                    var containingRoutine = y.GetContainingRoutine();
                    Debug.Assert(containingRoutine != null);

                    if (!yieldsInRoutines.ContainsKey(containingRoutine))
                    {
                        yieldsInRoutines.Add(containingRoutine, new List<IYieldLikeEx>());
                    }
                    yieldsInRoutines[containingRoutine].Add(yield);
                }

                foreach(var yieldsInRoutine in yieldsInRoutines)
                {
                    var routine = yieldsInRoutine.Key;
                    var yields = yieldsInRoutine.Value;

                    routine.Properties.SetProperty(typeof(ImmutableArray<IYieldLikeEx>), yields.ToImmutableArray());
                }
            }

            //
            foreach (var f in tree.Functions)
            {
                var routine = new SourceFunctionSymbol(fsymbol, f);

                f.SetProperty(routine); // remember bound function symbol
                fsymbol.AddFunction(routine);
            }

            //
            foreach (var l in tree.Lambdas)
            {
                var lambdasymbol = new SourceLambdaSymbol(l, fsymbol, !l.Modifiers.IsStatic());
                ((ILambdaContainerSymbol)fsymbol).AddLambda(lambdasymbol);
            }

            //
            _files.Add(fsymbol.RelativeFilePath, fsymbol);
            _ordinalMap.Add(tree, _ordinalMap.Count);
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

        public SourceFileSymbol GetFile(string fname) => fname != null ? _files.TryGetOrDefault(fname) : null;

        /// <summary>
        /// Gets compilation syntax trees.
        /// </summary>
        public IEnumerable<PhpSyntaxTree> SyntaxTrees => _files.Values.Select(f => f.SyntaxTree);

        public IEnumerable<SourceFileSymbol> GetFiles() => _files.Values;

        public int FilesCount => _files.Count;

        /// <summary>
        /// Gets function symbol, may return <see cref="ErrorMethodSymbol"/> in case of ambiguity or a missing function.
        /// </summary>
        public MethodSymbol GetFunction(QualifiedName name)
        {
            var fncs = _functions.GetAll(name).WhereReachable().AsImmutable();
            if (fncs.Length == 1 && !fncs[0].IsConditional) return fncs[0];
            if (fncs.Length == 0) return new MissingMethodSymbol(name.Name.Value);
            return new AmbiguousMethodSymbol(fncs.AsImmutable<MethodSymbol>(), overloadable: false);
        }

        public IEnumerable<MethodSymbol> GetFunctions(QualifiedName name) => _functions[name].WhereReachable();

        public IEnumerable<SourceFunctionSymbol> GetFunctions()
        {
            return _functions.Symbols.WhereReachable();
        }

        public IEnumerable<SourceLambdaSymbol> GetLambdas()
        {
            return _files.Values.Cast<ILambdaContainerSymbol>().SelectMany(c => c.Lambdas);
        }

        /// <summary>
        /// Gets enumeration of all routines (global code, functions, lambdas and class methods) in source code.
        /// </summary>
        public IEnumerable<SourceRoutineSymbol> AllRoutines    // all functions + global code + methods + lambdas
        {
            get
            {
                var funcs = GetFunctions().Cast<SourceRoutineSymbol>();
                var mains = _files.Values.Select(f => (SourceRoutineSymbol)f.MainMethod);
                var methods = GetTypes().SelectMany(f => f.GetMembers().OfType<SourceRoutineSymbol>());
                var lambdas = GetLambdas();
                
                //
                return funcs.Concat(mains).Concat(methods).Concat(lambdas);
            }
        }

        public NamedTypeSymbol GetType(QualifiedName name)
        {
            NamedTypeSymbol first = null;
            List<NamedTypeSymbol> alternatives = null;

            var types = _types.GetAll(name).SelectMany(t => t.AllReachableVersions());   // get all types with {name} and their versions
            foreach (var t in types)
            {
                if (first == null)
                {
                    first = t;
                }
                else
                {
                    // ambiguity
                    if (alternatives == null)
                    {
                        alternatives = new List<NamedTypeSymbol>() { first };
                    }
                    alternatives.Add(t);
                }
            }

            return
                (alternatives != null) ? new AmbiguousErrorTypeSymbol(alternatives.AsImmutable())   // ambiguity
                : first ?? new MissingMetadataTypeSymbol(name.ClrName(), 0, false);
        }

        /// <summary>
        /// Gets source declarations without versions.
        /// </summary>
        internal IEnumerable<SourceTypeSymbol> GetDeclaredTypes(QualifiedName name)
        {
            return _types.GetAll(name);//.WhereReachable();
        }

        /// <summary>
        /// Gets source declarations without versions.
        /// </summary>
        internal IEnumerable<SourceTypeSymbol> GetDeclaredTypes() => _types.Symbols;

        /// <summary>
        /// Gets all source types and their versions.
        /// </summary>
        public IEnumerable<SourceTypeSymbol> GetTypes() => GetDeclaredTypes().SelectMany(t => t.AllReachableVersions());
    }
}
