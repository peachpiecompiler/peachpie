using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.MySql.MySqli
{
    /// <summary>
    /// MySqli PHP constants.
    /// </summary>
    [PhpExtension(ExtensionName)]
    public static class Constants
    {
        internal const string ExtensionName = "mysqli";

        //MYSQLI_READ_DEFAULT_GROUP
        //Read options from the named group from my.cnf or the file specified with MYSQLI_READ_DEFAULT_FILE

        //MYSQLI_READ_DEFAULT_FILE
        //Read options from the named option file instead of from my.cnf

        /// <summary>
        /// Connect timeout in seconds.
        /// </summary>
        public const int MYSQLI_OPT_CONNECT_TIMEOUT = 0;

        //MYSQLI_OPT_LOCAL_INFILE
        //Enables command LOAD LOCAL INFILE

        //MYSQLI_INIT_COMMAND
        //Command to execute when connecting to MySQL server.Will automatically be re-executed when reconnecting.

        /// <summary>
        /// Option whether to verify SSL cert.
        /// </summary>
        public const int MYSQLI_OPT_SSL_VERIFY_SERVER_CERT = 21;

        /// <summary>
        /// Option for RSA public key file used with the SHA-256 based authentication.
        /// </summary>
        public const int MYSQLI_SERVER_PUBLIC_KEY = 35;

        internal const int MYSQLI_CACertificateFile = 1000;
        //internal const int MYSQLI_CertificateThumbprint = 1001;
        //internal const int MYSQLI_CertificatePassword = 1002;
        internal const int MYSQLI_CertificateFile = 1003;

        /// <summary>
        /// Use SSL (encrypted protocol). This option should not be set by application programs; it is set internally in the MySQL client library
        /// </summary>
        public const int MYSQLI_CLIENT_SSL = MySql.MYSQL_CLIENT_SSL;

        /// <summary>
        /// Use compression protocol.
        /// </summary>
        public const int MYSQLI_CLIENT_COMPRESS = MySql.MYSQL_CLIENT_COMPRESS;

        /// <summary>
        /// Allow interactive_timeout seconds (instead of wait_timeout seconds) of inactivity before closing the connection.
        /// The client's session wait_timeout variable will be set to the value of the session interactive_timeout variable.
        /// </summary>
        public const int MYSQLI_CLIENT_INTERACTIVE = MySql.MYSQL_CLIENT_INTERACTIVE;


        /// <summary>
        /// Allow spaces after function names. Makes all functions names reserved words.
        /// </summary>
        public const int MYSQLI_CLIENT_IGNORE_SPACE = MySql.MYSQL_CLIENT_IGNORE_SPACE;

        //MYSQLI_CLIENT_NO_SCHEMA
        //Don't allow the db_name.tbl_name.col_name syntax.


        //MYSQLI_CLIENT_MULTI_QUERIES
        //Allows multiple semicolon-delimited queries in a single mysqli_query() call.

        /// <summary>
        /// For using buffered resultsets.
        /// </summary>
        public const int MYSQLI_STORE_RESULT = 0;

        /// <summary>
        /// For using unbuffered resultsets.
        /// </summary>
        public const int MYSQLI_USE_RESULT = 1;

        /// <summary>
        /// Add items keyed by column names.
        /// </summary>
        public const int MYSQLI_ASSOC = 1;

        /// <summary>
        /// Add items keyed by column indices.
        /// </summary>
        public const int MYSQLI_NUM = 2;

        /// <summary>
        /// Add both items keyed by column names and items keyd by column indices.
        /// </summary>
        public const int MYSQLI_BOTH = MYSQLI_ASSOC | MYSQLI_NUM;

        //MYSQLI_NOT_NULL_FLAG
        //Indicates that a field is defined as NOT NULL

        //MYSQLI_PRI_KEY_FLAG
        //Field is part of a primary index

        //MYSQLI_UNIQUE_KEY_FLAG
        //Field is part of a unique index.

        //MYSQLI_MULTIPLE_KEY_FLAG
        //Field is part of an index.

        //MYSQLI_BLOB_FLAG
        //Field is defined as BLOB

        //MYSQLI_UNSIGNED_FLAG
        //Field is defined as UNSIGNED

        //MYSQLI_ZEROFILL_FLAG
        //Field is defined as ZEROFILL

        //MYSQLI_AUTO_INCREMENT_FLAG
        //Field is defined as AUTO_INCREMENT

        //MYSQLI_TIMESTAMP_FLAG
        //Field is defined as TIMESTAMP

        //MYSQLI_SET_FLAG
        //Field is defined as SET

        //MYSQLI_NUM_FLAG
        //Field is defined as NUMERIC

        //MYSQLI_PART_KEY_FLAG
        //Field is part of an multi-index

        //MYSQLI_GROUP_FLAG
        //Field is part of GROUP BY

        //MYSQLI_TYPE_DECIMAL
        //Field is defined as DECIMAL

        //MYSQLI_TYPE_NEWDECIMAL
        //Precision math DECIMAL or NUMERIC field (MySQL 5.0.3 and up)

        //MYSQLI_TYPE_BIT
        //Field is defined as BIT(MySQL 5.0.3 and up)

        //MYSQLI_TYPE_TINY
        //Field is defined as TINYINT

        //MYSQLI_TYPE_SHORT
        //Field is defined as SMALLINT

        //MYSQLI_TYPE_LONG
        //Field is defined as INT

        //MYSQLI_TYPE_FLOAT
        //Field is defined as FLOAT

        //MYSQLI_TYPE_DOUBLE
        //Field is defined as DOUBLE

        //MYSQLI_TYPE_NULL
        //Field is defined as DEFAULT NULL

        //MYSQLI_TYPE_TIMESTAMP
        //Field is defined as TIMESTAMP

        //MYSQLI_TYPE_LONGLONG
        //Field is defined as BIGINT

        //MYSQLI_TYPE_INT24
        //Field is defined as MEDIUMINT

        //MYSQLI_TYPE_DATE
        //Field is defined as DATE

        //MYSQLI_TYPE_TIME
        //Field is defined as TIME

        //MYSQLI_TYPE_DATETIME
        //Field is defined as DATETIME

        //MYSQLI_TYPE_YEAR
        //Field is defined as YEAR

        //MYSQLI_TYPE_NEWDATE
        //Field is defined as DATE

        //MYSQLI_TYPE_INTERVAL
        //Field is defined as INTERVAL

        //MYSQLI_TYPE_ENUM
        //Field is defined as ENUM

        //MYSQLI_TYPE_SET
        //Field is defined as SET

        //MYSQLI_TYPE_TINY_BLOB
        //Field is defined as TINYBLOB

        //MYSQLI_TYPE_MEDIUM_BLOB
        //Field is defined as MEDIUMBLOB

        //MYSQLI_TYPE_LONG_BLOB
        //Field is defined as LONGBLOB

        //MYSQLI_TYPE_BLOB
        //Field is defined as BLOB

        //MYSQLI_TYPE_VAR_STRING
        //Field is defined as VARCHAR

        //MYSQLI_TYPE_STRING
        //Field is defined as CHAR or BINARY

        //MYSQLI_TYPE_CHAR
        //Field is defined as TINYINT.For CHAR, see MYSQLI_TYPE_STRING

        //MYSQLI_TYPE_GEOMETRY
        //Field is defined as GEOMETRY

        //MYSQLI_NEED_DATA
        //More data available for bind variable

        //MYSQLI_NO_DATA
        //No more data available for bind variable

        //MYSQLI_DATA_TRUNCATED
        //Data truncation occurred.Available since PHP 5.1.0 and MySQL 5.0.5.

        //MYSQLI_ENUM_FLAG
        //Field is defined as ENUM.Available since PHP 5.3.0.

        //MYSQLI_BINARY_FLAG
        //Field is defined as BINARY.Available since PHP 5.3.0.

        //MYSQLI_CURSOR_TYPE_FOR_UPDATE
        //MYSQLI_CURSOR_TYPE_NO_CURSOR
        //MYSQLI_CURSOR_TYPE_READ_ONLY
        //MYSQLI_CURSOR_TYPE_SCROLLABLE
        //MYSQLI_STMT_ATTR_CURSOR_TYPE
        //MYSQLI_STMT_ATTR_PREFETCH_ROWS
        //MYSQLI_STMT_ATTR_UPDATE_MAX_LENGTH

        /// <summary></summary>
        public const int MYSQLI_SET_CHARSET_NAME = 7;

        //MYSQLI_REPORT_INDEX
        //Report if no index or bad index was used in a query.

        //MYSQLI_REPORT_ERROR
        //Report errors from mysqli function calls.

        //MYSQLI_REPORT_STRICT
        //Throw a mysqli_sql_exception for errors instead of warnings.

        //MYSQLI_REPORT_ALL
        //Set all options on (report all).

        //MYSQLI_REPORT_OFF
        //Turns reporting off.

        //MYSQLI_DEBUG_TRACE_ENABLED
        //Is set to 1 if mysqli_debug() functionality is enabled.

        //MYSQLI_SERVER_QUERY_NO_GOOD_INDEX_USED
        //MYSQLI_SERVER_QUERY_NO_INDEX_USED
        //MYSQLI_REFRESH_GRANT
        //Refreshes the grant tables.

        //MYSQLI_REFRESH_LOG
        //Flushes the logs, like executing the FLUSH LOGS SQL statement.

        //MYSQLI_REFRESH_TABLES
        //Flushes the table cache, like executing the FLUSH TABLES SQL statement.

        //MYSQLI_REFRESH_HOSTS
        //Flushes the host cache, like executing the FLUSH HOSTS SQL statement.

        //MYSQLI_REFRESH_STATUS
        //Reset the status variables, like executing the FLUSH STATUS SQL statement.

        //MYSQLI_REFRESH_THREADS
        //Flushes the thread cache.

        //MYSQLI_REFRESH_SLAVE
        //On a slave replication server: resets the master server information, and restarts the slave. Like executing the RESET SLAVE SQL statement.

        //MYSQLI_REFRESH_MASTER
        //On a master replication server: removes the binary log files listed in the binary log index, and truncates the index file.Like executing the RESET MASTER SQL statement.

        //MYSQLI_TRANS_COR_AND_CHAIN
        //Appends "AND CHAIN" to mysqli_commit() or mysqli_rollback().

        //MYSQLI_TRANS_COR_AND_NO_CHAIN
        //Appends "AND NO CHAIN" to mysqli_commit() or mysqli_rollback().

        //MYSQLI_TRANS_COR_RELEASE
        //Appends "RELEASE" to mysqli_commit() or mysqli_rollback().

        //MYSQLI_TRANS_COR_NO_RELEASE
        //Appends "NO RELEASE" to mysqli_commit() or mysqli_rollback().

        //MYSQLI_TRANS_START_READ_ONLY
        //Start the transaction as "START TRANSACTION READ ONLY" with mysqli_begin_transaction().

        //MYSQLI_TRANS_START_READ_WRITE
        //Start the transaction as "START TRANSACTION READ WRITE" with mysqli_begin_transaction().

        //MYSQLI_TRANS_START_CONSISTENT_SNAPSHOT
        //Start the transaction as "START TRANSACTION WITH CONSISTENT SNAPSHOT" with mysqli_begin_transaction().
    }
}
