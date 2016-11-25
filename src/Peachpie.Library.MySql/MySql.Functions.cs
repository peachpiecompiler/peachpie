using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.MySql
{
    /// <summary>
    /// MySql functions container.
    /// </summary>
    [PhpExtension("mysql")]
    public static partial class MySql
    {
        /// <summary>
        /// Closes the non-persistent connection to the MySQL server that's associated with the specified link identifier.
        /// If link_identifier isn't specified, the last opened link is used.
        /// </summary>
        public static bool mysql_close(PhpResource link_identifier = null)
        {
            return false;
        }

        // MySqlResource mysql_connect(string $server = ini_get("mysql.default_host")[, string $username = ini_get("mysql.default_user")[, string $password = ini_get("mysql.default_password")[, bool $new_link = false[, int $client_flags = 0]]]]] )

        /// <summary>
        /// Open a connection to a MySQL Server.
        /// </summary>
        public static PhpResource mysql_connect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Open a connection to a MySQL Server.
        /// </summary>
        public static PhpResource mysql_connect(string server, string username, string password , bool new_link = false, int client_flags = 0)
        {
            throw new NotImplementedException();
        }
    }
}
