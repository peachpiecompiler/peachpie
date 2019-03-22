using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Peachpie.CodeAnalysis.Errors;

namespace Pchp.CodeAnalysis.Errors
{
    /// <summary>
    /// Provides detailed information about compilation errors identified by <see cref="ErrorCode"/>.
    /// </summary>
    internal static class ErrorFacts
    {
        public static DiagnosticSeverity GetSeverity(ErrorCode code)
        {
            var name = code.ToString();
            if (name.Length < 4)
            {
                throw new ArgumentException(nameof(code));
            }

            var prefix = name.Substring(0, 4);
            switch (prefix)
            {
                case "FTL_":
                case "ERR_":
                    return DiagnosticSeverity.Error;
                case "WRN_":
                    return DiagnosticSeverity.Warning;
                case "INF_":
                    return DiagnosticSeverity.Info;
                case "HDN_":
                    return DiagnosticSeverity.Hidden;
                default:
                    throw new ArgumentException(nameof(code));
            }
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
