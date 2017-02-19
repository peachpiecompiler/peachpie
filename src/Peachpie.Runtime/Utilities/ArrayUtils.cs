using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    public static class ArrayUtils
    {
        #region Properties

        public static readonly PhpValue[] EmptyValues = Empty<PhpValue>();

        public static readonly object[] EmptyObjects = Empty<object>();

        public static readonly byte[] EmptyBytes = Empty<byte>();

        public static string[] EmptyStrings => Array.Empty<string>();

        public static T[] Empty<T>() => Array.Empty<T>();

        #endregion

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        public static void Write(this Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);

        /// <summary>
        /// Decodes a sequence of bytes from the specified byte array into a string.
        /// </summary>
        public static string GetString(this Encoding encoding, byte[] bytes) => encoding.GetString(bytes, 0, bytes.Length);

        /// <summary>Cached <see cref="Func{T, TResult}"/> instance.</summary>
        static readonly Func<object, bool> _not_null = new Func<object, bool>((obj) => obj != null);

        /// <summary>
        /// Filters a sequence of values that are not a <c>null</c> reference.
        /// </summary>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> e) where T : class => e.Where<T>(_not_null);

        /// <summary>
        /// Gets last element of the list.
        /// </summary>
        /// <returns>Last element or default of {T}.</returns>
        public static T Last<T>(this IList<T> list) => list.Count != 0 ? list[list.Count - 1] : default(T);

        /// <summary>
        /// Gets last character of the string.
        /// </summary>
        /// <returns>Last character or <c>\0</c>.</returns>
        public static char Last(this string str) => str.Length != 0 ? str[str.Length - 1] : '\0';

        /// <summary>
        /// Fast trim of a specified character.
        /// </summary>
        public static string Trim(string str, char ch)
        {
            if (!string.IsNullOrEmpty(str))
            {
                int i = 0;
                int j = str.Length - 1;

                //
                while (i < str.Length && str[i] == ch) i++;
                while (j > i && str[j] == ch) j--;

                //
                return i < j ? str.Substring(i, j - i + 1) : string.Empty;
            }

            return str;
        }

        /// <summary>
        /// Fills a portion of an array of bytes by specified byte.
        /// </summary>
        /// <param name="array">The array to fill.</param>
        /// <param name="value">The value to fill the array with.</param>
        /// <param name="offset">The index of the first byte to be set.</param>
        /// <param name="count">The number of bytes to be set.</param>
        /// <remarks>This method uses fast unsafe filling of memory with bytes.</remarks>
        public static void Fill(byte[] array, byte value, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (offset < 0 || offset + count > array.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0)
                throw new ArgumentOutOfRangeException("length");
            if (array.Length == 0)
                return;

            for (int i = offset; i < count + offset; i++)
            {
                array[i] = value;
            }
        }

        /// <summary>
        /// Searches for specified character in sorted array of characters.
        /// Specialized version of <see cref="Array.BinarySearch{T}(T[], T)"/>.
        /// </summary>
        /// <param name="array">The array to search in.</param>
        /// <param name="c">The character to search for.</param>
        /// <returns>The position of the <paramref name="c"/> in <paramref name="array"/> or -1 if not found.</returns>
        public static int BinarySearch(char[] array, char c)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            int i = 0;
            int j = array.Length - 1;
            while (i < j)
            {
                int m = (i + j) >> 1;
                char cm = array[m];
                if (c == cm) return m;

                if (c > cm)
                {
                    i = m + 1;
                }
                else
                {
                    j = m - 1;
                }
            }
            return (array[i] == c) ? i : -1;
        }
    }

}
