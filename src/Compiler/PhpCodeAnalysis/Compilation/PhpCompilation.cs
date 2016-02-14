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

namespace Pchp.CodeAnalysis
{
    internal sealed partial class PhpCompilation : Compilation
    {
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

        }

        public override ImmutableArray<MetadataReference> DirectiveReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsCaseSensitive
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Language { get; } = Constants.PhpLanguageName;
            
        public override IEnumerable<AssemblyIdentity> ReferencedAssemblyNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override IAssemblySymbol CommonAssembly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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

        protected override CompilationOptions CommonOptions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override INamedTypeSymbol CommonScriptClass
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override IModuleSymbol CommonSourceModule
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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
                throw new NotImplementedException();
            }
        }

        internal override bool IsDelaySigned
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override byte LinkerMajorVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override CommonMessageProvider MessageProvider
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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
                throw new NotImplementedException();
            }
        }

        internal override StrongNameKeys StrongNameKeys
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public override INamedTypeSymbol CreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public override CompilationReference ToMetadataReference(ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false)
        {
            throw new NotImplementedException();
        }

        protected override void AppendDefaultVersionResource(Stream resourceStream)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility)
        {
            throw new NotImplementedException();
        }

        protected override INamedTypeSymbol CommonGetSpecialType(SpecialType specialType)
        {
            throw new NotImplementedException();
        }

        protected override INamedTypeSymbol CommonGetTypeByMetadataName(string metadataName)
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

        internal override CommonReferenceManager CommonGetBoundReferenceManager()
        {
            throw new NotImplementedException();
        }

        internal override ISymbol CommonGetWellKnownTypeMember(WellKnownMember member)
        {
            throw new NotImplementedException();
        }

        internal override int CompareSourceLocations(Location loc1, Location loc2)
        {
            throw new NotImplementedException();
        }

        internal override bool CompileImpl(CommonPEModuleBuilder moduleBuilder, Stream win32Resources, Stream xmlDocStream, bool emittingPdb, DiagnosticBag diagnostics, Predicate<ISymbol> filterOpt, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override CommonPEModuleBuilder CreateModuleBuilder(EmitOptions emitOptions, IMethodSymbol debugEntryPoint, IEnumerable<ResourceDescription> manifestResources, CompilationTestData testData, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override EmitDifferenceResult EmitDifference(EmitBaseline baseline, IEnumerable<SemanticEdit> edits, Func<ISymbol, bool> isAddedSymbol, Stream metadataStream, Stream ilStream, Stream pdbStream, ICollection<MethodDefinitionHandle> updatedMethodHandles, CompilationTestData testData, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override bool FilterAndAppendAndFreeDiagnostics(DiagnosticBag accumulator, ref DiagnosticBag incoming)
        {
            throw new NotImplementedException();
        }

        internal override int GetSyntaxTreeOrdinal(SyntaxTree tree)
        {
            throw new NotImplementedException();
        }

        internal override bool HasCodeToEmit()
        {
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
