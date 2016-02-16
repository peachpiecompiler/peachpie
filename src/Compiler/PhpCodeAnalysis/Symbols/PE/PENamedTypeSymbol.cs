using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// The class to represent all types imported from a PE/module.
    /// </summary>
    internal abstract class PENamedTypeSymbol : NamedTypeSymbol
    {
        #region PENamedTypeSymbolNonGeneric

        /// <summary>
        /// Specialized PENamedTypeSymbol for types with no type parameters in
        /// metadata (no type parameters on this type and all containing types).
        /// </summary>
        private sealed class PENamedTypeSymbolNonGeneric : PENamedTypeSymbol
        {
            internal PENamedTypeSymbolNonGeneric(
                PEModuleSymbol moduleSymbol,
                NamespaceOrTypeSymbol container,
                TypeDefinitionHandle handle,
                string emittedNamespaceName,
                out bool mangleName) :
                base(moduleSymbol, container, handle, emittedNamespaceName, 0, out mangleName)
            {
            }

            public override int Arity
            {
                get
                {
                    return 0;
                }
            }

            internal override bool MangleName
            {
                get
                {
                    return false;
                }
            }

            internal override int MetadataArity
            {
                get
                {
                    var containingType = _container as PENamedTypeSymbol;
                    return (object)containingType == null ? 0 : containingType.MetadataArity;
                }
            }
        }

        #endregion

        #region PENamedTypeSymbolGeneric

        /// <summary>
        /// Specialized PENamedTypeSymbol for types with type parameters in metadata.
        /// NOTE: the type may have Arity == 0 if it has same metadata arity as the metadata arity of the containing type.
        /// </summary>
        private sealed class PENamedTypeSymbolGeneric : PENamedTypeSymbol
        {
            private readonly GenericParameterHandleCollection _genericParameterHandles;
            private readonly ushort _arity;
            private readonly bool _mangleName;
            private ImmutableArray<ITypeParameterSymbol> _lazyTypeParameters;

            internal PENamedTypeSymbolGeneric(
                    PEModuleSymbol moduleSymbol,
                    NamespaceOrTypeSymbol container,
                    TypeDefinitionHandle handle,
                    string emittedNamespaceName,
                    GenericParameterHandleCollection genericParameterHandles,
                    ushort arity,
                    out bool mangleName
                )
                : base(moduleSymbol,
                      container,
                      handle,
                      emittedNamespaceName,
                      arity,
                      out mangleName)
            {
                Debug.Assert(genericParameterHandles.Count > 0);
                _arity = arity;
                _genericParameterHandles = genericParameterHandles;
                _mangleName = mangleName;
            }

            public override int Arity
            {
                get
                {
                    return _arity;
                }
            }

            internal override bool MangleName
            {
                get
                {
                    return _mangleName;
                }
            }

            override internal int MetadataArity
            {
                get
                {
                    return _genericParameterHandles.Count;
                }
            }

            //internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
            //{
            //    get
            //    {
            //        // This is always the instance type, so the type arguments are the same as the type parameters.
            //        return this.TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>();
            //    }
            //}

            //internal override bool HasTypeArgumentsCustomModifiers
            //{
            //    get
            //    {
            //        return false;
            //    }
            //}

            //internal override ImmutableArray<ImmutableArray<CustomModifier>> TypeArgumentsCustomModifiers
            //{
            //    get
            //    {
            //        return CreateEmptyTypeArgumentsCustomModifiers();
            //    }
            //}

            public override ImmutableArray<ITypeParameterSymbol> TypeParameters
            {
                get
                {
                    EnsureTypeParametersAreLoaded();
                    return _lazyTypeParameters;
                }
            }

            private void EnsureTypeParametersAreLoaded()
            {
                if (_lazyTypeParameters.IsDefault)
                {
                    var moduleSymbol = ContainingPEModule;

                    // If this is a nested type generic parameters in metadata include generic parameters of the outer types.
                    int firstIndex = _genericParameterHandles.Count - _arity;

                    var ownedParams = new ITypeParameterSymbol[_arity];
                    for (int i = 0; i < ownedParams.Length; i++)
                    {
                        throw new NotImplementedException();
                        //ownedParams[i] = new PETypeParameterSymbol(moduleSymbol, this, (ushort)i, _genericParameterHandles[firstIndex + i]);
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameters,
                        ImmutableArray.Create<ITypeParameterSymbol>(ownedParams));
                }
            }

            //protected override DiagnosticInfo GetUseSiteDiagnosticImpl()
            //{
            //    DiagnosticInfo diagnostic = null;

            //    if (!MergeUseSiteDiagnostics(ref diagnostic, base.GetUseSiteDiagnosticImpl()))
            //    {
            //        // Verify type parameters for containing types
            //        // match those on the containing types.
            //        if (!MatchesContainingTypeParameters())
            //        {
            //            diagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this);
            //        }
            //    }

            //    return diagnostic;
            //}

            ///// <summary>
            ///// Return true if the type parameters specified on the nested type (this),
            ///// that represent the corresponding type parameters on the containing
            ///// types, in fact match the actual type parameters on the containing types.
            ///// </summary>
            //private bool MatchesContainingTypeParameters()
            //{
            //    var container = this.ContainingType;
            //    if ((object)container == null)
            //    {
            //        return true;
            //    }

            //    var containingTypeParameters = container.GetAllTypeParameters();
            //    int n = containingTypeParameters.Length;

            //    if (n == 0)
            //    {
            //        return true;
            //    }

            //    // Create an instance of PENamedTypeSymbol for the nested type, but
            //    // with all type parameters, from the nested type and all containing
            //    // types. The type parameters on this temporary type instance are used
            //    // for comparison with those on the actual containing types. The
            //    // containing symbol for the temporary type is the namespace directly.
            //    var nestedType = Create(this.ContainingPEModule, (PENamespaceSymbol)this.ContainingNamespace, _handle, null);
            //    var nestedTypeParameters = nestedType.TypeParameters;
            //    var containingTypeMap = new TypeMap(containingTypeParameters, IndexedTypeParameterSymbol.Take(n), allowAlpha: false);
            //    var nestedTypeMap = new TypeMap(nestedTypeParameters, IndexedTypeParameterSymbol.Take(nestedTypeParameters.Length), allowAlpha: false);

            //    for (int i = 0; i < n; i++)
            //    {
            //        var containingTypeParameter = containingTypeParameters[i];
            //        var nestedTypeParameter = nestedTypeParameters[i];
            //        if (!MemberSignatureComparer.HaveSameConstraints(containingTypeParameter, containingTypeMap, nestedTypeParameter, nestedTypeMap))
            //        {
            //            return false;
            //        }
            //    }

            //    return true;
            //}
        }

        #endregion

        internal static NamedTypeSymbol Create(PEModuleSymbol moduleSymbol, PENamespaceSymbol containingNamespace, TypeDefinitionHandle handle, string emittedNamespaceName)
        {
            var genericParameterHandles = moduleSymbol.Module.GetTypeDefGenericParamsOrThrow(handle);
            ushort arity = (ushort)genericParameterHandles.Count;

            bool mangleName;
            
            if (arity == 0)
            {
                return new PENamedTypeSymbolNonGeneric(moduleSymbol, containingNamespace, handle, emittedNamespaceName, out mangleName);
            }
            else
            {
                return new PENamedTypeSymbolGeneric(
                    moduleSymbol,
                    containingNamespace,
                    handle,
                    emittedNamespaceName,
                    genericParameterHandles,
                    arity,
                    out mangleName);
            }
        }

        TypeDefinitionHandle _handle;
        NamespaceOrTypeSymbol _container;
        TypeAttributes _flags;
        string _name;
        SpecialType _corTypeId;
        TypeKind _lazyKind;

        private PENamedTypeSymbol(
            PEModuleSymbol moduleSymbol,
            NamespaceOrTypeSymbol container,
            TypeDefinitionHandle handle,
            string emittedNamespaceName,
            ushort arity,
            out bool mangleName)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert((object)container != null);
            Debug.Assert(arity == 0 || this is PENamedTypeSymbolGeneric);

            string metadataName;
            //bool makeBad = false;

            metadataName = moduleSymbol.Module.GetTypeDefNameOrThrow(handle);
            
            _handle = handle;
            _container = container;

            try
            {
                _flags = moduleSymbol.Module.GetTypeDefFlagsOrThrow(handle);
            }
            catch (BadImageFormatException)
            {
                throw; // makeBad = true;
            }

            if (arity == 0)
            {
                _name = metadataName;
                mangleName = false;
            }
            else
            {
                // Unmangle name for a generic type.
                _name = MetadataHelpers.UnmangleMetadataNameForArity(metadataName, arity);
                Debug.Assert(ReferenceEquals(_name, metadataName) == (_name == metadataName));
                mangleName = !ReferenceEquals(_name, metadataName);
            }

            // check if this is one of the COR library types
            if (emittedNamespaceName != null &&
                //moduleSymbol.ContainingAssembly.KeepLookingForDeclaredSpecialTypes &&
                this.DeclaredAccessibility == Accessibility.Public) // NB: this.flags was set above.
            {
                _corTypeId = SpecialTypes.GetTypeFromMetadataName(MetadataHelpers.BuildQualifiedName(emittedNamespaceName, metadataName));
            }
            else
            {
                _corTypeId = SpecialType.None;
            }

            //if (makeBad)
            //{
            //    _lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this);
            //}
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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
                return null;
            }
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            throw new NotImplementedException();
        }

        public override SpecialType SpecialType => _corTypeId;

        internal PEModuleSymbol ContainingPEModule
        {
            get
            {
                Symbol s = _container;

                while (s.Kind != SymbolKind.Namespace)
                {
                    s = s.ContainingSymbol;
                }

                return ((PENamespaceSymbol)s).ContainingPEModule;
            }
        }
        
        internal override IModuleSymbol ContainingModule
        {
            get
            {
                return ContainingPEModule;
            }
        }

        public override Symbol ContainingSymbol => _container;

        public override INamedTypeSymbol ContainingType => _container as NamedTypeSymbol;

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                Accessibility access = Accessibility.Private;

                switch (_flags & TypeAttributes.VisibilityMask)
                {
                    case TypeAttributes.NestedAssembly:
                        access = Accessibility.Internal;
                        break;

                    case TypeAttributes.NestedFamORAssem:
                        access = Accessibility.ProtectedOrInternal;
                        break;

                    case TypeAttributes.NestedFamANDAssem:
                        access = Accessibility.ProtectedAndInternal;
                        break;

                    case TypeAttributes.NestedPrivate:
                        access = Accessibility.Private;
                        break;

                    case TypeAttributes.Public:
                    case TypeAttributes.NestedPublic:
                        access = Accessibility.Public;
                        break;

                    case TypeAttributes.NestedFamily:
                        access = Accessibility.Protected;
                        break;

                    case TypeAttributes.NotPublic:
                        access = Accessibility.Internal;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_flags & TypeAttributes.VisibilityMask);
                }

                return access;
            }
        }

        public abstract override int Arity
        {
            get;
        }

        internal abstract int MetadataArity
        {
            get;
        }

        //internal override bool HasSpecialName
        //{
        //    get
        //    {
        //        return (_flags & TypeAttributes.SpecialName) != 0;
        //    }
        //}

        internal TypeDefinitionHandle Handle => _handle;
        public override IEnumerable<string> MemberNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            throw new NotImplementedException();
        }

        private MultiDictionary<string, IFieldSymbol> CreateFields(ArrayBuilder<IFieldSymbol> fieldMembers)
        {
            //var privateFieldNameToSymbols = new MultiDictionary<string, PEFieldSymbol>();

            //var moduleSymbol = this.ContainingPEModule;
            //var module = moduleSymbol.Module;

            //// for ordinary struct types we import private fields so that we can distinguish empty structs from non-empty structs
            //var isOrdinaryStruct = false;
            //// for ordinary embeddable struct types we import private members so that we can report appropriate errors if the structure is used 
            //var isOrdinaryEmbeddableStruct = false;

            //if (this.TypeKind == TypeKind.Struct)
            //{
            //    if (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.None)
            //    {
            //        isOrdinaryStruct = true;
            //        isOrdinaryEmbeddableStruct = this.ContainingAssembly.IsLinked;
            //    }
            //    else
            //    {
            //        isOrdinaryStruct = (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Nullable_T);
            //    }
            //}

            //try
            //{
            //    foreach (var fieldRid in module.GetFieldsOfTypeOrThrow(_handle))
            //    {
            //        try
            //        {
            //            if (!(isOrdinaryEmbeddableStruct ||
            //                (isOrdinaryStruct && (module.GetFieldDefFlagsOrThrow(fieldRid) & FieldAttributes.Static) == 0) ||
            //                module.ShouldImportField(fieldRid, moduleSymbol.ImportOptions)))
            //            {
            //                continue;
            //            }
            //        }
            //        catch (BadImageFormatException)
            //        { }

            //        var symbol = new PEFieldSymbol(moduleSymbol, this, fieldRid);
            //        fieldMembers.Add(symbol);

            //        // Only private fields are potentially backing fields for field-like events.
            //        if (symbol.DeclaredAccessibility == Accessibility.Private)
            //        {
            //            var name = symbol.Name;
            //            if (name.Length > 0)
            //            {
            //                privateFieldNameToSymbols.Add(name, symbol);
            //            }
            //        }
            //    }
            //}
            //catch (BadImageFormatException)
            //{ }

            //return privateFieldNameToSymbols;
            throw new NotImplementedException();
        }

        private PooledDictionary<MethodDefinitionHandle, IMethodSymbol> CreateMethods(ArrayBuilder<Symbol> members)
        {
            //var moduleSymbol = this.ContainingPEModule;
            //var module = moduleSymbol.Module;
            //var map = PooledDictionary<MethodDefinitionHandle, PEMethodSymbol>.GetInstance();

            //// for ordinary embeddable struct types we import private members so that we can report appropriate errors if the structure is used 
            //var isOrdinaryEmbeddableStruct = (this.TypeKind == TypeKind.Struct) && (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.None) && this.ContainingAssembly.IsLinked;

            //try
            //{
            //    foreach (var methodHandle in module.GetMethodsOfTypeOrThrow(_handle))
            //    {
            //        if (isOrdinaryEmbeddableStruct || module.ShouldImportMethod(methodHandle, moduleSymbol.ImportOptions))
            //        {
            //            var method = new PEMethodSymbol(moduleSymbol, this, methodHandle);
            //            members.Add(method);
            //            map.Add(methodHandle, method);
            //        }
            //    }
            //}
            //catch (BadImageFormatException)
            //{ }

            //return map;
            throw new NotImplementedException();
        }

        private void CreateProperties(Dictionary<MethodDefinitionHandle, IMethodSymbol> methodHandleToSymbol, ArrayBuilder<Symbol> members)
        {
            //var moduleSymbol = this.ContainingPEModule;
            //var module = moduleSymbol.Module;

            //try
            //{
            //    foreach (var propertyDef in module.GetPropertiesOfTypeOrThrow(_handle))
            //    {
            //        try
            //        {
            //            var methods = module.GetPropertyMethodsOrThrow(propertyDef);

            //            PEMethodSymbol getMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Getter);
            //            PEMethodSymbol setMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Setter);

            //            if (((object)getMethod != null) || ((object)setMethod != null))
            //            {
            //                members.Add(new PEPropertySymbol(moduleSymbol, this, propertyDef, getMethod, setMethod));
            //            }
            //        }
            //        catch (BadImageFormatException)
            //        { }
            //    }
            //}
            //catch (BadImageFormatException)
            //{ }
            throw new NotImplementedException();
        }

        private void CreateEvents(
            MultiDictionary<string, IFieldSymbol> privateFieldNameToSymbols,
            Dictionary<MethodDefinitionHandle, IMethodSymbol> methodHandleToSymbol,
            ArrayBuilder<Symbol> members)
        {
            //var moduleSymbol = this.ContainingPEModule;
            //var module = moduleSymbol.Module;

            //try
            //{
            //    foreach (var eventRid in module.GetEventsOfTypeOrThrow(_handle))
            //    {
            //        try
            //        {
            //            var methods = module.GetEventMethodsOrThrow(eventRid);

            //            // NOTE: C# ignores all other accessors (most notably, raise/fire).
            //            PEMethodSymbol addMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Adder);
            //            PEMethodSymbol removeMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Remover);

            //            // NOTE: both accessors are required, but that will be reported separately.
            //            // Create the symbol unless both accessors are missing.
            //            if (((object)addMethod != null) || ((object)removeMethod != null))
            //            {
            //                members.Add(new PEEventSymbol(moduleSymbol, this, eventRid, addMethod, removeMethod, privateFieldNameToSymbols));
            //            }
            //        }
            //        catch (BadImageFormatException)
            //        { }
            //    }
            //}
            //catch (BadImageFormatException)
            //{ }
            throw new NotImplementedException();
        }

        internal override Microsoft.CodeAnalysis.TypeLayout Layout
        {
            get
            {
                return this.ContainingPEModule.Module.GetTypeLayout(_handle);
            }
        }

        public override bool IsStatic
        {
            get
            {
                return
                    (_flags & TypeAttributes.Sealed) != 0 &&
                    (_flags & TypeAttributes.Abstract) != 0;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return
                    (_flags & TypeAttributes.Abstract) != 0 &&
                    (_flags & TypeAttributes.Sealed) == 0;
            }
        }

        internal override bool IsMetadataAbstract
        {
            get
            {
                return (_flags & TypeAttributes.Abstract) != 0;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return
                    (_flags & TypeAttributes.Sealed) != 0 &&
                    (_flags & TypeAttributes.Abstract) == 0;
            }
        }

        internal override bool IsMetadataSealed
        {
            get
            {
                return (_flags & TypeAttributes.Sealed) != 0;
            }
        }

        internal TypeAttributes Flags
        {
            get
            {
                return _flags;
            }
        }

        internal NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            //if (ReferenceEquals(_lazyDeclaredBaseType, ErrorTypeSymbol.UnknownResultType))
            //{
            //    Interlocked.CompareExchange(ref _lazyDeclaredBaseType, MakeDeclaredBaseType(), ErrorTypeSymbol.UnknownResultType);
            //}

            //return _lazyDeclaredBaseType;
            throw new NotImplementedException();
        }

        private NamedTypeSymbol MakeDeclaredBaseType()
        {
            if (!_flags.IsInterface())
            {
                try
                {
                    var moduleSymbol = ContainingPEModule;
                    EntityHandle token = moduleSymbol.Module.GetBaseTypeOfTypeOrThrow(_handle);

                    if (!token.IsNil)
                    {
                        //TypeSymbol decodedType = new MetadataDecoder(moduleSymbol, this).GetTypeOfToken(token);
                        //return (NamedTypeSymbol)DynamicTypeDecoder.TransformType(decodedType, 0, _handle, moduleSymbol);
                        throw new NotImplementedException();
                    }
                }
                catch (BadImageFormatException)
                {
                    throw;
                    //return new UnsupportedMetadataTypeSymbol(mrEx);
                }
            }

            return null;
        }

        public override TypeKind TypeKind
        {
            get
            {
                if (_lazyKind == TypeKind.Unknown)
                {
                    TypeKind result;

                    if (_flags.IsInterface())
                    {
                        result = TypeKind.Interface;
                    }
                    else
                    {
                        TypeSymbol @base = GetDeclaredBaseType(null);

                        result = TypeKind.Class;

                        if ((object)@base != null)
                        {
                            SpecialType baseCorTypeId = @base.SpecialType;

                            // Code is cloned from MetaImport::DoImportBaseAndImplements()
                            if (baseCorTypeId == SpecialType.System_Enum)
                            {
                                // Enum
                                result = TypeKind.Enum;
                            }
                            else if (baseCorTypeId == SpecialType.System_MulticastDelegate)
                            {
                                // Delegate
                                result = TypeKind.Delegate;
                            }
                            else if (baseCorTypeId == SpecialType.System_ValueType &&
                                     this.SpecialType != SpecialType.System_Enum)
                            {
                                // Struct
                                result = TypeKind.Struct;
                            }
                        }
                    }

                    _lazyKind = result;
                }

                return _lazyKind;
            }
        }
    }
}
