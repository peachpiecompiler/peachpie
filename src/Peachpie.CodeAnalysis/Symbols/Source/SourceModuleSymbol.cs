using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class SourceModuleSymbol : ModuleSymbol, IModuleSymbol
    {
        readonly SourceAssemblySymbol _sourceAssembly;
        readonly string _name;
        readonly NamespaceSymbol _ns;

        /// <summary>
        /// Tables of all source symbols to be compiled within the source module.
        /// </summary>
        public SourceSymbolCollection SymbolCollection => DeclaringCompilation.SourceSymbolCollection;

        public SourceModuleSymbol(SourceAssemblySymbol sourceAssembly, string name)
        {
            _sourceAssembly = sourceAssembly;
            _name = name;
            _ns = new SourceGlobalNamespaceSymbol(this);
        }

        public override string Name => _name;

        public override Symbol ContainingSymbol => _sourceAssembly;

        public override NamespaceSymbol GlobalNamespace => _ns;

        internal SourceAssemblySymbol SourceAssemblySymbol => _sourceAssembly;

        public override AssemblySymbol ContainingAssembly => _sourceAssembly;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override PhpCompilation DeclaringCompilation => _sourceAssembly.DeclaringCompilation;

        /// <summary>
        /// Lookup a top level type referenced from metadata, names should be
        /// compared case-sensitively.
        /// </summary>
        /// <param name="emittedName">
        /// Full type name, possibly with generic name mangling.
        /// </param>
        /// <returns>
        /// Symbol for the type, or MissingMetadataSymbol if the type isn't found.
        /// </returns>
        /// <remarks></remarks>
        internal sealed override NamedTypeSymbol LookupTopLevelMetadataType(ref MetadataTypeName emittedName)
        {
            NamedTypeSymbol result;
            NamespaceSymbol scope = this.GlobalNamespace; //.LookupNestedNamespace(emittedName.NamespaceSegments);

            if ((object)scope == null)
            {
                // We failed to locate the namespace
                throw new NotImplementedException();
                //result = new MissingMetadataTypeSymbol.TopLevel(this, ref emittedName);
            }
            else
            {
                result = scope.LookupMetadataType(ref emittedName);
            }

            Debug.Assert((object)result != null);
            return result;
        }

        ImmutableArray<AttributeData> _lazyAttributesToEmit;

        internal override IEnumerable<AttributeData> GetCustomAttributesToEmit(CommonModuleCompilationState compilationState)
        {
            var attrs = base.GetCustomAttributesToEmit(compilationState);

            if (_lazyAttributesToEmit.IsDefault)
            {
                _lazyAttributesToEmit = CreateAttributesToEmit().ToImmutableArray();
            }

            attrs = attrs.Concat(_lazyAttributesToEmit);

            //
            return attrs;
        }

        IEnumerable<AttributeData> CreateAttributesToEmit()
        {
            // [ImportPhpType( ... )]
            var ctor = (MethodSymbol)DeclaringCompilation.GetTypeByMetadataName("Pchp.Core.ImportPhpTypeAttribute").InstanceConstructors.Single();
            foreach (var t in DeclaringCompilation.GlobalSemantics.GetReferencedTypes())
            {
                yield return new SynthesizedAttributeData(
                    ctor,
                    ImmutableArray.Create(DeclaringCompilation.CreateTypedConstant(
                        t.IsTraitType() ? t.ConstructedFrom.ConstructUnboundGenericType() : t)),
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            // [ImportPhpFunctions( ... )]
            ctor = (MethodSymbol)DeclaringCompilation.GetTypeByMetadataName("Pchp.Core.ImportPhpFunctionsAttribute").InstanceConstructors.Single();
            var tcontainers = DeclaringCompilation.GlobalSemantics.ExtensionContainers.Where(x => !x.IsPhpSourceFile());
            foreach (var t in tcontainers)
            {
                // only if contains functions
                if (t.GetMembers().OfType<MethodSymbol>().Any(DeclaringCompilation.GlobalSemantics.IsFunction))
                {
                    yield return new SynthesizedAttributeData(
                        ctor,
                        ImmutableArray.Create(DeclaringCompilation.CreateTypedConstant(t)),
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }
            }

            // [ImportPhpConstants( ... )]
            ctor = (MethodSymbol)DeclaringCompilation.GetTypeByMetadataName("Pchp.Core.ImportPhpConstantsAttribute").InstanceConstructors.Single();
            //tcontainers = DeclaringCompilation.GlobalSemantics.ExtensionContainers.Where(x => !x.IsPhpSourceFile());
            foreach (var t in tcontainers)
            {
                // only if contains constants
                if (t.GetMembers().Any(DeclaringCompilation.GlobalSemantics.IsGlobalConstant))
                {
                    yield return new SynthesizedAttributeData(
                        ctor,
                        ImmutableArray.Create(DeclaringCompilation.CreateTypedConstant(t)),
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }
            }

            // [ExportPhpScript]

            // [PhpPackageReference( ... )]
            ctor = (MethodSymbol)DeclaringCompilation.GetTypeByMetadataName("Pchp.Core.PhpPackageReferenceAttribute").InstanceConstructors.Single();
            foreach (var a in DeclaringCompilation.GlobalSemantics.ReferencedPhpPackageReferences)
            {
                var scriptType = a.GetTypeByMetadataName(WellKnownPchpNames.DefaultScriptClassName); // <Script> for PHP libraries
                if (scriptType.IsErrorTypeOrNull())
                {
                    // pick any type as a refernce for C# extension libraries
                    var alltypes = a.PrimaryModule.GlobalNamespace.GetTypeMembers();
                    scriptType =
                        alltypes.FirstOrDefault(x => x.DeclaredAccessibility == Accessibility.Public) ??
                        alltypes.FirstOrDefault(x => x.DeclaredAccessibility == Accessibility.Internal);
                }

                if (scriptType.IsValidType())
                {
                    yield return new SynthesizedAttributeData(
                        ctor,
                        ImmutableArray.Create(DeclaringCompilation.CreateTypedConstant(scriptType)),
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }
            }

            //
            yield break;
        }
    }
}
