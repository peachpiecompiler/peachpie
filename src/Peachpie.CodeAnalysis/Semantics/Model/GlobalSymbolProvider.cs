using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;
using Devsense.PHP.Syntax;
using Roslyn.Utilities;
using Pchp.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.Model
{
    internal class GlobalSymbolProvider : ISymbolProvider
    {
        #region Fields

        readonly PhpCompilation _compilation;
        readonly ISymbolProvider _next;

        ImmutableArray<NamedTypeSymbol> _lazyExtensionContainers;

        /// <summary>
        /// Types that are visible from extension libraries.
        /// </summary>
        Dictionary<QualifiedName, NamedTypeSymbol> _lazyExportedTypes;

        /// <summary>
        /// Functions that are visible from extension libraries.
        /// </summary>
        MultiDictionary<QualifiedName, MethodSymbol> _lazyExportedFunctions;

        #endregion

        public GlobalSymbolProvider(PhpCompilation compilation)
        {
            _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            _next = new SourceSymbolProvider(compilation.SourceSymbolCollection);
        }

        static IEnumerable<PEAssemblySymbol> GetExtensionLibraries(PhpCompilation compilation)
            => compilation
            .GetBoundReferenceManager()
            .ExplicitReferencesSymbols
            .OfType<PEAssemblySymbol>()
            .Where(s => s.IsExtensionLibrary);

        /// <summary>
        /// Enumerates referenced assemblies which are extension libraries (has PhpExtensionAttribute).
        /// </summary>
        public IEnumerable<PEAssemblySymbol> ReferencedPhpPackageReferences
        {
            get
            {
                return GetExtensionLibraries(Compilation);//.Where( has scripts );
            }
        }

        static IEnumerable<NamedTypeSymbol> ResolveExtensionContainers(PhpCompilation compilation)
        {
            var libraries = GetExtensionLibraries(compilation).SelectMany(r => r.ExtensionContainers);
            var synthesized = compilation.SourceSymbolCollection.DefinedConstantsContainer;

            //
            if (synthesized.GetMembers().IsDefaultOrEmpty)
            {
                return libraries;
            }
            else
            {
                return libraries.Concat(synthesized);
            }
        }

        internal bool IsFunction(MethodSymbol method)
        {
            return method != null && method.IsStatic && method.DeclaredAccessibility == Accessibility.Public && method.MethodKind == MethodKind.Ordinary && !method.IsPhpHidden;
        }

        internal bool IsGlobalConstant(Symbol symbol)
        {
            if (symbol is FieldSymbol field)
            {
                return (field.IsConst || (field.IsReadOnly && field.IsStatic)) &&
                    field.DeclaredAccessibility == Accessibility.Public &&
                    !field.IsPhpHidden;
            }

            if (symbol is PropertySymbol prop)
            {
                return prop.IsStatic && prop.DeclaredAccessibility == Accessibility.Public && !prop.IsPhpHidden(_compilation);
            }

            return false;
        }

        internal ImmutableArray<NamedTypeSymbol> ExtensionContainers
        {
            get
            {
                if (_lazyExtensionContainers.IsDefault)
                {
                    _lazyExtensionContainers = ResolveExtensionContainers(_compilation).ToImmutableArray();
                }

                return _lazyExtensionContainers;
            }
        }

        /// <summary>
        /// (PHP) Types exported from extension libraries and cor library.
        /// </summary>
        public Dictionary<QualifiedName, NamedTypeSymbol> ExportedTypes
        {
            get
            {
                if (_lazyExportedTypes == null)
                {
                    var result = new Dictionary<QualifiedName, NamedTypeSymbol>();
                    var langVersion = _compilation.Options.LanguageVersion;

                    // lookup extensions and cor library for exported types
                    var libs = GetExtensionLibraries(_compilation).ToList();
                    libs.Add((PEAssemblySymbol)_compilation.PhpCorLibrary);

                    //
                    foreach (var lib in libs)
                    {
                        foreach (var t in lib.PrimaryModule.GlobalNamespace.GetTypeMembers().OfType<PENamedTypeSymbol>())
                        {
                            if (t.DeclaredAccessibility != Accessibility.Public)
                            {
                                continue;
                            }

                            if (t.TryGetPhpTypeAttribute(out var fullname, out var minLangVersion) == false)
                            {
                                continue;
                            }

                            if (minLangVersion != null && langVersion < minLangVersion)
                            {
                                // PHP type not valid in current language version:
                                continue;
                            }

                            NamedTypeSymbol tsymbol = t;

                            if (result.TryGetValue(fullname, out var existing))
                            {
                                // merge {t} and {existing}:
                                if (t.IsPhpUserType() && !existing.IsPhpUserType())
                                {
                                    // ignore {t}
                                    continue;
                                }
                                else if (existing.IsPhpUserType() && !t.IsPhpUserType())
                                {
                                    // replace existing (user type) with t (library type)
                                    tsymbol = t;
                                }
                                else if (existing is AmbiguousErrorTypeSymbol ambiguous)
                                {
                                    // just collect possible types, there is perf. penalty for that
                                    // TODO: if there are user & library types mixed together, we expect compilation assertions and errors, fix that
                                    // this will be fixed once we stop declare unreachable types
                                    ambiguous._candidates = ambiguous._candidates.Add(t);
                                    continue;
                                }
                                else
                                {
                                    tsymbol = new AmbiguousErrorTypeSymbol(ImmutableArray.Create(existing, t));
                                }
                            }

                            result[fullname] = tsymbol;
                        }
                    }

                    //
                    _lazyExportedTypes = result;
                }

                return _lazyExportedTypes;
            }
        }

        /// <summary>
        /// Gets script classes from referenced assemblies.
        /// </summary>
        IEnumerable<IPhpScriptTypeSymbol> GetScriptsFromReferencedAssemblies()
        {
            return _referencedScripts
                ?? (_referencedScripts = GetExtensionLibraries(_compilation)
                    .SelectMany(ass => ass.PrimaryModule.GlobalNamespace.GetTypeMembers())
                    .OfType<IPhpScriptTypeSymbol>()
                    .Where(t => t.RelativeFilePath != null)
                    .Where(t => !t.IsPharEntry())// ignore Phar entries
                    .ToArray());
        }

        IPhpScriptTypeSymbol[] _referencedScripts; // TODO: dictionary

        /// <summary>
        /// Gets scripts used within the compilation.
        /// </summary>
        public IEnumerable<IPhpScriptTypeSymbol> ExportedScripts
        {
            get
            {
                // source files
                var srcfiles = _compilation.SourceSymbolCollection.GetFiles().Cast<IPhpScriptTypeSymbol>();

                // scripts from referenced assemblies
                var refscripts = GetScriptsFromReferencedAssemblies();

                //
                return srcfiles.Concat(refscripts);
            }
        }

        public IEnumerable<IPhpValue> GetExportedConstants()
        {
            return ExtensionContainers
                .SelectMany(t => t.GetMembers().Where(IsGlobalConstant))
                .OfType<IPhpValue>();
        }

        /// <summary>
        /// Gets PHP types exported from referenced extension libraries and cor library.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<NamedTypeSymbol> GetReferencedTypes() => ExportedTypes.Values.Where(t => t.IsValidType() && !t.IsPhpUserType());

        public MultiDictionary<QualifiedName, MethodSymbol> ExportedFunctions
        {
            get
            {
                if (_lazyExportedFunctions == null)
                {
                    var dict = new MultiDictionary<QualifiedName, MethodSymbol>();

                    foreach (var container in ExtensionContainers)
                    {
                        container.GetMembers()
                            .OfType<MethodSymbol>()
                            .Where(IsFunction)
                            .ForEach(m => dict.Add(new QualifiedName(new Name(m.RoutineName)), m));
                    }

                    //
                    InterlockedOperations.Initialize(ref _lazyExportedFunctions, dict);
                }

                return _lazyExportedFunctions;
            }
        }

        #region ISemanticModel

        /// <summary>
        /// Gets declaring compilation.
        /// </summary>
        public PhpCompilation Compilation => _compilation;

        public INamedTypeSymbol ResolveType(QualifiedName name, Dictionary<QualifiedName, INamedTypeSymbol> resolved = null)
        {
            Debug.Assert(!name.IsReservedClassName);
            Debug.Assert(!name.IsEmpty());

            if (resolved != null && resolved.TryGetValue(name, out var type))
            {
                return type;
            }

            return
                ExportedTypes.TryGetOrDefault(name) ??
                GetTypeFromNonExtensionAssemblies(name.ClrName()) ??
                _next.ResolveType(name, resolved);
        }

        public NamedTypeSymbol GetTypeFromNonExtensionAssemblies(string clrName)
        {
            string nestedname = null;

            var lastdot = clrName.LastIndexOf('.');
            if (lastdot > 0)
            {
                // a nested class name, 1 level only
                nestedname = clrName.Remove(lastdot) + "+" + clrName.Substring(lastdot + 1);
            }

            foreach (AssemblySymbol ass in _compilation.ProbingAssemblies)
            {
                if (ass is PEAssemblySymbol peass) // && !peass.IsPchpCorLibrary && !peass.IsExtensionLibrary)
                {
                    var candidate = ass.GetTypeByMetadataName(clrName);

                    if (candidate.IsErrorTypeOrNull() && nestedname != null)
                    {
                        // try a nested class, 1 level only
                        candidate = ass.GetTypeByMetadataName(nestedname);
                    }

                    if (candidate.IsValidType())
                    {
                        if (candidate is PENamedTypeSymbol pe &&
                            pe.TryGetPhpTypeAttribute(out _, out var minLangVersion) && minLangVersion != null)
                        {
                            if (minLangVersion > _compilation.Options.LanguageVersion)
                            {
                                // PHP type not valid in current language version:
                                continue;
                            }
                        }

                        return candidate;
                    }
                }
            }

            return null;
        }

        public IPhpScriptTypeSymbol ResolveFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // normalize path
            path = FileUtilities.NormalizeRelativePath(path, null, _compilation.Options.BaseDirectory);

            if (string.IsNullOrEmpty(path))
            {
                // path cannot be normalized, usually contains a protocol://
                return null;
            }

            // absolute path
            if (PathUtilities.IsAbsolute(path))
            {
                path = PhpFileUtilities.GetRelativePath(path, _compilation.Options.BaseDirectory);
            }

            // normalize slashes
            path = PhpFileUtilities.NormalizeSlashes(path);

            // ./ handled by context semantics

            // ../ handled by context semantics

            // TODO: lookup include paths
            // TODO: calling script directory
            
            // lookup referenced assemblies
            var script = GetScriptsFromReferencedAssemblies().FirstOrDefault(t => t.RelativeFilePath == path);

            // TODO: RoutineSemantics // relative to current script

            return script ?? _next.ResolveFile(path);
        }

        public IPhpRoutineSymbol ResolveFunction(QualifiedName name)
        {
            var methods = ExportedFunctions[name];
            if (methods.Count == 1)
            {
                return methods.Single();
            }
            else if (methods.Count > 1)
            {
                // if the function is user defined (PHP), we might not treat this as CLR method (ie do not resolve overloads in compile time)
                bool userfunc = methods.Any(m => m.ContainingType.IsPhpSourceFile());
                return new AmbiguousMethodSymbol(methods.AsImmutable(), overloadable: !userfunc);
            }
            else
            {
                return _next.ResolveFunction(name);
            }
        }

        public IPhpValue ResolveConstant(string name)
        {
            foreach (var container in ExtensionContainers)
            {
                // container.Constant
                var match = container.GetMembers(name).Where(IsGlobalConstant).SingleOrDefault();
                if (match is IPhpValue phpv) // != null
                {
                    return phpv;
                }
            }

            return _next.ResolveConstant(name);
        }

        /// <summary>
        /// Gets enumeration of referenced extensions.
        /// </summary>
        public IEnumerable<string> Extensions
        {
            get
            {
                var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var t in GetExtensionLibraries(_compilation).Cast<Symbol>().Concat(ExtensionContainers).Concat(ExportedTypes.Values))   // assemblies & containers & types
                {
                    result.UnionWith(SymbolExtensions.PhpExtensionAttributeValues(t.GetPhpExtensionAttribute()));
                }

                return result;
            }
        }

        #endregion
    }
}
