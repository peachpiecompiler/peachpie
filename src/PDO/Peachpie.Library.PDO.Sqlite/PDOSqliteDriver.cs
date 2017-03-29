using System;
using System.Collections.Specialized;
using System.Data.Common;
using Pchp.Core;
using System.Collections.Generic;

#if NET46
using System.Data.SQLite;
using Factory = System.Data.SQLite.SQLiteFactory;
using Connection = System.Data.SQLite.SQLiteConnection;
using ConnectionStringBuilder = System.Data.SQLite.SQLiteConnectionStringBuilder;
using Command = System.Data.SQLite.SQLiteCommand;
#else
using Microsoft.Data.Sqlite;
using Factory = Microsoft.Data.Sqlite.SqliteFactory;
using Connection = Microsoft.Data.Sqlite.SqliteConnection;
using ConnectionStringBuilder = Microsoft.Data.Sqlite.SqliteConnectionStringBuilder;
using Command = Microsoft.Data.Sqlite.SqliteCommand;
#endif

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
#if NET46
            throw new NotImplementedException();
#else
            return PhpValue.False;
#endif
        }
        private static PhpValue sqliteCreateCollation(PDO pdo, PhpArray arguments)
        {
#if NET46
            throw new NotImplementedException();
#else
            return PhpValue.False;
#endif
        }

        private static PhpValue sqliteCreateFunction(PDO pdo, PhpArray arguments)
        {
#if NET46
            throw new NotImplementedException();
            // From https://github.com/DEVSENSE/Phalanger/blob/master/Source/Extensions/PDOSQLite/SQLitePDODriver.cs
            // SQLiteFunction.RegisterFunction(func_name, nbr_arg, FunctionType.Scalar, null, d, null);
#else
            //Microsoft connector does not support CreateFunction
            return PhpValue.False;
#endif
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