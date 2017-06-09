using Devsense.PHP.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Peachpie.DiagnosticTests
{
    static class SpanUtils
    {
        public static Span ToSpan(this Microsoft.CodeAnalysis.Text.TextSpan span)
        {
            return new Span(span.Start, span.Length);
        }

        public static Microsoft.CodeAnalysis.Text.TextSpan ToTextSpan(this Span span)
        {
            return new Microsoft.CodeAnalysis.Text.TextSpan(span.Start, span.Length);
        }

        public static Microsoft.CodeAnalysis.Text.TextSpan GetTextSpan(this Match match)
        {
            return new Microsoft.CodeAnalysis.Text.TextSpan(match.Index, match.Length);
        }
    }
}
