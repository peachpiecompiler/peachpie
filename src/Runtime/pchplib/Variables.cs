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
