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
using Pchp.Library.Database;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDOStatement class
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(PDOConfiguration.PdoExtensionName)]
    public class PDOStatement : Traversable
    {
        private protected struct BoundParam
        {
            public PhpValue Variable;

            public PARAM? Type;

            public void SetValue(object value)
            {
                var x = PhpValue.FromClr(value);

                //
                if (Variable.IsAlias)
                {
                    Variable.Alias.Value = Type.HasValue ? ConvertParam(x, Type.Value) : x;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            public void InitializeParameter(DbParameter p)
            {
                p.Direction = (Type.HasValue && (Type.Value & PARAM.PARAM_INPUT_OUTPUT) != 0)
                    ? ParameterDirection.InputOutput
                    : ParameterDirection.Input;

                if (Variable.IsNull)
                {
                    p.Value = DBNull.Value;
                    // TODO: p.DbType
                }
                else if (Type.HasValue)
                {
                    switch (Type.Value & ~PARAM.PARAM_INPUT_OUTPUT)
                    {
                        case PARAM.PARAM_BOOL:
                            p.Value = Variable.ToBoolean(); // TODO: boxed value
                            p.DbType = DbType.Boolean;
                            break;

                        case PARAM.PARAM_INT:
                            p.Value = Variable.ToLong();
                            p.DbType = DbType.Int64;
                            break;

                        case PARAM.PARAM_STR:
                            if (Variable.IsUnicodeString(out var str))
                            {
                                p.Value = str;
                                p.DbType = DbType.String;
                            }
                            else if (Variable.IsBinaryString(out var bin))
                            {
                                p.Value = bin.ToBytes(Encoding.UTF8);
                                p.DbType = DbType.Binary;
                            }
                            else
                            {
                                p.Value = Variable.ToString();
                                p.DbType = DbType.String;
                            }
                            break;

                        case PARAM.PARAM_NULL:
                            p.Value = DBNull.Value;
                            // TODO: p.DbType
                            break;

                        case PARAM.PARAM_ZVAL:
                            p.Value = Variable.ToClr();
                            // TODO: p.DbType
                            break;

                        default:
                            throw new NotImplementedException(Type.Value.ToString());
                    }
                }
                else
                {
                    p.Value = Variable.ToClr();
                    // TODO: p.DbType
                }
            }
        }

        private protected Context Context => Connection.Context;

        private protected PDO PDO => Connection.PDO;

        private protected PDODriver Driver => PDO.Driver;

        /// <summary>PDO connection owning this statement.</summary>
        private protected PdoConnectionResource Connection { get; private set; }

        /// <summary>Prepared command.</summary>
        private protected DbCommand _cmd;

        // TODO: use our "StatementResource"

        /// <summary>
        /// When rewriting from named to positional or to a different parameter name notation,
        /// this maps user-provided names to actual names.
        /// </summary>
        private protected Dictionary<IntStringKey, IntStringKey> bound_param_map;

        /// <summary>Keeps track of bound parameters.
        /// Positional keys are 0-based.</summary>
        private protected Dictionary<IntStringKey, BoundParam> bound_params;

        /// <summary>Keeps track of PHP variables bound to named (or positional) columns in the result set.
        /// Positional keys are 0-based.</summary>
        private protected Dictionary<IntStringKey, BoundParam> bound_columns;

        private protected PDO_FETCH _default_fetch_type;
        private protected int _fetch_column = -1;
        private protected PhpTypeInfo _default_fetch_class;
        private protected PhpValue[] _default_fetch_class_args;

        /// <summary>
        /// Result of the query.
        /// Set after calling to <see cref="execute"/>.
        /// </summary>
        private protected ResultResource Result { get; private set; }

        #region Error Handling

        /// <summary>
        /// Error information associated with the last operation on the statement.
        /// </summary>
        private ErrorInfo _lastError;

        private protected void ClearError()
        {
            _lastError = default;
            PDO.ClearError();
        }

        /// <summary>
        /// Raises simple "HY000" error.
        /// </summary>
        private protected void HandleError(string message) => HandleError(ErrorInfo.Create(message));

        private protected void HandleError(Exception exception)
        {
            Driver.HandleException(exception, out var error);
            HandleError(error);
        }

        private protected void HandleError(ErrorInfo error)
        {
            _lastError = error;
            PDO.HandleError(error);
        }

        #endregion

        #region Construction

        /// <summary>
        /// Default constructor that does not prepare the statement.
        /// </summary>
        [PhpFieldsOnlyCtor]
        protected PDOStatement()
        {
        }

        internal PDOStatement(PdoConnectionResource pdo, string queryString, PhpArray options = null)
        {
            Prepare(pdo, queryString, options);
        }

        /// <summary>
        /// 2 phase ctor.
        /// Prepares the command.
        /// </summary>
        /// <param name="pdo"></param>
        /// <param name="queryString"></param>
        /// <param name="options">Driver options. Optional.</param>
        internal void Prepare(PdoConnectionResource pdo, string queryString, PhpArray options)
        {
            pdo.ClosePendingReader();

            // initialize properties
            this.Connection = pdo ?? throw new ArgumentNullException(nameof(pdo));
            this.queryString = queryString;

            //
            var actualQuery = Driver.RewriteCommand(this, options, out bound_param_map);
            _cmd = PDO.CreateCommand(actualQuery);

            //_cmd.Prepare(); // <-- compiles the query, needs parameters to be bound
        }

        #endregion

        #region Properties

        /// <summary>Used query string.</summary>
        public string queryString { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Bind a column to a PHP variable.
        /// </summary>
        /// <param name="column">
        /// Number of the column (1-indexed) or name of the column in the result set.
        /// If using the column name, be aware that the name should match the case of the column, as returned by the driver</param>
        /// <param name="param">PHP variable to which the column will be bound.</param>
        /// <param name="type">Data type of the parameter, specified by the PDO::PARAM_* constants.</param>
        /// <param name="maxlen">A hint for pre-allocation.</param>
        /// <param name="driverdata">Optional parameter(s) for the driver.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public virtual bool bindColumn(IntStringKey column, PhpAlias param, PARAM? type = default, int maxlen = 0, PhpValue driverdata = default)
        {
            return StoreParameter(ref bound_columns, column, param, type);
        }

        /// <summary>
        /// Binds a parameter to the specified variable name
        /// </summary>
        /// <param name="parameter">Parameter identifier.
        /// For a prepared statement using named placeholders, this will be a parameter name of the form <c>:name</c>.
        /// For a prepared statement using question mark placeholders, this will be the 1-indexed position of the parameter.</param>
        /// <param name="variable">Name of the PHP variable to bind to the SQL statement parameter.</param>
        /// <param name="data_type">Explicit data type for the parameter using the PDO::PARAM_* constants. To return an INOUT parameter from a stored procedure, use the bitwise OR operator to set the PDO::PARAM_INPUT_OUTPUT bits for the data_type parameter.</param>
        /// <param name="length">Length of the data type. To indicate that a parameter is an OUT parameter from a stored procedure, you must explicitly set the length.</param>
        /// <param name="driver_options"></param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public virtual bool bindParam(IntStringKey parameter, PhpAlias variable, PARAM data_type = PARAM.PARAM_STR, int? length = default, PhpValue driver_options = default)
        {
            return StoreParameter(ref bound_params, parameter, variable, data_type);
        }

        /// <summary>
        /// Binds a value to a parameter.
        /// </summary>
        /// <param name="parameter">Parameter identifier.
        /// For a prepared statement using named placeholders, this will be a parameter name of the form <c>:name</c>.
        /// For a prepared statement using question mark placeholders, this will be the 1-indexed position of the parameter.</param>
        /// <param name="value">The value to bind to the parameter.</param>
        /// <param name="data_type">Explicit data type for the parameter using the PDO::PARAM_* constants.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public virtual bool bindValue(IntStringKey parameter, PhpValue value, PARAM data_type = PARAM.PARAM_STR)
        {
            return StoreParameter(ref bound_params, parameter, value.GetValue().DeepCopy(), data_type);
        }

        /// <summary>
        /// Closes the cursor, enabling the statement to be executed again.
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public virtual bool closeCursor()
        {
            Connection.ClosePendingReader();
            Result = null;
            return true;
        }

        /// <summary>
        /// Returns the number of columns in the result set
        /// </summary>
        /// <returns>
        /// Returns the number of columns in the result set represented by the <see cref="PDOStatement"/> object, even if the result set is empty.
        /// If there is no result set, <see cref="columnCount"/> returns <c>0</c>.
        /// </returns>
        public virtual int columnCount() => Result != null ? Result.FieldCount : 0;

        /// <summary>
        /// Dump an SQL prepared command.
        /// </summary>
        public virtual void debugDumpParams() { throw new NotImplementedException(); }

        /// <summary>
        /// Fetch the SQLSTATE associated with the last operation on the statement handle
        /// </summary>
        /// <returns>Identical to PDO::errorCode(), except that PDOStatement::errorCode() only retrieves error codes for operations performed with PDOStatement objects</returns>
        public virtual string errorCode() => _lastError.Code;

        /// <summary>
        /// Fetch extended error information associated with the last operation on the statement handle.
        /// </summary>
        /// <returns>PDOStatement::errorInfo() returns an array of error information about the last operation performed by this statement handle.</returns>
        public virtual PhpArray errorInfo() => _lastError.ToPhpErrorInfo();

        /// <summary>
        /// Executes a prepared statement
        /// </summary>
        /// <param name="input_parameters">An array of values with as many elements as there are bound parameters in the SQL statement being executed. All values are treated as PDO::PARAM_STR.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public virtual bool execute(PhpArray input_parameters = null)
        {
            if (Result != null)
            {
                // reusing command
                Connection.ClosePendingReader();
            }

            // parameters
            BindParameters(_cmd.Parameters, input_parameters);

            // execute
            Result = Connection.ExecuteCommand(_cmd, convertTypes: true, parameters: null /*already set*/, skipResults: false);

            // handle error:
            if (Connection.LastException == null)
            {
                // TODO: write-back output parameters from _cmd:
            }
            else
            {
                HandleError(Connection.LastException);
                return false;
            }

            return Result != null;
        }

        /// <summary>
        /// Fetches the specified fetch style.
        /// </summary>
        /// <param name="fetch_style">Controls how the next row will be returned to the caller. This value must be one of the PDO::FETCH_* constants.</param>
        /// <param name="cursor_orientation">This value determines which row will be returned to the caller.</param>
        /// <param name="cursor_offet">Relative or absolute position move for the cursor.</param>
        /// <returns>The return value of this function on success depends on the fetch type. In all cases, FALSE is returned on failure.</returns>
        public virtual PhpValue fetch(PDO.PDO_FETCH fetch_style = PDO_FETCH.Default/*0*/, PDO_FETCH_ORI cursor_orientation = PDO_FETCH_ORI.FETCH_ORI_NEXT/*0*/, int cursor_offet = 0)
        {
            ClearError();

            if (cursor_orientation != PDO_FETCH_ORI.FETCH_ORI_NEXT) // 0
            {
                throw new NotImplementedException(cursor_orientation.ToString());
            }

            if (Result == null)
            {
                // statement not executed
                PhpException.Throw(PhpError.Notice, "no results to fetch");
                return false;
            }

            try
            {
                var how = fetch_style != PDO_FETCH.Default ? fetch_style : _default_fetch_type;
                var flags = how & PDO_FETCH.Flags;

                switch (how & ~PDO_FETCH.Flags)
                {
                    case PDO_FETCH.Default:
                    case PDO_FETCH.FETCH_BOTH:
                        return Result.FetchArray(true, true) ?? PhpValue.False;

                    case PDO_FETCH.FETCH_ASSOC:
                        return Result.FetchAssocArray() ?? PhpValue.False;

                    case PDO_FETCH.FETCH_NUM:
                        return Result.FetchArray(true, false) ?? PhpValue.False;

                    case PDO_FETCH.FETCH_OBJ:
                        return ObjectOrFalse(Result.FetchStdClass());

                    case PDO_FETCH.FETCH_BOUND:
                        return FetchBound();

                    case PDO_FETCH.FETCH_COLUMN:
                        return fetchColumn(_fetch_column);

                    case PDO.PDO_FETCH.FETCH_CLASS:
                        return ObjectOrFalse(FetchClass(_default_fetch_class, _default_fetch_class_args));

                    case PDO_FETCH.FETCH_NAMED:
                        return this.ReadNamed();

                    //case PDO_FETCH.FETCH_LAZY:
                    //    return new PDORow( ... ) reads columns lazily

                    //case PDO_FETCH.FETCH_INTO:

                    //case PDO_FETCH.FETCH_FUNC:

                    //case PDO_FETCH.FETCH_KEY_PAIR:

                    default:
                        throw new NotImplementedException($"fetch {how}");
                }
            }
            catch (System.Exception ex)
            {
                HandleError(ex);
                return PhpValue.False;
            }
        }

        /// <summary>
        /// Controls the contents of the returned array as documented in PDOStatement::fetch()
        /// </summary>
        /// <param name="fetch_style">The fetch style.</param>
        /// <param name="fetch_argument">This argument has a different meaning depending on the value of the fetch_style parameter.</param>
        /// <param name="ctor_args">Arguments of custom class constructor when the fetch_style parameter is PDO::FETCH_CLASS.</param>
        /// <returns>An array containing all of the remaining rows in the result set. <c>FALSE</c> on failure.</returns>
        [return: CastToFalse]
        public virtual PhpArray fetchAll(PDO_FETCH fetch_style = default, PhpValue fetch_argument = default, PhpArray ctor_args = null)
        {
            // check parameters

            if (fetch_style == PDO_FETCH.FETCH_COLUMN)
            {
                if (fetch_argument.IsLong(out var l))
                {
                    _fetch_column = (int)l;
                }
                else
                {
                    HandleError("The fetch_argument must be an integer for FETCH_COLUMN.");
                    return null;
                }
            }

            var style = fetch_style != PDO_FETCH.Default ? fetch_style : _default_fetch_type;
            var flags = style & PDO_FETCH.Flags;

            var result = new PhpArray();

            switch (style)
            {
                case PDO_FETCH.FETCH_KEY_PAIR:

                    while (Result.TryReadRow(out var oa, out _))
                    {
                        // 1st col => 2nd col
                        result[PhpValue.FromClr(oa[0]).ToIntStringKey()] = PhpValue.FromClr(oa[1]);
                    }
                    break;

                case PDO_FETCH.FETCH_UNIQUE:

                    //Debug.Assert(m_dr.FieldCount >= 1);
                    while (Result.TryReadRow(out var oa, out var names))
                    {
                        // 1st col => [ 2nd col, 3rd col, ... ]
                        result[PhpValue.FromClr(oa[0]).ToIntStringKey()] = AsAssocArray(oa, names, 1);
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
        public virtual PhpValue fetchColumn(int? column_number = default)
        {
            if (Result.CheckRowIndex(Result.CurrentRowIndex))
            {
                int idx = column_number.HasValue ? column_number.Value : Result.FetchNextField();
                if (Result.CheckFieldIndex(idx))
                {
                    return PhpValue.FromClr(Result.GetFieldValue(Result.CurrentRowIndex, idx));
                }
            }

            //
            return PhpValue.False;
        }

        /// <summary>
        /// Fetches the next row and returns it as an object.
        /// </summary>
        /// <param name="class_name">Name of the created class.</param>
        /// <param name="ctor_args">Elements of this array are passed to the constructor.</param>
        /// <returns>Returns an instance of the required class with property names that correspond to the column names or FALSE on failure</returns>
        [return: CastToFalse]
        public virtual object fetchObject(string class_name = nameof(stdClass), PhpArray ctor_args = null)
        {
            if (string.IsNullOrEmpty(class_name) || nameof(stdClass).Equals(class_name, StringComparison.OrdinalIgnoreCase))
            {
                return Result.FetchStdClass();
            }

            //
            var phpt = Context.GetDeclaredTypeOrThrow(class_name, autoload: true);
            var args = (ctor_args == null) ? Array.Empty<PhpValue>() : ctor_args.GetValues();
            return FetchClass(phpt, args);
        }

        /// <summary>
        /// Retrieve a statement attribute.
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns>Returns the attribute value</returns>
        public virtual PhpValue getAttribute(int attribute) { throw new NotImplementedException(); }

        /// <summary>
        /// Set a statement attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="value">The value.</param>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public virtual bool setAttribute(int attribute, PhpValue value) { throw new NotImplementedException(); }

        /// <summary>
        /// Returns metadata for a column in a result set.
        /// </summary>
        /// <param name="column">The 0-indexed column in the result set.</param>
        /// <returns>Returns an associative array containing the values representing the metadata for a single column</returns>
        [return: CastToFalse]
        public virtual PhpArray getColumnMeta(int column)
        {
            if (Result != null && Result.CheckFieldIndex(column))
            {
                return new PhpArray(8)
                {
                    { "native_type", Result.GetPhpFieldType(column) },
                    { "driver:decl_type", Result.GetFieldType(column) },
                    // {  "flags", PhpValue.Null },
                    {  "name", Result.GetFieldName(column) },
                    // { "table", PhpValue.Null },
                    // { "len", PhpValue.Null },
                    // { "precision", PhpValue.Null },
                    // { "pdo_type", (int)PDO.PARAM.PARAM_NULL },
                };
            }
            else
            {
                return null; // FALSE
            }
        }

        /// <summary>
        /// Advances to the next rowset in a multi-rowset statement handle.
        /// </summary>
        /// <returns>Returns TRUE on success or FALSE on failure</returns>
        public virtual bool nextRowset() => Result != null && Result.NextResultSet();

        /// <summary>
        /// Returns the number of rows affected by the last SQL statement
        /// </summary>
        /// <returns></returns>
        public virtual int rowCount() => Result != null ? Result.RowCount : 0;

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
                        HandleError("fetch mode doesn't allow any extra arguments");
                        return false;
                    }

                    // ok
                    break;

                case PDO_FETCH.FETCH_COLUMN:
                    if (args.Length != 1)
                    {
                        HandleError("fetch mode requires the colno argument");
                        return false;
                    }
                    else if (args[0].IsLong(out var l))
                    {
                        _fetch_column = (int)l;
                        break;
                    }
                    else
                    {
                        HandleError("colno must be an integer");
                        return false;
                    }

                case PDO_FETCH.FETCH_CLASS:

                    _default_fetch_class_args = default;
                    _default_fetch_class = null;

                    if ((mode & PDO_FETCH.FETCH_CLASSTYPE) != 0)
                    {
                        // will be getting its class name from 1st column
                        if (args.Length != 0)
                        {
                            HandleError("fetch mode doesn't allow any extra arguments");
                            return false;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (args.Length == 0)
                        {
                            HandleError("fetch mode requires the classname argument");
                            return false;
                        }
                        else if (args.Length > 2)
                        {
                            HandleError("too many arguments");
                            return false;
                        }
                        else if (args[0].IsString(out var name))
                        {
                            _default_fetch_class = Context.GetDeclaredTypeOrThrow(name, autoload: true);
                            if (_default_fetch_class != null)
                            {
                                if (args.Length > 1)
                                {
                                    var ctorargs = args[1].AsArray();
                                    if (ctorargs == null && !args[1].IsNull)
                                    {
                                        HandleError("ctor_args must be either NULL or an array");
                                        return false;
                                    }
                                    else if (ctorargs != null && ctorargs.Count != 0)
                                    {
                                        _default_fetch_class_args = ctorargs.GetValues(); // TODO: deep copy
                                    }
                                }

                                break;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            HandleError("classname must be a string");
                            return false;
                        }
                    }

                //case PDO_FETCH.FETCH_INTO:
                //    if (args.Length != 1)
                //    {
                //        RaiseError("fetch mode requires the object parameter");
                //        return false;
                //    }
                //    else
                //    {
                //        FetchClassInto = args[0].AsObject();
                //        if (FetchClassInto == null)
                //        {
                //            RaiseError("object must be an object");
                //            return false;
                //        }

                //        m_fetchStyle = mode;
                //        return true;
                //    }

                //default:
                //    RaiseError("Invalid fetch mode specified");
                //    return false;

                default:
                    throw new NotImplementedException(mode.ToString());
            }

            //
            _default_fetch_type = mode;
            return true;
        }

        #endregion

        private protected PhpArray ReadNamed(int from = 0)
        {
            if (Result.TryReadRow(out var oa, out var names))
            {
                var arr = new PhpArray(oa.Length);

                for (int i = from; i < oa.Length; i++)
                {
                    var value = PhpValue.FromClr(oa[i]);
                    ref var bucket = ref arr.GetItemRef(new IntStringKey(names[i]));

                    if (Operators.IsSet(bucket))
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

            throw new InvalidOperationException();
        }

        private protected object FetchClass(PhpTypeInfo phpt, PhpValue[] ctor_args)
        {
            if (phpt == null)
            {
                HandleError("Fetch class not set.");
                return null;
            }

            // TODO: move to ResultResource:
            if (Result.TryReadRow(out object[] oa, out string[] names))
            {
                // instantiate class dynamically:
                var ctx = this.Context;
                var obj = phpt.Creator(ctx, ctor_args ?? Array.Empty<PhpValue>());

                // set object properties using reflection:
                for (int i = 0; i < names.Length; i++)
                {
                    // TODO: Operators.PropertySetValue( obj, names[i], FromClr(oa[i]) );

                    var p =
                        TypeMembersUtils.GetDeclaredProperty(phpt, names[i]) ??
                        TypeMembersUtils.GetRuntimeProperty(phpt, names[i], obj);

                    p.SetValue(ctx, obj, PhpValue.FromClr(oa[i]));
                }

                //
                return obj;
            }
            else
            {
                return null;
            }
        }

        private protected static PhpValue ObjectOrFalse(object obj) => obj != null ? PhpValue.FromClass(obj) : PhpValue.False;

        private protected static PhpArray AsAssocArray(object[] oa, string[] names, int from)
        {
            var arr = new PhpArray(oa.Length - from);

            for (; from < oa.Length; from++)
            {
                arr[names[from]] = PhpValue.FromClr(oa[from]);
            }

            return arr;
        }

        private protected static PhpValue ConvertParam(PhpValue value, PARAM type)
        {
            switch (type)
            {
                case PARAM.PARAM_BOOL:
                    return value.ToBoolean();

                case PARAM.PARAM_INT:
                    return value.ToLong();

                case PARAM.PARAM_STR:
                    return value.IsUnicodeString(out var str) ? str : value.IsBinaryString(out var bin) ? bin : value.ToString();

                default:
                    return value;
            }
        }

        private protected bool FetchBound()
        {
            if (Result.TryReadRow(out var oa, out var names))
            {
                var bound = bound_columns;
                if (bound != null)
                {
                    for (int i = 0; i < oa.Length; i++)
                    {
                        if (bound.TryGetValue(i, out var p))
                        {
                            p.SetValue(oa[i]);
                        }

                        if (bound.TryGetValue(names[i], out p))
                        {
                            p.SetValue(oa[i]);
                        }
                    }
                }

                // TODO: reset unbound variables ?

                return true;
            }

            return false;
        }

        private protected bool StoreParameter(ref Dictionary<IntStringKey, BoundParam> dict, IntStringKey parameter, PhpValue variable, PARAM? type)
        {
            if (dict == null)
            {
                dict = new Dictionary<IntStringKey, BoundParam>();
            }

            if (parameter.IsInteger)
            {
                // convert from 1-indexed to 0-based
                parameter = new IntStringKey(parameter.Integer - 1);
            }

            dict[parameter] = new BoundParam { Variable = variable, Type = type, };

            return true;
        }

        private protected bool/*!*/BindParameters(DbParameterCollection parameters, PhpArray input_parameters = null)
        {
            parameters.Clear();

            //
            if (input_parameters != null)
            {
                // clear currently bound parameters:
                if (bound_params != null)
                {
                    bound_params.Clear();
                }
                else
                {
                    bound_params = new Dictionary<IntStringKey, BoundParam>();
                }

                // bind new parameters:
                var enumerator = input_parameters.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    bound_params[enumerator.CurrentKey] = new BoundParam { Type = PARAM.PARAM_STR, Variable = enumerator.CurrentValue.ToString(Context), };
                }
            }

            //
            if (bound_params != null && bound_params.Count != 0)
            {
                foreach (var pair in bound_params)
                {
                    // unnamed and rewritten parameters are mapped to the real name
                    var key = bound_param_map != null && bound_param_map.TryGetValue(pair.Key, out var newkey)
                        ? newkey
                        : pair.Key;

                    //
                    var p = _cmd.CreateParameter();
                    p.ParameterName = key.ToString();

                    pair.Value.InitializeParameter(p);
                    parameters.Add(p);
                }
            }

            //
            return true;
        }
    }
}
