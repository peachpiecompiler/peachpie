using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    public static class SyntaxFactory
    {
        public static PhpSyntaxTree ParseSyntaxTree(
            string content,
            PhpParseOptions parseOptions,
            PhpParseOptions scriptParseOptions,
            string fname)
        {
            // TODO: new parser implementation based on Roslyn

            // TODO: file.IsScript ? scriptParseOptions : parseOptions
            var unit = new CodeSourceUnit(content.ToString(), fname, Encoding.UTF8);
            var errorSink = new ErrorSink();
            unit.Parse(new BasicNodesFactory(unit), errorSink);

            var result = new PhpSyntaxTree(unit, errorSink.DiagnosticStubs);

            return result;
        }
    }
}
