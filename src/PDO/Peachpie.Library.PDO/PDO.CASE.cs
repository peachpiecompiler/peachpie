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
        public enum PDO_CASE
        {
            /// <summary>
            /// 
            /// </summary>
            CASE_LOWER = 0,
            /// <summary>
            /// 
            /// </summary>
            CASE_NATURAL = 1,
            /// <summary>
            /// 
            /// </summary>
            CASE_UPPER = 2,
        }

        /// <summary>
        /// 
        /// </summary>
        public const int CASE_LOWER = (int)PDO_CASE.CASE_LOWER;
        /// <summary>
        /// 
        /// </summary>
        public const int CASE_NATURAL = (int)PDO_CASE.CASE_NATURAL;
        /// <summary>
        /// 
        /// </summary>
        public const int CASE_UPPER = (int)PDO_CASE.CASE_UPPER;
    }
}
