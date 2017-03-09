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
        /// fetch orientation for scrollable cursors
        /// </summary>
        [PhpHidden]
        public enum PDO_FETCH_ORI
        {
            /// <summary>
            /// default: fetch the next available row
            /// </summary>
            FETCH_ORI_NEXT = 0,
            /// <summary>
            /// scroll back to prior row and fetch that
            /// </summary>
            FETCH_ORI_PRIOR = 1,
            /// <summary>
            /// scroll to the first row and fetch that
            /// </summary>
            FETCH_ORI_FIRST = 2,
            /// <summary>
            /// scroll to the last row and fetch that
            /// </summary>
            FETCH_ORI_LAST = 3,
            /// <summary>
            /// scroll to an absolute numbered row and fetch that 
            /// </summary>
            FETCH_ORI_ABS = 4,
            /// <summary>
            /// scroll relative to the current row, and fetch that
            /// </summary>
            FETCH_ORI_REL = 5,
        }

        /// <summary>
        /// default: fetch the next available row
        /// </summary>
        public const int FETCH_ORI_NEXT = (int)PDO_FETCH_ORI.FETCH_ORI_NEXT;
        /// <summary>
        /// scroll back to prior row and fetch that
        /// </summary>
        public const int FETCH_ORI_PRIOR = (int)PDO_FETCH_ORI.FETCH_ORI_PRIOR;
        /// <summary>
        /// scroll to the first row and fetch that
        /// </summary>
        public const int FETCH_ORI_FIRST = (int)PDO_FETCH_ORI.FETCH_ORI_FIRST;
        /// <summary>
        /// scroll to the last row and fetch that
        /// </summary>
        public const int FETCH_ORI_LAST = (int)PDO_FETCH_ORI.FETCH_ORI_LAST;
        /// <summary>
        /// scroll to an absolute numbered row and fetch that 
        /// </summary>
        public const int FETCH_ORI_ABS = (int)PDO_FETCH_ORI.FETCH_ORI_ABS;
        /// <summary>
        /// scroll relative to the current row, and fetch that
        /// </summary>
        public const int FETCH_ORI_REL = (int)PDO_FETCH_ORI.FETCH_ORI_REL;
    }
}
