/*

 Copyright (c) 2005-2006 Tomas Matousek and Martin Maly.  
 Copyright (c) 20012-2017 Jakub Misek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Resources;

namespace Peachpie.Library.MsSql
{
    /// <summary>
    /// Implements PHP functions provided by MSSQL extension.
    /// </summary>
    [PhpExtension("mssql", Registrator = typeof(Registrator))]
    public static class MsSql
    {
        sealed class Registrator
        {
            public Registrator()
            {
                Context.RegisterConfiguration(new MsSqlConfiguration());
                MsSqlConfiguration.RegisterLegacyOptions();
            }
        }

        #region Constants: QueryResultKeys, VariableType

        /// <summary>
        /// Query result array format.
        /// </summary>
        [Flags, PhpHidden]
        public enum QueryResultKeys
        {
            /// <summary>
            /// Add items keyed by column names.
            /// </summary>
            ColumnNames = 1,

            /// <summary>
            /// Add items keyed by column indices.
            /// </summary>
            Numbers = 2,

            /// <summary>
            /// Add both items keyed by column names and items keyd by column indices.
            /// </summary>
            Both = ColumnNames | Numbers
        }

        /// <summary>Add items keyed by column names.</summary>
        public const int MSSQL_ASSOC = (int)QueryResultKeys.ColumnNames;
        /// <summary>Add items keyed by column indices.</summary>
        public const int MSSQL_NUM = (int)QueryResultKeys.Numbers;
        /// <summary>Add both items keyed by column names and items keyd by column indices.</summary>
        public const int MSSQL_BOTH = (int)QueryResultKeys.Both;

        /// <summary>
        /// Types of variables bound to stored procedure parameters.
        /// </summary>
        [PhpHidden]
        public enum VariableType
        {
            /// <summary></summary>
            Bit = 50,

            /// <summary></summary>
            Text = 35,
            /// <summary></summary>
            VarChar = 39,
            /// <summary></summary>
            Char = 47,

            /// <summary></summary>
            Int8 = 48,
            /// <summary></summary>
            Int16 = 52,
            /// <summary></summary>
            Int32 = 56,

            /// <summary></summary>
            Float = 59,
            /// <summary></summary>
            Double = 62,
            /// <summary></summary>
            FloatN = 109
        }

        /// <summary></summary>
        public const int SQLBIT = (int)VariableType.Bit;
        /// <summary></summary>
        public const int SQLTEXT = (int)VariableType.Text;
        /// <summary></summary>
        public const int SQLVARCHAR = (int)VariableType.VarChar;
        /// <summary></summary>
        public const int SQLCHAR = (int)VariableType.Char;
        /// <summary></summary>
        public const int SQLINT1 = (int)VariableType.Int8;
        /// <summary></summary>
        public const int SQLINT2 = (int)VariableType.Int16;
        /// <summary></summary>
        public const int SQLINT4 = (int)VariableType.Int32;
        /// <summary></summary>
        public const int SQLFLT4 = (int)VariableType.Float;
        /// <summary></summary>
        public const int SQLFLT8 = (int)VariableType.Double;
        /// <summary></summary>
        public const int SQLFLTN = (int)VariableType.FloatN;

        #endregion

        #region SqlConnectionManager

        static SqlConnectionManager GetManager(Context ctx) => SqlConnectionManager.GetInstance(ctx);

        /// <summary>
        /// Gets last active connection.
        /// </summary>
        static PhpSqlDbConnection LastConnection(Context ctx) => GetManager(ctx).GetLastConnection();

        static void UpdateConnectErrorInfo(Context ctx, PhpSqlDbConnection connection)
        {
            GetManager(ctx).FailConnectErrorMessage = connection.GetLastErrorMessage();
        }

        static PhpSqlDbConnection ValidConnection(Context ctx, PhpResource link)
        {
            var resource = link ?? LastConnection(ctx);
            if (resource is PhpSqlDbConnection)
            {
                return (PhpSqlDbConnection)resource;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Resources.invalid_connection_resource);
                return null;
            }
        }

        #endregion

        /// <summary>
		/// Closes a specified connection.
		/// </summary>
        /// <param name="ctx">PHP context.</param>
		/// <param name="linkIdentifier">The connection resource.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
        public static bool mssql_close(Context ctx, PhpResource linkIdentifier = null)
        {
            var connection = ValidConnection(ctx, linkIdentifier);
            if (connection != null)
            {
                connection.Dispose();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
		/// Establishes a connection to SQL server using a specified server, user, and password and default flags.
		/// </summary>
        /// <param name="ctx">PHP context.</param>
		/// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
		/// <param name="user">User name. A <b>null</b> reference means the default value.</param>
		/// <param name="password">Password. A <b>null</b> reference means the default value.</param>
		/// <param name="newLink">Whether to create a new link.</param>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// </remarks>		
        [return: CastToFalse]
        public static PhpResource mssql_connect(Context ctx, string server = null, string user = null, string password = null, bool newLink = false)
        {
            return Connect(ctx, server, user, password, newLink, false);
        }

        /// <summary>
		/// Establishes a connection to SQL server using a specified server, user, and password and default flags.
		/// </summary>
		/// <param name="ctx">PHP context.</param>
		/// <param name="server">Server (host). A <b>null</b> reference means the default value.</param>
		/// <param name="user">User name. A <b>null</b> reference means the default value.</param>
		/// <param name="password">Password. A <b>null</b> reference means the default value.</param>
		/// <param name="newLink">Whether to create a new link.</param>
		/// <returns>
		/// Resource representing the connection or a <B>null</B> reference (<B>false</B> in PHP) on failure.
		/// </returns>
		/// <remarks>
		/// Default values are taken from the configuration.
		/// Creates a non-persistent connection. Persistent connections are not supported.
		/// </remarks>		
        public static PhpResource mssql_pconnect(Context ctx, string server = null, string user = null, string password = null, bool newLink = false)
        {
            return Connect(ctx, server, user, password, newLink, true);
        }

        static PhpResource Connect(Context ctx, string server, string user, string password, bool newLink, bool persistent)
        {
            if (persistent)
            {
                // persistent connections are treated as transient, a warning is issued:
                PhpException.FunctionNotSupported("mssql_pconnect");
            }

            var config = ctx.Configuration.Get<MsSqlConfiguration>();
            var opts = new StringBuilder();

            if (config.ConnectTimeout > 0)
                opts.AppendFormat("Connect Timeout={0}", config.ConnectTimeout);

            if (config.NTAuthentication)
            {
                if (opts.Length > 0) opts.Append(';');
                user = password = null;
                opts.Append("Integrated Security=true");
            }

            string connection_string = PhpSqlDbConnection.BuildConnectionString(server, user, password, opts.ToString());

            bool success;
            PhpSqlDbConnection connection = GetManager(ctx).CreateConnection(connection_string,
              newLink, config.MaxConnections, out success);

            if (!success)
            {
                if (connection != null)
                {
                    UpdateConnectErrorInfo(ctx, connection);
                    connection = null;
                }
                return null;
            }

            return connection;
        }

        /// <summary>
		/// Releases a resource representing a query result.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource).</returns>
        public static bool mssql_free_result(PhpResource resultHandle)
        {
            var result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return false;

            result.Dispose();
            return true;
        }

        /// <summary>
		/// Selects the current DB for a specified connection.
		/// </summary>
        /// <param name="ctx">PHP context.</param>
		/// <param name="databaseName">Name of the database.</param>
		/// <param name="linkIdentifier">Connection resource.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure.</returns>
        public static bool mssql_select_db(Context ctx, string databaseName, PhpResource linkIdentifier = null)
        {
            var connection = ValidConnection(ctx, linkIdentifier);
            if (connection == null) return false;

            return connection.SelectDb(databaseName);
        }

        /// <summary>
		/// Sends a query to the current database associated with a specified connection.
		/// </summary>
		/// <param name="ctx">PHP context.</param>
		/// <param name="query">Query.</param>
		/// <param name="linkIdentifier">Connection resource.</param>
		/// <param name="batchSize">Connection resource.</param>
		/// <returns>Query resource or a <B>null</B> reference (<B>null</B> in PHP) on failure.</returns>
        [return: CastToFalse]
        public static PhpResource mssql_query(Context ctx, string query, PhpResource linkIdentifier = null, int batchSize = 0)
        {
            var connection = ValidConnection(ctx, linkIdentifier);
            if (query == null || connection == null) return null;

            var result = (PhpSqlDbResult)connection.ExecuteQuery(query.Trim(), true);
            if (result == null) return null;

            result.BatchSize = batchSize;
            return result;
        }

        /// <summary>
        /// Get a result row as an integer indexed array. 
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <returns>Array indexed by integers starting from 0 containing values of the current row.</returns>
        [return: CastToFalse]
        public static PhpArray mssql_fetch_row(PhpResource resultHandle) => mssql_fetch_array(resultHandle, QueryResultKeys.Numbers);

        /// <summary>
        /// Get a result row as an associative array. 
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <returns>Array indexed by column names containing values of the current row.</returns>			
        public static PhpArray mssql_fetch_assoc(PhpResource resultHandle) => mssql_fetch_array(resultHandle, QueryResultKeys.ColumnNames);

        /// <summary>
        /// Get a result row as an array with a specified key format. 
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <param name="resultType">Type(s) of keys in the resulting array.</param>
        /// <returns>
        /// Array containing values of the rows indexed by column keys and/or column indices depending 
        /// on value of <paramref name="resultType"/>.
        /// </returns>
        [return: CastToFalse]
        public static PhpArray mssql_fetch_array(PhpResource resultHandle, QueryResultKeys resultType = QueryResultKeys.Both)
        {
            var result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return null;

            switch (resultType)
            {
                case QueryResultKeys.ColumnNames: return result.FetchArray(false, true);
                case QueryResultKeys.Numbers: return result.FetchArray(true, false);
                case QueryResultKeys.Both: return result.FetchArray(true, true);
            }

            return null;
        }

        /// <summary>
        /// Get a result row as an object. 
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <returns>
        /// Object whose fields contain values from the current row. 
        /// Field names corresponds to the column names.
        /// </returns>
        [return: CastToFalse]
        public static stdClass mssql_fetch_object(PhpResource resultHandle)
        {
            var result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return null;

            return result.FetchStdClass();
        }

        /// <summary>
		/// Get a number of affected rows in the previous operation.
		/// </summary>
		/// <param name="ctx">PHP context.</param>
		/// <param name="linkIdentifier">Connection resource.</param>
		/// <returns>The number of affected rows or -1 if the last operation failed or the connection is invalid.</returns>
        public static int mssql_rows_affected(Context ctx, PhpResource linkIdentifier)
        {
            var connection = ValidConnection(ctx, linkIdentifier);
            if (connection == null) return -1;

            return connection.LastAffectedRows;
        }

        /// <summary>
		/// Get number of columns (fields) in a specified result.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Number of columns in the specified result or 0 if the result resource is invalid.</returns>
        public static int mssql_num_fields(PhpResource resultHandle)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return 0;

            return result.FieldCount;
        }

        /// <summary>
        /// Get number of rows in a specified result.
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <returns>Number of rows in the specified result or 0 if the result resource is invalid.</returns>
        public static int mssql_num_rows(PhpResource resultHandle)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return 0;

            return result.RowCount;
        }

        /// <summary>
		/// Gets last error message.
		/// </summary>
		/// <returns>The message sent by server.</returns>
        public static string mssql_get_last_message(Context ctx)
        {
            var manager = GetManager(ctx);
            var last_connection = manager.GetLastConnection();

            if (last_connection == null)
            {
                return manager.FailConnectErrorMessage;
            }
            else
            {
                return last_connection.GetLastErrorMessage();
            }
        }

        /// <summary>
        /// Sets a threshold for displaying errors sent by server. Not supported.
        /// </summary>
        /// <param name="severity">Severity threshold.</param>
        public static void mssql_min_error_severity(int severity)
        {
            PhpException.FunctionNotSupported("mssql_min_error_severity");
        }

        /// <summary>
        /// Sets a threshold for displaying messages sent by server. Not supported.
        /// </summary>
        /// <param name="severity">Severity threshold.</param>
        public static void mssql_min_message_severity(int severity)
        {
            PhpException.FunctionNotSupported("mssql_min_message_severity");
        }

        /// <summary>
		/// Gets a contents of a specified cell from a specified query result resource.
		/// </summary>
		/// <param name="ctx">PHP context.</param>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="rowIndex">Row index.</param>
		/// <param name="field">Column (field) integer index or string name.</param>
		/// <returns>The value of the cell or <B>false</B> on failure (invalid resource or row index).</returns>
        public static PhpValue mssql_result(Context ctx, PhpResource resultHandle, int rowIndex, PhpValue field)
        {
            var result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return PhpValue.False;

            string field_name;
            object field_value;
            if (field.IsNull)
            {
                field_value = result.GetFieldValue(rowIndex, result.CurrentFieldIndex);
            }
            else if ((field_name = PhpVariable.AsString(field)) != null)
            {
                field_value = result.GetFieldValue(rowIndex, field_name);
            }
            else
            {
                field_value = result.GetFieldValue(rowIndex, (int)field.ToLong());
            }

            if (field_value == null)
            {
                return PhpValue.False;
            }

            return PhpValue.FromClr(field_value); // Core.Convert.Quote(field_value, context);
        }

        /// <summary>
		/// Fetches the next result set if the query returned multiple result sets.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Whether the next result set is available.</returns>
        public static bool mssql_next_result(PhpResource resultHandle)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return false;

            return result.NextResultSet();
        }

        /// <summary>
		/// Gets a name of the current column (field) in a result. 
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <returns>Name of the column or a <B>null</B> reference on failure (invalid resource or column index).</returns>
		public static string mssql_field_name(PhpResource resultHandle)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return null;

            return result.GetFieldName();
        }

        /// <summary>
        /// Gets a name of a specified column (field) in a result. 
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <param name="fieldIndex">Column (field) index.</param>
        /// <returns>Name of the column or a <B>null</B> reference on failure (invalid resource or column index).</returns>
        public static string mssql_field_name(PhpResource resultHandle, int fieldIndex)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return null;

            return result.GetFieldName(fieldIndex);
        }

        /// <summary>
        /// Gets a type of the current column (field) in a result. 
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <returns>MSSQL type translated to PHP terminology.</returns>
        /// <remarks>
        /// Possible values are: TODO.
        /// </remarks>   
        public static string mssql_field_type(PhpResource resultHandle)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return null;

            return result.GetPhpFieldType();
        }

        /// <summary>
        /// Gets a type of a specified column (field) in a result. 
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <param name="fieldIndex">Column index.</param>
        /// <returns>MSSQL type translated to PHP terminology.</returns>
        /// <remarks>
        /// Possible values are: TODO.
        /// </remarks>   
        public static string mssql_field_type(PhpResource resultHandle, int fieldIndex)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return null;

            return result.GetPhpFieldType(fieldIndex);
        }

        /// <summary>
        /// Gets a length of the current column (field) in a result. 
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <returns>Length of the column or a -1 on failure (invalid resource or column index).</returns>
        public static int mssql_field_length(PhpResource resultHandle)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return -1;

            return result.GetFieldLength();
        }

        /// <summary>
        /// Gets a length of a specified column (field) in a result. 
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <param name="fieldIndex">Column index.</param>
        /// <returns>Length of the column or a -1 on failure (invalid resource or column index).</returns>
        public static int mssql_field_length(PhpResource resultHandle, int fieldIndex)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return -1;

            return result.GetFieldLength(fieldIndex);
        }

        /// <summary>
		/// Sets the result resource's current column (field) offset.
		/// </summary>
		/// <param name="resultHandle">Query result resource.</param>
		/// <param name="fieldOffset">New column offset.</param>
		/// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource or column offset).</returns>
        public static bool mssql_field_seek(PhpResource resultHandle, int fieldOffset)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return false;

            return result.SeekField(fieldOffset);
        }

        /// <summary>
        /// Sets the result resource's current row index.
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <param name="rowIndex">New row index.</param>
        /// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource or row index).</returns>
        public static bool mssql_data_seek(PhpResource resultHandle, int rowIndex)
        {
            PhpSqlDbResult result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null) return false;

            return result.SeekRow(rowIndex);
        }

        /// <summary>
        /// Gets a PHP object whose properties describes a specified field.
        /// </summary>
        /// <param name="resultHandle">Query result resource.</param>
        /// <param name="fieldIndex">Field index.</param>
        /// <returns>The PHP object.</returns>
        public static stdClass mssql_fetch_field(PhpResource resultHandle, int fieldIndex = -1)
        {
            var result = PhpSqlDbResult.ValidResult(resultHandle);
            if (result == null)
            {
                return null;
            }

            if (fieldIndex < 0)
            {
                fieldIndex = result.FetchNextField();
            }

            //DataRow info = result.GetSchemaRowInfo(fieldIndex);
            //if (info == null) return null;
            //string s;
            
            string php_type = result.GetPhpFieldType(fieldIndex);

            var arr = new PhpArray(5)
            {
                { "name", result.GetFieldName(fieldIndex) },
                // { ("column_source", (s = info["BaseColumnName"] as string) != null ? s : "" }, // TODO: column_source
                { "max_length", result.GetFieldLength(fieldIndex) },
                { "numeric", result.IsNumericType(php_type) ? 1 : 0 },
                { "type", php_type },
            };

            return arr.AsStdClass();
        }

        /// <summary>
		/// Not supported.
		/// </summary>
        [return: CastToFalse]
        public static PhpArray mssql_fetch_batch(PhpResource resultHandle)
        {
            PhpException.FunctionNotSupported("mssql_fetch_batch");
            return null;
        }

        /// <summary>
		/// Initializes a stored procedure of a given name.
		/// </summary>
		/// <param name="ctx">PHP context.</param>
		/// <param name="procedureName">Name of the stored procedure.</param>
		/// <param name="linkIdentifier">Connection resource.</param>
		/// <returns>Statement resource representing the procedure.</returns>
        [return: CastToFalse]
        public static PhpResource mssql_init(Context ctx, string procedureName, PhpResource linkIdentifier)
        {
            PhpSqlDbConnection connection = ValidConnection(ctx, linkIdentifier);
            if (connection == null) return null;

            if (procedureName == null)
            {
                PhpException.ArgumentNull("procedureName");
                return null;
            }

            return new PhpSqlDbProcedure(connection, procedureName);
        }

        /// <summary>
        /// Releases a resource representing a statement.
        /// </summary>
        /// <param name="statement">Statement resource.</param>
        /// <returns><B>true</B> on success, <B>false</B> on failure (invalid resource).</returns>
        public static bool mssql_free_statement(PhpResource statement)
        {
            PhpSqlDbProcedure procedure = PhpSqlDbProcedure.ValidProcedure(statement);
            if (procedure == null) return false;

            procedure.Dispose();
            return true;
        }

        /// <summary>
		/// Binds a PHP variable to an SQL parameter of a statement.
		/// </summary>
		/// <param name="statement">Statement resource.</param>
		/// <param name="parameterName">Parameter name starting with '@' character.</param>
		/// <param name="variable">PHP variable to bind to the parameter.</param>
		/// <param name="type">SQL type of the parameter.</param>
		/// <param name="isOutput">Whether the parameter is an output parameter.</param>
		/// <param name="isNullable">Whether the parameter accepts <B>null</B> values.</param>
		/// <param name="maxLength">Maximum size of input data.</param>
		/// <returns>Whether binding succeeded.</returns>
		public static bool mssql_bind(PhpResource statement, string parameterName, PhpAlias variable, VariableType type,
            bool isOutput = false, bool isNullable = false, int maxLength = -1)
        {
            PhpSqlDbProcedure procedure = PhpSqlDbProcedure.ValidProcedure(statement);
            if (procedure == null) return false;

            if (parameterName == null)
            {
                PhpException.ArgumentNull(nameof(parameterName));
                return false;
            }

            var param_type = PhpSqlDbProcedure.VariableTypeToParamType(type);
            if (param_type == PhpSqlDbProcedure.ParameterType.Invalid)
            {
                PhpException.ArgumentValueNotSupported("type", (int)type);
                return false;
            }

            SqlParameter parameter = new SqlParameter();
            parameter.ParameterName = parameterName;

            // it is necessary to set size for in-out params as the results are truncated to this size;
            // 8000 is maximal size of the data according to the doc:
            if (maxLength >= 0)
                parameter.Size = maxLength;
            else
                parameter.Size = 8000;

            if (string.Equals(parameterName, "RETVAL", StringComparison.OrdinalIgnoreCase))
                parameter.Direction = ParameterDirection.ReturnValue;
            else if (isOutput)
                parameter.Direction = ParameterDirection.InputOutput;
            else
                parameter.Direction = ParameterDirection.Input;

            if (!procedure.AddBinding(parameter, variable, param_type))
            {
                PhpException.Throw(PhpError.Notice, Resources.parameter_already_bound, parameterName);
                return false;
            }

            return true;
        }

        /// <summary>
		/// Executes a specified stored procedure statement.
		/// </summary>
		/// <param name="statement">Statement resource (stored procedure).</param>
		/// <param name="skipResults">Whether to retrieve and return procedure output.</param>
		/// <returns>
		/// Result resource containing procedure output, 
		/// <B>true</B> if the procedure succeeded yet doesn't return any value, or
		/// <B>false</B> on failure.
		/// </returns>
		public static object mssql_execute(PhpResource statement, bool skipResults = false)
        {
            PhpSqlDbProcedure procedure = PhpSqlDbProcedure.ValidProcedure(statement);
            if (procedure == null) return false;

            bool success;
            PhpSqlDbResult result = procedure.Execute(skipResults, out success);

            if (!success) return false;
            if (skipResults) return true;
            return result;
        }

        /// <summary>
		/// Converts 16 bytes to a string representation of a GUID.
		/// </summary>
		/// <param name="binary">Binary representation of a GUID.</param>
		/// <param name="shortFormat">Whether to return a short format.</param>
		/// <returns>String representation of a GUID.</returns>
        public static string mssql_guid_string(byte[] binary, bool shortFormat = false)
        {
            if (binary == null || binary.Length == 0)
                return String.Empty;

            if (binary.Length != 16)
            {
                PhpException.InvalidArgument("binary", Resources.arg_invalid_length);
                return null;
            }

            return shortFormat
                ? new Guid(binary).ToString("D").ToUpper()
                : Pchp.Core.Utilities.StringUtils.BinToHex(binary).ToUpper();
        }
    }
}
