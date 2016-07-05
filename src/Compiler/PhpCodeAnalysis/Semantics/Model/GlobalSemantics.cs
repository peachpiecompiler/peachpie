using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax;
using System.Collections.Immutable;

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
            // TODO: library types

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

        public IEnumerable<ISemanticFunction> ResolveFunction(QualifiedName name)
        {
            var result =
                // library functions, public static methods
                ExtensionContainers.SelectMany(r => r.GetMembers(name.ClrName())).OfType<MethodSymbol>().Where(IsFunction).OfType<ISemanticFunction>()
                // source functions
                .Concat(Next.ResolveFunction(name));

            return result;
        }

        public ISemanticValue ResolveConstant(string name)
        {
            var candidates = new List<FieldSymbol>();

            foreach (var container in ExtensionContainers)
            {
                // container.Constant
                var candidate = container
                    .GetMembers(name).OfType<FieldSymbol>()
                    .Where(f => f.IsConst && f.DeclaredAccessibility == Accessibility.Public)
                    .SingleOrDefault();
                if (candidate != null)
                    candidates.Add(candidate);

                //// container.SomeEnum.Constant
                //candidate = container.GetTypeMembers()
                //    .OfType<NamedTypeSymbol>().Where(TypeSymbolExtensions.IsEnumType)
                //        .SelectMany(t => t.GetMembers(name).OfType<FieldSymbol>())
                //        .SingleOrDefault();

                //if (candidate != null)
                //    candidates.Add(candidate);
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
