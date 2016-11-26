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

        public static string[] EmptyStrings = Empty<string>();

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
    }

}
