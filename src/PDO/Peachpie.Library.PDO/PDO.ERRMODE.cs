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
        /// 
        /// </summary>
        [PhpHidden]
        public enum PDO_ERRMODE
        {
            /// <summary>
            /// just set error codes
            /// </summary>
            ERRMODE_SILENT = 0,
            /// <summary>
            /// raise E_WARNING
            /// </summary>
            ERRMODE_WARNING = 1,
            /// <summary>
            /// throw exceptions
            /// </summary>
            ERRMODE_EXCEPTION = 2
        }

        /// <summary>
        /// just set error codes
        /// </summary>
        public const int ERRMODE_SILENT = (int)PDO_ERRMODE.ERRMODE_SILENT;
        /// <summary>
        /// raise E_WARNING
        /// </summary>
        public const int ERRMODE_WARNING = (int)PDO_ERRMODE.ERRMODE_WARNING;
        /// <summary>
        /// throw exceptions
        /// </summary>
        public const int ERRMODE_EXCEPTION = (int)PDO_ERRMODE.ERRMODE_EXCEPTION;

        /// <summary>
        /// Corresponds to SQLSTATE '00000' and successful statement. Value returned from <see cref="errorCode()"/>.
        /// </summary>
        public const int ERR_NONE = 0;
    }
}
