using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Pchp.Library.Streams
{
    /// <summary>
	/// Gives access to various network-based stream properties.
	/// </summary>
	/// <threadsafety static="true"/>
    [PhpExtension("standard")]
    public static class StreamSocket
    {
        #region Enums

        /// <summary>
        /// Options used for <see cref="StreamSocket.Connect"/>.
        /// </summary>
        [PhpHidden]
        public enum SocketOptions
        {
            /// <summary>
            /// Default option.
            /// </summary>
            None = 0,

            /// <summary>
            /// Client socket opened with <c>stream_socket_client</c> should remain persistent
            /// between page loads.
            /// </summary>
            Persistent = 1,

            /// <summary>
            /// Open client socket asynchronously.
            /// </summary>
            Asynchronous = 2
        }

        public const int STREAM_CLIENT_CONNECT = (int)SocketOptions.None;
        public const int STREAM_CLIENT_PERSISTENT = (int)SocketOptions.Persistent;
        public const int STREAM_CLIENT_ASYNC_CONNECT = (int)SocketOptions.Asynchronous;

        [PhpHidden]
        public enum _AddressFamily
        {
            InterNetwork = AddressFamily.InterNetwork,
            InterNetworkV6 = AddressFamily.InterNetworkV6,
            Unix = AddressFamily.Unix
        }

        public const int STREAM_PF_INET = (int)_AddressFamily.InterNetwork;
        public const int STREAM_PF_INET6 = (int)_AddressFamily.InterNetworkV6;
        public const int STREAM_PF_UNIX = (int)_AddressFamily.Unix;

        [PhpHidden]
        public enum _SocketType
        {
            Unknown = SocketType.Unknown,
            Stream = SocketType.Stream,
            Dgram = SocketType.Dgram,
            Raw = SocketType.Raw,
            Rdm = SocketType.Rdm,
            Seqpacket = SocketType.Seqpacket,
        }

        public const int STREAM_SOCK_STREAM = (int)_SocketType.Stream;
        public const int STREAM_SOCK_DGRAM = (int)_SocketType.Dgram;
        public const int STREAM_SOCK_RAW = (int)_SocketType.Raw;
        public const int STREAM_SOCK_RDM = (int)_SocketType.Rdm;
        public const int STREAM_SOCK_SEQPACKET = (int)_SocketType.Seqpacket;

        [PhpHidden]
        public enum _ProtocolType
        {
            IP = ProtocolType.IP,
            Icmp = ProtocolType.Icmp,
            Tcp = ProtocolType.Tcp,
            Udp = ProtocolType.Udp,
            Raw = ProtocolType.Raw
        }

        public const int STREAM_IPPROTO_IP = (int)_ProtocolType.IP;
        public const int STREAM_IPPROTO_ICMP = (int)_ProtocolType.Icmp;
        public const int STREAM_IPPROTO_TCP = (int)_ProtocolType.Tcp;
        public const int STREAM_IPPROTO_UDP = (int)_ProtocolType.Udp;
        public const int STREAM_IPPROTO_RAW = (int)_ProtocolType.Raw;

        [PhpHidden]
        public enum SendReceiveOptions
        {
            None = 0,
            OutOfBand = 1,
            Peek = 2
        }

        public const int STREAM_OOB = (int)SendReceiveOptions.OutOfBand;
        public const int STREAM_PEEK = (int)SendReceiveOptions.Peek;

        [Flags]
        public enum CryptoMethod
        {
            ANY_CLIENT = STREAM_CRYPTO_METHOD_ANY_CLIENT,
            SSLv2_CLIENT = STREAM_CRYPTO_METHOD_SSLv2_CLIENT,
            SSLv3_CLIENT = STREAM_CRYPTO_METHOD_SSLv3_CLIENT,
            SSLv23_CLIENT = STREAM_CRYPTO_METHOD_SSLv23_CLIENT,
            TLS_CLIENT = STREAM_CRYPTO_METHOD_TLS_CLIENT,
            TLSv1_0_CLIENT = STREAM_CRYPTO_METHOD_TLSv1_0_CLIENT,
            TLSv1_1_CLIENT = STREAM_CRYPTO_METHOD_TLSv1_1_CLIENT,
            TLSv1_2_CLIENT = STREAM_CRYPTO_METHOD_TLSv1_2_CLIENT,
            TLSv1_3_CLIENT = STREAM_CRYPTO_METHOD_TLSv1_3_CLIENT,
            ANY_SERVER = STREAM_CRYPTO_METHOD_ANY_SERVER,
            SSLv2_SERVER = STREAM_CRYPTO_METHOD_SSLv2_SERVER,
            SSLv3_SERVER = STREAM_CRYPTO_METHOD_SSLv3_SERVER,
            SSLv23_SERVER = STREAM_CRYPTO_METHOD_SSLv23_SERVER,
            TLS_SERVER = STREAM_CRYPTO_METHOD_TLS_SERVER,
            TLSv1_0_SERVER = STREAM_CRYPTO_METHOD_TLSv1_0_SERVER,
            TLSv1_1_SERVER = STREAM_CRYPTO_METHOD_TLSv1_1_SERVER,
            TLSv1_2_SERVER = STREAM_CRYPTO_METHOD_TLSv1_2_SERVER,
            TLSv1_3_SERVER = STREAM_CRYPTO_METHOD_TLSv1_3_SERVER,
        }

        public const int STREAM_CRYPTO_METHOD_ANY_CLIENT = 127;
        public const int STREAM_CRYPTO_METHOD_SSLv2_CLIENT = 3;
        public const int STREAM_CRYPTO_METHOD_SSLv3_CLIENT = 5;
        public const int STREAM_CRYPTO_METHOD_SSLv23_CLIENT = 57;
        public const int STREAM_CRYPTO_METHOD_TLS_CLIENT = 121;
        public const int STREAM_CRYPTO_METHOD_TLSv1_0_CLIENT = 9;
        public const int STREAM_CRYPTO_METHOD_TLSv1_1_CLIENT = 17;
        public const int STREAM_CRYPTO_METHOD_TLSv1_2_CLIENT = 33;
        public const int STREAM_CRYPTO_METHOD_TLSv1_3_CLIENT = 65;
        public const int STREAM_CRYPTO_METHOD_ANY_SERVER = 126;
        public const int STREAM_CRYPTO_METHOD_SSLv2_SERVER = 2;
        public const int STREAM_CRYPTO_METHOD_SSLv3_SERVER = 4;
        public const int STREAM_CRYPTO_METHOD_SSLv23_SERVER = 120;
        public const int STREAM_CRYPTO_METHOD_TLS_SERVER = 120;
        public const int STREAM_CRYPTO_METHOD_TLSv1_0_SERVER = 8;
        public const int STREAM_CRYPTO_METHOD_TLSv1_1_SERVER = 16;
        public const int STREAM_CRYPTO_METHOD_TLSv1_2_SERVER = 32;
        public const int STREAM_CRYPTO_METHOD_TLSv1_3_SERVER = 64;

        #endregion

        #region TODO: stream_get_transports, stream_socket_get_name

        /// <summary>Retrieve list of registered socket transports</summary>
        public static PhpArray stream_get_transports()
        {
            PhpException.FunctionNotSupported("stream_get_transports");
            return null;
        }

        /// <summary>
        /// Retrieve the name of the local or remote sockets.
        /// </summary>
        public static string stream_socket_get_name(PhpResource handle, bool wantPeer)
        {
            PhpException.FunctionNotSupported("stream_socket_get_name");
            return null;
        }

        #endregion

        #region stream_socket_client

        //private static void SplitSocketAddressPort(ref string socket, out int port)
        //{
        //	port = 0;
        //	String[] arr = socket.Split(new[] {':'}, 2, StringSplitOptions.RemoveEmptyEntries);
        //	if (arr.Length == 2)
        //	{
        //		socket = arr[0];
        //		port = int.Parse(arr[1]);
        //	}
        //}
        /// <summary>
        /// Open client socket.
        /// </summary>
        public static PhpResource stream_socket_client(Context ctx, string remoteSocket)
        {
            //SplitSocketAddressPort(ref remoteSocket, out port);
            return Connect(ctx, remoteSocket, 0, out var _, out var _, double.NaN, SocketOptions.None, StreamContext.Default);
        }

        /// <summary>
        /// Open client socket.
        /// </summary>
        public static PhpResource stream_socket_client(Context ctx, string remoteSocket, out int errno)
        {
            //SplitSocketAddressPort(ref remoteSocket, out port);
            return Connect(ctx, remoteSocket, 0, out errno, out var _, double.NaN, SocketOptions.None, StreamContext.Default);
        }

        /// <summary>
        /// Open client socket.
        /// </summary>
        public static PhpResource stream_socket_client(Context ctx, string remoteSocket, out int errno, out string errstr, double timeout = double.NaN, SocketOptions flags = SocketOptions.None)
        {
            //SplitSocketAddressPort(ref remoteSocket, out port);
            return Connect(ctx, remoteSocket, 0, out errno, out errstr, timeout, flags, StreamContext.Default);
        }

        /// <summary>
        /// Open client socket.
        /// </summary>
        public static PhpResource stream_socket_client(Context ctx, string remoteSocket, out int errno, out string errstr, double timeout, SocketOptions flags, PhpResource context)
        {
            var sc = StreamContext.GetValid(context);
            if (sc != null)
            {
                //SplitSocketAddressPort(ref remoteSocket, out port);
                return Connect(ctx, remoteSocket, 0, out errno, out errstr, timeout, flags, sc);
            }
            else
            {
                errno = -1;
                errstr = null;
                return null;
            }
        }

        #endregion

        #region TODO: stream_socket_server

        /// <summary>
        /// Open client socket.
        /// </summary>
        public static PhpResource stream_socket_server(Context ctx, string localSocket)
        {
            int errno;
            string errstr;
            int port = 0;
            //SplitSocketAddressPort(ref localSocket, out port);
            return Connect(ctx, localSocket, port, out errno, out errstr, Double.NaN, SocketOptions.None, StreamContext.Default);
        }

        /// <summary>
        /// Open client socket.
        /// </summary>
        public static PhpResource stream_socket_server(Context ctx, string localSocket, out int errno)
        {
            string errstr;
            int port = 0;
            //SplitSocketAddressPort(ref localSocket, out port);
            return Connect(ctx, localSocket, port, out errno, out errstr, Double.NaN, SocketOptions.None, StreamContext.Default);
        }

        /// <summary>
        /// Open client socket.
        /// </summary>
        public static PhpResource stream_socket_server(Context ctx, string localSocket, out int errno, out string errstr, SocketOptions flags = SocketOptions.None)
        {
            int port = 0;
            //SplitSocketAddressPort(ref localSocket, out port);
            return Connect(ctx, localSocket, port, out errno, out errstr, Double.NaN, flags, StreamContext.Default);
        }

        /// <summary>
        /// Open client socket.
        /// </summary>
        public static PhpResource stream_socket_server(Context ctx, string localSocket, out int errno, out string errstr, SocketOptions flags, PhpResource context)
        {
            StreamContext sc = StreamContext.GetValid(context);
            if (sc == null)
            {
                errno = -1;
                errstr = null;
                return null;
            }

            int port = 0;
            //SplitSocketAddressPort(ref localSocket, out port);
            return Connect(ctx, localSocket, port, out errno, out errstr, Double.NaN, flags, sc);
        }

        #endregion

        #region TODO: stream_socket_accept

        /// <summary>
        /// Accepts a connection on a server socket.
        /// </summary>
        public static bool stream_socket_accept(Context ctx, PhpResource serverSocket)
        {
            return stream_socket_accept(serverSocket, ctx.Configuration.Core.DefaultSocketTimeout, out _);
        }

        /// <summary>
        /// Accepts a connection on a server socket.
        /// </summary>
        public static bool stream_socket_accept(PhpResource serverSocket, int timeout)
        {
            string peerName;
            return stream_socket_accept(serverSocket, timeout, out peerName);
        }

        /// <summary>
        /// Accepts a connection on a server socket.
        /// </summary>
        public static bool stream_socket_accept(PhpResource serverSocket, int timeout, out string peerName)
        {
            peerName = "";

            SocketStream stream = SocketStream.GetValid(serverSocket);
            if (stream == null) return false;

            PhpException.FunctionNotSupported("stream_socket_accept");
            return false;
        }

        #endregion

        #region TODO: stream_socket_recvfrom

        public static string stream_socket_recvfrom(PhpResource socket, int length, SendReceiveOptions flags = SendReceiveOptions.None)
        {
            string address;
            return stream_socket_recvfrom(socket, length, flags, out address);
        }

        public static string stream_socket_recvfrom(PhpResource socket, int length, SendReceiveOptions flags,
          out string address)
        {
            address = null;

            SocketStream stream = SocketStream.GetValid(socket);
            if (stream == null) return null;

            PhpException.FunctionNotSupported("stream_socket_recvfrom");
            return null;
        }

        #endregion

        #region TODO: stream_socket_sendto

        public static int stream_socket_sendto(PhpResource socket, string data, SendReceiveOptions flags = SendReceiveOptions.None, string address = null)
        {
            SocketStream stream = SocketStream.GetValid(socket);
            if (stream == null) return -1;

            PhpException.FunctionNotSupported("stream_socket_sendto");
            return -1;
        }

        #endregion

        #region TODO: stream_socket_pair

        //public static PhpArray stream_socket_pair(ProtocolFamily protocolFamily, SocketType type, ProtocolType protocol)
        //{
        //    PhpException.FunctionNotSupported();
        //    return null;
        //}

        #endregion

        #region stream_socket_shutdown

        public const int STREAM_SHUT_RD = (int)SocketShutdown.Receive; // 0;
        public const int STREAM_SHUT_WR = (int)SocketShutdown.Send; // 1;
        public const int STREAM_SHUT_RDWR = (int)SocketShutdown.Both; // 2;

        /// <summary>
        /// Shutdown a full-duplex connection.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="how">One of the following constants:
        /// - STREAM_SHUT_RD (disable further receptions)<br />
        /// - STREAM_SHUT_WR (disable further transmissions)<br />
        /// - STREAM_SHUT_RDWR (disable further receptions and transmissions).<br/>
        /// </param>
        /// <returns></returns>
        public static bool stream_socket_shutdown(PhpResource stream, SocketShutdown how)
        {
            var s = SocketStream.GetValid(stream);
            if (s != null)
            {
                try
                {
                    s.Socket.Shutdown(how);
                }
                catch (SocketException ex)
                {
                    PhpException.Throw(PhpError.Warning, ex.Message);
                    return false;
                }

                return true;
            }

            return false;
        }

        #endregion

        #region stream_socket_enable_crypto

        /// <summary>
        /// Turns encryption on/off on an already connected socket
        /// </summary>
        /// <param name="stream">The stream resource.</param>
        /// <param name="enable">Enable/disable cryptography on the stream.</param>
        /// <returns>Returns TRUE on success, FALSE if negotiation has failed or 0 if there isn't enough data and you should try again (only for non-blocking sockets).</returns>
        public static PhpValue/*int|bool*/ stream_socket_enable_crypto(PhpResource stream, bool enable)
        {
            var s = SocketStream.GetValid(stream);
            if (s != null)
            {
                // obtain crypto_method option
                var crypto_method = default(CryptoMethod);

                if (enable)
                {
                    var ssl_options = s.Context.GetOptions("ssl");
                    if (ssl_options != null && ssl_options.TryGetValue("crypto_method", out var crypto_method_value))
                    {
                        crypto_method = (CryptoMethod)crypto_method_value.ToLong();
                    }
                    else
                    {
                        PhpException.InvalidArgument(nameof(crypto_method)); // 'crypto_method' must be specified when enabling encryption
                        return false;
                    }
                }

                return stream_socket_enable_crypto(s, enable, crypto_method, null);
            }

            //
            return false;
        }

        /// <summary>
        /// Turns encryption on/off on an already connected socket
        /// </summary>
        /// <param name="stream">The stream resource.</param>
        /// <param name="enable">Enable/disable cryptography on the stream.</param>
        /// <param name="crypto_method">Encryption on the stream. If omitted, the <c>crypto_method</c> context option on the stream's SSL context will be used instead.</param>
        /// <param name="session_stream">Seed the stream with settings from session_stream.</param>
        /// <returns>Returns TRUE on success, FALSE if negotiation has failed or 0 if there isn't enough data and you should try again (only for non-blocking sockets).</returns>
        public static PhpValue/*int|bool*/ stream_socket_enable_crypto(PhpResource stream, bool enable, CryptoMethod crypto_method = default, PhpResource session_stream = null)
        {
            var s = SocketStream.GetValid(stream);
            if (s == null)
            {
                return false;
            }

            if (enable && s.SslStream == null)
            {
                s.SslStream = OpenSslStream(s.Socket, s.OpenedPath/*=RemoteHost*/, crypto_method);
                return s.SslStream != null;
            }
            else if (!enable && s.SslStream != null)
            {
                s.CloseSslStream();
                return true;
            }

            //
            return false;
        }

        #endregion

        #region Connect

        static SslStream OpenSslStream(Socket socket, string remoteHost, CryptoMethod cryptomethod)
        {
            var sslstream = new SslStream(new NetworkStream(socket, System.IO.FileAccess.ReadWrite, false), false,
                            null, //(sender, certificate, chain, sslPolicyErrors) => true,
                            null, //(sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => ??,
                            EncryptionPolicy.AllowNoEncryption);

            // TODO: cryptomethod -> SslProtocols

            try
            {
                sslstream.AuthenticateAsClient(remoteHost); // , , SslProtocols, );
            }
            catch (AuthenticationException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return null;
            }

            return sslstream;
        }

        /// <summary>
        /// Opens a new SocketStream
        /// </summary>
        internal static SocketStream Connect(Context ctx, string remoteSocket, int port, out int errno, out string errstr, double timeout, SocketOptions flags, StreamContext/*!*/ context)
        {
            errno = 0;
            errstr = null;

            if (remoteSocket == null)
            {
                PhpException.ArgumentNull(nameof(remoteSocket));
                return null;
            }

            bool isSsl = false;

            // TODO: extract schema (tcp://, udp://) and port from remoteSocket
            // Uri uri = Uri.TryCreate(remoteSocket);
            const string protoSeparator = "://";
            var protocol = ProtocolType.Tcp;
            var protoIdx = remoteSocket.IndexOf(protoSeparator, StringComparison.Ordinal);
            if (protoIdx >= 0)
            {
                var protoStr = remoteSocket.AsSpan(0, protoIdx);
                if (protoStr.Equals("udp".AsSpan(), StringComparison.Ordinal))
                {
                    protocol = ProtocolType.Udp;
                }
                else if (protoStr.Equals("ssl".AsSpan(), StringComparison.Ordinal))
                {
                    // use SSL encryption
                    isSsl = true;
                }

                remoteSocket = remoteSocket.Substring(protoIdx + protoSeparator.Length);
            }

            var colonIdx = remoteSocket.IndexOf(':');
            if (colonIdx >= 0)
            {
                var portStr = remoteSocket.AsSpan(colonIdx + 1);
                if (portStr.Length != 0 &&
                    int.TryParse(portStr.ToString(), out var n) &&    // TODO: (perf) ReadOnlySpan<char>
                    n > 0 && n <= 0xffff)
                {
                    port = n;
                }

                remoteSocket = remoteSocket.Remove(colonIdx);
            }

            if (double.IsNaN(timeout))
            {
                timeout = ctx.Configuration.Core.DefaultSocketTimeout;
            }

            // TODO:
            if (flags != SocketOptions.None && flags != SocketOptions.Asynchronous)
            {
                PhpException.ArgumentValueNotSupported("flags", (int)flags);
            }

            var connect_async = (flags & SocketOptions.Asynchronous) != 0;

            try
            {
                // workitem 299181; for remoteSocket as IPv4 address it results in IPv6 address
                //IPAddress address = System.Net.Dns.GetHostEntry(remoteSocket).AddressList[0];

                if (!IPAddress.TryParse(remoteSocket, out var address)) // if remoteSocket is not a valid IP address then lookup the DNS
                {
                    var addresses = System.Net.Dns.GetHostAddressesAsync(remoteSocket).Result;
                    if (addresses != null && addresses.Length != 0)
                    {
                        address = addresses[0];
                    }
                    else
                    {
                        throw new ArgumentException(nameof(remoteSocket));
                    }
                }

                var socket = new Socket(address.AddressFamily, SocketType.Stream, protocol);

                // socket.Connect(new IPEndPoint(address, port));
                if (socket.ConnectAsync(address, port).Wait((int)(timeout * 1000)))
                {
                    SslStream sslstream;

                    if (isSsl)
                    {
                        var crypto_method = default(CryptoMethod);

                        var options = context.GetOptions("ssl");
                        if (options != null)
                        {
                            // TODO: provide parameters based on context[ssl][verify_peer|verify_peer_name|allow_self_signed|cafile]

                            options.TryGetValue("verify_peer", out var vpvalue);
                            options.TryGetValue("verify_peer_name", out var vpnvalue);
                            options.TryGetValue("allow_self_signed", out var assvalue);
                            options.TryGetValue("cafile", out var cafilevalue);
                            options.TryGetValue("crypto_method", out var crypto_method_value);

                            Debug.WriteLineIf(Operators.IsSet(vpvalue) && !vpvalue, "ssl: verify_peer not supported");
                            Debug.WriteLineIf(Operators.IsSet(vpnvalue) && (bool)vpnvalue, "ssl: verify_peer_name not supported");
                            Debug.WriteLineIf(Operators.IsSet(assvalue) && !assvalue, "ssl: allow_self_signed not supported");
                            Debug.WriteLineIf(Operators.IsSet(cafilevalue), "ssl: cafile not supported");

                            crypto_method = (CryptoMethod)crypto_method_value.ToLong();
                        }

                        sslstream = OpenSslStream(socket, remoteSocket, crypto_method);

                        // stream_socket_* functions work with "SocketStream" object
                        //return new NativeStream(ctx, sslstream, null, StreamAccessOptions.Read | StreamAccessOptions.Write, remoteSocket, context)
                        //{
                        //    IsWriteBuffered = false,
                        //    IsReadBuffered = false,
                        //};
                    }
                    else
                    {
                        sslstream = null;
                    }

                    //
                    return new SocketStream(ctx, socket, remoteSocket, context)
                    {
                        SslStream = sslstream,
                    };
                }
                else
                {
                    Debug.Assert(!socket.Connected);
                    PhpException.Throw(PhpError.Warning, string.Format(Resources.LibResources.socket_open_timeout, FileSystemUtils.StripPassword(remoteSocket)));
                    return null;
                }

            }
            catch (SocketException e)
            {
                errno = (int)e.SocketErrorCode;
                errstr = e.Message;
            }
            catch (System.Exception e)
            {
                errno = -1;
                errstr = e.Message;
            }

            PhpException.Throw(PhpError.Warning, string.Format(Resources.LibResources.socket_open_error, FileSystemUtils.StripPassword(remoteSocket), errstr));
            return null;
        }

        #endregion
    }
}
