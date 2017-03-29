using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Pchp.Core;
using System.Data.Common;

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
        protected override string BuildConnectionString(string dsn, string user, string password, PhpArray options)
        {
            //TODO mysql pdo parameters to dotnet connectionstring
            var csb = new MySqlConnectionStringBuilder(dsn);
            csb.UserID = user;
            csb.Password = password;
            return csb.ConnectionString;
        }

        /// <inheritDoc />
        public override string GetLastInsertId(PDO pdo, string name)
        {
            using (var cmd = pdo.CreateCommand("SELECT LAST_INSERT_ID()"))
            {
                object value = cmd.ExecuteScalar();
                return value?.ToString();
            }
        }

        /// <inheritDoc />
        public override string Quote(string str, PDO.PARAM param)
        {
            return MySqlHelper.EscapeString(str);
        }
    }
}
