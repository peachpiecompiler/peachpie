using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peachpie.Library.MySql
{
    /// <summary>
    /// MySql functions container.
    /// </summary>
    [PhpExtension("mysql")]
    public static partial class MySql
    {
        #region Enums

        /// <summary>
        /// Connection flags.
        /// </summary>
        [Flags]
        enum ConnectFlags
        {
            /// <summary>
            /// No flags.
            /// </summary>
            None = 0,

            /// <summary>
            ///  Use compression protocol.
            /// </summary>
            Compress = MYSQL_CLIENT_COMPRESS,

            /// <summary>
            /// Allow space after function names.
            /// Not supported (ignored).
            /// </summary>
            IgnoreSpace = MYSQL_CLIENT_IGNORE_SPACE,

            /// <summary>
            /// Allow interactive_timeout seconds (instead of wait_timeout) of inactivity before closing the connection.
            /// Not supported (ignored).
            /// </summary>
            Interactive = MYSQL_CLIENT_INTERACTIVE,

            /// <summary>
            /// Use SSL encryption.
            /// </summary>
            SSL = MYSQL_CLIENT_SSL
        }

        /// <summary>
        /// Query result array format.
        /// </summary>
        [Flags]
        enum QueryResultKeys
        {
            /// <summary>
            /// Add items keyed by column names.
            /// </summary>
            ColumnNames = MYSQL_ASSOC,

            /// <summary>
            /// Add items keyed by column indices.
            /// </summary>
            Numbers = MYSQL_NUM,

            /// <summary>
            /// Add both items keyed by column names and items keyd by column indices.
            /// </summary>
            Both = MYSQL_BOTH
        }

        #endregion

        /// <summary>
        /// Closes the non-persistent connection to the MySQL server that's associated with the specified link identifier.
        /// If link_identifier isn't specified, the last opened link is used.
        /// </summary>
        public static bool mysql_close(PhpResource link_identifier = null)
        {
            return false;
        }

        #region mysql_connect

        // MySqlResource mysql_connect(string $server = ini_get("mysql.default_host")[, string $username = ini_get("mysql.default_user")[, string $password = ini_get("mysql.default_password")[, bool $new_link = false[, int $client_flags = 0]]]]] )

        /// <summary>
        /// Open a connection to a MySQL Server.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource mysql_connect(string server = null, string username = null, string password = null, bool new_link = false, int client_flags = 0)
        {
            throw new NotImplementedException();
        }

        static string BuildConnectionString(MySqlConfiguration config, string server, string user, string password, ConnectFlags flags)
        {
            //// connection strings:
            //if (server == null && user == null && password == null && flags == ConnectFlags.None && !string.IsNullOrEmpty(local.ConnectionString))
            //    return local.ConnectionString;

            // TODO: local.ConnectionStringName

            // build connection string:
            string pipe_name = null;
            int port = -1;

            if (server != null)
                ParseServerName(ref server, out port, out pipe_name);
            else
                server = config.Server;

            if (port == -1) port = config.Port;
            if (user == null) user = config.User;
            if (password == null) password = config.Password;

            // build the connection string to be used with MySQL Connector/.NET
            // see http://dev.mysql.com/doc/refman/5.5/en/connector-net-connection-options.html
            return BuildConnectionString(
              server, user, password,
              string.Format("allowzerodatetime=true;allow user variables=true;connect timeout={0};Port={1};SSL Mode={2};Use Compression={3}{4}{5};Max Pool Size={6}{7}",
                (config.ConnectTimeout > 0) ? config.ConnectTimeout : Int32.MaxValue,
                port,
                (flags & ConnectFlags.SSL) != 0 ? "Preferred" : "None",     // (since Connector 6.2.1.) ssl mode={None|Preferred|Required|VerifyCA|VerifyFull}   // (Jakub) use ssl={true|false} has been deprecated
                (flags & ConnectFlags.Compress) != 0 ? "true" : "false",    // Use Compression={true|false}
                (pipe_name != null) ? ";Pipe=" + pipe_name : null,  // Pipe={...}
                (flags & ConnectFlags.Interactive) != 0 ? ";Interactive=true" : null,    // Interactive={true|false}
                config.MaxPoolSize,                                          // Max Pool Size=100
                (config.DefaultCommandTimeout >= 0) ? ";DefaultCommandTimeout=" + config.DefaultCommandTimeout : null
                )
            );
        }

        /// <summary>
		/// Builds a connection string.
		/// </summary>
		private static string/*!*/ BuildConnectionString(string server, string user, string password, string additionalSettings)
        {
            var result = new StringBuilder(8);
            result.Append("server=");
            result.Append(server);
            //			result.Append(";database=");
            //			result.Append(database);
            result.Append(";user id=");
            result.Append(user);
            result.Append(";password=");
            result.Append(password);

            if (!string.IsNullOrEmpty(additionalSettings))
            {
                result.Append(';');
                result.AppendFormat(additionalSettings);
            }

            return result.ToString();
        }

        private static void ParseServerName(ref string/*!*/ server, out int port, out string socketPath)
        {
            port = -1;
            socketPath = null;

            int i = server.IndexOf(':');
            if (i == -1) return;

            string port_or_socket = server.Substring(i + 1);
            server = server.Substring(0, i);

            // socket path:
            if (port_or_socket.Length > 0 && port_or_socket[0] == '/')
            {
                socketPath = port_or_socket;
            }
            else
            {
                try
                {
                    port = UInt16.Parse(port_or_socket);
                }
                catch
                {
                   // PhpException.Throw(PhpError.Notice, LibResources.GetString("invalid_port", port_or_socket));
                }
            }
        }

        #endregion
    }
}
