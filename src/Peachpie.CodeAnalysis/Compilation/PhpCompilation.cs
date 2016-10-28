using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using System.Diagnostics;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Collections;
using System.Collections.Concurrent;
using Devsense.PHP.Syntax;

namespace Pchp.CodeAnalysis
{
    internal sealed partial class PhpCompilation : Compilation
    {
        readonly SourceDeclarations _tables;
        MethodSymbol _lazyMainMethod;
        readonly PhpCompilationOptions _options;

        /// <summary>
        /// Manages anonymous types declared in this compilation. Unifies types that are structurally equivalent.
        /// </summary>
        readonly AnonymousTypeManager _anonymousTypeManager;

        /// <summary>
        /// The <see cref="SourceAssemblySymbol"/> for this compilation. Do not access directly, use Assembly property
        /// instead. This field is lazily initialized by ReferenceManager, ReferenceManager.CacheLockObject must be locked
        /// while ReferenceManager "calculates" the value and assigns it, several threads must not perform duplicate
        /// "calculation" simultaneously.
        /// </summary>
        private SourceAssemblySymbol _lazyAssemblySymbol;

        /// <summary>
        /// Holds onto data related to reference binding.
        /// The manager is shared among multiple compilations that we expect to have the same result of reference binding.
        /// In most cases this can be determined without performing the binding. If the compilation however contains a circular 
        /// metadata reference (a metadata reference that refers back to the compilation) we need to avoid sharing of the binding results.
        /// We do so by creating a new reference manager for such compilation. 
        /// </summary>
        private ReferenceManager _referenceManager;

        /// <summary>
        /// COR library containing base system types.
        /// </summary>
        public AssemblySymbol CorLibrary => ((ReferenceManager)GetBoundReferenceManager()).CorLibrary;

        /// <summary>
        /// PHP COR library containing PHP runtime.
        /// </summary>
        public AssemblySymbol PhpCorLibrary => ((ReferenceManager)GetBoundReferenceManager()).PhpCorLibrary;

        /// <summary>
        /// Tables containing all source symbols to be compiled.
        /// Used for enumeration and lookup.
        /// </summary>
        public SourceDeclarations SourceSymbolTables => _tables;

        /// <summary>
        /// The AssemblySymbol that represents the assembly being created.
        /// </summary>
        internal SourceAssemblySymbol SourceAssembly
        {
            get
            {
                GetBoundReferenceManager();
                return _lazyAssemblySymbol;
            }
        }

        internal new SourceModuleSymbol SourceModule => (SourceModuleSymbol)this.SourceAssembly.Modules[0];

        /// <summary>
        /// The AssemblySymbol that represents the assembly being created.
        /// </summary>
        internal new IAssemblySymbol Assembly => SourceAssembly;

        internal new PhpCompilationOptions Options => _options;

        private PhpCompilation(
            string assemblyName,
            PhpCompilationOptions options,
            ImmutableArray<MetadataReference> references,
            //ReferenceManager referenceManager,
            //SyntaxAndDeclarationManager syntaxAndDeclarations
            AsyncQueue<CompilationEvent> eventQueue = null
            )
            : base(assemblyName, references, SyntaxTreeCommonFeatures(ImmutableArray<SyntaxTree>.Empty), false, eventQueue)
        {
            _wellKnownMemberSignatureComparer = new WellKnownMembersSignatureComparer(this);

            _options = options;
            _referenceManager = new ReferenceManager(options.SdkDirectory);
            _tables = new SourceDeclarations();
            _coreTypes = new CoreTypes(this);
            _coreMethods = new CoreMethods(_coreTypes);

            _anonymousTypeManager = new AnonymousTypeManager(this);
        }

        public override ImmutableArray<MetadataReference> DirectiveReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns true if this is a case sensitive compilation, false otherwise.  Case sensitivity
        /// affects compilation features such as name lookup as well as choosing what names to emit
        /// when there are multiple different choices (for example between a virtual method and an
        /// override).
        /// </summary>
        public override bool IsCaseSensitive => false;

        public override string Language { get; } = Constants.PhpLanguageName;

        internal AnonymousTypeManager AnonymousTypeManager => _anonymousTypeManager;

        public override IEnumerable<AssemblyIdentity> ReferencedAssemblyNames => Assembly.Modules.SelectMany(module => module.ReferencedAssemblies);

        protected override IAssemblySymbol CommonAssembly => SourceAssembly;

        protected override ITypeSymbol CommonDynamicType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override INamespaceSymbol CommonGlobalNamespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override INamedTypeSymbol CommonObjectType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override CompilationOptions CommonOptions => _options;

        protected override INamedTypeSymbol CommonScriptClass
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Get a ModuleSymbol that refers to the module being created by compiling all of the code.
        /// By getting the GlobalNamespace property of that module, all of the namespaces and types
        /// defined in source code can be obtained.
        /// </summary>
        protected override IModuleSymbol CommonSourceModule => this.SourceModule;

        protected override IEnumerable<SyntaxTree> CommonSyntaxTrees
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override CommonAnonymousTypeManager CommonAnonymousTypeManager
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ScriptCompilationInfo CommonScriptCompilationInfo
        {
            get
            {
                return null; // throw new NotImplementedException();
            }
        }

        internal override bool IsDelaySigned
        {
            get
            {
                return false; // throw new NotImplementedException(); // SourceAssembly.IsDelaySigned
            }
        }

        internal static PhpCompilation Create(
            string assemblyName,
            IEnumerable<SourceUnit> syntaxTrees = null,
            IEnumerable<MetadataReference> references = null,
            PhpCompilationOptions options = null)
        {
            Debug.Assert(options != null);
            CheckAssemblyName(assemblyName);

            var compilation = new PhpCompilation(
                assemblyName,
                options,
                ValidateReferences<CompilationReference>(references));

            //
            compilation._tables.PopulateTables(compilation, syntaxTrees);

            //
            return compilation;
        }

        internal override byte LinkerMajorVersion => 0x0;

        internal override CommonMessageProvider MessageProvider => Errors.MessageProvider.Instance;

        internal override IDictionary<ValueTuple<string, string>, MetadataReference> ReferenceDirectiveMap
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override IEnumerable<ReferenceDirective> ReferenceDirectives
        {
            get
            {
                return ImmutableArray<ReferenceDirective>.Empty; // throw new NotImplementedException();
            }
        }

        internal override StrongNameKeys StrongNameKeys
        {
            get
            {
                return StrongNameKeys.None;
            }
        }

        public override bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (filter == SymbolFilter.None)
            {
                //throw new ArgumentException(CSharpResources.NoNoneSearchCriteria, nameof(filter));
            }

            //return this.Declarations.ContainsName(predicate, filter, cancellationToken);

            throw new NotImplementedException();
        }

        public override INamedTypeSymbol CreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ImmutableArray<Diagnostic>.Empty;
        }

        public override ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Diagnostic> GetParseDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            //return GetDiagnostics(CompilationStage.Parse, false, cancellationToken);
            return ImmutableArray<Diagnostic>.Empty;
        }

        public override IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (filter == SymbolFilter.None)
            {
                //throw new ArgumentException(CSharpResources.NoNoneSearchCriteria, nameof(filter));
            }

            //return new SymbolSearcher(this).GetSymbolsWithName(predicate, filter, cancellationToken);

            throw new NotImplementedException();
        }

        public override CompilationReference ToMetadataReference(ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return a list of assembly symbols than can be accessed without using an alias.
        /// For example:
        ///   1) /r:A.dll /r:B.dll -> A, B
        ///   2) /r:Foo=A.dll /r:B.dll -> B
        ///   3) /r:Foo=A.dll /r:A.dll -> A
        /// </summary>
        internal void GetUnaliasedReferencedAssemblies(ArrayBuilder<AssemblySymbol> assemblies)
        {
            var referenceManager = GetBoundReferenceManager();
            var referencedAssemblies = referenceManager.GetReferencedAssemblies().ToArray();

            for (int i = 0; i < referencedAssemblies.Length; i++)
            {
                //if (referenceManager.DeclarationsAccessibleWithoutAlias(i))
                {
                    assemblies.Add((AssemblySymbol)referencedAssemblies[i].Value);
                }
            }
        }

        protected override void AppendDefaultVersionResource(Stream resourceStream)
        {
            var sourceAssembly = SourceAssembly;
            string fileVersion = //sourceAssembly.FileVersion ?? 
                sourceAssembly.Identity.Version.ToString();

            Win32ResourceConversions.AppendVersionToResourceStream(resourceStream,
                !this.Options.OutputKind.IsApplication(),
                fileVersion: fileVersion,
                originalFileName: this.SourceModule.Name,
                internalName: this.SourceModule.Name,
                productVersion: /*sourceAssembly.InformationalVersion ??*/ fileVersion,
                //fileDescription: sourceAssembly.Title ?? " ", //alink would give this a blank if nothing was supplied.
                //legalCopyright: sourceAssembly.Copyright ?? " ", //alink would give this a blank if nothing was supplied.
                //legalTrademarks: sourceAssembly.Trademark,
                //productName: sourceAssembly.Product,
                //comments: sourceAssembly.Description,
                //companyName: sourceAssembly.Company
                assemblyVersion: sourceAssembly.Identity.Version
                );
        }

        protected override Compilation CommonAddSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            throw new NotImplementedException();
        }

        protected override Compilation CommonClone()
        {
            throw new NotImplementedException();
        }

        protected override bool CommonContainsSyntaxTree(SyntaxTree syntaxTree)
        {
            throw new NotImplementedException();
        }

        protected override IArrayTypeSymbol CommonCreateArrayTypeSymbol(ITypeSymbol elementType, int rank)
        {
            throw new NotImplementedException();
        }

        protected override IPointerTypeSymbol CommonCreatePointerTypeSymbol(ITypeSymbol elementType)
        {
            throw new NotImplementedException();
        }

        protected override ISymbol CommonGetAssemblyOrModuleSymbol(MetadataReference reference)
        {
            throw new NotImplementedException();
        }

        protected override INamespaceSymbol CommonGetCompilationNamespace(INamespaceSymbol namespaceSymbol)
        {
            throw new NotImplementedException();
        }

        protected override IMethodSymbol CommonGetEntryPoint(CancellationToken cancellationToken)
        {
            if (_lazyMainMethod == null && this.Options.OutputKind.IsApplication())
            {
                _lazyMainMethod = FindEntryPoint(cancellationToken);
                Debug.Assert(_lazyMainMethod != null);  // TODO: ErrorCode
            }
            return _lazyMainMethod;
        }

        MethodSymbol FindEntryPoint(CancellationToken cancellationToken)
        {
            var maintype = this.Options.MainTypeName;
            if (string.IsNullOrEmpty(maintype))
            {
                // first script
                if (this.SourceSymbolTables.FirstScript != null)
                    return this.SourceSymbolTables.FirstScript.MainMethod;
            }
            else
            {
                // "ScriptFile"
                var file = this.SourceSymbolTables.GetFile(maintype);
                if (file != null)
                    return file.MainMethod;

                // "Function"
                var func = this.SourceSymbolTables.GetFunction(NameUtils.MakeQualifiedName(maintype, true));
                if (func != null)
                    return func;

                // Method
                QualifiedName qname;
                string methodname = WellKnownMemberNames.EntryPointMethodName;

                var ddot = maintype.IndexOf(Name.ClassMemberSeparator);
                if (ddot == -1)
                {
                    // "Class"::Main
                    qname = NameUtils.MakeQualifiedName(methodname, true);
                }
                else
                {
                    // "Class::Method"
                    qname = NameUtils.MakeQualifiedName(methodname.Remove(ddot), true);
                    methodname = methodname.Substring(ddot + Name.ClassMemberSeparator.Length);
                }

                var type = this.SourceSymbolTables.GetType(qname);
                if (type != null)
                {
                    var mains = type.GetMembers(methodname).OfType<SourceMethodSymbol>().AsImmutable();
                    if (mains.Length == 1)
                        return mains[0];
                }
            }

            return null;
        }

        protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility)
        {
            throw new NotImplementedException();
        }

        protected override Compilation CommonRemoveAllSyntaxTrees()
        {
            throw new NotImplementedException();
        }

        protected override Compilation CommonRemoveSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            throw new NotImplementedException();
        }

        protected override Compilation CommonReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree)
        {
            throw new NotImplementedException();
        }

        protected override Compilation CommonWithAssemblyName(string outputName)
        {
            throw new NotImplementedException();
        }

        protected override Compilation CommonWithOptions(CompilationOptions options)
        {
            throw new NotImplementedException();
        }

        protected override Compilation CommonWithReferences(IEnumerable<MetadataReference> newReferences)
        {
            throw new NotImplementedException();
        }

        protected override Compilation CommonWithScriptCompilationInfo(ScriptCompilationInfo info)
        {
            throw new NotImplementedException();
        }

        internal override AnalyzerDriver AnalyzerForLanguage(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager)
        {
            throw new NotImplementedException();
        }

        internal override CommonReferenceManager CommonGetBoundReferenceManager() => GetBoundReferenceManager();

        internal new ReferenceManager GetBoundReferenceManager()
        {
            if (_lazyAssemblySymbol == null)
            {
                _referenceManager.CreateSourceAssemblyForCompilation(this);
                Debug.Assert(_lazyAssemblySymbol != null);
            }

            // referenceManager can only be accessed after we initialized the lazyAssemblySymbol.
            // In fact, initialization of the assembly symbol might change the reference manager.
            return _referenceManager;
        }

        internal override ISymbol CommonGetWellKnownTypeMember(WellKnownMember member)
        {
            return GetWellKnownTypeMember(member);
        }

        internal override int CompareSourceLocations(Location loc1, Location loc2)
        {
            throw new NotImplementedException();
        }

        internal override bool CompileImpl(CommonPEModuleBuilder moduleBuilder, Stream win32Resources, Stream xmlDocStream, bool emittingPdb, DiagnosticBag diagnostics, Predicate<ISymbol> filterOpt, CancellationToken cancellationToken)
        {
            // The diagnostics should include syntax and declaration errors. We insert these before calling Emitter.Emit, so that the emitter
            // does not attempt to emit if there are declaration errors (but we do insert all errors from method body binding...)
            bool hasDeclarationErrors = false;  // !FilterAndAppendDiagnostics(diagnostics, GetDiagnostics(CompilationStage.Declare, true, cancellationToken));

            var moduleBeingBuilt = (PEModuleBuilder)moduleBuilder;

            if (moduleBeingBuilt.EmitOptions.EmitMetadataOnly)
            {
                throw new NotImplementedException();
            }

            // Perform initial bind of method bodies in spite of earlier errors. This is the same
            // behavior as when calling GetDiagnostics()

            // Use a temporary bag so we don't have to refilter pre-existing diagnostics.
            DiagnosticBag methodBodyDiagnosticBag = DiagnosticBag.GetInstance();

            SourceCompiler.CompileSources(
                this,
                moduleBeingBuilt,
                emittingPdb,
                hasDeclarationErrors,
                methodBodyDiagnosticBag,
                cancellationToken);

            SetupWin32Resources(moduleBeingBuilt, win32Resources, methodBodyDiagnosticBag);

            ReportManifestResourceDuplicates(
                moduleBeingBuilt.ManifestResources,
                SourceAssembly.Modules.Skip(1).Select((m) => m.Name),   //all modules except the first one
                AddedModulesResourceNames(methodBodyDiagnosticBag),
                methodBodyDiagnosticBag);

            bool hasMethodBodyErrorOrWarningAsError = !FilterAndAppendAndFreeDiagnostics(diagnostics, ref methodBodyDiagnosticBag);

            if (hasDeclarationErrors || hasMethodBodyErrorOrWarningAsError)
            {
                return false;
            }
        
            cancellationToken.ThrowIfCancellationRequested();

            // Use a temporary bag so we don't have to refilter pre-existing diagnostics.
            DiagnosticBag xmlDiagnostics = DiagnosticBag.GetInstance();

            //string assemblyName = FileNameUtilities.ChangeExtension(moduleBeingBuilt.EmitOptions.OutputNameOverride, extension: null);
            //DocumentationCommentCompiler.WriteDocumentationCommentXml(this, assemblyName, xmlDocStream, xmlDiagnostics, cancellationToken);

            if (!FilterAndAppendAndFreeDiagnostics(diagnostics, ref xmlDiagnostics))
            {
                return false;
            }

            //// Use a temporary bag so we don't have to refilter pre-existing diagnostics.
            //DiagnosticBag importDiagnostics = DiagnosticBag.GetInstance();
            //this.ReportUnusedImports(importDiagnostics, cancellationToken);

            //if (!FilterAndAppendAndFreeDiagnostics(diagnostics, ref importDiagnostics))
            //{
            //    Debug.Assert(false, "Should never produce an error");
            //    return false;
            //}

            return true;
        }

        private IEnumerable<string> AddedModulesResourceNames(DiagnosticBag diagnostics)
        {
            //ImmutableArray<ModuleSymbol> modules = SourceAssembly.Modules;

            //for (int i = 1; i < modules.Length; i++)
            //{
            //    var m = (Symbols.Metadata.PE.PEModuleSymbol)modules[i];
            //    ImmutableArray<EmbeddedResource> resources;

            //    try
            //    {
            //        resources = m.Module.GetEmbeddedResourcesOrThrow();
            //    }
            //    catch (BadImageFormatException)
            //    {
            //        diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, m), NoLocation.Singleton);
            //        continue;
            //    }

            //    foreach (var resource in resources)
            //    {
            //        yield return resource.Name;
            //    }
            //}

            yield break;
        }

        internal string GetRuntimeMetadataVersion(EmitOptions emitOptions, DiagnosticBag diagnostics)
        {
            string runtimeMDVersion = GetRuntimeMetadataVersion(emitOptions);
            if (runtimeMDVersion != null)
            {
                return runtimeMDVersion;
            }

            //DiagnosticBag runtimeMDVersionDiagnostics = DiagnosticBag.GetInstance();
            //runtimeMDVersionDiagnostics.Add(ErrorCode.WRN_NoRuntimeMetadataVersion, NoLocation.Singleton);
            //if (!FilterAndAppendAndFreeDiagnostics(diagnostics, ref runtimeMDVersionDiagnostics))
            //{
            //    return null;
            //}

            return string.Empty; //prevent emitter from crashing.
        }

        private string GetRuntimeMetadataVersion(EmitOptions emitOptions)
        {
            var corAssembly = SourceAssembly.CorLibrary as PEAssemblySymbol;

            if ((object)corAssembly != null)
            {
                return corAssembly.Assembly.ManifestModule.MetadataVersion;
            }

            return emitOptions.RuntimeMetadataVersion;
        }

        private void SetupWin32Resources(PEModuleBuilder moduleBeingBuilt, Stream win32Resources, DiagnosticBag diagnostics)
        {
            if (win32Resources == null)
                return;

            switch (DetectWin32ResourceForm(win32Resources))
            {
                case Win32ResourceForm.COFF:
                    //moduleBeingBuilt.Win32ResourceSection = MakeWin32ResourcesFromCOFF(win32Resources, diagnostics);
                    break;
                case Win32ResourceForm.RES:
                    //moduleBeingBuilt.Win32Resources = MakeWin32ResourceList(win32Resources, diagnostics);
                    break;
                default:
                    //diagnostics.Add(ErrorCode.ERR_BadWin32Res, NoLocation.Singleton, "Unrecognized file format.");
                    break;
            }
        }

        internal override CommonPEModuleBuilder CreateModuleBuilder(EmitOptions emitOptions, IMethodSymbol debugEntryPoint, IEnumerable<ResourceDescription> manifestResources, CompilationTestData testData, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            Debug.Assert(!IsSubmission || HasCodeToEmit());
            
            var runtimeMDVersion = GetRuntimeMetadataVersion(emitOptions, diagnostics);
            if (runtimeMDVersion == null)
            {
                Debug.Assert(runtimeMDVersion != null, "Set PhpCommandLineArguments.EmitOptions");
                return null;
            }

            var moduleProps = ConstructModuleSerializationProperties(emitOptions, runtimeMDVersion);

            if (manifestResources == null)
            {
                manifestResources = SpecializedCollections.EmptyEnumerable<ResourceDescription>();
            }

            PEModuleBuilder moduleBeingBuilt;
            if (_options.OutputKind.IsNetModule())
            {
                moduleBeingBuilt = new PENetModuleBuilder(
                    this,
                    SourceModule,
                    emitOptions,
                    moduleProps,
                    manifestResources);
            }
            else
            {
                var kind = _options.OutputKind.IsValid() ? _options.OutputKind : OutputKind.DynamicallyLinkedLibrary;
                moduleBeingBuilt = new PEAssemblyBuilder(
                    SourceAssembly,
                    moduleProps,
                    manifestResources,
                    kind,
                    emitOptions);
            }

            if (debugEntryPoint != null)
            {
                moduleBeingBuilt.SetDebugEntryPoint(debugEntryPoint, diagnostics);
            }

            // testData is only passed when running tests.
            if (testData != null)
            {
                //moduleBeingBuilt.SetMethodTestData(testData.Methods);
                //testData.Module = moduleBeingBuilt;
                throw new NotImplementedException();
            }

            return moduleBeingBuilt;
        }

        internal override EmitDifferenceResult EmitDifference(EmitBaseline baseline, IEnumerable<SemanticEdit> edits, Func<ISymbol, bool> isAddedSymbol, Stream metadataStream, Stream ilStream, Stream pdbStream, ICollection<MethodDefinitionHandle> updatedMethodHandles, CompilationTestData testData, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Filter out warnings based on the compiler options (/nowarn, /warn and /warnaserror) and the pragma warning directives.
        /// 'incoming' is freed.
        /// </summary>
        /// <returns>True when there is no error or warning treated as an error.</returns>
        internal override bool FilterAndAppendAndFreeDiagnostics(DiagnosticBag accumulator, ref DiagnosticBag incoming)
        {
            bool result = FilterAndAppendDiagnostics(accumulator, incoming.AsEnumerableWithoutResolution());
            incoming.Free();
            incoming = null;
            return result;
        }

        /// <summary>
        /// Filter out warnings based on the compiler options (/nowarn, /warn and /warnaserror) and the pragma warning directives.
        /// </summary>
        /// <returns>True when there is no error.</returns>
        private bool FilterAndAppendDiagnostics(DiagnosticBag accumulator, IEnumerable<Diagnostic> incoming)
        {
            bool hasError = false;
            bool reportSuppressedDiagnostics = Options.ReportSuppressedDiagnostics;

            foreach (Diagnostic d in incoming)
            {
                var filtered = _options.FilterDiagnostic(d);
                if (filtered == null ||
                    (!reportSuppressedDiagnostics && filtered.IsSuppressed))
                {
                    continue;
                }
                else if (filtered.Severity == DiagnosticSeverity.Error)
                {
                    hasError = true;
                }

                accumulator.Add(filtered);
            }

            return !hasError;
        }

        internal override int GetSyntaxTreeOrdinal(SyntaxTree tree)
        {
            throw new NotImplementedException();
        }

        internal override bool HasCodeToEmit()
        {
            //foreach (var syntaxTree in this.SyntaxTrees)
            //{
            //    var unit = syntaxTree.GetCompilationUnitRoot();
            //    if (unit.Members.Count > 0)
            //    {
            //        return true;
            //    }
            //}

            //return false;

            throw new NotImplementedException();
        }

        internal override bool HasSubmissionResult()
        {
            throw new NotImplementedException();
        }

        internal override bool IsAttributeType(ITypeSymbol type)
        {
            throw new NotImplementedException();
        }

        internal override bool IsSystemTypeReference(ITypeSymbol type)
        {
            throw new NotImplementedException();
        }

        internal override void ValidateDebugEntryPoint(IMethodSymbol debugEntryPoint, DiagnosticBag diagnostics)
        {
            throw new NotImplementedException();
        }

        internal override Compilation WithEventQueue(AsyncQueue<CompilationEvent> eventQueue)
        {
            throw new NotImplementedException();
        }
    }
}
