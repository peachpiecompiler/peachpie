using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Sqlite;
using Pchp.Core;
using Peachpie.Library.PDO.Utilities;
using SQLitePCL;
using Convert = Pchp.Core.Convert;

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

        private static PhpValue sqliteCreateAggregate(Context ctx, PDO pdo, PhpArray arguments)
        {
            if (pdo.GetCurrentConnection<SqliteConnection>() is not {} connection)
                return PhpValue.False;

            var name = arguments[0].String;
            var step = arguments[1].AsCallable();
            var finalize = arguments[2].AsCallable();
            var numberOfArguments = -1;
            if (arguments.TryGetValue(3, out var args) && args.IsInteger())
                numberOfArguments = args.ToInt();
            
            var handle = connection.Handle;
            
            raw.sqlite3_create_function(
                handle,
                name,
                numberOfArguments,
                null,
                CreateAggregateStep(ctx, step),
                CreateAggregateFinalize(ctx, finalize));
            return PhpValue.True;
        }
        private static PhpValue sqliteCreateCollation(Context ctx, PDO pdo, PhpArray arguments)
        {
            if (pdo.GetCurrentConnection<SqliteConnection>() is not {} connection)
                return PhpValue.False;
            
            
            var name = arguments[0].String;
            var callable = arguments[1].AsCallable();
            
            var handle = connection.Handle;
            raw.sqlite3_create_collation(
                handle,
                name,
                null,
                CreateSortFunction(ctx, callable)
            );
            return PhpValue.True;
        }

        private static PhpValue sqliteCreateFunction(Context ctx, PDO pdo, PhpArray arguments)
        {
            if (pdo.GetCurrentConnection<SqliteConnection>() is not {} connection)
                return PhpValue.False;

            var name = arguments[0].String;
            var callable = arguments[1].AsCallable();
            var numberOfArguments = -1;
            if (arguments.TryGetValue(2, out var args) && args.IsInteger())
                numberOfArguments = args.ToInt();
            var flags = 0;
            if (arguments.TryGetValue(3, out var flagsValue) && flagsValue.IsInteger())
                flags = args.ToInt();
            
            var handle = connection.Handle;
            
            raw.sqlite3_create_function(
                handle,
                name,
                numberOfArguments,
                flags,
                CreateScalarFunction(ctx, callable));
            return PhpValue.True;
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

        private static delegate_function_aggregate_step CreateAggregateStep(Context ctx, IPhpCallable callback)
        {
            return (sqliteContext, data, args) =>
            {
                if (sqliteContext.state is not StepFunctionState state)
                {
                    sqliteContext.state = state = new StepFunctionState();
                }
                var rowIndex = state.RowIndex++;
                
                var phpArgs = args
                    .Select(AsPhp)
                    .Prepend(PhpValue.FromClr(rowIndex))
                    .Prepend(state.Value)
                    .ToArray();
                
                var ret = callback.Invoke(ctx, phpArgs);
                state.Value = ret;
            };
        }
        
        private static delegate_function_aggregate_final CreateAggregateFinalize(Context ctx, IPhpCallable callback)
        {
            return (sqliteContext, data) =>
            {
                if (sqliteContext.state is not StepFunctionState state)
                {
                    sqliteContext.state = state = new StepFunctionState();
                }
                
                var ret = callback.Invoke(ctx, state.Value, state.RowIndex);
                SetSqliteReturnValue(ret, sqliteContext);
            };
        }
        
        private static strdelegate_collation CreateSortFunction(Context ctx, IPhpCallable callback)
        {
            return (data, left, right) =>
            {
                var ret = callback.Invoke(
                    ctx,
                    PhpValue.FromClr(left),
                    PhpValue.FromClr(right)
                );
                if (ret.IsInteger())
                {
                    return ret.ToInt();
                }

                return 0;
            };
        }
        
        private static delegate_function_scalar CreateScalarFunction(Context ctx, IPhpCallable callback)
        {
            return (sqliteContext, data, args) =>
            {
                var phpArgs = args
                    .Select(AsPhp)
                    .ToArray();
                var ret = callback.Invoke(ctx, phpArgs);
                SetSqliteReturnValue(ret, sqliteContext);
            };
        }

        private static void SetSqliteReturnValue(PhpValue ret, sqlite3_context sqliteContext)
        {
            if (ret.IsLong(out var longValue))
            {
                raw.sqlite3_result_int64(sqliteContext, longValue);
            }
            else if (ret.IsInteger())
            {
                raw.sqlite3_result_int(sqliteContext, ret.ToInt());
            }
            else if (ret.IsDouble(out var doubleValue))
            {
                raw.sqlite3_result_double(sqliteContext, doubleValue);
            }
            else if (ret.IsString(out var stringValue))
            {
                var foo = stringValue;
                raw.sqlite3_result_text(sqliteContext, foo);
            }
            else
            {
                raw.sqlite3_result_null(sqliteContext);
            }
        }

        private class StepFunctionState
        {
            public int RowIndex { get; set; }
            public PhpValue Value { get; set; } = PhpValue.Null;
        }
        
        private static PhpValue AsPhp(sqlite3_value value)
        {
            switch (raw.sqlite3_value_type(value))
            {
                case raw.SQLITE_INTEGER:
                    return PhpValue.FromClr(raw.sqlite3_value_int(value));
                case raw.SQLITE_TEXT:
                    return PhpValue.FromClr(raw.sqlite3_value_text(value).utf8_to_string());
                case raw.SQLITE_FLOAT:
                    return PhpValue.FromClr(raw.sqlite3_value_double(value));
                case raw.SQLITE_NULL:
                    return PhpValue.Null;
            }
            throw new NotImplementedException();
        }
    }
}