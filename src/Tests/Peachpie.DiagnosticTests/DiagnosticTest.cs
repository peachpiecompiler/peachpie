using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis;
using Peachpie.Library.Scripting;
using Xunit;
using Xunit.Abstractions;

namespace Peachpie.DiagnosticTests
{
    public class DiagnosticTest
    {
        private readonly ITestOutputHelper _output;
        private readonly PhpCompilation _emptyCompilation;

        private static readonly Regex DiagnosticAnnotationRegex = new Regex(@"/\*!([A-Z]*[0-9]*)!\*/");

        public DiagnosticTest(ITestOutputHelper output)
        {
            _output = output;
            _emptyCompilation = CreateEmptyCompilation();
        }

        [Theory]
        [DiagnosticScriptsListData]
        public void DiagnosticRunTest(string dir, string fname)
        {
            var path = Path.Combine(dir, fname);

            _output.WriteLine("Analysing {0} ...", path);

            string code = File.ReadAllText(path);
            var syntaxTree = PhpSyntaxTree.ParseCode(code, PhpParseOptions.Default, PhpParseOptions.Default, path);

            var compilation = _emptyCompilation.AddSyntaxTrees(syntaxTree);
            var actual = compilation.GetDiagnostics()
                .OrderBy(diag => diag.Location.SourceSpan.Start)
                .ToArray();

            var expected = DiagnosticAnnotationRegex.Matches(code);

            CheckDiagnostics(syntaxTree, actual, expected);
        }

        private void CheckDiagnostics(PhpSyntaxTree syntaxTree, Diagnostic[] actual, MatchCollection expected)
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

            Assert.True(isCorrect);
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

        private static PhpCompilation CreateEmptyCompilation()
        {
            var compilation = PhpCompilation.Create("project",
                references: MetadataReferences().Select((string path) => MetadataReference.CreateFromFile(path)),
                syntaxTrees: Array.Empty<PhpSyntaxTree>(),
                options: new PhpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    baseDirectory: System.IO.Directory.GetCurrentDirectory(),
                    sdkDirectory: null));

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
