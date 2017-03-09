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
    }
}
