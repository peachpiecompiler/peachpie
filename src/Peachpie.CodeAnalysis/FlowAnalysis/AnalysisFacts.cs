using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    static class AnalysisFacts
    {
        /// <summary>
        /// Resolves value of the function call in compile time if possible.
        /// </summary>
        public static Optional<object> TryResolve(BoundGlobalFunctionCall x, ISymbolProvider model)
        {
            if (x.Name.IsDirect && x.ArgumentsInSourceOrder.All(arg => arg.Value.ConstantValue.HasValue))
            {
                // direct func name with all arguments resolved:

                // take the function name ignoring current namespace resolution, simple names only:
                var name = x.NameOpt.HasValue ? x.NameOpt.Value : x.Name.NameValue;
                if (name.IsSimpleName)
                {
                    var args = x.ArgumentsInSourceOrder;
                    switch (name.Name.Value)
                    {
                        case "function_exists":
                            if (args.Length == 1)
                            {
                                // TRUE <=> function name is defined unconditionally in a reference library (PE assembly)
                                var str = args[0].Value.ConstantValue.Value as string;
                                if (str != null)
                                {
                                    var tmp = model.ResolveFunction(NameUtils.MakeQualifiedName(str, true));
                                    if (tmp is PEMethodSymbol || (tmp is AmbiguousMethodSymbol && ((AmbiguousMethodSymbol)tmp).Ambiguities.All(f => f is PEMethodSymbol)))
                                    {
                                        return new Optional<object>(true);
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            //
            return default(Optional<object>);
        }
    }
}