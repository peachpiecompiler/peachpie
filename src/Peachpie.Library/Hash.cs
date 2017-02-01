using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Pchp.Library.Streams;

namespace Pchp.Library
{
    /// <summary>
    /// PHP hash functions support.
    /// </summary>
    [PhpExtension("hash")]
    public static class Hash
    {
        /// <summary>
        /// Calculate the md5 hash of a string.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <param name="raw_output">If the optional raw_output is set to TRUE, then the md5 digest is instead returned in raw binary format with a length of 16.</param>
        /// <returns>Returns the hash as a 32-character hexadecimal number.</returns>
        public static PhpString md5(PhpString str, bool raw_output = false)
        {
            var bytes = MD5.Create().ComputeHash(str.ToBytes(Encoding.UTF8));
            return raw_output
                ? new PhpString(bytes)
                : new PhpString(StringUtils.BinToHex(bytes, string.Empty));
        }

        /// <summary>
        /// Calculate the SHA1 hash of a string of bytes.
        /// </summary>
        /// <param name="ctx">Runtime context used for unicode conversions.
        /// <param name="bytes">The string of bytes to compute SHA1 of.</param>
        /// a sequence of hexadecimal numbers.</param>
        /// <returns>md5 of <paramref name="bytes"/>.</returns>
        public static string sha1(Context ctx, PhpString bytes)
        {
            return StringUtils.BinToHex(SHA1.Create().ComputeHash(bytes.ToBytes(ctx)));
        }

        /// <summary>
        /// Calculate the SHA1 hash of a string of bytes.
        /// </summary>
        /// <param name="ctx">Runtime context used for unicode conversions.</param>
        /// <param name="bytes">The string of bytes to compute SHA1 of.</param>
        /// <param name="rawOutput">If <B>true</B>, returns raw binary hash, otherwise returns hash as 
        /// a sequence of hexadecimal numbers.</param>
        /// <returns>md5 of <paramref name="bytes"/>.</returns>
        public static PhpString sha1(Context ctx, PhpString bytes, bool rawOutput = false)
        {
            return HashBytes(ctx, SHA1.Create(), bytes, rawOutput);
        }

        /// <summary>
        /// Computes a hash of a string of bytes using specified algorithm.
        /// </summary>
        static PhpString HashBytes(Context ctx, HashAlgorithm/*!*/ algorithm, PhpString bytes, bool rawOutput = false)
        {
            if (bytes == null) return null;

            byte[] hash = algorithm.ComputeHash(bytes.ToBytes(ctx));

            return (rawOutput)
                ? new PhpString(hash)
                : new PhpString(StringUtils.BinToHex(hash, null));
        }

        /// <summary>
        /// Computes a hash of a file using specified algorithm.
        /// </summary>
        static PhpString HashFromFile(Context ctx, HashAlgorithm/*!*/ algorithm, string fileName, bool rawOutput = false)
        {
            byte[] hash;

            try
            {
                using (PhpStream stream = PhpStream.Open(ctx, fileName, "rb", StreamOpenOptions.Empty, StreamContext.Default))
                //using (FileStream file = new FileStream(ctx, fileName, FileMode.Open, FileAccess.Read))
                {
                    if (stream == null)
                        return null;

                    var data = stream.ReadContents();
                    if (data.IsNull)
                    {
                        return null;
                    }

                    var bytes = data.AsBytes(ctx.StringEncoding);
                    if (bytes == null)
                    {
                        return null;
                    }

                    hash = algorithm.ComputeHash(bytes);
                }
            }
            catch (System.Exception)
            {
                return null;
            }

            return rawOutput
                ? new PhpString(hash)
                : new PhpString(StringUtils.BinToHex(hash));
        }
    }
}
