using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    partial class PDO
    {
        /// <summary>
        /// Internal representation of PDO error.
        /// </summary>
        public struct ErrorInfo
        {
            /// <summary>SQLSTATE. Can be <c>null</c>.</summary>
            public string SqlState;

            /// <summary>Error code.</summary>
            public string Code;

            /// <summary>Error message.</summary>
            public string Message;

            /// <summary>
            /// Error code for own PDO errors.
            /// </summary>
            public static string HY000 => "HY000";

            /// <summary>Gets code as number if possible.</summary>
            internal int CodeOrZero()
            {
                if (string.IsNullOrEmpty(Code) || Code == "0") return 0;
                return int.TryParse(Code, out var code) ? code : 0;
            }

            /// <summary>
            /// Create error infor for own PDO error.
            /// </summary>
            public static ErrorInfo Create(string message) => Create(null, HY000, message);

            /// <summary>
            /// Create error info.
            /// </summary>
            public static ErrorInfo Create(string sqlstate, string code, string message) => new ErrorInfo
            {
                SqlState = sqlstate,
                Code = code,
                Message = message,
            };

            /// <summary>
            /// Gets array representation of the error according to <see cref="PDO.errorInfo()"/>.
            /// </summary>
            public PhpArray ToPhpErrorInfo() => new PhpArray(3)
            {
                SqlState,
                Code,
                Message,
            };
        }

        ErrorInfo _lastError;

        /// <summary>
        /// Clears the error.
        /// </summary>
        [PhpHidden]
        internal void ClearError()
        {
            _lastError = default;
        }

        /// <summary>
        /// Handles the error.
        /// </summary>
        /// <param name="exception">The exception to be handled.</param>
        internal protected void HandleError(Exception exception)
        {
            Driver.HandleException(exception, out var error);
            HandleError(error);
        }

        /// <summary></summary>
        internal protected void HandleError(string sqlstate, string code, string message)
        {
            HandleError(ErrorInfo.Create(sqlstate, code, message));
        }

        /// <summary>Raises error according to <see cref="PDO_ATTR.ATTR_ERRMODE"/>.</summary>
        /// <exception cref="PDOException">
        /// In case <see cref="PDO_ATTR.ATTR_ERRMODE"/> is set to <see cref="PDO_ERRMODE.ERRMODE_EXCEPTION"/>, the exception is thrown.
        /// </exception>
        internal protected void HandleError(ErrorInfo error)
        {
            _lastError = error;

            //

            TryGetAttribute(PDO_ATTR.ATTR_ERRMODE, out var errmode);
            switch ((PDO_ERRMODE)errmode.ToLong())
            {
                case PDO_ERRMODE.ERRMODE_SILENT:
                    break;

                case PDO_ERRMODE.ERRMODE_WARNING:
                    PhpException.Throw(PhpError.E_WARNING, $"{error.Code}: {error.Message}");   // TODO: format string in resources
                    break;

                case PDO_ERRMODE.ERRMODE_EXCEPTION:
                    throw new PDOException(error);
            }
        }

        /// <summary>
        /// Raises simple "HY000" error.
        /// </summary>
        internal protected void HandleError(string message) => HandleError(ErrorInfo.Create(message));

        /// <summary>
        /// Fetch the SQLSTATE associated with the last operation on the database handle
        /// </summary>
        /// <returns></returns>
        public virtual string errorCode() => _lastError.Code;

        /// <summary>
        /// Fetch extended error information associated with the last operation on the database handle
        /// </summary>
        /// <returns></returns>
        public virtual PhpArray errorInfo() => _lastError.ToPhpErrorInfo();
    }
}
