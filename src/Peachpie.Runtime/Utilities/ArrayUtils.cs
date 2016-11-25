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

        public static void Write(this Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);

        public static string GetString(this Encoding encoding, byte[] bytes) => encoding.GetString(bytes, 0, bytes.Length);

        static readonly Func<object, bool> _not_null = new Func<object, bool>((obj) => obj != null);

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> e) where T : class
        {
            return (IEnumerable<T>)e.Where(_not_null);
        }
    }

}
