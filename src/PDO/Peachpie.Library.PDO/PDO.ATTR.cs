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
            /// <summary>default string parameter type</summary>
            DEFAULT_STR_PARAM = PDO.ATTR_DEFAULT_STR_PARAM,

            /// <summary>
            /// this defines the start of the range for driver specific options
            /// </summary>
            ATTR_DRIVER_SPECIFIC = PDO.ATTR_DRIVER_SPECIFIC,
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
        /// default string parameter type, this can be one of <see cref="PARAM_STR_NATL"/> and <see cref="PARAM_STR_CHAR"/>.
        /// </summary>
        public const int ATTR_DEFAULT_STR_PARAM = 21;

        /// <summary>
        /// this defines the start of the range for driver specific options
        /// </summary>
        public const int ATTR_DRIVER_SPECIFIC = 1000;

        #region MYSQL_ATTR

        ///<summary>MySql specific attributes.</summary>
        public enum MYSQL_ATTR
        {
            ///<summary></summary>
            USE_BUFFERED_QUERY = PDO.MYSQL_ATTR_USE_BUFFERED_QUERY,
            ///<summary></summary>
            LOCAL_INFILE = PDO.MYSQL_ATTR_LOCAL_INFILE,
            ///<summary></summary>
            INIT_COMMAND = PDO.MYSQL_ATTR_INIT_COMMAND,
            ///<summary></summary>
            COMPRESS = PDO.MYSQL_ATTR_COMPRESS,
            ///<summary></summary>
            DIRECT_QUERY = PDO.MYSQL_ATTR_DIRECT_QUERY,
            ///<summary></summary>
            FOUND_ROWS = PDO.MYSQL_ATTR_FOUND_ROWS,
            ///<summary></summary>
            IGNORE_SPACE = PDO.MYSQL_ATTR_IGNORE_SPACE,
            ///<summary></summary>
            SSL_KEY = PDO.MYSQL_ATTR_SSL_KEY,
            ///<summary></summary>
            SSL_CERT = PDO.MYSQL_ATTR_SSL_CERT,
            ///<summary></summary>
            SSL_CA = PDO.MYSQL_ATTR_SSL_CA,
            ///<summary></summary>
            SSL_CAPATH = PDO.MYSQL_ATTR_SSL_CAPATH,
            ///<summary></summary>
            SSL_CIPHER = PDO.MYSQL_ATTR_SSL_CIPHER,
            ///<summary></summary>
            SERVER_PUBLIC_KEY = PDO.MYSQL_ATTR_SERVER_PUBLIC_KEY,
            ///<summary></summary>
            MULTI_STATEMENTS = PDO.MYSQL_ATTR_MULTI_STATEMENTS,
            ///<summary></summary>
            SSL_VERIFY_SERVER_CERT = PDO.MYSQL_ATTR_SSL_VERIFY_SERVER_CERT,
        }

        ///<summary></summary>
        public const int MYSQL_ATTR_USE_BUFFERED_QUERY = 1000;
        ///<summary></summary>
        public const int MYSQL_ATTR_LOCAL_INFILE = 1001;
        ///<summary></summary>
        public const int MYSQL_ATTR_INIT_COMMAND = 1002;
        ///<summary></summary>
        public const int MYSQL_ATTR_COMPRESS = 1003;
        ///<summary></summary>
        public const int MYSQL_ATTR_DIRECT_QUERY = 1004;
        ///<summary></summary>
        public const int MYSQL_ATTR_FOUND_ROWS = 1005;
        ///<summary></summary>
        public const int MYSQL_ATTR_IGNORE_SPACE = 1006;
        ///<summary></summary>
        public const int MYSQL_ATTR_SSL_KEY = 1007;
        ///<summary></summary>
        public const int MYSQL_ATTR_SSL_CERT = 1008;
        ///<summary></summary>
        public const int MYSQL_ATTR_SSL_CA = 1009;
        ///<summary></summary>
        public const int MYSQL_ATTR_SSL_CAPATH = 1010;
        ///<summary></summary>
        public const int MYSQL_ATTR_SSL_CIPHER = 1011;
        ///<summary></summary>
        public const int MYSQL_ATTR_SERVER_PUBLIC_KEY = 1012;
        ///<summary></summary>
        public const int MYSQL_ATTR_MULTI_STATEMENTS = 1013;
        ///<summary></summary>
        public const int MYSQL_ATTR_SSL_VERIFY_SERVER_CERT = 1014;

        #endregion

        #region SQLITE_ATTR

        ///<summary></summary>
        public enum SQLITE_ATTR
        {
            ///<summary></summary>
            OPEN_FLAGS = PDO.SQLITE_ATTR_OPEN_FLAGS,
            ///<summary></summary>
            READONLY_STATEMENT = PDO.SQLITE_ATTR_READONLY_STATEMENT,
            ///<summary></summary>
            EXTENDED_RESULT_CODES = PDO.SQLITE_ATTR_EXTENDED_RESULT_CODES,
        }

        ///<summary></summary>
        public const int SQLITE_ATTR_OPEN_FLAGS = 1000;
        ///<summary></summary>
        public const int SQLITE_ATTR_READONLY_STATEMENT = 1001;
        ///<summary></summary>
        public const int SQLITE_ATTR_EXTENDED_RESULT_CODES = 1002;

        #endregion

        #region SQLSRV_ATTR

        ///<summary></summary>
        public enum SQLSRV_ATTR
        {
            ///<summary></summary>
            ENCODING = SQLSRV_ATTR_ENCODING,
            ///<summary></summary>
            ENCODING_DEFAULT = SQLSRV_ENCODING_DEFAULT,
            ///<summary></summary>
            ENCODING_BINARY = SQLSRV_ENCODING_BINARY,
            ///<summary></summary>
            ENCODING_SYSTEM = SQLSRV_ENCODING_SYSTEM,
            ///<summary></summary>
            ENCODING_UTF8 = SQLSRV_ENCODING_UTF8,
            ///<summary></summary>
            CURSOR_SCROLL_TYPE = SQLSRV_ATTR_CURSOR_SCROLL_TYPE,
            ///<summary></summary>
            DIRECT_QUERY = SQLSRV_ATTR_DIRECT_QUERY,
            ///<summary></summary>
            FETCHES_NUMERIC_TYPE = SQLSRV_ATTR_FETCHES_NUMERIC_TYPE,
            ///<summary></summary>
            QUERY_TIMEOUT = SQLSRV_ATTR_QUERY_TIMEOUT,
            ///<summary></summary>
            PARAM_OUT_DEFAULT_SIZE = SQLSRV_PARAM_OUT_DEFAULT_SIZE,
        }

        ///<summary></summary>
        public const int SQLSRV_ENCODING_DEFAULT = 1;
        ///<summary></summary>
        public const int SQLSRV_ENCODING_BINARY = 2;
        ///<summary></summary>
        public const int SQLSRV_ENCODING_SYSTEM = 3;
        ///<summary></summary>
        public const int SQLSRV_ENCODING_UTF8 = 65001;
        ///<summary></summary>
        public const int SQLSRV_ATTR_ENCODING = 1000;
        ///<summary></summary>
        public const int SQLSRV_ATTR_QUERY_TIMEOUT = 1001;
        ///<summary></summary>
        public const int SQLSRV_ATTR_DIRECT_QUERY = 1002;
        ///<summary></summary>
        public const int SQLSRV_ATTR_CURSOR_SCROLL_TYPE = 1003;
        ///<summary></summary>
        public const int SQLSRV_ATTR_FETCHES_NUMERIC_TYPE = 1005;
        ///<summary></summary>
        public const int SQLSRV_PARAM_OUT_DEFAULT_SIZE = -1;

        #endregion
    }
}
