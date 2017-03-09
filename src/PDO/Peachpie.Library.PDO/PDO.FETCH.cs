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
        public enum PDO_FETCH
        {
            /// <summary>
            /// 
            /// </summary>
            FETCH_USE_DEFAULT,
            /// <summary>
            /// 
            /// </summary>
            FETCH_LAZY,
            /// <summary>
            /// 
            /// </summary>
            FETCH_ASSOC,
            /// <summary>
            /// 
            /// </summary>
            FETCH_NUM,
            /// <summary>
            /// 
            /// </summary>
            FETCH_BOTH,
            /// <summary>
            /// 
            /// </summary>
            FETCH_OBJ,
            /// <summary>
            /// return true/false only; rely on bound columns
            /// </summary>
            FETCH_BOUND,
            /// <summary>
            /// fetch a numbered column only
            /// </summary>
            FETCH_COLUMN,  
            /// <summary>
            /// create an instance of named class, call ctor and set properties
            /// </summary>
            FETCH_CLASS,   
            /// <summary>
            /// fetch row into an existing object
            /// </summary>
            FETCH_INTO,     
            /// <summary>
            /// fetch into function and return its result
            /// </summary>
            FETCH_FUNC,     
            /// <summary>
            /// like FETCH_ASSOC, but can handle duplicate names
            /// </summary>
            FETCH_NAMED,   
            /// <summary>
            /// fetch into an array where the 1st column is a key and all subsequent columns are values
            /// </summary>
            FETCH_KEY_PAIR, 
            
            /// <summary>
            /// must be last
            /// </summary>
            FETCH__MAX
        }

        /// <summary>
        /// 
        /// </summary>
        public const int FETCH_USE_DEFAULT = (int)PDO_FETCH.FETCH_USE_DEFAULT;
        /// <summary>
        /// 
        /// </summary>
        public const int FETCH_LAZY = (int)PDO_FETCH.FETCH_LAZY;
        /// <summary>
        /// 
        /// </summary>
        public const int FETCH_ASSOC = (int)PDO_FETCH.FETCH_ASSOC;
        /// <summary>
        /// 
        /// </summary>
        public const int FETCH_NUM = (int)PDO_FETCH.FETCH_NUM;
        /// <summary>
        /// 
        /// </summary>
        public const int FETCH_BOTH = (int)PDO_FETCH.FETCH_BOTH;
        /// <summary>
        /// 
        /// </summary>
        public const int FETCH_OBJ = (int)PDO_FETCH.FETCH_OBJ;
        /// <summary>
        /// return true/false only; rely on bound columns
        /// </summary>
        public const int FETCH_BOUND = (int)PDO_FETCH.FETCH_BOUND;
        /// <summary>
        /// fetch a numbered column only
        /// </summary>
        public const int FETCH_COLUMN = (int)PDO_FETCH.FETCH_COLUMN;
        /// <summary>
        /// create an instance of named class, call ctor and set properties
        /// </summary>
        public const int FETCH_CLASS = (int)PDO_FETCH.FETCH_CLASS;
        /// <summary>
        /// fetch row into an existing object
        /// </summary>
        public const int FETCH_INTO = (int)PDO_FETCH.FETCH_INTO;
        /// <summary>
        /// fetch into function and return its result
        /// </summary>
        public const int FETCH_FUNC = (int)PDO_FETCH.FETCH_FUNC;
        /// <summary>
        /// like PDO_FETCH_ASSOC, but can handle duplicate names
        /// </summary>
        public const int FETCH_NAMED = (int)PDO_FETCH.FETCH_NAMED;
        /// <summary>
        /// fetch into an array where the 1st column is a key and all subsequent columns are values
        /// </summary>
        public const int FETCH_KEY_PAIR = (int)PDO_FETCH.FETCH_KEY_PAIR;
    }
}
