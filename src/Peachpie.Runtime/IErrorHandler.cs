using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Provides PHP errors handling.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Handles an error raised by a PHP code.
        /// </summary>
        /// <param name="error">Error category.</param>
        /// <param name="message">Error message.</param>
        void Throw(PhpError error, string message);

        /// <summary>
        /// Handles an error raised by a PHP code.
        /// </summary>
        /// <param name="error">Error category.</param>
        /// <param name="formatString">Error message format string.</param>
        /// <param name="args"><paramref name="formatString"/> arguments.</param>
        void Throw(PhpError error, string formatString, params string[] args);
    }

    /// <summary>
    /// Default error handler that throws debug assertion in case of a fatal error.
    /// All errors are logged into debug output window.
    /// </summary>
    internal sealed class DefaultErrorHandler : IErrorHandler
    {
        public void Throw(PhpError error, string message) => Throw(error, message, Array.Empty<string>());

        public void Throw(PhpError error, string formatString, params string[] args)
        {
            Debug.WriteLine(string.Format(formatString, args), "PHP");
            Debug.Assert((error & (PhpError)PhpErrorSets.Fatal) == 0, string.Format(formatString, args));
        }
    }
}
