using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

namespace Peachpie.Library.PDO.SqlSrv
{
    /// <summary>
    /// PDO driver for Microsoft SqlServer
    /// </summary>
    /// <seealso cref="Peachpie.Library.PDO.PDODriver" />
    [System.Composition.Export(typeof(IPDODriver))]
    public class PDOSqlServerDriver : PDODriver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PDOSqlServerDriver"/> class.
        /// </summary>
        public PDOSqlServerDriver() : base("sqlsrv", SqlClientFactory.Instance)
        {
        }

        /// <inheritDoc />
        public override string GetLastInsertId(PDO pdo, string name)
        {
            using (var com = pdo.CreateCommand("SELECT SCOPE_IDENTITY()"))
            {
                object value = com.ExecuteScalar();
                return value?.ToString();
            }
        }

        /// <inheritDoc />
        protected override string BuildConnectionString(string dsn, string user, string password, PhpArray options)
        {
            //TODO sqlserver pdo dsn to dotnet connectionstring
            var csb = new SqlConnectionStringBuilder(dsn);
            csb.UserID = user;
            csb.Password = password;
            return csb.ConnectionString;
        }
    }
}
