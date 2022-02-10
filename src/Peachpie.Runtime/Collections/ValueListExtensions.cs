using System;
using System.Collections.Generic;
using System.Text;

namespace Pchp.Core.Collections
{
    /// <summary>
    /// Extension method for <see cref="ValueList{T}"/>.
    /// </summary>
    public static class ValueListExtensions
    {
        /// <summary>
        /// Concatenates all the elements of a string list, using the specified separator between each element.
        /// </summary>
        public static string Join(this ValueList<string> values, string separator)
        {
            values.GetArraySegment(out var array, out var count);
            return string.Join(separator, array, 0, count);
        }

        /// <summary>
        /// Decodes string to bytes using given encoding. The result is added efficiently into the <see cref="ValueList{T}"/>.
        /// </summary>
        public static void AddBytes(this ref ValueList<byte> buffer, string @string, Encoding encoding)
        {
            if (string.IsNullOrEmpty(@string))
            {
                return;
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            //
            var maxbytes = encoding.GetMaxByteCount(@string.Length);
            var insertAt = buffer.Count;
            buffer.GetArraySegment(insertAt + maxbytes, out var array, out _);

            //
            buffer.Count += encoding.GetBytes(@string, 0, @string.Length, array, insertAt);
        }

        /// <summary>
        /// Decodes chars to bytes using given encoding. The result is added efficiently into the <see cref="ValueList{T}"/>.
        /// </summary>
        public static void AddBytes(this ref ValueList<byte> buffer, char[] chars, int index, int count, Encoding encoding)
        {
            if (count == 0)
            {
                return;
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (index < 0 || count < 0 || index + count > chars.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            //
            var bytes = encoding.GetByteCount(chars, index, count);
            var insertAt = buffer.Count;
            buffer.GetArraySegment(insertAt + bytes, out var array, out _);

            //
            buffer.Count += encoding.GetBytes(chars, index, count, array, insertAt);
        }
    }
}
