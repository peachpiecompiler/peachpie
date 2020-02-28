using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.Errors;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    internal partial class DiagnosticWalker<T>
    {
        /// <summary>
        /// Matches <c>printf()</c> format specifier.
        /// </summary>
        static readonly Lazy<Regex> s_printfSpecsRegex = new Lazy<Regex>(
            () => new Regex(@"%(?:(\d)+\$)?[+-]?(?:[ 0]|'.{1})?-?\d*(?:\.\d+)?[bcdeEufFgGosxX]", RegexOptions.Compiled | RegexOptions.CultureInvariant)
        );

        void printfCheck(string name, ImmutableArray<BoundArgument> arguments)
        {
            // Check that the number of arguments matches the format string
            if (arguments.Length != 0 &&
                arguments[0].Value.ConstantValue.TryConvertToString(out string format))
            {
                int posSpecCount = 0;
                int numSpecMax = 0;
                foreach (Match match in s_printfSpecsRegex.Value.Matches(format))
                {
                    var numSpecStr = match.Groups[1].Value;
                    if (numSpecStr == string.Empty)
                    {
                        // %d
                        posSpecCount++;
                    }
                    else
                    {
                        // %2$d
                        int numSpec = int.Parse(numSpecStr);
                        numSpecMax = Math.Max(numSpec, numSpecMax);
                    }
                }

                int expectedArgCount = 1 + Math.Max(posSpecCount, numSpecMax);

                if (arguments.Length != expectedArgCount)
                {
                    // Wrong number of arguments with respect to the format string

                    _diagnostics.Add(
                        _routine, arguments[0].Value.GetTextSpan(),
                        ErrorCode.WRN_FormatStringWrongArgCount, name.ToLowerInvariant(), expectedArgCount, arguments.Length);
                }
            }
        }

        void CheckGlobalFunctionCall(BoundGlobalFunctionCall call)
        {
            // TODO: regular Roslyn analyzers as part of the referenced assembly

            if (AnalysisFacts.HasSimpleName(call, out string name))
            {
                if (name.Equals("printf", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("sprintf", StringComparison.OrdinalIgnoreCase))
                {
                    printfCheck(name, call.ArgumentsInSourceOrder);
                }
            }
        }
    }
}
