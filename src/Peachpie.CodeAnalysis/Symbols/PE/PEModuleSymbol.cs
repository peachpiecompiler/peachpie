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
using Cci = Microsoft.Cci;

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
        /// Module's custom attributes
        /// </summary>
        private ImmutableArray<AttributeData> _lazyCustomAttributes;

        /// <summary>
        /// Module's assembly attributes
        /// </summary>
        private ImmutableArray<AttributeData> _lazyAssemblyAttributes;

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

        private static EntityHandle Token
        {
            get
            {
                return EntityHandle.ModuleDefinition;
            }
        }

        public override AssemblySymbol ContainingAssembly => _assembly;

        public override Symbol ContainingSymbol => _assembly;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal DocumentationProvider DocumentationProvider
        {
            get
            {
                var assembly = _assembly as PEAssemblySymbol;
                if ((object)assembly != null)
                {
                    return assembly.DocumentationProvider;
                }
                else
                {
                    return DocumentationProvider.Default;
                }
            }
        }

        #region Custom Attributes

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                this.LoadCustomAttributes(Token, ref _lazyCustomAttributes);
            }
            return _lazyCustomAttributes;
        }

        internal ImmutableArray<AttributeData> GetAssemblyAttributes()
        {
            if (_lazyAssemblyAttributes.IsDefault)
            {
                ArrayBuilder<AttributeData> moduleAssemblyAttributesBuilder = null;

                string corlibName = ContainingAssembly.CorLibrary.Name;
                EntityHandle assemblyMSCorLib = Module.GetAssemblyRef(corlibName);
                if (!assemblyMSCorLib.IsNil)
                {
                    foreach (var qualifier in Cci.MetadataWriter.dummyAssemblyAttributeParentQualifier)
                    {
                        EntityHandle typerefAssemblyAttributesGoHere =
                                    Module.GetTypeRef(
                                        assemblyMSCorLib,
                                        Cci.MetadataWriter.dummyAssemblyAttributeParentNamespace,
                                        Cci.MetadataWriter.dummyAssemblyAttributeParentName + qualifier);

                        if (!typerefAssemblyAttributesGoHere.IsNil)
                        {
                            try
                            {
                                foreach (var customAttributeHandle in Module.GetCustomAttributesOrThrow(typerefAssemblyAttributesGoHere))
                                {
                                    if (moduleAssemblyAttributesBuilder == null)
                                    {
                                        moduleAssemblyAttributesBuilder = new ArrayBuilder<AttributeData>();
                                    }
                                    moduleAssemblyAttributesBuilder.Add(new PEAttributeData(this, customAttributeHandle));
                                }
                            }
                            catch (BadImageFormatException)
                            { }
                        }
                    }
                }

                ImmutableInterlocked.InterlockedCompareExchange(
                    ref _lazyAssemblyAttributes,
                    (moduleAssemblyAttributesBuilder != null) ? moduleAssemblyAttributesBuilder.ToImmutableAndFree() : ImmutableArray<AttributeData>.Empty,
                    default(ImmutableArray<AttributeData>));
            }
            return _lazyAssemblyAttributes;
        }

        internal void LoadCustomAttributes(EntityHandle token, ref ImmutableArray<AttributeData> customAttributes)
        {
            var loaded = GetCustomAttributesForToken(token);
            ImmutableInterlocked.InterlockedInitialize(ref customAttributes, loaded);
        }

        internal void LoadCustomAttributesFilterExtensions(EntityHandle token,
            ref ImmutableArray<AttributeData> customAttributes,
            out bool foundExtension)
        {
            var loadedCustomAttributes = GetCustomAttributesFilterExtensions(token, out foundExtension);
            ImmutableInterlocked.InterlockedInitialize(ref customAttributes, loadedCustomAttributes);
        }

        internal void LoadCustomAttributesFilterExtensions(EntityHandle token,
            ref ImmutableArray<AttributeData> customAttributes)
        {
            // Ignore whether or not extension attributes were found
            bool ignore;
            var loadedCustomAttributes = GetCustomAttributesFilterExtensions(token, out ignore);
            ImmutableInterlocked.InterlockedInitialize(ref customAttributes, loadedCustomAttributes);
        }

        /// <summary>
        /// Filters extension attributes from the attribute results.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="foundExtension">True if we found an extension method, false otherwise.</param>
        /// <returns>The attributes on the token, minus any ExtensionAttributes.</returns>
        internal ImmutableArray<AttributeData> GetCustomAttributesFilterExtensions(EntityHandle token, out bool foundExtension)
        {
            CustomAttributeHandle extensionAttribute;
            CustomAttributeHandle ignore;
            var result = GetCustomAttributesForToken(token,
                out extensionAttribute,
                AttributeDescription.CaseSensitiveExtensionAttribute,
                out ignore,
                default(AttributeDescription));

            foundExtension = !extensionAttribute.IsNil;
            return result;
        }

        /// <summary>
        /// Returns a possibly ExtensionAttribute filtered roArray of attributes. If
        /// filterExtensionAttributes is set to true, the method will remove all ExtensionAttributes
        /// from the returned array. If it is false, the parameter foundExtension will always be set to
        /// false and can be safely ignored.
        /// 
        /// The paramArrayAttribute parameter is similar to the foundExtension parameter, but instead
        /// of just indicating if the attribute was found, the parameter is set to the attribute handle
        /// for the ParamArrayAttribute if any is found and is null otherwise. This allows NoPia to filter
        /// the attribute out for the symbol but still cache it separately for emit.
        /// </summary>
        internal ImmutableArray<AttributeData> GetCustomAttributesForToken(EntityHandle token,
            out CustomAttributeHandle filteredOutAttribute1,
            AttributeDescription filterOut1,
            out CustomAttributeHandle filteredOutAttribute2,
            AttributeDescription filterOut2)
        {
            filteredOutAttribute1 = default(CustomAttributeHandle);
            filteredOutAttribute2 = default(CustomAttributeHandle);
            ArrayBuilder<AttributeData> customAttributesBuilder = null;

            try
            {
                foreach (var customAttributeHandle in _module.GetCustomAttributesOrThrow(token))
                {
                    if (filterOut1.Signatures != null &&
                        Module.GetTargetAttributeSignatureIndex(customAttributeHandle, filterOut1) != -1)
                    {
                        // It is important to capture the last application of the attribute that we run into,
                        // it makes a difference for default and constant values.
                        filteredOutAttribute1 = customAttributeHandle;
                        continue;
                    }

                    if (filterOut2.Signatures != null &&
                        Module.GetTargetAttributeSignatureIndex(customAttributeHandle, filterOut2) != -1)
                    {
                        // It is important to capture the last application of the attribute that we run into,
                        // it makes a difference for default and constant values.
                        filteredOutAttribute2 = customAttributeHandle;
                        continue;
                    }

                    if (customAttributesBuilder == null)
                    {
                        customAttributesBuilder = ArrayBuilder<AttributeData>.GetInstance();
                    }

                    customAttributesBuilder.Add(new PEAttributeData(this, customAttributeHandle));
                }
            }
            catch (BadImageFormatException)
            { }

            if (customAttributesBuilder != null)
            {
                return customAttributesBuilder.ToImmutableAndFree();
            }

            return ImmutableArray<AttributeData>.Empty;
        }

        internal ImmutableArray<AttributeData> GetCustomAttributesForToken(EntityHandle token)
        {
            // Do not filter anything and therefore ignore the out results
            CustomAttributeHandle ignore1;
            CustomAttributeHandle ignore2;
            return GetCustomAttributesForToken(token,
                out ignore1,
                default(AttributeDescription),
                out ignore2,
                default(AttributeDescription));
        }

        /// <summary>
        /// Get the custom attributes, but filter out any ParamArrayAttributes.
        /// </summary>
        /// <param name="token">The parameter token handle.</param>
        /// <param name="paramArrayAttribute">Set to a ParamArrayAttribute</param>
        /// CustomAttributeHandle if any are found. Nil token otherwise.
        internal ImmutableArray<AttributeData> GetCustomAttributesForToken(EntityHandle token,
            out CustomAttributeHandle paramArrayAttribute)
        {
            CustomAttributeHandle ignore;
            return GetCustomAttributesForToken(
                token,
                out paramArrayAttribute,
                AttributeDescription.ParamArrayAttribute,
                out ignore,
                default(AttributeDescription));
        }


        internal bool HasAnyCustomAttributes(EntityHandle token)
        {
            try
            {
                foreach (var attr in _module.GetCustomAttributesOrThrow(token))
                {
                    return true;
                }
            }
            catch (BadImageFormatException)
            { }

            return false;
        }

        #endregion

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

            Debug.Assert(result != null);
            return result;
        }

        private NamedTypeSymbol GetTypeSymbolForWellKnownType(WellKnownType type)
        {
            MetadataTypeName emittedName = MetadataTypeName.FromFullName(type.GetMetadataName(), useCLSCompliantNameArityEncoding: true);
            // First, check this module
            NamedTypeSymbol currentModuleResult = this.LookupTopLevelMetadataType(ref emittedName);

            if (IsAcceptableSystemTypeSymbol(currentModuleResult))
            {
                // It doesn't matter if there's another of this type in a referenced assembly -
                // we prefer the one in the current module.
                return currentModuleResult;
            }

            // If we didn't find it in this module, check the referenced assemblies
            NamedTypeSymbol referencedAssemblyResult = null;
            foreach (AssemblySymbol assembly in this.ReferencedAssemblySymbols)
            {
                NamedTypeSymbol currResult = assembly.LookupTopLevelMetadataType(ref emittedName, digThroughForwardedTypes: true);
                if (IsAcceptableSystemTypeSymbol(currResult))
                {
                    if ((object)referencedAssemblyResult == null)
                    {
                        referencedAssemblyResult = currResult;
                    }
                    else
                    {
                        // CONSIDER: setting result to null will result in a MissingMetadataTypeSymbol 
                        // being returned.  Do we want to differentiate between no result and ambiguous
                        // results?  There doesn't seem to be an existing error code for "duplicate well-
                        // known type".
                        if (referencedAssemblyResult != currResult)
                        {
                            referencedAssemblyResult = null;
                        }
                        break;
                    }
                }
            }

            if ((object)referencedAssemblyResult != null)
            {
                Debug.Assert(IsAcceptableSystemTypeSymbol(referencedAssemblyResult));
                return referencedAssemblyResult;
            }

            Debug.Assert((object)currentModuleResult != null);
            return currentModuleResult;
        }

        private static bool IsAcceptableSystemTypeSymbol(NamedTypeSymbol candidate)
        {
            return candidate.Kind != SymbolKind.ErrorType || !(candidate is MissingMetadataTypeSymbol);
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
