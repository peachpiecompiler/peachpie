using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context : IErrorHandler
    {
        /// <summary>
        /// Gets <see cref="IErrorHandler"/> for application errors in case current <see cref="Context"/> is not provided.
        /// </summary>
        public static IErrorHandler DefaultErrorHandler { get; set; } = new DefaultErrorHandler();

        /// <summary>
        /// Whether to throw an exception on soft error (Notice, Warning, Strict).
        /// </summary>
        public bool ThrowExceptionOnError { get; set; } = true;

        /// <summary>
        /// Gets whether error reporting is disabled or enabled.
        /// </summary>
        public bool ErrorReportingDisabled => _errorReportingDisabled != 0; // && !config.ErrorControl.IgnoreAtOperator;
        int _errorReportingDisabled = 0;

        /// <summary>
        /// Disables error reporting. Can be called for multiple times. To enable reporting again 
        /// <see cref="EnableErrorReporting"/> should be called as many times as <see cref="DisableErrorReporting"/> was.
        /// </summary>
        public void DisableErrorReporting()
        {
            _errorReportingDisabled++;
        }

        /// <summary>
        /// Enables error reporting disabled by a single call to <see cref="DisableErrorReporting"/>.
        /// </summary>
        public void EnableErrorReporting()
        {
            if (_errorReportingDisabled > 0)
                _errorReportingDisabled--;
        }

        public void Throw(PhpError error, string message) => Throw(error, message, Utilities.ArrayUtils.EmptyStrings);

        public void Throw(PhpError error, string formatString, params string[] args)
        {
            // TODO: when (if) this will get called, currently errors are passed to Context.DefaultErrorHandler
        }
    }
}
