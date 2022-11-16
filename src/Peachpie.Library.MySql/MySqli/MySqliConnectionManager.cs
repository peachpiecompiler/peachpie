using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using Pchp.Core;

namespace Peachpie.Library.MySql.MySqli
{
    class MySqliConnectionManager : MySqlConnectionManager
    {
        /// <summary>
        /// Gets the manager singleton within runtime context.
        /// </summary>
        public static new MySqliConnectionManager GetInstance(Context ctx) => ctx.GetStatic<MySqliConnectionManager>();

        public string LastConnectionError { get; set; }

        /// <summary>
        /// The error handling mode.
        /// </summary>
        public ReportMode ReportMode { get; set; } = ReportMode.Default;

        public override void ReportException(Exception exception, string exceptionMessage)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if ((ReportMode & ReportMode.Error) != 0)
            {
                if ((ReportMode & ReportMode.Strict) != 0)
                {
                    var errcode = exception is MySqlException me ? me.ErrorCode : MySqlErrorCode.UnknownError;

                    throw new mysqli_sql_exception(exceptionMessage, (int)errcode);
                }
                else
                {
                    // outputs the error to php error handler
                    PhpException.Throw(PhpError.Warning, exceptionMessage);
                }
            }
        }
    }

    /// <summary>
    /// MYSQLI_REPORT_*** constants.
    /// </summary>
    [PhpHidden, Flags]
    public enum ReportMode
    {
        /// <summary><see cref="Constants.MYSQLI_REPORT_OFF"/></summary>
        Off = Constants.MYSQLI_REPORT_OFF,
        /// <summary><see cref="Constants.MYSQLI_REPORT_ERROR"/></summary>
        Error = Constants.MYSQLI_REPORT_ERROR,
        /// <summary><see cref="Constants.MYSQLI_REPORT_STRICT"/></summary>
        Strict = Constants.MYSQLI_REPORT_STRICT,
        /// <summary><see cref="Constants.MYSQLI_REPORT_INDEX"/></summary>
        Index = Constants.MYSQLI_REPORT_INDEX,
        /// <summary><see cref="Constants.MYSQLI_REPORT_ALL"/></summary>
        All = Constants.MYSQLI_REPORT_ALL,

        /// <summary>Default error handling flags.</summary>
        Default = Error | Strict, // as it is in PHP 8.1
    }
}
