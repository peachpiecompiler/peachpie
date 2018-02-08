using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.MySql.MySqli
{
    /// <summary>
    /// Represents a connection between PHP and a MySQL database.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(Constants.ExtensionName)]
    public class mysqli
    {
        [PhpHidden]
        MySqlConnectionResource/*!*/_connection;

        [PhpHidden]
        Dictionary<int, PhpValue> _lazyoptions;

        /// <summary>
        /// A number that represents given version in format: main_version*10000 + minor_version *100 + sub_version.
        /// </summary>
        /// 
        static int VersionAsInteger(Version v) => v.Major * 10000 + v.Minor * 100 + v.Build;

        internal static string ClientInfo => MySql.EquivalentNativeLibraryVersion.ToString();
        internal static int ClientVersion => VersionAsInteger(MySql.EquivalentNativeLibraryVersion);

        /// <summary>
        /// Empty ctor.
        /// </summary>
        public mysqli() { }

        /// <summary>
        /// Open a new connection to the MySQL server.
        /// </summary>
        public mysqli(Context ctx, string host = null, string username = null, string passwd = null, string dbname = null, int port = -1, string socket = null, int flags = 0)
        {
            __construct(ctx, host, username, passwd, dbname, port, socket);
        }

        /* Properties */

        /// <summary>
        /// Gets the number of affected rows in a previous MySQL operation.
        /// </summary>
        public int affected_rows => _connection.LastAffectedRows;

        /// <summary>
        /// The connection error number or <c>0</c>.
        /// </summary>
        public int connect_errno => string.IsNullOrEmpty(connect_error) ? 0 : -1;

        /// <summary>
        /// The connection error message. Otherwise <c>null</c>.
        /// </summary>
        public string connect_error { get; private set; }

        /// <summary>
        /// Returns the error code for the most recent function call.
        /// </summary>
        public int errno => _connection.GetLastErrorNumber();
        //array $error_list;

        /// <summary>
        /// Returns a string description of the last error.
        /// </summary>
        public string error => _connection.GetLastErrorMessage();

        //int $field_count;

        /// <summary>
        /// Get MySQL client info.
        /// </summary>
        public string client_info => ClientInfo;

        /// <summary>
        /// Returns the MySQL client version as an integer.
        /// </summary>
        public int client_version => ClientVersion;

        /// <summary>
        /// Returns a string representing the type of connection used.
        /// </summary>
        public string host_info => string.Concat(_connection.Server, " via TCP/IP"); // TODO: how to get the protocol?

        //string $protocol_version;

        /// <summary>
        /// Returns the version of the MySQL server.
        /// </summary>
        public string server_info => _connection.ServerVersion;

        /// <summary>
        /// Returns the version of the MySQL server as an integer.
        /// </summary>
        /// <remarks>
        /// The form of this version number is main_version * 10000 + minor_version * 100 + sub_version (i.e. version 4.1.0 is 40100).
        /// </remarks>
        public int server_version => Version.TryParse(_connection.ServerVersion, out Version v) ? VersionAsInteger(v) : 0;

        //string $info;

        /// <summary>
        /// Returns the auto generated id used in the latest query.
        /// </summary>
        public long insert_id { get; private set; }

        //string $sqlstate;

        /// <summary>
        /// Returns the thread ID for the current connection.
        /// </summary>
        public int thread_id => _connection.ServerThread;
        //int $warning_count;

        /* Methods */

        /// <summary>
        /// Open a new connection to the MySQL server.
        /// </summary>
        public virtual void __construct(Context ctx, string host = null, string username = null, string passwd = null, string dbname = "", int port = -1, string socket = null, int flags = 0)
        {
            real_connect(ctx, host, username, passwd, dbname, port, socket);
        }

        //bool autocommit(bool $mode )
        //bool change_user(string $user , string $password , string $database )

        /// <summary>
        /// Returns the default character set for the database connection.
        /// </summary>
        public string character_set_name()
        {
            object value = _connection.QueryGlobalVariable("character_set_client");
            return (value != null) ? value.ToString() : MySql.DefaultClientCharset;
        }

        /// <summary>
        /// Closes a previously opened database connection.
        /// </summary>
        public bool close() { _connection.Dispose(); return true; }

        //bool commit([ int $flags[, string $name]] )

        ///// <summary>
        ///// Alias of mysqli::__construct()
        ///// </summary>
        //public void connect(Context ctx, string host = null, string username = null, string passwd = null, string dbname = "", int port = -1, string socket = null, int flags = 0)
        //{
        //    __construct(ctx, host, username, passwd, dbname, port, socket);
        //}

        //bool debug(string $message )
        //bool dump_debug_info(void )
        //object get_charset(void )
        //string get_client_info(void )
        //bool get_connection_stats(void )
        //string mysqli_stmt::get_server_info(void )
        //mysqli_warning get_warnings(void )

        /// <summary>Initializes MySQLi and returns a resource for use with mysqli_real_connect()</summary>
        public static mysqli init() => new mysqli();

        //bool kill(int $processid )
        //bool more_results(void )
        //bool multi_query(string $query )
        //bool next_result(void )

        /// <summary>
        /// Set options.
        /// </summary>
        public bool options(int option, PhpValue value)
        {
            if (_lazyoptions == null)
            {
                _lazyoptions = new Dictionary<int, PhpValue>();
            }

            switch (option)
            {
                case Constants.MYSQLI_OPT_CONNECT_TIMEOUT: // connection timeout in seconds(supported on Windows with TCP / IP since PHP 5.3.1)
                case Constants.MYSQLI_SET_CHARSET_NAME:
                    //case Constants.MYSQLI_OPT_LOCAL_INFILE enable/ disable use of LOAD LOCAL INFILE
                    //case Constants.MYSQLI_INIT_COMMAND command to execute after when connecting to MySQL server
                    //case Constants.MYSQLI_READ_DEFAULT_FILE    Read options from named option file instead of my.cnf
                    //case Constants.MYSQLI_READ_DEFAULT_GROUP   Read options from the named group from my.cnf or the file specified with MYSQL_READ_DEFAULT_FILE.
                    //case Constants.MYSQLI_SERVER_PUBLIC_KEY RSA public key file used with the SHA-256 based authentication.
                    //case Constants.MYSQLI_OPT_NET_CMD_BUFFER_SIZE  The size of the internal command/network buffer.Only valid for mysqlnd.
                    //case Constants.MYSQLI_OPT_NET_READ_BUFFER_SIZE Maximum read chunk size in bytes when reading the body of a MySQL command packet. Only valid for mysqlnd.
                    //case Constants.MYSQLI_OPT_INT_AND_FLOAT_NATIVE Convert integer and float columns back to PHP numbers. Only valid for mysqlnd.
                    //case Constants.MYSQLI_OPT_SSL_VERIFY_SERVER_CERT
                    _lazyoptions[option] = value;
                    return true;

                default:
                    PhpException.InvalidArgument(nameof(option));
                    return false;
            }
        }

        /// <summary>
        /// Pings a server connection, or tries to reconnect if the connection has gone down.
        /// </summary>
        public bool ping() => _connection.Ping();

        //public static int poll(array &$read , array &$error , array &$reject , int $sec[, int $usec] )
        //mysqli_stmt prepare(string $query )

        /// <summary>
        /// Performs a query on the database.
        /// </summary>
        /// <returns>
        /// Returns FALSE on failure.
        /// For successful SELECT, SHOW, DESCRIBE or EXPLAIN queries mysqli_query() will return a mysqli_result object.
        /// For other successful queries mysqli_query() will return TRUE</returns>
        public PhpValue query(PhpString query, int resultmode = Constants.MYSQLI_STORE_RESULT)
        {
            MySqlResultResource result;

            if (query.ContainsBinaryData)
            {
                var encoding = _connection.Context.StringEncoding;

                // be aware of binary data
                result = (MySqlResultResource)MySql.QueryBinary(encoding, query.ToBytes(encoding), _connection);
            }
            else
            {
                // standard unicode behaviour
                result = (MySqlResultResource)_connection.ExecuteQuery(query.ToString(Encoding.UTF8/*not used*/), true);
            }


            if (result != null)
            {
                insert_id = result.Command.LastInsertedId;

                if (result.FieldCount == 0)
                {
                    // no result set => not a SELECT
                    result.Dispose();
                    return PhpValue.True;
                }

                // TODO: resultmode

                return PhpValue.FromClass(new mysqli_result(result));
            }
            else
            {
                return PhpValue.False;
            }
        }

        /// <summary>
        /// Opens a connection to a mysql server.
        /// </summary>
        public bool real_connect(Context ctx, string host = null, string username = null, string passwd = null, string dbname = "", int port = -1, string socket = null, int flags = 0)
        {
            var config = ctx.Configuration.Get<MySqlConfiguration>();
            int connectiontimeout = 0;
            string characterset = null;

            if (_lazyoptions != null)
            {
                PhpValue value;
                if (_lazyoptions.TryGetValue(Constants.MYSQLI_OPT_CONNECT_TIMEOUT, out value)) connectiontimeout = (int)value.ToLong();
                if (_lazyoptions.TryGetValue(Constants.MYSQLI_SET_CHARSET_NAME, out value)) characterset = value.ToStringOrThrow(ctx);
            }

            // string $host = ini_get("mysqli.default_host")
            // string $username = ini_get("mysqli.default_user")
            // string $passwd = ini_get("mysqli.default_pw")
            // string $dbname = ""
            // int $port = ini_get("mysqli.default_port")
            // string $socket = ini_get("mysqli.default_socket")

            var connection_string = MySql.BuildConnectionString(config, ref host, username, passwd,
                flags: (MySql.ConnectFlags)flags,
                connectiontimeout: connectiontimeout,
                characterset: characterset);

            _connection = MySqlConnectionManager.GetInstance(ctx)
                .CreateConnection(connection_string, false, -1, out bool success);

            if (success)
            {
                _connection.Server = host;

                if (!string.IsNullOrEmpty(dbname))
                {
                    _connection.SelectDb(dbname);
                }
            }
            else
            {
                connect_error = _connection.GetLastErrorMessage();
            }

            //
            return success;
        }

        /// <summary>
        /// Escapes special characters in a string for use in an SQL statement, taking into account the current charset of the connection.
        /// </summary>
        public PhpString escape_string(PhpString escapestr) => real_escape_string(escapestr);

        /// <summary>
        /// Escapes special characters in a string for use in an SQL statement, taking into account the current charset of the connection.
        /// </summary>
        public PhpString real_escape_string(PhpString escapestr) => MySql.mysql_escape_string(_connection.Context, escapestr);

        //bool real_query(string $query )
        //public mysqli_result reap_async_query(void )
        //public bool refresh(int $options )
        //bool rollback([ int $flags[, string $name]] )
        //int rpl_query_type(string $query )

        /// <summary>
        /// Selects the default database for database queries.
        /// </summary>
        public bool select_db(string dbname) => _connection.SelectDb(dbname);

        //bool send_query(string $query )

        /// <summary>
        /// Sets the default client character set,
        /// </summary>
        public bool set_charset(string charset)
        {
            // validate the charset (only a-z, 0-9, _ allowed, see mysqlnd_find_charset_name):
            if (!MySql.MysqlValidateCharset(charset))
            {
                PhpException.InvalidArgument(nameof(charset));
                return false;
            }

            // set the charset:
            var result = _connection.ExecuteCommand("SET NAMES " + charset, CommandType.Text, false, null, true);
            if (result != null)
            {
                result.Dispose();
            }

            // success if there were no errors:
            return _connection.LastException == null;
        }

        //bool set_local_infile_handler(mysqli $link , callable $read_func )

        /// <summary>
        /// Used for establishing secure connections using SSL
        /// </summary>
        /// <returns>Always true.</returns>
        public bool ssl_set(string key, string cert, string ca, string capath, string cipher)
        {
            throw new NotImplementedException();
        }

        //string stat(void )
        //mysqli_stmt stmt_init(void )
        //mysqli_result store_result([ int $option ] )
        //mysqli_result use_result(void )
    }
}
