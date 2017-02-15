using System;
using System.Diagnostics;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Peachpie.Library.Graphics
{
    internal static class Utils
    {
        /// <summary>
        /// Open stream using working directory and PHP include directories.
        /// </summary>
        internal static System.IO.Stream OpenStream(Context ctx, string filename)
        {
            var stream = PhpStream.Open(ctx, filename, "rb", StreamOpenOptions.Empty, StreamContext.Default);
            if (stream == null)
                return null;

            return stream.RawStream;
        }

        /// <summary>
        /// Reads PhpBytes from file using the PhpStream.
        /// </summary>
        internal static byte[] ReadPhpBytes(Context ctx, string filename)
        {
            byte[] bytes;

            using (PhpStream stream = PhpStream.Open(ctx, filename, "rb", StreamOpenOptions.Empty, StreamContext.Default))
            {
                if (stream == null)
                    return null;

                try
                {
                    var element = stream.ReadContents();
                    if (element.IsNull)
                        return null;

                    bytes = element.AsBytes(ctx.StringEncoding);
                }
                catch
                {
                    return null;
                }
            }

            return bytes;
        }

        /// <summary>
        /// Tests if specified portions of two byte arrays are equal
        /// </summary>
        /// <param name="array1">First array. Cannot be <c>null</c> reference.</param>
        /// <param name="array2">Second array. Cannot be <c>null</c> reference.</param>
        /// <param name="length">Amount of bytes to compare.</param>
        /// <returns>returns true if both arrays are equal</returns>
        internal static bool ByteArrayCompare(byte[]/*!*/array1, byte[]/*!*/array2, int length)
        {
            //int max = (array1.Length > array2.Length) ? array1.Length : array2.Length;

            Debug.Assert(array1 != null);
            Debug.Assert(array2 != null);

            if (array1.Length < length || array2.Length < length)
            {
                return false;
            }

            try
            {
                for (int i = 0; i < length; i++)
                {
                    if (array1[i] != array2[i])
                        return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
