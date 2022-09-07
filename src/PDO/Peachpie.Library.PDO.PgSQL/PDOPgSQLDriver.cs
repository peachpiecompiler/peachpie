using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Pchp.Core;
using Peachpie.Library.PDO.Utilities;

namespace Peachpie.Library.PDO.PgSQL
{
    /// <summary>
    /// PDO driver class for postgresql
    /// </summary>
    /// <seealso cref="Peachpie.Library.PDO.PDODriver" />
    public class PDONpgsqlDriver : PDODriver
    {
        /// <inheritDoc />
        public override string Name => "pgsql";

        /// <inheritDoc />
        public override DbProviderFactory DbFactory => NpgsqlFactory.Instance;

        /// <inheritDoc />
        protected override string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            var csb = new NpgsqlConnectionStringBuilder
            {

            };

            // parse and validate the datasource string:
            DataSourceString.ParseNameValue(dsn, csb, (_csb, name, value) =>
            {
                // unknown option aliases:
                if (name.Equals("dbname", StringComparison.OrdinalIgnoreCase)) name = "Database";

                //
                _csb[name] = value;
            });

            //
            if (!string.IsNullOrEmpty(user)) csb.Username = user;
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
            throw new NotImplementedException();
        }
    }
}
