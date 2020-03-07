using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Pchp.Library
{
    [PhpExtension(ExtensionName)]
    public static class PhpFileInfo
    {
        internal const string ExtensionName = "fileinfo";

        #region Constants

        ///<summary>No special handling.</summary>
        public const int FILEINFO_NONE = 0;
        ///<summary>Follow symlinks.</summary>
        public const int FILEINFO_SYMLINK = 2;
        /////<summary>Decompress compressed files. Disabled since PHP 5.3.0 due to thread safety issues.</summary>
        //public const int FILEINFO_COMPRESS = 4;
        ///<summary>Look at the contents of blocks or character special devices.</summary>
        public const int FILEINFO_DEVICES = 8;
        ///<summary>Return the mime type.</summary>
        public const int FILEINFO_MIME_TYPE = 16;
        ///<summary>Return all matches, not just the first.</summary>
        public const int FILEINFO_CONTINUE = 32;
        ///<summary>Return the mime encoding of the file.</summary>
        ///<summary>If possible preserve the original access time.</summary>
        public const int FILEINFO_PRESERVE_ATIME = 128;
        public const int FILEINFO_MIME_ENCODING = 1024;
        ///<summary>Return the mime type and mime encoding as defined by RFC 2045.</summary>
        public const int FILEINFO_MIME = 1040;
        ///<summary>Don't translate unprintable characters to a \ooo octal representation.</summary>
        public const int FILEINFO_RAW = 256;
        ///<summary>Returns the file extension appropiate for a the MIME type detected in the file. For types that commonly have multiple file extensions, such as JPEG images, then the return value is multiple extensions speparated by a forward slash e.g.: "jpeg/jpg/jpe/jfif". For unknown types not available in the magic.mime database, then return value is "???".</summary>
        public const int FILEINFO_EXTENSION = 0x1000000;

        #endregion

        /// <summary>
        /// Create a new fileinfo resource. Alias of <c>new finfo</c>.
        /// </summary>
        public static PhpResource finfo_open(Context ctx, int options = FILEINFO_NONE, string magic_file = null) => new finfoResource(new finfo(ctx, options, magic_file));

        /// <summary>
        /// Close fileinfo resource
        /// </summary>
        public static bool finfo_close(PhpResource finfo)
        {
            finfo?.Dispose();
            return true;
        }

        /// <summary>
        /// Return information about a string buffer.
        /// </summary>
        [return: CastToFalse]
        public static string finfo_buffer(PhpResource finfo, byte[] @string = null, int options = FILEINFO_NONE, PhpResource context = null)
            => ((finfoResource)finfo).finfo.buffer(@string, options, context);

        /// <summary>
        /// Return information about a file.
        /// </summary>
        [return: CastToFalse]
        public static string finfo_file(PhpResource finfo, string file_name = null, int options = FILEINFO_NONE, PhpResource context = null)
            => ((finfoResource)finfo).finfo.file(file_name, options, context);

        /// <summary>
        /// Set configuration options.
        /// </summary>
        public static bool finfo_set_flags(PhpResource finfo, int options) => ((finfoResource)finfo).finfo.set_flags(options);

        /// <summary>
        /// Detect MIME Content-type for a file.
        /// </summary>
        [return: CastToFalse]
        public static string mime_content_type(Context ctx, string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                PhpException.ArgumentNull(nameof(filename));
                return null; // FALSE
            }
            
            //
            return new finfo(ctx).file(filename);
        }
    }

    /// <summary>
    /// A file type descriptor.
    /// TODO: refactor into a separate project
    /// </summary>
    [DebuggerDisplay(".{_extension,nq}, {_mime,nq}")]
    internal sealed class FileType
    {
        readonly byte?[] _header;
        readonly ushort _headerOffset;
        readonly string _extension;
        readonly string _mime;

        /// <summary>
        /// Mime type.
        /// </summary>
        public string Mime => _mime;

        static byte?[] EmptyHeader => Array.Empty<byte?>();

        /// <summary>
        /// Gets value indicating the file type has specified header information.
        /// </summary>
        public bool HasHeader => _header.Length != 0;

        /// <summary>
        /// Max number of bytes we read from a file.
        /// </summary>
        public const ushort MaxHeaderSize = 560;  // some file formats have headers offset to 512 bytes

        #region s_mimeTypes // mime database

        /// <summary>
        /// mime database:
        /// file headers are taken from here:
        /// http://www.garykessler.net/library/file_sigs.html
        /// mime types are taken from here:
        /// http://www.webmaster-toolkit.com/mime-types.shtml
        /// </summary>
        static readonly FileType[] s_mimeTypes = new FileType[]
        {
            #region office, excel, ppt and documents, xml, pdf, rtf, msdoc

            // office and documents
            new FileType(new byte?[] { 0xEC, 0xA5, 0xC1, 0x00 }, "doc", "application/msword", 512),

            new FileType(new byte?[] { 0x09, 0x08, 0x10, 0x00, 0x00, 0x06, 0x05, 0x00 }, "xls", "application/excel", 512),

            //see source control for old version, def maybe wrong period
            new FileType(new byte?[] { 0xA0, 0x46, 0x1D, 0xF0 }, "ppt", "application/mspowerpoint", 512),

            //ms office and openoffice docs (they're zip files: rename and enjoy!)
            //don't add them to the list, as they will be 'subtypes' of the ZIP type
            new FileType(EmptyHeader, "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 512),
            new FileType(EmptyHeader, "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", 512),
            new FileType(EmptyHeader, "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 512),
            new FileType(EmptyHeader, "odt", "application/vnd.oasis.opendocument.text", 512),
            new FileType(EmptyHeader, "ods", "application/vnd.oasis.opendocument.spreadsheet", 512),

            // common documents
            new FileType(new byte?[] { 0x7B, 0x5C, 0x72, 0x74, 0x66, 0x31 }, "rtf", "application/rtf"),

            new FileType(new byte?[] { 0x25, 0x50, 0x44, 0x46 }, "pdf", "application/pdf"),

            //todo place holder extension
            new FileType(new byte?[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, "msdoc", "application/octet-stream"),

            //application/xml text/xml
            new FileType(new byte?[] { 0x72, 0x73, 0x69, 0x6F, 0x6E, 0x3D, 0x22, 0x31, 0x2E, 0x30, 0x22, 0x3F, 0x3E },
                                                                "xml,xul", "text/xml"),

            //text files
            new FileType(EmptyHeader, "txt", "text/plain"),

            new FileType(new byte?[] { 0xEF, 0xBB, 0xBF }, "txt", "text/plain"),
            new FileType(new byte?[] { 0xFE, 0xFF }, "txt", "text/plain"),
            new FileType(new byte?[] { 0xFF, 0xFE }, "txt", "text/plain"),
            new FileType(new byte?[] { 0x00, 0x00, 0xFE, 0xFF }, "txt", "text/plain"),
            new FileType(new byte?[] { 0xFF, 0xFE, 0x00, 0x00 }, "txt", "text/plain"),

            #endregion office, excel, ppt and documents, xml, pdf, rtf, msdoc

            #region Graphics jpeg, png, gif, bmp, ico, tiff

            new FileType(new byte?[] { 0xFF, 0xD8, 0xFF }, "jpg", "image/jpeg"),
            new FileType(new byte?[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png", "image/png"),
            new FileType(new byte?[] { 0x47, 0x49, 0x46, 0x38, null, 0x61 }, "gif", "image/gif"),
            new FileType(new byte?[] { 0x42, 0x4D }, "bmp", "image/bmp"), // or image/x-windows-bmp
            new FileType(new byte?[] { 0, 0, 1, 0 }, "ico", "image/x-icon"),

            //tiff
            //todo review support for tiffs, values for files need verified
            new FileType(new byte?[] { 0x49, 0x20, 0x49 }, "tiff", "image/tiff"),

            new FileType(new byte?[] { 0x49, 0x49, 0x2A, 0 }, "tiff", "image/tiff"),
            new FileType(new byte?[] { 0x4D, 0x4D, 0, 0x2A }, "tiff", "image/tiff"),
            new FileType(new byte?[] { 0x4D, 0x4D, 0, 0x2B }, "tiff", "image/tiff"),

            #endregion Graphics jpeg, png, gif, bmp, ico, tiff

            #region Video

            //todo review these
            //mp4 iso base file format, value: ....ftypisom
            new FileType(new byte?[] { 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D }, "mp4", "video/mp4", 4),

            new FileType(new byte?[] { 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32 }, "m4v", "video/x-m4v", 4),

            new FileType(new byte?[] { 0x66, 0x74, 0x79, 0x70, 0x71, 0x74, 0x20, 0x20 }, "mov", "video/quicktime", 4),

            new FileType(new byte?[] { 0x66, 0x74, 0x79, 0x70, 0x33, 0x67, 0x70, 0x35 }, "mp4", "video/mp4", 4),

            new FileType(new byte?[] { 0x66, 0x74, 0x79, 0x70, 0x4D, 0x53, 0x4E, 0x56 }, "mp4", "video/mp4", 4),

            new FileType(new byte?[] { 0x66, 0x74, 0x79, 0x70, 0x4D, 0x34, 0x41, 0x20 }, "mp4a", "audio/mp4", 4),

            //FLV	 	Flash video file
            new FileType(new byte?[] { 0x46, 0x4C, 0x56, 0x01 }, "flv", "application/unknown"),

            new FileType(new byte?[] { 0, 0, 0, 0x20, 0x66, 0x74, 0x79, 0x70, 0x33, 0x67, 0x70 }, "3gp", "video/3gg"),

            #endregion Video

            #region Audio

            new FileType(new byte?[] { 0x49, 0x44, 0x33 }, "mp3", "audio/mpeg"),

            //WAV	 	Resource Interchange File Format -- Audio for Windows file, where xx xx xx xx is the file size (little endian), audio/wav audio/x-wav

            new FileType(new byte?[] { 0x52, 0x49, 0x46, 0x46, null, null, null, null,
                0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20 }, "wav", "audio/wav"),

            //MID, MIDI	 	Musical Instrument Digital Interface (MIDI) sound file
            new FileType(new byte?[] { 0x4D, 0x54, 0x68, 0x64 }, "midi,mid", "audio/midi"),

            new FileType(new byte?[] { 0x66, 0x4C, 0x61, 0x43, 0, 0, 0, 0x22 }, "flac", "audio/x-flac"),

            #endregion Audio

            #region Zip, 7zip, rar, dll_exe, tar, bz2, gz_tgz

            new FileType(new byte?[] { 0x1F, 0x8B, 0x08 }, "gz, tgz", "application/x-gz"),

            new FileType(new byte?[] { 66, 77 }, "7z", "application/x-compressed"),
            new FileType(new byte?[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, "7z", "application/x-compressed"),

            new FileType(new byte?[] { 0x50, 0x4B, 0x03, 0x04 }, "zip", "application/x-compressed"),
            new FileType(new byte?[] { 0x52, 0x61, 0x72, 0x21 }, "rar", "application/x-compressed"),
            new FileType(new byte?[] { 0x4D, 0x5A }, "dll, exe", "application/octet-stream"),

            //Compressed tape archive file using standard (Lempel-Ziv-Welch) compression
            new FileType(new byte?[] { 0x1F, 0x9D }, "tar.z", "application/x-tar"),

            //Compressed tape archive file using LZH (Lempel-Ziv-Huffman) compression
            new FileType(new byte?[] { 0x1F, 0xA0 }, "tar.z", "application/x-tar"),

            //bzip2 compressed archive
            new FileType(new byte?[] { 0x42, 0x5A, 0x68 }, "bz2,tar,bz2,tbz2,tb2", "application/x-bzip2"),

            #endregion Zip, 7zip, rar, dll_exe, tar, bz2, gz_tgz

            #region Media ogg, dwg, pst, psd

            // media
            new FileType(new byte?[] { 103, 103, 83, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 }, "oga,ogg,ogv,ogx", "application/ogg"),

            new FileType(new byte?[] { 0x21, 0x42, 0x44, 0x4E }, "pst", "application/octet-stream"),

            //eneric AutoCAD drawing image/vnd.dwg  image/x-dwg application/acad
            new FileType(new byte?[] { 0x41, 0x43, 0x31, 0x30 }, "dwg", "application/acad"),

            //Photoshop image file
            new FileType(new byte?[] { 0x38, 0x42, 0x50, 0x53 }, "psd", "application/octet-stream"),

            #endregion Media ogg, dwg, pst, psd

            new FileType(new byte?[] { 0x21, 0x3C, 0x61, 0x72, 0x63, 0x68, 0x3E, 0x0A }, "lib", "application/octet-stream"),

            #region Crypto aes, skr, skr_2, pkr

            //AES Crypt file format. (The fourth byte is the version number.)
            new FileType(new byte?[] { 0x41, 0x45, 0x53 }, "aes", "application/octet-stream"),

            //SKR	 	PGP secret keyring file
            new FileType(new byte?[] { 0x95, 0x00 }, "skr", "application/octet-stream"),

            //SKR	 	PGP secret keyring file
            new FileType(new byte?[] { 0x95, 0x01 }, "skr", "application/octet-stream"),

            //PKR	 	PGP public keyring file
            new FileType(new byte?[] { 0x99, 0x01 }, "pkr", "application/octet-stream"),

            #endregion Crypto aes, skr, skr_2, pkr

            /*
		     * 46 72 6F 6D 20 20 20 or	 	From
		    46 72 6F 6D 20 3F 3F 3F or	 	From ???
		    46 72 6F 6D 3A 20	 	From:
		    EML	 	A commmon file extension for e-mail files. Signatures shown here
		    are for Netscape, Eudora, and a generic signature, respectively.
		    EML is also used by Outlook Express and QuickMail.
		     */
            new FileType(new byte?[] { 0x46, 0x72, 0x6F, 0x6D }, "eml", "message/rfc822"),

            //EVTX	 	Windows Vista event log file
            new FileType(new byte?[] { 0x45, 0x6C, 0x66, 0x46, 0x69, 0x6C, 0x65, 0x00 }, "elf", "text/plain"),
        };

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="FileType"/> class.
        /// </summary>
        /// <param name="header">Byte array with header.</param>
        /// <param name="offset">The header offset - how far into the file we need to read the header</param>
        /// <param name="extension">String with extension.</param>
        /// <param name="mime">The description of MIME.</param>
        private FileType(byte?[] header, string extension, string mime, ushort offset = 0)
        {
            Debug.Assert(string.IsNullOrEmpty(extension) || extension[0] != '.');

            _header = header ?? throw new ArgumentNullException();
            _headerOffset = offset;
            _extension = extension;
            _mime = mime;
        }

        /// <summary>
        /// Determines if this file type header matches given file content.
        /// </summary>
        /// <param name="data">File content.</param>
        public bool IsMatch(byte[] data)
        {
            if (data == null || data.Length < _headerOffset + _header.Length)
            {
                return false;
            }

            for (int i = 0; i < _header.Length; i++)
            {
                if (_header[i].HasValue && _header[i].Value != data[i + _headerOffset])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Lists all file types that matches given file content.
        /// Note file types without a header specified are ignored.
        /// </summary>
        public static IEnumerable<FileType> LookupFileTypes(byte[] data)
        {
            // TODO: lexicographical map
            return (data != null && data.Length != 0)
                ? s_mimeTypes.Where(t => t.HasHeader && t.IsMatch(data))
                : Enumerable.Empty<FileType>();
        }
    }

    /// <summary>
    /// <c>resource</c> wrapping instance of <see cref="finfo"/>.
    /// </summary>
    sealed class finfoResource : PhpResource
    {
        /// <summary>
        /// Underlaying <see cref="finfo"/> instance. Cannot be <c>null</c>.
        /// </summary>
        public finfo finfo => _finfo;
        readonly finfo _finfo;

        public finfoResource(finfo finfo)
           : base("file_info")
        {
            _finfo = finfo;
        }
    }

    /// <summary>
    /// This class provides an object oriented interface into the fileinfo functions.
    /// </summary>
    [PhpExtension(PhpFileInfo.ExtensionName)]
    [PhpType(PhpTypeAttribute.InheritName)]
    public class finfo
    {
        readonly protected Context/*!*/_ctx;
        int _options;

        public finfo(Context ctx, int options = PhpFileInfo.FILEINFO_NONE, string magic_file = null)
            : this(ctx)
        {
            __construct(options, magic_file);
        }

        [PhpFieldsOnlyCtor]
        protected finfo(Context ctx)
        {
            _ctx = ctx;
        }

        public virtual void __construct(int options = PhpFileInfo.FILEINFO_NONE, string magic_file = null)
        {
            if (!string.IsNullOrEmpty(magic_file))
            {
                Debug.Fail(nameof(magic_file));
            }

            _options = options;
        }

        [return: CastToFalse]
        public virtual string buffer(byte[] @string = null, int options = PhpFileInfo.FILEINFO_NONE, PhpResource context = null)
        {
            // TODO: options
            return FileType.LookupFileTypes(@string).FirstOrDefault()?.Mime;
        }

        [return: CastToFalse]
        public virtual string file(string file_name, int options = PhpFileInfo.FILEINFO_NONE, PhpResource context = null)
        {
            byte[] bytes;

            using (var stream = PhpStream.Open(_ctx, file_name, "rb"/*ReadBinary*/, StreamOpenOptions.Empty, StreamContext.GetValid(context, allowNull: true)))
            {
                bytes = stream?.ReadBytes(FileType.MaxHeaderSize);
            }

            // TODO: options
            return FileType.LookupFileTypes(bytes).FirstOrDefault()?.Mime;
        }

        public virtual bool set_flags(int options)
        {
            _options = options;
            return true;
        }
    }
}
