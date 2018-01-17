using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.Core;

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
        public static finfo finfo_open(Context ctx, int options = FILEINFO_NONE, string magic_file = null) => new finfo(ctx, options, magic_file);

        /// <summary>
        /// Close fileinfo resource
        /// </summary>
        public static bool finfo_close(finfo finfo)
        {
            if (finfo != null && finfo.IsValid)
            {
                finfo.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Return information about a string buffer.
        /// </summary>
        public static string finfo_buffer(finfo finfo, string @string = null, int options = FILEINFO_NONE, PhpResource context = null)
            => finfo?.buffer(@string, options, context);

        /// <summary>
        /// Return information about a file.
        /// </summary>
        public static string finfo_file(finfo finfo, string file_name = null, int options = FILEINFO_NONE, PhpResource context = null)
            => finfo?.file(file_name, options, context);

        /// <summary>
        /// Set configuration options.
        /// </summary>
        public static bool finfo_set_flags(finfo finfo, int options) => finfo.set_flags(options);

        /// <summary>
        /// Detect MIME Content-type for a file.
        /// </summary>
        public static string mime_content_type(Context ctx, string filename) => new finfo(ctx).file(filename);
    }

    /// <summary>
    /// This class provides an object oriented interface into the fileinfo functions.
    /// </summary>
    [PhpExtension(PhpFileInfo.ExtensionName)]
    [PhpType(PhpTypeAttribute.InheritName)]
    public class finfo : PhpResource
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
            : base("file_info")
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

        public virtual string buffer(string @string = null, int options = PhpFileInfo.FILEINFO_NONE, PhpResource context = null)
        {
            throw new NotImplementedException();
        }

        public virtual string file(string file_name = null, int options = PhpFileInfo.FILEINFO_NONE, PhpResource context = null)
        {
            throw new NotImplementedException();
        }

        public virtual bool set_flags(int options)
        {
            _options = options;
            return true;
        }
    }
}
