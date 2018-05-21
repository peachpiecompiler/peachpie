using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pchp.Core;

namespace Pchp.Library.Streams
{
    #region PhpUserStream class

    /// <summary>
    /// An implementation of <see cref="PhpStream"/> as a simple
    /// encapsulation of a .NET <see cref="System.IO.Stream"/> class
    /// which is directly accessible via the RawStream property.
    /// </summary>
    public class PhpUserStream : PhpStream
    {
        #region names of user wrapper methods

        public const string USERSTREAM_OPEN = "stream_open";
        public const string USERSTREAM_CLOSE = "stream_close";
        public const string USERSTREAM_READ = "stream_read";
        public const string USERSTREAM_WRITE = "stream_write";
        public const string USERSTREAM_FLUSH = "stream_flush";
        public const string USERSTREAM_SEEK = "stream_seek";
        public const string USERSTREAM_TELL = "stream_tell";
        public const string USERSTREAM_EOF = "stream_eof";
        public const string USERSTREAM_STAT = "stream_stat";
        public const string USERSTREAM_STATURL = "url_stat";
        public const string USERSTREAM_UNLINK = "unlink";
        public const string USERSTREAM_RENAME = "rename";
        public const string USERSTREAM_MKDIR = "mkdir";
        public const string USERSTREAM_RMDIR = "rmdir";
        public const string USERSTREAM_DIR_OPEN = "dir_opendir";
        public const string USERSTREAM_DIR_READ = "dir_readdir";
        public const string USERSTREAM_DIR_REWIND = "dir_rewinddir";
        public const string USERSTREAM_DIR_CLOSE = "dir_closedir";
        public const string USERSTREAM_LOCK = "stream_lock";
        public const string USERSTREAM_CAST = "stream_cast";
        public const string USERSTREAM_SET_OPTION = "stream_set_option";
        public const string USERSTREAM_TRUNCATE = "stream_truncate";
        public const string USERSTREAM_METADATA = "stream_metadata";

        #endregion

        #region PhpStream overrides

        public PhpUserStream(Context ctx, UserStreamWrapper/*!*/openingWrapper, StreamAccessOptions accessOptions, string openedPath, StreamContext context)
            : base(ctx, openingWrapper, accessOptions, openedPath, context)
        {
        }

        /// <summary>
        /// PhpResource.FreeManaged overridden to get rid of the contained context on Dispose.
        /// </summary>
        protected override void FreeManaged()
        {
            // stream_close
            if (UserWrapper != null)
                UserWrapper.OnClose(this);

            // free
            base.FreeManaged();
            if (Wrapper != null)	//Can be php://output
                Wrapper.Dispose();
        }

        #endregion

        #region Raw byte access (mandatory)

        protected override int RawRead(byte[] buffer, int offset, int count)
        {
            // stream_read:
            var result = UserWrapper.InvokeWrapperMethod(USERSTREAM_READ, (PhpValue)count);
            if (result.IsEmpty == false)
            {
                var bytes = result.ToBytes(RuntimeContext);
                int readbytes = bytes.Length;
                if (readbytes > count)
                {
                    //php_error_docref(NULL TSRMLS_CC, E_WARNING, "%s::" USERSTREAM_READ " - read %ld bytes more data than requested (%ld read, %ld max) - excess data will be lost",
                    //    us->wrapper->classname, (long)(didread - count), (long)didread, (long)count);
                    readbytes = count;
                }

                if (readbytes > 0)
                {
                    Array.Copy(bytes, 0, buffer, offset, readbytes);
                }

                return readbytes;
            }

            //
            return 0;
        }

        protected override int RawWrite(byte[] buffer, int offset, int count)
        {
            PhpValue bytes;
            if (count == 0)
            {
                bytes = PhpValue.Create(string.Empty);
            }
            if (offset == 0 && count == buffer.Length)
            {
                bytes = PhpValue.Create(new PhpString(buffer));
            }
            else
            {
                var data = new byte[count];
                Array.Copy(buffer, offset, data, 0, count);
                bytes = PhpValue.Create(new PhpString(data));
            }

            var result = UserWrapper.InvokeWrapperMethod(USERSTREAM_WRITE, bytes);

            var byteswrote = result.ToLong();
            if (byteswrote > count)
            {
                //php_error_docref(NULL TSRMLS_CC, E_WARNING, "%s::" USERSTREAM_WRITE " wrote %ld bytes more data than requested (%ld written, %ld max)",
                //us->wrapper->classname, (long)(didwrite - count), (long)didwrite, (long)count);
                byteswrote = count;
            }

            return (int)byteswrote;
        }

        protected override bool RawFlush()
        {
            return UserWrapper.InvokeWrapperMethod(USERSTREAM_FLUSH).ToBoolean();
        }

        protected override bool RawEof
        {
            get
            {
                // stream_eof:
                if (UserWrapper.InvokeWrapperMethod(USERSTREAM_EOF).ToBoolean())
                {
                    return true;
                }

                // TODO: if USERSTREAM_EOF not implemented, assume EOF too

                return false;
            }
        }

        #endregion

        #region Raw Seeking (optional)

        public override bool CanSeek { get { return true; } }

        protected override int RawTell()
        {
            return (int)UserWrapper.InvokeWrapperMethod(USERSTREAM_TELL).ToLong();

        }

        protected override bool RawSeek(int offset, SeekOrigin whence)
        {
            // stream_seek:
            return UserWrapper.InvokeWrapperMethod(USERSTREAM_SEEK, (PhpValue)offset, (PhpValue)(int)whence).ToBoolean();
        }

        /// <summary>
        /// Returns the Length property of the underlying stream.
        /// </summary>
        /// <returns></returns>
        protected override int RawLength()
        {
            try
            {
                return -1;
            }
            catch (Exception)
            {
                PhpException.Throw(PhpError.Warning, Core.Resources.ErrResources.wrapper_op_unsupported, "Seek");
                return -1;
            }
        }

        #endregion

        #region PhpUserStream properties

        /// <summary><see cref="UserStreamWrapper"/>.</summary>
        protected UserStreamWrapper/*!*/UserWrapper => (UserStreamWrapper)Wrapper;

        /// <summary>
        /// Gets current context.
        /// </summary>
        protected Context/*!*/RuntimeContext => (Context)_encoding;    // _encoding is Context, see .ctor

        #endregion
    }

    #endregion
}
