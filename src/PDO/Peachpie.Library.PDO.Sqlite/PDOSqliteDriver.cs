using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Pchp.Core;
using Peachpie.Library.PDO.Utilities;

namespace Peachpie.Library.PDO.Sqlite
{
    /// <summary>
    /// PDO driver class for SQLite
    /// </summary>
    /// <seealso cref="Peachpie.Library.PDO.PDODriver" />
    public class PDOSqliteDriver : PDODriver
    {
        /// <inheritDoc />
        public override string Name => "sqlite";

        /// <inheritDoc />
        public override DbProviderFactory DbFactory => SqliteFactory.Instance;

        /// <inheritDoc />
        protected override string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            var csb = new SqliteConnectionStringBuilder();

            csb.DataSource = dsn.ToString();
            csb.Add("Password", password);
            csb.Add("UserId", user);

            return csb.ConnectionString;
        }

        /// <inheritDoc />
        public override DbConnection OpenConnection(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            var connection = (SqliteConnection)base.OpenConnection(dsn, user, password, options);

            new SqliteCommand("PRAGMA foreign_keys=OFF", connection).ExecuteNonQuery();

            return connection;
        }

        /// <inheritDoc />
        public override bool TrySetStringifyFetches(PDO pdo, bool stringify)
        {
            // SQLite PDO driver can retrieve values only as strings in PHP
            pdo.Stringify = true;
            return stringify == true;
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
        public override string Quote(string str, PDO.PARAM param)
        {
            // Template: sqlite3_snprintf("'%q'", unquoted);
            // - %q doubles every '\'' character

            if (string.IsNullOrEmpty(str))
            {
                return "''";
            }
            else
            {
                return $"'{str.Replace("'", "''")}'";
            }
        }

        /// <inheritDoc />
        public override string GetLastInsertId(PDO pdo, string name)
        {
            // The last_insert_rowid() SQL function is a wrapper around the sqlite3_last_insert_rowid()
            // https://www.sqlite.org/lang_corefunc.html#last_insert_rowid

            using (var cmd = pdo.CreateCommand("SELECT LAST_INSERT_ROWID()"))
            {
                object value = cmd.ExecuteScalar(); // can't be null
                return value != null ? value.ToString() : string.Empty;
            }
        }
    }
}