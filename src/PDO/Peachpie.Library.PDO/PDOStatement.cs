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

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDOStatement class
    /// </summary>
    /// <seealso cref="IPDOStatement" />
    [PhpType(PhpTypeAttribute.InheritName)]
    public class PDOStatement : IPDOStatement, IDisposable
    {
        /// <summary>Runtime context. Cannot be <c>null</c>.</summary>
        protected readonly Context _ctx; // "_ctx" is a special name recognized by compiler. Will be reused by inherited classes.

        private readonly PDO m_pdo;
        private readonly string m_stmt;
        private readonly PhpArray m_options;

        private readonly DbCommand m_cmd;
        private DbDataReader m_dr;
        private readonly Dictionary<PDO.PDO_ATTR, PhpValue> m_attributes = new Dictionary<PDO.PDO_ATTR, PhpValue>();
        private string[] m_dr_names;

        private bool m_positionalAttr = false;
        private bool m_namedAttr = false;
        private Dictionary<string, string> m_namedPlaceholders;
        private List<String> m_positionalPlaceholders;

        private Dictionary<string, PhpAlias> m_boundParams;      
        private PDO.PDO_FETCH m_fetchStyle = PDO.PDO_FETCH.FETCH_BOTH;

        /// <summary>
        /// Property telling whether there are any command parameter variables bound with bindParam.
        /// </summary>
        bool HasParamsBound => (m_boundParams != null && m_boundParams.Count > 0);
        /// <summary>
        /// Column Number property for FETCH_COLUMN fetching.
        /// </summary>
        int FetchColNo { get; set; } = -1;
        /// <summary>
        /// Class Name property for FETCH_CLASS fetching mode.
        /// </summary>
        string FetchClassName { get; set; } = null;
        /// <summary>
        /// Class Constructor Args for FETCH_CLASS fetching mode.
        /// </summary>
        PhpArray FetchClassCtorArgs { get; set; } = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PDOStatement" /> class.
        /// </summary>
        /// <param name="ctx">The php context.</param>
        /// <param name="pdo">The PDO statement is created for.</param>
        /// <param name="statement">The statement.</param>
        /// <param name="driver_options">The driver options.</param>
        internal PDOStatement(Context ctx, PDO pdo, string statement, PhpArray driver_options)
        {
            this.m_pdo = pdo;
            this._ctx = ctx;
            this.m_stmt = statement;
            this.m_options = driver_options ?? PhpArray.Empty;

            this.m_cmd = pdo.CreateCommand(this.m_stmt);

            PrepareStatement();

            this.SetDefaultAttributes();
        }

        /// <summary>
        /// Empty ctor.
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

        private static readonly Regex regName = new Regex(@"[\w_]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
            while(pos < m_stmt.Length)
            {
                char currentChar = m_stmt[pos];
                string paramName = "";

                switch (currentChar)
                {
                    case '?':
                        if(m_namedAttr)
                        {
                            throw new PDOException("Mixing positional and named parameters not allowed. Use only '?' or ':name' pattern");
                        }

                        m_positionalAttr = true;

                        paramName = "@p" + m_positionalPlaceholders.Count();
                        m_positionalPlaceholders.Add(paramName);
                        rewrittenQuery.Append(paramName);

                        break;

                    case ':':
                        if(m_positionalAttr)
                        {
                            throw new PDOException("Mixing positional and named parameters not allowed.Use only '?' or ':name' pattern");
                        }

                        m_namedAttr = true;

                        var match= regName.Match(m_stmt, pos);
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

            if(m_positionalAttr)
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
            } else if(m_namedAttr)
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
            while(++pos < query.Length)
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

        private void SetDefaultAttributes()
        {
            this.m_attributes[PDO.PDO_ATTR.ATTR_CURSOR] = (PhpValue)(int)PDO.PDO_CURSOR.CURSOR_FWDONLY;
        }

        /// <inheritDoc />
        void IDisposable.Dispose()
        {
            this.m_dr?.Dispose();
            this.m_cmd.Dispose();
        }

        private void OpenReader()
        {
            if (this.m_dr == null)
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
                this.m_dr_names = new string[this.m_dr.FieldCount];
                for (int i = 0; i < this.m_dr_names.Length; i++)
                {
                    this.m_dr_names[i] = this.m_dr.GetName(i);
                }
            }
        }

        /// <inheritDoc />
        public bool bindColumn(PhpValue colum, ref PhpValue param, int? type = default(int?), int? maxlen = default(int?), PhpValue? driverdata = default(PhpValue?))
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public bool bindParam(PhpValue parameter, PhpAlias variable, PDO.PARAM data_type = PDO.PARAM.PARAM_STR, int length = -1, PhpValue driver_options = default(PhpValue))
        {
            Debug.Assert(this.m_cmd != null);

            // lazy instantization
            if (m_boundParams == null)
                m_boundParams = new Dictionary<string, PhpAlias>();

            IDbDataParameter param = null;

            if (m_namedAttr)
            {
                // Mixed parameters not allowed
                if (m_positionalAttr)
                {
                    m_pdo.HandleError(new PDOException("Mixed parameters mode not allowed. Use either only positional, or only named parameters."));
                    return false;
                }

                string key = parameter.AsString();
                if (key == null)
                {
                    m_pdo.HandleError(new PDOException("Supplied parameter name must be a string."));
                    return false;
                }

                if (key.Length > 0 && key[0] == ':')
                {
                    key = key.Substring(1);
                }

                //Store the bounded variable reference in the dictionary
                m_boundParams.Add(key, new PhpAlias(variable.Value));

                param = m_cmd.Parameters[key];

            }
            else if (m_positionalAttr)
            {
                if (!parameter.IsInteger())
                {
                    m_pdo.HandleError(new PDOException("Supplied parameter index must be an integer."));
                    return false;
                }
                int paramIndex = (int)parameter;

                //Store the bounded variable.Value reference in the dictionary
                m_boundParams.Add(paramIndex.ToString(), new PhpAlias(variable.Value));

                if (paramIndex < m_positionalPlaceholders.Count)
                {
                    param = m_cmd.Parameters[paramIndex];
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
                    string str = null;
                    if ((str = variable.Value.ToStringOrNull()) != null)
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
                    byte[] bytes = null;
                    if ((bytes = variable.Value.ToBytesOrNull()) != null)
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

        /// <inheritDoc />
        public bool bindValue(PhpValue parameter, PhpValue value, PDO.PARAM data_type = (PDO.PARAM)PDO.PARAM_STR)
        {
            Debug.Assert(this.m_cmd != null);

            IDbDataParameter param = null;

            if (m_namedAttr)
            {
                // Mixed parameters not allowed
                if (m_positionalAttr)
                {
                    m_pdo.HandleError(new PDOException("Mixed parameters mode not allowed. Use either only positional, or only named parameters."));
                    return false;
                }

                string key = parameter.AsString();
                if(key == null)
                {
                    m_pdo.HandleError(new PDOException("Supplied parameter name must be a string."));
                    return false;
                }

                if (key.Length > 0 && key[0] == ':')
                {
                    key = key.Substring(1);
                }

                param = m_cmd.Parameters[key];

                //rewrite the bounded params dictionary
                if(HasParamsBound)
                    if (m_boundParams.ContainsKey(key))
                        m_boundParams.Remove(key);

            } else if(m_positionalAttr)
            {
                if (!parameter.IsInteger())
                {
                    m_pdo.HandleError(new PDOException("Supplied parameter index must be an integer."));
                    return false;
                }
                int paramIndex = (int)parameter;

                if(paramIndex < m_positionalPlaceholders.Count)
                {
                    param = m_cmd.Parameters[paramIndex];
                }

                //rewrite the bounded params dictionary
                if(HasParamsBound)
                    if (m_boundParams.ContainsKey(paramIndex.ToString()))
                        m_boundParams.Remove(paramIndex.ToString());

            } else
            {
                m_pdo.HandleError(new PDOException("No parameter mode set yet for this Statement. Possibly no parameters required?"));
                return false;
            }

            if(param == null)
            {
                m_pdo.HandleError(new PDOException("No matching parameter found."));
                return false;
            }

            switch(data_type)
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
                    } else
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

        /// <inheritDoc />
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

        /// <inheritDoc />
        public int columnCount()
        {
            if (this.m_dr == null)
            {
                return 0;
            }

            return this.m_dr.FieldCount;
        }

        /// <inheritDoc />
        public void debugDumpParams()
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public string errorCode()
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public PhpArray errorInfo()
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public bool execute(PhpArray input_parameters = null)
        {
            // Assign the bound variables from bindParam() function if any present
            if(HasParamsBound)
            {
                foreach(KeyValuePair<string, PhpAlias> pair in m_boundParams)
                {
                    IDbDataParameter param = null;

                    if(m_namedAttr)
                    {
                        // Mixed parameters not allowed
                        if (m_positionalAttr)
                        {
                            m_pdo.HandleError(new PDOException("Mixed parameters mode not allowed. Use either only positional, or only named parameters."));
                            return false;
                        }

                        param = m_cmd.Parameters[pair.Key];
                    } else if(m_positionalAttr)
                    {
                        int index = -1;
                        bool result = Int32.TryParse(pair.Key, out index);

                        if(result)
                        {
                            param = m_cmd.Parameters[index];
                        } else
                        {
                            m_pdo.HandleError(new PDOException("The string for positional parameter index must be an integer."));
                            return false;
                        }

                    } else
                    {
                        m_pdo.HandleError(new PDOException("No parameter mode set yet for this Statement. Possibly no parameters required?"));
                        return false;
                    }

                    switch (param.DbType)
                    {
                        case DbType.Int32:
                            if (pair.Value.Value.IsInteger())
                            {
                                param.Value = (int)pair.Value.Value;
                            }
                            else
                            {
                                m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                                return false;
                            }
                            break;

                        case DbType.String:
                            string str = null;
                            if ((str = pair.Value.Value.ToStringOrNull()) != null)
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
                            if (pair.Value.Value.IsBoolean())
                            {
                                param.Value = pair.Value.ToBoolean();
                            }
                            else
                            {
                                m_pdo.HandleError(new PDOException("Parameter type does not match the declared type."));
                                return false;
                            }
                            break;

                        case DbType.Binary:
                            byte[] bytes = null;
                            if ((bytes = pair.Value.Value.ToBytesOrNull()) != null)
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

            m_dr = null;
            try
            {
                m_dr = m_cmd.ExecuteReader();
            } catch(Exception e)
            {
                m_pdo.HandleError(new PDOException("Query could not be executed; \n" + e.Message));
                return false;
            }           

            return true;
        }

        /// <inheritDoc />
        public PhpValue fetch(PDO.PDO_FETCH fetch_style = PDO.PDO_FETCH.FETCH_USE_DEFAULT ,int cursor_orientation = default(int), int cursor_offet = 0)
        {
            this.m_pdo.ClearError();
            try
            {
                PDO.PDO_FETCH style = this.m_fetchStyle;

                if ((int)fetch_style != -1 && Enum.IsDefined(typeof(PDO.PDO_FETCH), fetch_style))
                {
                    style = (PDO.PDO_FETCH)fetch_style;
                }
                PDO.PDO_FETCH_ORI ori = PDO.PDO_FETCH_ORI.FETCH_ORI_NEXT;
                if (Enum.IsDefined(typeof(PDO.PDO_FETCH_ORI), cursor_orientation))
                {
                    ori = (PDO.PDO_FETCH_ORI)cursor_orientation;
                }

                switch (ori)
                {
                    case PDO.PDO_FETCH_ORI.FETCH_ORI_NEXT:
                        break;
                    default:
                        throw new NotSupportedException();
                }

                if (!this.m_dr.Read())
                    return PhpValue.False;

                // Get the column schema, if possible, for the associative fetch
                if(this.m_dr_names == null)
                {
                    this.m_dr_names = new string[m_dr.FieldCount];

                    if (this.m_dr.CanGetColumnSchema()) {
                        var columnSchema = this.m_dr.GetColumnSchema();

                        for (int i = 0; i < m_dr.FieldCount; i++)
                        {
                            this.m_dr_names[i] = columnSchema[i].ColumnName;
                        }
                    }
                }

                switch (style)
                {
                    case PDO.PDO_FETCH.FETCH_OBJ:
                        return this.ReadObj();
                    case PDO.PDO_FETCH.FETCH_ASSOC:
                        return PhpValue.Create(this.ReadArray(true, false));
                    case PDO.PDO_FETCH.FETCH_BOTH:
                    case PDO.PDO_FETCH.FETCH_USE_DEFAULT:
                        return PhpValue.Create(this.ReadArray(true, true));
                    case PDO.PDO_FETCH.FETCH_NUM:
                        return PhpValue.Create(this.ReadArray(false, true));
                    case PDO.PDO_FETCH.FETCH_COLUMN:
                        if (FetchColNo != -1)
                        {
                            m_pdo.HandleError(new PDOException("The column number for FETCH_COLUMN mode is not set."));
                            return PhpValue.False;
                        }

                        return this.ReadArray(false, true)[FetchColNo].GetValue();
                    case PDO.PDO_FETCH.FETCH_CLASS:
                        if(FetchClassName == null)
                        {
                            m_pdo.HandleError(new PDOException("The className for FETCH_CLASS mode is not set."));
                            return PhpValue.False;
                        }

                        dynamic obj = null;
                        if(FetchClassCtorArgs == null)
                        {
                            obj = _ctx.Create(FetchClassName);
                        } else
                        {
                            _ctx.Create(FetchClassName, FetchClassCtorArgs);
                        }

                        return PhpValue.FromClass(obj);
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (System.Exception ex)
            {
                this.m_pdo.HandleError(ex);
                return PhpValue.False;
            }
        }

        private PhpValue ReadObj()
        {
            return PhpValue.FromClass(this.ReadArray(true, false).ToClass());
        }


        private PhpArray ReadArray(bool assoc, bool num)
        {
            PhpArray arr = new PhpArray();
            for (int i = 0; i < this.m_dr.FieldCount; i++)
            {
                if (this.m_dr.IsDBNull(i))
                {
                    if (assoc)
                        arr.Add(this.m_dr_names[i], PhpValue.Null);
                    if (num)
                        arr.Add(i, PhpValue.Null);
                }
                else
                {
                    var value = PhpValue.FromClr(this.m_dr.GetValue(i));
                    if (assoc)
                        arr.Add(this.m_dr_names[i], value);
                    if (num)
                        arr.Add(i, value);
                }
            }
            return arr;
        }

        /// <inheritDoc />
        public PhpArray fetchAll(PDO.PDO_FETCH fetch_style = FETCH_USE_DEFAULT, PhpValue fetch_argument = default(PhpValue), PhpArray ctor_args = null)
        {
            if (m_dr == null)
            {
                m_pdo.HandleError(new PDOException("The data reader can not be null."));
                return null;
            }

            if(fetch_style == PDO.PDO_FETCH.FETCH_COLUMN)
            {
                if(fetch_argument.IsInteger())
                {
                    FetchColNo = (int)fetch_argument;
                } else
                {
                    m_pdo.HandleError(new PDOException("The fetch_argument must be an integer for FETCH_COLUMN."));
                    return null;
                }
            }

            PhpArray returnArray = new PhpArray();

            while(m_dr.HasRows)
            {
                var value = fetch(fetch_style);

                returnArray.Add(value);
            }

            return returnArray;
        }

        /// <inheritDoc />
        public PhpValue fetchColumn(int column_number = 0)
        {
            if(m_dr == null)
            {
                m_pdo.HandleError(new PDOException("The data reader can not be null."));
                return PhpValue.False;
            }

            return this.ReadArray(false, true)[column_number].GetValue();
        }

        /// <inheritDoc />cp
        public PhpValue fetchObject(string class_name = "stdClass", PhpArray ctor_args = null)
        {
            if (m_dr == null)
            {
                m_pdo.HandleError(new PDOException("The data reader can not be null."));
                return PhpValue.False;
            }

            if(class_name == "stdClass")
            {
                return this.ReadObj();
            } else
            {
                dynamic obj = _ctx.Create(class_name, ctor_args);
                return PhpValue.FromClass(obj);
            }
        }

        /// <inheritDoc />
        public PhpValue getAttribute(int attribute)
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        [return: CastToFalse]
        public PhpArray getColumnMeta(int column)
        {
            if (this.m_dr == null)
            {
                return null;
            }

            if (column < 0 || column >= this.m_dr.FieldCount)
                return null;

            PhpArray meta = new PhpArray();
            meta.Add("native_type", this.m_dr.GetFieldType(column).FullName);
            meta.Add("driver:decl_type", this.m_dr.GetDataTypeName(column));
            //meta.Add("flags", PhpValue.Null);
            meta.Add("name", this.m_dr_names[column]);
            //meta.Add("table", PhpValue.Null);
            //meta.Add("len", PhpValue.Null);
            //meta.Add("prevision", PhpValue.Null);
            //meta.Add("pdo_type", (int)PDO.PARAM.PARAM_NULL);
            return meta;
        }

        /// <inheritDoc />
        public bool nextRowset()
        {
            if (this.m_dr == null)
            {
                return false;
            }
            if (this.m_dr.NextResult())
            {
                this.m_dr_names = new string[this.m_dr.FieldCount];
                for (int i = 0; i < this.m_dr_names.Length; i++)
                {
                    this.m_dr_names[i] = this.m_dr.GetName(i);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritDoc />
        public int rowCount()
        {
            if(m_cmd == null)
            {
                m_pdo.HandleError(new PDOException("Command cannot be null."));
                return -1;
            }
            if(m_pdo == null)
            {
                m_pdo.HandleError(new PDOException("The associated PDO object cannot be null."));
                return -1;
            }

            var statement = m_pdo.query("SELECT ROW_COUNT()");
            var rowCount = statement.fetchColumn(0);
            
            if(rowCount.IsInteger())
            {
                return (int)rowCount;
            } else
            {
                m_pdo.HandleError(new PDOException("The rowCount returned by the database is not a integer."));
                return -1;
            }
        }

        /// <inheritDoc />
        public bool setAttribute(int attribute, PhpValue value)
        {
            throw new NotImplementedException();
        }

        /// <inheritDoc />
        public bool setFetchMode(params PhpValue[] args)
        {
            PDO_FETCH fetch = PDO_FETCH.FETCH_USE_DEFAULT;

            PhpValue fetchMode = args[0];
            if (fetchMode.IsInteger())
            {
                int value = (int)fetchMode.Long;
                if (Enum.IsDefined(typeof(PDO_FETCH), value))
                {
                    fetch = (PDO_FETCH)value;

                    this.m_fetchStyle = fetch;
                }
                else
                {
                    m_pdo.HandleError(new PDOException("Given PDO_FETCH constant is not implemented."));
                }
            }

            if (fetch == PDO_FETCH.FETCH_COLUMN)
            {
                int colNo = -1;
                if (args.Length > 1)
                {
                    if (args[1].IsInteger())
                    {
                        colNo = (int)args[1].ToLong();
                        this.FetchColNo = colNo;
                    }
                    else
                    {
                        m_pdo.HandleError(new PDOException("General error: colno must be an integer"));
                    }
                }
                else
                {
                    m_pdo.HandleError(new PDOException("General error: fetch mode requires the colno argument"));

                    //TODO what to do if missing parameter ?
                    //fetch = PDO_FETCH.FETCH_USE_DEFAULT;
                }
            }
            string className = null;

            if (fetch == PDO_FETCH.FETCH_CLASS)
            {
                if (args.Length > 2)
                {
                    className = args[1].ToStringOrNull();
                    this.FetchClassName = className;

                    if (args.Length > 3)
                    {
                        this.FetchClassCtorArgs = args[2].ArrayOrNull();
                    }
                }
                else
                {
                    throw new PDOException("General error: fetch mode requires the classname argument.");
                }
            }

            return true;
        }
    }
}
