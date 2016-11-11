using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.FlowAnalysis;
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
            _compilation.SourceSymbolTables.AllRoutines.ForEach(action);

            // TODO: methodsWalker.VisitNamespace(_compilation.SourceModule.GlobalNamespace)
        }

        void WalkTypes(Action<SourceTypeSymbol> action)
        {
            _compilation.SourceSymbolTables.GetTypes().Cast<SourceTypeSymbol>().Foreach(action);
        }

        /// <summary>
        /// Enqueues routine's start block for analysis.
        /// </summary>
        void EnqueueRoutine(SourceRoutineSymbol routine)
        {
            Contract.ThrowIfNull(routine);

            // lazily binds CFG and
            // adds their entry block to the worklist

            // TODO: reset LocalsTable, FlowContext and CFG

            _worklist.Enqueue(routine.ControlFlowGraph?.Start);

            // enqueue routine parameter default values
            routine.Parameters.OfType<SourceParameterSymbol>().Foreach(p =>
            {
                if (p.Initializer != null)
                {
                    EnqueueExpression(p.Initializer, routine.TypeRefContext, routine.GetNamingContext());
                }
            });
        }

        /// <summary>
        /// Enqueues the standalone expression for analysis.
        /// </summary>
        void EnqueueExpression(BoundExpression expression, TypeRefContext/*!*/ctx, NamingContext naming)
        {
            Contract.ThrowIfNull(expression);
            Contract.ThrowIfNull(ctx);

            var dummy = new BoundBlock()
            {
                FlowState = new FlowState(new FlowContext(ctx, null)),
                Naming = naming
            };

            dummy.Add(new BoundExpressionStatement(expression));

            _worklist.Enqueue(dummy);
        }

        /// <summary>
        /// Enqueues initializers of a class fields and constants.
        /// </summary>
        void EnqueueFieldsInitializer(SourceTypeSymbol type)
        {
            type.GetMembers().OfType<SourceFieldSymbol>().Foreach(f =>
            {
                if (f.Initializer != null)
                {
                    EnqueueExpression(
                        f.Initializer,
                        TypeRefFactory.CreateTypeRefContext(type), //the context will be lost, analysis resolves constant values only and types are temporary
                        NameUtils.GetNamingContext(type.Syntax));
                }
            });
        }

        internal void ReanalyzeMethods()
        {
            this.WalkMethods(routine => _worklist.Enqueue(routine.ControlFlowGraph.Start));
        }

        internal void AnalyzeMethods()
        {
            // _worklist.AddAnalysis:

            // TypeAnalysis + ResolveSymbols
            // LowerBody(block)

            // analyse blocks
            _worklist.DoAll();
        }

        void AnalyzeBlock(BoundBlock block) // TODO: driver
        {
            // TODO: pool of CFGAnalysis
            // TODO: async
            // TODO: in parallel

            block.Accept(AnalysisFactory());
        }

        ExpressionAnalysis AnalysisFactory()
        {
            return new ExpressionAnalysis(_worklist, new GlobalSemantics(_compilation));
        }

        internal void EmitMethodBodies()
        {
            // source routines
            this.WalkMethods(this.EmitMethodBody);
        }

        internal void EmitSynthesized()
        {
            // TODO: Visit every symbol with Synthesize() method and call it instead of following

            // ghost stubs
            this.WalkMethods(f => f.SynthesizeGhostStubs(_moduleBuilder, _diagnostics));

            // initialize RoutineInfo
            _compilation.SourceSymbolTables.GetFiles().SelectMany(f => f.Functions)
                .ForEach(f => f.EmitInit(_moduleBuilder));

            // __statics.Init, .phpnew, .ctor
            WalkTypes(t => t.EmitInit(_moduleBuilder));

            // realize .cctor if any
            _moduleBuilder.RealizeStaticCtors();
        }

        /// <summary>
        /// Generates analyzed method.
        /// </summary>
        void EmitMethodBody(SourceRoutineSymbol routine)
        {
            Contract.ThrowIfNull(routine);
            Debug.Assert(routine.ControlFlowGraph != null);
            Debug.Assert(routine.ControlFlowGraph.Start.FlowState != null);

            var body = MethodGenerator.GenerateMethodBody(_moduleBuilder, routine, 0, null, _diagnostics, _emittingPdb);
            _moduleBuilder.SetMethodBody(routine, body);
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

            // 1.Bind Syntax & Symbols to Operations (CFG)
            //   a.equivalent to building CFG
            //   b.most generic types(and empty type - mask)
            compiler.WalkMethods(compiler.EnqueueRoutine);
            compiler.WalkTypes(compiler.EnqueueFieldsInitializer);

            // 2.Analyze Operations
            //   a.declared variables
            //   b.build global variables/constants table
            //   c.type analysis(converge type - mask), resolve symbols
            //   d.lower semantics, update bound tree, repeat
            compiler.AnalyzeMethods();

            // 3. Emit method bodies
            //   a. declared routines
            //   b. synthesized symbols
            compiler.EmitMethodBodies();
            compiler.EmitSynthesized();
            compiler.CompileReflectionEnumerators(cancellationToken);

            // 4. Entry Point (.exe)
            compiler.CompileEntryPoint(cancellationToken);
        }
    }
}
