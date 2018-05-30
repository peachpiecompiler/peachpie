using System.Collections.Generic;
using Pchp.Core;
using ConnectionStringBuilder = Microsoft.Data.Sqlite.SqliteConnectionStringBuilder;
using Factory = Microsoft.Data.Sqlite.SqliteFactory;

namespace Peachpie.Library.PDO.Sqlite
{
    /// <summary>
    /// PDO driver class for SQLite
    /// </summary>
    /// <seealso cref="Peachpie.Library.PDO.PDODriver" />
    [System.Composition.Export(typeof(IPDODriver))]
    public class PDOSqliteDriver : PDODriver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PDOSqliteDriver"/> class.
        /// </summary>
        public PDOSqliteDriver() : base("sqlite", Factory.Instance)
        {

        }

        /// <inheritDoc />
        protected override string BuildConnectionString(string dsn, string user, string password, PhpArray options)
        {
            var csb = new ConnectionStringBuilder();
            csb.DataSource = dsn;
            csb.Add("Password", password);
            return csb.ConnectionString;
        }

        /// <inheritDoc />
        public override Dictionary<string, ExtensionMethodDelegate> GetPDObjectExtensionMethods()
        {
            var methods = new Dictionary<string, ExtensionMethodDelegate>();
            methods.Add("sqliteCreateAggregate", sqliteCreateAggregate);
            methods.Add("sqliteCreateCollation", sqliteCreateCollation);
            methods.Add("sqliteCreateFunction", sqliteCreateFunction);
            return methods;
        }

        private static PhpValue sqliteCreateAggregate(PDO pdo, PhpArray arguments)
        {
            return PhpValue.False;
        }
        private static PhpValue sqliteCreateCollation(PDO pdo, PhpArray arguments)
        {
            return PhpValue.False;
        }

        private static PhpValue sqliteCreateFunction(PDO pdo, PhpArray arguments)
        {
            //Microsoft connector does not support CreateFunction
            return PhpValue.False;
        }

        /// <inheritDoc />

        public override string GetLastInsertId(PDO pdo, string name)
        {
            using (var cmd = pdo.CreateCommand("SELECT LAST_INSERT_ROWID()"))
            {
                object value = cmd.ExecuteScalar();
                return value?.ToString();
            }
        }
    }
}