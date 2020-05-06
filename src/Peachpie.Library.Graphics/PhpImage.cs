using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;

namespace Peachpie.Library.Graphics
{
    [PhpExtension("image")]
    public static class PhpImage
    {
        #region ImageType

        /// <summary>
        /// Image types enumeration, corresponds to IMAGETYPE_ PHP constants.
        /// </summary>
        [PhpHidden]
        public enum ImageType
        {
            /// <summary></summary>
            Unknown = 0,
            /// <summary></summary>
            GIF = 1,
            /// <summary></summary>
            JPEG = 2,
            /// <summary></summary>
            PNG = 3,
            /// <summary></summary>
            SWF = 4,
            /// <summary></summary>
            PSD = 5,
            /// <summary></summary>
            BMP = 6,
            /// <summary></summary>
            TIFF_II = 7,
            /// <summary></summary>
            TIFF_MM = 8,
            /// <summary></summary>
            JPC = 9,
            /// <summary></summary>
            JPEG2000 = 9,
            /// <summary></summary>
            JP2 = 10,
            /// <summary></summary>
            JPX = 11,
            /// <summary></summary>
            JB2 = 12,
            /// <summary></summary>
            SWC = 13,
            /// <summary></summary>
            IFF = 14,
            /// <summary></summary>
            WBMP = 15,
            /// <summary></summary>
            XBM = 16,
            /// <summary></summary>
            ICO = 17,
            /// <summary></summary>
            WEBP = 18,

            /// <summary>Number of values.</summary>
            Count,
        }

        #endregion

        #region PHP constants

        public const int IMAGETYPE_UNKNOWN = (int)ImageType.Unknown;
        public const int IMAGETYPE_GIF = (int)ImageType.GIF;
        public const int IMAGETYPE_JPEG = (int)ImageType.JPEG;
        public const int IMAGETYPE_PNG = (int)ImageType.PNG;
        public const int IMAGETYPE_SWF = (int)ImageType.SWF;
        public const int IMAGETYPE_PSD = (int)ImageType.PSD;
        public const int IMAGETYPE_BMP = (int)ImageType.BMP;
        public const int IMAGETYPE_TIFF_II = (int)ImageType.TIFF_II;
        public const int IMAGETYPE_TIFF_MM = (int)ImageType.TIFF_MM;
        public const int IMAGETYPE_JPC = (int)ImageType.JPC;
        public const int IMAGETYPE_JPEG2000 = (int)ImageType.JPEG2000;
        public const int IMAGETYPE_JP2 = (int)ImageType.JP2;
        public const int IMAGETYPE_JPX = (int)ImageType.JPX;
        public const int IMAGETYPE_JB2 = (int)ImageType.JB2;
        public const int IMAGETYPE_SWC = (int)ImageType.SWC;
        public const int IMAGETYPE_IFF = (int)ImageType.IFF;
        public const int IMAGETYPE_WBMP = (int)ImageType.WBMP;
        public const int IMAGETYPE_XBM = (int)ImageType.XBM;
        public const int IMAGETYPE_ICO = (int)ImageType.ICO;
        public const int IMAGETYPE_WEBP = (int)ImageType.WEBP;
        public const int IMAGETYPE_COUNT = (int)ImageType.Count;

        #endregion

        #region ImageSignature helper class

        internal static class ImageSignature
        {
            internal struct ImageInfo
            {
                public uint width, height, bits, channels;
                public PhpArray exif;
            }

            #region signatures
            static readonly byte[] sig_gif = { (byte)'G', (byte)'I', (byte)'F' };
            static readonly byte[] sig_psd = { (byte)'8', (byte)'B', (byte)'P', (byte)'S' };
            static readonly byte[] sig_bmp = { (byte)'B', (byte)'M' };
            static readonly byte[] sig_swf = { (byte)'F', (byte)'W', (byte)'S' };
            static readonly byte[] sig_swc = { (byte)'C', (byte)'W', (byte)'S' };
            static readonly byte[] sig_jpg = { 0xff, 0xd8, 0xff };
            static readonly byte[] sig_png = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
            static readonly byte[] sig_tif_ii = { (byte)'I', (byte)'I', 0x2A, 0x00 };
            static readonly byte[] sig_tif_mm = { (byte)'M', (byte)'M', 0x00, 0x2A };
            static readonly byte[] sig_jpc = { 0xff, 0x4f, 0xff };
            static readonly byte[] sig_jp2 = { 0x00, 0x00, 0x00, 0x0c, 0x6a, 0x50, 0x20, 0x20, 0x0d, 0x0a, 0x87, 0x0a };
            static readonly byte[] sig_iff = { (byte)'F', (byte)'O', (byte)'R', (byte)'M' };
            static readonly byte[] sig_ico = { (byte)0x00, (byte)0x00, (byte)0x01, 0x00 };

            static ImageType handle_gif(Stream/*!*/stream, ref ImageInfo info)
            {
                byte[] dim = new byte[5];

                stream.Seek(3, SeekOrigin.Current);
                if (stream.Read(dim, 0, dim.Length) != dim.Length)
                    return ImageType.Unknown;

                info.width = ((uint)dim[0] | (((uint)dim[1]) << 8));
                info.height = ((uint)dim[2] | (((uint)dim[3]) << 8));
                info.bits = (((dim[4] & 0x80) != 0) ? ((((uint)dim[4]) & 0x07) + 1) : 0);
                info.channels = 3; /* always */

                return ImageType.GIF;
            }

            static ImageType handle_psd(Stream/*!*/stream, ref ImageInfo info)
            {
                byte[] dim = new byte[8];

                stream.Seek(11, SeekOrigin.Current);
                if (stream.Read(dim, 0, dim.Length) != dim.Length)
                    return ImageType.Unknown;

                info.height = ((((uint)dim[0]) << 24) + (((uint)dim[1]) << 16) + (((uint)dim[2]) << 8) + ((uint)dim[3]));
                info.width = ((((uint)dim[4]) << 24) + (((uint)dim[5]) << 16) + (((uint)dim[6]) << 8) + ((uint)dim[7]));

                return ImageType.PSD;
            }

            static ImageType handle_bmp(Stream/*!*/stream, ref ImageInfo info)
            {
                byte[] dim = new byte[16];

                stream.Seek(11, SeekOrigin.Current);
                if (stream.Read(dim, 0, dim.Length) != dim.Length)
                    return ImageType.Unknown;

                uint size = (((uint)dim[3]) << 24) + (((uint)dim[2]) << 16) + (((uint)dim[1]) << 8) + ((uint)dim[0]);
                if (size == 12)
                {
                    info.width = (((uint)dim[5]) << 8) + ((uint)dim[4]);
                    info.height = (((uint)dim[7]) << 8) + ((uint)dim[6]);
                    info.bits = ((uint)dim[11]);
                }
                else if (size > 12 && (size <= 64 || size == 108))
                {
                    info.width = (((uint)dim[7]) << 24) + (((uint)dim[6]) << 16) + (((uint)dim[5]) << 8) + ((uint)dim[4]);
                    info.height = (((uint)dim[11]) << 24) + (((uint)dim[10]) << 16) + (((uint)dim[9]) << 8) + ((uint)dim[8]);
                    info.bits = (((uint)dim[15]) << 8) + ((uint)dim[14]);
                }
                else
                {
                    return ImageType.Unknown;
                }

                return ImageType.BMP;
            }

            static ImageType handle_png(Stream/*!*/stream, ref ImageInfo info)
            {
                byte[] dim = new byte[9];

                stream.Seek(8, SeekOrigin.Current);
                if (stream.Read(dim, 0, dim.Length) != dim.Length)
                    return ImageType.Unknown;

                info.width = (((uint)dim[0]) << 24) + (((uint)dim[1]) << 16) + (((uint)dim[2]) << 8) + ((uint)dim[3]);
                info.height = (((uint)dim[4]) << 24) + (((uint)dim[5]) << 16) + (((uint)dim[6]) << 8) + ((uint)dim[7]);
                info.bits = (uint)dim[8];

                return ImageType.PNG;
            }

            static ImageType handle_jpg(Stream/*!*/stream, ref ImageInfo info, bool exif)
            {
                GetExif(stream, ref info, exif);

                return ImageType.JPEG;
            }

            static ImageType handle_jpc(Stream/*!*/stream, ref ImageInfo info)
            {
                int first_marker_id = stream.ReadByte();

                if (first_marker_id != 0x51) /* Image and tile size */
                {
                    return ImageType.Unknown;
                }

                stream.Seek(4, SeekOrigin.Current);

                byte[] buffer = new byte[4];

                // Width
                stream.Read(buffer, 0, 4);
                buffer = ReversedBytes(buffer, 0, 4, BitConverter.IsLittleEndian);
                info.width = (uint)BitConverter.ToInt32(buffer, 0);

                // Height
                stream.Read(buffer, 0, 4);
                buffer = ReversedBytes(buffer, 0, 4, BitConverter.IsLittleEndian);
                info.height = (uint)BitConverter.ToInt32(buffer, 0);

                stream.Seek(24, SeekOrigin.Current);

                // Channels
                buffer = new byte[2];
                stream.Read(buffer, 0, 2);
                buffer = ReversedBytes(buffer, 0, 2, BitConverter.IsLittleEndian);
                int channels = BitConverter.ToInt16(buffer, 0);

                if (channels < 0 || channels > 256)
                    return ImageType.Unknown;

                info.channels = (uint)channels;

                // Bit depth
                int highest_bit_depth = 0;
                for (int i = 0; i < channels; i++)
                {
                    int bit_depth = stream.ReadByte();
                    bit_depth++;
                    if (bit_depth > highest_bit_depth)
                    {
                        highest_bit_depth = bit_depth;
                    }

                    stream.ReadByte();
                    stream.ReadByte();
                }

                info.bits = (uint)highest_bit_depth;

                return ImageType.JPC;
            }

            static ImageType handle_jp2(Stream/*!*/stream, ref ImageInfo info)
            {
                byte[] jp2c_box_id = { 0x63, 0x32, 0x70, 0x6a }; // 106 112 50 99

                byte[] buffer = new byte[4];

                int box_length;
                int box_type;

                /* JP2 is a wrapper format for JPEG 2000. Data is contained within "boxes".
	               Boxes themselves can be contained within "super-boxes". Super-Boxes can
	               contain super-boxes which provides us with a hierarchical storage system.

	               It is valid for a JP2 file to contain multiple individual codestreams.
	               We'll just look for the first codestream at the root of the box structure
	               and handle that.
	            */

                while (true)
                {
                    if (stream.Read(buffer, 0, 4) != 4) /* LBox */
                        break;

                    buffer = ReversedBytes(buffer, 0, 4, BitConverter.IsLittleEndian);

                    box_length = BitConverter.ToInt32(buffer, 0);

                    /* TBox */
                    if (stream.Read(buffer, 0, 4) != 4)
                        break;

                    buffer = ReversedBytes(buffer, 0, 4, BitConverter.IsLittleEndian);

                    box_type = BitConverter.ToInt32(buffer, 0);

                    if (box_length == 1)
                    {
                        /* We won't handle XLBoxes */
                        return ImageType.Unknown;
                    }

                    if (Utils.ByteArrayCompare(buffer, jp2c_box_id, 4))
                    {
                        /* Skip the first 3 bytes to emulate the file type examination */
                        stream.Seek(3, SeekOrigin.Current);

                        handle_jpc(stream, ref info);
                        return ImageType.JP2;
                    }

                    /* Stop if this was the last box */
                    if (box_length <= 0)
                    {
                        break;
                    }

                    /* Skip over LBox (Which includes both TBox and LBox itself */
                    if (stream.Seek((long)box_length - 8, SeekOrigin.Current) == 0)
                        break;
                }

                return ImageType.Unknown;
            }

            static ImageType handle_ico(Stream/*!*/stream, ref ImageInfo info)
            {
                byte[] dim = new byte[16];

                if (stream.Read(dim, 0, 2) != 2)
                    return ImageType.Unknown;

                uint num_icons = (((uint)dim[1]) << 8) + ((uint)dim[0]);

                if (num_icons < 1 || num_icons > 255)
                    return ImageType.Unknown;

                while (num_icons > 0)
                {
                    if (stream.Read(dim, 0, dim.Length) != dim.Length)
                        break;

                    if ((((uint)dim[7]) << 8) + ((uint)dim[6]) >= info.bits)
                    {
                        info.width = (uint)dim[0];
                        info.height = (uint)dim[1];
                        info.bits = (((uint)dim[7]) << 8) + ((uint)dim[6]);
                    }
                    num_icons--;
                }

                return ImageType.ICO;
            }

            /// <summary>
            /// Build reversed (if required) portion of a given byte array.
            /// </summary>
            /// <param name="array">Source array.</param>
            /// <param name="offset">Index of the first byte.</param>
            /// <param name="count">Amount of bytes to cut of (and reverse).</param>
            /// <param name="reverse">True to reverse the portion of bytes.</param>
            /// <returns></returns>
            private static byte[]/*!*/ ReversedBytes(byte[]/*!*/array, int offset, int count, bool reverse)
            {
                Debug.Assert(array != null);
                Debug.Assert(offset >= 0);
                Debug.Assert(count >= 0);
                Debug.Assert(offset + count <= array.Length);

                if (!reverse && offset == 0 && count == array.Length)
                    return array;

                byte[] result = new byte[count];

                if (reverse)
                {
                    int i = 0;
                    int j = offset + count - 1;
                    for (; i < count; i++, j--)
                        result[i] = array[j];
                }
                else
                {
                    Buffer.BlockCopy(array, offset, result, 0, count);
                }

                return result;
            }

            private enum TagTypes
            {
                TAG_IMAGEWIDTH = 0x0100,
                TAG_IMAGEHEIGHT = 0x0101,
                TAG_COMP_IMAGEWIDTH = 0xA002,
                TAG_COMP_IMAGEHEIGHT = 0xA003,
                TAG_FMT_BYTE = 1,
                TAG_FMT_STRING = 2,
                TAG_FMT_USHORT = 3,
                TAG_FMT_ULONG = 4,
                TAG_FMT_URATIONAL = 5,
                TAG_FMT_SBYTE = 6,
                TAG_FMT_UNDEFINED = 7,
                TAG_FMT_SSHORT = 8,
                TAG_FMT_SLONG = 9,
                TAG_FMT_SRATIONAL = 10,
                TAG_FMT_SINGLE = 11,
                TAG_FMT_DOUBLE = 12
            }

            static ImageType handle_tiff(Stream/*!*/stream, ref ImageInfo info, bool IsLittleEndian)
            {
                bool reverseBytes = IsLittleEndian ^ BitConverter.IsLittleEndian;

                byte[] a = new byte[4];
                if (stream.Read(a, 0, 4) != 4)
                    return ImageType.Unknown;

                a = ReversedBytes(a, 0, 4, reverseBytes);

                int ifd_addr = BitConverter.ToInt32(a, 0);

                stream.Seek(ifd_addr - 8, SeekOrigin.Current);

                byte[] ifd_data = new byte[2];
                if (stream.Read(ifd_data, 0, 2) != 2)
                    return ImageType.Unknown;

                ifd_data = ReversedBytes(ifd_data, 0, 2, reverseBytes);

                short num_entries = BitConverter.ToInt16(ifd_data, 0);

                int dir_size = 2/*num dir entries*/ + 12/*length of entry*/* num_entries + 4/* offset to next ifd (points to thumbnail or NULL)*/;
                int ifd_size = dir_size;

                ifd_data = new byte[ifd_size];

                if (stream.Read(ifd_data, 2, dir_size - 2) != dir_size - 2)
                    return ImageType.Unknown;

                //int offset;
                int entry_value;
                byte[] buffer = new byte[2];

                for (int i = 0; i < num_entries; i++)
                {
                    int dir_entry = 2 + i * 12;

                    buffer[0] = ifd_data[dir_entry + 0];
                    buffer[1] = ifd_data[dir_entry + 1];

                    //ushort entry_tag = BitConverter.ToUInt16(ReversedBytes(ifd_data, dir_entry + 0, 2, IsLittleEndian), 0);
                    //short entry_type = BitConverter.ToInt16(ReversedBytes(ifd_data, dir_entry + 2, 2, IsLittleEndian), 0);

                    buffer = ReversedBytes(buffer, 0, 2, reverseBytes);

                    ushort entry_tag = (ushort)BitConverter.ToInt16(buffer, 0);

                    buffer[0] = ifd_data[dir_entry + 2];
                    buffer[1] = ifd_data[dir_entry + 3];

                    buffer = ReversedBytes(buffer, 0, 2, reverseBytes);

                    short entry_type = BitConverter.ToInt16(buffer, 0);

                    switch (entry_type)
                    {
                        case (short)TagTypes.TAG_FMT_BYTE:
                        case (short)TagTypes.TAG_FMT_SBYTE:
                            entry_value = ifd_data[dir_entry + 8];
                            break;
                        case (short)TagTypes.TAG_FMT_USHORT:

                            buffer[0] = ifd_data[dir_entry + 8];
                            buffer[1] = ifd_data[dir_entry + 9];

                            buffer = ReversedBytes(buffer, 0, 2, reverseBytes);

                            entry_value = BitConverter.ToUInt16(buffer, 0);
                            break;
                        case (short)TagTypes.TAG_FMT_SSHORT:

                            buffer[0] = ifd_data[dir_entry + 8];
                            buffer[1] = ifd_data[dir_entry + 9];

                            buffer = ReversedBytes(buffer, 0, 2, reverseBytes);

                            entry_value = BitConverter.ToInt16(buffer, 0);
                            break;
                        case (short)TagTypes.TAG_FMT_ULONG:

                            buffer[0] = ifd_data[dir_entry + 8];
                            buffer[1] = ifd_data[dir_entry + 9];

                            buffer = ReversedBytes(buffer, 0, 2, reverseBytes);

                            entry_value = (int)BitConverter.ToUInt16(buffer, 0);
                            break;
                        case (short)TagTypes.TAG_FMT_SLONG:

                            buffer[0] = ifd_data[dir_entry + 8];
                            buffer[1] = ifd_data[dir_entry + 9];

                            buffer = ReversedBytes(buffer, 0, 2, reverseBytes);

                            entry_value = (int)BitConverter.ToInt16(buffer, 0);
                            break;
                        default:
                            continue;
                    }
                    switch (entry_tag)
                    {
                        case (ushort)TagTypes.TAG_IMAGEWIDTH:
                        case (ushort)TagTypes.TAG_COMP_IMAGEWIDTH:
                            info.width = (uint)entry_value;
                            break;
                        case (ushort)TagTypes.TAG_IMAGEHEIGHT:
                        case (ushort)TagTypes.TAG_COMP_IMAGEHEIGHT:
                            info.height = (uint)entry_value;
                            break;
                    }
                }

                if (info.width == 0 || info.height == 0)
                    return ImageType.Unknown;

                if (IsLittleEndian)
                    return ImageType.TIFF_II;
                else
                    return ImageType.TIFF_MM;
            }

            static ImageType handle_iff(Stream/*!*/stream, ref ImageInfo info)
            {
                bool reversed = BitConverter.IsLittleEndian;

                byte[] a = new byte[10];
                if (stream.Read(a, 0, 8) != 8) return ImageType.Unknown;

                if (!Equals(a, 4, new byte[] { (byte)'I', (byte)'L', (byte)'B', (byte)'M' }, 4) &&
                    !Equals(a, 4, new byte[] { (byte)'P', (byte)'B', (byte)'M', (byte)' ' }, 4))
                    return ImageType.Unknown;

                /* loop chunks to find BMHD chunk */
                for (; ; )
                {
                    if (stream.Read(a, 0, 8) != 8) return ImageType.Unknown;
                    int chunkId = BitConverter.ToInt32(ReversedBytes(a, 0, 4, reversed), 0);
                    int size = BitConverter.ToInt32(ReversedBytes(a, 4, 4, reversed), 0);
                    if (size < 0) return ImageType.Unknown;
                    if ((size & 1) == 1) size++;
                    if (chunkId == 0x424d4844)
                    { /* BMHD chunk */
                        if (size < 9 || stream.Read(a, 0, 9) != 9) return ImageType.Unknown;
                        short width = BitConverter.ToInt16(ReversedBytes(a, 0, 2, reversed), 0);
                        short height = BitConverter.ToInt16(ReversedBytes(a, 2, 2, reversed), 0);
                        byte bits = (byte)(a[8] & 0xff);
                        if (width > 0 && height > 0 && bits > 0 && bits < 33)
                        {
                            info.width = (uint)width;
                            info.height = (uint)height;
                            info.bits = (uint)bits;
                            info.channels = 0;
                            return ImageType.IFF;
                        }
                    }
                    else
                    {
                        stream.Seek(size, SeekOrigin.Current);
                    }
                }
            }

            static ImageType handle_swf(Stream/*!*/stream, ref ImageInfo info, bool compressed)
            {
                stream.Seek(5, SeekOrigin.Current); // skip file version, and file size

                byte[] b = new byte[128];

                if (compressed)
                {
                    stream.Seek(2, SeekOrigin.Current);
                    stream = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Decompress, false);
                }

                // read RECT:

                byte byTemp = (byte)stream.ReadByte();
                byte byNbits = (byte)((int)byTemp >> 3);
                byTemp &= 7;
                byTemp <<= 5;

                int nBitCount = 0, nCurrentValue = 0, nCurrentBit = 2;
                int xMax = 0, xMin = 0, yMin = 0, yMax = 0;

                for (int i = 0; i < 4; i++)
                {
                    while (nBitCount < byNbits)
                    {
                        if ((byTemp & 128) == 128)
                            nCurrentValue += 1 << (byNbits - nBitCount - 1);

                        byTemp <<= 1;
                        byTemp &= 255;
                        nCurrentBit--;
                        nBitCount++;
                        if (nCurrentBit < 0)
                        {
                            byTemp = (byte)stream.ReadByte();
                            nCurrentBit = 7;
                        }
                    }

                    switch (i)
                    {
                        case 0:
                            xMin = nCurrentValue;
                            break;
                        case 1:
                            xMax = nCurrentValue;
                            break;
                        case 2:
                            yMin = nCurrentValue;
                            break;
                        case 3:
                            yMax = nCurrentValue;
                            break;
                        default:
                            Debug.Fail(null);
                            break;
                    }

                    nBitCount = 0;
                    nCurrentValue = 0;
                }

                info.width = (uint)(xMax - xMin) / 20;
                info.height = (uint)(yMax - yMin) / 20;

                return compressed ? ImageType.SWC : ImageType.SWF;
            }

            #region jpeg

            #region JpegMarkerTypes

            /// <summary>
            /// List of possible Jpeg Exif Markers
            /// </summary>
            private enum JpegMarker
            {
                M_SOF0 = 0xC0,
                M_SOF1 = 0xC1,
                M_SOF2 = 0xC2,
                M_SOF3 = 0xC3,
                M_SOF4 = 0xC4,
                M_SOF5 = 0xC5,
                M_SOF6 = 0xC6,
                M_SOF7 = 0xC7,
                M_SOF8 = 0xC8,
                M_SOF9 = 0xC9,
                M_SOF10 = 0xCA,
                M_SOF11 = 0xCB,
                M_SOF12 = 0xCC,
                M_SOF13 = 0xCD,
                M_SOF14 = 0xCE,
                M_SOF15 = 0xCF,
                M_SOI = 0xD8,
                M_EOI = 0xD9, /* End Of Image (end of datastream) */
                M_SOS = 0xDA, /* Start Of Scan (begins compressed data) */
                M_APP0 = 0xe0,
                M_APP1 = 0xe1,
                M_APP2 = 0xe2,
                M_APP3 = 0xe3,
                M_APP4 = 0xe4,
                M_APP5 = 0xe5,
                M_APP6 = 0xe6,
                M_APP7 = 0xe7,
                M_APP8 = 0xe8,
                M_APP9 = 0xe9,
                M_APP10 = 0xea,
                M_APP11 = 0xeb,
                M_APP12 = 0xec,
                M_APP13 = 0xed,
                M_APP14 = 0xee,
                M_APP15 = 0xef,
                M_COM = 0xFE,
                M_PSEUDO = 0xFFD8 /* pseudo marker for start of image(byte 0) */
            }

            #endregion

            /// <summary>
            /// Read next two bytes (marker size)
            /// </summary>
            /// <param name="stream"></param>
            /// <returns></returns>
            private static int ReadMarkerSize(Stream stream)
            {
                byte[] buffer = new byte[2];

                stream.Read(buffer, 0, 2);
                return (((int)buffer[0] << 8) | buffer[1]);
            }

            /// <summary>
            /// Skip over a variable-length block; assumes proper length marker
            /// </summary>
            /// <param name="stream"></param>
            /// <returns></returns>
            private static bool SkipVariable(Stream stream)
            {
                int length = ReadMarkerSize(stream);

                if (length < 2)
                {
                    return false;
                }
                length = length - 2;

                stream.Seek(length, SeekOrigin.Current);

                return true;
            }

            /// <summary>
            /// Get next marker in jpeg file (starts with 0xff)
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="last_marker"></param>
            /// <param name="comment_correction"></param>
            /// <param name="ff_read"></param>
            /// <returns></returns>
            private static JpegMarker GetNextMarker(Stream stream, JpegMarker last_marker, int comment_correction, int ff_read)
            {
                int a = 0;
                JpegMarker marker;

                /* get marker byte, swallowing possible padding                               */
                if (last_marker == JpegMarker.M_COM && comment_correction != 0)
                {
                    /* some software does not count the length bytes of COM section           */
                    /* one company doing so is very much envolved in JPEG... so we accept too */
                    /* by the way: some of those companies changed their code now...          */
                    comment_correction = 2;
                }
                else
                {
                    last_marker = 0;
                    comment_correction = 0;
                }

                if (ff_read != 0)
                {
                    a = 1; /* already read 0xff in filetype detection */
                }
                do
                {
                    marker = (JpegMarker)stream.ReadByte();
                    if (marker == JpegMarker.M_EOI)
                    {
                        return JpegMarker.M_EOI;/* we hit EOF */
                    }
                    if (last_marker == JpegMarker.M_COM && comment_correction > 0)
                    {
                        if (marker != (JpegMarker)0xFF)
                        {
                            marker = (JpegMarker)0xFF;
                            comment_correction--;
                        }
                        else
                        {
                            last_marker = JpegMarker.M_PSEUDO; /* stop skipping non 0xff for M_COM */
                        }
                    }
                    a++;
                } while (marker == (JpegMarker)0xff);
                if (a < 2)
                {
                    return JpegMarker.M_EOI; /* at least one 0xff is needed before marker code */
                }
                if (last_marker == JpegMarker.M_COM && comment_correction != 0)
                {
                    return JpegMarker.M_EOI; /* ah illegal: char after COM section not 0xFF */
                }

                return marker;
            }

            /// <summary>
            /// Save specific jpeg marker into result array
            /// </summary>
            /// <param name="array"></param>
            /// <param name="ms"></param>
            /// <param name="markerName"></param>
            /// <returns></returns>
            private static bool SaveMarker(ref PhpArray array, Stream ms, string markerName)
            {
                int markerLength;
                byte[] buffer;

                markerLength = ReadMarkerSize(ms);
                if (markerLength < 2)
                {
                    return false;
                }
                markerLength -= 2; // length includes itself

                buffer = new byte[markerLength];

                ms.Read(buffer, 0, markerLength);

                if (!array.Contains(markerName))
                {
                    array.Add(markerName, PhpValue.Create(new PhpString(buffer)));
                }

                return true;
            }

            /// <summary>
            /// Extracts Exif information from specified memory stream.
            /// </summary>
            /// <param name="ms"></param>
            /// <param name="info">Will be filled with image info.</param>
            /// <param name="exif">Whether we are interested in additional EXIF data.</param>
            /// <returns></returns>
            public static bool GetExif(Stream ms, ref ImageInfo info, bool exif)
            {
                JpegMarker marker = JpegMarker.M_PSEUDO;
                int ff_read = 1;

                for (; ; )
                {
                    marker = GetNextMarker(ms, marker, 1, ff_read);
                    ff_read = 0;

                    switch ((JpegMarker)marker)
                    {
                        case JpegMarker.M_SOF0:
                        case JpegMarker.M_SOF1:
                        case JpegMarker.M_SOF2:
                        case JpegMarker.M_SOF3:
                        case JpegMarker.M_SOF5:
                        case JpegMarker.M_SOF6:
                        case JpegMarker.M_SOF7:
                        case JpegMarker.M_SOF9:
                        case JpegMarker.M_SOF10:
                        case JpegMarker.M_SOF11:
                        case JpegMarker.M_SOF13:
                        case JpegMarker.M_SOF14:
                        case JpegMarker.M_SOF15:
                            {
                                /* handle SOFn block */
                                int length = ReadMarkerSize(ms);
                                info.bits = (uint)ms.ReadByte();
                                info.height = (uint)ReadMarkerSize(ms);
                                info.width = (uint)ReadMarkerSize(ms);
                                info.channels = (uint)ms.ReadByte();
                                if (length < 8 || !exif) // if we don't want an extanded info -> return
                                    return true;

                                ms.Seek(length - 8, SeekOrigin.Current); // after info
                            }
                            break;
                        case JpegMarker.M_APP0:
                        case JpegMarker.M_APP1:
                        case JpegMarker.M_APP2:
                        case JpegMarker.M_APP3:
                        case JpegMarker.M_APP4:
                        case JpegMarker.M_APP5:
                        case JpegMarker.M_APP6:
                        case JpegMarker.M_APP7:
                        case JpegMarker.M_APP8:
                        case JpegMarker.M_APP9:
                        case JpegMarker.M_APP10:
                        case JpegMarker.M_APP11:
                        case JpegMarker.M_APP12:
                        case JpegMarker.M_APP13:
                        case JpegMarker.M_APP14:
                        case JpegMarker.M_APP15:
                            if (exif)
                            {
                                if (info.exif == null)
                                    info.exif = new PhpArray(32);

                                SaveMarker(ref info.exif, ms, "APP" + (marker - (int)JpegMarker.M_APP0));
                            }
                            else
                            {
                                if (!SkipVariable(ms))
                                    return true;
                            }
                            break;

                        case JpegMarker.M_SOS:
                        case JpegMarker.M_EOI:
                            return true;    // End of Jpeg File or start of image data

                        default:
                            if (!SkipVariable(ms)) // anything else isn't interesting
                                return true;
                            break;
                    }
                }
            }

            #endregion

            #region wbmp, xbm

            /// <summary>
            /// Read next byte from given <paramref name="buffer"/>. If buffer is at the end, read the next byte from the given <paramref name="stream"/> and buffer it.
            /// </summary>
            /// <param name="buffer">Buffer to read bytes primarily.</param>
            /// <param name="stream">Stream that is used for shadow copying into the buffer.</param>
            /// <returns>Value of the next byte converted to int or -1 if there is not more bytes in the <paramref name="stream"/>.</returns>
            private static int ReadByte(MemoryStream/*!*/buffer, Stream/*!*/stream)
            {
                int value;

                if (buffer.Position >= buffer.Length)   // at the end of the buffer, read more bytes from the stream
                {
                    value = stream.ReadByte();
                    if (value >= 0)
                        buffer.WriteByte((byte)value);
                }
                else
                {
                    value = buffer.ReadByte();
                }

                return value;
            }

            private static ImageType handle_wbmp(MemoryStream/*!*/buffer, Stream/*!*/stream, ref ImageInfo info)
            {
                buffer.Position = 0;

                if (ReadByte(buffer, stream) != 0)
                    return ImageType.Unknown;

                int i = 0, w = 0, h = 0;

                do
                {
                    //i = stream.ReadByte();
                    i = ReadByte(buffer, stream);

                    if (i < 0)
                    {
                        return ImageType.Unknown;
                    }
                } while ((i & 0x80) != 0);

                /* get width */
                do
                {
                    //i = stream.ReadByte();
                    i = ReadByte(buffer, stream);

                    if (i < 0)
                    {
                        return ImageType.Unknown;
                    }
                    w = (w << 7) | (i & 0x7f);
                } while ((i & 0x80) != 0);

                /* get height */
                do
                {
                    //i = stream.ReadByte();
                    i = ReadByte(buffer, stream);

                    if (i < 0)
                    {
                        return ImageType.Unknown;
                    }
                    h = (h << 7) | (i & 0x7f);
                } while ((i & 0x80) != 0);

                /* maximum valid sizes for wbmp (although 127x127 may be a more accurate one) */
                if (h == 0 || w == 0 || h > 2048 || w > 2048)
                {
                    return ImageType.Unknown;
                }
                else
                {
                    info.width = (uint)w;
                    info.height = (uint)h;
                    return ImageType.WBMP;
                }
            }

            private static byte[] xbm_define = new byte[] { (byte)'#', (byte)'d', (byte)'e', (byte)'f', (byte)'i', (byte)'n', (byte)'e', (byte)' ' };

            private static ImageType handle_xbm(MemoryStream/*!*/buffer, Stream/*!*/stream, ref ImageInfo info)
            {
                buffer.Position = 0;
                int w = 0, h = 0;
                int badlines = 0;  // read up to 4 lines, if we did not found any #define

                do
                {
                    bool define_found = true;

                    // read "#define ", otherwise break;
                    for (int i = 0; i < xbm_define.Length; i++)
                    {
                        int b;
                        do b = ReadByte(buffer, stream);
                        while (i == 0 && (b == 13 || b == 10)); // skip line ends at the beginning of reading

                        if (b < 0) return ImageType.Unknown;

                        if ((char)b != xbm_define[i])
                        {
                            define_found = false;
                            break;
                        }
                    }

                    // 
                    if (define_found)
                    {
                        // read the line:
                        var bld = new StringBuilder(32);
                        for (; ; )
                        {
                            int b = ReadByte(buffer, stream);
                            if (b == 10 || b == 13 || b <= 0)   // until 13, 10, 0, EOF
                                break;

                            bld.Append((char)b);

                            if (bld.Length > 4096) return ImageType.Unknown;    // do not read if there are too long lines, invalid file probably.
                        }

                        string[] parts = bld.ToString().Split(' ');

                        // read 
                        if (parts.Length == 2)
                        {
                            if (parts[0].EndsWith("_width"))
                            {
                                if (!int.TryParse(parts[1], out w) || w == 0) return ImageType.Unknown;
                            }
                            else if (parts[0].EndsWith("_height"))
                                if (!int.TryParse(parts[1], out h) || h == 0) return ImageType.Unknown;

                            if (w != 0 && h != 0)
                            {
                                info.width = (uint)w;
                                info.height = (uint)h;
                                return ImageType.XBM;
                            }
                        }
                    }
                    else
                    {
                        badlines++;
                        // read the rest of the line:
                        int b;
                        do
                        {
                            b = ReadByte(buffer, stream);
                            if (b < 0) return ImageType.Unknown;
                        } while (b != 10 && b != 13);
                    }
                } while (badlines < 4);

                return ImageType.Unknown;
            }

            #endregion

            #endregion

            private static bool Equals(byte[] a, byte[] b, int length)
            {
                return Utils.ByteArrayCompare(a, b, length);
            }

            private static bool Equals(byte[]/*!*/a, int offset, byte[]/*!*/b, int length)
            {
                Debug.Assert(a != null && b != null && offset + length <= a.Length && length <= b.Length && offset >= 0);

                int i = offset;
                int j = 0;
                for (; j < length; i++, j++)
                    if (a[i] != b[j])
                        return false;
                return true;
            }

            private static ImageType ReadError(bool quiet)
            {
                if (!quiet) PhpException.Throw(PhpError.Notice, Resources.read_error);
                return ImageType.Unknown;
            }

            /// <summary>
            /// Read the image type from the <paramref name="stream"/>. Advances the position in the stream accordingly.
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="quiet">True to output warning messages.</param>
            /// <param name="info"></param>
            /// <param name="collectInfo"></param>
            /// <param name="collectExif"></param>
            /// <returns></returns>
            public static ImageType ProcessImageType(Stream/*!*/stream, bool quiet, out ImageInfo info, bool collectInfo, bool collectExif)
            {
                Debug.Assert(stream != null);

                byte[] filetype = new byte[12];
                info = new ImageInfo();

                if (stream.Read(filetype, 0, 3) != 3) return ReadError(quiet);

                // 3 byte headers:
                if (Equals(filetype, sig_gif, 3)) return collectInfo ? handle_gif(stream, ref info) : ImageType.GIF;
                if (Equals(filetype, sig_jpg, 3)) return collectInfo ? handle_jpg(stream, ref info, collectExif) : ImageType.JPEG;
                if (Equals(filetype, sig_png, 3))
                {
                    if (stream.Read(filetype, 3, 5) != 5) return ReadError(quiet);
                    if (Equals(filetype, sig_png, 8)) return collectInfo ? handle_png(stream, ref info) : ImageType.PNG;

                    if (!quiet) PhpException.Throw(PhpError.Warning, Resources.png_corrupted);
                    return ImageType.Unknown;
                }
                if (Equals(filetype, sig_swf, 3)) return collectInfo ? handle_swf(stream, ref info, false) : ImageType.SWF;
                if (Equals(filetype, sig_swc, 3)) return collectInfo ? handle_swf(stream, ref info, true) : ImageType.SWC;
                if (Equals(filetype, sig_psd, 3)) return collectInfo ? handle_psd(stream, ref info) : ImageType.PSD;
                if (Equals(filetype, sig_bmp, 2)) return collectInfo ? handle_bmp(stream, ref info) : ImageType.BMP;
                if (Equals(filetype, sig_jpc, 3)) return collectInfo ? handle_jpc(stream, ref info) : ImageType.JPC;

                if (stream.Read(filetype, 3, 1) != 1) return ReadError(quiet);

                // 4 byte headers:
                if (Equals(filetype, sig_tif_ii, 4)) return collectInfo ? handle_tiff(stream, ref info, true) : ImageType.TIFF_II;
                if (Equals(filetype, sig_tif_mm, 4)) return collectInfo ? handle_tiff(stream, ref info, false) : ImageType.TIFF_MM;
                if (Equals(filetype, sig_iff, 4)) return collectInfo ? handle_iff(stream, ref info) : ImageType.IFF;
                if (Equals(filetype, sig_ico, 4)) return collectInfo ? handle_ico(stream, ref info) : ImageType.ICO;


                if (stream.Read(filetype, 4, 8) != 8) return ReadError(quiet);
                // 12 byte headers:
                if (Equals(filetype, sig_jp2, 12)) return collectInfo ? handle_jp2(stream, ref info) : ImageType.JP2;

                // remaining cases:
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(filetype, 0, 12);

                    if (handle_wbmp(ms, stream, ref info) == ImageType.WBMP)
                        return ImageType.WBMP;

                    if (handle_xbm(ms, stream, ref info) == ImageType.XBM)
                        return ImageType.XBM;
                }

                return ImageType.Unknown;
            }


        }

        #endregion

        #region getimagesize, getimagesizefromstring

        static PhpArray getimagesize(Stream stream, PhpAlias imageinfo)
        {
            PhpArray exif;

            var result = GetImageSize(stream, imageinfo != null, out exif);

            if (imageinfo != null)
            {
                imageinfo.Value = PhpValue.Create(exif ?? new PhpArray());
            }

            return result;
        }

        static PhpArray GetImageSize(Stream/*!*/stream, bool exif, out PhpArray exifarray)
        {
            exifarray = null;

            if (stream == null)
                return null;

            ImageSignature.ImageInfo info;
            ImageType type;
            try
            {
                type = ImageSignature.ProcessImageType(stream, false, out info, true, exif);
            }
            catch
            {
                /*rw error*/
                type = ImageType.Unknown;
                info.width = info.height = info.bits = info.channels = 0;
                info.exif = null;
            }
            finally
            {
                stream.Dispose();
            }

            if (type != ImageType.Unknown)
            {
                var result = new PhpArray(7);

                result.Add((int)info.width);
                result.Add((int)info.height);
                result.Add((int)type);
                result.Add(string.Format($"width=\"{info.width}\" height=\"{info.height}\""));

                if (info.bits != 0) result.Add("bits", (int)info.bits);
                if (info.channels != 0) result.Add("channels", (int)info.channels);
                result.Add("mime", image_type_to_mime_type(type));

                exifarray = info.exif;
                return result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get the size of an image.
        /// </summary> 
        /// <param name="ctx">Runtime context.</param>
        /// <param name="filename">This parameter specifies the file you wish to retrieve information about. It can reference a local file or (configuration permitting) a remote file using one of the supported streams.</param>
        /// <param name="imageinfo">This optional parameter allows you to extract some extended information from the image file.</param>
        [return: CastToFalse]
        public static PhpArray getimagesize(Context ctx, string filename, PhpAlias imageinfo = null)
        {
            if (string.IsNullOrEmpty(filename))
            {
                PhpException.Throw(PhpError.Warning, Resources.filename_cannot_be_empty);
                return null;
            }

            return getimagesize(Utils.OpenStream(ctx, filename), imageinfo);
        }

        /// <summary>
        /// Get the size of an image.
        /// </summary>
        /// <param name="bytes">Content of the image.</param>
        /// <param name="imageinfo">This optional parameter allows you to extract some extended information from the image file.</param>
        [return: CastToFalse]
        public static PhpArray getimagesizefromstring(byte[] bytes, PhpAlias imageinfo = null)
        {
            if (bytes == null)
                return null;

            return getimagesize(new MemoryStream(bytes), imageinfo);
        }

        #endregion

        #region iptcparse, iptcembed

        /// <summary>
        /// Parse a binary IPTC block into single tags.
        /// </summary>
        /// <param name="iptcblock">A binary IPTC block.</param>
        /// <returns>Returns an array using the tagmarker as an index and the value as the value. It returns FALSE on error or if no IPTC data was found.</returns>
        [return: CastToFalse]
        public static PhpArray iptcparse(byte[] iptcblock)
        {
            // validate arguments:
            if (iptcblock == null || iptcblock.Length == 0)
                return null;

            // parse IPTC block:
            uint inx = 0, len;
            var buffer = iptcblock;

            // find 1st tag:
            for (; inx < buffer.Length; ++inx)
            {
                if ((buffer[inx] == 0x1c) && ((buffer[inx + 1] == 0x01) || (buffer[inx + 1] == 0x02)))
                    break;
            }

            PhpArray result = null;

            // search for IPTC items:
            while (inx < buffer.Length)
            {
                if (buffer[inx++] != 0x1c)
                    break;   // we ran against some data which does not conform to IPTC - stop parsing!

                if ((inx + 4) >= buffer.Length)
                    break;

                // data, recnum:
                byte dataset = buffer[inx++];
                byte recnum = buffer[inx++];

                // len:
                if ((buffer[inx] & (byte)0x80) != 0)
                { // long tag
                    len = (((uint)buffer[inx + 2]) << 24) | (((uint)buffer[inx + 3]) << 16) |
                          (((uint)buffer[inx + 4]) << 8) | (((uint)buffer[inx + 5]));
                    inx += 6;
                }
                else
                { // short tag
                    len = (((uint)buffer[inx + 0]) << 8) | (((uint)buffer[inx + 1]));
                    inx += 2;
                }

                if ((len > buffer.Length) || (inx + len) > buffer.Length)
                    break;

                // snprintf(key, sizeof(key), "%d#%03d", (unsigned int) dataset, (unsigned int) recnum);
                string key = string.Format("{0}#{1}", dataset, recnum.ToString("D3"));

                // create result array lazily:
                if (result == null)
                    result = new PhpArray();

                // parse out the data (buffer+inx)[len]:
                var data = new byte[len];
                Buffer.BlockCopy(buffer, (int)inx, data, 0, (int)len);

                // add data into result[key][]:
                var values = result[key].AsArray();
                if (values == null)
                {
                    values = new PhpArray(1);
                    result.Add(key, PhpValue.Create(values));
                }

                values.Add(data);

                //
                inx += len;
            }

            //
            return result;  // null if no items were found
        }

        /// <summary>
        /// Embeds binary IPTC data into a JPEG image.
        /// </summary>
        /// <param name="iptcdata">The data to be written.</param>
        /// <param name="jpeg_file_name">Path to the JPEG image.</param>
        /// <param name="spool">Spool flag. If the spool flag is over 2 then the JPEG will be returned as a string.</param>
        /// <returns>If success and spool flag is lower than 2 then the JPEG will not be returned as a string, FALSE on errors.</returns>
        public static PhpValue iptcembed(byte[] iptcdata, string jpeg_file_name, int spool = 0)
        {
            PhpException.FunctionNotSupported("iptcembed");
            return PhpValue.False;
        }

        #endregion

        #region image_type_to_extension

        /// <summary>
        /// Get file extension for image type
        /// </summary> 
        [return: CastToFalse]
        public static string image_type_to_extension(ImageType imagetype, bool include_dot = true)
        {
            var extension = imagetype switch
            {
                ImageType.GIF => "gif",

                ImageType.JPEG => "jpeg",

                ImageType.PNG => "png",

                ImageType.SWF => "swf",

                ImageType.PSD => "psd",

                ImageType.BMP => "bmp",

                ImageType.TIFF_II => "tiff",

                ImageType.TIFF_MM => "tiff",

                ImageType.JPC => "jpc",

                ImageType.JP2 => "jp2",

                ImageType.JPX => "jpx",

                ImageType.JB2 => "jb2",

                ImageType.SWC => "swc",

                ImageType.IFF => "iff",

                ImageType.WBMP => "wbmp",

                ImageType.XBM => "xbm",

                ImageType.ICO => "ico",

                ImageType.WEBP => "webp",

                _ => null,
            };

            if (extension != null)
            {
                return include_dot ? ("." + extension) : (extension);
            }
            else
            {
                return null; // FALSE
            }
        }

        #endregion

        #region image_type_to_mime_type

        /// <summary>
        /// Get Mime-Type for image-type returned by getimagesize, exif_read_data, exif_thumbnail, exif_imagetype
        /// </summary> 
        //[return: CastToFalse]
        public static string image_type_to_mime_type(ImageType imagetype) => imagetype switch
        {
            ImageType.GIF => "image/gif",

            ImageType.JPEG => "image/jpeg",

            ImageType.PNG => "image/png",

            ImageType.SWF => "application/x-shockwave-flash",

            ImageType.PSD => "image/psd",

            ImageType.BMP => "image/x-ms-bmp",

            ImageType.TIFF_II => "image/tiff",

            ImageType.TIFF_MM => "image/tiff",

            ImageType.JPC => "application/octet-stream",

            ImageType.JP2 => "image/jp2",

            ImageType.JPX => "application/octet-stream",

            ImageType.JB2 => "application/octet-stream",

            ImageType.SWC => "application/x-shockwave-flash",

            ImageType.IFF => "image/iff",

            ImageType.WBMP => "image/vnd.wap.wbmp",

            ImageType.XBM => "image/xbm",

            ImageType.ICO => "image/vnd.microsoft.icon",

            ImageType.WEBP => "image/webp",

            _ => "application/octet-stream", // suppose binary format
        };

        #endregion
    }
}
