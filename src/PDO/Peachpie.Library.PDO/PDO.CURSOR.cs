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
        public enum PDO_CURSOR
        {
            /// <summary>
            /// forward only cursor (default)
            /// </summary>
            CURSOR_FWDONLY = 0,
            /// <summary>
            /// scrollable cursor
            /// </summary>
            CURSOR_SCROLL = 1
        }

        /// <summary>
        /// forward only cursor (default)
        /// </summary>
        public const int CURSOR_FWDONLY = 0;
        /// <summary>
        /// scrollable cursor
        /// </summary>
        public const int CURSOR_SCROLL = 1;
        
        #region SQLSRV_CURSOR

        ///<summary></summary>
        public enum SQLSRV_CURSOR
        {
            ///<summary></summary>
            BUFFERED = SQLSRV_CURSOR_BUFFERED,
            ///<summary></summary>
            DYNAMIC = SQLSRV_CURSOR_DYNAMIC,
            ///<summary></summary>
            KEYSET = SQLSRV_CURSOR_KEYSET,
            ///<summary></summary>
            STATIC = SQLSRV_CURSOR_STATIC,
        }
        ///<summary></summary>
        public const int SQLSRV_CURSOR_KEYSET = 1;
        ///<summary></summary>
        public const int SQLSRV_CURSOR_DYNAMIC = 2;
        ///<summary></summary>
        public const int SQLSRV_CURSOR_STATIC = 3;
        ///<summary></summary>
        public const int SQLSRV_CURSOR_BUFFERED = -2;

        #endregion
    }
}
