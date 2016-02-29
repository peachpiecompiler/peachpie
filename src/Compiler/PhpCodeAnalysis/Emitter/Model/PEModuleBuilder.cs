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
using Cci = Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit.NoPia;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Emitter;

namespace Pchp.CodeAnalysis.Emit
{
    internal class PEModuleBuilder : CommonPEModuleBuilder, Cci.IModule, ITokenDeferral
    {
        private readonly SourceModuleSymbol _sourceModule;
        private readonly PhpCompilation _compilation;
        private readonly OutputKind _outputKind;
        private readonly EmitOptions _emitOptions;
        private readonly Cci.ModulePropertiesForSerialization _serializationProperties;

        protected readonly ConcurrentDictionary<Symbol, Cci.IModuleReference> AssemblyOrModuleSymbolToModuleRefMap = new ConcurrentDictionary<Symbol, Cci.IModuleReference>();
        readonly StringTokenMap _stringsInILMap = new StringTokenMap();
        readonly ConcurrentDictionary<IMethodSymbol, Cci.IMethodBody> _methodBodyMap = new ConcurrentDictionary<IMethodSymbol, Cci.IMethodBody>(ReferenceEqualityComparer.Instance);
        readonly TokenMap<Cci.IReference> _referencesInILMap = new TokenMap<Cci.IReference>();
        readonly Cci.RootModuleType _rootModuleType = new Cci.RootModuleType();
        Cci.IMethodReference _peEntryPoint, _debugEntryPoint;
        PrivateImplementationDetails _privateImplementationDetails;
        HashSet<string> _namesOfTopLevelTypes;  // initialized with set of type names within first call to GetTopLevelTypes()

        internal readonly IEnumerable<ResourceDescription> ManifestResources;
        internal readonly CommonModuleCompilationState CompilationState;

        // This is a map from the document "name" to the document.
        // Document "name" is typically a file path like "C:\Abc\Def.cs". However, that is not guaranteed.
        // For compatibility reasons the names are treated as case-sensitive in C# and case-insensitive in VB.
        // Neither language trims the names, so they are both sensitive to the leading and trailing whitespaces.
        // NOTE: We are not considering how filesystem or debuggers do the comparisons, but how native implementations did.
        // Deviating from that may result in unexpected warnings or different behavior (possibly without warnings).
        private readonly ConcurrentDictionary<string, Cci.DebugSourceDocument> _debugDocuments;

        protected PEModuleBuilder(
            PhpCompilation compilation,
            SourceModuleSymbol sourceModule,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            OutputKind outputKind,
            EmitOptions emitOptions)
        {
            Debug.Assert(sourceModule != null);
            Debug.Assert(serializationProperties != null);

            _compilation = compilation;
            _sourceModule = sourceModule;
            _serializationProperties = serializationProperties;
            this.ManifestResources = manifestResources;
            _outputKind = outputKind;
            _emitOptions = emitOptions;
            this.CompilationState = new CommonModuleCompilationState();
            _debugDocuments = new ConcurrentDictionary<string, Cci.DebugSourceDocument>(compilation.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

            AssemblyOrModuleSymbolToModuleRefMap.Add(sourceModule, this);
        }

        internal SourceModuleSymbol SourceModule => _sourceModule;

        public ArrayMethods ArrayMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal virtual int CurrentGenerationOrdinal => 0; // used for EditAndContinue

        public Cci.IAssembly AsAssembly => this as Cci.IAssembly;

        public IEnumerable<Cci.ICustomAttribute> AssemblyAttributes
        {
            get
            {
                yield break; // throw new NotImplementedException();
            }
        }

        public IEnumerable<Cci.SecurityAttribute> AssemblySecurityAttributes
        {
            get
            {
                yield break; // throw new NotImplementedException();
            }
        }

        internal Cci.DebugSourceDocument GetOrAddDebugDocument(string path, string basePath, Func<string, Cci.DebugSourceDocument> factory)
        {
            return _debugDocuments.GetOrAdd(NormalizeDebugDocumentPath(path, basePath), factory);
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

        public string DefaultNamespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool GenerateVisualBasicStylePdb
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int HintNumberOfMethodDefinitions => _methodBodyMap.Count;

        public OutputKind Kind => _outputKind;

        public IEnumerable<string> LinkedAssembliesDebugInfo
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<Cci.ICustomAttribute> ModuleAttributes
        {
            get
            {
                yield break; // throw new NotImplementedException();
            }
        }

        public string ModuleName => Name;

        public IEnumerable<Cci.IModuleReference> ModuleReferences
        {
            get
            {
                // Let's not add any module references explicitly,
                // PeWriter will implicitly add those needed.
                return SpecializedCollections.EmptyEnumerable<Cci.IModuleReference>();
            }
        }

        public virtual string Name => _sourceModule.Name;

        Cci.IMethodReference Cci.IModule.PEEntryPoint => _peEntryPoint;
        Cci.IMethodReference Cci.IModule.DebugEntryPoint => _debugEntryPoint;

        internal void SetPEEntryPoint(IMethodSymbol method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition((IMethodSymbol)method));
            Debug.Assert(_outputKind.IsApplication());

            _peEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        internal void SetDebugEntryPoint(IMethodSymbol method, DiagnosticBag diagnostics)
        {
            Debug.Assert(method == null || IsSourceDefinition((IMethodSymbol)method));

            _debugEntryPoint = Translate(method, diagnostics, needDeclaration: true);
        }

        #region Method Body Map

        internal Cci.IMethodBody GetMethodBody(IMethodSymbol methodSymbol)
        {
            Debug.Assert(object.ReferenceEquals(((IMethodSymbol)methodSymbol).ContainingModule, this.SourceModule));
            Debug.Assert(((IMethodSymbol)methodSymbol).IsDefinition);
            Debug.Assert(((IMethodSymbol)methodSymbol).PartialDefinitionPart == null); // Must be definition.

            Cci.IMethodBody body;

            if (_methodBodyMap.TryGetValue(methodSymbol, out body))
            {
                return body;
            }

            return null;
        }

        public void SetMethodBody(IMethodSymbol methodSymbol, Cci.IMethodBody body)
        {
            Debug.Assert(object.ReferenceEquals(((IMethodSymbol)methodSymbol).ContainingModule, this.SourceModule));
            Debug.Assert(((IMethodSymbol)methodSymbol).IsDefinition);
            Debug.Assert(((IMethodSymbol)methodSymbol).PartialDefinitionPart == null); // Must be definition.
            Debug.Assert(body == null || (object)methodSymbol == body.MethodDefinition);

            _methodBodyMap.Add(methodSymbol, body);
        }

        #endregion

        public Cci.ModulePropertiesForSerialization Properties => _serializationProperties;

        public IEnumerable<Cci.IWin32Resource> Win32Resources
        {
            get
            {
                return ImmutableArray<Cci.IWin32Resource>.Empty; // throw new NotImplementedException();
            }
            private set
            {
                throw new NotImplementedException();
            }
        }

        public Cci.ResourceSection Win32ResourceSection
        {
            get
            {
                return null; // throw new NotImplementedException();
            }
            private set
            {
                throw new NotImplementedException();
            }
        }

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

        internal override EmitOptions EmitOptions => _emitOptions;

        public Cci.IDefinition AsDefinition(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public virtual void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IModule)this);
        }

        public ImmutableArray<Cci.AssemblyReferenceAlias> GetAssemblyReferenceAliases(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.IAssemblyReference> GetAssemblyReferences(EmitContext context)
        {
            // Only add Cor Library reference explicitly, PeWriter will add
            // other references implicitly on as needed basis.
            foreach (var aRef in GetCorLibraryReferencesToEmit(context))
            {
                yield return aRef;
            }

            if (_outputKind != OutputKind.NetModule)
            {
                // Explicitly add references from added modules
                foreach (var aRef in GetAssemblyReferencesFromAddedModules(context.Diagnostics))
                {
                    yield return aRef;
                }
            }

            yield break;
        }

        private IEnumerable<Cci.IAssemblyReference> GetCorLibraryReferencesToEmit(EmitContext context)
        {
            Debug.Assert(_compilation.CorLibrary != null);
            Debug.Assert(_compilation.PhpCorLibrary != null);

            yield return Translate(_compilation.CorLibrary, context.Diagnostics);
            yield return Translate(_compilation.PhpCorLibrary, context.Diagnostics);
        }

        protected IEnumerable<Cci.IAssemblyReference> GetAssemblyReferencesFromAddedModules(DiagnosticBag diagnostics)
        {
            foreach (ModuleSymbol m in _sourceModule.ContainingAssembly.Modules)
            {
                foreach (AssemblySymbol aRef in m.ReferencedAssemblySymbols)
                {
                    yield return Translate(aRef, diagnostics);
                }
            }
        }

        public IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public Cci.IAssembly GetContainingAssembly(EmitContext context)
        {
            return _outputKind.IsNetModule() ? null : (Cci.IAssembly)this;
        }

        public Cci.IAssemblyReference GetCorLibrary(EmitContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.ITypeReference> GetExportedTypes(EmitContext context)
        {
            return ImmutableArray<Cci.ITypeReference>.Empty; // throw new NotImplementedException();
        }

        public uint GetFakeStringTokenForIL(string value)
        {
            return _stringsInILMap.GetOrAddTokenFor(value);
        }

        public uint GetFakeSymbolTokenForIL(Cci.IReference symbol, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            bool added;
            uint token = _referencesInILMap.GetOrAddTokenFor(symbol, out added);
            if (added)
            {
                ReferenceDependencyWalker.VisitReference(symbol, new EmitContext(this, syntaxNode, diagnostics));
            }
            return token;
        }

        public Cci.IFieldReference GetFieldForData(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<Cci.UsedNamespaceOrType> GetImports()
        {
            return ImmutableArray<Cci.UsedNamespaceOrType>.Empty; // throw new NotImplementedException();
        }

        public Cci.IMethodReference GetInitArrayHelper()
        {
            throw new NotImplementedException();
        }

        public Cci.ITypeReference GetPlatformType(Cci.PlatformType t, EmitContext context)
        {
            throw new NotImplementedException();
        }

        public Cci.IReference GetReferenceFromToken(uint token)
        {
            return _referencesInILMap.GetItem(token);
        }

        IEnumerable<Cci.ManagedResource> _lazyManagedResources;

        public IEnumerable<Cci.ManagedResource> GetResources(EmitContext context)
        {
            if (_lazyManagedResources == null)
            {
                var builder = ArrayBuilder<Cci.ManagedResource>.GetInstance();

                foreach (ResourceDescription r in ManifestResources)
                {
                    builder.Add(r.ToManagedResource(this));
                }

                if (_outputKind != OutputKind.NetModule)
                {
                    // Explicitly add resources from added modules
                    AddEmbeddedResourcesFromAddedModules(builder, context.Diagnostics);
                }

                _lazyManagedResources = builder.ToImmutableAndFree();
            }

            return _lazyManagedResources;
        }

        protected virtual void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics)
        {
            throw new NotSupportedException(); // override
        }

        public string GetStringFromToken(uint token)
        {
            return _stringsInILMap.GetItem(token);
        }
        
        public IEnumerable<string> GetStrings() => _stringsInILMap.GetAllItems();

        public MultiDictionary<Cci.DebugSourceDocument, Cci.DefinitionWithLocation> GetSymbolToLocationMap()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Cci.INamespaceTypeDefinition> GetTopLevelTypes(EmitContext context)
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

            foreach (var type in this.GetAnonymousTypes())
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

            // TODO: <global> type containing global functions, constants, variables

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
            //foreach (var type in GetAdditionalTopLevelTypes())
            //{
            //    yield return type;
            //}

            return _sourceModule.SymbolTables
                .GetTypes()
                .Cast<Cci.INamespaceTypeDefinition>();

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
                    if (symbol.ContainingType.TypeKind == TypeKind.Submission)
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

        #endregion

        static void AddTopLevelType(HashSet<string> names, Cci.INamespaceTypeDefinition type)
        {
            names?.Add(MetadataHelpers.BuildQualifiedName(type.NamespaceName, Cci.MetadataWriter.GetMangledName(type)));
        }

        static void VisitTopLevelType(Cci.NoPiaReferenceIndexer noPiaIndexer, Cci.INamespaceTypeDefinition type)
        {
            noPiaIndexer?.Visit((Cci.ITypeDefinition)type);
        }

        public bool IsPlatformType(Cci.ITypeReference typeRef, Cci.PlatformType platformType)
        {
            var namedType = typeRef as PENamedTypeSymbol;
            if (namedType != null)
            {
                if (platformType == Cci.PlatformType.SystemType)
                {
                    throw new NotImplementedException(); // below is sufficient ?
                    //return (object)namedType == (object)Compilation.GetWellKnownType(WellKnownType.System_Type);
                }

                return namedType.SpecialType == (SpecialType)platformType;
            }

            return false;
        }

        public IEnumerable<Cci.IReference> ReferencesInIL(out int count)
        {
            return _referencesInILMap.GetAllItemsAndCount(out count);
        }

        internal override void CompilationFinished()
        {
            this.CompilationState.Freeze();
        }

        internal override Cci.ITypeReference EncTranslateType(ITypeSymbol type, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<Cci.INamespaceTypeDefinition> GetAnonymousTypes()
        {
            return ImmutableArray<Cci.INamespaceTypeDefinition>.Empty; // throw new NotImplementedException();
        }

        internal override ImmutableDictionary<Cci.ITypeDefinition, ImmutableArray<Cci.ITypeDefinitionMember>> GetSynthesizedMembers()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        internal override Cci.ITypeReference Translate(ITypeSymbol symbol, SyntaxNode syntaxOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            //if (!namedTypeSymbol.IsDefinition)
            //{
            //    // generic instantiation for sure
            //    Debug.Assert(!needDeclaration);

            //    if (namedTypeSymbol.IsUnboundGenericType)
            //    {
            //        namedTypeSymbol = namedTypeSymbol.OriginalDefinition;
            //    }
            //    else
            //    {
            //        return namedTypeSymbol;
            //    }
            //}
            //else if (!needDeclaration)
            //{
            //    object reference;
            //    Cci.INamedTypeReference typeRef;

            //    NamedTypeSymbol container = namedTypeSymbol.ContainingType;

            //    if (namedTypeSymbol.Arity > 0)
            //    {
            //        if (_genericInstanceMap.TryGetValue(namedTypeSymbol, out reference))
            //        {
            //            return (Cci.INamedTypeReference)reference;
            //        }

            //        if ((object)container != null)
            //        {
            //            if (IsGenericType(container))
            //            {
            //                // Container is a generic instance too.
            //                typeRef = new SpecializedGenericNestedTypeInstanceReference(namedTypeSymbol);
            //            }
            //            else
            //            {
            //                typeRef = new GenericNestedTypeInstanceReference(namedTypeSymbol);
            //            }
            //        }
            //        else
            //        {
            //            typeRef = new GenericNamespaceTypeInstanceReference(namedTypeSymbol);
            //        }

            //        typeRef = (Cci.INamedTypeReference)_genericInstanceMap.GetOrAdd(namedTypeSymbol, typeRef);

            //        return typeRef;
            //    }
            //    else if (IsGenericType(container))
            //    {
            //        Debug.Assert((object)container != null);

            //        if (_genericInstanceMap.TryGetValue(namedTypeSymbol, out reference))
            //        {
            //            return (Cci.INamedTypeReference)reference;
            //        }

            //        typeRef = new SpecializedNestedTypeReference(namedTypeSymbol);
            //        typeRef = (Cci.INamedTypeReference)_genericInstanceMap.GetOrAdd(namedTypeSymbol, typeRef);

            //        return typeRef;
            //    }
            //}

            //// NoPia: See if this is a type, which definition we should copy into our assembly.
            //Debug.Assert(namedTypeSymbol.IsDefinition);

            //if (_embeddedTypesManagerOpt != null)
            //{
            //    return _embeddedTypesManagerOpt.EmbedTypeIfNeedTo(namedTypeSymbol, fromImplements, syntaxNodeOpt, diagnostics);
            //}

            return (Cci.ITypeReference)symbol;
        }

        internal ImmutableArray<Cci.IParameterTypeInformation> Translate(ImmutableArray<IParameterSymbol> @params)
        {
            return @params.Cast<Cci.IParameterTypeInformation>().ToImmutableArray();
        }

        Cci.IAssemblyReference Cci.IModuleReference.GetContainingAssembly(EmitContext context)
        {
            throw new NotImplementedException();
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
