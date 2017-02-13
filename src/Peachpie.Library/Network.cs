using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Library.Streams;

namespace Pchp.Library
{
    /// <summary>
	/// Socket functions.
	/// </summary>
    public static class Network
    {
        #region Constants

        /// <summary>
        /// Types of the DNS record.
        /// </summary>
        [PhpHidden]
        public enum DnsRecordType
        {
            /// <summary>IPv4 Address Resource</summary>
            Ip4Address,

            /// <summary>Mail Exchanger Resource</summary>
            Mail,

            /// <summary>Alias (Canonical Name) Resource</summary>
            Alias,

            /// <summary>Authoritative Name Server Resource.</summary>
            NameServer,

            /// <summary>Pointer Resource.</summary>
            Pointer,

            /// <summary>Host Info Resource.</summary>
            HostInfo,

            /// <summary>Start of Authority Resource.</summary>
            StartOfAuthority,

            /// <summary>Text Resource.</summary>
            Text,

            /// <summary>Any Resource Record.</summary>
            Any,

            /// <summary>IPv6 Address Resource</summary>
            Ip6Address,

            /// <summary>Iteratively query the name server for each available record type.</summary>
            All
        }

        public const int DNS_A = (int)DnsRecordType.Ip4Address;
        public const int DNS_MX = (int)DnsRecordType.Mail;
        public const int DNS_CNAME = (int)DnsRecordType.Alias;
        public const int DNS_NS = (int)DnsRecordType.NameServer;
        public const int DNS_PTR = (int)DnsRecordType.Pointer;
        public const int DNS_HINFO = (int)DnsRecordType.HostInfo;
        public const int DNS_SOA = (int)DnsRecordType.StartOfAuthority;
        public const int DNS_TXT = (int)DnsRecordType.Text;
        public const int DNS_ANY = (int)DnsRecordType.Any;
        public const int DNS_AAAA = (int)DnsRecordType.Ip6Address;
        public const int DNS_ALL = (int)DnsRecordType.All;

        #endregion

        #region pfsockopen

        [return: CastToFalse]
        public static PhpResource pfsockopen(Context ctx, string target, int port)
        {
            int errno;
            string errstr;
            return fsockopen(ctx, target, port, out errno, out errstr, ctx.Configuration.Core.DefaultSocketTimeout, true);
        }

        [return: CastToFalse]
        public static PhpResource pfsockopen(Context ctx, string target, int port, out int errno)
        {
            string errstr;
            return fsockopen(ctx, target, port, out errno, out errstr, ctx.Configuration.Core.DefaultSocketTimeout, true);
        }

        [return: CastToFalse]
        public static PhpResource pfsockopen(Context ctx, string target, int port, out int errno, out string errstr)
        {
            return fsockopen(ctx, target, port, out errno, out errstr, ctx.Configuration.Core.DefaultSocketTimeout, true);
        }

        [return: CastToFalse]
        public static PhpResource pfsockopen(Context ctx, string target, int port, out int errno, out string errstr, double timeout)
        {
            return fsockopen(ctx, target, port, out errno, out errstr, timeout, true);
        }

        #endregion

        #region fsockopen

        public static PhpResource fsockopen(Context ctx, string target, int port)
        {
            int errno;
            string errstr;
            return fsockopen(ctx, target, port, out errno, out errstr);
        }

        public static PhpResource fsockopen(Context ctx, string target, int port, out int errno)
        {
            string errstr;
            return fsockopen(ctx, target, port, out errno, out errstr, ctx.Configuration.Core.DefaultSocketTimeout, false);
        }

        public static PhpResource fsockopen(Context ctx, string target, int port, out int errno, out string errstr)
        {
            return fsockopen(ctx, target, port, out errno, out errstr, ctx.Configuration.Core.DefaultSocketTimeout, false);
        }

        public static PhpResource fsockopen(Context ctx, string target, int port, out int errno, out string errstr, double timeout, bool persistent = false)
        {
            return StreamSocket.Connect(ctx, target, port, out errno, out errstr, timeout,
              persistent ? StreamSocket.SocketOptions.Persistent : StreamSocket.SocketOptions.None,
              StreamContext.Default);
        }

        #endregion

        #region socket_get_status, socket_set_blocking, socket_set_timeout

        /// <summary>
        /// Gets status.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <returns>The array containing status info.</returns>
        public static PhpArray socket_get_status(PhpResource stream)
        {
            return PhpStreams.stream_get_meta_data(stream);
        }

        /// <summary>
        /// Sets blocking mode.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <param name="mode">A mode.</param>
        public static bool socket_set_blocking(PhpResource stream, int mode)
        {
            return PhpStreams.stream_set_blocking(stream, mode);
        }


        /// <summary>
        /// Sets a timeout.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <param name="seconds">Seconds part of the timeout.</param>
        /// <param name="microseconds">Microseconds part of the timeout.</param>
        public static bool socket_set_timeout(PhpResource stream, int seconds, int microseconds = 0)
        {
            return PhpStreams.stream_set_timeout(stream, seconds, microseconds);
        }

        #endregion
    }

    /// <summary>Functions working with DNS.</summary>
	public static class PhpDns
    {
        //#region NS: dns_check_record, checkdnsrr

        //       /// <summary>
        //       /// Not supported.
        //       /// </summary>
        //       [ImplementsFunction("checkdnsrr", FunctionImplOptions.NotSupported)]
        //       public static int CheckRecordRows(string host)
        //       {
        //           return CheckRecords(host, "MX");
        //       }

        //       /// <summary>
        //       /// Not supported.
        //       /// </summary>
        //       [ImplementsFunction("checkdnsrr", FunctionImplOptions.NotSupported)]
        //       public static int CheckRecordRows(string host, string type)
        //       {
        //           return CheckRecords(host, type);
        //       }

        //       /// <summary>
        //       /// Not supported.
        //       /// </summary>
        //       [ImplementsFunction("dns_check_record", FunctionImplOptions.NotSupported)]
        //       public static int CheckRecords(string host, string type)
        //       {
        //           PhpException.FunctionNotSupported();
        //           return 0;
        //       }


        //       #endregion

        //       #region NS: dns_get_record

        //       /// <summary>
        //       /// Not supported.
        //       /// </summary>
        //       [ImplementsFunction("dns_get_record", FunctionImplOptions.NotSupported)]
        //       public static PhpArray GetRecord(string host)
        //       {
        //           return GetRecord(host, DnsRecordType.All);
        //       }

        //       /// <summary>
        //       /// Not supported.
        //       /// </summary>
        //       [ImplementsFunction("dns_get_record", FunctionImplOptions.NotSupported)]
        //       public static PhpArray GetRecord(string host, DnsRecordType type)
        //       {
        //           PhpException.FunctionNotSupported();
        //           return null;
        //       }

        //       /// <summary>
        //       /// Not supported.
        //       /// </summary>
        //       [ImplementsFunction("dns_get_record", FunctionImplOptions.NotSupported)]
        //       public static PhpArray GetRecord(string host, DnsRecordType type, out PhpArray authNS, out PhpArray additional)
        //       {
        //           PhpException.FunctionNotSupported();
        //           authNS = null;
        //           additional = null;
        //           return null;
        //       }

        //       #endregion

        #region gethostbyaddr, gethostbyname, gethostbynamel

        /// <summary>
        /// Gets the Internet host name corresponding to a given IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        /// <returns>The host name or unmodified <paramref name="ipAddress"/> on failure.</returns>
        public static string gethostbyaddr(string ipAddress)
        {
            try
            {
                return Dns.GetHostEntryAsync(ipAddress).Result.HostName;
            }
            catch (System.Exception)
            {
                return ipAddress;
            }
        }

        /// <summary>
        /// Gets the IP address corresponding to a given Internet host name.
        /// </summary>
        /// <param name="hostName">The host name.</param>
        /// <returns>The IP address or unmodified <paramref name="hostName"/> on failure.</returns>
        public static string gethostbyname(string hostName)
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostEntryAsync(hostName).Result.AddressList;
                return (addresses.Length > 0) ? addresses[0].ToString() : hostName;
            }
            catch (System.Exception)
            {
                return hostName;
            }
        }

        /// <summary>
        /// Gets a list of IP addresses corresponding to a given Internet host name.
        /// </summary>
        /// <param name="hostName">The host name.</param>
        /// <returns>The list of IP addresses to which the Internet host specified by <paramref name="hostName"/> resolves.
        /// </returns>
        [return: CastToFalse]
        public static PhpArray gethostbynamel(string hostName)
        {
            try
            {

                IPAddress[] addresses = Dns.GetHostEntryAsync(hostName).Result.AddressList;
                var result = new PhpArray(addresses.Length);

                foreach (IPAddress address in addresses)
                {
                    result.Add(address.ToString());
                }

                return result;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        #endregion

        #region getmxrr, dns_get_mx

        /// <summary>
        /// Not supported.
        /// </summary>
        public static bool getmxrr(string hostName, PhpArray mxHosts, PhpArray weight = null)
        {
            return dns_get_mx(hostName, mxHosts, weight);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public static bool dns_get_mx(string hostName, PhpArray mxHosts, PhpArray weight = null)
        {
            PhpException.FunctionNotSupported("dns_get_mx");
            return false;
        }

        #endregion

        #region getprotobyname, getprotobynumber, getservbyname, getservbyport

        ///// <summary>
        ///// Returns protocol number associated with a given protocol name.
        ///// </summary>
        ///// <param name="name">The protocol name.</param>
        ///// <returns>The protocol number or <c>-1</c> if <paramref name="name"/> is not found.</returns>
        //[return: CastToFalse]
        //public static int getprotobyname(string name)
        //{
        //    if (string.IsNullOrEmpty(name)) return -1;

        //    NetworkUtils.ProtoEnt ent = NetworkUtils.GetProtocolByName(name);
        //    if (ent == null) return -1;
        //    return ent.p_proto;
        //}

        ///// <summary>
        ///// Returns protocol name associated with a given protocol number.
        ///// </summary>
        ///// <param name="number">The protocol number.</param>
        ///// <returns>The protocol name or <B>null</B> if <paramref name="number"/> is not found.</returns>
        //[ImplementsFunction("getprotobynumber")]
        //[return: CastToFalse]
        //public static string GetProtocolByNumber(int number)
        //{
        //    NetworkUtils.ProtoEnt ent = NetworkUtils.GetProtocolByNumber(number);
        //    if (ent == null) return null;
        //    return ent.p_name;
        //}

        ///// <summary>
        ///// Returns port number associated with a given Internet service and protocol.
        ///// </summary>
        ///// <param name="service">The service.</param>
        ///// <param name="protocol">The protocol.</param>
        ///// <returns>The port number or <c>-1</c> if not found.</returns>
        //[ImplementsFunction("getservbyname")]
        //[return: CastToFalse]
        //public static int GetServiceByName(string service, string protocol)
        //{
        //    if (service == null) return -1;

        //    NetworkUtils.ServEnt ent = NetworkUtils.GetServiceByName(service, protocol);
        //    if (ent == null) return -1;
        //    return IPAddress.NetworkToHostOrder(ent.s_port);
        //}

        ///// <summary>
        ///// Returns an Internet service that corresponds to a given port and protocol.
        ///// </summary>
        ///// <param name="port">The port.</param>
        ///// <param name="protocol">The protocol.</param>
        ///// <returns>The service name or <B>null</B> if not found.</returns>
        //[ImplementsFunction("getservbyport")]
        //[return: CastToFalse]
        //public static string GetServiceByPort(int port, string protocol)
        //{
        //    NetworkUtils.ServEnt ent = NetworkUtils.GetServiceByPort(IPAddress.HostToNetworkOrder(port), protocol);
        //    if (ent == null) return null;
        //    return ent.s_proto;
        //}

        #endregion

        #region ip2long, long2ip

        /// <summary>
        /// Converts a string containing an (IPv4) Internet Protocol dotted address into a proper address.
        /// </summary>
        /// <param name="ipAddress">The string representation of the address.</param>
        /// <returns>The integer representation of the address.</returns>
        [return: CastToFalse]
        public static int ip2long(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                return -1;
            }

            IPAddress addr;
            try
            {
                addr = IPAddress.Parse(ipAddress);
            }
            catch (FormatException)
            {
                return -1;
            }

            if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return -1;
            }

            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(addr.GetAddressBytes(), 0));
        }

        /// <summary>
        /// Converts an (IPv4) Internet network address into a string in Internet standard dotted format.
        /// </summary>
        /// <param name="properAddress">The integer representation of the address.</param>
        /// <returns>The string representation of the address.</returns>
        public static string long2ip(int properAddress)
        {
            IPAddress addr;
            unchecked
            {
                addr = new IPAddress((long)(uint)IPAddress.HostToNetworkOrder(properAddress));
            }
            return addr.ToString();
        }

        #endregion
    }
}
