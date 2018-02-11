using Pchp.Core;
using Pchp.Core.Reflection;
using System;
using System.Diagnostics;
using System.Text;

namespace Pchp.Library
{
    internal static class ErrorsHelper
    {
        /// <summary>
        /// Gets stack trace as string.
        /// </summary>
        public static string GetStackTraceString(this PhpStackTrace trace, int skip = 0)
        {
            var lines = trace.GetLines();
            var result = new StringBuilder();

            for (int i = 1 + skip, order = 0; i < lines.Length; i++, order++)
            {
                result.AppendLine(lines[i].ToStackTraceLine(order));
            }

            return result.ToString();
        }

        /// <summary>
        /// Gets PHP `backtrace`. See <c>debug_backtrace()</c>.
        /// </summary>
        public static PhpArray GetBacktrace(this PhpStackTrace trace, int skip = 0, int limit = int.MaxValue)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException();

            var lines = trace.GetLines();
            var arr = new PhpArray();
            for (int i = 1 + skip; i < lines.Length - 1 && arr.Count < limit; i++)
            {
                arr.Add((PhpValue)lines[i].ToUserFrame());
            }

            return arr;
        }

        /// <summary>
        /// Gets exception string.
        /// </summary>
        public static string FormatExceptionString(this PhpStackTrace trace, string exceptionname, string message)
        {
            var result = new StringBuilder();

            // TODO: texts to resources

            // {exceptionname} in {location}
            // Stack trace:
            // #0 ...
            var lines = trace.GetLines();

            result.Append(exceptionname);

            if (!string.IsNullOrEmpty(message))
            {
                result.Append(": ");
                result.Append(message);
            }

            if (lines.Length != 0)
            {
                if (lines[0].HasLocation)
                {
                    result.Append(" in ");
                    result.Append(lines[0].ToStackTraceLine(-1));
                }

                if (lines.Length > 1)
                {
                    result.AppendLine();
                    result.AppendLine("Stack trace:");
                    for (int i = 1; i < lines.Length; i++)
                    {
                        result.AppendLine(lines[i].ToStackTraceLine(i - 1));
                    }
                }
            }

            return result.ToString();
        }
    }
}
