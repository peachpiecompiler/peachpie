using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.Emit;
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
    internal static class SourceCompiler
    {
        public static void CompileMethodBodies(
            PhpCompilation compilation,
            PEModuleBuilder module,
            bool emittingPdb,
            bool hasDeclarationErrors,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            var sourcesymbols = compilation.SourceSymbolTables;
            // var callgraph = new ...
            // var worklist = new ... // parralel worklist algorithm

            // 1.Bind Syntax & Symbols to Operations (CFG)
            //   a.equivalent to building CFG
            //   b.most generic types(and empty type - mask)
            //   c.inline syntax like traits

            // 2.Analyze Operations + Synthetize(magic)
            //   a.synthetize entry point, getters, setters, ctors, dispose, magic methods, …
            //   b.type analysis(converge type - mask), resolve symbols
            //   c.update types(from type-mask)

            // 3. Emit method bodies

            // debug:
            var methods = sourcesymbols.GetFunctions()
                .Concat(sourcesymbols.GetTypes().SelectMany(t => t.GetMembers()))
                .OfType<SourceBaseMethodSymbol>();

            methods.Foreach(m =>
            {
                var body = MethodGenerator.GenerateMethodBody(module, m, 0, null, diagnostics, emittingPdb);
                module.SetMethodBody(m, body);
            });
        }
    }
}
