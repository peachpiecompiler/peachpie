using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class PhpIni
    {
        #region ini_get, ini_set, ini_restore, get_cfg_var, ini_alter, ini_get_all

        /// <summary>
        /// Gets the value of a configuration option.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        /// <returns>The option old value conveted to string or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public static string ini_get(Context ctx, string option)
        {
            var value = ctx.Options?.GetValue(option);
            if (value.HasValue == false || !value.Value.IsSet)
            {
                return null;
            }

            return value.Value.ToString(ctx);
        }

        /// <summary>
        /// Sets the value of a configuration option.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        /// <param name="value">The option new value.</param>
        /// <returns>The option old value converted to string or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public static string ini_set(Context ctx, string option, PhpValue value)
        {
            var old = ini_get(ctx, option);
            if (old != null)
            {
                ctx.Options?.SetValue(option, value);
                return null;
            }

            return old;
        }

        /// <summary>
        /// Restores the value of a configuration option to its global value.
        /// No value is returned.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        public static void ini_restore(Context ctx, string option)
        {
            ctx.Options.SetValue(option, ctx.Options.GetDefaultValue(option));
        }

        /// <summary>
        /// Gets the value of a configuration option (alias for <see cref="Get"/>).
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        /// <returns>The option old value conveted to string or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public static string get_cfg_var(Context ctx, string option) => ini_get(ctx, option);

        /// <summary>
        /// Sets the value of a configuration option (alias for <see cref="Set"/>).
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        /// <param name="value">The option new value converted to string.</param>
        /// <returns>The option old value.</returns>
        [return: CastToFalse]
        public static string ini_alter(Context ctx, string option, PhpValue value) => ini_set(ctx, option, value);

        /// <summary>
        /// Retrieves an array of configuration entries of a specified extension.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="extension">The PHP internal extension name.</param>
        /// <remarks>
        /// For each supported configuration option an entry is added to the resulting array.
        /// The key is the name of the option and the value is an array having three entries: 
        /// <list type="bullet">
        ///   <item><c>global_value</c> - global value of the option</item>
        ///   <item><c>local_value</c> - local value of the option</item>
        ///   <item><c>access</c> - 7 (PHP_INI_ALL), 6 (PHP_INI_PERDIR | PHP_INI_SYSTEM) or 4 (PHP_INI_SYSTEM)</item>
        /// </list>
        /// </remarks>
        public static PhpArray ini_get_all(Context ctx, string extension = null)
        {
            PhpArray result = new PhpArray();

            throw new NotImplementedException();
        }

        #endregion
    }
}
