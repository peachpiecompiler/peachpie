using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents an assembly imported from a PE.
    /// </summary>
    internal sealed class PEAssemblySymbol : NonMissingAssemblySymbol
    {
        /// <summary>
        /// An Assembly object providing metadata for the assembly.
        /// </summary>
        readonly PEAssembly _assembly;

        /// <summary>
        /// The list of contained PEModuleSymbol objects.
        /// The list doesn't use type ReadOnlyCollection(Of PEModuleSymbol) so that we
        /// can return it from Modules property as is.
        /// </summary>
        readonly ImmutableArray<ModuleSymbol> _modules;

        /// <summary>
        /// Optional full file path to the assembly.
        /// </summary>
        readonly string _filePath;

        /// <summary>
        /// An array of assemblies referenced by this assembly, which are linked (/l-ed) by 
        /// each compilation that is using this AssemblySymbol as a reference. 
        /// If this AssemblySymbol is linked too, it will be in this array too.
        /// The array and its content is provided by ReferenceManager and must not be modified.
        /// </summary>
        private ImmutableArray<AssemblySymbol> _linkedReferencedAssemblies;

        /// <summary>
        /// Assembly is /l-ed by compilation that is using it as a reference.
        /// </summary>
        readonly bool _isLinked;

        /// <summary>
        /// Assembly's custom attributes
        /// </summary>
        private ImmutableArray<AttributeData> _lazyCustomAttributes;

        /// <summary>
        /// The assembly purpose, and whether the compiler treats it as a .NET reference, PHP extension or a Cor library.
        /// </summary>
        SpecialAssembly _specialAssembly;

        /// <summary>
        /// Public static classes containing public static methods and nested classes seen as global declarations in source module.
        /// </summary>
        ImmutableArray<NamedTypeSymbol> _lazyExtensionContainers;

        bool _lazyIsExtensionLibraryResolved;

        /// <summary>
        /// A DocumentationProvider that provides XML documentation comments for this assembly.
        /// </summary>
        readonly DocumentationProvider _documentationProvider;

        internal PEAssembly Assembly => _assembly;

        public override AssemblyIdentity Identity => _assembly.Identity;

        public override Version AssemblyVersionPattern => null;

        public override ImmutableArray<ModuleSymbol> Modules => _modules;

        internal DocumentationProvider DocumentationProvider => _documentationProvider;

        public override INamespaceSymbol GlobalNamespace => PrimaryModule.GlobalNamespace;

        internal PEModuleSymbol PrimaryModule => (PEModuleSymbol)_modules[0];

        internal override PhpCompilation DeclaringCompilation => null;

        public override AssemblyMetadata GetMetadata() => _assembly.GetNonDisposableMetadata();

        internal override ImmutableArray<byte> PublicKey => this.Identity.PublicKey;

        public override bool IsCorLibrary => _specialAssembly == SpecialAssembly.CorLibrary;

        public override bool IsPchpCorLibrary => _specialAssembly == SpecialAssembly.PchpCorLibrary;

        internal override bool IsLinked => _isLinked;

        internal override void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            _linkedReferencedAssemblies = assemblies;
        }

        internal override ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies()
        {
            return _linkedReferencedAssemblies;
        }

        internal bool IsExtensionLibrary
        {
            get
            {
                if (_specialAssembly == SpecialAssembly.None && !_lazyIsExtensionLibraryResolved)
                {
                    var attrs = GetAttributes();
                    foreach (var a in attrs)
                    {
                        var fullname = MetadataHelpers.BuildQualifiedName((a.AttributeClass as NamedTypeSymbol)?.NamespaceName, a.AttributeClass.Name);
                        if (fullname == CoreTypes.PhpExtensionAttributeName)
                        {
                            _specialAssembly = SpecialAssembly.ExtensionLibrary;
                            break;
                        }
                    }

                    _lazyIsExtensionLibraryResolved = true;
                }

                return _specialAssembly == SpecialAssembly.ExtensionLibrary;
            }
        }

        public override string Name => _assembly.ManifestModule.Name;

        public string FilePath => _filePath;

        internal PEAssemblySymbol(PEAssembly assembly, DocumentationProvider documentationProvider, string filePath, bool isLinked, MetadataImportOptions importOptions)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(documentationProvider != null);

            _assembly = assembly;
            _documentationProvider = documentationProvider;
            _filePath = filePath;

            var modules = new ModuleSymbol[assembly.Modules.Length];

            for (int i = 0; i < assembly.Modules.Length; i++)
            {
                modules[i] = new PEModuleSymbol(this, assembly.Modules[i], importOptions, i);
            }

            _modules = modules.AsImmutableOrNull();
            _isLinked = isLinked;

            if (IsPchpCor(assembly))
            {
                _specialAssembly = SpecialAssembly.PchpCorLibrary;

                // initialize CoreTypes
                this.PrimaryModule.GlobalNamespace.GetTypeMembers();
            }
            else if (assembly.Identity.Name == "System.Runtime")
            {
                _specialAssembly = SpecialAssembly.CorLibrary;
            }
            else if (assembly.AssemblyReferences.Length == 0 && assembly.DeclaresTheObjectClass)
            {
                _specialAssembly = SpecialAssembly.CorLibrary;
            }
            else
            {
                // extension assembly ?
                //var attrs = this.GetAttributes();
            }
        }

        internal static bool IsPchpCor(PEAssembly ass) => ass.Identity.Name == "Peachpie.Runtime";

        internal static PEAssemblySymbol Create(PortableExecutableReference reference, PEAssembly ass = null, bool isLinked = true)
        {
            if (ass == null)
            {
                ass = ((AssemblyMetadata)reference.GetMetadata()).GetAssembly();
            }

            return new PEAssemblySymbol(
                ass, reference.DocumentationProvider, reference.FilePath, isLinked,
                IsPchpCor(ass) ? MetadataImportOptions.Internal : MetadataImportOptions.Public);
        }

        /// <summary>
        /// Look up the assembly to which the given metadata type is forwarded.
        /// </summary>
        /// <param name="emittedName"></param>
        /// <returns>
        /// The assembly to which the given type is forwarded or null, if there isn't one.
        /// </returns>
        /// <remarks>
        /// The returned assembly may also forward the type.
        /// </remarks>
        internal AssemblySymbol LookupAssemblyForForwardedMetadataType(ref MetadataTypeName emittedName)
        {
            // Look in the type forwarders of the primary module of this assembly, clr does not honor type forwarder
            // in non-primary modules.

            // Examine the type forwarders, but only from the primary module.
            return this.PrimaryModule.GetAssemblyForForwardedType(ref emittedName);
        }

        internal override NamedTypeSymbol TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies)
        {
            // Check if it is a forwarded type.
            var forwardedToAssembly = LookupAssemblyForForwardedMetadataType(ref emittedName);
            if ((object)forwardedToAssembly != null)
            {
                // Don't bother to check the forwarded-to assembly if we've already seen it.
                if (visitedAssemblies != null && visitedAssemblies.Contains(forwardedToAssembly))
                {
                    return CreateCycleInTypeForwarderErrorTypeSymbol(ref emittedName);
                }
                else
                {
                    visitedAssemblies = new ConsList<AssemblySymbol>(this, visitedAssemblies ?? ConsList<AssemblySymbol>.Empty);
                    return forwardedToAssembly.LookupTopLevelMetadataTypeWithCycleDetection(ref emittedName, visitedAssemblies, digThroughForwardedTypes: true);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets containers which members represent globals in source module context.
        /// </summary>
        internal ImmutableArray<NamedTypeSymbol> ExtensionContainers
        {
            get
            {
                if (_lazyExtensionContainers.IsDefault)
                {
                    if (this.IsExtensionLibrary)
                    {
                        var containers = new ArrayBuilder<NamedTypeSymbol>();
                        containers.AddRange(
                            this.PrimaryModule.GlobalNamespace
                            .GetTypeMembers()
                            .Where(t => t.IsStatic && t.DeclaredAccessibility == Accessibility.Public && !t.IsPhpHidden()));

                        //
                        _lazyExtensionContainers = containers.ToImmutable();
                    }
                    else
                    {
                        _lazyExtensionContainers = ImmutableArray<NamedTypeSymbol>.Empty;
                    }
                }

                return _lazyExtensionContainers;
            }
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                //if (this.MightContainExtensionMethods)
                //{
                //    this.PrimaryModule.LoadCustomAttributesFilterExtensions(_assembly.Handle,
                //        ref _lazyCustomAttributes);
                //}
                //else
                {
                    this.PrimaryModule.LoadCustomAttributes(_assembly.Handle,
                        ref _lazyCustomAttributes);
                }
            }
            return _lazyCustomAttributes;
        }
    }
}
