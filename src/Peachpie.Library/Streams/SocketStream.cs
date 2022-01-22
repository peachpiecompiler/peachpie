﻿using Pchp.Core;
using Pchp.Core.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Pchp.Library.Streams
{
    /// <summary>
	/// An implementation of <see cref="PhpStream"/> as an encapsulation 
	/// of System.Net.Socket transports.
	/// </summary>
	public class SocketStream : PhpStream, IPhpConvertible
    {
        public override bool CanReadWithoutLock
        {
            get
            {
                // NOTE:
                // when using SslStream, it has already read data from underlaying Socket
                // so Poll always returns FALSE and Socket.Available is almost always 0

                return Socket != null && (Socket.Poll(0, SelectMode.SelectRead) || SslStream != null);
            }
        }

        public override bool CanWriteWithoutLock
        {
            get
            {
                return Socket != null && Socket.Poll(0, SelectMode.SelectWrite);
            }
        }


        /// <summary>
        /// The encapsulated network socket.
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        /// Optionally, SSL stream wrapping the socket if encryption is enabled.
        /// </summary>
        public SslStream SslStream { get; set; }

        /// <summary>
        /// Result of the last read/write operation.
        /// </summary>
        protected bool eof;

        #region PhpStream overrides

        public SocketStream(Context ctx, Socket socket, string openedPath, StreamContext context)
            : base(ctx, null, StreamAccessOptions.Read | StreamAccessOptions.Write, openedPath, context)
        {
            Debug.Assert(socket != null);
            this.Socket = socket;
            this.IsWriteBuffered = false;
            this.eof = false;
            this.IsReadBuffered = false;
        }

        protected override void FreeManaged()
        {
            base.FreeManaged();

            CloseSslStream();

            if (Socket != null)
            {
                Socket.Dispose();    // .Close()
                Socket = null;
            }
        }

        internal void CloseSslStream()
        {
            if (SslStream != null)
            {
                SslStream.Flush();
                SslStream.Dispose();
                SslStream = null;
            }
        }

        #endregion
        #region IPhpConvertible

        //(int)thesocket should be the native pointer of thesocket,justify this behavior to php
        long IPhpConvertible.ToLong() => IsValid ? Socket.Handle.ToInt64() : 0;

        Pchp.Core.Convert.NumberInfo IPhpConvertible.ToNumber(out PhpNumber number)
        {
            var temp = IsValid ? Socket.Handle.ToInt64() : 0;
            number = PhpNumber.Create(temp);
            return Pchp.Core.Convert.NumberInfo.LongInteger;
        }

        #endregion

        #region Raw byte access (mandatory)

        protected override int RawRead(byte[] buffer, int offset, int count)
        {
            try
            {
                int rv;

                if (SslStream != null)
                {
                    // we have stream:
                    rv = SslStream.Read(buffer, offset, count);
                }
                else
                {
                    // raw socket:
                    rv = Socket.Receive(buffer, offset, count, SocketFlags.None);
                }

                eof = rv == 0;
                return rv;
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
                PhpException.Throw(PhpError.Warning, ErrResources.stream_socket_error, e.Message);
                return -1;
            }
        }

        protected override int RawWrite(byte[] buffer, int offset, int count)
        {
            try
            {
                // we have stream:
                if (SslStream != null)
                {
                    SslStream.Write(buffer, offset, count);
                    return count;
                }

                // raw socket:
                int rv = Socket.Send(buffer, offset, count, SocketFlags.None);
                eof = rv == 0;
                return rv;
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
            SslStream?.Flush();

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
            switch (option)
            {
                case StreamParameterOptions.ReadTimeout:
                    // value is in seconds
                    var timeout = (int)(value.ToDouble() * 1000.0); // milliseconds
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout);
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, timeout);
                    return true;

                case StreamParameterOptions.BlockingMode:
                    // value is boolean indicating whether blocking should be enabled
                    Socket.Blocking = (bool)value;
                    return true;
            }

            //
            return base.SetParameter(option, value);
        }

        #endregion

        #region Conversion to .NET native Stream (NS)

        public override Stream RawStream
        {
            get
            {
                return SslStream ?? throw new NotImplementedException();
            }
        }

        #endregion

        new public static SocketStream GetValid(PhpResource handle)
        {
            if (handle is SocketStream result && result.IsValid)
            {
                return result;
            }

            PhpException.Throw(PhpError.Warning, ErrResources.invalid_socket_stream_resource);
            return null;
        }
    }
}
