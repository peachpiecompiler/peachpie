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
        /// "oracle" NULL handling
        /// </summary>
        [PhpHidden]
        public enum PDO_NULL
        {
            /// <summary>
            /// 
            /// </summary>
            NULL_NATURAL = 0,
            /// <summary>
            /// 
            /// </summary>
            NULL_EMPTY_STRING = 1,
            /// <summary>
            /// 
            /// </summary>
            NULL_TO_STRING = 2
        }

        /// <summary>
        /// 
        /// </summary>
        public const int NULL_NATURAL = (int)PDO_NULL.NULL_NATURAL;
        /// <summary>
        /// 
        /// </summary>
        public const int NULL_EMPTY_STRING = (int)PDO_NULL.NULL_EMPTY_STRING;
        /// <summary>
        /// 
        /// </summary>
        public const int NULL_TO_STRING = (int)PDO_NULL.NULL_TO_STRING;
    }
}
