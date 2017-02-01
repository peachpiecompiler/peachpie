using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComponentAce.Compression.Libs.zlib;
using Pchp.Core;

namespace Pchp.Library
{
    internal class PhpZlibResource : PhpResource
    {
        public PhpZlibResource()
            : base("zlib") // TODO
        { }
    }

    [PhpExtension("zlib")]
    public static class zlib
    {
        #region Constants

        /// <summary>
        /// Zlib force* constants.
        /// </summary>
        enum ForceConstants
        {
            FORCE_GZIP = 1,

            FORCE_DEFLATE = 2,
        }

        public const int FORCE_GZIP = (int)ForceConstants.FORCE_GZIP;
        public const int FORCE_DEFLATE = (int)ForceConstants.FORCE_DEFLATE;

        #endregion

        internal static readonly byte[] GZIP_HEADER = new byte[] { 0x1f, 0x8b };
        internal const byte GZIP_HEADER_EXTRAFIELD = 4;
        internal const byte GZIP_HEADER_FILENAME = 8;
        internal const byte GZIP_HEADER_COMMENT = 16;
        internal const byte GZIP_HEADER_CRC = 2;
        internal const byte Z_DEFLATED = 8;
        internal const byte GZIP_HEADER_RESERVED_FLAGS = 0xe0;
        internal const byte OS_CODE = 0x03;
        internal const int MAX_WBITS = 15;
        internal const int PHP_ZLIB_MODIFIER = 100;

        internal const int GZIP_HEADER_LENGTH = 10;
        internal const int GZIP_FOOTER_LENGTH = 8;

        internal static string z_error = string.Empty;

        internal static string zError(int status)
        {
            return z_error;
        }

        #region gzdeflate, gzinflate
        
        /// <summary>
        /// This function compress the given string using the DEFLATE data format.
        /// </summary>
        /// <param name="data">The data to deflate.</param>
        /// <param name="level">
        ///     The level of compression. Can be given as 0 for no compression up to 9 for maximum compression.
        ///     If not given, the default compression level will be the default compression level of the zlib library.
        /// </param>
        /// <returns>The deflated string or FALSE if an error occurred.</returns>
        [return: CastToFalse]
        public static PhpString gzdeflate(byte[] data, int level = -1)
        {
            if (level < -1 || level > 9)
            {
                PhpException.Throw(PhpError.Warning, String.Format("compression level ({0}) must be within -1..9", level));
                return null;
            }

            var zs = new ZStream();
            
            zs.next_in = data;
            zs.avail_in = data.Length;

            // heuristic for max data length
            zs.avail_out = data.Length + data.Length / PHP_ZLIB_MODIFIER + 15 + 1;
            zs.next_out = new byte[zs.avail_out];

            // -15 omits the header (undocumented feature of zlib)
            int status = zs.deflateInit(level, -MAX_WBITS);

            if (status == zlibConst.Z_OK)
            {
                status = zs.deflate(zlibConst.Z_FINISH);
                if (status != zlibConst.Z_STREAM_END)
                {
                    zs.deflateEnd();
                    if (status == zlibConst.Z_OK)
                    {
                        status = zlibConst.Z_BUF_ERROR;
                    }
                }
                else
                {
                    status = zs.deflateEnd();
                }
            }

            if (status == zlibConst.Z_OK)
            {
                byte[] result = new byte[zs.total_out];
                Buffer.BlockCopy(zs.next_out, 0, result, 0, (int)zs.total_out);
                return new PhpString(result);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return null;
            }
        }

        /// <summary>
        /// This function inflate a deflated string.
        /// </summary> 
        /// <param name="data">The data compressed by gzdeflate().</param>
        /// <param name="length">The maximum length of data to decode.</param>
        /// <returns>
        ///     <para>
        ///         The original uncompressed data or FALSE on error.
        ///     </para>
        ///     <para>
        ///         The function will return an error if the uncompressed data is more than 32768 times the length of 
        ///         the compressed input data or more than the optional parameter length.
        ///     </para>
        /// </returns>
        [return: CastToFalse]
        public static PhpString gzinflate(byte[] data, long length = 0)
        {
            uint factor = 1, maxfactor = 16;
            long ilength;

            var zs = new ZStream();

            zs.avail_in = data.Length;
            zs.next_in = data;
            zs.total_out = 0;

            // -15 omits the header (undocumented feature of zlib)
            int status = zs.inflateInit(-15);

            if (status != zlibConst.Z_OK)
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return null;
            }

            do
            {
                ilength = length != 0 ? length : data.Length * (1 << (int)(factor++));

                try
                {
                    byte[] newOutput = new byte[ilength];

                    if (zs.next_out != null)
                    {
                        Buffer.BlockCopy(zs.next_out, 0, newOutput, 0, zs.next_out.Length);
                    }

                    zs.next_out = newOutput;
                }
                catch (OutOfMemoryException)
                {
                    zs.inflateEnd();
                    return null;
                }

                zs.next_out_index = (int)zs.total_out;
                zs.avail_out = unchecked((int)(ilength - zs.total_out));
                status = zs.inflate(zlibConst.Z_NO_FLUSH);
            }
            while ((status == zlibConst.Z_BUF_ERROR || (status == zlibConst.Z_OK && (zs.avail_in != 0 || zs.avail_out == 0))) && length == 0 && factor < maxfactor);

            zs.inflateEnd();

            if ((length != 0 && status == zlibConst.Z_OK) || factor >= maxfactor)
            {
                status = zlibConst.Z_MEM_ERROR;
            }

            if (status == zlibConst.Z_STREAM_END || status == zlibConst.Z_OK)
            {
                byte[] result = new byte[zs.total_out];
                Buffer.BlockCopy(zs.next_out, 0, result, 0, (int)zs.total_out);
                return new PhpString(result);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return null;
            }
        }

        #endregion
    }
}
