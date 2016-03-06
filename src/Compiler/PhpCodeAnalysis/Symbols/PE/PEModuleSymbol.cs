using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed partial class PEModuleSymbol : ModuleSymbol
    {
        /// <summary>
        /// Owning AssemblySymbol. This can be a PEAssemblySymbol or a SourceAssemblySymbol.
        /// </summary>
        readonly PEAssemblySymbol _assembly;

        private readonly int _ordinal;

        /// <summary>
        /// A Module object providing metadata.
        /// </summary>
        readonly PEModule _module;

        ///// <summary>
        ///// Global namespace.
        ///// </summary>
        readonly PENamespaceSymbol _namespace;

        /// <summary>
        /// The same value as ConcurrentDictionary.DEFAULT_CAPACITY
        /// </summary>
        private const int DefaultTypeMapCapacity = 31;

        /// <summary>
        /// This is a map from TypeDef handle to the target <see cref="TypeSymbol"/>. 
        /// It is used by <see cref="MetadataDecoder"/> to speed up type reference resolution
        /// for metadata coming from this module. The map is lazily populated
        /// as we load types from the module.
        /// </summary>
        internal readonly ConcurrentDictionary<TypeDefinitionHandle, TypeSymbol> TypeHandleToTypeMap =
                                    new ConcurrentDictionary<TypeDefinitionHandle, TypeSymbol>(concurrencyLevel: 2, capacity: DefaultTypeMapCapacity);

        /// <summary>
        /// This is a map from TypeRef row id to the target <see cref="TypeSymbol"/>. 
        /// It is used by <see cref="MetadataDecoder"/> to speed up type reference resolution
        /// for metadata coming from this module. The map is lazily populated
        /// by <see cref="MetadataDecoder"/> as we resolve TypeRefs from the module.
        /// </summary>
        internal readonly ConcurrentDictionary<TypeReferenceHandle, TypeSymbol> TypeRefHandleToTypeMap =
                                    new ConcurrentDictionary<TypeReferenceHandle, TypeSymbol>(concurrencyLevel: 2, capacity: DefaultTypeMapCapacity);


        public PEModuleSymbol(PEAssemblySymbol assembly, PEModule module, MetadataImportOptions importOptions, int ordinal)
        {
            _assembly = assembly;
            _module = module;
            _ordinal = ordinal;
            this.ImportOptions = importOptions;
            _namespace = new PEGlobalNamespaceSymbol(this);
        }

        internal readonly MetadataImportOptions ImportOptions;

        public PEModule Module => _module;

        public override NamespaceSymbol GlobalNamespace => _namespace;

        public override string Name => _module.Name;

        //private static EntityHandle Token
        //{
        //    get
        //    {
        //        return EntityHandle.ModuleDefinition;
        //    }
        //}

        public override AssemblySymbol ContainingAssembly => _assembly;

        public override Symbol ContainingSymbol => _assembly;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return ObsoleteAttributeData.Uninitialized; // throw new NotImplementedException();
            }
        }

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
            NamespaceSymbol scope = this.GlobalNamespace;//.LookupNestedNamespace(emittedName.NamespaceSegments);

            if ((object)scope == null)
            {
                // We failed to locate the namespace
                //result = new MissingMetadataTypeSymbol.TopLevel(this, ref emittedName);
                throw new NotImplementedException();
            }
            else
            {
                result = scope.LookupMetadataType(ref emittedName);
            }

            Debug.Assert((object)result != null);
            return result;
        }

        /// <summary>
        /// If this module forwards the given type to another assembly, return that assembly;
        /// otherwise, return null.
        /// </summary>
        /// <param name="fullName">Type to look up.</param>
        /// <returns>Assembly symbol or null.</returns>
        /// <remarks>
        /// The returned assembly may also forward the type.
        /// </remarks>
        internal AssemblySymbol GetAssemblyForForwardedType(ref MetadataTypeName fullName)
        {
            try
            {
                string matchedName;
                AssemblyReferenceHandle assemblyRef = Module.GetAssemblyForForwardedType(fullName.FullName, ignoreCase: false, matchedName: out matchedName);
                return assemblyRef.IsNil ? null : this.ReferencedAssemblySymbols[Module.GetAssemblyReferenceIndexOrThrow(assemblyRef)];
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }
    }
}
