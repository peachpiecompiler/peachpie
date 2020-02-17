using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Library;
using System.Data.Common;
using System.Diagnostics;
using Pchp.Core.Reflection;
using Pchp.Library.Database;
using System.Data;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// Represents a connection between PHP and a database server
    /// </summary>
    /// <seealso cref="Pchp.Core.PhpResource" />
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(PDOConfiguration.PdoExtensionName)]
    public partial class PDO : IDisposable
    {
        /// <summary>Implementation of our connection resource to be used by PDO.</summary>
        internal sealed class PdoConnectionResource : ConnectionResource
        {
            public PDO PDO { get; }

            /// <summary>Current runtime context.</summary>
            public Context Context => PDO._ctx;

            public PdoConnectionResource(PDO pdo, DbConnection connection)
                : base(connection.ConnectionString, nameof(PdoConnectionResource))
            {
                this.PDO = pdo;
                this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            }

            /// <summary>Actual database connection.</summary>
            public DbConnection Connection { get; }

            protected override IDbConnection ActiveConnection => Connection;

            public DbCommand LastCommand { get; private set; }

            public DbCommand CreateCommand(string commandText)
            {
                var dbCommand = Connection.CreateCommand();
                dbCommand.CommandText = commandText;
                dbCommand.Transaction = PDO.CurrentTransaction;
                dbCommand.CommandTimeout = (PDO.TryGetAttribute(PDO_ATTR.ATTR_TIMEOUT, out var timeout) ? (int)timeout : 30) * 1000;

                LastCommand = dbCommand;

                return dbCommand;
            }

            protected override IDbCommand CreateCommand(string commandText, CommandType commandType) => CreateCommand(commandText);

            protected override ResultResource GetResult(IDataReader reader, bool convertTypes)
            {
                return new PdoResultResource(this, reader, convertTypes);
            }

            internal ResultResource ExecuteCommand(IDbCommand command, bool convertTypes, IList<IDataParameter> parameters, bool skipResults)
            {
                return ExecuteCommandProtected(command, convertTypes, parameters, skipResults);
            }
        }

        /// <summary>Result loaded with data supporting iteration.</summary>
        sealed class PdoResultResource : ResultResource
        {
            public PdoResultResource(PdoConnectionResource connection, IDataReader reader, bool convertTypes)
                : base(connection, reader, nameof(PdoResultResource), convertTypes)
            {
            }

            protected override object[] GetValues(string[] dataTypes, bool convertTypes)
            {
                var my_reader = Reader;
                var oa = new object[my_reader.FieldCount];

                if (convertTypes)
                {
                    for (int i = 0; i < oa.Length; i++)
                    {
                        oa[i] = ConvertDbValue(dataTypes[i], my_reader.GetValue(i));
                    }
                }
                else
                {
                    for (int i = 0; i < oa.Length; i++)
                    {
                        oa[i] = my_reader.GetValue(i);
                    }
                }

                return oa;
            }

            protected override string MapFieldTypeName(string typeName)
            {
                return typeName;
            }

            /// <summary>
            /// Converts a value of a specified MySQL DB type to PHP value.
            /// </summary>
            /// <param name="dataType">MySQL DB data type.</param>
            /// <param name="sqlValue">The value.</param>
            /// <returns>PHP value.</returns>
            private static object ConvertDbValue(string dataType, object sqlValue)
            {
                //if (sqlValue == null || sqlValue.GetType() == typeof(string))
                //    return sqlValue;

                //if (sqlValue.GetType() == typeof(double))
                //    return Pchp.Core.Convert.ToString((double)sqlValue);

                if (sqlValue == DBNull.Value)
                    return null;

                //if (sqlValue.GetType() == typeof(int))
                //    return ((int)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(uint))
                //    return ((uint)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(bool))
                //    return (bool)sqlValue ? "1" : "0";

                //if (sqlValue.GetType() == typeof(byte))
                //    return ((byte)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(sbyte))
                //    return ((sbyte)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(short))
                //    return ((short)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(ushort))
                //    return ((ushort)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(float))
                //    return Pchp.Core.Convert.ToString((float)sqlValue);

                if (sqlValue.GetType() == typeof(System.DateTime))
                    return ConvertDateTime(dataType, (System.DateTime)sqlValue);

                //if (sqlValue.GetType() == typeof(long))
                //    return ((long)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(ulong))
                //    return ((ulong)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(TimeSpan))
                //    return ((TimeSpan)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(decimal))
                //    return ((decimal)sqlValue).ToString();

                //if (sqlValue.GetType() == typeof(byte[]))
                //    return (byte[])sqlValue;

                ////MySqlDateTime sql_date_time = sqlValue as MySqlDateTime;
                //if (sqlValue.GetType() == typeof(MySqlDateTime))
                //{
                //    MySqlDateTime sql_date_time = (MySqlDateTime)sqlValue;
                //    if (sql_date_time.IsValidDateTime)
                //        return ConvertDateTime(dataType, sql_date_time.GetDateTime());

                //    if (dataType == "DATE" || dataType == "NEWDATE")
                //        return "0000-00-00";
                //    else
                //        return "0000-00-00 00:00:00";
                //}

                return sqlValue;
            }

            static string ConvertDateTime(string dataType, System.DateTime value)
            {
                if (dataType == "DATE" || dataType == "NEWDATE")
                    return value.ToString("yyyy-MM-dd");
                else
                    return value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        private protected PdoConnectionResource _connection;

        /// <summary>Runtime context. Cannot be <c>null</c>.</summary>
        /// <remarks>"_ctx" is a special name recognized by compiler. Will be reused by inherited classes.</remarks>
        protected readonly Context _ctx;

        internal DbTransaction CurrentTransaction { get; private set; }

        internal PDODriver Driver { get; private set; }

        internal DbCommand CurrentCommand => _connection?.LastCommand;

        /// <summary>
        /// Gets the native connection instance
        /// </summary>
        public DbConnection Connection => _connection?.Connection;

        /// <summary>
        /// Empty constructor.
        /// </summary>
        [PhpFieldsOnlyCtor]
        protected PDO(Context/*!*/ctx)
        {
            Debug.Assert(ctx != null);
            _ctx = ctx;
            _ctx.RegisterDisposable(this);
        }

        /// <summary>
        /// Creates a <see cref="PDO"/> instance to represent a connection to the requested database.
        /// </summary>
        /// <param name="ctx">The php context.</param>
        /// <param name="dsn">The Data Source Name.</param>
        /// <param name="username">The user name for the DSN string.</param>
        /// <param name="password">The password for the DSN string.</param>
        /// <param name="options">A key=&gt;value array of driver-specific connection options.</param>
        public PDO(Context ctx, string dsn, string username = null, string password = null, PhpArray options = null)
            : this(ctx)
        {
            __construct(dsn, username, password, options);
        }

        /// <summary>
        /// Creates a PDO instance representing a connection to a database
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="options">The options.</param>
        public void __construct(string dsn, string username = null, string password = null, PhpArray options = null)
        {
            int doublecolon = dsn.IndexOf(':');
            if (doublecolon < 0)
            {
                // lookup dsn alias
                var dsn2 = _ctx.Configuration.Get<PDOConfiguration>()?.Dsn[dsn];
                if (dsn2 == null)
                {
                    throw new PDOException("Invalid DSN Alias.");
                }

                dsn = dsn2;
                doublecolon = dsn.IndexOf(':');

                if (doublecolon <= 0)
                {
                    throw new PDOException("Invalid DSN.");
                }
            }

            //
            var driver = dsn.Remove(doublecolon);
            var connstring = dsn.AsSpan(doublecolon + 1);

            // resolve the driver:
            Driver = PDOEngine.TryGetDriver(driver) ?? throw new PDOException($"could not find driver: '{driver}'"); // TODO: resources

            if (options != null && options.TryGetValue((int)PDO_ATTR.ATTR_PERSISTENT, out var persistent))
            {
                // TODO: lookup for persistent connection in `ConnectionManager`, mark as persistent
            }

            try
            {
                // create connection object:
                _connection = new PdoConnectionResource(this, Driver.OpenConnection(connstring, username, password, options));
            }
            catch (Exception e)
            {
                // PDO construct always throws PDOException on error:
                throw new PDOException(e.Message);
            }

            // set attributes
            if (options != null)
            {
                var enumerator = options.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    var key = enumerator.CurrentKey;
                    if (key.IsInteger &&
                        key.Integer != ATTR_PERSISTENT)
                    {
                        setAttribute((PDO_ATTR)key.Integer, enumerator.CurrentValue);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if inside a transaction
        /// </summary>
        /// <returns></returns>
        public bool inTransaction() => CurrentTransaction != null;

        /// <inheritDoc />
        public PhpValue __call(string name, PhpArray arguments)
        {
            var method = Driver.TryGetExtensionMethod(name)
                ?? throw new PDOException($"Method '{name}' not found"); // TODO: resources

            return method.Invoke(this, arguments);
        }

        /// <inheritDoc />
        void IDisposable.Dispose()
        {
            Connection?.Dispose();
        }

        /// <summary>
        /// This function returns all currently available PDO drivers which can be used in DSN parameter of <see cref="PDO" /> constructor.
        /// </summary>
        [return: NotNull]
        public static PhpArray getAvailableDrivers()
        {
            return PDOStatic.pdo_drivers();
        }

        /// <summary>
        /// Creates a DbCommand object.
        /// </summary>
        /// <param name="statement">The command text.</param>
        /// <returns></returns>
        internal DbCommand CreateCommand(string statement) => _connection.CreateCommand(statement);

        /// <summary>
        /// Closes pending data reader if any.
        /// </summary>
        internal void ClosePendingReader() => _connection.ClosePendingReader();

        /// <summary>
        /// Execute an SQL statement and return the number of affected rows.
        /// </summary>
        /// <param name="statement">The statement.</param>
        [return: CastToFalse]
        public virtual int exec(string statement)
        {
            this.ClearError();

            this.ClosePendingReader();

            using (var dbCommand = this.CreateCommand(statement))
            {
                try
                {
                    return dbCommand.ExecuteNonQuery();
                }
                catch (System.Exception ex)
                {
                    this.HandleError(ex);
                    return -1; // FALSE
                }
            }
        }

        /// <summary>
        /// Initiates a transaction
        /// </summary>
        /// <exception cref="PDOException">When a transaction has already been started</exception>
        /// <returns>True if transaction started successfully, or false</returns>
        public virtual bool beginTransaction()
        {
            if (CurrentTransaction != null)
                throw new PDOException("Transaction already active");
            //TODO DbTransaction isolation level
            CurrentTransaction = Connection.BeginTransaction();
            return true;
        }

        /// <summary>
        /// Commits a transaction
        /// </summary>
        /// <returns></returns>
        public virtual bool commit()
        {
            if (CurrentTransaction == null)
                throw new PDOException("No active transaction");
            CurrentTransaction.Commit();
            CurrentTransaction = null;
            return true;
        }

        /// <summary>
        /// Rolls back a transaction.
        /// </summary>
        /// <returns></returns>
        public virtual bool rollBack()
        {
            if (CurrentTransaction == null)
                throw new PDOException("No active transaction");
            CurrentTransaction.Rollback();
            CurrentTransaction = null;
            return true;
        }

        /// <summary>
        /// Returns the ID of the last inserted row or sequence value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public virtual string lastInsertId(string name = null)
        {
            return Driver.GetLastInsertId(this, name);
        }

        /// <summary>
        /// Prepares a statement for execution and returns a statement object.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="driver_options">The driver options.</param>
        /// <returns></returns>
        [return: CastToFalse]
        public virtual PDOStatement prepare(string statement, PhpArray driver_options = null)
        {
            try
            {
                return CreateStatement(statement, driver_options);
            }
            catch (System.Exception ex)
            {
                this.HandleError(ex);
                return null;
            }
        }

        /// <summary>
        /// Executes an SQL statement, returning a result set as a PDOStatement object.
        /// </summary>
        /// <param name="statement">The statement.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        [return: CastToFalse]
        public virtual PDOStatement query(string statement, params PhpValue[] args)
        {
            var stmt = CreateStatement(statement, null);

            if (args.Length > 0)
            {
                // Set the fetch mode, logic inside PDOStatement
                if (args[0].IsLong(out var mode) && stmt.setFetchMode((PDO_FETCH)mode, args.AsSpan(1)))
                {
                    // ok
                }
                else
                {
                    return null;
                }
            }

            if (stmt.execute())
            {
                return stmt;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Quotes a string for use in a query.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="parameter_type">Type of the parameter.</param>
        /// <returns></returns>
        [return: CastToFalse]
        public virtual string quote(string str, PARAM parameter_type = PARAM.PARAM_STR)
        {
            return Driver?.Quote(str, parameter_type);
        }

        [PhpHidden]
        PDOStatement CreateStatement(string statement, PhpArray options)
        {
            // TODO: lookup driver_options for `ATTR_STATEMENT_CLASS` instead ?

            if (TryGetAttribute(PDO_ATTR.ATTR_STATEMENT_CLASS, out var classattr) && classattr.IsSet && classattr.IsPhpArray(out var classarr))
            {
                if (classarr[0].IsString(out var classname))
                {
                    var tinfo = _ctx.GetDeclaredTypeOrThrow(classname, autoload: true);
                    var args = classarr[1].IsPhpArray(out var argsarr) ? argsarr : PhpArray.Empty;

                    var instance = (PDOStatement)tinfo.CreateUninitializedInstance(_ctx);

                    instance.Prepare(_connection, statement, options);

                    // __construct
                    var construct = tinfo.RuntimeMethods[ReflectionUtils.PhpConstructorName];
                    if (construct != null)
                    {
                        construct.Invoke(_ctx, instance, args.GetValues());
                    }
                    else if (args.Count != 0)
                    {
                        // arguments provided but __construct() was not found
                        throw new InvalidOperationException();
                    }

                    //
                    return instance;
                }

                throw new PDOException();
            }
            else
            {
                // shortcut
                return new PDOStatement(_connection, statement, options);
            }
        }
    }
}
