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
using Pchp.CodeAnalysis.DocumentationComments;

namespace Pchp.CodeAnalysis
{
    public sealed partial class PhpCompilation : Compilation
    {
        readonly SourceSymbolCollection _tables;
        MethodSymbol _lazyMainMethod;
        readonly PhpCompilationOptions _options;

        Task<IEnumerable<Diagnostic>> _lazyAnalysisTask;

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
        internal AssemblySymbol CorLibrary => GetBoundReferenceManager().CorLibrary;

        /// <summary>
        /// PHP COR library containing PHP runtime.
        /// </summary>
        internal AssemblySymbol PhpCorLibrary => GetBoundReferenceManager().PhpCorLibrary;

        /// <summary>
        /// Tables containing all source symbols to be compiled.
        /// Used for enumeration and lookup.
        /// </summary>
        internal SourceSymbolCollection SourceSymbolCollection => _tables;

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

        public new PhpCompilationOptions Options => _options;

        /// <summary>
        /// Gets enumeration of all user declared routines (global code, functions, methods and lambdas) in the compilation.
        /// </summary>
        public IEnumerable<IPhpRoutineSymbol> UserDeclaredRoutines => this.SourceSymbolCollection.AllRoutines;

        /// <summary>
        /// Gets enumeration of all user declared types (classes, interfaces and traits) in the compilation.
        /// </summary>
        public IEnumerable<IPhpTypeSymbol> UserDeclaredTypes => this.SourceSymbolCollection.GetTypes();

        private PhpCompilation(
            string assemblyName,
            PhpCompilationOptions options,
            ImmutableArray<MetadataReference> references,
            bool isSubmission,
            ReferenceManager referenceManager = null,
            bool reuseReferenceManager = false,
            //SyntaxAndDeclarationManager syntaxAndDeclarations
            AsyncQueue<CompilationEvent> eventQueue = null
            )
            : base(assemblyName, references, SyntaxTreeCommonFeatures(ImmutableArray<SyntaxTree>.Empty), isSubmission, eventQueue)
        {
            _wellKnownMemberSignatureComparer = new WellKnownMembersSignatureComparer(this);

            _options = options;
            _tables = new SourceSymbolCollection(this);
            _coreTypes = new CoreTypes(this);
            _coreMethods = new CoreMethods(_coreTypes);
            _anonymousTypeManager = new AnonymousTypeManager(this);

            _referenceManager = (reuseReferenceManager && referenceManager != null)
                ? referenceManager
                : new ReferenceManager(MakeSourceAssemblySimpleName(), options.AssemblyIdentityComparer, referenceManager?.ObservedMetadata, options.SdkDirectory);
        }

        /// <summary>
        /// Create a duplicate of this compilation with different symbol instances.
        /// </summary>
        public new PhpCompilation Clone()
        {
            return Update(reuseReferenceManager: true);
        }

        private PhpCompilation Update(
            string assemblyName = null,
            PhpCompilationOptions options = null,
            IEnumerable<MetadataReference> references = null,
            ReferenceManager referenceManager = null,
            bool reuseReferenceManager = false,
            IEnumerable<PhpSyntaxTree> syntaxTrees = null)
        {
            var compilation = new PhpCompilation(
                assemblyName ?? this.AssemblyName,
                options ?? _options,
                this.ExternalReferences.AddRange((references == null) ? ImmutableArray<MetadataReference>.Empty : references),
                //this.PreviousSubmission,
                //this.SubmissionReturnType,
                //this.HostObjectType,
                this.IsSubmission,
                referenceManager ?? _referenceManager,
                reuseReferenceManager,
                EventQueue);

            compilation.SourceSymbolCollection.AddSyntaxTreeRange(syntaxTrees ?? SyntaxTrees);

            return compilation;
        }

        private PhpCompilation WithPhpSyntaxTrees(IEnumerable<PhpSyntaxTree> syntaxTrees)
        {
            return Update(
                reuseReferenceManager: true,
                syntaxTrees: syntaxTrees);
        }

        public PhpCompilation WithPhpOptions(PhpCompilationOptions options)
        {
            return Update(options: options);
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
                return this.CoreTypes.Object.Symbol;
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

        public static PhpCompilation Create(
            string assemblyName,
            IEnumerable<PhpSyntaxTree> syntaxTrees = null,
            IEnumerable<MetadataReference> references = null,
            PhpCompilationOptions options = null)
        {
            Debug.Assert(options != null);
            CheckAssemblyName(assemblyName);

            var compilation = new PhpCompilation(
                assemblyName,
                options,
                ValidateReferences<CompilationReference>(references),
                false);

            //
            compilation.SourceSymbolCollection.AddSyntaxTreeRange(syntaxTrees);

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
            return new MissingMetadataTypeSymbol(name, arity, false);
        }

        /// <summary>
        /// The bag in which semantic analysis should deposit its diagnostics.
        /// </summary>
        internal DiagnosticBag DeclarationDiagnostics
        {
            get
            {
                if (_lazyDeclarationDiagnostics == null)
                {
                    var diagnostics = new DiagnosticBag();
                    Interlocked.CompareExchange(ref _lazyDeclarationDiagnostics, diagnostics, null);
                }

                return _lazyDeclarationDiagnostics;
            }
        }

        private DiagnosticBag _lazyDeclarationDiagnostics;

        public override ImmutableArray<Diagnostic> GetParseDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnostics(CompilationStage.Parse, false, cancellationToken);
        }

        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnostics(CompilationStage.Declare, false, cancellationToken);
        }

        public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnostics(CompilationStage.Compile, false, cancellationToken);
        }

        public override ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnostics(CompilationStage.Compile, true, cancellationToken);
        }

        internal ImmutableArray<Diagnostic> GetDiagnostics(CompilationStage stage, bool includeEarlierStages, CancellationToken cancellationToken)
        {
            var builder = DiagnosticBag.GetInstance();

            // Parse
            if (stage == CompilationStage.Parse || (stage > CompilationStage.Parse && includeEarlierStages))
            {
                var syntaxTrees = this.SyntaxTrees;
                if (this.Options.ConcurrentBuild)
                {
                    Parallel.ForEach(syntaxTrees, UICultureUtilities.WithCurrentUICulture<PhpSyntaxTree>(syntaxTree =>
                        {
                            builder.AddRange(syntaxTree.GetDiagnostics(cancellationToken));
                        }));
                }
                else
                {
                    foreach (var syntaxTree in syntaxTrees)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        builder.AddRange(syntaxTree.GetDiagnostics(cancellationToken));
                    }
                }
            }

            // Declare
            if (stage == CompilationStage.Declare || stage > CompilationStage.Declare && includeEarlierStages)
            {
                // CheckAssemblyName(builder);
                builder.AddRange(Options.Errors);

                cancellationToken.ThrowIfCancellationRequested();

                // the set of diagnostics related to establishing references.
                builder.AddRange(GetBoundReferenceManager().Diagnostics);

                //cancellationToken.ThrowIfCancellationRequested();

                builder.AddRange(this.BindAndAnalyseTask().Result.AsImmutable());   // TODO: cancellationToken

                cancellationToken.ThrowIfCancellationRequested();

                builder.AddRange(_lazyDeclarationDiagnostics?.AsEnumerable() ?? Enumerable.Empty<Diagnostic>());
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Compile
            if (stage == CompilationStage.Compile || stage > CompilationStage.Compile && includeEarlierStages)
            {
                var methodBodyDiagnostics = DiagnosticBag.GetInstance();
                // TODO: perform compilation and report diagnostics
                // GetDiagnosticsForAllMethodBodies(methodBodyDiagnostics, cancellationToken); 
                builder.AddRangeAndFree(methodBodyDiagnostics);
            }

            // Before returning diagnostics, we filter warnings
            // to honor the compiler options (e.g., /nowarn, /warnaserror and /warn) and the pragmas.
            var result = DiagnosticBag.GetInstance();
            FilterAndAppendAndFreeDiagnostics(result, ref builder);
            return result.ToReadOnlyAndFree<Diagnostic>();
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

        protected override Compilation CommonClone()
        {
            return Clone();
        }

        protected override IArrayTypeSymbol CommonCreateArrayTypeSymbol(ITypeSymbol elementType, int rank)
        {
            return ArrayTypeSymbol.CreateCSharpArray(SourceAssembly, (TypeSymbol)elementType, rank: rank);
        }

        protected override IPointerTypeSymbol CommonCreatePointerTypeSymbol(ITypeSymbol elementType)
        {
            return new PointerTypeSymbol((TypeSymbol)elementType);
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
                if (this.SourceSymbolCollection.FirstScript != null)
                    return this.SourceSymbolCollection.FirstScript.MainMethod;
            }
            else
            {
                // "ScriptFile"
                var file = this.SourceSymbolCollection.GetFile(maintype);
                if (file != null)
                    return file.MainMethod;

                // "Function"
                var func = this.SourceSymbolCollection.GetFunction(NameUtils.MakeQualifiedName(maintype, true));
                if (!func.IsErrorMethod())
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

                var type = this.SourceSymbolCollection.GetType(qname);
                if (type != null)
                {
                    var mains = type.GetMembers(methodname).OfType<SourceMethodSymbol>().AsImmutable();
                    if (mains.Length == 1)
                        return mains[0];
                }
            }

            return null;
        }

        protected override IEnumerable<SyntaxTree> CommonSyntaxTrees => SyntaxTrees;

        public new IEnumerable<PhpSyntaxTree> SyntaxTrees => this.SourceSymbolCollection.SyntaxTrees;

        protected override bool CommonContainsSyntaxTree(SyntaxTree syntaxTree)
        {
            return this.SyntaxTrees.Contains(syntaxTree);
        }

        protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility)
        {
            throw new NotImplementedException();
        }

        protected override Compilation CommonAddSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            return WithPhpSyntaxTrees(this.SyntaxTrees.Concat(trees.Cast<PhpSyntaxTree>()));
        }

        protected override Compilation CommonRemoveAllSyntaxTrees()
        {
            return WithPhpSyntaxTrees(ImmutableArray<PhpSyntaxTree>.Empty);
        }

        protected override Compilation CommonRemoveSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            return WithPhpSyntaxTrees(this.SyntaxTrees.Except(trees.OfType<PhpSyntaxTree>()));
        }

        protected override Compilation CommonReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree)
        {
            if (oldTree == null)
            {
                throw new ArgumentNullException(nameof(oldTree));
            }

            if (newTree == null)
            {
                return this.RemoveSyntaxTrees(oldTree);
            }

            if (newTree == oldTree)
            {
                return this;
            }

            if (!ContainsSyntaxTree(oldTree))
            {
                throw new KeyNotFoundException();
            }

            return WithPhpSyntaxTrees(SyntaxTrees.Select(t => (t == oldTree) ? (PhpSyntaxTree)newTree : t));
        }

        internal override int GetSyntaxTreeOrdinal(SyntaxTree tree)
        {
            Debug.Assert(this.ContainsSyntaxTree(tree));
            return SourceSymbolCollection.OrdinalMap[tree];
        }

        protected override Compilation CommonWithAssemblyName(string outputName)
        {
            if (this.AssemblyName == outputName)
            {
                return this;
            }

            return Update(assemblyName: outputName);
        }

        protected override Compilation CommonWithOptions(CompilationOptions options)
        {
            if (!(options is PhpCompilationOptions))
            {
                throw ExceptionUtilities.UnexpectedValue(options);
            }

            return WithPhpOptions((PhpCompilationOptions)options);
        }

        protected override Compilation CommonWithReferences(IEnumerable<MetadataReference> newReferences)
        {
            return Update(references: newReferences);
        }

        protected override Compilation CommonWithScriptCompilationInfo(ScriptCompilationInfo info)
        {
            throw new NotImplementedException();
        }

        internal override AnalyzerDriver AnalyzerForLanguage(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager)
        {
            Func<SyntaxNode, int> getKind = node => node.RawKind;
            Func<SyntaxTrivia, bool> isComment = trivia => false;
            return new AnalyzerDriver<int>(analyzers, getKind, analyzerManager, isComment);
        }

        internal override CommonReferenceManager CommonGetBoundReferenceManager()
        {
            return GetBoundReferenceManager();
        }

        internal new ReferenceManager GetBoundReferenceManager()
        {
            if (_lazyAssemblySymbol == null)
            {
                lock (_referenceManager)
                {
                    _lazyAssemblySymbol = _referenceManager.CreateSourceAssemblyForCompilation(this);
                }
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
            Debug.Assert(loc1.IsInSource);
            Debug.Assert(loc2.IsInSource);

            var comparison = CompareSyntaxTreeOrdering(loc1.SourceTree, loc2.SourceTree);
            if (comparison != 0)
            {
                return comparison;
            }

            return loc1.SourceSpan.Start - loc2.SourceSpan.Start;
        }

        /// <summary>
        /// Ensures semantic binding and flow analysis.
        /// </summary>
        /// <returns>The result of the task contains enumeration of diagnostics.</returns>
        public async Task<IEnumerable<Diagnostic>> BindAndAnalyseTask()
        {
            if (_lazyAnalysisTask == null)
            {
                _lazyAnalysisTask = Task.Run(() => SourceCompiler.BindAndAnalyze(this));
            }

            return await _lazyAnalysisTask;
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

            string assemblyName = FileNameUtilities.ChangeExtension(moduleBeingBuilt.EmitOptions.OutputNameOverride, extension: null);
            DocumentationCommentCompiler.WriteDocumentationCommentXml(this, assemblyName, xmlDocStream, xmlDiagnostics, cancellationToken);

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

        internal override bool HasCodeToEmit()
        {
            return SourceSymbolCollection.GetFiles().Any();
        }

        internal override bool HasSubmissionResult()
        {
            throw new NotImplementedException();
        }

        internal override void ValidateDebugEntryPoint(IMethodSymbol debugEntryPoint, DiagnosticBag diagnostics)
        {
            Debug.Assert(debugEntryPoint != null);

            // Debug entry point has to be a method definition from this compilation.
            var methodSymbol = debugEntryPoint as MethodSymbol;
            if (methodSymbol?.DeclaringCompilation != this || !methodSymbol.IsDefinition)
            {
                //diagnostics.Add(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition, Location.None);
            }
        }

        internal override Compilation WithEventQueue(AsyncQueue<CompilationEvent> eventQueue)
        {
            throw new NotImplementedException();
        }
    }
}
