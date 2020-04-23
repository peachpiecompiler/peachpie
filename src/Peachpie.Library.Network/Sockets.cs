using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Pchp.Core;
using Pchp.Core.Resources;

namespace Peachpie.Library.Network
{
    /// <summary>
    /// PHP socket resource.
    /// </summary>
    class SocketResource : PhpResource
    {
        public Socket Socket { get; }

        public SocketResource(Socket socket) : base("socket")
        {
            this.Socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        protected override void FreeManaged()
        {
            this.Socket.Close();
        }

        public static SocketResource GetValid(PhpResource resource)
        {
            if (resource is SocketResource s && s.IsValid)
            {
                return s;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, ErrResources.invalid_socket_resource);
                return null;
            }
        }
    }

    #region Helpers

    /// <summary>
    /// Helper socket methods.
    /// </summary>
    static class SocketsExtension
    {
        public static AddressFamily GetAddressFamily(this Sockets.PhpAddressFamily af) => af switch
        {
            Sockets.PhpAddressFamily.UNIX => AddressFamily.Unix,
            Sockets.PhpAddressFamily.INET => AddressFamily.InterNetwork,
            Sockets.PhpAddressFamily.INET6 => AddressFamily.InterNetworkV6,
            _ => default,
        };

        public static SocketType GetSocketType(this Sockets.PhpSocketType type) => type switch
        {
            Sockets.PhpSocketType.STREAM => SocketType.Stream,
            Sockets.PhpSocketType.DGRAM => SocketType.Dgram,
            Sockets.PhpSocketType.RAW => SocketType.Raw,
            Sockets.PhpSocketType.SEQPACKET => SocketType.Seqpacket,
            Sockets.PhpSocketType.RDM => SocketType.Rdm,
            _ => default,
        };
    }

    #endregion

    /// <summary>
    /// "socket" extension functions.
    /// </summary>
    [PhpExtension("sockets")]
    public static class Sockets
    {
        #region Constants

        public const int AF_UNIX = 1;
        public const int AF_INET = 2;
        public const int AF_INET6 = 23;

        /// <summary>
        /// Socket family to be used for creating new sockets.
        /// </summary>
        public enum PhpAddressFamily
        {
            UNIX = AF_UNIX,
            INET = AF_INET,
            INET6 = AF_INET6,
        }

        public const int SOCK_STREAM = 1;
        public const int SOCK_DGRAM = 2;
        public const int SOCK_RAW = 3;
        public const int SOCK_SEQPACKET = 5;
        public const int SOCK_RDM = 4;

        /// <summary>
        /// Socket underlying communication type.
        /// </summary>
        public enum PhpSocketType
        {
            STREAM = SOCK_STREAM,
            DGRAM = SOCK_DGRAM,
            RAW = SOCK_RAW,
            SEQPACKET = SOCK_SEQPACKET,
            RDM = SOCK_RDM,
        }

        public const int SOL_SOCKET = 65535;
        public const int SOL_TCP = (int)ProtocolType.Tcp;
        public const int SOL_UDP = (int)ProtocolType.Udp;

        #endregion

        static void HandleException(SocketResource resource, Exception ex)
        {
            PhpException.Throw(PhpError.Warning, ex.Message);
            // TODO: remember last error
        }

        //socket_accept — Accepts a connection on a socket

        //socket_addrinfo_bind — Create and bind to a socket from a given addrinfo
        //socket_addrinfo_connect — Create and connect to a socket from a given addrinfo
        //socket_addrinfo_explain — Get information about addrinfo
        //socket_addrinfo_lookup — Get array with contents of getaddrinfo about the given hostname

        /// <summary>
        /// Binds a name to a socket.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="address"></param>
        /// <param name="port">The port parameter is only used when binding an <see cref="AF_INET"/> socket, and designates the port on which to listen for connections.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool socket_bind(PhpResource socket, string address, int port = 0)
        {
            var s = SocketResource.GetValid(socket);
            if (s == null)
            {
                return false;
            }

            switch (s.Socket.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                case AddressFamily.InterNetworkV6:
                    // address is IP address
                    // port is used
                    if (IPAddress.TryParse(address, out var ipaddress))
                    {
                        var endpoint = new IPEndPoint(ipaddress, port);
                        try
                        {
                            s.Socket.Bind(endpoint);
                            return true;
                        }
                        catch (SocketException ex)
                        {
                            HandleException(s, ex);
                        }
                    }

                    return false;

                default:
                    PhpException.ArgumentValueNotSupported(nameof(s.Socket.AddressFamily), s.Socket.AddressFamily);
                    return false;
            }
        }

        //socket_clear_error — Clears the error on the socket or the last error code

        /// <summary>
        /// Closes a socket resource.
        /// </summary>
        public static void socket_close(PhpResource socket)
        {
            SocketResource.GetValid(socket)?.Dispose();
        }

        //socket_cmsg_space — Calculate message buffer size
        //socket_connect — Initiates a connection on a socket
        //socket_create_listen — Opens a socket on port to accept connections
        //socket_create_pair — Creates a pair of indistinguishable sockets and stores them in an array

        /// <summary>
        /// Create a socket(endpoint for communication).
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="type"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static PhpResource socket_create(PhpAddressFamily domain, PhpSocketType type, ProtocolType protocol)
        {
            // TODO: validate arguments and return FALSE
            return new SocketResource(new Socket(domain.GetAddressFamily(), type.GetSocketType(), protocol));
        }

        //socket_export_stream — Export a socket extension resource into a stream that encapsulates a socket
        //socket_get_option — Gets socket options for the socket
        //socket_getopt — Alias of socket_get_option
        //socket_getpeername — Queries the remote side of the given socket which may either result in host/port or in a Unix filesystem path, dependent on its type
        //socket_getsockname — Queries the local side of the given socket which may either result in host/port or in a Unix filesystem path, dependent on its type
        //socket_import_stream — Import a stream
        //socket_last_error — Returns the last error on the socket

        /// <summary>
        /// Listens for a connection on a socket.
        /// </summary>
        public static bool socket_listen(PhpResource socket, int backlog = 0)
        {
            var s = SocketResource.GetValid(socket);
            if (s == null)
            {
                return false;
            }

            // applicable only to sockets of type SOCK_STREAM or SOCK_SEQPACKET
            switch (s.Socket.SocketType)
            {
                case SocketType.Stream:
                case SocketType.Seqpacket:

                    try
                    {
                        s.Socket.Listen(backlog);
                        return true;
                    }
                    catch (SocketException ex)
                    {
                        HandleException(s, ex);
                        return false;
                    }

                default:
                    PhpException.ArgumentValueNotSupported(nameof(SocketType), s.Socket.SocketType);
                    return false;
            }
        }

        //socket_read — Reads a maximum of length bytes from a socket
        //socket_recv — Receives data from a connected socket
        //socket_recvfrom — Receives data from a socket whether or not it is connection-oriented
        //socket_recvmsg — Read a message
        //socket_select — Runs the select() system call on the given arrays of sockets with a specified timeout
        //socket_send — Sends data to a connected socket
        //socket_sendmsg — Send a message
        //socket_sendto — Sends a message to a socket, whether it is connected or not
        //socket_set_block — Sets blocking mode on a socket resource
        //socket_set_nonblock — Sets nonblocking mode for file descriptor fd
        //socket_set_option — Sets socket options for the socket
        //socket_setopt — Alias of socket_set_option
        //socket_shutdown — Shuts down a socket for receiving, sending, or both
        //socket_strerror — Return a string describing a socket error
        //socket_write — Write to a socket
        //socket_wsaprotocol_info_export — Exports the WSAPROTOCOL_INFO Structure
        //socket_wsaprotocol_info_import — Imports a Socket from another Process
        //socket_wsaprotocol_info_release — Releases an exported WSAPROTOCOL_INFO Structure
    }
}
