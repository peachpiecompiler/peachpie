using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pchp.Core;
using static Peachpie.Library.PDO.PDO;
using static Pchp.Library.Objects;
using Pchp.Core.Reflection;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDOStatement class
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(PDOConfiguration.PdoExtensionName)]
    public class PDOStatement : IDisposable
    {
        /// <summary>Runtime context. Cannot be <c>null</c>.</summary>
        protected readonly Context _ctx; // "_ctx" is a special name recognized by compiler. Will be reused by inherited classes.

        private PDO m_pdo;
        private string m_stmt;
        private PhpArray m_options;

        private DbCommand m_cmd;
        private DbDataReader m_dr;
        private readonly Dictionary<PDO.PDO_ATTR, PhpValue> m_attributes = new Dictionary<PDO_ATTR, PhpValue>();
        private string[] m_dr_names;

        private bool m_positionalAttr = false;
        private bool m_namedAttr = false;
        private Dictionary<string, string> m_namedPlaceholders;
        private List<string> m_positionalPlaceholders;

        private Dictionary<string, PhpAlias> m_boundParams;
        private PDO_FETCH m_fetchStyle = PDO_FETCH.Default;

        private PhpArray storedQueryResult = null;
        private int storedResultPosition = 0;

        /// <summary>
        /// Property telling whether there are any command parameter variables bound with bindParam.
        /// </summary>
        bool HasParamsBound => m_boundParams != null && m_boundParams.Count != 0;
        /// <summary>
        /// Column Number property for FETCH_COLUMN fetching.
        /// </summary>
        int? FetchColNo { get; set; }
        /// <summary>
        /// Class Name property for FETCH_CLASS fetching mode.
        /// </summary>
        PhpTypeInfo FetchClassName { get; set; }
        /// <summary>
        /// Class Constructor Args for FETCH_CLASS fetching mode.
        /// </summary>
        PhpValue[] FetchClassCtorArgs { get; set; }

        /// <summary>
        /// Class instance to results be fetched into (FETCH_INTO).
        /// </summary>
        object FetchClassInto { get; set; }

        private bool CheckDataReader()
        {
            if (m_dr == null)
            {
                RaiseError("The data reader cannot be null.");
                return false;
            }
            else
            {
                return true;
            }
        }

        private void RaiseError(string message)
        {
            m_pdo.HandleError(new PDOException(message));
        }

        /// <summary>
        /// Default constructor that does not initialize the statement.
        /// </summary>
        [PhpFieldsOnlyCtor]
        protected PDOStatement(Context ctx)
        {
            Debug.Assert(ctx != null);

            _ctx = ctx;
            m_pdo = null;
            m_stmt = null;
            m_options = PhpArray.Empty;
            m_cmd = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PDOStatement" /> class.
        /// </summary>
        /// <param name="ctx">The php context.</param>
        /// <param name="pdo">The PDO statement is created for.</param>
        /// <param name="statement">The statement.</param>
        /// <param name="driver_options">The driver options.</param>
        internal PDOStatement(Context ctx, PDO pdo, string statement, PhpArray driver_options)
            : this(ctx)
        {
            PrepareStatement(pdo, statement, driver_options);
        }

        internal void PrepareStatement(PDO pdo, string statement, PhpArray driver_options)
        {
            if (pdo.HasExecutedQuery)
            {
                if (!pdo.StoreLastExecutedQuery())
                {
                    pdo.HandleError(new PDOException("Last executed PDOStatement result set could not be saved correctly."));
                }
            }

            this.m_pdo = pdo;
            this.m_stmt = statement;
            this.m_options = driver_options ?? PhpArray.Empty;

            this.m_cmd = pdo.CreateCommand(this.m_stmt);

            PrepareStatement();
        }

        private static readonly Regex regName = new Regex(@"[\w_]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private PhpValue FetchFromStored()
        {
            if (storedQueryResult != null)
            {
                if (storedResultPosition < storedQueryResult.Count)
                {
                    return storedQueryResult[storedResultPosition++];
                }
                return PhpValue.False;
            }
            return PhpValue.False;
        }

        /// <summary>
        /// Function for storing the whole dataset result of a query from the dataReader.
        /// Necessary, because there can be only one data reader open for a single Db connection = single PDO instance.
        /// </summary>
        /// <returns>true on success, false otherwise</returns>
        public bool StoreQueryResult()
        {
            if (m_dr != null)
            {
                if (m_dr.HasRows)
                {
                    storedQueryResult = fetchAll();
                    storedResultPosition = 0;
                }
                else
                {
                    // No rows to save - possibly a non-query statement was executed
                    return true;
                }
            }
            else
            {
                //m_pdo.HandleError(new PDOException("There are no rows to store."));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prepare the PDOStatement command.
        /// Set either positional, named or neither parameters mode.
        /// Create the parameters, add them to the command and prepare the command.
        /// </summary>
        /// <returns></returns>
        private bool PrepareStatement()
        {
            Debug.Assert(m_stmt != null && m_stmt.Length > 0);

            m_namedPlaceholders = new Dictionary<string, string>();
            m_positionalPlaceholders = new List<string>();
            m_namedAttr = false;
            m_positionalAttr = false;

            int pos = 0;
            var rewrittenQuery = new StringBuilder();

            // Go throught the text query and find either positional or named parameters
            while (pos < m_stmt.Length)
            {
                char currentChar = m_stmt[pos];
                string paramName = "";

                switch (currentChar)
                {
                    case '?':
                        if (m_namedAttr)
                        {
                            throw new PDOException("Mixing positional and named parameters not allowed. Use only '?' or ':name' pattern");
                        }

                        m_positionalAttr = true;

                        paramName = "@p" + m_positionalPlaceholders.Count();
                        m_positionalPlaceholders.Add(paramName);
                        rewrittenQuery.Append(paramName);

                        break;

                    case ':':
                        if (m_positionalAttr)
                        {
                            throw new PDOException("Mixing positional and named parameters not allowed.Use only '?' or ':name' pattern");
                        }

                        m_namedAttr = true;

                        var match = regName.Match(m_stmt, pos);
                        string param = match.Value;

                        paramName = "@" + param;
                        m_namedPlaceholders[param] = paramName;
                        rewrittenQuery.Append(paramName);

                        pos += param.Length;

                        break;

                    case '"':
                        rewrittenQuery.Append(currentChar);
                        pos = SkipQuotedWord(m_stmt, rewrittenQuery, pos, '"');
                        break;

                    case '\'':
                        rewrittenQuery.Append(currentChar);
                        pos = SkipQuotedWord(m_stmt, rewrittenQuery, pos, '\'');
                        break;

                    default:
                        rewrittenQuery.Append(currentChar);
                        break;
                }
                pos++;
            }

            m_cmd.CommandText = rewrittenQuery.ToString();
            m_cmd.Parameters.Clear();

            if (m_positionalAttr)
            {
                // Mixed parameters not allowed
                if (m_namedAttr)
                {
                    m_pdo.HandleError(new PDOException("Mixed parameters mode not allowed. Use either only positional, or only named parameters."));
                    return false;
                }

                foreach (var paramName in m_positionalPlaceholders.ToArray())
                {
                    var param = m_cmd.CreateParameter();
                    param.ParameterName = paramName;
                    m_cmd.Parameters.Add(param);
                }
            }
            else if (m_namedAttr)
            {
                foreach (var paramPair in m_namedPlaceholders)
                {
                    var param = m_cmd.CreateParameter();
                    param.ParameterName = paramPair.Key;
                    m_cmd.Parameters.Add(param);
                }
            }

            // Finalise the command preparation
            m_cmd.Prepare();

            return true;
        }

        private void SetAttributesType()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Skip the quoted part of query
        /// </summary>
        /// <param name="query">Textual query</param>
        /// <param name="rewrittenBuilder">StringBuilder for the rewritten query</param>
        /// <param name="pos">Current position in the query</param>
        /// <param name="quoteType">Quotational character used</param>
        /// <returns>New position in the query</returns>
        private int SkipQuotedWord(string query, StringBuilder rewrittenBuilder, int pos, char quoteType)
        {
            while (++pos < query.Length)
            {
                char currentChar = query[pos];
                rewrittenBuilder.Append(currentChar);

                if (currentChar == quoteType)
                    break;

                if (currentChar == '\\')
                    pos++;
            }
            return pos;
        }

        /// <inheritDoc />
        void IDisposable.Dispose()
        {
            this.m_dr?.Dispose();
            this.m_cmd.Dispose();
        }

        private void OpenReader()
        {
            if (m_dr == null)
            {
                PDO.PDO_CURSOR cursor = (PDO.PDO_CURSOR)this.m_attributes[PDO.PDO_ATTR.ATTR_CURSOR].ToLong();
                this.m_dr = this.m_pdo.Driver.OpenReader(this.m_pdo, this.m_cmd, cursor);
                switch (cursor)
                {
                    case PDO.PDO_CURSOR.CURSOR_FWDONLY:
                        this.m_dr = this.m_cmd.ExecuteReader();
                        break;
                    case PDO.PDO_CURSOR.CURSOR_SCROLL:
                        this.m_dr = this.m_cmd.ExecuteReader();
                        break;
                    default:
                        throw new InvalidProgramException();
                }

                InitializeColumnNames();
            }
        }

        /// <summary>
        /// Bind a column to a PHP variable.
        /// </summary>
        /// <param name="colum">Number of the column (1-indexed) or name of the column in the result set. If using the column name, be aware that the name should match the case of the column, as returned by the driver</param>
        /// <param name="param">Name of the PHP variable to which the column will be bound.</param>
        /// <param name="type">Data type of the parameter, specified by the PDO::PARAM_* constants.</param>
        /// <param name="maxlen">A hint for pre-allocation.</param>
        /// <param name="driverdata">Optional parameter(s) for the driver.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public bool bindColumn(PhpValue colum, PhpAlias param, int? type = default(int?), int? maxlen = default(int?), PhpValue? driverdata = default(PhpValue?))
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Binds a parameter to the specified variable name
        /// </summary>
        /// <param name="parameter">Parameter identifier. For a prepared statement using named placeholders, this will be a parameter name of the form :name. For a prepared statement using question mark placeholders, this will be the 1-indexed position of the parameter.</param>
        /// <param name="variable">Name of the PHP variable to bind to the SQL statement parameter.</param>
        /// <param name="data_type">Explicit data type for the parameter using the PDO::PARAM_* constants. To return an INOUT parameter from a stored procedure, use the bitwise OR operator to set the PDO::PARAM_INPUT_OUTPUT bits for the data_type parameter.</param>
        /// <param name="length">Length of the data type. To indicate that a parameter is an OUT parameter from a stored procedure, you must explicitly set the length.</param>
        /// <param name="driver_options"></param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public bool bindParam(PhpValue parameter, PhpAlias variable, PDO.PARAM data_type = PDO.PARAM.PARAM_STR, int length = -1, PhpValue driver_options = default(PhpValue))
        {
            Debug.Assert(this.m_cmd != null);

            // lazy instantization
            if (m_boundParams == null)
                m_boundParams = new Dictionary<string, PhpAlias>();

            IDbDataParameter param;

            if (m_namedAttr)
            {
                // Mixed parameters not allowed
                if (m_positionalAttr)
                {
                    m_pdo.HandleError(new PDOException("Mixed parameters mode not allowed. Use either only positional, or only named parameters."));
                    return false;
                }

                if (parameter.IsString(out var key) && !string.IsNullOrEmpty(key))
                {
                    if (key.Length > 0 && key[0] == ':')
                    {
                        key = key.Substring(1);
                    }

                    //Store the bounded variable reference in the dictionary
                    m_boundParams.Add(key, variable);

                    param = m_cmd.Parameters[key];
                }
                else
                {
                    m_pdo.HandleError(new PDOException("Supplied parameter name must be a string."));
                    return false;
                }
            }
            else if (m_positionalAttr)
            {
                if (parameter.IsLong(out var index))    // 1-based index
                {
                    index--;

                    //Store the bounded variable.Value reference in the dictionary
                    m_boundParams.Add(index.ToString(), variable);

                    param = (index < m_positionalPlaceholders.Count)
                        ? m_cmd.Parameters[(int)index]
                        : null;
                }
                else
                {
                    m_pdo.HandleError(new PDOException("Supplied parameter index must be an integer."));
                    return false;
                }
            }
            else
            {
                m_pdo.HandleError(new PDOException("No parameter mode set yet for this Statement. Possibly no parameters required?"));
                return false;
            }

            if (param == null)
            {
                m_pdo.HandleError(new PDOException("No matching parameter found."));
                return false;
            }

            switch (data_type)
            {
                case PDO.PARAM.PARAM_INT:
                    if (variable.Value.IsInteger())
                    {
                        param.DbType = DbType.Int32;
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                        return false;
                    }
                    break;

                case PDO.PARAM.PARAM_STR:
                    if (variable.Value.IsString() || variable.Value.IsNull)
                    {
                        param.DbType = DbType.String;
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                        return false;
                    }
                    break;
                case PDO.PARAM.PARAM_BOOL:
                    if (variable.Value.IsBoolean())
                    {
                        param.DbType = DbType.Boolean;
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                        return false;
                    }
                    break;

                case PDO.PARAM.PARAM_LOB:
                    if (variable.Value.IsString() || variable.Value.IsNull)  // Unicode or byte[]
                    {
                        param.DbType = DbType.Binary;
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                        return false;
                    }
                    break;

                // Currently not supported by any drivers
                case PDO.PARAM.PARAM_NULL:
                case PDO.PARAM.PARAM_STMT:
                    throw new NotImplementedException();
            }

            return true;
        }

        /// <summary>
        /// Binds a value to a parameter.
        /// </summary>
        /// <param name="parameter">Parameter identifier. For a prepared statement using named placeholders, this will be a parameter name of the form :name. For a prepared statement using question mark placeholders, this will be the 1-indexed position of the parameter.</param>
        /// <param name="value">The value to bind to the parameter.</param>
        /// <param name="data_type">Explicit data type for the parameter using the PDO::PARAM_* constants.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public bool bindValue(PhpValue parameter, PhpValue value, PDO.PARAM data_type = (PDO.PARAM)PDO.PARAM_STR)
        {
            Debug.Assert(this.m_cmd != null);

            IDbDataParameter param;

            if (m_namedAttr)
            {
                // Mixed parameters not allowed
                if (m_positionalAttr)
                {
                    m_pdo.HandleError(new PDOException("Mixed parameters mode not allowed. Use either only positional, or only named parameters."));
                    return false;
                }

                if (parameter.IsString(out var key))
                {
                    if (key.Length > 0 && key[0] == ':')
                    {
                        key = key.Substring(1);
                    }

                    param = m_cmd.Parameters[key];

                    //rewrite the bounded params dictionary
                    if (HasParamsBound)
                    {
                        m_boundParams.Remove(key);
                    }
                }
                else
                {
                    m_pdo.HandleError(new PDOException("Supplied parameter name must be a string."));
                    return false;
                }
            }
            else if (m_positionalAttr)
            {
                if (parameter.IsLong(out var index)) // 1-based index
                {
                    index--;

                    param = (index < m_positionalPlaceholders.Count)
                        ? m_cmd.Parameters[(int)index]
                        : null;

                    //rewrite the bounded params dictionary
                    if (HasParamsBound)
                    {
                        m_boundParams.Remove(index.ToString());
                    }
                }
                else
                {
                    m_pdo.HandleError(new PDOException("Supplied parameter index must be an integer."));
                    return false;
                }
            }
            else
            {
                m_pdo.HandleError(new PDOException("No parameter mode set yet for this Statement. Possibly no parameters required?"));
                return false;
            }

            if (param == null)
            {
                m_pdo.HandleError(new PDOException("No matching parameter found."));
                return false;
            }

            switch (data_type)
            {
                case PDO.PARAM.PARAM_INT:
                    if (value.IsInteger())
                    {
                        param.DbType = DbType.Int32;
                        param.Value = (int)value;
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                        return false;
                    }
                    break;

                case PDO.PARAM.PARAM_STR:
                    string str = null;
                    if ((str = value.ToStringOrNull()) != null)
                    {
                        param.DbType = DbType.String;
                        param.Value = str;
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                        return false;
                    }
                    break;
                case PDO.PARAM.PARAM_BOOL:
                    if (value.IsBoolean())
                    {
                        param.DbType = DbType.Boolean;
                        param.Value = value.ToBoolean();
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                        return false;
                    }
                    break;

                case PDO.PARAM.PARAM_LOB:
                    byte[] bytes = null;
                    if ((bytes = value.ToBytesOrNull()) != null)
                    {
                        param.DbType = DbType.Binary;
                        param.Value = bytes;
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                        return false;
                    }
                    break;

                // Currently not supported by any drivers
                case PDO.PARAM.PARAM_NULL:
                case PDO.PARAM.PARAM_STMT:
                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        /// <inheritDoc />
        public bool bindValues(PhpValue parameter, PhpValue value, int data_type = 2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Closes the current data reader, so that the DbConnection can be reused.
        /// Should store its state
        /// </summary>
        /// <returns></returns>
        public bool CloseReader()
        {
            if (this.m_dr != null && !this.m_dr.IsClosed)
            {
                m_dr.Close();
            }

            return false;
        }

        /// <summary>
        /// Closes the cursor, enabling the statement to be executed again.
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public bool closeCursor()
        {
            if (this.m_dr != null)
            {
                ((IDisposable)this.m_dr).Dispose();
                this.m_dr = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the number of columns in the result set
        /// </summary>
        /// <returns>Returns the number of columns in the result set represented by the PDOStatement object, even if the result set is empty. If there is no result set, PDOStatement::columnCount() returns 0</returns>
        public int columnCount()
        {
            if (CheckDataReader())
            {
                return this.m_dr.FieldCount;
            }
            else
            {
                return 0;
            }

        }

        /// <summary>
        /// Dump an SQL prepared command.
        /// </summary>
        public void debugDumpParams()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Fetch the SQLSTATE associated with the last operation on the statement handle
        /// </summary>
        /// <returns>Identical to PDO::errorCode(), except that PDOStatement::errorCode() only retrieves error codes for operations performed with PDOStatement objects</returns>
        public string errorCode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Fetch extended error information associated with the last operation on the statement handle.
        /// </summary>
        /// <returns>PDOStatement::errorInfo() returns an array of error information about the last operation performed by this statement handle.</returns>
        public PhpArray errorInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes a prepared statement
        /// </summary>
        /// <param name="input_parameters">An array of values with as many elements as there are bound parameters in the SQL statement being executed. All values are treated as PDO::PARAM_STR.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public bool execute(PhpArray input_parameters = null)
        {
            // Sets PDO executed flag
            m_pdo.HasExecutedQuery = true;

            // Assign the bound variables from bindParam() function if any present
            if (HasParamsBound)
            {
                foreach (var pair in m_boundParams)
                {
                    IDbDataParameter param = null;

                    if (m_namedAttr)
                    {
                        // Mixed parameters not allowed
                        if (m_positionalAttr)
                        {
                            m_pdo.HandleError(new PDOException("Mixed parameters mode not allowed. Use either only positional, or only named parameters."));
                            return false;
                        }

                        param = m_cmd.Parameters[pair.Key];
                    }
                    else if (m_positionalAttr)
                    {
                        if (int.TryParse(pair.Key, out var index))
                        {
                            param = m_cmd.Parameters[index];
                        }
                        else
                        {
                            m_pdo.HandleError(new PDOException("The string for positional parameter index must be an integer."));
                            return false;
                        }

                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("No parameter mode set yet for this Statement. Possibly no parameters required?"));
                        return false;
                    }

                    if (pair.Value.Value.IsNull)
                    {
                        param.Value = DBNull.Value;
                    }
                    else
                    {
                        switch (param.DbType)
                        {
                            case DbType.Int32:
                                if (pair.Value.Value.IsLong(out var l))
                                {
                                    param.Value = (int)l;
                                }
                                else
                                {
                                    m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                                    return false;
                                }
                                break;

                            case DbType.Int64:
                                if (pair.Value.Value.IsLong(out l))
                                {
                                    param.Value = l;
                                }
                                else
                                {
                                    m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                                    return false;
                                }
                                break;

                            case DbType.String:
                                if (pair.Value.Value.IsString(out var str))
                                {
                                    param.Value = str;
                                }
                                else
                                {
                                    m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                                    return false;
                                }
                                break;
                            case DbType.Boolean:
                                if (pair.Value.Value.IsBoolean(out var b))
                                {
                                    param.Value = b;
                                }
                                else
                                {
                                    m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                                    return false;
                                }
                                break;

                            case DbType.Binary:
                                var bytes = pair.Value.Value.ToBytesOrNull(_ctx);
                                if (bytes != null || pair.Value.Value.IsNull)
                                {
                                    param.Value = bytes;
                                }
                                else
                                {
                                    m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                                    return false;
                                }
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
            }

            // assign the parameters passed as an argument
            if (input_parameters != null)
            {
                foreach (var pair in input_parameters)
                {
                    IDbDataParameter param = null;

                    if (m_namedAttr)
                    {
                        // Mixed parameters not allowed
                        if (m_positionalAttr)
                        {
                            m_pdo.HandleError(new PDOException("Mixed parameters mode not allowed. Use either only positional, or only named parameters."));
                            return false;
                        }

                        string key = pair.Key.String;
                        if (key.Length > 0 && key[0] == ':')
                        {
                            key = key.Substring(1);
                        }
                        param = m_cmd.Parameters[key];
                    }
                    else if (m_positionalAttr)
                    {
                        if (pair.Key.IsInteger)
                        {
                            param = m_cmd.Parameters[pair.Key.Integer];
                        }
                        else
                        {
                            m_pdo.HandleError(new PDOException("The string for positional parameter index must be an integer."));
                            return false;
                        }
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("No parameter mode set yet for this Statement. Possibly no parameters required?"));
                        return false;
                    }

                    // Assign the value as a string
                    param.Value = pair.Value.ToString();
                }
            }

            // Close the previously used reader, so that the same connection can open a new one
            if (m_dr != null)
            {
                if (!m_dr.IsClosed)
                {
                    m_dr.Close();
                }
                m_dr = null;
            }

            // Actually execute
            try
            {
                m_dr = m_cmd.ExecuteReader();
            }
            catch (Exception e)
            {
                m_pdo.HandleError(new PDOException("Query could not be executed; \n" + e.Message));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Fetches the specified fetch style.
        /// </summary>
        /// <param name="fetch_style">Controls how the next row will be returned to the caller. This value must be one of the PDO::FETCH_* constants.</param>
        /// <param name="cursor_orientation">This value determines which row will be returned to the caller.</param>
        /// <param name="cursor_offet">Relative or absolute position move for the cursor.</param>
        /// <returns>The return value of this function on success depends on the fetch type. In all cases, FALSE is returned on failure.</returns>
        public PhpValue fetch(PDO.PDO_FETCH fetch_style = PDO_FETCH.Default/*0*/, PDO_FETCH_ORI cursor_orientation = PDO_FETCH_ORI.FETCH_ORI_NEXT/*0*/, int cursor_offet = 0)
        {
            this.m_pdo.ClearError();

            if (storedQueryResult != null)
            {
                return FetchFromStored();
            }
            else
            {
                if (cursor_orientation != PDO_FETCH_ORI.FETCH_ORI_NEXT) // 0
                {
                    throw new NotSupportedException(nameof(cursor_orientation));
                }

                try
                {
                    // read next row
                    if (!this.m_dr.Read())
                    {
                        return PhpValue.False;
                    }

                    if (m_dr_names == null)
                    {
                        // Get the column schema, if possible,
                        // for the associative fetch
                        InitializeColumnNames();
                    }

                    var style = fetch_style != PDO_FETCH.Default ? fetch_style : m_fetchStyle;
                    var flags = style & PDO_FETCH.Flags;

                    switch (style & ~PDO_FETCH.Flags)
                    {
                        case PDO_FETCH.FETCH_ASSOC:
                            return this.ReadArray(true, false);

                        case PDO_FETCH.FETCH_NUM:
                            return this.ReadArray(false, true);

                        case PDO_FETCH.Default:
                        case PDO_FETCH.FETCH_BOTH:
                            return this.ReadArray(true, true);

                        case PDO_FETCH.FETCH_OBJ:
                            return this.ReadObj();

                        case PDO_FETCH.FETCH_COLUMN:
                            if (FetchColNo.HasValue)
                            {
                                return this.ReadColumn(FetchColNo.Value);
                            }
                            else
                            {
                                m_pdo.HandleError(new PDOException("The column number for FETCH_COLUMN mode is not set."));
                                return PhpValue.False;
                            }

                        //case PDO.PDO_FETCH.FETCH_CLASS:
                        //    if (FetchClassName != null)
                        //    {
                        //        var obj = FetchClassName.Creator(_ctx, FetchClassCtorArgs ?? Array.Empty<PhpValue>());
                        //        // TODO: set properties
                        //        return PhpValue.FromClass(obj);
                        //    }
                        //    else
                        //    {
                        //        m_pdo.HandleError(new PDOException("The className for FETCH_CLASS mode is not set."));
                        //        return PhpValue.False;
                        //    }

                        case PDO_FETCH.FETCH_NAMED:
                            return this.ReadNamed();

                        //case PDO_FETCH.FETCH_LAZY:
                        //    return new PDORow( ... ) reads columns lazily

                        //case PDO_FETCH.FETCH_INTO:

                        //case PDO_FETCH.FETCH_FUNC:

                        //case PDO_FETCH.FETCH_KEY_PAIR:

                        default:
                            throw new NotImplementedException($"fetch {style}");
                    }
                }
                catch (System.Exception ex)
                {
                    this.m_pdo.HandleError(ex);
                    return PhpValue.False;
                }
            }
        }

        private PhpValue ReadObj()
        {
            return PhpValue.FromClass(this.ReadArray(true, false).ToObject());
        }

        private PhpValue ReadColumn(int column)
        {
            return this.m_dr.IsDBNull(column) ? PhpValue.Null : PhpValue.FromClr(this.m_dr.GetValue(column));
        }

        private PhpArray ReadArray(bool assoc, bool num, int from = 0)
        {
            var arr = new PhpArray(m_dr.FieldCount);

            for (int i = from; i < this.m_dr.FieldCount; i++)
            {
                var value = ReadColumn(i);

                if (assoc) arr.Add(this.m_dr_names[i], value);
                if (num) arr.Add(i, value);
            }
            return arr;
        }

        private PhpArray ReadNamed(int from = 0)
        {
            var arr = new PhpArray(m_dr.FieldCount);

            for (int i = from; i < this.m_dr.FieldCount; i++)
            {
                var key = new IntStringKey(this.m_dr_names[i]);
                var value = ReadColumn(i);
                ref var bucket = ref arr.GetItemRef(key);

                if (bucket.IsSet)
                {
                    var nested = bucket.AsArray();
                    if (nested != null)
                    {
                        nested.Add(value);
                    }
                    else
                    {
                        bucket = new PhpArray(2) { bucket, value };
                    }
                }
                else
                {
                    bucket = value;
                }
            }
            return arr;
        }

        /// <summary>
        /// Controls the contents of the returned array as documented in PDOStatement::fetch()
        /// </summary>
        /// <param name="fetch_style">The fetch style.</param>
        /// <param name="fetch_argument">This argument has a different meaning depending on the value of the fetch_style parameter.</param>
        /// <param name="ctor_args">Arguments of custom class constructor when the fetch_style parameter is PDO::FETCH_CLASS.</param>
        /// <returns>An array containing all of the remaining rows in the result set. <c>FALSE</c> on failure.</returns>
        [return: CastToFalse]
        public PhpArray fetchAll(PDO.PDO_FETCH fetch_style = default, PhpValue fetch_argument = default, PhpArray ctor_args = null)
        {
            // check parameters

            if (fetch_style == PDO.PDO_FETCH.FETCH_COLUMN)
            {
                if (!fetch_argument.IsDefault && fetch_argument.IsLong(out var l))
                {
                    FetchColNo = (int)l;
                }
                else
                {
                    m_pdo.HandleError(new PDOException("The fetch_argument must be an integer for FETCH_COLUMN."));
                    return null;
                }
            }

            if (!CheckDataReader())
            {
                // nothing to read from,
                return null;
            }

            if (m_dr_names == null)
            {
                // Get the column schema, if possible,
                // for the associative fetch
                InitializeColumnNames();
            }

            var style = fetch_style != PDO_FETCH.Default ? fetch_style : m_fetchStyle;
            var flags = style & PDO_FETCH.Flags;

            var result = new PhpArray();

            switch (style)
            {
                case PDO_FETCH.FETCH_KEY_PAIR:

                    Debug.Assert(m_dr.FieldCount == 2);
                    while (m_dr.Read())
                    {
                        // 1st col => 2nd col
                        result.Add(ReadColumn(0).ToIntStringKey(), ReadColumn(1));
                    }
                    break;

                case PDO_FETCH.FETCH_UNIQUE:

                    Debug.Assert(m_dr.FieldCount >= 1);
                    while (m_dr.Read())
                    {
                        // 1st col => [ 2nd col, 3rd col, ... ]
                        result.Add(ReadColumn(0).ToIntStringKey(), ReadArray(true, false, from: 1));
                    }
                    break;

                default:

                    for (; ; )
                    {
                        var value = fetch(style);
                        if (value.IsFalse)
                            break;

                        result.Add(value);
                    }

                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns a single column from the next row of a result set.
        /// </summary>
        /// <param name="column_number">0-indexed number of the column you wish to retrieve from the row. If no value is supplied, PDOStatement::fetchColumn() fetches the first column</param>
        /// <returns>Single column from the next row of a result set or FALSE if there are no more rows</returns>
        public PhpValue fetchColumn(int column_number = 0)
        {
            if (!CheckDataReader())
            {
                return PhpValue.False;
            }

            // read next row
            if (!this.m_dr.Read())
            {
                return PhpValue.False;
            }

            return this.ReadArray(false, true)[column_number].GetValue();
        }

        /// <summary>
        /// Fetches the next row and returns it as an object.
        /// </summary>
        /// <param name="class_name">Name of the created class.</param>
        /// <param name="ctor_args">Elements of this array are passed to the constructor.</param>
        /// <returns>Returns an instance of the required class with property names that correspond to the column names or FALSE on failure</returns>
        [return: CastToFalse]
        public object fetchObject(string class_name = nameof(stdClass), PhpArray ctor_args = null)
        {
            if (!CheckDataReader())
            {
                return null;
            }

            // read next row
            if (!this.m_dr.Read())
            {
                return PhpValue.False;
            }

            if (string.IsNullOrEmpty(class_name) || class_name == nameof(stdClass))
            {
                return this.ReadObj();
            }
            else
            {
                var args = ctor_args != null ? ctor_args.GetValues() : Array.Empty<PhpValue>();
                var obj = _ctx.Create(class_name, args);
                // TODO: set properties
                throw new NotImplementedException();
                //return obj;
            }
        }

        /// <summary>
        /// Retrieve a statement attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns>Returns the attribute value</returns>
        public PhpValue getAttribute(int attribute)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns metadata for a column in a result set.
        /// </summary>
        /// <param name="column">The 0-indexed column in the result set.</param>
        /// <returns>Returns an associative array containing the values representing the metadata for a single column</returns>
        [return: CastToFalse]
        public PhpArray getColumnMeta(int column)
        {
            if (!CheckDataReader())
            {
                return null;
            }

            if (column < 0 || column >= m_dr.FieldCount)
            {
                // TODO: invalid arg warning
                return null;
            }

            // If the column names are not initialized, then initialize them
            if (m_dr_names == null)
            {
                InitializeColumnNames();
            }

            PhpArray meta = new PhpArray();
            meta.Add("native_type", m_dr.GetFieldType(column)?.FullName);
            meta.Add("driver:decl_type", m_dr.GetDataTypeName(column));
            //meta.Add("flags", PhpValue.Null);
            meta.Add("name", m_dr_names[column]);
            //meta.Add("table", PhpValue.Null);
            //meta.Add("len", PhpValue.Null);
            //meta.Add("prevision", PhpValue.Null);
            //meta.Add("pdo_type", (int)PDO.PARAM.PARAM_NULL);
            return meta;
        }

        /// <summary>
        /// Advances to the next rowset in a multi-rowset statement handle.
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public bool nextRowset()
        {
            if (CheckDataReader() && this.m_dr.NextResult())
            {
                InitializeColumnNames();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the number of rows affected by the last SQL statement
        /// </summary>
        /// <returns></returns>
        public int rowCount()
        {
            if (m_cmd == null)
            {
                m_pdo.HandleError(new PDOException("Command cannot be null."));
                return -1;
            }
            if (m_pdo == null)
            {
                m_pdo.HandleError(new PDOException("The associated PDO object cannot be null."));
                return -1;
            }

            if (m_pdo.Driver.Name == "sqlite")
            {
                // This method returns "0" (zero) with the SQLite driver at all times
                return 0;
            }

            var statement = m_pdo.query("SELECT ROW_COUNT()");
            var rowCount = statement.fetchColumn(0);

            if (rowCount.IsInteger())
            {
                return (int)rowCount;
            }
            else
            {
                m_pdo.HandleError(new PDOException("The rowCount returned by the database is not a integer."));
                return -1;
            }
        }

        /// <summary>
        /// Set a statement attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="value">The value.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public bool setAttribute(int attribute, PhpValue value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set the default fetch mode for this statement
        /// 
        /// </summary>
        /// <param name="mode">The fetch mode.</param>
        /// <param name="args">
        /// args[0] For FETCH_COLUMN : column number. For FETCH_CLASS : the class name. For FETCH_INTO, the object.
        /// args[1] For FETCH_CLASS : the constructor arguments.
        /// </param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public bool setFetchMode(PDO_FETCH mode, params PhpValue[] args) => setFetchMode(mode, args.AsSpan());

        internal bool setFetchMode(PDO_FETCH mode, ReadOnlySpan<PhpValue> args)
        {
            switch (mode & ~PDO_FETCH.Flags)
            {
                case PDO_FETCH.Default:
                case PDO_FETCH.FETCH_LAZY:
                case PDO_FETCH.FETCH_ASSOC:
                case PDO_FETCH.FETCH_NUM:
                case PDO_FETCH.FETCH_BOTH:
                case PDO_FETCH.FETCH_OBJ:
                case PDO_FETCH.FETCH_BOUND:
                case PDO_FETCH.FETCH_NAMED:
                case PDO_FETCH.FETCH_KEY_PAIR:
                    if (args.Length != 0)
                    {
                        RaiseError("fetch mode doesn't allow any extra arguments");
                        return false;
                    }
                    else
                    {
                        m_fetchStyle = mode;
                        return true;
                    }

                case PDO_FETCH.FETCH_COLUMN:
                    if (args.Length != 1)
                    {
                        RaiseError("fetch mode requires the colno argument");
                        return false;
                    }
                    else if (args[0].IsLong(out var l))
                    {
                        m_fetchStyle = mode;
                        FetchColNo = (int)l;
                        return true;
                    }
                    else
                    {
                        RaiseError("colno must be an integer");
                        return false;
                    }

                case PDO_FETCH.FETCH_CLASS:

                    FetchClassCtorArgs = default;

                    if ((mode & PDO_FETCH.FETCH_CLASSTYPE) != 0)
                    {
                        // will be getting its class name from 1st column
                        if (args.Length != 0)
                        {
                            RaiseError("fetch mode doesn't allow any extra arguments");
                            return false;
                        }
                        else
                        {
                            m_fetchStyle = mode;
                            FetchClassName = null;
                            return true;
                        }
                    }
                    else
                    {
                        if (args.Length == 0)
                        {
                            RaiseError("fetch mode requires the classname argument");
                            return false;
                        }
                        else if (args.Length > 2)
                        {
                            RaiseError("too many arguments");
                            return false;
                        }
                        else if (args[0].IsString(out var name))
                        {
                            FetchClassName = _ctx.GetDeclaredTypeOrThrow(name, autoload: true);
                            if (FetchClassName != null)
                            {
                                if (args.Length > 1)
                                {
                                    var ctorargs = args[1].AsArray();
                                    if (ctorargs == null && !args[1].IsNull)
                                    {
                                        RaiseError("ctor_args must be either NULL or an array");
                                        return false;
                                    }
                                    else if (ctorargs != null && ctorargs.Count != 0)
                                    {
                                        FetchClassCtorArgs = ctorargs.GetValues(); // TODO: deep copy
                                    }
                                }

                                m_fetchStyle = mode;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            RaiseError("classname must be a string");
                            return false;
                        }
                    }

                case PDO_FETCH.FETCH_INTO:
                    if (args.Length != 1)
                    {
                        RaiseError("fetch mode requires the object parameter");
                        return false;
                    }
                    else
                    {
                        FetchClassInto = args[0].AsObject();
                        if (FetchClassInto == null)
                        {
                            RaiseError("object must be an object");
                            return false;
                        }

                        m_fetchStyle = mode;
                        return true;
                    }

                default:
                    RaiseError("Invalid fetch mode specified");
                    return false;
            }
        }

        // Initializes the column names by looping through the data reads columns or using the schema if it is available
        private void InitializeColumnNames()
        {
            m_dr_names = new string[m_dr.FieldCount];

            if (m_dr.CanGetColumnSchema())
            {
                var columnSchema = m_dr.GetColumnSchema();

                for (var i = 0; i < m_dr_names.Length; i++)
                {
                    m_dr_names[i] = columnSchema[i].ColumnName;
                }
            }
            else
            {
                for (var i = 0; i < m_dr_names.Length; i++)
                {
                    m_dr_names[i] = m_dr.GetName(i);
                }
            }
        }
    }
}
