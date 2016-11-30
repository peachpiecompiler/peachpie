using Devsense.PHP.Errors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Errors
{
    /// <summary>
    /// This class serves as a database of errors that can be thrown by Devsense PHP parser. It is populated dynamically -
    /// every time the parser throws an error, it is registered in this class so that its details (such as format string)
    /// can be later retrieved.
    /// </summary>
    internal static class ParserErrors
    {
        public const int MaxParserErrorId = 3999;

        private static ConcurrentDictionary<int, ErrorType> errorTypes = new ConcurrentDictionary<int, ErrorType>();

        public static bool IsParserError(int code) => code <= MaxParserErrorId;

        public static ErrorType RegisterError(ErrorInfo errorInfo)
        {
            Debug.Assert(IsParserError(errorInfo.Id));

            return errorTypes.GetOrAdd(
                errorInfo.Id,
                (id) => new ErrorType(errorInfo.Id, ConvertSeverity(errorInfo.Severity), errorInfo.FormatString));
        }

        public static ErrorType GetError(int code)
        {
            return errorTypes[code];
        }

        private static DiagnosticSeverity ConvertSeverity(ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Information:
                    return DiagnosticSeverity.Info;
                case ErrorSeverity.Warning:
                case ErrorSeverity.WarningAsError:      // TODO: Check if it is right
                    return DiagnosticSeverity.Warning;
                case ErrorSeverity.Error:
                case ErrorSeverity.FatalError:
                    return DiagnosticSeverity.Error;
                default:
                    throw new ArgumentException(nameof(severity));
            }
        }
    }
}
