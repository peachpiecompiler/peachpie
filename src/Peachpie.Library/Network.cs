using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Library.Streams;
using static Pchp.Library.Network;

namespace Pchp.Library
{
    /// <summary>
	/// Socket functions.
	/// </summary>
    [PhpExtension("standard")]
    public static class Network
    {
        #region Constants

        /// <summary>
        /// Types of the DNS record.
        /// </summary>
        [PhpHidden, Flags]
        public enum DnsRecordType
        {
            /// <summary>IPv4 Address Resource</summary>
            Ip4Address = 1,

            /// <summary>Authoritative Name Server Resource.</summary>
            NameServer = 1 << 1,

            /// <summary>Alias (Canonical Name) Resource</summary>
            Alias = 1 << 4,

            /// <summary>Start of Authority Resource.</summary>
            StartOfAuthority = 1 << 5,

            /// <summary>Pointer Resource.</summary>
            Pointer = 1 << 11,

            /// <summary>Host Info Resource.</summary>
            HostInfo = 1 << 12,

            CertificationAuthorityAuthorization = 1 << 13,

            /// <summary>Mail Exchanger Resource</summary>
            Mail = 1 << 14,

            /// <summary>Text Resource.</summary>
            Text = 1 << 15,

            /// <summary>
            /// IPv6 A6 record.
            /// </summary>
            A6 = 1 << 24,

            ServiceRecord = 1 << 25,

            NameAuthorityPointer = 1 << 26,

            /// <summary>IPv6 Address Resource</summary>
            Ip6Address = 1 << 27,

            /// <summary>Any Resource Record.</summary>
            Any = 1 << 28,
        }

        public const int DNS_A = (int)DnsRecordType.Ip4Address;
        public const int DNS_NS = (int)DnsRecordType.NameServer;
        public const int DNS_CNAME = (int)DnsRecordType.Alias;
        public const int DNS_SOA = (int)DnsRecordType.StartOfAuthority;
        public const int DNS_PTR = (int)DnsRecordType.Pointer;
        public const int DNS_HINFO = (int)DnsRecordType.HostInfo;
        public const int DNS_CAA = (int)DnsRecordType.CertificationAuthorityAuthorization;
        public const int DNS_MX = (int)DnsRecordType.Mail;
        public const int DNS_TXT = (int)DnsRecordType.Text;
        public const int DNS_SRV = (int)DnsRecordType.ServiceRecord;
        public const int DNS_NAPTR = (int)DnsRecordType.NameAuthorityPointer;
        public const int DNS_AAAA = (int)DnsRecordType.Ip6Address;
        public const int DNS_A6 = (int)DnsRecordType.A6;
        public const int DNS_ANY = (int)DnsRecordType.Any;
        public const int DNS_ALL = DNS_A | DNS_NS | DNS_CNAME | DNS_SOA | DNS_PTR | DNS_HINFO | DNS_CAA | DNS_MX | DNS_TXT | DNS_SRV | DNS_NAPTR | DNS_AAAA | DNS_A6 | DNS_ANY;

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

        #region getprotobyname, getprotobynumber

        static IEnumerable<(string name, ProtocolType type)> EnumerateProtocolTypes()
        {
            yield return ("ip", ProtocolType.IP);
            yield return ("icmp", ProtocolType.Icmp);
            yield return ("igmp", ProtocolType.Igmp);
            yield return ("ggp", ProtocolType.Ggp);
            yield return ("tcp", ProtocolType.Tcp);
            //yield return ("egp", ProtocolType.); // 8
            yield return ("pup", ProtocolType.Pup);
            yield return ("udp", ProtocolType.Udp);
            //yield return ("hmp", ProtocolType.); // 20
            yield return ("xns-idp", ProtocolType.Idp);
            //yield return ("rdp", ProtocolType.); // 27
            yield return ("ipv6", ProtocolType.IPv6);
            yield return ("ipv6-route", ProtocolType.IPv6RoutingHeader);
            yield return ("ipv6-frag", ProtocolType.IPv6FragmentHeader);
            yield return ("esp", ProtocolType.IPSecEncapsulatingSecurityPayload);
            yield return ("ah", ProtocolType.IPSecAuthenticationHeader);
            yield return ("ipv6-icmp", ProtocolType.IcmpV6);
            yield return ("ipv6-nonxt", ProtocolType.IPv6NoNextHeader);
            yield return ("ipv6-opts", ProtocolType.IPv6DestinationOptions);
            //yield return ("rvd", ProtocolType.); // 66
            //yield return ("nd", ProtocolType.ND);
            yield return ("ipx", ProtocolType.Ipx);
        }

        static readonly Lazy<Dictionary<string, ProtocolType>> s_protoByName = new Lazy<Dictionary<string, ProtocolType>>(() =>
        {
            var map = new Dictionary<string, ProtocolType>(19, StringComparer.OrdinalIgnoreCase);
            
            foreach (var value in EnumerateProtocolTypes())
            {
                map[value.name] = value.type;
            }

            return map;
        });

        static readonly Lazy<Dictionary<ProtocolType, string>> s_protoByNumber = new Lazy<Dictionary<ProtocolType, string>>(() =>
        {
            var map = new Dictionary<ProtocolType, string>(19);

            foreach (var value in EnumerateProtocolTypes())
            {
                map[value.type] = value.name;
            }

            return map;
        });

        /// <summary>
        /// Get protocol number associated with protocol name.
        /// </summary>
        /// <remarks>Numbers correspond to <see cref="System.Net.Sockets.ProtocolType"/>.</remarks>
        [return: CastToFalse]
        public static int getprotobyname(string name)
        {
            if (s_protoByName.Value.TryGetValue(name, out var type))
            {
                return (int)type;
            }
            else
            {
                return -1; // FALSE
            }
        }

        /// <summary>
        /// Get protocol number associated with protocol name.
        /// </summary>
        /// <remarks>Numbers correspond to <see cref="System.Net.Sockets.ProtocolType"/>.</remarks>
        [return: CastToFalse]
        public static string getprotobynumber(ProtocolType type)
        {
            if (s_protoByNumber.Value.TryGetValue(type, out var name))
            {
                return name;
            }
            else
            {
                return null; // FALSE
            }
        }

        #endregion
    }

    /// <summary>Functions working with DNS.</summary>
    [PhpExtension("standard")]
    public static class PhpDns
    {
        #region dns_check_record, checkdnsrr

        /// <summary>
        /// Not supported.
        /// </summary>
        public static bool checkdnsrr(string host, string type = "MX") => dns_check_record(host, type);

        /// <summary>
        /// Not supported.
        /// </summary>
        public static bool dns_check_record(string host, string type = "MX")
        {
            throw new NotImplementedException();
        }

        #endregion

        #region dns_get_record

        /// <summary>
        /// Not supported.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray dns_get_record(string host, DnsRecordType type = DnsRecordType.Any)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray dns_get_record(string host, DnsRecordType type, out PhpArray authNS, out PhpArray additional)
        {
            authNS = null;
            additional = null;
            throw new NotImplementedException();
        }

        #endregion

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

        #region ip2long, long2ip, inet_ntop, inet_pton

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

        /// <summary>
        /// Converts a human readable IP address to its packed in_addr representation.
        /// </summary>
        [return: CastToFalse]
        public static PhpString inet_pton(string address)
        {
            IPAddress addr;
            try
            {
                addr = IPAddress.Parse(address);
            }
            catch (FormatException)
            {
                return default(PhpString);
            }

            return new PhpString(addr.GetAddressBytes());
        }

        /// <summary>
        /// Converts a packed internet address to a human readable representation.
        /// </summary>
        /// 
        [return: CastToFalse]
        public static string inet_ntop(byte[] in_addr)
        {
            IPAddress addr;
            try
            {
                addr = new IPAddress(in_addr);
            }
            catch (FormatException)
            {
                return null;
            }

            return addr.ToString();
        }


        #endregion
    }
}
