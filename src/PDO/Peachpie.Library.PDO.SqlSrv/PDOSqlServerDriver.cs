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
            pdo.ClosePendingReader();

            string last_insert_id_query;

            if (string.IsNullOrEmpty(name))
            {
                last_insert_id_query = "SELECT @@IDENTITY";
            }
            else
            {
                // TODO: "SELECT CURRENT_VALUE FROM SYS.SEQUENCES WHERE NAME=%s"
                throw new NotImplementedException(nameof(name));
            }

            using (var com = pdo.CreateCommand(last_insert_id_query))
            {
                var value = com.ExecuteScalar();
                if (value == null)
                {
                    // TODO: error
                    return null;
                }

                return value.ToString();
            }
        }

        /// <inheritDoc />
        protected override string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            //TODO sqlserver pdo dsn to dotnet connectionstring
            var csb = new SqlConnectionStringBuilder(dsn.ToString());

            if (user != null) csb.UserID = user;
            if (password != null) csb.Password = password;

            return csb.ConnectionString;
        }
    }
}
