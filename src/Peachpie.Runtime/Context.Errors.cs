using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            // TODO: once this method gets called, pass the error to actual error handler

            PhpException.Throw(error, formatString, args);
        }

        /// <summary>
        /// Performs debug assertion.
        /// </summary>
        /// <param name="condition">Condition to be checked.</param>
        /// <param name="action">Either nothing, a message or a <c>Throwable</c>(implementing <see cref="Exception"/>).</param>
        /// <returns></returns>
        public bool Assert(bool condition, PhpValue action = default(PhpValue))
        {
            if (condition == false)
            {
                AssertFailed(action);
            }

            //
            return condition;
        }

        /// <summary>
        /// Invoked by runtime when PHP assertion fails.
        /// </summary>
        protected virtual void AssertFailed(PhpValue action = default(PhpValue))
        {
            const string AssertionErrorName = "AssertionError";

            var t_assertex = GetDeclaredType(AssertionErrorName);
            Debug.Assert(t_assertex != null);

            Exception exception; // exception to be thrown

            if (action.IsSet && !action.IsEmpty)
            {
                var description = action.AsString();
                exception = action.AsObject() as Exception ?? (Exception)t_assertex.Creator(this, (PhpValue)description);
            }
            else
            {
                exception = (Exception)t_assertex.Creator(this);
            }

            //
            throw exception;
        }
    }
}
