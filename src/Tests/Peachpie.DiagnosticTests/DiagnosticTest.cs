using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.Library.Scripting;
using Xunit;
using Xunit.Abstractions;

namespace Peachpie.DiagnosticTests
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class DiagnosticTest
    {
        private readonly ITestOutputHelper _output;

        private static readonly PhpCompilation EmptyCompilation = CreateEmptyCompilation();

        private static readonly Regex DiagnosticAnnotationRegex = new Regex(@"/\*!([A-Z]*[0-9]*)!\*/");
        private static readonly Regex TypeAnnotationRegex = new Regex(@"/\*\|([^/]*)\|\*/");
        private static readonly Regex RoutinePropertiesRegex = new Regex(@"/\*{version:([0-9]+)}\*/");
        private static readonly Regex ParameterPropertiesRegex = new Regex(@"/\*{skipPass:([01])}\*/");
        private static readonly Regex OperationPropertiesRegex = new Regex(@"/\*{skipCopy:([01])}\*/");

        /// <summary>
        /// Init test class.
        /// </summary>
        public DiagnosticTest(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Test runner.
        /// </summary>
        [Theory]
        [DiagnosticScriptsListData]
        public void DiagnosticRunTest(string dir, string fname)
        {
            var path = Path.Combine(dir, fname);

            _output.WriteLine("Analysing {0} ...", path);

            var code = File.ReadAllText(path);
            var syntaxTree = PhpSyntaxTree.ParseCode(SourceText.From(code, Encoding.UTF8), PhpParseOptions.Default, PhpParseOptions.Default, path);
            var compilation = (PhpCompilation)EmptyCompilation.AddSyntaxTrees(syntaxTree);

            bool isCorrect = true;

            // Gather and check diagnostics
            var expectedDiags = DiagnosticAnnotationRegex.Matches(code);
            var actualDiags = compilation.GetDiagnostics()
                .OrderBy(diag => diag.Location.SourceSpan.Start)
                .ToArray();
            isCorrect &= CheckDiagnostics(syntaxTree, actualDiags, expectedDiags);

            // Gather and check types and parameter properties if there are any annotations
            var expectedTypes = TypeAnnotationRegex.Matches(code);
            var expectedParamProps = ParameterPropertiesRegex.Matches(code);
            if (expectedTypes.Count > 0 || expectedParamProps.Count > 0)
            {
                var symbolsInfo = compilation.UserDeclaredRoutines
                        .Where(routine => routine.ControlFlowGraph != null)
                        .Select(routine => SymbolsSelector.Select(routine.ControlFlowGraph))
                        .Concat(compilation.UserDeclaredRoutines.Select(routine => SymbolsSelector.Select(routine)))    // routine declarations
                        .Concat(compilation.UserDeclaredTypes.Select(type => SymbolsSelector.Select(type)))    // type declarations
                        .SelectMany(enumerators => enumerators)    // IEnumerable<IEnumerable<T>> => IEnumerable<T>
                        .ToArray();                                // Cache results

                isCorrect &= CheckTypes(syntaxTree, symbolsInfo, expectedTypes);
                isCorrect &= CheckParameterProperties(syntaxTree, symbolsInfo, expectedParamProps);
            }

            // Gather and check routine properties if there are any annotations
            var expectedRoutineProps = RoutinePropertiesRegex.Matches(code);
            if (expectedRoutineProps.Count > 0)
            {
                isCorrect &= CheckRoutineProperties(syntaxTree, compilation.SourceSymbolCollection.AllRoutines, expectedRoutineProps);
            }

            // Gather and check operation properties if there are any annotations
            var expectedOpProps = OperationPropertiesRegex.Matches(code);
            if (expectedOpProps.Count > 0)
            {
                var interestingOps = compilation.UserDeclaredRoutines
                    .OfType<SourceRoutineSymbol>()
                    .SelectMany(r => OperationSelector.Select(r))
                    .ToArray();

                isCorrect &= CheckOperationProperties(syntaxTree, interestingOps, expectedOpProps);
            }

            Assert.True(isCorrect);
        }

        private bool CheckDiagnostics(PhpSyntaxTree syntaxTree, Diagnostic[] actual, MatchCollection expected)
        {
            // Compare in ascending order to systematically find all discrepancies
            bool isCorrect = true;
            int iActual = 0;
            int iExpected = 0;
            while (iActual < actual.Length && iExpected < expected.Count)
            {
                // The comment is right after the expected diagnostic
                int posActual = actual[iActual].Location.SourceSpan.End;
                int posExpected = expected[iExpected].Index;

                if (posActual < posExpected)
                {
                    isCorrect = false;
                    ReportUnexpectedDiagnostic(actual[iActual]);

                    iActual++;
                }
                else if (posActual > posExpected)
                {
                    isCorrect = false;
                    ReportMissingDiagnostic(syntaxTree, expected[iExpected]);

                    iExpected++;
                }
                else
                {
                    string idActual = actual[iActual].Id;
                    string idExpected = expected[iExpected].Groups[1].Value;

                    if (idActual != idExpected)
                    {
                        isCorrect = false;
                        ReportWrongDiagnosticId(actual[iActual], idActual, idExpected);
                    }

                    iActual++;
                    iExpected++;
                }
            }

            // Process the remainder if present
            if (iActual < actual.Length)
            {
                isCorrect = false;

                for (; iActual < actual.Length; iActual++)
                {
                    ReportUnexpectedDiagnostic(actual[iActual]);
                }
            }
            else if (iExpected < expected.Count)
            {
                isCorrect = false;

                for (; iExpected < expected.Count; iExpected++)
                {
                    ReportMissingDiagnostic(syntaxTree, expected[iExpected]);
                }
            }

            return isCorrect;
        }

        private void ReportUnexpectedDiagnostic(Diagnostic diagnostic)
        {
            var position = GetLinePosition(diagnostic.Location.GetLineSpan());
            _output.WriteLine($"Unexpected diagnostic {diagnostic.Id} on {position.Line},{position.Character}");
        }

        private void ReportMissingDiagnostic(PhpSyntaxTree syntaxTree, Match expectedMatch)
        {
            string idExpected = expectedMatch.Groups[1].Value;
            var span = new TextSpan(expectedMatch.Index, 0);
            var position = GetLinePosition(syntaxTree.GetLineSpan(span));
            _output.WriteLine($"Missing diagnostic {idExpected} on {position.Line},{position.Character}");
        }

        private void ReportWrongDiagnosticId(Diagnostic diagnostic, string idActual, string idExpected)
        {
            var position = GetLinePosition(diagnostic.Location.GetLineSpan());
            _output.WriteLine($"Wrong diagnostic {idActual} instead of {idExpected} on {position.Line},{position.Character}");
        }

        private static LinePosition GetLinePosition(FileLinePositionSpan span)
        {
            // It is zero-based both for line and character, therefore we must add 1
            var originalPos = span.StartLinePosition;
            return new LinePosition(originalPos.Line + 1, originalPos.Character + 1);
        }

        private bool CheckTypes(PhpSyntaxTree syntaxTree, IEnumerable<SymbolsSelector.SymbolStat> symbolStats, MatchCollection expectedTypes)
        {
            var positionSymbolMap = new Dictionary<int, SymbolsSelector.SymbolStat>();
            foreach (var stat in symbolStats)
            {
                positionSymbolMap.TryAdd(stat.Span.Start, stat);
            }

            // Type annotation is voluntary; therefore, check only the specified types
            bool isCorrect = true;
            foreach (Match match in expectedTypes)
            {
                // The text of symbol should start where the annotation ends
                int expectedPos = match.Index + match.Length;
                if (!positionSymbolMap.TryGetValue(expectedPos, out var symbolStat))
                {
                    var linePos = GetLinePosition(syntaxTree.GetLineSpan(match.GetTextSpan()));
                    _output.WriteLine($"Cannot get type information for type annotation on {linePos.Line},{linePos.Character}");
                    isCorrect = false;
                    continue;
                }

                // Obtain the type of the given symbol or expression
                string actualType = GetTypeString(symbolStat);
                if (string.IsNullOrEmpty(actualType))
                {
                    var linePos = GetLinePosition(syntaxTree.GetLineSpan(symbolStat.Span.ToTextSpan()));
                    _output.WriteLine($"Unable to get the type of the symbol on {linePos.Line},{linePos.Character}");
                    isCorrect = false;
                    continue;
                }

                // Reorder expected types alphabetically
                string expectedType = string.Join("|", match.Groups[1].Value.Split('|').OrderBy(s => s));

                // Report any problem
                if (actualType != expectedType)
                {
                    var linePos = GetLinePosition(syntaxTree.GetLineSpan(symbolStat.Span.ToTextSpan()));
                    _output.WriteLine(
                        $"Wrong type {actualType} instead of {expectedType} of the expression on {linePos.Line},{linePos.Character}");
                    isCorrect = false;
                }
            }

            return isCorrect;
        }

        private string GetTypeString(SymbolsSelector.SymbolStat symbolStat)
        {
            if (symbolStat.TypeCtx == null)
            {
                return null;
            }

            if (symbolStat.BoundExpression != null)
            {
                return symbolStat.TypeCtx.ToString(symbolStat.BoundExpression.TypeRefMask);
            }
            else if (symbolStat.Symbol is IPhpValue typedValue)
            {
                TypeRefMask typeMask = typedValue.GetResultType(symbolStat.TypeCtx);
                return symbolStat.TypeCtx.ToString(typeMask);
            }
            else
            {
                return null;
            }
        }

        private bool CheckRoutineProperties(PhpSyntaxTree syntaxTree, IEnumerable<SourceRoutineSymbol> userDeclaredRoutines, MatchCollection expectedRoutineProps)
        {
            var positionRoutineMap = new Dictionary<int, SourceRoutineSymbol>(
                from routine in userDeclaredRoutines
                let position = (routine.Syntax as FunctionDecl)?.ParametersSpan.End ?? (routine.Syntax as MethodDecl)?.ParametersSpan.End
                where position != null
                select new KeyValuePair<int, SourceRoutineSymbol>(position.Value, routine));

            // Routine properties are voluntary; therefore, check only the specified ones
            bool isCorrect = true;
            foreach (Match match in expectedRoutineProps)
            {
                int expectedPos = match.Index;
                if (!positionRoutineMap.TryGetValue(expectedPos, out var routine))
                {
                    var linePos = GetLinePosition(syntaxTree.GetLineSpan(match.GetTextSpan()));
                    _output.WriteLine($"Cannot get routine information for properties on {linePos.Line},{linePos.Character}");
                    isCorrect = false;
                    continue;
                }

                int expectedVersion = int.Parse(match.Groups[1].Value);
                int actualVersion = routine.ControlFlowGraph.FlowContext.Version;
                if (expectedVersion != actualVersion)
                {
                    var linePos = GetLinePosition(syntaxTree.GetLineSpan(match.GetTextSpan()));
                    _output.WriteLine(
                        $"Wrong final flow analysis version {actualVersion} instead of {expectedVersion} of the routine {routine.Name} on {linePos.Line},{linePos.Character}");
                    isCorrect = false;
                }
            }

            return isCorrect;
        }

        private bool CheckParameterProperties(PhpSyntaxTree syntaxTree, IEnumerable<SymbolsSelector.SymbolStat> symbolStats, MatchCollection expectedParamProps)
        {
            var positionParamMap = new Dictionary<int, SourceParameterSymbol>(
                from symbolStat in symbolStats
                let symbol = symbolStat.Symbol
                where symbol is SourceParameterSymbol
                select new KeyValuePair<int, SourceParameterSymbol>(symbolStat.Span.End, (SourceParameterSymbol)symbol));

            bool isCorrect = true;
            foreach (Match match in expectedParamProps)
            {
                int expectedPos = match.Index;
                if (!positionParamMap.TryGetValue(expectedPos, out var param))
                {
                    var linePos = GetLinePosition(syntaxTree.GetLineSpan(match.GetTextSpan()));
                    _output.WriteLine($"Cannot get parameter information for properties on {linePos.Line},{linePos.Character}");
                    isCorrect = false;
                    continue;
                }

                bool expectedSkipPass = (int.Parse(match.Groups[1].Value) != 0);
                bool actualSkipPass = !param.CopyOnPass;
                if (expectedSkipPass != actualSkipPass)
                {
                    var linePos = GetLinePosition(syntaxTree.GetLineSpan(match.GetTextSpan()));
                    _output.WriteLine(
                        $"Wrong value of SkipPass {actualSkipPass} instead of {expectedSkipPass} of the parameter {param.Name} in {param.Routine.Name} on {linePos.Line},{linePos.Character}");
                    isCorrect = false;
                }
            }

            return isCorrect;
        }

        private bool CheckOperationProperties(PhpSyntaxTree syntaxTree, IEnumerable<IPhpOperation> interestingOps, MatchCollection expectedOpProps)
        {
            var copyPositionSet = new HashSet<int>(
                interestingOps
                .OfType<BoundCopyValue>()
                .Select(c => c.Expression.PhpSyntax.Span.End));

            bool isCorrect = true;
            foreach (Match match in expectedOpProps)
            {
                bool expectedSkipCopy = (int.Parse(match.Groups[1].Value) != 0);
                bool actualSkipCopy = !copyPositionSet.Contains(match.Index);
                if (expectedSkipCopy != actualSkipCopy)
                {
                    var linePos = GetLinePosition(syntaxTree.GetLineSpan(match.GetTextSpan()));
                    _output.WriteLine(
                        $"Wrong value of copy skipping {actualSkipCopy} instead of {expectedSkipCopy} of the expression on {linePos.Line},{linePos.Character}");
                    isCorrect = false;
                }
            }

            return isCorrect;
        }

        private static PhpCompilation CreateEmptyCompilation()
        {
            var compilation = PhpCompilation.Create("project",
                references: MetadataReferences().Select((string path) => MetadataReference.CreateFromFile(path)),
                syntaxTrees: Array.Empty<PhpSyntaxTree>(),
                options: new PhpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    baseDirectory: System.IO.Directory.GetCurrentDirectory(),
                    sdkDirectory: null,
                    optimizationLevel: PhpOptimizationLevel.Release));

            // bind reference manager, cache all references
            var assemblytmp = compilation.Assembly;

            return compilation;
        }

        /// <summary>
        /// Collect references we have to pass to the compilation.
        /// </summary>
        private static IEnumerable<string> MetadataReferences()
        {
            // implicit references
            var types = new List<Type>()
            {
                typeof(object),                 // mscorlib (or System.Runtime)
                typeof(Pchp.Core.Context),      // Peachpie.Runtime
                typeof(Pchp.Library.Strings),   // Peachpie.Library
                typeof(ScriptingProvider),      // Peachpie.Library.Scripting
            };

            var list = types.Distinct().Select(ass => ass.GetTypeInfo().Assembly).ToList();
            var set = new HashSet<Assembly>(list);

            for (int i = 0; i < list.Count; i++)
            {
                var assembly = list[i];
                var refs = assembly.GetReferencedAssemblies();
                foreach (var refname in refs)
                {
                    var refassembly = Assembly.Load(refname);
                    if (refassembly != null && set.Add(refassembly))
                    {
                        list.Add(refassembly);
                    }
                }
            }

            return list.Select(ass => ass.Location);
        }
    }
}
