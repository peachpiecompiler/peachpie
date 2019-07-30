using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Devsense.PHP.Syntax;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Provides helper methods for resolving functions, files and types in a context (aka semantic model).
    /// </summary>
    static class CompilationSymbolsExtension
    {
        /// <summary>
        /// Gets value indicating the extension is defined in compilation time.
        /// </summary>
        public static bool HasPhpExtenion(this PhpCompilation compilation, string extension_name)
        {
            return compilation.GlobalSemantics.Extensions.Contains(extension_name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get global function symbol by its name in current context.
        /// Can be <c>null</c> if function could not be found.
        /// Can be an <see cref="AmbiguousMethodSymbol"/> in case there are more functions possible or possible overrides.
        /// </summary>
        public static IPhpRoutineSymbol ResolveFunction(this PhpCompilation compilation, QualifiedName name, SourceRoutineSymbol routine)
        {
            var symbol = compilation.GlobalSemantics.ResolveFunction(name);
            if (symbol != null)
            {
                if (symbol is AmbiguousMethodSymbol ambiguous && !ambiguous.IsOverloadable && ambiguous.Ambiguities.All(a => a is SourceFunctionSymbol))
                {
                    // there are more functions with same name within the compilation (in sources),
                    // we can pick the right one if it is declared unconditionally and in current file or included from within current routine:
                    var candidates = ambiguous.Ambiguities;
                    SourceFunctionSymbol result = null;

                    for (int i = 0; i < candidates.Length; i++)
                    {
                        var c = (SourceFunctionSymbol)candidates[i];
                        // function is unconditionally declared in this file:
                        if (c.IsUnreachable == false && !c.IsConditional && routine != null && routine.ContainingFile == c.ContainingFile)  // TODO: or {c.ContainingFile} included unconditionally
                        {
                            if (result == null)
                            {
                                result = c;
                            }
                            else
                            {
                                result = null;
                                break;
                            }
                        }
                    }

                    if (result != null)
                    {
                        symbol = result;
                    }
                }
            }

            return symbol;
        }
    }
}
