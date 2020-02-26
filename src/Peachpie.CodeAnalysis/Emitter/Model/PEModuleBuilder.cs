using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Cci = Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit.NoPia;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Emitter;
using Pchp.CodeAnalysis.Utilities;
using Pchp.CodeAnalysis.CodeGen;

namespace Pchp.CodeAnalysis.Emit
{
    internal abstract partial class PEModuleBuilder : CommonPEModuleBuilder, ITokenDeferral
    {
        private readonly SourceModuleSymbol _sourceModule;
        private readonly PhpCompilation _compilation;
        private readonly EmitOptions _emitOptions;
        //private readonly Cci.ModulePropertiesForSerialization _serializationProperties;

        /// <summary>
        /// Gets script type containing entry point and additional assembly level symbols.
        /// </summary>
        internal SynthesizedScriptTypeSymbol ScriptType { get; }

        /// <summary>
        /// Manages synthesized methods and fields.
        /// </summary>
        public SynthesizedManager SynthesizedManager { get; }

        Cci.ICustomAttribute _debuggableAttribute, _phpextensionAttribute, _targetphpversionAttribute, _assemblyinformationalversionAttribute;

        protected readonly ConcurrentDictionary<Symbol, Cci.IModuleReference> AssemblyOrModuleSymbolToModuleRefMap = new ConcurrentDictionary<Symbol, Cci.IModuleReference>();
        readonly ConcurrentDictionary<Symbol, object> _genericInstanceMap = new ConcurrentDictionary<Symbol, object>();
        readonly PhpRootModuleType _rootModuleType = new PhpRootModuleType();
        PrivateImplementationDetails _privateImplementationDetails;
        HashSet<string> _namesOfTopLevelTypes;  // initialized with set of type names within first call to GetTopLevelTypes()

        internal readonly CommonModuleCompilationState CompilationState;

        /// <summary>
        /// Builders for synthesized static constructors.
        /// </summary>
        readonly ConcurrentDictionary<Cci.ITypeDefinition, ILBuilder> _cctorBuilders = new ConcurrentDictionary<Cci.ITypeDefinition, ILBuilder>(ReferenceEqualityComparer.Instance);

        protected PEModuleBuilder(
            PhpCompilation compilation,
            SourceModuleSymbol sourceModule,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            OutputKind outputKind,
            EmitOptions emitOptions)
            : base(manifestResources, emitOptions, outputKind, serializationProperties, compilation)
        {
            Debug.Assert(sourceModule != null);
            Debug.Assert(serializationProperties != null);

            _compilation = compilation;
            _sourceModule = sourceModule;
            _emitOptions = emitOptions;
            this.CompilationState = new CommonModuleCompilationState();
            this.SynthesizedManager = new SynthesizedManager(this);
            this.ScriptType = new SynthesizedScriptTypeSymbol(_compilation);

            //
            AssemblyOrModuleSymbolToModuleRefMap.Add(sourceModule, this);
        }


        #region PEModuleBuilder

        internal MetadataConstant CreateConstant(
            ITypeSymbol type,
            object value,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            return new MetadataConstant(Translate(type, syntaxNodeOpt, diagnostics), value);
        }

        #endregion

        #region Synthesized

        /// <summary>
        /// Gets enumeration of synthesized fields for <paramref name="container"/>.
        /// </summary>
        /// <param name="container">Containing type symbol.</param>
        /// <returns>Enumeration of synthesized fields.</returns>
        public IEnumerable<FieldSymbol> GetSynthesizedFields(Cci.ITypeDefinition container) => SynthesizedManager.GetMembers<FieldSymbol>(container);

        /// <summary>
        /// Gets enumeration of synthesized properties for <paramref name="container"/>.
        /// </summary>
        /// <param name="container">Containing type symbol.</param>
        /// <returns>Enumeration of synthesized properties.</returns>
        public IEnumerable<PropertySymbol> GetSynthesizedProperties(Cci.ITypeDefinition container) => SynthesizedManager.GetMembers<PropertySymbol>(container);

        /// <summary>
        /// Gets enumeration of synthesized methods for <paramref name="container"/>.
        /// </summary>
        /// <param name="container">Containing type symbol.</param>
        /// <returns>Enumeration of synthesized methods.</returns>
        public IEnumerable<MethodSymbol> GetSynthesizedMethods(Cci.ITypeDefinition container) => SynthesizedManager.GetMembers<MethodSymbol>(container);

        /// <summary>
        /// Gets enumeration of synthesized nested types for <paramref name="container"/>.
        /// </summary>
        /// <param name="container">Containing type symbol.</param>
        /// <returns>Enumeration of synthesized nested types.</returns>
        public IEnumerable<TypeSymbol> GetSynthesizedTypes(Cci.ITypeDefinition container) => SynthesizedManager.GetMembers<TypeSymbol>(container);

        #endregion

        internal SourceModuleSymbol SourceModule => _sourceModule;

        public ArrayMethods ArrayMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int CurrentGenerationOrdinal => 0; // used for EditAndContinue

        public sealed override IEnumerable<Cci.ICustomAttribute> GetSourceAssemblyAttributes(bool isRefAssembly)
        {
            // [Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations)]
            if (_compilation.Options.DebugPlusMode)
            {
                if (_debuggableAttribute == null)
                {
                    var debuggableAttrCtor = (MethodSymbol)this.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggableAttribute__ctorDebuggingModes);
                    _debuggableAttribute = new SynthesizedAttributeData(debuggableAttrCtor,
                        ImmutableArray.Create(new TypedConstant(Compilation.CoreTypes.Int32.Symbol, TypedConstantKind.Primitive, DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations)),
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }

                yield return _debuggableAttribute;
            }
            //if (targetfr == null)
            //{
            //    var TargetFrameworkType = (NamedTypeSymbol)this.Compilation.GetTypeByMetadataName("System.Runtime.Versioning.TargetFrameworkAttribute");

            //    targetfr = new SynthesizedAttributeData(TargetFrameworkType.Constructors[0],
            //        ImmutableArray.Create(new TypedConstant(Compilation.CoreTypes.String.Symbol, TypedConstantKind.Primitive, ".NETPortable,Version=v4.5,Profile=Profile7")),
            //        ImmutableArray.Create(new KeyValuePair<string, TypedConstant>("FrameworkDisplayName", new TypedConstant(Compilation.CoreTypes.String.Symbol, TypedConstantKind.Primitive, ".NET Portable Subset"))));
            //}

            //yield return targetfr;

            // [assembly: PhpExtension(new string[0])]
            if (_phpextensionAttribute == null)
            {
                var phpextensionAttributeCtor = this.Compilation.PhpCorLibrary.GetTypeByMetadataName(CoreTypes.PhpExtensionAttributeFullName).InstanceConstructors.First();
                _phpextensionAttribute = new SynthesizedAttributeData(phpextensionAttributeCtor,
                    ImmutableArray.Create(new TypedConstant(Compilation.CreateArrayTypeSymbol(Compilation.CoreTypes.String.Symbol), ImmutableArray<TypedConstant>.Empty)),  // string[] { }
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }
            yield return _phpextensionAttribute;

            // [assembly: TargetPhpLanguage(LanguageVersion : "7.0", ShortOpenTag : false)]
            if (_targetphpversionAttribute == null)
            {
                var targetphpversionAttribute = this.Compilation.PhpCorLibrary.GetTypeByMetadataName(CoreTypes.TargetPhpLanguageAttributeFullName);
                if (targetphpversionAttribute.IsErrorTypeOrNull() == false)
                {
                    var parseOptions = this.Compilation.Options.ParseOptions ?? PhpParseOptions.Default;
                    var targetphpversionAttributeCtor = this.Compilation.PhpCorLibrary.GetTypeByMetadataName(CoreTypes.TargetPhpLanguageAttributeFullName).InstanceConstructors.First();
                    _targetphpversionAttribute = new SynthesizedAttributeData(targetphpversionAttributeCtor,
                        ImmutableArray.Create(
                            Compilation.CreateTypedConstant(parseOptions.LanguageVersion?.ToString(2)),
                            Compilation.CreateTypedConstant(parseOptions.AllowShortOpenTags)),
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }
            }
            if (_targetphpversionAttribute != null)
            {
                yield return _targetphpversionAttribute;
            }

            // [assembly: AssemblyInformationalVersion( FileVersion )]
            if (Compilation.SourceAssembly.FileVersion != null)
            {
                if (_assemblyinformationalversionAttribute == null)
                {
                    var attr = (NamedTypeSymbol)this.Compilation.GetTypeByMetadataName("System.Reflection.AssemblyInformationalVersionAttribute");
                    if (attr != null)
                    {
                        _assemblyinformationalversionAttribute = new SynthesizedAttributeData(
                            attr.InstanceConstructors[0],
                            ImmutableArray.Create(Compilation.CreateTypedConstant(Compilation.SourceAssembly.FileVersion)),
                            ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                    }
                }

                if (_assemblyinformationalversionAttribute != null)
                {
                    yield return _assemblyinformationalversionAttribute;
                }
            }

            //
            yield break;
        }

        public sealed override IEnumerable<Cci.SecurityAttribute> GetSourceAssemblySecurityAttributes()
        {
            yield break;
        }

        public sealed override IEnumerable<Cci.ICustomAttribute> GetSourceModuleAttributes()
        {
            return SourceModule.GetCustomAttributesToEmit(CompilationState).Cast<Cci.ICustomAttribute>();
        }

        internal sealed override Cci.ICustomAttribute SynthesizeAttribute(WellKnownMember attributeConstructor)
        {
            throw new NotImplementedException();
        }

        private string NormalizeDebugDocumentPath(string path, string basePath)
        {
            //var resolver = _compilation.Options.SourceReferenceResolver;
            //if (resolver == null)
            //{
            //    return path;
            //}

            //var key = ValueTuple.Create(path, basePath);
            //string normalizedPath;
            //if (!_normalizedPathsCache.TryGetValue(key, out normalizedPath))
            //{
            //    normalizedPath = resolver.NormalizePath(path, basePath) ?? path;
            //    _normalizedPathsCache.TryAdd(key, normalizedPath);
            //}

            //return normalizedPath;

            return path;
        }

        public override string DefaultNamespace
        {
            get
            {
                // used for PDB writer,
                // C# returns null
                return null;
            }
        }

        public override bool GenerateVisualBasicStylePdb => false;

        public override IEnumerable<string> LinkedAssembliesDebugInfo
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override string ModuleName => Name;

        public IEnumerable<Cci.IModuleReference> ModuleReferences
        {
            get
            {
                // Let's not add any module references explicitly,
                // PeWriter will implicitly add those needed.
                return SpecializedCollections.EmptyEnumerable<Cci.IModuleReference>();
            }
        }

        public override string Name => _sourceModule.Name;

        #region Method Body Map

        /// <summary>
        /// Gets IL builder for lazy static constructor.
        /// </summary>
        public ILBuilder GetStaticCtorBuilder(Cci.ITypeDefinition container)
        {
            ILBuilder il;

            lock (container)
            {
                if (!_cctorBuilders.TryGetValue(container, out il))
                {
                    var cctor = SynthesizedManager.EnsureStaticCtor(container); // ensure .cctor is declared
                    _cctorBuilders[container] = il = new ILBuilder(this, new LocalSlotManager(null), _compilation.Options.OptimizationLevel.AsOptimizationLevel());
                }
            }

            return il;
        }

        /// <summary>
        /// Any lazily emitted static constructor will be realized and its body saved to method map.
        /// </summary>
        public void RealizeStaticCtors()
        {
            // Create module static cctor
            EmitAddScriptReference(GetStaticCtorBuilder(_rootModuleType));

            // finish synthesized .cctor methods:
            foreach (var pair in _cctorBuilders)
            {
                var cctor = SynthesizedManager.EnsureStaticCtor(pair.Key);
                var il = pair.Value;

                //
                Debug.Assert(cctor.ReturnsVoid);
                il.EmitRet(true);

                //
                var body = CodeGen.MethodGenerator.CreateSynthesizedBody(this, cctor, il);
                SetMethodBody(cctor, body);
            }
        }

        void EmitAddScriptReference(ILBuilder il)
        {
            // Context.DllLoader<TScript>
            var tDllLoader_T = this.Compilation.GetTypeByMetadataName(CoreTypes.Context_DllLoader_T);
            var tDllLoader = tDllLoader_T.Construct(this.ScriptType);

            // .AddScriptReference()
            var addmethod = (MethodSymbol)tDllLoader.GetMembers("AddScriptReference").Single();

            // .call Context.DllLoader<TScript>.Bootstrap()
            il
                .EmitCall(this, DiagnosticBag.GetInstance(), System.Reflection.Metadata.ILOpCode.Call, addmethod)
                .Expect(SpecialType.System_Void);
        }

        #endregion

        internal override Compilation CommonCompilation => _compilation;

        internal PhpCompilation Compilation => _compilation;

        internal override CommonEmbeddedTypesManager CommonEmbeddedTypesManagerOpt
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override CommonModuleCompilationState CommonModuleCompilationState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override IAssemblySymbol CommonCorLibrary => _compilation.CorLibrary;

        internal EmitOptions EmitOptions => _emitOptions;

        public Cci.IDefinition AsDefinition(EmitContext context)
        {
            throw new NotImplementedException();
        }

        protected override Cci.IAssemblyReference GetCorLibraryReferenceToEmit(EmitContext context)
        {
            Debug.Assert(_compilation.CorLibrary != null);

            return Translate(_compilation.CorLibrary, context.Diagnostics);
        }

        protected override IEnumerable<Cci.IAssemblyReference> GetAssemblyReferencesFromAddedModules(DiagnosticBag diagnostics)
        {
            // Cannot be retrieved from GetCoreLibraryReferenceToEmit, because it can return only one reference
            Debug.Assert(_compilation.PhpCorLibrary != null);
            yield return Translate(_compilation.PhpCorLibrary, diagnostics);

            ImmutableArray<ModuleSymbol> modules = SourceModule.ContainingAssembly.Modules;

            for (int i = 1; i < modules.Length; i++)
            {
                foreach (AssemblySymbol aRef in modules[i].ReferencedAssemblySymbols)
                {
                    yield return Translate(aRef, diagnostics);
                }
            }
        }

        public IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Cci.ExportedType> GetExportedTypes(DiagnosticBag diagnostics)
        {
            return ImmutableArray<Cci.ExportedType>.Empty; // throw new NotImplementedException();
        }

        public Cci.IFieldReference GetFieldForData(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Cci.UsedNamespaceOrType> GetImports()
        {
            return ImmutableArray<Cci.UsedNamespaceOrType>.Empty; // throw new NotImplementedException();
        }

        public Cci.IMethodReference GetInitArrayHelper()
        {
            throw new NotImplementedException();
        }

        public override Cci.ITypeReference GetPlatformType(Cci.PlatformType t, EmitContext context)
        {
            throw new NotImplementedException();
        }

        //IEnumerable<Cci.ManagedResource> _lazyManagedResources;

        protected override void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics)
        {
            throw new NotSupportedException(); // override
        }

        public override MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> GetSymbolToLocationMap()
        {
            var result = new MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation>();

            //var namespacesAndTypesToProcess = new Stack<NamespaceOrTypeSymbol>();
            //namespacesAndTypesToProcess.Push(SourceModule.GlobalNamespace);

            //Location location = null;

            //while (namespacesAndTypesToProcess.Count > 0)
            //{
            //    NamespaceOrTypeSymbol symbol = namespacesAndTypesToProcess.Pop();
            //    switch (symbol.Kind)
            //    {
            //        case SymbolKind.Namespace:
            //            location = GetSmallestSourceLocationOrNull(symbol);

            //            // filtering out synthesized symbols not having real source 
            //            // locations such as anonymous types, etc...
            //            if (location != null)
            //            {
            //                foreach (var member in symbol.GetMembers())
            //                {
            //                    switch (member.Kind)
            //                    {
            //                        case SymbolKind.Namespace:
            //                        case SymbolKind.NamedType:
            //                            namespacesAndTypesToProcess.Push((NamespaceOrTypeSymbol)member);
            //                            break;

            //                        default:
            //                            throw ExceptionUtilities.UnexpectedValue(member.Kind);
            //                    }
            //                }
            //            }
            //            break;

            //        case SymbolKind.NamedType:
            //            location = GetSmallestSourceLocationOrNull(symbol);
            //            if (location != null)
            //            {
            //                //  add this named type location
            //                AddSymbolLocation(result, location, (Cci.IDefinition)symbol);

            //                foreach (var member in symbol.GetMembers())
            //                {
            //                    switch (member.Kind)
            //                    {
            //                        case SymbolKind.NamedType:
            //                            namespacesAndTypesToProcess.Push((NamespaceOrTypeSymbol)member);
            //                            break;

            //                        case SymbolKind.Method:
            //                            // NOTE: Dev11 does not add synthesized static constructors to this map,
            //                            //       but adds synthesized instance constructors, Roslyn adds both
            //                            var method = (MethodSymbol)member;
            //                            if (method.IsDefaultValueTypeConstructor() ||
            //                                method.IsPartialMethod() && (object)method.PartialImplementationPart == null)
            //                            {
            //                                break;
            //                            }

            //                            AddSymbolLocation(result, member);
            //                            break;

            //                        case SymbolKind.Property:
            //                        case SymbolKind.Field:
            //                            // NOTE: Dev11 does not add synthesized backing fields for properties,
            //                            //       but adds backing fields for events, Roslyn adds both
            //                            AddSymbolLocation(result, member);
            //                            break;

            //                        case SymbolKind.Event:
            //                            AddSymbolLocation(result, member);
            //                            //  event backing fields do not show up in GetMembers
            //                            FieldSymbol field = ((EventSymbol)member).AssociatedField;
            //                            if ((object)field != null)
            //                            {
            //                                AddSymbolLocation(result, field);
            //                            }
            //                            break;

            //                        default:
            //                            throw ExceptionUtilities.UnexpectedValue(member.Kind);
            //                    }
            //                }
            //            }
            //            break;

            //        default:
            //            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            //    }
            //}

            return result;
        }

        public override IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes(EmitContext context)
        {
            Cci.NoPiaReferenceIndexer noPiaIndexer = null;

            // First time through, we need to collect emitted names of all top level types.
            HashSet<string> names = (_namesOfTopLevelTypes == null) ? new HashSet<string>() : null;

            //// First time through, we need to push things through NoPiaReferenceIndexer
            //// to make sure we collect all to be embedded NoPia types and members.
            //if (EmbeddedTypesManagerOpt != null && !EmbeddedTypesManagerOpt.IsFrozen)
            //{
            //    noPiaIndexer = new Cci.NoPiaReferenceIndexer(context);
            //    Debug.Assert(names != null);
            //    this.Dispatch(noPiaIndexer);
            //}

            AddTopLevelType(names, _rootModuleType);
            VisitTopLevelType(noPiaIndexer, _rootModuleType);
            yield return _rootModuleType;

            foreach (var type in this.GetAnonymousTypes(context))
            {
                AddTopLevelType(names, type);
                VisitTopLevelType(noPiaIndexer, type);
                yield return type;
            }

            foreach (var type in this.GetTopLevelTypesCore(context))
            {
                AddTopLevelType(names, type);
                VisitTopLevelType(noPiaIndexer, type);
                yield return type;
            }

            var privateImpl = this.PrivateImplClass;
            if (privateImpl != null)
            {
                AddTopLevelType(names, privateImpl);
                VisitTopLevelType(noPiaIndexer, privateImpl);
                yield return privateImpl;
            }

            //if (EmbeddedTypesManagerOpt != null)
            //{
            //    foreach (var embedded in EmbeddedTypesManagerOpt.GetTypes(context.Diagnostics, names))
            //    {
            //        AddTopLevelType(names, embedded);
            //        yield return embedded;
            //    }
            //}

            if (names != null)
            {
                Debug.Assert(_namesOfTopLevelTypes == null);
                _namesOfTopLevelTypes = names;
            }
        }

        internal virtual IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypesCore(EmitContext context)
        {
            // <script> type containing assembly level symbols
            yield return this.ScriptType;   // TODO: move to anonymous type manager

            foreach (var t in this.Compilation.AnonymousTypeManager.GetAllCreatedTemplates())
                yield return t;

            //foreach (var type in GetAdditionalTopLevelTypes())
            //{
            //    yield return type;
            //}

            foreach (var f in _sourceModule.SymbolCollection.GetFiles())
                yield return f;

            foreach (var t in _sourceModule.SymbolCollection.GetTypes())
                yield return t;

            //var namespacesToProcess = new Stack<INamespaceSymbol>();
            //namespacesToProcess.Push(this.SourceModule.GlobalNamespace);

            //while (namespacesToProcess.Count != 0)
            //{
            //    var ns = namespacesToProcess.Pop();
            //    foreach (var member in ns.GetMembers())
            //    {
            //        var memberNamespace = member as INamespaceSymbol;
            //        if (memberNamespace != null)
            //        {
            //            namespacesToProcess.Push(memberNamespace);
            //        }
            //        else
            //        {
            //            var type = (NamedTypeSymbol)member;
            //            yield return type;
            //        }
            //    }
            //}
        }

        public static Cci.TypeMemberVisibility MemberVisibility(Symbol symbol)
        {
            //
            // We need to make trait members and fields in synthesized _statics holder public:
            //
            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                if (PhpFieldSymbolExtension.IsInStaticsHolder(symbol as FieldSymbol) || // field is generated within `_statics` holder class and must be accessed from outside // note: maybe internal?
                    ((symbol.ContainingSymbol is SourceTraitTypeSymbol) && (symbol is SourceMethodSymbol || symbol is SourceFieldSymbol || symbol is SynthesizedTraitMethodSymbol || symbol is SynthesizedTraitFieldSymbol)))  // member is in trait => hence must be friend with any class (public)
                {
                    return Cci.TypeMemberVisibility.Public;
                }
            }

            //
            // We need to relax visibility of members in interactive submissions since they might be emitted into multiple assemblies.
            // 
            // Top-level:
            //   private                       -> public
            //   protected                     -> public (compiles with a warning)
            //   public                         
            //   internal                      -> public
            // 
            // In a nested class:
            //   
            //   private                       
            //   protected                     
            //   public                         
            //   internal                      -> public
            //
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return Cci.TypeMemberVisibility.Public;

                case Accessibility.Private:
                    if (symbol.ContainingType != null && symbol.ContainingType.TypeKind == TypeKind.Submission)
                    {
                        // top-level private member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.Private;
                    }

                case Accessibility.Internal:
                    if (symbol.ContainingAssembly.IsInteractive)
                    {
                        // top-level or nested internal member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.Assembly;
                    }

                case Accessibility.Protected:
                    if (symbol.ContainingType.TypeKind == TypeKind.Submission)
                    {
                        // top-level protected member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.Family;
                    }

                case Accessibility.ProtectedAndInternal: // Not supported by language, but we should be able to import it.
                    Debug.Assert(symbol.ContainingType.TypeKind != TypeKind.Submission);
                    return Cci.TypeMemberVisibility.FamilyAndAssembly;

                case Accessibility.ProtectedOrInternal:
                    if (symbol.ContainingAssembly.IsInteractive)
                    {
                        // top-level or nested protected internal member:
                        return Cci.TypeMemberVisibility.Public;
                    }
                    else
                    {
                        return Cci.TypeMemberVisibility.FamilyOrAssembly;
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.DeclaredAccessibility);
            }
        }

        #region Private Implementation Details Type

        internal PrivateImplementationDetails GetPrivateImplClass(SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            var result = _privateImplementationDetails;

            if ((result == null) && this.SupportsPrivateImplClass)
            {
                //result = new PrivateImplementationDetails(
                //        this,
                //        _sourceModule.Name,
                //        _compilation.GetSubmissionSlotIndex(),
                //        this.GetSpecialType(SpecialType.System_Object, syntaxNodeOpt, diagnostics),
                //        this.GetSpecialType(SpecialType.System_ValueType, syntaxNodeOpt, diagnostics),
                //        this.GetSpecialType(SpecialType.System_Byte, syntaxNodeOpt, diagnostics),
                //        this.GetSpecialType(SpecialType.System_Int16, syntaxNodeOpt, diagnostics),
                //        this.GetSpecialType(SpecialType.System_Int32, syntaxNodeOpt, diagnostics),
                //        this.GetSpecialType(SpecialType.System_Int64, syntaxNodeOpt, diagnostics),
                //        SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

                if (Interlocked.CompareExchange(ref _privateImplementationDetails, result, null) != null)
                {
                    result = _privateImplementationDetails;
                }
            }

            return result;
        }

        internal PrivateImplementationDetails PrivateImplClass
        {
            get { return _privateImplementationDetails; }
        }

        internal override bool SupportsPrivateImplClass
        {
            get { return false; }   // TODO: true when GetSpecialType() will be implemented
        }

        internal override IModuleSymbol CommonSourceModule => SourceModule;

        #endregion

        static void AddTopLevelType(HashSet<string> names, Cci.INamespaceTypeDefinition type)
        {
            names?.Add(MetadataHelpers.BuildQualifiedName(type.NamespaceName, Cci.MetadataWriter.GetMangledName(type)));
        }

        static void VisitTopLevelType(Cci.NoPiaReferenceIndexer noPiaIndexer, Cci.INamespaceTypeDefinition type)
        {
            noPiaIndexer?.Visit((Cci.ITypeDefinition)type);
        }

        public override bool IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType platformType)
        {
            var namedType = typeRef as PENamedTypeSymbol;
            if (namedType != null)
            {
                if (platformType == Cci.PlatformType.SystemType)
                {
                    return (object)namedType == (object)Compilation.GetWellKnownType(WellKnownType.System_Type);
                }

                return namedType.SpecialType == (SpecialType)platformType;
            }

            return false;
        }

        internal override void CompilationFinished()
        {
            this.CompilationState.Freeze();
            this.Compilation.TrackOnCompleted();
        }

        internal override Cci.ITypeReference EncTranslateType(ITypeSymbol type, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<Cci.INamespaceTypeDefinition> GetAnonymousTypes(EmitContext context)
        {
            return ImmutableArray<Cci.INamespaceTypeDefinition>.Empty; // throw new NotImplementedException();
        }

        internal override ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> GetSynthesizedMembers()
        {
            throw new NotImplementedException(); // _synthesized.GetMembers
        }

        internal Cci.INamedTypeReference GetSpecialType(SpecialType specialType, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            var typeSymbol = SourceModule.ContainingAssembly.GetSpecialType(specialType);

            //DiagnosticInfo info = typeSymbol.GetUseSiteDiagnostic();
            //if (info != null)
            //{
            //    Symbol.ReportUseSiteDiagnostic(info,
            //                                   diagnostics,
            //                                   syntaxNodeOpt != null ? syntaxNodeOpt.Location : NoLocation.Singleton);
            //}

            return (Cci.INamedTypeReference)Translate(typeSymbol, syntaxNodeOpt, diagnostics, needDeclaration: true);
        }

        internal override Cci.IAssemblyReference Translate(IAssemblySymbol iassembly, DiagnosticBag diagnostics)
        {
            var assembly = (AssemblySymbol)iassembly;

            if (ReferenceEquals(SourceModule.ContainingAssembly, assembly))
            {
                return (Cci.IAssemblyReference)this;
            }

            Cci.IModuleReference reference;

            if (AssemblyOrModuleSymbolToModuleRefMap.TryGetValue(assembly, out reference))
            {
                return (Cci.IAssemblyReference)reference;
            }

            AssemblyReference asmRef = new AssemblyReference(assembly);

            AssemblyReference cachedAsmRef = (AssemblyReference)AssemblyOrModuleSymbolToModuleRefMap.GetOrAdd(assembly, asmRef);

            if (cachedAsmRef == asmRef)
            {
                ValidateReferencedAssembly(assembly, cachedAsmRef, diagnostics);
            }

            // TryAdd because whatever is associated with assembly should be associated with Modules[0]
            AssemblyOrModuleSymbolToModuleRefMap.TryAdd((ModuleSymbol)assembly.Modules[0], cachedAsmRef);

            return cachedAsmRef;
        }

        private void ValidateReferencedAssembly(AssemblySymbol assembly, AssemblyReference asmRef, DiagnosticBag diagnostics)
        {
            //AssemblyIdentity asmIdentity = SourceModule.ContainingAssembly.Identity;
            //AssemblyIdentity refIdentity = asmRef.MetadataIdentity;

            //if (asmIdentity.IsStrongName && !refIdentity.IsStrongName &&
            //    ((Cci.IAssemblyReference)asmRef).ContentType != System.Reflection.AssemblyContentType.WindowsRuntime)
            //{
            //    // Dev12 reported error, we have changed it to a warning to allow referencing libraries 
            //    // built for platforms that don't support strong names.
            //    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.WRN_ReferencedAssemblyDoesNotHaveStrongName, assembly), NoLocation.Singleton);
            //}

            //if (OutputKind != OutputKind.NetModule &&
            //   !string.IsNullOrEmpty(refIdentity.CultureName) &&
            //   !string.Equals(refIdentity.CultureName, asmIdentity.CultureName, StringComparison.OrdinalIgnoreCase))
            //{
            //    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.WRN_RefCultureMismatch, assembly, refIdentity.CultureName), NoLocation.Singleton);
            //}

            //var refMachine = assembly.Machine;
            //// If other assembly is agnostic this is always safe
            //// Also, if no mscorlib was specified for back compat we add a reference to mscorlib
            //// that resolves to the current framework directory. If the compiler is 64-bit
            //// this is a 64-bit mscorlib, which will produce a warning if /platform:x86 is
            //// specified. A reference to the default mscorlib should always succeed without
            //// warning so we ignore it here.
            //if ((object)assembly != (object)assembly.CorLibrary &&
            //    !(refMachine == Machine.I386 && !assembly.Bit32Required))
            //{
            //    var machine = SourceModule.Machine;

            //    if (!(machine == Machine.I386 && !SourceModule.Bit32Required) &&
            //        machine != refMachine)
            //    {
            //        // Different machine types, and neither is agnostic
            //        diagnostics.Add(new CSDiagnosticInfo(ErrorCode.WRN_ConflictingMachineAssembly, assembly), NoLocation.Singleton);
            //    }
            //}

            //if (_embeddedTypesManagerOpt != null && _embeddedTypesManagerOpt.IsFrozen)
            //{
            //    _embeddedTypesManagerOpt.ReportIndirectReferencesToLinkedAssemblies(assembly, diagnostics);
            //}
        }

        internal override Cci.IMethodReference Translate(IMethodSymbol symbol, DiagnosticBag diagnostics, bool needDeclaration)
        {
            return Translate((MethodSymbol)symbol, null, diagnostics, /*null,*/ needDeclaration);
        }

        //internal Cci.IMethodReference Translate(
        //    MethodSymbol methodSymbol,
        //    SyntaxNode syntaxNodeOpt,
        //    DiagnosticBag diagnostics,
        //    BoundArgListOperator optArgList = null,
        //    bool needDeclaration = false)
        //{
        //    Debug.Assert(optArgList == null || (methodSymbol.IsVararg && !needDeclaration));

        //    Cci.IMethodReference unexpandedMethodRef = Translate(methodSymbol, syntaxNodeOpt, diagnostics, needDeclaration);

        //    if (optArgList != null && optArgList.Arguments.Length > 0)
        //    {
        //        Cci.IParameterTypeInformation[] @params = new Cci.IParameterTypeInformation[optArgList.Arguments.Length];
        //        int ordinal = methodSymbol.ParameterCount;

        //        for (int i = 0; i < @params.Length; i++)
        //        {
        //            @params[i] = new ArgListParameterTypeInformation(ordinal,
        //                                                            !optArgList.ArgumentRefKindsOpt.IsDefaultOrEmpty && optArgList.ArgumentRefKindsOpt[i] != RefKind.None,
        //                                                            Translate(optArgList.Arguments[i].Type, syntaxNodeOpt, diagnostics));
        //            ordinal++;
        //        }

        //        return new ExpandedVarargsMethodReference(unexpandedMethodRef, @params.AsImmutableOrNull());
        //    }
        //    else
        //    {
        //        return unexpandedMethodRef;
        //    }
        //}

        internal Cci.IMethodReference Translate(
            MethodSymbol methodSymbol,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            bool needDeclaration)
        {
            object reference;
            Cci.IMethodReference methodRef;
            NamedTypeSymbol container = methodSymbol.ContainingType;

            Debug.Assert(methodSymbol.IsDefinitionOrDistinct());

            //// Method of anonymous type being translated
            //if (container.IsAnonymousType)
            //{
            //    //methodSymbol = AnonymousTypeManager.TranslateAnonymousTypeMethodSymbol(methodSymbol);
            //    throw new NotImplementedException();
            //}

            if (!methodSymbol.IsDefinition)
            {
                Debug.Assert(!needDeclaration);

                return methodSymbol;
            }
            else if (!needDeclaration)
            {
                bool methodIsGeneric = methodSymbol.IsGenericMethod;
                bool typeIsGeneric = IsGenericType(container);

                if (methodIsGeneric || typeIsGeneric)
                {
                    if (_genericInstanceMap.TryGetValue(methodSymbol, out reference))
                    {
                        return (Cci.IMethodReference)reference;
                    }

                    if (methodIsGeneric)
                    {
                        if (typeIsGeneric)
                        {
                            // Specialized and generic instance at the same time.
                            methodRef = new SpecializedGenericMethodInstanceReference(methodSymbol);
                        }
                        else
                        {
                            methodRef = new GenericMethodInstanceReference(methodSymbol);
                        }
                    }
                    else
                    {
                        Debug.Assert(typeIsGeneric);
                        methodRef = new SpecializedMethodReference(methodSymbol);
                    }

                    methodRef = (Cci.IMethodReference)_genericInstanceMap.GetOrAdd(methodSymbol, methodRef);

                    return methodRef;
                }
            }

            //if (_embeddedTypesManagerOpt != null)
            //{
            //    return _embeddedTypesManagerOpt.EmbedMethodIfNeedTo(methodSymbol, syntaxNodeOpt, diagnostics);
            //}

            return methodSymbol;
        }

        internal Cci.IMethodReference TranslateOverriddenMethodReference(
            MethodSymbol methodSymbol,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics)
        {
            Cci.IMethodReference methodRef;
            NamedTypeSymbol container = methodSymbol.ContainingType;

            if (IsGenericType(container))
            {
                if (methodSymbol.IsDefinition)
                {
                    object reference;

                    if (_genericInstanceMap.TryGetValue(methodSymbol, out reference))
                    {
                        methodRef = (Cci.IMethodReference)reference;
                    }
                    else
                    {
                        methodRef = new SpecializedMethodReference(methodSymbol);
                        methodRef = (Cci.IMethodReference)_genericInstanceMap.GetOrAdd(methodSymbol, methodRef);
                    }
                }
                else
                {
                    methodRef = new SpecializedMethodReference(methodSymbol);
                }
            }
            else
            {
                Debug.Assert(methodSymbol.IsDefinition);

                //if (_embeddedTypesManagerOpt != null)
                //{
                //    methodRef = _embeddedTypesManagerOpt.EmbedMethodIfNeedTo(methodSymbol, syntaxNodeOpt, diagnostics);
                //}
                //else
                {
                    methodRef = methodSymbol;
                }
            }

            return methodRef;
        }

        internal static Cci.IGenericParameterReference Translate(TypeParameterSymbol param)
        {
            if (!param.IsDefinition)
                throw new InvalidOperationException(/*string.Format(CSharpResources.GenericParameterDefinition, param.Name)*/);

            return param;
        }

        internal sealed override Cci.ITypeReference Translate(ITypeSymbol typeSymbol, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            switch (typeSymbol.Kind)
            {
                //case SymbolKind.DynamicType:
                //    return Translate((DynamicTypeSymbol)typeSymbol, syntaxNodeOpt, diagnostics);

                case SymbolKind.ArrayType:
                    return Translate((ArrayTypeSymbol)typeSymbol);

                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                    return Translate((NamedTypeSymbol)typeSymbol, syntaxNodeOpt, diagnostics);

                case SymbolKind.PointerType:
                    return Translate((PointerTypeSymbol)typeSymbol);

                case SymbolKind.TypeParameter:
                    return Translate((TypeParameterSymbol)typeSymbol);
            }

            throw ExceptionUtilities.UnexpectedValue(typeSymbol.Kind);
        }

        internal Cci.IFieldReference Translate(
            FieldSymbol fieldSymbol,
            SyntaxNode syntaxNodeOpt,
            DiagnosticBag diagnostics,
            bool needDeclaration = false)
        {
            Debug.Assert(fieldSymbol.IsDefinitionOrDistinct());
            // Debug.Assert(!fieldSymbol.IsTupleField, "tuple fields should be rewritten to underlying by now");

            if (!fieldSymbol.IsDefinition)
            {
                Debug.Assert(!needDeclaration);

                return fieldSymbol;
            }
            else if (!needDeclaration && IsGenericType(fieldSymbol.ContainingType))
            {
                object reference;
                Cci.IFieldReference fieldRef;

                if (_genericInstanceMap.TryGetValue(fieldSymbol, out reference))
                {
                    return (Cci.IFieldReference)reference;
                }

                fieldRef = new SpecializedFieldReference(fieldSymbol);
                fieldRef = (Cci.IFieldReference)_genericInstanceMap.GetOrAdd(fieldSymbol, fieldRef);

                return fieldRef;
            }

            //if (_embeddedTypesManagerOpt != null)
            //{
            //    return _embeddedTypesManagerOpt.EmbedFieldIfNeedTo(fieldSymbol, syntaxNodeOpt, diagnostics);
            //}

            return fieldSymbol;
        }

        internal Cci.ITypeReference Translate(
            NamedTypeSymbol namedTypeSymbol, SyntaxNode syntaxOpt, DiagnosticBag diagnostics,
            bool fromImplements = false,
            bool needDeclaration = false)
        {
            Debug.Assert(namedTypeSymbol.IsDefinitionOrDistinct());
            Debug.Assert(diagnostics != null);

            //// Anonymous type being translated
            //if (namedTypeSymbol.IsAnonymousType)
            //{
            //    //namedTypeSymbol = AnonymousTypeManager.TranslateAnonymousTypeSymbol(namedTypeSymbol);
            //    throw new NotImplementedException();
            //}

            // Substitute error types with a special singleton object.
            // Unreported bad types can come through NoPia embedding, for example.
            if (namedTypeSymbol.OriginalDefinition.Kind == SymbolKind.ErrorType)
            {
                //ErrorTypeSymbol errorType = (ErrorTypeSymbol)namedTypeSymbol.OriginalDefinition;
                //DiagnosticInfo diagInfo = errorType.GetUseSiteDiagnostic() ?? errorType.ErrorInfo;

                //if (diagInfo == null && namedTypeSymbol.Kind == SymbolKind.ErrorType)
                //{
                //    errorType = (ErrorTypeSymbol)namedTypeSymbol;
                //    diagInfo = errorType.GetUseSiteDiagnostic() ?? errorType.ErrorInfo;
                //}

                //// Try to decrease noise by not complaining about the same type over and over again.
                //if (_reportedErrorTypesMap.Add(errorType))
                //{
                //    diagnostics.Add(new CSDiagnostic(diagInfo ?? new CSDiagnosticInfo(ErrorCode.ERR_BogusType, string.Empty), syntaxNodeOpt == null ? NoLocation.Singleton : syntaxNodeOpt.Location));
                //}

                //return CodeAnalysis.Emit.ErrorType.Singleton;

                throw new NotImplementedException($"Translate(ErrorType {namedTypeSymbol.Name})");
            }

            if (!namedTypeSymbol.IsDefinition)
            {
                // generic instantiation for sure
                Debug.Assert(!needDeclaration);

                if (namedTypeSymbol.IsUnboundGenericType)
                {
                    namedTypeSymbol = namedTypeSymbol.OriginalDefinition;
                }
                else
                {
                    return namedTypeSymbol;
                }
            }
            else if (!needDeclaration)
            {
                object reference;
                Cci.INamedTypeReference typeRef;

                NamedTypeSymbol container = namedTypeSymbol.ContainingType;

                if (namedTypeSymbol.Arity > 0)
                {
                    if (_genericInstanceMap.TryGetValue(namedTypeSymbol, out reference))
                    {
                        return (Cci.INamedTypeReference)reference;
                    }

                    if ((object)container != null)
                    {
                        if (IsGenericType(container))
                        {
                            // Container is a generic instance too.
                            typeRef = new SpecializedGenericNestedTypeInstanceReference(namedTypeSymbol);
                        }
                        else
                        {
                            typeRef = new GenericNestedTypeInstanceReference(namedTypeSymbol);
                        }
                    }
                    else
                    {
                        typeRef = new GenericNamespaceTypeInstanceReference(namedTypeSymbol);
                    }

                    typeRef = (Cci.INamedTypeReference)_genericInstanceMap.GetOrAdd(namedTypeSymbol, typeRef);

                    return typeRef;
                }
                else if (IsGenericType(container))
                {
                    Debug.Assert((object)container != null);

                    if (_genericInstanceMap.TryGetValue(namedTypeSymbol, out reference))
                    {
                        return (Cci.INamedTypeReference)reference;
                    }

                    typeRef = new SpecializedNestedTypeReference(namedTypeSymbol);
                    typeRef = (Cci.INamedTypeReference)_genericInstanceMap.GetOrAdd(namedTypeSymbol, typeRef);

                    return typeRef;
                }
            }

            // NoPia: See if this is a type, which definition we should copy into our assembly.
            Debug.Assert(namedTypeSymbol.IsDefinition);

            //if (_embeddedTypesManagerOpt != null)
            //{
            //    return _embeddedTypesManagerOpt.EmbedTypeIfNeedTo(namedTypeSymbol, fromImplements, syntaxNodeOpt, diagnostics);
            //}

            return (Cci.ITypeReference)namedTypeSymbol;
        }

        internal static Cci.IArrayTypeReference Translate(ArrayTypeSymbol symbol)
        {
            return symbol;
        }

        internal static Cci.IPointerTypeReference Translate(PointerTypeSymbol symbol)
        {
            return symbol;
        }

        internal ImmutableArray<Cci.IParameterTypeInformation> Translate(ImmutableArray<ParameterSymbol> @params)
        {
            return @params.Cast<Cci.IParameterTypeInformation>().ToImmutableArray();
        }

        private bool IsSourceDefinition(IMethodSymbol method)
        {
            return (object)method.ContainingModule == _sourceModule && method.IsDefinition;
        }

        public static bool IsGenericType(INamedTypeSymbol toCheck)
        {
            while ((object)toCheck != null)
            {
                if (toCheck.Arity > 0)
                {
                    return true;
                }

                toCheck = toCheck.ContainingType;
            }

            return false;
        }
    }
}
