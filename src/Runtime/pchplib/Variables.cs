using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class Variables
    {
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
        public static bool settype(ref PhpValue variable, string type)
        {
            //switch (System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToLower(type)) // we don't need Unicode characters to be lowercased properly // CurrentCulture is slow
            //{
            //    case "bool":
            //    case "boolean":
            //        variable = PHP.Core.Convert.ObjectToBoolean(variable);
            //        return true;

            //    case "int":
            //    case "integer":
            //        variable = PHP.Core.Convert.ObjectToInteger(variable);
            //        return true;

            //    case "float":
            //    case "double":
            //        variable = PHP.Core.Convert.ObjectToDouble(variable);
            //        return true;

            //    case "string":
            //        variable = PHP.Core.Convert.ObjectToString(variable);
            //        return true;

            //    case "array":
            //        variable = PHP.Core.Convert.ObjectToPhpArray(variable);
            //        return true;

            //    case "object":
            //        variable = PHP.Core.Convert.ObjectToDObject(variable, ScriptContext.CurrentContext);
            //        return true;

            //    case "null":
            //        variable = null;
            //        return true;

            //    default:
            //        PhpException.InvalidArgument("type", LibResources.GetString("invalid_type_name"));
            //        return false;
            //}
            throw new NotImplementedException();
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
