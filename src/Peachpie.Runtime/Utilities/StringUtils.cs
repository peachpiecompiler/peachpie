using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Utilities
{
    public static class StringUtils
    {
        public static bool EqualsOrdinalIgnoreCase(this string str1, string str2) => string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);

        public static bool EqualsOrdinalIgnoreCase(this ReadOnlySpan<char> str1, ReadOnlySpan<char> str2) => str1.Equals(str2, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Converts a string of bytes into hexadecimal representation.
        /// </summary>
        /// <param name="bytes">The string of bytes.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>Concatenation of hexadecimal values of bytes of <paramref name="bytes"/> separated by <paramref name="separator"/>.</returns>
        public static string BinToHex(byte[] bytes, string separator = null)
        {
            if (bytes == null) return null;
            if (bytes.Length == 0) return string.Empty;
            if (separator == null) separator = string.Empty;

            int c;
            int length = bytes.Length;
            int sep_length = separator.Length;
            int res_length = length * (2 + sep_length);

            const string hex_digs = "0123456789abcdef";

            // prepares characters which will be appended to the result for each byte:
            char[] chars = new char[2 + sep_length];
            separator.CopyTo(0, chars, 2, sep_length);

            // prepares the result:
            StringBuilder result = new StringBuilder(res_length, res_length);

            // appends characters to the result for each byte:
            for (int i = 0; i < length - 1; i++)
            {
                c = bytes[i];
                chars[0] = hex_digs[(c & 0xf0) >> 4];
                chars[1] = hex_digs[(c & 0x0f)];
                result.Append(chars);
            }

            // the last byte:
            c = bytes[length - 1];
            result.Append(hex_digs[(c & 0xf0) >> 4]);
            result.Append(hex_digs[(c & 0x0f)]);

            return result.ToString();
        }

        /// <summary>
        /// Gets last string character or <c>\0</c> if the given string is <c>null</c> or empty.
        /// </summary>
        public static char LastChar(this string str) => string.IsNullOrEmpty(str) ? '\0' : str[str.Length - 1];

        /// <summary>
        /// Most efficient way of searching for index of a substring ordinarily.
        /// </summary>
        /// <param name="source">The string to search. </param>
        /// <param name="value">The string to locate within <paramref name="source" />. </param>
        /// <param name="startIndex">The zero-based starting index of the search. </param>
        /// <param name="count">The number of elements in the section to search. </param>
        /// <returns>
        /// The zero-based index of the first occurrence of <paramref name="value" /> within the section of <paramref name="source" />
        /// that starts at<paramref name= "startIndex" /> and
        /// contains the number of elements specified by<paramref name="count" />, if found; otherwise, -1.</returns>
        public static int IndexOfOrdinal(this string source, string value, int startIndex, int count)
        {
            return System.Globalization.CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, value, startIndex, count, System.Globalization.CompareOptions.Ordinal);
        }

        /// <summary>
        /// Specialized <c>concat</c> method.
        /// </summary>
        public static string Concat(ReadOnlySpan<char> str1, char str2, ReadOnlySpan<char> str3)
        {
            Span<char> chars = stackalloc char[str1.Length + 1 + str3.Length]; // ~512 bytes

            str1.CopyTo(chars);
            chars[str1.Length] = str2;
            str3.CopyTo(chars.Slice(str1.Length + 1));
            
            return chars.ToString();
        }
    }
}
