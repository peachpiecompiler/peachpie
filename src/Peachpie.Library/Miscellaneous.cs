using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class Miscellaneous
    {
        // [return: CastToFalse] // once $extension will be supported
        public static string phpversion(string extension = null)
        {
            if (extension != null)
            {
                throw new NotImplementedException(nameof(extension));
            }

            return Environment.PHP_MAJOR_VERSION + "." + Environment.PHP_MINOR_VERSION + "." + Environment.PHP_RELEASE_VERSION;
        }

        #region Helpers

        /// <summary>
        /// Compares parts of varsions delimited by '.'.
        /// </summary>
        /// <param name="part1">A part of the first version.</param>
        /// <param name="part2">A part of the second version.</param>
        /// <returns>The result of parts comparison (-1,0,+1).</returns>
        static int CompareParts(string part1, string part2)
        {
            string[] parts = { "dev", "alpha", "a", "beta", "b", "RC", " ", "#", "pl", "p" };
            int[] order = { -1, 0, 1, 1, 2, 2, 3, 4, 5, 6, 6 };

            int i = Array.IndexOf(parts, part1);
            int j = Array.IndexOf(parts, part2);
            return Math.Sign(order[i + 1] - order[j + 1]);
        }

        /// <summary>
		/// Parses a version and splits it into an array of parts.
		/// </summary>
		/// <param name="version">The version to be parsed (can be a <B>null</B> reference).</param>
		/// <returns>An array of parts.</returns>
		/// <remarks>
		/// Non-alphanumeric characters are eliminated.
		/// The version is split in between a digit following a non-digit and by   
		/// characters '.', '-', '+', '_'. 
		/// </remarks>
		static string[] VersionToArray(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return Array.Empty<string>();
            }

            var sb = new StringBuilder(version.Length);
            char last = '\0';

            for (int i = 0; i < version.Length; i++)
            {
                var ch = version[i];
                if (ch == '-' || ch == '+' || ch == '_' || ch == '.')
                {
                    if (last != '.')
                    {
                        sb.Append(last = '.');
                    }
                }
                else if (i > 0 && (char.IsDigit(ch) ^ char.IsDigit(version[i - 1])))
                {
                    if (last != '.')
                    {
                        sb.Append('.');
                    }
                    sb.Append(last = ch);
                }
                else if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(last = ch);
                }
                else
                {
                    if (last != '.')
                    {
                        sb.Append(last = '.');
                    }
                }
            }

            if (last == '.')
            {
                sb.Length--;
            }

            return sb.ToString().Split('.');
        }

        #endregion

        /// <summary>
        /// Compares two "PHP-standardized" version number strings.
        /// </summary>
        public static int version_compare(string version1, string version2)
        {
            string[] v1 = VersionToArray(version1);
            string[] v2 = VersionToArray(version2);
            int result;

            for (int i = 0; i < Math.Max(v1.Length, v2.Length); i++)
            {
                string item1 = (i < v1.Length) ? v1[i] : " ";
                string item2 = (i < v2.Length) ? v2[i] : " ";

                if (char.IsDigit(item1[0]) && char.IsDigit(item2[0]))
                {
                    result = Comparison.Compare(Core.Convert.StringToLongInteger(item1), Core.Convert.StringToLongInteger(item2));
                }
                else
                {
                    result = CompareParts(char.IsDigit(item1[0]) ? "#" : item1, char.IsDigit(item2[0]) ? "#" : item2);
                }

                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        /// <summary>
        /// Compares two "PHP-standardized" version number strings.
        /// </summary>
        public static bool version_compare(string version1, string version2, string op)
        {
            var compare = version_compare(version1, version2);

            switch (op)
            {
                case "<":
                case "lt": return compare < 0;

                case "<=":
                case "le": return compare <= 0;

                case ">":
                case "gt": return compare > 0;

                case ">=":
                case "ge": return compare >= 0;

                case "==":
                case "=":
                case "eq": return compare == 0;

                case "!=":
                case "<>":
                case "ne": return compare != 0;
            }

            throw new ArgumentException();  // TODO: return NULL
        }

        /// <summary>
        /// Find out whether an extension is loaded.
        /// </summary>
        /// <param name="name">The extension name.</param>
        /// <returns>Returns <c>TRUE</c> if the extension identified by name is loaded, <c>FALSE</c> otherwise.</returns>
        public static bool extension_loaded(string name) => Context.IsExtensionLoaded(name);

        /// <summary>
        /// Returns an array with names of all loaded native extensions.
        /// </summary>
        /// <param name="zend_extensions">Only return Zend extensions.</param>
        public static PhpArray get_loaded_extensions(bool zend_extensions = false)
        {
            if (zend_extensions)
            {
                throw new NotImplementedException(nameof(zend_extensions));
            }

            var extensions = Context.GetLoadedExtensions();
            var result = new PhpArray(extensions.Count);

            foreach (var e in extensions)
            {
                result.Add(PhpValue.Create(e));
            }

            return result;
        }

        /// <summary>
		/// Returns an array with names of the functions of a native extension.
		/// </summary>
        /// <param name="extension">Internal extension name (e.g. <c>sockets</c>).</param>
        /// <returns>The array of function names or <c>null</c> if the <paramref name="extension"/> is not loaded.</returns>
        [return: CastToFalse]
        public static PhpArray get_extension_funcs(string extension)
        {
            var funcs = Context.GetRoutinesByExtensionOrNull(extension);
            if (funcs != null)
            {
                var result = new PhpArray();
                foreach (var e in funcs)
                {
                    result.Add(PhpValue.Create(e));
                }

                return result;
            }
            else
            {
                return null;
            }
        }
    }
}
