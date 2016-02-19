using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Semantics;
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

        private SourceCompiler(PhpCompilation compilation, PEModuleBuilder moduleBuilder, bool emittingPdb, DiagnosticBag diagnostics)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(moduleBuilder);
            Contract.ThrowIfNull(diagnostics);

            _compilation = compilation;
            _moduleBuilder = moduleBuilder;
            _emittingPdb = emittingPdb;
            _diagnostics = diagnostics;

            // var callgraph = new ...
            // var worklist = new ... // parallel worklist algorithm
            // semantic model
        }

        private void WalkMethods(Action<SourceBaseMethodSymbol> action)
        {
            // DEBUG
            var sourcesymbols = _compilation.SourceSymbolTables;
            var methods = sourcesymbols.GetFunctions()
                    .Concat(sourcesymbols.GetTypes().SelectMany(t => t.GetMembers()))
                    .OfType<SourceBaseMethodSymbol>();
            methods.Foreach(action);

            // TODO: methodsWalker.VisitNamespace(_compilation.SourceModule.GlobalNamespace)
        }

        internal BoundMethodBody BindMethod(SourceBaseMethodSymbol method)
        {
            Contract.ThrowIfNull(method);

            return method.BoundBlock;
        }

        internal void AnalyzeMethod(SourceBaseMethodSymbol method)
        {
            Contract.ThrowIfNull(method);

            var bound = method.BoundBlock;
        }

        /// <summary>
        /// Emits analyzed method.
        /// </summary>
        internal void EmitMethodBody(SourceBaseMethodSymbol method)
        {
            Contract.ThrowIfNull(method);

            var body = MethodGenerator.GenerateMethodBody(_moduleBuilder, method, 0, null, _diagnostics, _emittingPdb);
            _moduleBuilder.SetMethodBody(method, body);
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
            compiler.WalkMethods(compiler.AnalyzeMethod);

            // 4. Emit method bodies
            compiler.WalkMethods(compiler.EmitMethodBody);
        }
    }
}
