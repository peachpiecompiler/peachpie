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
        [PhpHidden, Flags]
        public enum PDO_FETCH
        {
            /// <summary>
            /// Zero. No flags.
            /// </summary>
            Default = 0,
            
            /// <summary>
            /// 
            /// </summary>
            FETCH_LAZY = 1,
            
            /// <summary>
            /// 
            /// </summary>
            FETCH_ASSOC = 2,
            
            /// <summary>
            /// 
            /// </summary>
            FETCH_NUM = 3,
            
            /// <summary>
            /// 
            /// </summary>
            FETCH_BOTH = 4,
            
            /// <summary>
            /// 
            /// </summary>
            FETCH_OBJ = 5,
            
            /// <summary>
            /// return true/false only; rely on bound columns
            /// </summary>
            FETCH_BOUND = 6,
            
            /// <summary>
            /// fetch a numbered column only
            /// </summary>
            FETCH_COLUMN = 7,  
            
            /// <summary>
            /// create an instance of named class, call ctor and set properties
            /// </summary>
            FETCH_CLASS = 8,
            
            /// <summary>
            /// fetch row into an existing object
            /// </summary>
            FETCH_INTO = 9,     
            
            /// <summary>
            /// fetch into function and return its result
            /// </summary>
            FETCH_FUNC = 10,     
            
            /// <summary>
            /// like FETCH_ASSOC, but can handle duplicate names
            /// </summary>
            FETCH_NAMED = 11,

            /// <summary>
            /// Fetch a two-column result into an array where the first column is a key and the second column is the value
            /// </summary>
            FETCH_KEY_PAIR = 12,

            /// <summary>
            /// Group return by values. Usually combined with PDO::FETCH_COLUMN or PDO::FETCH_KEY_PAIR
            /// </summary>
            FETCH_GROUP = 0x10000,

            /// <summary>
            /// Fetch only the unique values.
            /// </summary>
            FETCH_UNIQUE = 0x30000,

            /// <summary>
            /// Determine the class name from the value of first column.
            /// </summary>
            FETCH_CLASSTYPE = 0x40000,

            /// <summary>
            /// As PDO::FETCH_INTO but object is provided as a serialized string. Available since PHP 5.1.0. Since PHP 5.3.0 the class constructor is never called if this flag is set.
            /// </summary>
            FETCH_SERIALIZE = 0x80000,

            /// <summary>
            /// Call the constructor before setting properties.
            /// </summary>
            FETCH_PROPS_LATE = 0x100000,

            /// <summary>
            /// Additional flags mask combined with fetch mode.
            /// </summary>
            Flags = 0xff0000,
        }

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

        /// <summary>
        /// Group return by values. Usually combined with PDO::FETCH_COLUMN or PDO::FETCH_KEY_PAIR
        /// </summary>
        public const int FETCH_GROUP = (int)PDO_FETCH.FETCH_GROUP;

        /// <summary>
        /// Fetch only the unique values.
        /// </summary>
        public const int FETCH_UNIQUE = (int)PDO_FETCH.FETCH_UNIQUE;

        /// <summary>
        /// Determine the class name from the value of first column.
        /// </summary>
        public const int FETCH_CLASSTYPE = (int)PDO_FETCH.FETCH_CLASSTYPE;

        /// <summary>
        /// As PDO::FETCH_INTO but object is provided as a serialized string. Available since PHP 5.1.0. Since PHP 5.3.0 the class constructor is never called if this flag is set.
        /// </summary>
        public const int FETCH_SERIALIZE = (int)PDO_FETCH.FETCH_SERIALIZE;

        /// <summary>
        /// Call the constructor before setting properties.
        /// </summary>
        public const int FETCH_PROPS_LATE = (int)PDO_FETCH.FETCH_PROPS_LATE;
    }
}
