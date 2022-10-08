using MySqlConnector;
using Pchp.Core;
using Pchp.Library.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using Pchp.Library.Resources;

namespace Peachpie.Library.MySql
{
    /// <summary>
    /// Resource representing MySql connection.
    /// </summary>
    sealed class MySqlConnectionResource : ConnectionResource
    {
        const string ResourceName = "mysql connection";

        readonly MySqlConnectionManager _manager;

        readonly IDbConnection _connection;

        /// <summary>
        /// <see cref="_connection"/> lazily cast to <see cref="MySqlConnector.MySqlConnection"/>.
        /// <see cref="MySqlExtensions.GetUnderlyingValue"/> for details.
        /// </summary>
        MySqlConnection _mySqlConnection;

        /// <summary>
        /// Whether to keep the underlying connection open after disposing this resource.
        /// (The owner of the connection is someone else)
        /// </summary>
        readonly bool _leaveopen = false;

        /// <summary>
        /// Lazily set server name used to initiate connection.
        /// </summary>
        internal string Server { get; set; }

        /// <summary>
        /// Gets associated runtime <see cref="Context"/>.
        /// </summary>
        internal Context Context => _manager.Context;

        public MySqlConnectionResource(MySqlConnectionManager manager, string connectionString)
            : base(connectionString, ResourceName)
        {
            _manager = manager;
            _connection = new MySqlConnection(this.ConnectionString);
        }

        public MySqlConnectionResource(MySqlConnectionManager manager, IDbConnection connection)
            : base(connection.ConnectionString, ResourceName)
        {
            _manager = manager;
            _connection = connection;
            _leaveopen = true;
        }

        protected override void FreeManaged()
        {
            if (_leaveopen)
            {
                // do not close the underlying connection,
                // just dispose the reader
                ClosePendingReader();
            }
            else
            {
                base.FreeManaged();
            }

            _manager.RemoveConnection(this);
        }

        public override void ClosePendingReader()
        {
            if (_pendingReader != null)
            {
                _pendingReader.Dispose();
                _pendingReader = null;
            }
        }

        /// <summary>
        /// Gets the underlying MySql connection from the connection. We specifically support the case where
        /// the connection is a wrapped connection such as we get from MiniProfiler, and we look for WrappedConnection to
        /// find the native MySqlConnection when we need it.
        /// </summary>
        internal MySqlConnection MySqlConnection => _mySqlConnection ?? (_mySqlConnection = _connection.AsMySqlConnection());

        protected override IDbConnection ActiveConnection => _connection;

        protected override ResultResource GetResult(IDataReader reader, bool convertTypes)
        {
            return new MySqlResultResource(this, reader, convertTypes);
        }

        protected override IDbCommand CreateCommand(string commandText, CommandType commandType)
        {
            var command = _connection.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;

            // 
            var config = Context.Configuration.Get<MySqlConfiguration>();
            if (config.DefaultCommandTimeout >= 0)
            {
                command.CommandTimeout = config.DefaultCommandTimeout;
            }

            return command;
        }

        internal IDbCommand CreateCommandInternal(string commandText, CommandType commandType = CommandType.Text) => CreateCommand(commandText, commandType);

        internal ResultResource ExecuteCommandInternal(IDbCommand command, bool convertTypes, IList<IDataParameter> parameters, bool skipResults)
        {
            return ExecuteCommandProtected(command, convertTypes, parameters, skipResults);
        }

        /// <summary>
        /// Gets the server version.
        /// </summary>
        internal string ServerVersion => MySqlConnection.ServerVersion;

        /// <summary>
        /// Returns the id of the server thread this connection is executing on.
        /// </summary>
        internal int ServerThread => MySqlConnection.ServerThread;

        /// <summary>
        /// Pings the server.
        /// </summary>
        internal bool Ping() => MySqlConnection.Ping();

        /// <summary>
		/// Queries server for a value of a global variable.
		/// </summary>
		/// <param name="name">Global variable name.</param>
		/// <returns>Global variable value (converted).</returns>
		internal object QueryGlobalVariable(string name)
        {
            // TODO: better query:

            var result = ExecuteQuery("SHOW GLOBAL VARIABLES LIKE '" + name + "'", true);

            // default value
            if (result.FieldCount != 2 || result.RowCount != 1)
            {
                return null;
            }

            return result.GetFieldValue(0, 1);
        }

        /// <summary>
        /// Gets last inserted row autogenerated ID if applicable, otherwise <c>-1</c>.
        /// </summary>
        internal long LastInsertedId
        {
            get
            {
                var command = LastResult?.Command;
                return command != null ? MySqlExtensions.LastInsertedId(command) : -1;
            }
        }

        public override int GetLastErrorNumber()
        {
            if (LastException == null)
            {
                return (int)MySqlErrorCode.None; // success
            }
            else if (LastException is MySqlException me)
            {
                return (int)me.ErrorCode;
            }
            else
            {
                return (int)MySqlErrorCode.UnknownError; // unk erro number
            }
        }

        protected override void ReportException(Exception exception, string exceptionMessage)
        {
            _manager.ReportException(exception, exceptionMessage);
        }
    }
}
