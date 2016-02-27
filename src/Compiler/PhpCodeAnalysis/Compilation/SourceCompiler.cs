using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
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

            _worklist = new Worklist<BoundBlock>(AnalyzeMethod); // parallel worklist algorithm

            // semantic model
        }

        void WalkMethods(Action<SourceRoutineSymbol> action)
        {
            // DEBUG
            var sourcesymbols = _compilation.SourceSymbolTables;
            var methods = sourcesymbols.GetFunctions()
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

            var start = routine.ControlFlowGraph.Start;

            if (start.FlowState == null)
            {
                // create initial flow state
                start.FlowState = StateBinder.CreateInitialState(routine);

                // enqueue the method for the analysis
                _worklist.Enqueue(start);
            }
        }

        internal void AnalyzeMethods()
        {
            this.WalkMethods(EnsureRoutine);

            // analyze methods
            _worklist.DoAll();
        }

        private void AnalyzeMethod(BoundBlock block)
        {
            Contract.ThrowIfNull(block);

            // TypeAnalysis + ResolveSymbols
            // if (LowerBody(block)) Enqueue(block)
        }

        /// <summary>
        /// Emits analyzed method.
        /// </summary>
        internal void EmitMethodBody(SourceRoutineSymbol routine)
        {
            Contract.ThrowIfNull(routine);

            var body = MethodGenerator.GenerateMethodBody(_moduleBuilder, routine, 0, null, _diagnostics, _emittingPdb);
            _moduleBuilder.SetMethodBody(routine, body);
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
            // Done lazily.

            // 3.Analyze Operations
            //   a.declared variables
            //   b.build global variables/constants table
            //   c.type analysis(converge type - mask), resolve symbols
            //   d.lower semantics, update bound tree, repeat
            compiler.AnalyzeMethods();

            // 4. Emit method bodies
            compiler.WalkMethods(compiler.EmitMethodBody);
        }
    }
}
