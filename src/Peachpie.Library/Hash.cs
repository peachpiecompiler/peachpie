using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    }
}
