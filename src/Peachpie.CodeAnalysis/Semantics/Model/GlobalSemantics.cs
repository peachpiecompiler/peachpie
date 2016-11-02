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
    internal class GlobalSemantics : ISemanticModel
    {
        #region Fields

        readonly PhpCompilation _compilation;

        ImmutableArray<NamedTypeSymbol> _lazyExtensionContainers;

        #endregion

        public GlobalSemantics(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
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
            return method.IsStatic && method.DeclaredAccessibility == Accessibility.Public && method.MethodKind == MethodKind.Ordinary;
        }

        internal static bool IsConstantField(FieldSymbol field)
        {
            return (field.IsConst || (field.IsReadOnly && field.IsStatic)) && field.DeclaredAccessibility == Accessibility.Public;
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

        public ISemanticModel Next => _compilation.SourceSymbolTables;

        public INamedTypeSymbol GetType(QualifiedName name)
        {
            // std // TODO: table of types in PchpCor
            if (name == NameUtils.SpecialNames.stdClass)
            {
                return _compilation.PhpCorLibrary.GetTypeByMetadataName(name.ClrName());
            }

            // TODO: reserved type names: self, parent, static
            Debug.Assert(!name.IsReservedClassName);

            // library types
            foreach (AssemblySymbol ass in _compilation.ProbingAssemblies)
            {
                if (!ass.IsPchpCorLibrary)
                {
                    var candidate = ass.GetTypeByMetadataName(name.ClrName());
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
            return Next.GetType(name);
        }

        public SourceFileSymbol GetFile(string path)
        {
            // TODO: lookup referenced assemblies

            // TODO: .\
            // TODO: ..\

            // TODO: RoutineSemantics // relative to current script

            return Next.GetFile(path);
        }

        public IEnumerable<IPhpRoutineSymbol> ResolveFunction(QualifiedName name)
        {
            var result =
                // library functions, public static methods
                ExtensionContainers.SelectMany(r => r.GetMembers(name.ClrName())).OfType<MethodSymbol>().Where(IsFunction).OfType<IPhpRoutineSymbol>()
                // source functions
                .Concat(Next.ResolveFunction(name));

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

            return Next.ResolveConstant(name);
        }

        public bool IsAssignableFrom(QualifiedName qname, INamedTypeSymbol from)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
