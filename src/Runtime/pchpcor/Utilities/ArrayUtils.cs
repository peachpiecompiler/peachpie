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

        public static readonly PhpValue[] EmptyValues = new PhpValue[0];

        #endregion

        public static void Write(this Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);

        public static string GetString(this Encoding encoding, byte[] bytes) => encoding.GetString(bytes, 0, bytes.Length);
    }
}
