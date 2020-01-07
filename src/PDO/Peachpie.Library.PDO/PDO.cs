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
                dbCommand.CommandTimeout = (int)(PDO.m_attributes[PDO_ATTR.ATTR_TIMEOUT]) * 1000;

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

                //if (convertTypes)
                //{
                //    for (int i = 0; i < oa.Length; i++)
                //    {
                //        oa[i] = ConvertDbValue(dataTypes[i], my_reader.GetValue(i));
                //    }
                //}
                //else
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
        }

        private protected PdoConnectionResource _connection;

        /// <summary>Runtime context. Cannot be <c>null</c>.</summary>
        protected readonly Context _ctx; // "_ctx" is a special name recognized by compiler. Will be reused by inherited classes.

        readonly Dictionary<PDO_ATTR, PhpValue> m_attributes = new Dictionary<PDO_ATTR, PhpValue>();

        internal DbTransaction CurrentTransaction { get; private set; }
        internal PDODriver Driver { get; private set; }
        internal DbCommand CurrentCommand => _connection.LastCommand;

        /// <summary>
        /// Gets the native connection instance
        /// </summary>
        public DbConnection Connection => _connection.Connection;

        /// <summary>
        /// true is there has been already a PDOStatement executed for this PDO, false otherwise
        /// </summary>
        public bool HasExecutedQuery { get; set; } = false;

        private PDOStatement lastExecutedStatement = null;  // move to _connection

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
            this.SetDefaultAttributes();    // TODO: !!! do not

            string driver;
            ReadOnlySpan<char> connstring;

            int doublecolon = dsn.IndexOf(':');
            if (doublecolon >= 0)
            {
                driver = dsn.Remove(doublecolon);
                connstring = dsn.AsSpan(doublecolon + 1);
            }
            else
            {
                // Alias mode
                throw new NotImplementedException("PDO DSN alias not implemented");
                // replace DSN alias with value
            }

            if (driver == "uri") // TODO: move to a driver "UriDriver"
            {
                // Uri mode
                if (Uri.TryCreate(connstring.ToString(), UriKind.Absolute, out var uri))
                {
                    if (uri.Scheme.Equals("file", StringComparison.Ordinal))
                    {
                        throw new NotImplementedException("PDO uri DSN not implemented");
                        //return
                    }
                    else
                    {
                        throw new PDOException("PDO DSN as URI does not support other schemes than 'file'");
                    }
                }
                else
                {
                    throw new PDOException("Invalid uri in DSN");
                }
            }

            // DSN mode
            Driver = PDOEngine.TryGetDriver(driver)
                ?? throw new PDOException($"Driver '{driver}' not found"); // TODO: resources

            try
            {
                _connection = new PdoConnectionResource(this, Driver.OpenConnection(connstring, username, password, options));
            }
            catch (Exception e)
            {
                throw new PDOException(e.Message);
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
        /// <returns></returns>
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
        /// Execute an SQL statement and return the number of affected rows.
        /// </summary>
        /// <param name="statement">The statement.</param>
        [return: CastToFalse]
        public virtual int exec(string statement)
        {
            this.ClearError();

            _connection.ClosePendingReader();
            lastExecutedStatement = null;

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
                return lastExecutedStatement = CreateStatement(statement, driver_options);
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
            var stmt = lastExecutedStatement = CreateStatement(statement, null);

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
        void SetDefaultAttributes()
        {
            this.m_attributes[PDO_ATTR.ATTR_AUTOCOMMIT] = (PhpValue)true;
            this.m_attributes[PDO_ATTR.ATTR_PREFETCH] = (PhpValue)0;
            this.m_attributes[PDO_ATTR.ATTR_TIMEOUT] = (PhpValue)30;
            this.m_attributes[PDO_ATTR.ATTR_ERRMODE] = (PhpValue)ERRMODE_SILENT;
            this.m_attributes[PDO_ATTR.ATTR_SERVER_VERSION] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_CLIENT_VERSION] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_SERVER_INFO] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_CONNECTION_STATUS] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_CASE] = (PhpValue)(int)PDO_CASE.CASE_LOWER;
            this.m_attributes[PDO_ATTR.ATTR_CURSOR_NAME] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_CURSOR] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_ORACLE_NULLS] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_PERSISTENT] = PhpValue.False;
            this.m_attributes[PDO_ATTR.ATTR_STATEMENT_CLASS] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_FETCH_CATALOG_NAMES] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_FETCH_TABLE_NAMES] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_STRINGIFY_FETCHES] = PhpValue.Null;
            this.m_attributes[PDO_ATTR.ATTR_MAX_COLUMN_LEN] = PhpValue.Null;
            //this.m_attributes[PDO_ATTR.ATTR_DEFAULT_FETCH_MODE] = 0;
            this.m_attributes[PDO_ATTR.ATTR_EMULATE_PREPARES] = PhpValue.False;
        }

        [PhpHidden]
        PDOStatement CreateStatement(string statement, PhpArray options)
        {
            // TODO: lookup driver_options for `ATTR_STATEMENT_CLASS` instead ?
            if (m_attributes.TryGetValue(PDO_ATTR.ATTR_STATEMENT_CLASS, out var classattr) && classattr.IsSet && classattr.IsPhpArray(out var classarr))
            {
                if (classarr[0].IsString(out var classname))
                {
                    var tinfo = _ctx.GetDeclaredTypeOrThrow(classname, autoload: true);
                    var args = classarr[1].IsPhpArray(out var argsarr) ? argsarr : PhpArray.Empty;

                    var instance = (PDOStatement)tinfo.GetUninitializedInstance(_ctx);

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
