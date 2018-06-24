using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

using Pchp.Core;
using Peachpie.Library.PDO.Utilities;

namespace Peachpie.Library.PDO.MySQL
{
    /// <summary>
    /// PDO driver class for MySQL
    /// </summary>
    /// <seealso cref="Peachpie.Library.PDO.PDODriver" />
    [System.Composition.Export(typeof(IPDODriver))]
    public sealed class PDOMySQLDriver : PDODriver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PDOMySQLDriver"/> class.
        /// </summary>
        public PDOMySQLDriver() : base("mysql", MySqlClientFactory.Instance)
        {

        }

        /// <inheritDoc />
        protected override string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            var csb = new MySqlConnectionStringBuilder();

            // parse and validate the datasource string:
            DataSourceString.ParseNameValue(dsn, csb, (_csb, name, value) =>
            {
                // unknown option aliases:
                if (name.Equals("dbname", StringComparison.OrdinalIgnoreCase)) name = "Database";

                //
                _csb[name] = value;
            });

            //
            if (!string.IsNullOrEmpty(user)) csb.UserID = user;
            if (!string.IsNullOrEmpty(password)) csb.Password = password;

            if (options != null && options.Count != 0)
            {
                csb.Pooling = options[PDO.ATTR_PERSISTENT].ToBoolean();
            }

            //
            return csb.ConnectionString;
        }

        /// <inheritDoc />
        public override string GetLastInsertId(PDO pdo, string name)
        {
            var command = (MySqlCommand)pdo.GetCurrentCommand();
            var lastid = (command != null) ? command.LastInsertedId : -1;

            return lastid.ToString();
        }

        /// <inheritDoc />
        public override string Quote(string str, PDO.PARAM param)
        {
            return MySqlHelper.EscapeString(str);
        }
    }
}
