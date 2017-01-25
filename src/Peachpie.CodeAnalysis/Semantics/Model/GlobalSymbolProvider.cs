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

        #endregion

        public GlobalSymbolProvider(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
            _next = new SourceSymbolProvider(compilation.SourceSymbolCollection);
        }

        internal static ImmutableArray<NamedTypeSymbol> ResolveExtensionContainers(PhpCompilation compilation)
        {
            return compilation.GetBoundReferenceManager()
                .ExplicitReferencesSymbols.OfType<PEAssemblySymbol>().Where(s => s.IsExtensionLibrary)
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

        #region ISemanticModel

        public INamedTypeSymbol GetType(QualifiedName name)
        {
            var clrName = name.ClrName();

            // std
            if (Core.std.StdTable.Types.Contains(clrName))
            {
                return _compilation.PhpCorLibrary.GetTypeByMetadataName(clrName);
            }

            // TODO: reserved type names: self, parent, static
            Debug.Assert(!name.IsReservedClassName);

            // library types
            foreach (AssemblySymbol ass in _compilation.ProbingAssemblies)
            {
                if (!ass.IsPchpCorLibrary)
                {
                    var candidate = ass.GetTypeByMetadataName(clrName);
                    if (candidate != null && !candidate.IsErrorType())
                    {
                        if (ass is PEAssemblySymbol && ((PEAssemblySymbol)ass).IsExtensionLibrary && candidate.IsStatic)
                        {
                            continue;
                        }

                        return candidate;
                    }
                }
            }

            //
            return _next.GetType(name);
        }

        public SourceFileSymbol GetFile(string path)
        {
            // TODO: lookup referenced assemblies

            // TODO: .\
            // TODO: ..\

            // TODO: RoutineSemantics // relative to current script

            return _next.GetFile(path);
        }

        public IEnumerable<IPhpRoutineSymbol> ResolveFunction(QualifiedName name)
        {
            // library functions, public static methods
            var result = ExtensionContainers.SelectMany(r => r.GetMembers(name.ClrName(), true)).OfType<MethodSymbol>().Where(IsFunction).OfType<IPhpRoutineSymbol>().ToList();
            if (result.Count == 0)
            {
                // source functions
                result.AddRange(_next.ResolveFunction(name));
            }

            return result;
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
