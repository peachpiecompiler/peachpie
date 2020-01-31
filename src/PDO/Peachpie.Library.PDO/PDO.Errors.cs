using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    partial class PDO
    {
        string _errorSqlState;
        string _errorCode;
        string _errorMessage;

        /// <summary>
        /// Clears the error.
        /// </summary>
        [PhpHidden]
        internal void ClearError()
        {
            _errorSqlState = null;
            _errorCode = null;
            _errorMessage = null;
        }

        /// <summary>
        /// Handles the error.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <exception cref="Peachpie.Library.PDO.PDOException">
        /// </exception>
        internal protected void HandleError(System.Exception ex)
        {
            // fill errorInfo
            Driver.HandleException(ex, out _errorSqlState, out _errorCode, out _errorMessage);

            //
            TryGetAttribute(PDO_ATTR.ATTR_ERRMODE, out var errmode);
            switch ((PDO_ERRMODE)errmode.ToLong())
            {
                case PDO_ERRMODE.ERRMODE_SILENT:
                    break;
                case PDO_ERRMODE.ERRMODE_WARNING:
                    PhpException.Throw(PhpError.E_WARNING, ex.Message);
                    break;
                case PDO_ERRMODE.ERRMODE_EXCEPTION:
                    if (ex is Pchp.Library.Spl.Exception pex)
                    {
                        throw new PDOException(pex.Message, pex.getCode(), pex);
                    }
                    else
                    {
                        throw new PDOException(ex.GetType().Name + ": " + ex.Message);
                    }
            }
        }

        /// <summary></summary>
        internal protected void RaiseError(string sqlstate, string code, string message)
        {
            _errorSqlState = sqlstate;
            _errorCode = code;
            _errorMessage = message;

            //

            TryGetAttribute(PDO_ATTR.ATTR_ERRMODE, out var errmode);
            switch ((PDO_ERRMODE)errmode.ToLong())
            {
                case PDO_ERRMODE.ERRMODE_SILENT:
                    break;

                case PDO_ERRMODE.ERRMODE_WARNING:
                    PhpException.Throw(PhpError.E_WARNING, $"{code}: {message}");   // TODO: format string in resources
                    break;

                case PDO_ERRMODE.ERRMODE_EXCEPTION:
                    throw new PDOException(message);
            }
        }

        /// <summary>
        /// Raises simple "HY000" error.
        /// </summary>
        internal protected void RaiseError(string message) => RaiseError(null, "HY000", message);

        /// <summary>
        /// Fetch the SQLSTATE associated with the last operation on the database handle
        /// </summary>
        /// <returns></returns>
        public virtual string errorCode() => _errorCode;

        /// <summary>
        /// Fetch extended error information associated with the last operation on the database handle
        /// </summary>
        /// <returns></returns>
        public virtual PhpArray errorInfo() => new PhpArray(3)
        {
            _errorSqlState, _errorCode, _errorMessage,
        };
    }
}
