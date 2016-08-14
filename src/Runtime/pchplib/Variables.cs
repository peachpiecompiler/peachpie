using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    #region Enumerations

    /// <summary>
    /// Type of extraction for <c>extract</c> function.
    /// </summary>
    [Flags]
    public enum ExtractType
    {
        /// <summary>PHP constant: EXTR_OVERWRITE</summary>
        Overwrite,

        /// <summary>PHP constant: EXTR_SKIP</summary>
        Skip,

        /// <summary>PHP constant: EXTR_PREFIX_SAME</summary>
        PrefixSame,

        /// <summary>PHP constant: EXTR_PREFIX_ALL</summary>
        PrefixAll,

        /// <summary>PHP constant: EXTR_PREFIX_INVALID</summary>
        PrefixInvalid,

        /// <summary>PHP constant: EXTR_PREFIX_IF_EXISTS</summary>
        PrefixIfExists,

        /// <summary>PHP constant: EXTR_IF_EXISTS</summary>
        IfExists,

        /// <summary>PHP constant: EXTR_REFS</summary>
        Refs = 256,

        /// <summary>A value masking all options but <see cref="Refs"/> (0xff).</summary>
        NonFlags = 0xff
    }

    /// <summary>
    /// File upload errors.
    /// </summary>
    public enum FileUploadError
    {
        /// <summary>
        /// No error.
        /// </summary>
        None,

        /// <summary>
        /// The uploaded file exceeds the "upload_max_filesize" configuration option.
        /// </summary>
        SizeExceededOnServer,

        /// <summary>
        /// The uploaded file exceeds the "MAX_FILE_SIZE" value specified in the form.
        /// </summary>
        SizeExceededOnClient,

        /// <summary>
        /// The uploaded file was only partially uploaded.
        /// </summary>
        Partial,

        /// <summary>
        /// No file was uploaded.
        /// </summary>
        NoFile,

        /// <summary>
        /// Missing a temporary folder
        /// </summary>
        NoTempDirectory,

        /// <summary>
        /// Missing a temporary folder
        /// </summary>
        CantWrite,

        /// <summary>
        /// A PHP extension stopped the file upload
        /// </summary>
        ErrorExtension,
    }

    #endregion

    public static class Variables
    {
        #region Constants

        /// <summary>
        /// Recursive counting.
        /// </summary>
        public const int COUNT_RECURSIVE = 1;

        /// <summary>
        /// Non-recursive counting.
        /// </summary>
        public const int COUNT_NORMAL = 0;

        /// <summary>PHP constant: EXTR_OVERWRITE</summary>
        public const int EXTR_OVERWRITE = (int)ExtractType.Overwrite;

        /// <summary>PHP constant: EXTR_SKIP</summary>
        public const int EXTR_SKIP = (int)ExtractType.Skip;

        /// <summary>PHP constant: EXTR_PREFIX_SAME</summary>
        public const int EXTR_PREFIX_SAME = (int)ExtractType.PrefixSame;

        /// <summary>PHP constant: EXTR_PREFIX_ALL</summary>
        public const int EXTR_PREFIX_ALL = (int)ExtractType.PrefixAll;

        /// <summary>PHP constant: EXTR_PREFIX_INVALID</summary>
        public const int EXTR_PREFIX_INVALID = (int)ExtractType.PrefixInvalid;

        /// <summary>PHP constant: EXTR_PREFIX_IF_EXISTS</summary>
        public const int EXTR_PREFIX_IF_EXISTS = (int)ExtractType.PrefixIfExists;

        /// <summary>PHP constant: EXTR_IF_EXISTS</summary>
        public const int EXTR_IF_EXISTS = (int)ExtractType.IfExists;

        /// <summary>PHP constant: EXTR_REFS</summary>
        public const int EXTR_REFS = (int)ExtractType.Refs;

        /// <summary>
        /// No error.
        /// </summary>
        public const int UPLOAD_ERR_OK = (int)FileUploadError.None;

        /// <summary>
        /// The uploaded file exceeds the "upload_max_filesize" configuration option.
        /// </summary>
        public const int UPLOAD_ERR_INI_SIZE = (int)FileUploadError.SizeExceededOnServer;

        /// <summary>
        /// The uploaded file exceeds the "MAX_FILE_SIZE" value specified in the form.
        /// </summary>
        public const int UPLOAD_ERR_FORM_SIZE = (int)FileUploadError.SizeExceededOnClient;

        /// <summary>
        /// The uploaded file was only partially uploaded.
        /// </summary>
        public const int UPLOAD_ERR_PARTIAL = (int)FileUploadError.Partial;

        /// <summary>
        /// No file was uploaded.
        /// </summary>
        public const int UPLOAD_ERR_NO_FILE = (int)FileUploadError.NoFile;

        /// <summary>
        /// Missing a temporary folder
        /// </summary>
        public const int UPLOAD_ERR_NO_TMP_DIR = (int)FileUploadError.NoTempDirectory;

        /// <summary>
        /// Missing a temporary folder
        /// </summary>
        public const int UPLOAD_ERR_CANT_WRITE = (int)FileUploadError.CantWrite;

        /// <summary>
        /// A PHP extension stopped the file upload
        /// </summary>
        public const int UPLOAD_ERR_EXTENSION = (int)FileUploadError.ErrorExtension;

        #endregion

        #region count, sizeof

        /// <summary>
        /// Counts items in a variable.
        /// </summary>
        /// <param name="variable">The variable which items to count.</param>
        /// <param name="mode">Whether to count recursively.</param>
        /// <returns>The number of items in all arrays contained recursivelly in <paramref name="variable"/>.</returns>
        /// <remarks>If any item of the <paramref name="variable"/> contains infinite recursion 
        /// skips items that are repeating because of such recursion.
        /// </remarks>
        public static long @sizeof(PhpValue variable, int mode = COUNT_NORMAL) => count(variable, mode);

        /// <summary>
        /// Counts items in a variable.
        /// </summary>
        /// <param name="variable">The variable which items to count.</param>
        /// <param name="mode">Whether to count recursively.</param>
        /// <returns>The number of items in all arrays contained recursivelly in <paramref name="variable"/>.</returns>
        /// <remarks>If any item of the <paramref name="variable"/> contains infinite recursion 
        /// skips items that are repeating because of such recursion.
        /// </remarks>
        public static long count(PhpValue variable, int mode = COUNT_NORMAL)
        {
            // null or uninitialized variable:
            if (variable.IsNull)
            {
                return 0;
            }
            else if (variable.IsArray)
            {
                // PHP array
                return (mode == COUNT_RECURSIVE)
                    ? variable.Array.RecursiveCount
                    : variable.Array.Count;
            }
            else if (variable.IsObject)
            {
                // PHP Countable
                var countable = variable.Object as Countable;
                if (countable != null)
                {
                    return countable.count().ToLong();
                }

                // CLR ICollection
                var collection = variable.Object as System.Collections.ICollection;
                if (collection != null)
                {
                    return collection.Count;
                }
            }
            else if (variable.IsAlias)
            {
                return count(variable.Alias.Value, mode);
            }

            // count not supported
            return 1;
        }

        #endregion

        #region doubleval, floatval, intval, strval, settype, gettype, boolval

        /// <summary>
        /// Converts to double.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The converted value.</returns>
        public static double doubleval(PhpValue variable) => variable.ToDouble();

        /// <summary>
        /// Converts to double.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The converted value.</returns>
        public static double floatval(PhpValue variable) => variable.ToDouble();

        /// <summary>
        /// Converts to integer.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The converted value.</returns>
        public static long intval(PhpValue variable) => variable.ToLong();

        /// <summary>
        /// Converts to integer using a specified base.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="base">The base.</param>
        /// <returns>The converted value.</returns>
        public static long intval(PhpValue variable, int @base)
        {
            // TODO: base
            // The integer value of var on success, or 0 on failure. Empty arrays and objects return 0, non-empty arrays and objects return 1. 
            // The maximum value depends on the system. 32 bit systems have a maximum signed integer range of -2147483648 to 2147483647. So for example on such a system, intval('1000000000000') will return 2147483647. The maximum signed integer value for 64 bit systems is 9223372036854775807. 
            return variable.ToLong();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The converted value.</returns>
        public static string strval(Context ctx, PhpValue variable) => variable.ToString(ctx);

        /// <summary>
        /// Converts to boolean.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The converted value.</returns>
        public static bool boolval(PhpValue variable) => variable.ToBoolean();

        /// <summary>
        /// Sets variable's type.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="type">The string identifying a new type. See PHP manual for details.</param>
        /// <returns>Whether <paramref name="type"/> is valid type identifier.</returns>
        /// <exception cref="PhpException"><paramref name="type"/> has invalid value.</exception>
        public static bool settype(Context ctx, ref PhpValue variable, string type)
        {
            switch (type.ToLowerInvariant())
            {
                case "bool":
                case "boolean":
                    variable = PhpValue.Create(variable.ToBoolean());
                    return true;

                case "int":
                case "integer":
                    variable = PhpValue.Create(variable.ToLong());
                    return true;

                case "float":
                case "double":
                    variable = PhpValue.Create(variable.ToDouble());
                    return true;

                case "string":
                    variable = PhpValue.Create(variable.ToString(ctx));
                    return true;

                case "array":
                    variable = PhpValue.Create(variable.AsArray());
                    return true;

                case "object":
                    variable = PhpValue.FromClass(variable.ToClass());
                    return true;

                case "null":
                    variable = PhpValue.Null;
                    return true;
            }

            //PhpException.InvalidArgument("type", LibResources.GetString("invalid_type_name"));
            //return false;
            throw new ArgumentException(nameof(type));
        }

        /// <summary>
        /// Retrieves name of a variable type.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The string type identifier. See PHP manual for details.</returns>
        public static string gettype(PhpValue variable)
        {
            // works well on references:
            return PhpVariable.GetTypeName(variable);
        }

        #endregion

        #region is_<type>

        /// <summary>
        /// Checks whether a dereferenced variable is integer.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is integer.</returns>
        public static bool is_int(PhpValue variable) => variable.TypeCode == PhpTypeCode.Long || variable.TypeCode == PhpTypeCode.Int32;

        /// <summary>
        /// Checks whether a dereferenced variable is integer.
        /// Alias for is_int().
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is integer.</returns>
        public static bool is_integer(PhpValue variable) => is_int(variable);

        /// <summary>
        /// Checks whether a dereferenced variable is long. 
        /// TODO: Alias for is_int(). But not in Phalanger.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is long.</returns>
        public static bool is_long(PhpValue variable) => is_int(variable);

        /// <summary>
        /// Checks whether a dereferenced variable is boolean.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is boolean.</returns>
        public static bool is_bool(PhpValue variable) => variable.TypeCode == PhpTypeCode.Boolean;

        /// <summary>
        /// Checks whether a dereferenced variable is double.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is double.</returns>
        public static bool is_float(PhpValue variable) => is_double(variable);

        /// <summary>
        /// Checks whether a dereferenced variable is double.
        /// Alias for is_float().
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is double.</returns>
        public static bool is_double(PhpValue variable) => variable.TypeCode == PhpTypeCode.Double;

        /// <summary>
        /// Checks whether a dereferenced variable is double.
        /// Alias for is_float().
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is double.</returns>
        public static bool is_real(PhpValue variable) => is_double(variable);

        /// <summary>
        /// Checks whether a dereferenced variable is string.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is string.</returns>
        public static bool is_string(PhpValue variable) => variable.TypeCode == PhpTypeCode.String || variable.TypeCode == PhpTypeCode.WritableString;

        /// <summary>
        /// Checks whether a dereferenced variable is an <see cref="PhpArray"/>.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is <see cref="PhpArray"/>.</returns>
        public static bool is_array(PhpValue variable) => variable.IsArray;

        /// <summary>
        /// Checks whether a dereferenced variable is <see cref="Core.Reflection.DObject"/>.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is <see cref="Core.Reflection.DObject"/>.</returns>
        public static bool is_object(PhpValue variable)
            => variable.IsObject && variable.Object != null && !(variable.Object is __PHP_Incomplete_Class);

        /// <summary>
        /// Checks whether a dereferenced variable is a valid <see cref="PhpResource"/>.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is a valid <see cref="PhpResource"/>.</returns>
        public static bool is_resource(PhpValue variable)
        {
            //PhpResource res = variable as PhpResource;
            //return res != null && res.IsValid;
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether a dereferenced variable is a <B>null</B> reference.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is a <B>null</B> reference.</returns>
        public static bool is_null(PhpValue variable) => variable.IsNull;

        #endregion

        #region is_scalar, is_numeric, is_callable, get_resource_type

        /// <summary>
        /// Checks whether a dereferenced variable is a scalar.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is an integer, a double, a bool or a string after dereferencing.</returns>
        public static bool is_scalar(PhpValue variable) => PhpVariable.IsScalar(variable);

        /// <summary>
        /// Checks whether a dereferenced variable is numeric.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is integer, double or numeric string.
        /// <seealso cref="PHP.Core.Convert.StringToNumber"/></returns>
        public static bool is_numeric(PhpValue variable)
        {
            switch (variable.TypeCode)
            {
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                    return true;

                case PhpTypeCode.String:
                case PhpTypeCode.WritableString:
                    PhpNumber tmp;
                    return (variable.ToNumber(out tmp) & Core.Convert.NumberInfo.IsNumber) != 0;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Verifies that the contents of a variable can be called as a function.
        /// </summary>
        /// <param name="caller">Current class context.</param>
        /// <param name="variable">The variable.</param>
        /// <param name="syntaxOnly">If <B>true</B>, it is only checked that has <pararef name="variable"/>
        /// a valid structure to be used as a callback. if <B>false</B>, the existence of the function (or
        /// method) is also verified.</param>
        /// <returns><B>true</B> if <paramref name="variable"/> denotes a function, <B>false</B>
        /// otherwise.</returns>
        public static bool IsCallable(PhpValue variable, bool syntaxOnly = false)
        {
            return PhpVariable.IsValidCallback(variable.AsCallable());  // TODO: check syntaxOnly || can be bound
        }

        /// <summary>
        /// Verifies that the contents of a variable can be called as a function.
        /// </summary>
        /// <param name="caller">Current class context.</param>
        /// <param name="variable">The variable.</param>
        /// <param name="syntaxOnly">If <B>true</B>, it is only checked that has <pararef name="variable"/>
        /// a valid structure to be used as a callback. if <B>false</B>, the existence of the function (or
        /// method) is also verified.</param>
        /// <param name="callableName">Receives the name of the function or method (for example
        /// <c>SomeClass::SomeMethod</c>).</param>
        /// <returns><B>true</B> if <paramref name="variable"/> denotes a function, <B>false</B>
        /// otherwise.</returns>
        public static bool is_callable(Context ctx, PhpValue variable, bool syntaxOnly, out string callableName)
        {
            var callback = variable.AsCallable();
            if (PhpVariable.IsValidCallback(callback))
            {
                callableName = callback.ToString();
                return true;
            }

            callableName = variable.ToString(ctx);
            return false;
        }

        ///// <summary>
        ///// Returns the type of a resource.
        ///// </summary>
        ///// <param name="resource">The resource.</param>
        ///// <returns>The resource type name or <c>null</c> if <paramref name="resource"/> is <c>null</c>.</returns>
        ////[return: CastToFalse]
        //public static string get_resource_type(PhpResource resource)  // TODO: once we implement PhpResource API
        //{
        //    return (resource != null ? resource.TypeName : null);
        //}

        #endregion

        #region print_r, var_export, var_dump

        #region IPhpVariableFormatter Implementation

        class PrintFormatter : PhpVariableVisitor, IPhpVariableFormatter
        {
            readonly Context _ctx;
            readonly string _nl;

            PhpString _output;
            int _indent;

            const int IndentSize = 4;

            void OutputIndent()
            {
                if (_indent > 0)
                {
                    _output.Append(new string(' ', _indent * IndentSize));
                }
            }

            public PrintFormatter(Context ctx, string newline)
            {
                Debug.Assert(ctx != null);
                _ctx = ctx;
                _nl = newline;
            }

            public PhpString Serialize(PhpValue value)
            {
                _output = new PhpString();
                _indent = 0;

                //
                Accept(value);
                _output.Append(_nl);

                return _output;
            }

            public override void Accept(bool obj) => _output.Append(obj ? "1" : string.Empty);

            public override void Accept(long obj) => _output.Append(obj.ToString());

            public override void Accept(double obj) => _output.Append(Core.Convert.ToString(obj, _ctx));

            public override void Accept(string obj) => _output.Append(obj);

            public override void Accept(PhpString obj) => _output.Append(obj);

            public override void AcceptNull() { }

            public override void Accept(PhpArray obj)
            {
                // Array
                // (
                _output.Append(PhpArray.PrintablePhpTypeName);
                _output.Append(_nl);
                OutputIndent();
                _output.Append("(");
                _output.Append(_nl);

                _indent++;

                base.Accept(obj);

                _indent--;
                OutputIndent();
                _output.Append(")");
            }

            public override void AcceptArrayItem(KeyValuePair<IntStringKey, PhpValue> entry)
            {
                // [key] => value
                OutputIndent();

                _output.Append($"[{entry.Key.ToString()}] => ");
                _indent++;
                Accept(entry.Value);
                _indent--;

                if (entry.Value.IsArray)
                {
                    _output.Append(_nl);
                }

                _output.Append(_nl);
            }

            public override void AcceptObject(object obj)
            {
                // typename Object
                // (
                _output.Append(obj.GetType().FullName.Replace('.', '\\').Replace('+', '\\') + " ");
                _output.Append("Object");
                _output.Append(_nl);
                OutputIndent();
                _output.Append("(");

                _indent++;

                // TODO: object members

                _indent--;
                _output.Append(")");
            }
        }

        class ExportFormatter : PhpVariableVisitor, IPhpVariableFormatter
        {
            readonly Context _ctx;
            readonly string _nl;

            PhpString _output;
            int _indent;

            const int IndentSize = 2;

            void OutputIndent()
            {
                if (_indent > 0)
                {
                    _output.Append(new string(' ', _indent * IndentSize));
                }
            }

            public ExportFormatter(Context ctx, string newline)
            {
                Debug.Assert(ctx != null);
                _ctx = ctx;
                _nl = newline;
            }

            public PhpString Serialize(PhpValue value)
            {
                _output = new PhpString();
                _indent = 0;

                //
                Accept(value);
                return _output;
            }

            public override void Accept(bool obj) => _output.Append(obj ? PhpVariable.True : PhpVariable.False);

            public override void Accept(long obj) => _output.Append(obj.ToString());

            public override void Accept(double obj) => _output.Append(Core.Convert.ToString(obj, _ctx));

            public override void Accept(string obj) => _output.Append($"'{obj}'");

            public override void Accept(PhpString obj)
            {
                _output.Append("'");
                _output.Append(obj);
                _output.Append("'");
            }

            public override void AcceptNull() => _output.Append(PhpVariable.TypeNameNull);

            public override void Accept(PhpArray obj)
            {
                // array (
                _output.Append(PhpArray.PhpTypeName);
                _output.Append(" (");
                _output.Append(_nl);
                
                _indent++;

                base.Accept(obj);

                _indent--;
                OutputIndent();
                _output.Append(")");
            }

            public override void AcceptArrayItem(KeyValuePair<IntStringKey, PhpValue> entry)
            {
                // [key] => value
                OutputIndent();

                Accept(PhpValue.Create(entry.Key));
                _output.Append(" => ");
                if (entry.Value.IsArray || entry.Value.IsObject && !entry.Value.IsNull)
                {
                    _output.Append(_nl);
                    OutputIndent();
                }
                Accept(entry.Value);
                
                _output.Append(",");
                _output.Append(_nl);
            }

            public override void AcceptObject(object obj)
            {
                throw new NotImplementedException();
            }
        }

        class DumpFormatter : PhpVariableVisitor, IPhpVariableFormatter
        {
            readonly Context _ctx;
            readonly string _nl;

            PhpString _output;
            int _indent;

            const int IndentSize = 2;

            void OutputIndent()
            {
                if (_indent > 0)
                {
                    _output.Append(new string(' ', _indent * IndentSize));
                }
            }

            public DumpFormatter(Context ctx, string newline)
            {
                Debug.Assert(ctx != null);
                _ctx = ctx;
                _nl = newline;
            }

            public PhpString Serialize(PhpValue value)
            {
                _output = new PhpString();
                _indent = 0;

                //
                Accept(value);
                _output.Append(_nl);

                return _output;
            }

            public override void Accept(bool obj)
            {
                _output.Append(PhpVariable.TypeNameBool);
                _output.Append("(");
                _output.Append(obj ? PhpVariable.True : PhpVariable.False);
                _output.Append(")");
            }

            public override void Accept(long obj)
            {
                _output.Append(PhpVariable.TypeNameInt);
                _output.Append("(");
                _output.Append(obj.ToString());
                _output.Append(")");
            }

            public override void Accept(double obj)
            {
                _output.Append(PhpVariable.TypeNameFloat);
                _output.Append("(");
                _output.Append(Core.Convert.ToString(obj, _ctx));
                _output.Append(")");
            }

            public override void Accept(string obj)
            {
                _output.Append(PhpVariable.TypeNameString);
                _output.Append($"({obj.Length}) ");
                _output.Append($"\"{obj}\"");
            }

            public override void Accept(PhpString obj)
            {
                _output.Append(PhpVariable.TypeNameString);
                _output.Append($"({obj.Length}) ");
                _output.Append("\"");
                _output.Append(obj);
                _output.Append("\"");
            }

            public override void Accept(PhpAlias obj)
            {
                _output.Append("&");
                base.Accept(obj);
            }

            public override void AcceptNull() => _output.Append(PhpVariable.TypeNameNull);

            public override void Accept(PhpArray obj)
            {
                // array (size=COUNT)
                _output.Append(PhpArray.PhpTypeName);
                _output.Append($"({obj.Count}) {{");
                _output.Append(_nl);

                _indent++;

                base.Accept(obj);

                _indent--;
                OutputIndent();
                _output.Append("}");
            }

            public override void AcceptArrayItem(KeyValuePair<IntStringKey, PhpValue> entry)
            {
                // ikey => value
                // 'skey' => value

                OutputIndent();

                _output.Append("[" + (entry.Key.IsString ? $"\"{entry.Key.String}\"" : entry.Key.Integer.ToString()) + "]");
                _output.Append("=>");
                _output.Append(_nl);
                OutputIndent();
                Accept(entry.Value);
                _output.Append(_nl);
            }

            public override void AcceptObject(object obj)
            {
                throw new NotImplementedException();
            }
        }

        //class HtmlDumpFormatter : DumpFormatter
        //{
            
        //}

        #endregion

        /// <summary>
        /// Outputs or returns human-readable information about a variable. 
        /// </summary>
        /// <param name="value">The variable.</param>
        /// <param name="returnString">Whether to return a string representation.</param>
        /// <returns>A string representation or <c>true</c> if <paramref name="returnString"/> is <c>false</c>.</returns>
        public static PhpValue print_r(Context ctx, PhpValue value, bool returnString = false)
        {
            var output = (new PrintFormatter(ctx, "\n")).Serialize(value);

            if (returnString)
            {
                // output to a string:
                return PhpValue.Create(output);
            }
            else
            {
                // output to script context:
                ctx.Echo(output);
                return PhpValue.True;
            }
        }

        /// <summary>
        /// Dumps variables.
        /// </summary>
        /// <param name="variables">Variables to be dumped.</param>
        public static void var_dump(Context ctx, params PhpValue[] variables)
        {
            var formatter = new DumpFormatter(ctx, "\n"); // TODO: HtmlDumpFormatter
            for (int i = 0; i < variables.Length; i++)
            {
                ctx.Echo(formatter.Serialize(variables[i]));
            }
        }

        /// <summary>
        /// Outputs or returns a pars-able string representation of a variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="returnString">Whether to return a string representation.</param>
        /// <returns>A string representation or a <c>null</c> reference if <paramref name="returnString"/> is <c>false</c>.</returns>
        public static string var_export(Context ctx, PhpValue variable, bool returnString = false)
        {
            var output = (new ExportFormatter(ctx, "\n")).Serialize(variable);

            if (returnString)
            {
                // output to a string:
                return output.ToString(ctx);
            }
            else
            {
                // output to script context:
                ctx.Echo(output);
                return null;
            }

        }

        #endregion
    }
}
