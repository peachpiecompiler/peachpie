using Pchp.Core;
using System;
using System.Collections.Generic;
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
    }
}
