using Pchp.Core;
using Pchp.Core.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library.Streams
{
    /// <summary>
	/// An implementation of <see cref="PhpStream"/> as a simple
	/// encapsulation of a .NET <see cref="System.IO.Stream"/> class
	/// which is directly accessible via the RawStream property.
	/// </summary>
	public class NativeStream : PhpStream
    {
        #region PhpStream overrides

        public NativeStream(IEncodingProvider enc_provider, Stream nativeStream, StreamWrapper openingWrapper, StreamAccessOptions accessOptions, string openedPath, StreamContext context)
            : base(enc_provider, openingWrapper, accessOptions, openedPath, context)
        {
            Debug.Assert(nativeStream != null);
            this.stream = nativeStream;
        }

        /// <summary>
        /// PhpResource.FreeManaged overridden to get rid of the contained context on Dispose.
        /// </summary>
        protected override void FreeManaged()
        {
            base.FreeManaged();
            try
            {
                stream.Dispose();
            }
            catch (NotSupportedException)
            {
            }

            if (Wrapper != null)    //Can be php://output
            {
                Wrapper.Dispose();
            }

            stream = null;
        }

        #endregion

        #region Raw byte access (mandatory)

        protected override int RawRead(byte[] buffer, int offset, int count)
        {
            try
            {
                int read = stream.Read(buffer, offset, count);
                if (read == 0) reportEof = true;
                return read;
            }
            catch (NotSupportedException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Read");
                return -1;
            }
            catch (IOException e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_read_io_error, e.Message);
                return -1;
            }
            catch (System.Exception e)
            {
                // For example WebException (timeout)
                PhpException.Throw(PhpError.Warning, ErrResources.stream_read_error, e.Message);
                return -1;
            }
        }

        protected override int RawWrite(byte[] buffer, int offset, int count)
        {
            long position = stream.CanSeek ? stream.Position : -1;

            try
            {
                stream.WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
                return stream.CanSeek ? unchecked((int)(stream.Position - position)) : count;
            }
            catch (NotSupportedException)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Write");
                return -1;
            }
            catch (IOException e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_write_io_error, e.Message);
                return -1;
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_write_error, e.Message);
                return -1;
            }
        }

        protected override bool RawFlush()
        {
            if (stream.CanWrite) stream.Flush();
            return true;
        }

        protected override bool RawEof
        {
            get
            {
                return stream.CanSeek
                    ? stream.Position == stream.Length
                    : reportEof;

                // Otherwise there is no apriori information - will be revealed at next read...
            }
        }

        /// <summary>EOF stored at the time of the last read.</summary>
        private bool reportEof = false;

        #endregion

        #region Raw Seeking (optional)

        public override bool CanSeek { get { return stream.CanSeek; } }

        protected override int RawTell()
        {
            return unchecked((int)stream.Position);
        }

        protected override bool RawSeek(int offset, SeekOrigin whence)
        {
            // Store the current position to be able to check for seek()'s success.
            long position = stream.Position;
            return stream.Seek(offset, (SeekOrigin)whence)
                == SeekExpects(position, stream.Length, offset, whence);
        }

        /// <summary>
        /// Returns the Length property of the underlying stream.
        /// </summary>
        /// <returns></returns>
        protected override int RawLength()
        {
            try
            {
                return unchecked((int)stream.Length);
            }
            catch (System.Exception)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.wrapper_op_unsupported, "Seek");
                return -1;
            }
        }


        /// <summary>
        /// Get the expected position in the stream to check for Seek() failure.
        /// </summary>
        /// <param name="position">Actual position in the stream.</param>
        /// <param name="length">The length of the stream.</param>
        /// <param name="offset">The offset for the seek() operation.</param>
        /// <param name="whence">Where to count the new position from.</param>
        /// <returns>The expected new position.</returns>
        protected long SeekExpects(long position, long length, long offset, SeekOrigin whence)
        {
            switch (whence)
            {
                case SeekOrigin.Begin:
                    return offset;
                case SeekOrigin.Current:
                    return position + offset;
                case SeekOrigin.End:
                    return length + offset;
                default:
                    return -1;
            }
        }

        #endregion

        #region Conversion to .NET native Stream
        //    /// <include file='Doc/Streams.xml' path='docs/property[@name="CanCast"]/*'/>
        //    public override bool CanCast { get { return true; } }

        public override Stream RawStream => stream;

        #endregion

        #region NativeStream properties

        /// <summary>The encapsulated native stream.</summary>
        protected Stream stream;

        #endregion
    }
}
