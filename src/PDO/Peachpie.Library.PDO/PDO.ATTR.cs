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
        public enum PDO_ATTR
        {
            /// <summary>
            /// use to turn on or off auto-commit mode
            /// </summary>
            ATTR_AUTOCOMMIT = 0,
            /// <summary>
            /// configure the prefetch size for drivers that support it. Size is in KB
            /// </summary>
            ATTR_PREFETCH = 1,
            /// <summary>
            /// connection timeout in seconds
            /// </summary>
            ATTR_TIMEOUT = 2,
            /// <summary>
            /// control how errors are handled
            /// </summary>
            ATTR_ERRMODE = 3,
            /// <summary>
            /// database server version
            /// </summary>
            ATTR_SERVER_VERSION = 4,
            /// <summary>
            /// client library version
            /// </summary>
            ATTR_CLIENT_VERSION = 5,
            /// <summary>
            /// server information
            /// </summary>
            ATTR_SERVER_INFO = 6,
            /// <summary>
            /// connection status
            /// </summary>
            ATTR_CONNECTION_STATUS = 7,
            /// <summary>
            /// control case folding for portability
            /// </summary>
            ATTR_CASE = 8,
            /// <summary>
            /// name a cursor for use in "WHERE CURRENT OF &lt;name&gt;" 
            /// </summary>
            ATTR_CURSOR_NAME = 9,
            /// <summary>
            /// cursor type
            /// </summary>
            ATTR_CURSOR = 10,
            /// <summary>
            /// name of the driver (as used in the constructor) 
            /// </summary>
            ATTR_DRIVER_NAME = 11,
            /// <summary>
            /// convert empty strings to NULL
            /// </summary>
            ATTR_ORACLE_NULLS = 12,
            /// <summary>
            /// pconnect style connection
            /// </summary>
            ATTR_PERSISTENT = 13,
            /// <summary>
            /// array(classname, array(ctor_args)) to specify the class of the constructed statement
            /// </summary>
            ATTR_STATEMENT_CLASS = 14,
            /// <summary>
            /// include the catalog/db name names in the column names, where available
            /// </summary>
            ATTR_FETCH_CATALOG_NAMES = 15,
            /// <summary>
            /// include table names in the column names, where available
            /// </summary>
            ATTR_FETCH_TABLE_NAMES = 16,
            /// <summary>
            /// converts integer/float types to strings during fetch
            /// </summary>
            ATTR_STRINGIFY_FETCHES = 17,
            /// <summary>
            /// make database calculate maximum length of data found in a column
            /// </summary>
            ATTR_MAX_COLUMN_LEN = 18,
            /// <summary>
            /// Set the default fetch mode
            /// </summary>
            ATTR_DEFAULT_FETCH_MODE = 19,
            /// <summary>
            /// use query emulation rather than native
            /// </summary>
            ATTR_EMULATE_PREPARES = 20,

            /// <summary>
            /// this defines the start of the range for driver specific options
            /// </summary>
            ATTR_DRIVER_SPECIFIC = 1000
        }
        /// <summary>
        /// use to turn on or off auto-commit mode
        /// </summary>
        public const int ATTR_AUTOCOMMIT = (int)PDO_ATTR.ATTR_AUTOCOMMIT;
        /// <summary>
        /// configure the prefetch size for drivers that support it. Size is in KB
        /// </summary>
        public const int ATTR_PREFETCH = (int)PDO_ATTR.ATTR_PREFETCH;
        /// <summary>
        /// connection timeout in seconds
        /// </summary>
        public const int ATTR_TIMEOUT = (int)PDO_ATTR.ATTR_TIMEOUT;
        /// <summary>
        /// control how errors are handled
        /// </summary>
        public const int ATTR_ERRMODE = (int)PDO_ATTR.ATTR_ERRMODE;
        /// <summary>
        /// database server version
        /// </summary>
        public const int ATTR_SERVER_VERSION = (int)PDO_ATTR.ATTR_SERVER_VERSION;
        /// <summary>
        /// client library version
        /// </summary>
        public const int ATTR_CLIENT_VERSION = (int)PDO_ATTR.ATTR_CLIENT_VERSION;
        /// <summary>
        /// server information
        /// </summary>
        public const int ATTR_SERVER_INFO = (int)PDO_ATTR.ATTR_SERVER_INFO;
        /// <summary>
        /// connection status
        /// </summary>
        public const int ATTR_CONNECTION_STATUS = (int)PDO_ATTR.ATTR_CONNECTION_STATUS;
        /// <summary>
        /// control case folding for portability
        /// </summary>
        public const int ATTR_CASE = (int)PDO_ATTR.ATTR_CASE;
        /// <summary>
        /// name a cursor for use in "WHERE CURRENT OF &lt;name&gt;" 
        /// </summary>
        public const int ATTR_CURSOR_NAME = (int)PDO_ATTR.ATTR_CURSOR_NAME;
        /// <summary>
        /// cursor type
        /// </summary>
        public const int ATTR_CURSOR = (int)PDO_ATTR.ATTR_CURSOR;
        /// <summary>
        /// name of the driver (as used in the constructor) 
        /// </summary>
        public const int ATTR_DRIVER_NAME = (int)PDO_ATTR.ATTR_DRIVER_NAME;
        /// <summary>
        /// convert empty strings to NULL
        /// </summary>
        public const int ATTR_ORACLE_NULLS = (int)PDO_ATTR.ATTR_ORACLE_NULLS;
        /// <summary>
        /// pconnect style connection
        /// </summary>
        public const int ATTR_PERSISTENT = (int)PDO_ATTR.ATTR_PERSISTENT;
        /// <summary>
        /// array(classname, array(ctor_args)) to specify the class of the constructed statement
        /// </summary>
        public const int ATTR_STATEMENT_CLASS = (int)PDO_ATTR.ATTR_STATEMENT_CLASS;
        /// <summary>
        /// include the catalog/db name names in the column names, where available
        /// </summary>
        public const int ATTR_FETCH_CATALOG_NAMES = (int)PDO_ATTR.ATTR_FETCH_CATALOG_NAMES;
        /// <summary>
        /// include table names in the column names, where available
        /// </summary>
        public const int ATTR_FETCH_TABLE_NAMES = (int)PDO_ATTR.ATTR_FETCH_TABLE_NAMES;
        /// <summary>
        /// converts integer/float types to strings during fetch
        /// </summary>
        public const int ATTR_STRINGIFY_FETCHES = (int)PDO_ATTR.ATTR_STRINGIFY_FETCHES;
        /// <summary>
        /// make database calculate maximum length of data found in a column
        /// </summary>
        public const int ATTR_MAX_COLUMN_LEN = (int)PDO_ATTR.ATTR_MAX_COLUMN_LEN;
        /// <summary>
        /// Set the default fetch mode
        /// </summary>
        public const int ATTR_DEFAULT_FETCH_MODE = (int)PDO_ATTR.ATTR_DEFAULT_FETCH_MODE;
        /// <summary>
        /// use query emulation rather than native
        /// </summary>
        public const int ATTR_EMULATE_PREPARES = (int)PDO_ATTR.ATTR_EMULATE_PREPARES;

        /// <summary>
        /// this defines the start of the range for driver specific options
        /// </summary>
        public const int ATTR_DRIVER_SPECIFIC = (int)PDO_ATTR.ATTR_DRIVER_SPECIFIC;
    }
}
