using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis.Visitors;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Semantics.Model;
using Pchp.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    /// <summary>
    /// Performs compilation of all source methods.
    /// </summary>
    internal class SourceCompiler
    {
        readonly PhpCompilation _compilation;
        readonly PEModuleBuilder _moduleBuilder;
        readonly bool _emittingPdb;
        readonly DiagnosticBag _diagnostics;

        readonly Worklist<BoundBlock> _worklist;
        
        private SourceCompiler(PhpCompilation compilation, PEModuleBuilder moduleBuilder, bool emittingPdb, DiagnosticBag diagnostics)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(moduleBuilder);
            Contract.ThrowIfNull(diagnostics);

            _compilation = compilation;
            _moduleBuilder = moduleBuilder;
            _emittingPdb = emittingPdb;
            _diagnostics = diagnostics;

            // parallel worklist algorithm
            _worklist = new Worklist<BoundBlock>(AnalyzeBlock);

            // semantic model
        }

        void WalkMethods(Action<SourceRoutineSymbol> action)
        {
            // DEBUG
            var sourcesymbols = _compilation.SourceSymbolTables;
            var methods = sourcesymbols.GetFiles().SelectMany(t => t.GetMembers())
                    .Concat(sourcesymbols.GetTypes().SelectMany(t => t.GetMembers()))
                    .OfType<SourceRoutineSymbol>();
            methods.ForEach(action);

            // TODO: methodsWalker.VisitNamespace(_compilation.SourceModule.GlobalNamespace)
        }

        /// <summary>
        /// Ensures the routine has flow context.
        /// Otherwise it is created and routine is enqueued for analysis.
        /// </summary>
        void EnsureRoutine(SourceRoutineSymbol routine)
        {
            Contract.ThrowIfNull(routine);

            var cfg = routine.ControlFlowGraph;
            if (cfg == null)
            {
                // create initial flow state
                var state = StateBinder.CreateInitialState(routine);
                var binder = new SemanticsBinder(routine, state.FlowContext);

                // create control flow
                routine.ControlFlowGraph = cfg = new ControlFlowGraph(routine.Statements, binder);

                // enqueue the method for the analysis
                cfg.Start.FlowState = state;
                _worklist.Enqueue(cfg.Start);
            }
        }

        internal void BindMethods()
        {
            this.WalkMethods(EnsureRoutine);
        }

        internal void ReanalyzeMethods()
        {
            this.WalkMethods(routine => _worklist.Enqueue(routine.ControlFlowGraph.Start));
        }

        internal void AnalyzeMethods()
        {
            // _worklist.AddAnalysis:

            // Resolve variable references
            // TypeAnalysis + ResolveSymbols
            // LowerBody(block)

            // Resolve variable references
            this.WalkMethods(routine
                => GraphWalker.Walk(routine.ControlFlowGraph, new VariableResolver(routine.ControlFlowGraph.FlowContext)));

            // analyse blocks
            _worklist.DoAll();
        }

        void AnalyzeBlock(BoundBlock block) // TODO: driver
        {
            // TODO: pool of CFGAnalysis
            // TODO: async
            // TODO: in parallel
            var analysis = CFGAnalysis.Create(_worklist, new ExpressionAnalysis(new GlobalSemantics(_compilation)));
            block.Accept(analysis);
        }

        internal void EmitMethodBodies()
        {
            // source routines
            this.WalkMethods(this.EmitMethodBody);

            // <Main>`0
            _compilation.SourceSymbolTables.GetFiles().ForEach(f =>
                _moduleBuilder.CreateMainMethodWrapper(f.EnsureMainMethodRegular(), f.MainMethod, _diagnostics));

            // default .ctors
            _compilation.SourceSymbolTables.GetTypes().Cast<SourceNamedTypeSymbol>()
                .Select(t => (SynthesizedCtorWrapperSymbol)t.InstanceCtorMethodSymbol)
                .WhereNotNull()
                .ForEach(this.EmitCtorBody);

            // realize .cctor if any
            _moduleBuilder.GetTopLevelTypes(default(Microsoft.CodeAnalysis.Emit.EmitContext)).OfType<NamedTypeSymbol>()
                .ForEach(_moduleBuilder.SetStaticCtorBody);
        }

        /// <summary>
        /// Generates analyzed method.
        /// </summary>
        void EmitMethodBody(SourceRoutineSymbol routine)
        {
            Contract.ThrowIfNull(routine);
            Debug.Assert(routine.ControlFlowGraph.Start.FlowState != null);

            var body = MethodGenerator.GenerateMethodBody(_moduleBuilder, routine, 0, null, _diagnostics, _emittingPdb);
            _moduleBuilder.SetMethodBody(routine, body);
        }

        void EmitCtorBody(SynthesizedCtorWrapperSymbol ctorsymbol)
        {
            Contract.ThrowIfNull(ctorsymbol);
            MethodGenerator.EmitCtorBody(_moduleBuilder, ctorsymbol, _diagnostics, _emittingPdb);
        }

        void CompileEntryPoint(CancellationToken cancellationToken)
        {
            if (_compilation.Options.OutputKind.IsApplication() && _moduleBuilder != null)
            {
                var entryPoint = _compilation.GetEntryPoint(cancellationToken);
                if (entryPoint != null)
                {
                    // wrap call to entryPoint within real <Script>.EntryPointSymbol
                    _moduleBuilder.CreateEntryPoint((MethodSymbol)entryPoint, _diagnostics);

                    //
                    Debug.Assert(_moduleBuilder.ScriptType.EntryPointSymbol != null);
                    _moduleBuilder.SetPEEntryPoint(_moduleBuilder.ScriptType.EntryPointSymbol, _diagnostics);
                }
            }
        }

        void CompileReflectionEnumerators(CancellationToken cancellationToken)
        {
            _moduleBuilder.CreateEnumerateReferencedFunctions(_diagnostics);
            _moduleBuilder.CreateEnumerateScriptsSymbol(_diagnostics);
            _moduleBuilder.CreateEnumerateConstantsSymbol(_diagnostics);
        }

        public static void CompileSources(
            PhpCompilation compilation,
            PEModuleBuilder moduleBuilder,
            bool emittingPdb,
            bool hasDeclarationErrors,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            var compiler = new SourceCompiler(compilation, moduleBuilder, emittingPdb, diagnostics);

            // 1. Synthetize magic
            //   a.inline syntax like traits
            //   b.synthetize entry point, getters, setters, ctors, dispose, magic methods, …
            // TODO.

            // 2.Bind Syntax & Symbols to Operations (CFG)
            //   a.equivalent to building CFG
            //   b.most generic types(and empty type - mask)
            compiler.BindMethods();

            // 3.Analyze Operations
            //   a.declared variables
            //   b.build global variables/constants table
            //   c.type analysis(converge type - mask), resolve symbols
            //   d.lower semantics, update bound tree, repeat
            compiler.AnalyzeMethods();

            // 4. Emit method bodies
            compiler.EmitMethodBodies();
            compiler.CompileReflectionEnumerators(cancellationToken);

            // 5. Entry Point (.exe)
            compiler.CompileEntryPoint(cancellationToken);
        }
    }
}
