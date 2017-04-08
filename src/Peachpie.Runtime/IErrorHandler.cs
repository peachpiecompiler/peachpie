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
        public void Throw(PhpError error, string message)
        {
            Debug.WriteLine(message, "PHP");
            Debug.Assert((error & (PhpError)PhpErrorSets.Fatal) == 0, message);
        }

        public void Throw(PhpError error, string formatString, params string[] args) => Throw(error, string.Format(formatString, args));
    }

    /// <summary>
    /// A custom error handler providing events to be subscribed to.
    /// </summary>
    public class CustomErrorHandler : IErrorHandler
    {
        void IErrorHandler.Throw(PhpError error, string message)
        {
            OnError?.Invoke(this, new ErrorEventArgs() { Error = error, Message = message });
        }

        void IErrorHandler.Throw(PhpError error, string formatString, params string[] args) => ((IErrorHandler)this).Throw(error, string.Format(formatString, args));

        /// <summary>
        /// Invoked when an error in PHP code occurs.
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError;
    }

    /// <summary>
    /// <see cref="CustomErrorHandler.OnError"/> event arguments.
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Error category.
        /// </summary>
        public PhpError Error { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; set; }

        // TODO: callstack
    }
}
