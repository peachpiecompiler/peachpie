using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.MySql.MySqli
{
    /// <summary>
    /// MySqli procedural style API.
    /// </summary>
    [PhpExtension(Constants.ExtensionName)]
    public static class Functions
    {
        /// <summary>
        /// Internal object used to store per-request (context) data.
        /// </summary>
        internal class MySqliContextData
        {
            public static MySqliContextData/*!*/GetContextData(Context ctx) => ctx.GetStatic<MySqliContextData>();

            public string LastConnectionError { get; set; }
        }

        /// <summary>
        /// Initializes MySQLi and returns a resource for use with mysqli_real_connect().
        /// </summary>
        [return: NotNull]
        public static mysqli/*!*/mysqli_init() => new mysqli();

        /// <summary>
        /// Pings a server connection, or tries to reconnect if the connection has gone down.
        /// </summary>
        public static bool mysqli_ping(mysqli link) => link.ping();

        /// <summary>
        /// Closes a previously opened database connection.
        /// </summary>
        public static bool mysqli_close(mysqli link) => link.close();

        /// <summary>
        /// Opens a connection to a mysql server
        /// </summary>
        public static bool mysqli_real_connect(Context ctx, mysqli link, string host = null, string username = null, string passwd = null, string dbname = "", int port = -1, string socket = null, int flags = 0)
            => link.real_connect(ctx, host, username, passwd, dbname, port, socket, flags);

        /// <summary>
        /// Opens a connection to a mysql server
        /// </summary>
        public static mysqli mysqli_connect(Context ctx, string host = null, string username = null, string passwd = null, string dbname = "", int port = -1, string socket = null, int flags = 0)
        {
            var link = new mysqli(ctx, host, username, passwd, dbname, port, socket);
            return (string.IsNullOrEmpty(link.connect_error)) ? link : null;
        }

        /// <summary>
        /// Escapes special characters in a string for use in an SQL statement, taking into account the current charset of the connection.
        /// </summary>
        public static PhpString mysqli_escape_string(mysqli link, PhpString escapestr) => mysqli_real_escape_string(link, escapestr);

        /// <summary>
        /// Escapes special characters in a string for use in an SQL statement, taking into account the current charset of the connection.
        /// </summary>
        public static PhpString mysqli_real_escape_string(mysqli link, PhpString escapestr) => link.real_escape_string(escapestr);

        /// <summary>
        /// Gets the number of affected rows in a previous MySQL operation.
        /// </summary>
        public static int mysqli_affected_rows(mysqli link) => link.affected_rows;

        /// <summary>
        /// Returns a string description of the last error.
        /// </summary>
        public static string mysqli_error(mysqli link) => link.error;

        /// <summary>
        /// The connection error message. Otherwise <c>null</c>.
        /// </summary>
        public static string mysqli_connect_error(Context ctx, mysqli link = null)
            => (link != null) ? link.connect_error : MySqliContextData.GetContextData(ctx).LastConnectionError;

        /// <summary>
        /// Returns the error code from last connect call.
        /// </summary>
        public static int mysqli_connect_errno(Context ctx, mysqli link = null)
            => (link != null)
                ? link.connect_errno
                : string.IsNullOrEmpty(MySqliContextData.GetContextData(ctx).LastConnectionError) ? 0 : -1;

        /// <summary>
        /// Returns the error code for the most recent function call.
        /// </summary>
        public static int mysqli_errno(mysqli link) => link.errno;

        /// <summary>
        /// Sets the default client character set.
        /// </summary>
        public static bool mysqli_set_charset(mysqli link, string charset) => link.set_charset(charset);

        /// <summary>
        /// Returns the version of the MySQL server.
        /// </summary>
        public static string mysqli_get_server_info(mysqli link) => link.server_info;

        /// <summary>
        /// Returns a string representing the type of connection used.
        /// </summary>
        public static string mysqli_get_host_info(mysqli link) => link.host_info;

        /// <summary>
        /// Returns the version of the MySQL server as an integer.
        /// </summary>
        public static int mysqli_get_server_version(mysqli link) => link.server_version;

        /// <summary>
        /// Get MySQL client info.
        /// </summary>
        public static string mysqli_get_client_info(mysqli link = null) => mysqli.ClientInfo;

        /// <summary>
        /// Returns the MySQL client version as an integer.
        /// </summary>
        public static int mysqli_get_client_version(mysqli link = null) => mysqli.ClientVersion;

        /// <summary>
        /// Returns the default character set for the database connection.
        /// </summary>
        public static string mysqli_client_encoding(mysqli link) => link.character_set_name();

        /// <summary>
        /// Returns the default character set for the database connection.
        /// </summary>
        public static string mysqli_character_set_name(mysqli link) => link.character_set_name();

        /// <summary>
        /// Check if there are any more query results from a multi query.
        /// </summary>
        public static bool mysqli_more_results(mysqli link) => link.more_results();

        /// <summary>
        /// Prepare next result from multi_query.
        /// </summary>
        public static bool mysqli_next_result(mysqli link) => link.next_result();

        /// <summary>
        /// Returns the thread ID for the current connection.
        /// </summary>
        public static int mysqli_thread_id(mysqli link) => link.thread_id;

        /// <summary>
        /// Selects the default database for database queries.
        /// </summary>
        public static bool mysqli_select_db(mysqli link, string dbname) => link.select_db(dbname);

        /// <summary>
        /// Performs a query on the database.
        /// </summary>
        public static PhpValue mysqli_query(mysqli link, PhpString query, int resultmode = Constants.MYSQLI_STORE_RESULT) => link.query(query, resultmode);

        /// <summary>
        /// Returns the auto generated id used in the latest query.
        /// </summary>
        public static long mysqli_insert_id(mysqli link) => link.insert_id;

        /// <summary>
        /// Used to set extra connect options and affect behavior for a connection.
        /// </summary>
        public static bool mysqli_options(mysqli link, int option, PhpValue value) => link.options(option, value);

        /// <summary>
        /// Used for establishing secure connections using SSL
        /// </summary>
        /// <returns>Always true.</returns>
        public static bool mysqli_ssl_set(mysqli link, string key = null, string cert = null, string ca = null, string capath = null, string cipher = null)
            => link.ssl_set(key, cert, ca, capath, cipher);

        /// <summary>
        /// Fetch a result row as an associative, a numeric array, or both.
        /// </summary>
        public static PhpArray mysqli_fetch_array(mysqli_result result, int resulttype = Constants.MYSQLI_BOTH) => result.fetch_array(resulttype);

        /// <summary>
        /// Get a result row as an enumerated array.
        /// </summary>
        public static PhpArray mysqli_fetch_row(mysqli_result result) => result.fetch_row();

        /// <summary>
        /// Returns the current row of a result set as an object.
        /// </summary>
        public static object mysqli_fetch_object(mysqli_result result, string class_name = null, PhpArray class_params = null) => result.fetch_object(class_name, class_params);

        /// <summary>
        /// Returns an array of objects representing the fields in a result set.
        /// </summary>
        public static PhpArray mysqli_fetch_fields(mysqli_result result) => result.fetch_fields();

        /// <summary>
        /// Returns the next field in the result set.
        /// </summary>
        [return: CastToFalse]
        public static stdClass mysqli_fetch_field(mysqli_result result) => result.fetch_field();

        /// <summary>
        /// Fetch meta-data for a single field.
        /// </summary>
        [return: CastToFalse]
        public static stdClass mysqli_fetch_field_direct(mysqli_result result, int fieldnr) => result.fetch_field_direct(fieldnr);

        /// <summary>
        /// Get the number of fields in a result.
        /// </summary>
        public static int mysqli_num_fields(mysqli_result result) => result.field_count;

        /// <summary>
        /// Get current field offset of a result pointer..
        /// </summary>
        public static int mysqli_field_tell(mysqli_result result) => result.current_field;

        /// <summary>
        /// Gets the number of rows in a result.
        /// </summary>
        public static int mysqli_num_rows(mysqli_result result) => result.num_rows;

        /// <summary>
        /// Adjusts the result pointer to an arbitrary row in the result.
        /// </summary>
        public static bool mysqli_data_seek(mysqli_result result, int offset) => result.data_seek(offset);

        /// <summary>
        /// Returns the current row of a result set as an object.
        /// </summary>
        public static bool mysqli_field_seek(mysqli_result result, int fieldnr) => result.field_seek(fieldnr);

        /// <summary>
        /// Frees the memory associated with a result.
        /// Alias to <see cref="mysqli_result.close"/>
        /// </summary>
        public static void mysqli_free_result(mysqli_result result) => result.close();
    }
}
