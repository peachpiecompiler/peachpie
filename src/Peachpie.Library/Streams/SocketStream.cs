using Pchp.Core;
using Pchp.Core.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Pchp.Library.Streams
{
    /// <summary>
	/// An implementation of <see cref="PhpStream"/> as an encapsulation 
	/// of System.Net.Socket transports.
	/// </summary>
	public class SocketStream : PhpStream
    {
        public override bool CanReadWithoutLock => socket.Available > 0 && (currentTask == null || currentTask.IsCompleted);

        public override bool CanWriteWithoutLock => currentTask == null || currentTask.IsCompleted;

        /// <summary>
        /// The encapsulated network socket.
        /// </summary>
        protected Socket socket;

        /// <summary>
        /// Result of the last read/write operation.
        /// </summary>
        protected bool eof;

        private bool isAsync;
        private Task currentTask;

        #region PhpStream overrides

        public SocketStream(Context ctx, Socket socket, string openedPath, StreamContext context, bool isAsync = false)
            : base(ctx, null, StreamAccessOptions.Read | StreamAccessOptions.Write, openedPath, context)
        {
            Debug.Assert(socket != null);
            this.socket = socket;
            this.IsWriteBuffered = false;
            this.eof = false;
            this.isAsync = isAsync;
            this.IsReadBuffered = false;
        }

        /// <summary>
        /// PhpResource.FreeManaged overridden to get rid of the contained context on Dispose.
        /// </summary>
        protected override void FreeManaged()
        {
            base.FreeManaged();
            socket.Dispose();    // .Close()
            socket = null;
        }

        #endregion

        #region Raw byte access (mandatory)

        protected override int RawRead(byte[] buffer, int offset, int count)
        {
            if (currentTask != null)
                currentTask.Wait();
            try
            {
                int rv = socket.Receive(buffer, offset, count, SocketFlags.None);
                eof = rv == 0;
                return rv;
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_socket_error, e.Message);
                return 0;
            }
        }

        protected override int RawWrite(byte[] buffer, int offset, int count)
        {
            try
            {
                if (isAsync)
                {
                    if (currentTask != null)
                        currentTask.Wait();
                    currentTask = new Task(() =>
                    {
                        int rv = socket.Send(buffer, offset, count, SocketFlags.None);
                        eof = rv == 0;
                    });
                    currentTask.Start();
                    return count;
                }
                else
                {
                    int rv = socket.Send(buffer, offset, count, SocketFlags.None);
                    eof = rv == 0;
                    return rv;
                }
            }
            catch (System.Exception e)
            {
                PhpException.Throw(PhpError.Warning, ErrResources.stream_socket_error, e.Message);
                return 0;
            }
        }

        protected override bool RawFlush()
        {
            return true;
        }

        protected override bool RawEof
        {
            get
            {
                return eof;
            }
        }

        public override bool SetParameter(StreamParameterOptions option, PhpValue value)
        {
            if (option == StreamParameterOptions.ReadTimeout)
            {
                int timeout = (int)(value.ToDouble() * 1000.0);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, timeout);
                return true;
            }
            return base.SetParameter(option, value);
        }

        #endregion

        #region Conversion to .NET native Stream (NS)

        public override Stream RawStream
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        new public static SocketStream GetValid(PhpResource handle)
        {
            SocketStream result = handle as SocketStream;
            if (result == null)
                PhpException.Throw(PhpError.Warning, ErrResources.invalid_socket_stream_resource);
            return result;
        }
    }
}
