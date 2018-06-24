using System;
using System.Data.SqlClient;
using Pchp.Core;
using Peachpie.Library.PDO.Utilities;

namespace Peachpie.Library.PDO.SqlSrv
{
    /// <summary>
    /// PDO driver for Microsoft SqlServer
    /// </summary>
    /// <seealso cref="PDODriver" />
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
        protected override string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            //TODO sqlserver pdo dsn to dotnet connectionstring
            var csb = new SqlConnectionStringBuilder(dsn.ToString())
            {
                UserID = user,
                Password = password
            };

            return csb.ConnectionString;
        }
    }
}
