using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

using Pchp.Core;

namespace Peachpie.Library.PDO.MySQL
{
    /// <summary>
    /// PDO driver class for MySQL
    /// </summary>
    /// <seealso cref="Peachpie.Library.PDO.PDODriver" />
    [System.Composition.Export(typeof(IPDODriver))]
    public sealed class PDOMySQLDriver : PDODriver
    {
        static readonly Dictionary<string, string> s_optionasliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"dbname", "Database"},
        };

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
            Utilities.DataSourceString.ParseNameValue(dsn, csb, (_csb, name, value) =>
            {
                if (s_optionasliases.TryGetValue(name, out var realname))
                {
                    name = realname;
                }

                _csb[name] = value;
            });

            //
            if (!string.IsNullOrEmpty(user)) csb.UserID = user;
            if (!string.IsNullOrEmpty(password)) csb.Password = password;
            if (options != null && options[PDO.ATTR_PERSISTENT].ToBoolean()) csb.Pooling = true;

            //
            return csb.ConnectionString;
        }

        /// <inheritDoc />
        public override string GetLastInsertId(PDO pdo, string name)
        {
            //MySqlCommand command = get it somewhere;
            //command.LastInsertedId

            throw new NotImplementedException();

            // NOTE: this is not correct:
            //using (var cmd = pdo.CreateCommand("SELECT LAST_INSERT_ID()"))
            //{
            //    object value = cmd.ExecuteScalar();
            //    return value?.ToString();
            //}
        }

        /// <inheritDoc />
        public override string Quote(string str, PDO.PARAM param)
        {
            return MySqlHelper.EscapeString(str);
        }
    }
}
