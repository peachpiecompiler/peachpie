using System;
using System.Collections.Generic;
using System.Diagnostics;
using Pchp.Core;
using Peachpie.Library.PDO.Utilities;
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
        protected override string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            // TODO: Sqlite connection string

            var csb = new ConnectionStringBuilder();

            csb.DataSource = dsn.ToString();
            csb.Add("Password", password);
            csb.Add("UserId", password);

            return csb.ConnectionString;
        }

        /// <inheritDoc />
        public override ExtensionMethodDelegate TryGetExtensionMethod(string name)
        {
            if (name.Equals("sqliteCreateAggregate", StringComparison.OrdinalIgnoreCase)) return sqliteCreateAggregate;
            if (name.Equals("sqliteCreateCollation", StringComparison.OrdinalIgnoreCase)) return sqliteCreateCollation;
            if (name.Equals("sqliteCreateFunction", StringComparison.OrdinalIgnoreCase)) return sqliteCreateFunction;

            return null;
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
            Debug.Fail("last_insert_id not implemented");

            // this is probably not correct:
            using (var cmd = pdo.CreateCommand("SELECT LAST_INSERT_ROWID()"))
            {
                object value = cmd.ExecuteScalar();
                return value?.ToString();
            }
        }
    }
}