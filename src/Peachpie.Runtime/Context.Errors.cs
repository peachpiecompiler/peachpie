using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Gets whether error reporting is disabled or enabled.
        /// </summary>
        public bool ErrorReportingDisabled => _errorReportingDisabled != 0; // && !config.ErrorControl.IgnoreAtOperator;
        int _errorReportingDisabled;

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

        /// <summary>
        /// Performs debug assertion.
        /// </summary>
        /// <param name="condition">Condition to be checked.</param>
        /// <param name="action">Either nothing, a message or a <c>Throwable</c>(implementing <see cref="Exception"/>).</param>
        /// <returns></returns>
        public bool Assert(bool condition, PhpValue action = default)
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
        protected virtual void AssertFailed(PhpValue action = default)
        {
            // exception to be thrown
            var userexception = action.AsObject() as Exception;
            var usermessage = action.AsString();

            var exception = userexception ?? PhpException.AssertionErrorException(usermessage ?? string.Empty);

            throw exception;
        }
    }
}
