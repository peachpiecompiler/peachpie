using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ComponentAce.Compression.Libs.zlib;
using Pchp.Core;
using Pchp.Library.Streams;
using static Pchp.Library.StandardPhpOptions;

namespace Pchp.Library
{
    internal class PhpZlibResource : PhpResource
    {
        public PhpZlibResource()
            : base("zlib") // TODO
        { }
    }

    [PhpExtension("zlib", Registrator = typeof(Zlib.Registrator))]
    public static class Zlib
    {
        internal sealed class Registrator
        {
            public Registrator()
            {
                PhpFilter.AddFilterFactory(new ZlibFilterFactory());
                StreamWrapper.RegisterSystemWrapper(new ZlibStreamWrapper());
                RegisterLegacyOptions();
            }

            /// <summary>
            /// Gets, sets, or restores a value of a legacy configuration option.
            /// </summary>
            private static PhpValue GetSet(Context ctx, IPhpConfigurationService config, string option, PhpValue value, IniAction action)
            {
                switch (option)
                {
                    case "zlib.output_compression":
                        Debug.Assert(action == IniAction.Get, "Setting zlib.output_compression is not currently implemented.");
                        return PhpValue.False;

                    case "zlib.output_compression_level":
                        Debug.Assert(action == IniAction.Get, "Setting zlib.output_compression_level is not currently implemented.");
                        return PhpValue.Create(-1);

                    case "zlib.output_handler":
                        Debug.Assert(action == IniAction.Get, "Setting zlib.output_handler is not currently implemented.");
                        return PhpValue.Create("");
                }

                Debug.Fail("Option '" + option + "' is not currently supported.");
                return PhpValue.Null;
            }

            /// <summary>
            /// Registers legacy ini-options.
            /// </summary>
            static void RegisterLegacyOptions()
            {
                const string s = "zlib";
                GetSetDelegate d = new GetSetDelegate(GetSet);

                Register("zlib.output_compression", IniFlags.Supported | IniFlags.Global, d, s);
                Register("zlib.output_compression_level", IniFlags.Supported | IniFlags.Global, d, s);
                Register("zlib.output_handler", IniFlags.Supported | IniFlags.Global, d, s);
            }
        }

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

        internal static string zError(int status)
        {
            return null; // z_error;
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
                return default(PhpString);
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
                return default(PhpString);
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
                return default(PhpString);
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
                    return default(PhpString);
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
                PhpException.Throw(PhpError.Warning, zError(status) ?? zs.msg);
                return default(PhpString);
            }
        }

        #endregion

        #region gzclose, gzopen

        /// <summary>
        /// Closes the given gz-file pointer.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool gzclose(PhpResource zp)
        {
            return PhpPath.fclose(zp);
        }

        /// <summary>
        /// Opens a gzip (.gz) file for reading or writing.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="filename">The file name.</param>
        /// <param name="mode">
        ///     As in fopen() (rb or wb) but can also include a compression level (wb9) or a strategy: f for filtered data as
        ///     in wb6f, h for Huffman only compression as in wb1h.
        /// </param>
        /// <param name="use_include_path">
        ///     You can set this optional parameter to 1, if you want to search for the file in the include_path too.
        /// </param>
        /// <returns>
        ///     <para>Returns a file pointer to the file opened, after that, everything you read from this file descriptor will be 
        ///     transparently decompressed and what you write gets compressed.</para>
        ///     <para>If the open fails, the function returns FALSE.</para>
        /// </returns>
        public static PhpResource gzopen(Context ctx, string filename, string mode, int use_include_path = 0)
        {
            return new ZlibStreamWrapper().Open(ctx,
                ref filename,
                mode,
                use_include_path == 1 ? StreamOpenOptions.UseIncludePath : StreamOpenOptions.Empty,
                null);
        }

        /// <summary>
        /// Calls gzopen
        /// Opens a gzip (.gz) file for reading or writing.
        /// Some versions of linux have the function named differently, which is used in some PHP libraries
        /// </summary>
        public static PhpResource gzopen64(Context ctx, string filename, string mode, int use_include_path = 0) => gzopen(ctx, filename, mode, use_include_path);

        #endregion

        #region gzcompress, gzuncompress

        /// <summary>
        /// This function compress the given string using the ZLIB data format.
        /// </summary> 
        /// <param name="data">The data to compress.</param>
        /// <param name="level">The level of compression. Can be given as 0 for no compression up to 9 for maximum compression.</param>
        /// <returns>The compressed string or FALSE if an error occurred.</returns>
        [return: CastToFalse]
        public static PhpString gzcompress(byte[] data, int level = -1)
        {
            if ((level < -1) || (level > 9))
            {
                PhpException.Throw(PhpError.Warning, String.Format("compression level ({0}) must be within -1..9", level));
                return default(PhpString);
            }

            if (data == null)
            {
                data = Array.Empty<byte>();
            }

            int length_bound = data.Length + (data.Length / PHP_ZLIB_MODIFIER) + 15 + 1;

            byte[] output;

            try
            {
                output = new byte[length_bound];
            }
            catch (OutOfMemoryException)
            {
                return default(PhpString);
            }

            int status;

            status = ZlibCompress(ref output, data, level);

            if (status == zlibConst.Z_OK)
            {
                return new PhpString(output);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status));
                return default(PhpString);
            }
        }

        /// <summary>
        /// This function uncompress a compressed string.
        /// </summary>
        /// <param name="data">The data compressed by gzcompress().</param>
        /// <param name="length">The maximum length of data to decode.</param>
        /// <returns>
        ///     <para>
        ///         The original uncompressed data or FALSE on error.
        ///     </para>
        ///     <para>
        ///         The function will return an error if the uncompressed data is more than 32768 times the length of the compressed
        ///         input data or more than the optional parameter length.
        ///     </para>
        /// </returns>
        [return: CastToFalse]
        public static PhpString gzuncompress(byte[] data, int length = 0)
        {
            if (length < 0)
            {
                PhpException.Throw(PhpError.Warning, String.Format("length {0} must be greater or equal zero", length));
                return default(PhpString);
            }

            int ilength;
            int factor = 1, maxfactor = 16;
            byte[] output;
            int status;
            string msg;

            do
            {
                ilength = length != 0 ? length : (data.Length * (1 << factor++));

                try
                {
                    output = new byte[ilength];
                }
                catch (OutOfMemoryException)
                {
                    return default(PhpString);
                }

                status = ZlibUncompress(ref output, data, out msg);
            }
            while ((status == zlibConst.Z_BUF_ERROR) && (length == 0) && (factor < maxfactor));

            if (status == zlibConst.Z_OK)
            {
                return new PhpString(output);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status) ?? msg);
                return default(PhpString);
            }
        }

        /// <summary>
        /// Reimplements function from zlib (compress2) that is not present in ZLIB.NET.
        /// </summary>
        /// <param name="dest">Destination array of bytes. May be trimmed if necessary.</param>
        /// <param name="source">Source array of bytes.</param>
        /// <param name="level">Level of compression.</param>
        /// <returns>Zlib status code.</returns>
        static int ZlibCompress(ref byte[] dest, byte[] source, int level)
        {
            ZStream stream = new ZStream();
            int err;

            stream.next_in = source;
            stream.avail_in = source.Length;
            stream.next_out = dest;
            stream.avail_out = dest.Length;

            err = stream.deflateInit(level);
            if (err != zlibConst.Z_OK) return err;

            err = stream.deflate(zlibConst.Z_FINISH);
            if (err != zlibConst.Z_STREAM_END)
            {
                stream.deflateEnd();
                return err == zlibConst.Z_OK ? zlibConst.Z_BUF_ERROR : err;
            }

            if (stream.total_out != dest.Length)
            {
                byte[] output = new byte[stream.total_out];
                Buffer.BlockCopy(stream.next_out, 0, output, 0, (int)stream.total_out);
                dest = output;
            }

            return stream.deflateEnd();
        }

        /// <summary>
        /// Reimplements function from zlib (uncompress) that is not present in ZLIB.NET.
        /// </summary>
        /// <param name="dest">Destination array of bytes. May be trimmed if necessary.</param>
        /// <param name="source">Source array of bytes.</param>
        /// <param name="msg">Eventual message from zstream.</param>
        /// <returns>Zlib status code.</returns>
        static int ZlibUncompress(ref byte[] dest, byte[] source, out string msg)
        {
            var zs = new ZStream();
            int err;

            zs.next_in = source;
            zs.avail_in = source.Length;
            zs.next_out = dest;
            zs.avail_out = dest.Length;

            err = zs.inflateInit();
            if (err != zlibConst.Z_OK)
            {
                msg = zs.msg;
                return err;
            }

            err = zs.inflate(zlibConst.Z_FINISH);
            if (err != zlibConst.Z_STREAM_END)
            {
                zs.inflateEnd();
                msg = zs.msg;
                return err == zlibConst.Z_OK ? zlibConst.Z_BUF_ERROR : err;
            }

            if (zs.total_out != dest.Length)
            {
                byte[] output = new byte[zs.total_out];
                Buffer.BlockCopy(zs.next_out, 0, output, 0, (int)zs.total_out);
                dest = output;
            }

            msg = zs.msg;

            return zs.inflateEnd();
        }

        #endregion

        #region gzdecode, gzencode

        /// <summary>
        /// This function returns a decoded version of the input data.
        /// </summary>
        /// <param name="data">The data to decode, encoded by gzencode().</param>
        /// <param name="length">The maximum length of data to decode.</param>
        /// <returns>The decoded string, or FALSE if an error occurred.</returns>
        [return: CastToFalse]
        public static PhpString gzdecode(byte[] data, int length = 0)
        {
            if (length < 0)
            {
                PhpException.Throw(PhpError.Warning, "length ({0}) must be greater or equal zero", length.ToString());
                return default(PhpString);
            }

            if (data.Length == 0)
            {
                return default(PhpString);
            }

            if (data.Length < 10 || data[0] != GZIP_HEADER[0] || data[1] != GZIP_HEADER[1])
            {
                PhpException.Throw(PhpError.Warning, "incorrect gzip header");
                return default(PhpString);
            }

            var zs = new ZStream();
            zs.next_in = data;
            zs.next_in_index = GZIP_HEADER_LENGTH;
            zs.avail_in = data.Length - GZIP_HEADER_LENGTH;
            zs.total_out = 0;

            // negative number omits the zlib header (undocumented feature of zlib)
            int status = zs.inflateInit(-MAX_WBITS);
            if (status == zlibConst.Z_OK)
            {

                status = zlib_inflate_rounds(zs, length, out byte[] output);
                if (status == zlibConst.Z_STREAM_END)
                {
                    zs.inflateEnd();
                    return new PhpString(output);
                }
            }

            PhpException.Throw(PhpError.Warning, zError(status) ?? zs.msg);
            zs.inflateEnd();

            return default(PhpString);
        }

        private static int zlib_inflate_rounds(ZStream zs, int max, out byte[] output)
        {
            int status = zlibConst.Z_OK;
            int round = 0;

            output = null;
            int bufferFree = 0;
            int bufferSize = (max != 0 && max < zs.avail_in) ? max : zs.avail_in;
            int bufferUsed = 0;

            do
            {
                if (max != 0 && max <= bufferUsed)
                {
                    status = zlibConst.Z_MEM_ERROR;
                }

                try
                {
                    Array.Resize(ref output, bufferSize);
                }
                catch (OutOfMemoryException)
                {
                    status = zlibConst.Z_MEM_ERROR;
                }

                if (status != zlibConst.Z_MEM_ERROR)
                {
                    bufferFree = bufferSize - bufferUsed;
                    zs.avail_out = bufferFree;
                    zs.next_out = output;
                    zs.next_out_index = bufferUsed;

                    status = zs.inflate(zlibConst.Z_NO_FLUSH);

                    bufferUsed += bufferFree - zs.avail_out;
                    bufferFree = zs.avail_out;

                    bufferSize += (bufferSize >> 3) + 1;
                }
            }
            while ((status == zlibConst.Z_BUF_ERROR || (status == zlibConst.Z_OK && zs.avail_in != 0)) && ++round < 100);

            if (status == zlibConst.Z_STREAM_END)
            {
                Array.Resize(ref output, bufferUsed);
            }
            else
            {
                status = (status == zlibConst.Z_OK) ? zlibConst.Z_DATA_ERROR : status;
            }

            return status;
        }

        /// <summary>
        /// This function returns a compressed version of the input data compatible with the output of the gzip program.
        /// </summary>
        /// <param name="data">The data to encode.</param>
        /// <param name="level">
        ///     The level of compression. Can be given as 0 for no compression up to 9 for maximum compression. If not 
        ///     given, the default compression level will be the default compression level of the zlib library.
        /// </param>
        /// <param name="encoding_mode">
        ///     <para>The encoding mode. Can be FORCE_GZIP (the default) or FORCE_DEFLATE.</para>
        ///     <para>
        ///         If you use FORCE_DEFLATE, you get a standard zlib deflated string (inclusive zlib headers) after 
        ///         the gzip file header but without the trailing crc32 checksum.
        ///     </para>
        /// </param>
        /// <returns>The encoded string, or FALSE if an error occurred.</returns>
        [return: CastToFalse]
        public static PhpString gzencode(byte[] data, int level = -1, int encoding_mode = FORCE_GZIP)
        {
            if ((level < -1) || (level > 9))
            {
                PhpException.Throw(PhpError.Warning, "compression level ({0}) must be within -1..9", level.ToString());
                return default(PhpString);
            }

            ZStream zs = new ZStream();
            int status = zlibConst.Z_OK;

            zs.next_in = data;
            zs.avail_in = data.Length;

            // heuristic for max data length
            zs.avail_out = data.Length + data.Length / Zlib.PHP_ZLIB_MODIFIER + 15 + 1;
            zs.next_out = new byte[zs.avail_out];

            switch (encoding_mode)
            {
                case (int)ForceConstants.FORCE_GZIP:
                    if ((status = zs.deflateInit(level, -MAX_WBITS)) != zlibConst.Z_OK)
                    {
                        PhpException.Throw(PhpError.Warning, zError(status));
                        return default(PhpString);
                    }
                    break;
                case (int)ForceConstants.FORCE_DEFLATE:
                    if ((status = zs.deflateInit(level)) != zlibConst.Z_OK)
                    {
                        PhpException.Throw(PhpError.Warning, zError(status));
                        return default(PhpString);
                    }
                    break;
            }

            status = zs.deflate(zlibConst.Z_FINISH);

            if (status != zlibConst.Z_STREAM_END)
            {
                zs.deflateEnd();

                if (status == zlibConst.Z_OK)
                {
                    status = zlibConst.Z_STREAM_ERROR;
                }
            }
            else
            {
                status = zs.deflateEnd();
            }

            if (status == zlibConst.Z_OK)
            {
                long output_length = zs.total_out + (encoding_mode == (int)ForceConstants.FORCE_GZIP ? GZIP_HEADER_LENGTH + GZIP_FOOTER_LENGTH : GZIP_HEADER_LENGTH);
                long output_offset = GZIP_HEADER_LENGTH;

                byte[] output = new byte[output_length];
                Buffer.BlockCopy(zs.next_out, 0, output, (int)output_offset, (int)zs.total_out);

                // fill the header
                output[0] = GZIP_HEADER[0];
                output[1] = GZIP_HEADER[1];
                output[2] = Z_DEFLATED; // zlib constant (private in ZLIB.NET)
                output[3] = 0; // reserved flag bits (this function puts invalid flags in here)
                // 4-8 represent time and are set to zero
                output[9] = OS_CODE; // php constant

                if (encoding_mode == (int)ForceConstants.FORCE_GZIP)
                {
                    byte[] crc = new PhpHash.HashPhpResource.CRC32().ComputeHash(data);

                    output[output_length - 8] = crc[0];
                    output[output_length - 7] = crc[1];
                    output[output_length - 6] = crc[2];
                    output[output_length - 5] = crc[3];
                    output[output_length - 4] = (byte)(zs.total_in & 0xFF);
                    output[output_length - 3] = (byte)((zs.total_in >> 8) & 0xFF);
                    output[output_length - 2] = (byte)((zs.total_in >> 16) & 0xFF);
                    output[output_length - 1] = (byte)((zs.total_in >> 24) & 0xFF);
                }

                return new PhpString(output);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, zError(status) ?? zs.msg);
                return default(PhpString);
            }
        }

        #endregion

        #region gzeof, gzrewind, gzseek, gztell

        /// <summary>
        /// Tests the given GZ file pointer for EOF.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>Returns TRUE if the gz-file pointer is at EOF or an error occurs; otherwise returns FALSE.</returns>
        public static bool gzeof(PhpResource zp)
        {
            return PhpPath.feof(zp);
        }

        /// <summary>
        /// Sets the file position indicator of the given gz-file pointer to the beginning of the file stream.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool gzrewind(PhpResource zp)
        {
            return PhpPath.rewind(zp);
        }

        /// <summary>
        ///     <para>
        ///         Sets the file position indicator for the given file pointer to the given offset byte into the file stream. Equivalent
        ///         to calling (in C) gzseek(zp, offset, SEEK_SET).
        ///     </para>
        ///     <para>
        ///         If the file is opened for reading, this function is emulated but can be extremely slow. If the file is opened for 
        ///         writing, only forward seeks are supported; gzseek() then compresses a sequence of zeroes up to the new starting position. 
        ///     </para>
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="offset">The seeked offset.</param>
        /// <param name="whence">
        ///     whence values are: SEEK_SET (relative to origin), SEEK_CUR (relative to current position).
        /// </param>
        /// <returns>Upon success, returns 0; otherwise, returns -1. Note that seeking past EOF is not considered an error.</returns>
        public static int gzseek(PhpResource zp, int offset, int whence = PhpStreams.SEEK_SET)
        {
            return PhpPath.fseek(zp, offset, whence);
        }

        /// <summary>
        /// Calls gzseek
        /// Some versions of linux have the function named differently, which is used in some PHP libraries
        /// </summary>
        public static int gzseek64(PhpResource zp, int offset, int whence = PhpStreams.SEEK_SET) => gzseek(zp, offset, whence);

        /// <summary>
        /// Gets the position of the given file pointer; i.e., its offset into the uncompressed file stream.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>The position of the file pointer or FALSE if an error occurs.</returns>
        public static int gztell(PhpResource zp)
        {
            return PhpPath.ftell(zp);
        }

        /// <summary>
        /// Gets the position of the given file pointer; i.e., its offset into the uncompressed file stream.
        /// 
        /// Some versions of linux have the function named differently, which is used in some PHP libraries
        /// </summary>
        public static object gztell64(PhpResource zp) => gztell(zp);

        #endregion

        #region gzfile

        /// <summary>
        /// This function is identical to readgzfile(), except that it returns the file in an array.
        /// </summary>
        /// <param name="ctx">Current script context, passed automatically by the caller.</param>
        /// <param name="filename">The file name.</param>
        /// <param name="use_include_path">
        ///     You can set this optional parameter to 1, if you want to search for the file in the include_path too.
        /// </param>
        /// <returns>An array containing the file, one line per cell.</returns>
        [return: CastToFalse]
        public static PhpArray gzfile(Context ctx, string filename, int use_include_path = 0)
        {
            var fs = (PhpStream)gzopen(ctx, filename, "r", use_include_path);
            if (fs == null)
            {
                return null;
            }

            var returnValue = new PhpArray();
            int blockLength = 8192;

            while (!fs.Eof)
            {
                var value = fs.ReadData(blockLength, true).AsText(ctx.StringEncoding);
                returnValue.Add(/*Core.Convert.Quote*/(value));
            }

            return returnValue;
        }

        #endregion

        #region gzgetc, gzgets, gzgetss

        /// <summary>
        /// Returns a string containing a single (uncompressed) character read from the given gz-file pointer.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>The uncompressed character or FALSE on EOF (unlike gzeof()).</returns>
        [return: CastToFalse]
        public static PhpString gzgetc(Context ctx, PhpResource zp)
        {
            return PhpPath.fgetc(ctx, zp);
        }

        /// <summary>
        /// Gets a (uncompressed) string of up to length - 1 bytes read from the given file pointer. Reading ends when length - 1 bytes 
        /// have been read, on a newline, or on EOF (whichever comes first). 
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="length">The length of data to get.</param>
        /// <returns>The uncompressed string, or FALSE on error.</returns>
        [return: CastToFalse]
        public static PhpString gzgets(PhpResource zp, int length = -1)
        {
            return (length == -1) ? PhpPath.fgets(zp) : PhpPath.fgets(zp, length);
        }

        /// <summary>
        /// Identical to gzgets(), except that gzgetss() attempts to strip any HTML and PHP tags from the text it reads.
        /// </summary>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="length">The length of data to get.</param>
        /// /// <param name="allowable_tags">You can use this optional parameter to specify tags which should not be stripped.</param>
        /// <returns>The uncompressed and striped string, or FALSE on error.</returns>
        [return: CastToFalse]
        public static string gzgetss(PhpResource zp, int length = -1, string allowable_tags = null)
        {
            return length < 0
                ? PhpPath.fgetss(zp)
                : PhpPath.fgetss(zp, length, allowable_tags);
        }

        #endregion

        #region gzpassthru, gzputs

        /// <summary>
        /// Reads to EOF on the given gz-file pointer from the current position and writes the (uncompressed) results to standard output.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <returns>The number of uncompressed characters read from gz and passed through to the input, or FALSE on error.</returns>
        public static int gzpassthru(Context ctx, PhpResource zp)
        {
            return PhpPath.fpassthru(ctx, zp);
        }

        /// <summary>
        /// This function is an alias of gzwrite(), which writes the contents of string to the given gz-file.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="str">The string to write.</param>
        /// <param name="length">
        ///     The number of uncompressed bytes to write. If supplied, writing will stop after length (uncompressed) bytes have been 
        ///     written or the end of string is reached, whichever comes first.
        /// </param>
        /// <returns>Returns the number of (uncompressed) bytes written to the given gz-file stream.</returns>
        public static int gzputs(Context ctx, PhpResource zp, PhpString str, int length = -1)
        {
            return gzwrite(ctx, zp, str, length);
        }

        #endregion

        #region gzread, gzwrite

        /// <summary>
        /// Reads up to length bytes from the given gz-file pointer. Reading stops when length (uncompressed) bytes 
        /// have been read or EOF is reached, whichever comes first.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The data that have been read.</returns>
        public static PhpString gzread(Context ctx, PhpResource zp, int length)
        {
            return PhpPath.fread(ctx, zp, length);
        }

        /// <summary>
        /// Writes the contents of string to the given gz-file.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="zp">The gz-file pointer. It must be valid, and must point to a file successfully opened by gzopen().</param>
        /// <param name="str">The string to write.</param>
        /// <param name="length">
        ///     The number of uncompressed bytes to write. If supplied, writing will stop after length (uncompressed) bytes have been 
        ///     written or the end of string is reached, whichever comes first.
        /// </param>
        /// <returns>Returns the number of (uncompressed) bytes written to the given gz-file stream.</returns>
        public static int gzwrite(Context ctx, PhpResource zp, PhpString str, int length = -1)
        {
            return PhpPath.fwrite(ctx, zp, str, length);
        }

        #endregion

        #region readgzfile

        /// <summary>
        /// Reads a file, decompresses it and writes it to standard output.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="filename">
        ///     The file name. This file will be opened from the filesystem and its contents written to standard output.
        /// </param>
        /// <param name="use_include_path">
        ///     You can set this optional parameter to 1, if you want to search for the file in the include_path too.
        /// </param>
        /// <returns>
        ///     Returns the number of (uncompressed) bytes read from the file. If an error occurs, FALSE is returned and 
        ///     unless the function was called as @readgzfile, an error message is printed.
        /// </returns>
        public static int readgzfile(Context ctx, string filename, int use_include_path = 0)
        {
            var fs = (PhpStream)gzopen(ctx, filename, "r", use_include_path);
            return PhpStreams.stream_copy_to_stream(fs, InputOutputStreamWrapper.ScriptOutput(ctx));
        }

        #endregion

        #region zlib_get_coding_type

        /// <summary>
        /// Returns the coding type used for output compression.
        /// </summary>
        /// <returns>Possible return values are gzip, deflate, or FALSE.</returns>
        [return: CastToFalse]
        public static string zlib_get_coding_type()
        {
            PhpException.FunctionNotSupported("zlib_get_coding_type");
            return null;    // gzip, deflate, or FALSE.
        }

        #endregion
    }
}
