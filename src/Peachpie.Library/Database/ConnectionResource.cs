using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Library.Resources;

namespace Pchp.Library.Database
{
    /// <summary>
	/// Abstract class implementing common functionality of PHP connection resources.
	/// </summary>
    public abstract class ConnectionResource : PhpResource
    {
        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString => _connectionString;
        readonly string _connectionString;

        /// <summary>
		/// A result associated with this connection that possibly has not been closed yet.
		/// </summary>
		protected IDataReader _pendingReader;

        /// <summary>
		/// Last result resource.
		/// </summary>
		public ResultResource LastResult => _lastResult;
        private ResultResource _lastResult;

        /// <summary>
        /// Gets an exception thrown by last performed operation or a <B>null</B> reference 
        /// if that operation succeeded.
        /// </summary>
        public Exception LastException => _lastException;
        protected Exception _lastException;

        /// <summary>
        /// Gets the number of rows affected by the last query executed on this connection.
        /// </summary>
        public int LastAffectedRows
        {
            get
            {
                if (_lastResult == null) return -1;

                // SELECT gives -1, UPDATE/INSERT gives the number:
                return (_lastResult.RecordsAffected >= 0) ? _lastResult.RecordsAffected : _lastResult.RowCount;
            }
        }

        /// <summary>
        /// Gets underlaying DB connection synchnously.
        /// </summary>
        protected abstract IDbConnection ActiveConnection { get; }

        /// <summary>
        /// Constructs the connection resource.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="resourceTypeName"></param>
        protected ConnectionResource(string connectionString, string resourceTypeName)
            : base(resourceTypeName)
        {
            Debug.Assert(connectionString != null);

            _connectionString = connectionString;
        }

        /// <summary>
        /// Establishes the connection to the database.
        /// Gets <c>false</c> if connection can't be opened.
        /// </summary>
        public virtual bool Connect()
        {
            if (ActiveConnection.State == ConnectionState.Open)
            {
                return true;
            }

            try
            {
                ActiveConnection.Open();  // TODO: Async
                _lastException = null;
            }
            catch (Exception e)
            {
                _lastException = e;
                PhpException.Throw(PhpError.Warning, LibResources.cannot_open_connection, GetExceptionMessage(e));
                return false;
            }

            return true;
        }

        protected override void FreeManaged()
        {
            base.FreeManaged();

            ClosePendingReader();

            var connection = ActiveConnection;

            try
            {
                if (connection != null && connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }

                _lastException = null;
            }
            catch (Exception e)
            {
                _lastException = e;
                PhpException.Throw(PhpError.Warning, LibResources.error_closing_connection, GetExceptionMessage(e));
            }
        }

        /// <summary>
		/// Gets a query result resource.
		/// </summary>
		/// <param name="reader">Data reader to be used for result resource population.</param>
		/// <param name="convertTypes">Whether to convert data types to PHP ones.</param>
		/// <returns>Result resource holding all resulting data of the query.</returns>
		protected abstract ResultResource GetResult(IDataReader/*!*/ reader, bool convertTypes);

        /// <summary>
        /// Creates a command instance.
        /// </summary>
        /// <returns>Instance of command specific for the database provider and connection.</returns>
        protected abstract IDbCommand/*!*/ CreateCommand(string/*!*/ commandText, CommandType commandType);

        /// <summary>
		/// Builds a connection string in form of <c>server=;user id=;password=</c>.
		/// </summary>
		public static string BuildConnectionString(string server, string user, string password, string additionalSettings)
        {
            var result = new StringBuilder(32);

            result.Append("server=");
            result.Append(server);
            result.Append(";user id=");
            result.Append(user);
            result.Append(";password=");
            result.Append(password);

            if (!string.IsNullOrEmpty(additionalSettings))
            {
                result.Append(';');
                result.AppendFormat(additionalSettings);
            }

            return result.ToString();
        }

        /// <summary>
		/// Closes pending reader.
		/// </summary>
		public virtual void ClosePendingReader()
        {
            if (_pendingReader != null)
            {
                _pendingReader.Close();
                _pendingReader = null;
            }
        }

        /// <summary>
		/// Executes a query on the connection.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="convertTypes">Whether to convert data types to PHP ones.</param>
		/// <returns>PhpDbResult class representing the data read from database.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="query"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException">Query execution failed (Warning).</exception>
		public ResultResource ExecuteQuery(string/*!*/ query, bool convertTypes)
        {
            if (query == null)
                throw new ArgumentNullException("query");

            return ExecuteCommand(query, CommandType.Text, convertTypes, null, false);
        }

        /// <summary>
        /// Executes a stored procedure on the connection.
        /// </summary>
        /// <param name="procedureName">Procedure name.</param>
        /// <param name="parameters">Parameters.</param>
        /// <param name="skipResults">Whether to load results.</param>
        /// <returns>PhpDbResult class representing the data read from database.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="procedureName"/> is a <B>null</B> reference.</exception>
        /// <exception cref="PhpException">Procedure execution failed (Warning).</exception>
        public ResultResource ExecuteProcedure(string/*!*/ procedureName, IList<IDataParameter> parameters, bool skipResults)
        {
            if (procedureName == null)
                throw new ArgumentNullException("procedureName");

            return ExecuteCommand(procedureName, CommandType.StoredProcedure, true, parameters, skipResults);
        }

        /// <summary>
        /// Executes a command on the connection.
        /// </summary>
        /// <param name="commandText">Command text.</param>
        /// <param name="convertTypes">Whether to convert data types to PHP ones.</param>
        /// <param name="commandType">Command type.</param>
        /// <param name="parameters">Parameters.</param>
        /// <param name="skipResults">Whether to load results.</param>
        /// <returns>PhpDbResult class representing the data read from database.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="commandText"/> is a <B>null</B> reference.</exception>
        /// <exception cref="PhpException">Command execution failed (Warning).</exception>
        public ResultResource ExecuteCommand(string/*!*/ commandText, CommandType commandType, bool convertTypes, IList<IDataParameter> parameters, bool skipResults)
        {
            if (commandText == null)
            {
                throw new ArgumentNullException(nameof(commandText));
            }

            if (Connect())
            {
                ClosePendingReader();   // needs to be closed before creating the command

                var command = CreateCommand(commandText, commandType);

                return ExecuteCommandProtected(command, convertTypes, parameters, skipResults);
            }
            else
            {
                return null;
            }
        }

        protected virtual ResultResource ExecuteCommandProtected(IDbCommand command, bool convertTypes, IList<IDataParameter> parameters, bool skipResults)
        {
            if (parameters != null)
            {
                command.Parameters.Clear();

                for (int iparam = 0; iparam < parameters.Count; iparam ++)
                {
                    command.Parameters.Add(parameters[iparam]);
                }
            }

            // ExecuteReader
            ResultResource result = null;

            try
            {
                var/*!*/reader = _pendingReader = command.ExecuteReader();

                if (skipResults)
                {
                    // reads all data:
                    do { while (reader.Read()) ; } while (reader.NextResult());
                }
                else
                {
                    _lastResult = null;

                    // read all data into PhpDbResult:
                    result = GetResult(reader, convertTypes);
                    result.command = command;

                    _lastResult = result;
                }

                _lastException = null;
            }
            catch (Exception e)
            {
                _lastException = e;
                PhpException.Throw(PhpError.Warning, LibResources.command_execution_failed, GetExceptionMessage(e));
            }

            //
            return result;
        }

        /// <summary>
		/// Re-executes a command associated with a specified result resource to get schema of the command result.
		/// </summary>
		/// <param name="result">The result resource.</param>
		internal void ReexecuteSchemaQuery(ResultResource/*!*/ result)
        {
            if (!Connect() || result.Command == null) return;

            ClosePendingReader();

            try
            {
                result.Reader = _pendingReader = result.Command.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly);
            }
            catch (Exception e)
            {
                _lastException = e;
                PhpException.Throw(PhpError.Warning, LibResources.command_execution_failed, GetExceptionMessage(e));
            }
        }

        /// <summary>
		/// Changes the active database on opened connection.
		/// </summary>
		/// <param name="databaseName">Database name.</param>
		/// <returns><c>True</c> if the database was changed, otherwise <c>false</c>.</returns>
		public bool SelectDb(string databaseName)
        {
            ClosePendingReader();

            try
            {
                var connection = ActiveConnection;
                if (connection.State == ConnectionState.Open)
                {
                    connection.ChangeDatabase(databaseName);
                    _lastException = null;
                    return true;
                }
            }
            catch (Exception e)
            {
                _lastException = e;
                PhpException.Throw(PhpError.Warning, LibResources.database_selection_failed, GetExceptionMessage(e));
            }

            return false;
        }

        /// <summary>
        /// Gets a message from an exception raised by the connector.
        /// Removes the ending dot.
        /// </summary>
        /// <param name="e">Exception.</param>
        /// <returns>The message.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="e"/> is a <B>null</B> reference.</exception>
        public virtual string GetExceptionMessage(System.Exception/*!*/ e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            return PhpException.ToErrorMessage(e.Message);
        }

        /// <summary>
        /// Gets the last error message.
        /// </summary>
        /// <returns>The message or an empty string if no error occured.</returns>
        public virtual string GetLastErrorMessage()
        {
            return (LastException != null) ? LastException.Message : string.Empty;
        }

        /// <summary>
        /// Gets the last error number.
        /// </summary>
        /// <returns>-1 on error, zero otherwise.</returns>
        /// <remarks>Should be implemented by the subclass if the respective provider supports error numbers.</remarks>
        public virtual int GetLastErrorNumber()
        {
            return (LastException != null) ? -1 : 0;
        }
    }
}
