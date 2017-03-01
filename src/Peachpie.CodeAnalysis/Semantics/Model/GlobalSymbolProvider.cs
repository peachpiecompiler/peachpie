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
        /// Standard (builtin) types from <c>Peachpie.Runtime</c>.
        /// </summary>
        Dictionary<QualifiedName, NamedTypeSymbol> _lazyStdTypes;

        #endregion

        public GlobalSymbolProvider(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
            _next = new SourceSymbolProvider(compilation.SourceSymbolCollection);
        }

        static IEnumerable<PEAssemblySymbol> GetExtensionLibraries(PhpCompilation compilation)
            => compilation
            .GetBoundReferenceManager()
            .ExplicitReferencesSymbols
            .OfType<PEAssemblySymbol>()
            .Where(s => s.IsExtensionLibrary);

        internal static ImmutableArray<NamedTypeSymbol> ResolveExtensionContainers(PhpCompilation compilation)
        {
            return GetExtensionLibraries(compilation)
                .SelectMany(r => r.ExtensionContainers)
                .ToImmutableArray();
        }

        internal static bool IsFunction(MethodSymbol method)
        {
            return method.IsStatic && method.DeclaredAccessibility == Accessibility.Public && method.MethodKind == MethodKind.Ordinary && !method.IsPhpHidden();
        }

        internal static bool IsConstantField(FieldSymbol field)
        {
            return (field.IsConst || (field.IsReadOnly && field.IsStatic)) && field.DeclaredAccessibility == Accessibility.Public && !field.IsPhpHidden();
        }

        ImmutableArray<NamedTypeSymbol> ExtensionContainers
        {
            get
            {
                if (_lazyExtensionContainers.IsDefault)
                {
                    _lazyExtensionContainers = ResolveExtensionContainers(_compilation);
                }

                return _lazyExtensionContainers;
            }
        }

        /// <summary>
        /// Types in extension libraries.
        /// </summary>
        Dictionary<QualifiedName, NamedTypeSymbol> ExportedTypes
        {
            get
            {
                if (_lazyExportedTypes == null)
                {
                    var result = new Dictionary<QualifiedName, NamedTypeSymbol>();

                    foreach (var lib in GetExtensionLibraries(_compilation))
                    {
                        foreach (var t in lib.PrimaryModule.GlobalNamespace.GetTypeMembers().OfType<PENamedTypeSymbol>())
                        {
                            if (t.DeclaredAccessibility == Accessibility.Public)
                            {
                                var qname = t.GetPhpTypeNameOrNull();
                                if (!qname.IsEmpty())
                                {
                                    result[qname] = t;
                                }
                            }
                        }
                    }
                    
                    //
                    _lazyExportedTypes = result;
                }

                return _lazyExportedTypes;
            }
        }

        /// <summary>
        /// Standard types in Peachpie.Runtime.
        /// </summary>
        Dictionary<QualifiedName, NamedTypeSymbol> StandardTypes
        {
            get
            {
                if (_lazyStdTypes == null)
                {
                    _lazyStdTypes = Core.std.StdTable.Types
                        .Select(tname => _compilation.PhpCorLibrary.GetTypeByMetadataName(tname))
                        .Where(t => t != null)
                        .ToDictionary(t => ((IPhpTypeSymbol)t).FullName);
                }

                return _lazyStdTypes;
            }
        }

        public IEnumerable<IPhpValue> GetExportedConstants()
        {
            return ExtensionContainers
                .SelectMany(t => t.GetMembers().OfType<FieldSymbol>())
                .Where(IsConstantField);
        }

        internal IEnumerable<NamedTypeSymbol> GetReferencedTypes()
        {
            return StandardTypes.Values.Concat(ExportedTypes.Values);
        }

        #region ISemanticModel

        public INamedTypeSymbol GetType(QualifiedName name)
        {
            Debug.Assert(!name.IsReservedClassName);
            Debug.Assert(!name.IsEmpty());

            return
                StandardTypes.TryGetOrDefault(name) ??
                ExportedTypes.TryGetOrDefault(name) ??
                GetTypeFromNonExtensionAssemblies(name.ClrName()) ??
                _next.GetType(name);
        }

        NamedTypeSymbol GetTypeFromNonExtensionAssemblies(string clrName)
        {
            foreach (AssemblySymbol ass in _compilation.ProbingAssemblies)
            {
                var peass = ass as PEAssemblySymbol;
                if (peass != null && !peass.IsPchpCorLibrary && !peass.IsExtensionLibrary)
                {
                    var candidate = ass.GetTypeByMetadataName(clrName);
                    if (candidate != null && !candidate.IsErrorType())
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        public SourceFileSymbol GetFile(string path)
        {
            // TODO: lookup referenced assemblies

            // TODO: .\
            // TODO: ..\

            // TODO: RoutineSemantics // relative to current script

            return _next.GetFile(path);
        }

        public IPhpRoutineSymbol ResolveFunction(QualifiedName name)
        {
            // library functions, public static methods
            var methods = new List<MethodSymbol>();
            foreach (var m in ExtensionContainers.SelectMany(r => r.GetMembers(name.ClrName(), true)).OfType<MethodSymbol>().Where(IsFunction))
            {
                methods.Add(m);
            }

            if (methods.Count == 0)
            {
                // source functions
                return _next.ResolveFunction(name);
            }
            else if (methods.Count == 1)
            {
                return methods[0];
            }
            else
            {
                return new AmbiguousMethodSymbol(methods.AsImmutable(), true);
            }
        }

        public IPhpValue ResolveConstant(string name)
        {
            var candidates = new List<IPhpValue>();

            foreach (var container in ExtensionContainers)
            {
                // container.Constant
                var candidate = container.GetMembers(name).OfType<FieldSymbol>().Where(IsConstantField).SingleOrDefault();
                if (candidate != null)
                    candidates.Add(candidate);
            }

            if (candidates.Count == 1)
                return candidates[0];

            if (candidates.Count > 1)
                return null;    // TODO: ErrCode ambiguity

            return _next.ResolveConstant(name);
        }

        public bool IsAssignableFrom(QualifiedName qname, INamedTypeSymbol from)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
