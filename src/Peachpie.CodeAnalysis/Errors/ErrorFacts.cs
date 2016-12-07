using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace Pchp.CodeAnalysis.Errors
{
    internal static class ErrorFacts
    {
        public static DiagnosticSeverity GetSeverity(ErrorCode code)
        {
            return code.ToString().StartsWith("WRN_") ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error;
        }

        public static string GetFormatString(ErrorCode code)
        {
            return ErrorStrings.ResourceManager.GetString(code.ToString());
        }

        public static string GetFormatString(ErrorCode code, CultureInfo language)
        {
            return ErrorStrings.ResourceManager.GetString(code.ToString(), language);
        }
    }
}
