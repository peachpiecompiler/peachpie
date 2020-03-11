using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Pchp.Core;
using Pchp.Core.Utilities;
using Pchp.Library.Streams;

namespace Peachpie.Library.Network
{
    [PhpExtension(ExtensionName)]
    public static class CURLConstants
    {
        /// <summary>
        /// Name of the cURL extension as it apears in PHP.
        /// </summary>
        internal const string ExtensionName = "curl";

        internal const string CurlResourceName = "curl";

        internal const string CurlMultiResourceName = "curl_multi";

        internal static Version FakeCurlVersion => new Version(7, 11, 2);

        #region Constants

        public const int CURLOPT_AUTOREFERER = 58;
        public const int CURLOPT_BINARYTRANSFER = 19914;
        public const int CURLOPT_BUFFERSIZE = 98;
        public const int CURLOPT_CAINFO = 10065;
        public const int CURLOPT_CAPATH = 10097;
        public const int CURLOPT_CONNECTTIMEOUT = 78;
        public const int CURLOPT_COOKIE = 10022;
        public const int CURLOPT_COOKIEFILE = 10031;
        public const int CURLOPT_COOKIEJAR = 10082;
        public const int CURLOPT_COOKIESESSION = 96;
        public const int CURLOPT_CRLF = 27;
        public const int CURLOPT_CUSTOMREQUEST = 10036;
        public const int CURLOPT_DNS_CACHE_TIMEOUT = 92;
        public const int CURLOPT_DNS_USE_GLOBAL_CACHE = 91;
        public const int CURLOPT_EGDSOCKET = 10077;
        public const int CURLOPT_ENCODING = 10102;
        public const int CURLOPT_FAILONERROR = 45;
        public const int CURLOPT_FILE = 10001;
        public const int CURLOPT_FILETIME = 69;
        public const int CURLOPT_FOLLOWLOCATION = 52;
        public const int CURLOPT_FORBID_REUSE = 75;
        public const int CURLOPT_FRESH_CONNECT = 74;
        public const int CURLOPT_FTPAPPEND = 50;
        public const int CURLOPT_FTPLISTONLY = 48;
        public const int CURLOPT_FTPPORT = 10017;
        public const int CURLOPT_FTP_USE_EPRT = 106;
        public const int CURLOPT_FTP_USE_EPSV = 85;
        public const int CURLOPT_HEADER = 42;
        public const int CURLOPT_HEADERFUNCTION = 20079;
        public const int CURLOPT_HTTP200ALIASES = 10104;
        public const int CURLOPT_HTTPGET = 80;
        public const int CURLOPT_HTTPHEADER = 10023;
        public const int CURLOPT_HTTPPROXYTUNNEL = 61;
        public const int CURLOPT_HTTP_VERSION = 84;
        public const int CURLOPT_INFILE = 10009;
        public const int CURLOPT_INFILESIZE = 14;
        public const int CURLOPT_INTERFACE = 10062;
        public const int CURLOPT_KRB4LEVEL = 10063;
        public const int CURLOPT_LOW_SPEED_LIMIT = 19;
        public const int CURLOPT_LOW_SPEED_TIME = 20;
        public const int CURLOPT_MAXCONNECTS = 71;
        public const int CURLOPT_MAXREDIRS = 68;
        public const int CURLOPT_NETRC = 51;
        public const int CURLOPT_NOBODY = 44;
        public const int CURLOPT_NOPROGRESS = 43;
        public const int CURLOPT_NOSIGNAL = 99;
        public const int CURLOPT_PORT = 3;
        public const int CURLOPT_POST = 47;
        public const int CURLOPT_POSTFIELDS = 10015;
        public const int CURLOPT_POSTQUOTE = 10039;
        public const int CURLOPT_PREQUOTE = 10093;
        public const int CURLOPT_PRIVATE = 10103;
        public const int CURLOPT_PROGRESSFUNCTION = 20056;
        public const int CURLOPT_PROXY = 10004;
        public const int CURLOPT_PROXYPORT = 59;
        public const int CURLOPT_PROXYTYPE = 101;
        public const int CURLOPT_PROXYUSERPWD = 10006;
        public const int CURLOPT_PUT = 54;
        public const int CURLOPT_QUOTE = 10028;
        public const int CURLOPT_RANDOM_FILE = 10076;
        public const int CURLOPT_RANGE = 10007;
        public const int CURLOPT_READDATA = 10009;
        public const int CURLOPT_READFUNCTION = 20012;
        public const int CURLOPT_REFERER = 10016;
        public const int CURLOPT_RESUME_FROM = 21;
        public const int CURLOPT_RETURNTRANSFER = 19913;
        public const int CURLOPT_SHARE = 10100;
        public const int CURLOPT_SSLCERT = 10025;
        public const int CURLOPT_SSLCERTPASSWD = 10026;
        public const int CURLOPT_SSLCERTTYPE = 10086;
        public const int CURLOPT_SSLENGINE = 10089;
        public const int CURLOPT_SSLENGINE_DEFAULT = 90;
        public const int CURLOPT_SSLKEY = 10087;
        public const int CURLOPT_SSLKEYPASSWD = 10026;
        public const int CURLOPT_SSLKEYTYPE = 10088;
        public const int CURLOPT_SSLVERSION = 32;
        public const int CURLOPT_SSL_CIPHER_LIST = 10083;
        public const int CURLOPT_SSL_VERIFYHOST = 81;
        public const int CURLOPT_SSL_VERIFYPEER = 64;
        public const int CURLOPT_STDERR = 10037;
        public const int CURLOPT_TELNETOPTIONS = 10070;
        public const int CURLOPT_TIMECONDITION = 33;
        public const int CURLOPT_TIMEOUT = 13;
        public const int CURLOPT_TIMEVALUE = 34;
        public const int CURLOPT_TRANSFERTEXT = 53;
        public const int CURLOPT_UNRESTRICTED_AUTH = 105;
        public const int CURLOPT_UPLOAD = 46;
        public const int CURLOPT_URL = 10002;
        public const int CURLOPT_USERAGENT = 10018;
        public const int CURLOPT_USERPWD = 10005;
        public const int CURLOPT_VERBOSE = 41;
        public const int CURLOPT_WRITEFUNCTION = 20011;
        public const int CURLOPT_WRITEHEADER = 10029;
        public const int CURLE_ABORTED_BY_CALLBACK = (int)CurlErrors.CURLE_ABORTED_BY_CALLBACK;
        public const int CURLE_BAD_CALLING_ORDER = (int)CurlErrors.CURLE_BAD_CALLING_ORDER;
        public const int CURLE_BAD_CONTENT_ENCODING = (int)CurlErrors.CURLE_BAD_CONTENT_ENCODING;
        public const int CURLE_BAD_DOWNLOAD_RESUME = (int)CurlErrors.CURLE_BAD_DOWNLOAD_RESUME;
        public const int CURLE_BAD_FUNCTION_ARGUMENT = (int)CurlErrors.CURLE_BAD_FUNCTION_ARGUMENT;
        public const int CURLE_BAD_PASSWORD_ENTERED = (int)CurlErrors.CURLE_BAD_PASSWORD_ENTERED;
        public const int CURLE_COULDNT_CONNECT = (int)CurlErrors.CURLE_COULDNT_CONNECT;
        public const int CURLE_COULDNT_RESOLVE_HOST = (int)CurlErrors.CURLE_COULDNT_RESOLVE_HOST;
        public const int CURLE_COULDNT_RESOLVE_PROXY = (int)CurlErrors.CURLE_COULDNT_RESOLVE_PROXY;
        public const int CURLE_FAILED_INIT = (int)CurlErrors.CURLE_FAILED_INIT;
        public const int CURLE_FILE_COULDNT_READ_FILE = (int)CurlErrors.CURLE_FILE_COULDNT_READ_FILE;
        public const int CURLE_FTP_ACCESS_DENIED = (int)CurlErrors.CURLE_FTP_ACCESS_DENIED;
        public const int CURLE_FTP_BAD_DOWNLOAD_RESUME = (int)CurlErrors.CURLE_FTP_BAD_DOWNLOAD_RESUME;
        public const int CURLE_FTP_CANT_GET_HOST = (int)CurlErrors.CURLE_FTP_CANT_GET_HOST;
        public const int CURLE_FTP_CANT_RECONNECT = (int)CurlErrors.CURLE_FTP_CANT_RECONNECT;
        public const int CURLE_FTP_COULDNT_GET_SIZE = (int)CurlErrors.CURLE_FTP_COULDNT_GET_SIZE;
        public const int CURLE_FTP_COULDNT_RETR_FILE = (int)CurlErrors.CURLE_FTP_COULDNT_RETR_FILE;
        public const int CURLE_FTP_COULDNT_SET_ASCII = (int)CurlErrors.CURLE_FTP_COULDNT_SET_ASCII;
        public const int CURLE_FTP_COULDNT_SET_BINARY = (int)CurlErrors.CURLE_FTP_COULDNT_SET_BINARY;
        public const int CURLE_FTP_COULDNT_STOR_FILE = (int)CurlErrors.CURLE_FTP_COULDNT_STOR_FILE;
        public const int CURLE_FTP_COULDNT_USE_REST = (int)CurlErrors.CURLE_FTP_COULDNT_USE_REST;
        public const int CURLE_FTP_PARTIAL_FILE = (int)CurlErrors.CURLE_FTP_PARTIAL_FILE;
        public const int CURLE_FTP_PORT_FAILED = (int)CurlErrors.CURLE_FTP_PORT_FAILED;
        public const int CURLE_FTP_QUOTE_ERROR = (int)CurlErrors.CURLE_FTP_QUOTE_ERROR;
        public const int CURLE_FTP_USER_PASSWORD_INCORRECT = (int)CurlErrors.CURLE_FTP_USER_PASSWORD_INCORRECT;
        public const int CURLE_FTP_WEIRD_227_FORMAT = (int)CurlErrors.CURLE_FTP_WEIRD_227_FORMAT;
        public const int CURLE_FTP_WEIRD_PASS_REPLY = (int)CurlErrors.CURLE_FTP_WEIRD_PASS_REPLY;
        public const int CURLE_FTP_WEIRD_PASV_REPLY = (int)CurlErrors.CURLE_FTP_WEIRD_PASV_REPLY;
        public const int CURLE_FTP_WEIRD_SERVER_REPLY = (int)CurlErrors.CURLE_FTP_WEIRD_SERVER_REPLY;
        public const int CURLE_FTP_WEIRD_USER_REPLY = (int)CurlErrors.CURLE_FTP_WEIRD_USER_REPLY;
        public const int CURLE_FTP_WRITE_ERROR = (int)CurlErrors.CURLE_FTP_WRITE_ERROR;
        public const int CURLE_FUNCTION_NOT_FOUND = (int)CurlErrors.CURLE_FUNCTION_NOT_FOUND;
        public const int CURLE_GOT_NOTHING = (int)CurlErrors.CURLE_GOT_NOTHING;
        public const int CURLE_HTTP_NOT_FOUND = (int)CurlErrors.CURLE_HTTP_NOT_FOUND;
        public const int CURLE_HTTP_PORT_FAILED = (int)CurlErrors.CURLE_HTTP_PORT_FAILED;
        public const int CURLE_HTTP_POST_ERROR = (int)CurlErrors.CURLE_HTTP_POST_ERROR;
        public const int CURLE_HTTP_RANGE_ERROR = (int)CurlErrors.CURLE_HTTP_RANGE_ERROR;
        public const int CURLE_HTTP_RETURNED_ERROR = (int)CurlErrors.CURLE_HTTP_RETURNED_ERROR;
        public const int CURLE_LDAP_CANNOT_BIND = (int)CurlErrors.CURLE_LDAP_CANNOT_BIND;
        public const int CURLE_LDAP_SEARCH_FAILED = (int)CurlErrors.CURLE_LDAP_SEARCH_FAILED;
        public const int CURLE_LIBRARY_NOT_FOUND = (int)CurlErrors.CURLE_LIBRARY_NOT_FOUND;
        public const int CURLE_MALFORMAT_USER = (int)CurlErrors.CURLE_MALFORMAT_USER;
        public const int CURLE_OBSOLETE = (int)CurlErrors.CURLE_OBSOLETE;
        public const int CURLE_OK = (int)CurlErrors.CURLE_OK;
        public const int CURLE_OPERATION_TIMEDOUT = (int)CurlErrors.CURLE_OPERATION_TIMEDOUT;
        public const int CURLE_OPERATION_TIMEOUTED = (int)CurlErrors.CURLE_OPERATION_TIMEOUTED;
        public const int CURLE_OUT_OF_MEMORY = (int)CurlErrors.CURLE_OUT_OF_MEMORY;
        public const int CURLE_PARTIAL_FILE = (int)CurlErrors.CURLE_PARTIAL_FILE;
        public const int CURLE_READ_ERROR = (int)CurlErrors.CURLE_READ_ERROR;
        public const int CURLE_RECV_ERROR = (int)CurlErrors.CURLE_RECV_ERROR;
        public const int CURLE_SEND_ERROR = (int)CurlErrors.CURLE_SEND_ERROR;
        public const int CURLE_SHARE_IN_USE = (int)CurlErrors.CURLE_SHARE_IN_USE;
        public const int CURLE_SSL_CACERT = (int)CurlErrors.CURLE_SSL_CACERT;
        public const int CURLE_SSL_CERTPROBLEM = (int)CurlErrors.CURLE_SSL_CERTPROBLEM;
        public const int CURLE_SSL_CIPHER = (int)CurlErrors.CURLE_SSL_CIPHER;
        public const int CURLE_SSL_CONNECT_ERROR = (int)CurlErrors.CURLE_SSL_CONNECT_ERROR;
        public const int CURLE_SSL_ENGINE_NOTFOUND = (int)CurlErrors.CURLE_SSL_ENGINE_NOTFOUND;
        public const int CURLE_SSL_ENGINE_SETFAILED = (int)CurlErrors.CURLE_SSL_ENGINE_SETFAILED;
        public const int CURLE_SSL_PEER_CERTIFICATE = (int)CurlErrors.CURLE_SSL_PEER_CERTIFICATE;
        public const int CURLE_SSL_PINNEDPUBKEYNOTMATCH = (int)CurlErrors.CURLE_SSL_PINNEDPUBKEYNOTMATCH;
        public const int CURLE_TELNET_OPTION_SYNTAX = (int)CurlErrors.CURLE_TELNET_OPTION_SYNTAX;
        public const int CURLE_TOO_MANY_REDIRECTS = (int)CurlErrors.CURLE_TOO_MANY_REDIRECTS;
        public const int CURLE_UNKNOWN_TELNET_OPTION = (int)CurlErrors.CURLE_UNKNOWN_TELNET_OPTION;
        public const int CURLE_UNSUPPORTED_PROTOCOL = (int)CurlErrors.CURLE_UNSUPPORTED_PROTOCOL;
        public const int CURLE_URL_MALFORMAT = (int)CurlErrors.CURLE_URL_MALFORMAT;
        public const int CURLE_URL_MALFORMAT_USER = (int)CurlErrors.CURLE_URL_MALFORMAT_USER;
        public const int CURLE_WRITE_ERROR = (int)CurlErrors.CURLE_WRITE_ERROR;
        public const int CURLE_FILESIZE_EXCEEDED = (int)CurlErrors.CURLE_FILESIZE_EXCEEDED;
        public const int CURLE_LDAP_INVALID_URL = (int)CurlErrors.CURLE_LDAP_INVALID_URL;
        public const int CURLE_FTP_SSL_FAILED = (int)CurlErrors.CURLE_FTP_SSL_FAILED;
        public const int CURLE_SSL_CACERT_BADFILE = (int)CurlErrors.CURLE_SSL_CACERT_BADFILE;
        public const int CURLE_SSH = (int)CurlErrors.CURLE_SSH;
        public const int CURLINFO_CONNECT_TIME = 3145733;
        /// <summary>
        /// Content length of download, read from Content-Length: field.
        /// </summary>
        public const int CURLINFO_CONTENT_LENGTH_DOWNLOAD = 3145743;
        public const int CURLINFO_CONTENT_LENGTH_UPLOAD = 3145744;
        public const int CURLINFO_CONTENT_TYPE = 1048594;
        public const int CURLINFO_EFFECTIVE_URL = 1048577;
        public const int CURLINFO_FILETIME = 2097166;
        public const int CURLINFO_HEADER_OUT = 2;
        public const int CURLINFO_HEADER_SIZE = 2097163;
        public const int CURLINFO_HTTP_CODE = 2097154;
        public const int CURLINFO_LASTONE = 49;
        public const int CURLINFO_NAMELOOKUP_TIME = 3145732;
        public const int CURLINFO_PRETRANSFER_TIME = 3145734;
        public const int CURLINFO_PRIVATE = 1048597;
        public const int CURLINFO_REDIRECT_COUNT = 2097172;
        public const int CURLINFO_REDIRECT_TIME = 3145747;

        /// <summary>
        /// Total size of issued requests.
        /// </summary>
        public const int CURLINFO_REQUEST_SIZE = 2097164;
        public const int CURLINFO_SIZE_DOWNLOAD = 3145736;
        public const int CURLINFO_SIZE_UPLOAD = 3145735;
        public const int CURLINFO_SPEED_DOWNLOAD = 3145737;
        public const int CURLINFO_SPEED_UPLOAD = 3145738;
        public const int CURLINFO_SSL_VERIFYRESULT = 2097165;
        public const int CURLINFO_STARTTRANSFER_TIME = 3145745;
        /// <summary>Total transaction time in seconds for last transfer.</summary>
        public const int CURLINFO_TOTAL_TIME = 3145731;
        public const int CURLMSG_DONE = 1;
        public const int CURLVERSION_NOW = 4;
        public const int CURLM_BAD_EASY_HANDLE = (int)CurlMultiErrors.CURLM_BAD_EASY_HANDLE;
        public const int CURLM_BAD_HANDLE = (int)CurlMultiErrors.CURLM_BAD_HANDLE;
        public const int CURLM_CALL_MULTI_PERFORM = (int)CurlMultiErrors.CURLM_CALL_MULTI_PERFORM;
        public const int CURLM_INTERNAL_ERROR = (int)CurlMultiErrors.CURLM_INTERNAL_ERROR;
        public const int CURLM_OK = (int)CurlMultiErrors.CURLM_OK;
        public const int CURLM_OUT_OF_MEMORY = (int)CurlMultiErrors.CURLM_OUT_OF_MEMORY;
        public const int CURLM_ADDED_ALREADY = (int)CurlMultiErrors.CURLM_ADDED_ALREADY;
        public const int CURLPROXY_HTTP = 0;
        public const int CURLPROXY_SOCKS4 = 4;
        public const int CURLPROXY_SOCKS5 = 5;
        public const int CURLSHOPT_NONE = 0;
        public const int CURLSHOPT_SHARE = 1;
        public const int CURLSHOPT_UNSHARE = 2;
        public const int CURL_HTTP_VERSION_1_0 = 1;
        public const int CURL_HTTP_VERSION_1_1 = 2;
        public const int CURL_HTTP_VERSION_NONE = 0;
        public const int CURL_LOCK_DATA_COOKIE = 2;
        public const int CURL_LOCK_DATA_DNS = 3;
        public const int CURL_LOCK_DATA_SSL_SESSION = 4;
        public const int CURL_NETRC_IGNORED = 0;
        public const int CURL_NETRC_OPTIONAL = 1;
        public const int CURL_NETRC_REQUIRED = 2;
        public const int CURL_SSLVERSION_DEFAULT = 0;
        public const int CURL_SSLVERSION_SSLv2 = 2;
        public const int CURL_SSLVERSION_SSLv3 = 3;
        public const int CURL_SSLVERSION_TLSv1 = 1;
        public const int CURL_TIMECOND_IFMODSINCE = 1;
        public const int CURL_TIMECOND_IFUNMODSINCE = 2;
        public const int CURL_TIMECOND_LASTMOD = 3;
        public const int CURL_TIMECOND_NONE = 0;
        public const int CURL_VERSION_IPV6 = 1;
        public const int CURL_VERSION_KERBEROS4 = 2;
        public const int CURL_VERSION_LIBZ = 8;
        public const int CURL_VERSION_SSL = 4;
        public const int CURLOPT_HTTPAUTH = 107;
        public const long CURLAUTH_ANY = 4294967279;
        public const long CURLAUTH_ANYSAFE = 4294967278;
        public const int CURLAUTH_BASIC = 1;
        public const int CURLAUTH_DIGEST = 2;
        public const int CURLAUTH_GSSNEGOTIATE = 4;
        public const int CURLAUTH_NONE = 0;
        public const int CURLAUTH_NTLM = 8;
        public const int CURLINFO_HTTP_CONNECTCODE = 2097174;
        public const int CURLOPT_FTP_CREATE_MISSING_DIRS = 110;
        public const int CURLOPT_PROXYAUTH = 111;
        public const int CURLINFO_HTTPAUTH_AVAIL = 2097175;
        public const int CURLINFO_RESPONSE_CODE = 2097154;
        public const int CURLINFO_PROXYAUTH_AVAIL = 2097176;
        public const int CURLOPT_FTP_RESPONSE_TIMEOUT = 112;
        public const int CURLOPT_IPRESOLVE = 113;
        public const int CURLOPT_MAXFILESIZE = 114;
        public const int CURL_IPRESOLVE_V4 = 1;
        public const int CURL_IPRESOLVE_V6 = 2;
        public const int CURL_IPRESOLVE_WHATEVER = 0;
        public const int CURLFTPSSL_ALL = 3;
        public const int CURLFTPSSL_CONTROL = 2;
        public const int CURLFTPSSL_NONE = 0;
        public const int CURLFTPSSL_TRY = 1;
        public const int CURLOPT_FTP_SSL = 119;
        public const int CURLOPT_NETRC_FILE = 10118;
        public const int CURLFTPAUTH_DEFAULT = 0;
        public const int CURLFTPAUTH_SSL = 1;
        public const int CURLFTPAUTH_TLS = 2;
        public const int CURLOPT_FTPSSLAUTH = 129;
        public const int CURLOPT_FTP_ACCOUNT = 10134;
        /// <summary>
        /// TRUE to disable TCP's Nagle algorithm, which tries to minimize the number of small packets on the network.
        /// </summary>
        public const int CURLOPT_TCP_NODELAY = 121;
        public const int CURLINFO_OS_ERRNO = 2097177;
        public const int CURLINFO_NUM_CONNECTS = 2097178;
        public const int CURLINFO_SSL_ENGINES = 4194331;
        public const int CURLINFO_COOKIELIST = 4194332;
        public const int CURLOPT_COOKIELIST = 10135;
        public const int CURLOPT_IGNORE_CONTENT_LENGTH = 136;
        public const int CURLOPT_FTP_SKIP_PASV_IP = 137;
        public const int CURLOPT_FTP_FILEMETHOD = 138;
        public const int CURLOPT_CONNECT_ONLY = 141;
        public const int CURLOPT_LOCALPORT = 139;
        public const int CURLOPT_LOCALPORTRANGE = 140;
        public const int CURLFTPMETHOD_MULTICWD = 1;
        public const int CURLFTPMETHOD_NOCWD = 2;
        public const int CURLFTPMETHOD_SINGLECWD = 3;
        public const int CURLINFO_FTP_ENTRY_PATH = 1048606;
        public const int CURLOPT_FTP_ALTERNATIVE_TO_USER = 10147;
        public const int CURLOPT_MAX_RECV_SPEED_LARGE = 30146;
        public const int CURLOPT_MAX_SEND_SPEED_LARGE = 30145;
        public const int CURLOPT_SSL_SESSIONID_CACHE = 150;
        public const int CURLMOPT_PIPELINING = 3;
        public const int CURLOPT_FTP_SSL_CCC = 154;
        public const int CURLOPT_SSH_AUTH_TYPES = 151;
        public const int CURLOPT_SSH_PRIVATE_KEYFILE = 10153;
        public const int CURLOPT_SSH_PUBLIC_KEYFILE = 10152;
        public const int CURLFTPSSL_CCC_ACTIVE = 2;
        public const int CURLFTPSSL_CCC_NONE = 0;
        public const int CURLFTPSSL_CCC_PASSIVE = 1;
        public const int CURLOPT_CONNECTTIMEOUT_MS = 156;
        public const int CURLOPT_HTTP_CONTENT_DECODING = 158;
        public const int CURLOPT_HTTP_TRANSFER_DECODING = 157;
        public const int CURLOPT_TIMEOUT_MS = 155;
        public const int CURLMOPT_MAXCONNECTS = 6;
        public const int CURLOPT_KRBLEVEL = 10063;
        public const int CURLOPT_NEW_DIRECTORY_PERMS = 160;
        public const int CURLOPT_NEW_FILE_PERMS = 159;
        public const int CURLOPT_APPEND = 50;
        public const int CURLOPT_DIRLISTONLY = 48;
        public const int CURLOPT_USE_SSL = 119;
        public const int CURLUSESSL_ALL = 3;
        public const int CURLUSESSL_CONTROL = 2;
        public const int CURLUSESSL_NONE = 0;
        public const int CURLUSESSL_TRY = 1;
        public const int CURLOPT_SSH_HOST_PUBLIC_KEY_MD5 = 10162;
        public const int CURLOPT_PROXY_TRANSFER_MODE = 166;
        public const int CURLPAUSE_ALL = 5;
        public const int CURLPAUSE_CONT = 0;
        public const int CURLPAUSE_RECV = 1;
        public const int CURLPAUSE_RECV_CONT = 0;
        public const int CURLPAUSE_SEND = 4;
        public const int CURLPAUSE_SEND_CONT = 0;
        public const int CURL_READFUNC_PAUSE = 268435457;
        public const int CURL_WRITEFUNC_PAUSE = 268435457;
        public const int CURLPROXY_SOCKS4A = 6;
        public const int CURLPROXY_SOCKS5_HOSTNAME = 7;
        public const int CURLINFO_REDIRECT_URL = 1048607;
        public const int CURLINFO_APPCONNECT_TIME = 3145761;
        public const int CURLINFO_PRIMARY_IP = 1048608;
        public const int CURLOPT_ADDRESS_SCOPE = 171;
        public const int CURLOPT_CRLFILE = 10169;
        public const int CURLOPT_ISSUERCERT = 10170;
        public const int CURLOPT_KEYPASSWD = 10026;
        public const int CURLSSH_AUTH_ANY = -1;
        public const int CURLSSH_AUTH_DEFAULT = -1;
        public const int CURLSSH_AUTH_HOST = 4;
        public const int CURLSSH_AUTH_KEYBOARD = 8;
        public const int CURLSSH_AUTH_NONE = 0;
        public const int CURLSSH_AUTH_PASSWORD = 2;
        public const int CURLSSH_AUTH_PUBLICKEY = 1;
        public const int CURLINFO_CERTINFO = 4194338;
        public const int CURLOPT_CERTINFO = 172;
        public const int CURLOPT_PASSWORD = 10174;
        public const int CURLOPT_POSTREDIR = 161;
        public const int CURLOPT_PROXYPASSWORD = 10176;
        public const int CURLOPT_PROXYUSERNAME = 10175;
        public const int CURLOPT_USERNAME = 10173;
        public const int CURL_REDIR_POST_301 = 1;
        public const int CURL_REDIR_POST_302 = 2;
        public const int CURL_REDIR_POST_ALL = 7;
        public const int CURLAUTH_DIGEST_IE = 16;
        public const int CURLINFO_CONDITION_UNMET = 2097187;
        public const int CURLOPT_NOPROXY = 10177;
        public const int CURLOPT_PROTOCOLS = 181;
        public const int CURLOPT_REDIR_PROTOCOLS = 182;
        public const int CURLOPT_SOCKS5_GSSAPI_NEC = 180;
        public const int CURLOPT_SOCKS5_GSSAPI_SERVICE = 10179;
        public const int CURLOPT_TFTP_BLKSIZE = 178;
        public const int CURLPROTO_ALL = -1;
        public const int CURLPROTO_DICT = 512;
        public const int CURLPROTO_FILE = 1024;
        public const int CURLPROTO_FTP = 4;
        public const int CURLPROTO_FTPS = 8;
        public const int CURLPROTO_HTTP = 1;
        public const int CURLPROTO_HTTPS = 2;
        public const int CURLPROTO_LDAP = 128;
        public const int CURLPROTO_LDAPS = 256;
        public const int CURLPROTO_SCP = 16;
        public const int CURLPROTO_SFTP = 32;
        public const int CURLPROTO_TELNET = 64;
        public const int CURLPROTO_TFTP = 2048;
        public const int CURLPROTO_IMAP = 4096;
        public const int CURLPROTO_IMAPS = 8192;
        public const int CURLPROTO_POP3 = 16384;
        public const int CURLPROTO_POP3S = 32768;
        public const int CURLPROTO_RTSP = 262144;
        public const int CURLPROTO_SMTP = 65536;
        public const int CURLPROTO_SMTPS = 131072;
        public const int CURLPROTO_RTMP = 524288;
        public const int CURLPROTO_RTMPE = 2097152;
        public const int CURLPROTO_RTMPS = 8388608;
        public const int CURLPROTO_RTMPT = 1048576;
        public const int CURLPROTO_RTMPTE = 4194304;
        public const int CURLPROTO_RTMPTS = 16777216;
        public const int CURLPROTO_GOPHER = 33554432;
        public const int CURLPROTO_SMB = 67108864;
        public const int CURLPROTO_SMBS = 134217728;
        public const int CURLPROXY_HTTP_1_0 = 1;
        public const int CURLFTP_CREATE_DIR = 1;
        public const int CURLFTP_CREATE_DIR_NONE = 0;
        public const int CURLFTP_CREATE_DIR_RETRY = 2;
        public const int CURLOPT_SSH_KNOWNHOSTS = 10183;
        public const int CURLINFO_RTSP_CLIENT_CSEQ = 2097189;
        public const int CURLINFO_RTSP_CSEQ_RECV = 2097191;
        public const int CURLINFO_RTSP_SERVER_CSEQ = 2097190;
        public const int CURLINFO_RTSP_SESSION_ID = 1048612;
        public const int CURLOPT_FTP_USE_PRET = 188;
        public const int CURLOPT_MAIL_FROM = 10186;
        public const int CURLOPT_MAIL_RCPT = 10187;
        public const int CURLOPT_RTSP_CLIENT_CSEQ = 193;
        public const int CURLOPT_RTSP_REQUEST = 189;
        public const int CURLOPT_RTSP_SERVER_CSEQ = 194;
        public const int CURLOPT_RTSP_SESSION_ID = 10190;
        public const int CURLOPT_RTSP_STREAM_URI = 10191;
        public const int CURLOPT_RTSP_TRANSPORT = 10192;
        public const int CURL_RTSPREQ_ANNOUNCE = 3;
        public const int CURL_RTSPREQ_DESCRIBE = 2;
        public const int CURL_RTSPREQ_GET_PARAMETER = 8;
        public const int CURL_RTSPREQ_OPTIONS = 1;
        public const int CURL_RTSPREQ_PAUSE = 6;
        public const int CURL_RTSPREQ_PLAY = 5;
        public const int CURL_RTSPREQ_RECEIVE = 11;
        public const int CURL_RTSPREQ_RECORD = 10;
        public const int CURL_RTSPREQ_SET_PARAMETER = 9;
        public const int CURL_RTSPREQ_SETUP = 4;
        public const int CURL_RTSPREQ_TEARDOWN = 7;
        public const int CURLINFO_LOCAL_IP = 1048617;
        public const int CURLINFO_LOCAL_PORT = 2097194;
        public const int CURLINFO_PRIMARY_PORT = 2097192;
        public const int CURLOPT_FNMATCH_FUNCTION = 20200;
        public const int CURLOPT_WILDCARDMATCH = 197;
        public const int CURL_FNMATCHFUNC_FAIL = 2;
        public const int CURL_FNMATCHFUNC_MATCH = 0;
        public const int CURL_FNMATCHFUNC_NOMATCH = 1;
        public const long CURLAUTH_ONLY = 2147483648;
        public const int CURLOPT_RESOLVE = 10203;
        public const int CURLOPT_TLSAUTH_PASSWORD = 10205;
        public const int CURLOPT_TLSAUTH_TYPE = 10206;
        public const int CURLOPT_TLSAUTH_USERNAME = 10204;
        public const int CURL_TLSAUTH_SRP = 1;
        public const int CURLOPT_ACCEPT_ENCODING = 10102;
        public const int CURLOPT_TRANSFER_ENCODING = 207;
        public const int CURLAUTH_NTLM_WB = 32;
        public const int CURLGSSAPI_DELEGATION_FLAG = 2;
        public const int CURLGSSAPI_DELEGATION_POLICY_FLAG = 1;
        public const int CURLOPT_GSSAPI_DELEGATION = 210;
        public const int CURLOPT_ACCEPTTIMEOUT_MS = 212;
        public const int CURLOPT_DNS_SERVERS = 10211;
        public const int CURLOPT_MAIL_AUTH = 10217;
        public const int CURLOPT_SSL_OPTIONS = 216;
        public const int CURLOPT_TCP_KEEPALIVE = 213;
        public const int CURLOPT_TCP_KEEPIDLE = 214;
        public const int CURLOPT_TCP_KEEPINTVL = 215;
        public const int CURLSSLOPT_ALLOW_BEAST = 1;
        public const int CURL_REDIR_POST_303 = 4;
        public const int CURLSSH_AUTH_AGENT = 16;
        public const int CURLMOPT_CHUNK_LENGTH_PENALTY_SIZE = 30010;
        public const int CURLMOPT_CONTENT_LENGTH_PENALTY_SIZE = 30009;
        public const int CURLMOPT_MAX_HOST_CONNECTIONS = 7;
        public const int CURLMOPT_MAX_PIPELINE_LENGTH = 8;
        public const int CURLMOPT_MAX_TOTAL_CONNECTIONS = 13;
        public const int CURLOPT_SASL_IR = 218;
        public const int CURLOPT_DNS_INTERFACE = 10221;
        public const int CURLOPT_DNS_LOCAL_IP4 = 10222;
        public const int CURLOPT_DNS_LOCAL_IP6 = 10223;
        public const int CURLOPT_XOAUTH2_BEARER = 10220;
        public const int CURL_HTTP_VERSION_2_0 = 3;
        public const int CURL_VERSION_HTTP2 = 65536;
        public const int CURLOPT_LOGIN_OPTIONS = 10224;
        public const int CURL_SSLVERSION_TLSv1_0 = 4;
        public const int CURL_SSLVERSION_TLSv1_1 = 5;
        public const int CURL_SSLVERSION_TLSv1_2 = 6;
        public const int CURLOPT_EXPECT_100_TIMEOUT_MS = 227;
        public const int CURLOPT_SSL_ENABLE_ALPN = 226;
        public const int CURLOPT_SSL_ENABLE_NPN = 225;
        public const int CURLHEADER_SEPARATE = 1;
        public const int CURLHEADER_UNIFIED = 0;
        public const int CURLOPT_HEADEROPT = 229;
        public const int CURLOPT_PROXYHEADER = 10228;
        public const int CURLAUTH_NEGOTIATE = 4;
        public const int CURLOPT_PINNEDPUBLICKEY = 10230;
        public const int CURLOPT_UNIX_SOCKET_PATH = 10231;
        public const int CURLOPT_SSL_VERIFYSTATUS = 232;
        public const int CURLOPT_PATH_AS_IS = 234;
        public const int CURLOPT_SSL_FALSESTART = 233;
        public const int CURL_HTTP_VERSION_2 = 3;
        public const int CURLOPT_PIPEWAIT = 237;
        public const int CURLOPT_PROXY_SERVICE_NAME = 10235;
        public const int CURLOPT_SERVICE_NAME = 10236;
        public const int CURLPIPE_NOTHING = 0;
        public const int CURLPIPE_HTTP1 = 1;
        public const int CURLPIPE_MULTIPLEX = 2;
        public const int CURLSSLOPT_NO_REVOKE = 2;
        public const int CURLOPT_DEFAULT_PROTOCOL = 10238;
        public const int CURLOPT_STREAM_WEIGHT = 239;
        public const int CURLMOPT_PUSHFUNCTION = 20014;
        public const int CURL_PUSH_OK = 0;
        public const int CURL_PUSH_DENY = 1;
        public const int CURL_HTTP_VERSION_2TLS = 4;
        public const int CURLOPT_TFTP_NO_OPTIONS = 242;
        public const int CURL_HTTP_VERSION_2_PRIOR_KNOWLEDGE = 5;
        public const int CURLOPT_CONNECT_TO = 10243;
        public const int CURLOPT_TCP_FASTOPEN = 244;

        /// <summary>
        /// If set to <c>true</c>, `@` is not allowed in for uploading files with <see cref="CURLOPT_POSTFIELDS"/>.
        /// Since PHP 7, this option is removed and <see cref="CURLFile"/> must be used to upload files.
        /// </summary>
        public const int CURLOPT_SAFE_UPLOAD = -1;

        #endregion

        #region Helpers

        static bool TryProcessMethodFromStream(PhpValue value, ProcessMethod @default, ref ProcessMethod processing, bool readable = false)
        {
            if (Operators.IsSet(value))
            {
                var stream = TryProcessMethodFromStream(value, readable);
                if (stream != null)
                {
                    processing = new ProcessMethod(stream);
                }
                else
                {
                    return false; // failure
                }
            }
            else
            {
                processing = @default;
            }

            return true;
        }

        static PhpStream TryProcessMethodFromStream(PhpValue value, bool readable = false)
        {
            if (Operators.IsSet(value))
            {
                var stream = value.AsObject() as PhpStream;
                if (stream != null && (readable ? stream.CanRead : stream.CanWrite))
                {
                    return stream;
                }
                else
                {
                    return null; // failure
                }
            }

            return null;
        }

        static bool TryProcessMethodFromCallable(PhpValue value, ProcessMethod @default, ref ProcessMethod processing, RuntimeTypeHandle callerCtx)
        {
            if (Operators.IsSet(value))
            {
                var callable = value.AsCallable(callerCtx);
                if (callable != null)
                {
                    processing = new ProcessMethod(callable);
                }
                else
                {
                    return false; // failure
                }
            }
            else
            {
                processing = @default;
            }

            return true;
        }

        internal static bool TrySetOption(this CURLResource ch, long option, PhpValue value, RuntimeTypeHandle callerCtx)
        {
            switch (option)
            {
                case CURLOPT_URL: return (ch.Url = value.AsString()) != null;
                case CURLOPT_DEFAULT_PROTOCOL: return (ch.DefaultSheme = value.AsString()) != null;
                case CURLOPT_HTTPGET: if (value.ToBoolean()) { ch.Method = WebRequestMethods.Http.Get; } break;
                case CURLOPT_POST: if (value.ToBoolean()) { ch.Method = WebRequestMethods.Http.Post; } break;
                case CURLOPT_PUT: if (value.ToBoolean()) { ch.Method = WebRequestMethods.Http.Put; } break;
                case CURLOPT_NOBODY: if (value.ToBoolean()) { ch.Method = WebRequestMethods.Http.Head; } break;
                case CURLOPT_CUSTOMREQUEST: return (ch.Method = value.AsString()) != null;
                case CURLOPT_POSTFIELDS: ch.PostFields = value.GetValue().DeepCopy(); break;
                case CURLOPT_FOLLOWLOCATION: ch.FollowLocation = value.ToBoolean(); break;
                case CURLOPT_MAXREDIRS: ch.MaxRedirects = (int)value.ToLong(); break;
                case CURLOPT_REFERER: return SetOption<CurlOption_Referer, string>(ch, value.AsString());
                case CURLOPT_RETURNTRANSFER:
                    ch.ProcessingResponse = value.ToBoolean()
                        ? ProcessMethod.Return
                        : ProcessMethod.StdOut;
                    break;
                case CURLOPT_HEADER:
                    ch.ProcessingHeaders = value.ToBoolean()
                        ? ProcessMethod.StdOut // NOTE: if ProcessingResponse is RETURN, RETURN headers as well
                        : ProcessMethod.Ignore;
                    break;
                case CURLOPT_HTTPHEADER: return SetOption<CurlOption_Headers, PhpArray>(ch, value.ToArray().DeepCopy());
                case CURLOPT_ENCODING: ch.SetOption(new CurlOption_AcceptEncoding { OptionValue = value.ToStringOrNull() }); return true;
                case CURLOPT_COOKIE: return (ch.CookieHeader = value.AsString()) != null;
                case CURLOPT_COOKIEFILE:
                    ch.CookieContainer ??= new CookieContainer();   // enable the cookie container

                    if (ch.TryGetOption<CurlOption_CookieFile>(out var cookiefileoption) == false)
                    {
                        ch.SetOption(cookiefileoption = new CurlOption_CookieFile());
                    }

                    // add the file name to the list of file names:
                    cookiefileoption.OptionValue.Add(value.ToStringOrNull());

                    break; // always return true
                case CURLOPT_COOKIEJAR:
                    ch.CookieContainer ??= new CookieContainer();   // enable the cookie container
                    return SetOption<CurlOption_CookieJar, string>(ch, value.ToStringOrNull().EmptyToNull());

                case CURLOPT_FILE: return TryProcessMethodFromStream(value, ProcessMethod.StdOut, ref ch.ProcessingResponse);
                case CURLOPT_INFILE: return TryProcessMethodFromStream(value, ProcessMethod.Ignore, ref ch.ProcessingRequest, readable: true);
                case CURLOPT_WRITEHEADER: return TryProcessMethodFromStream(value, ProcessMethod.Ignore, ref ch.ProcessingHeaders);
                //case CURLOPT_STDERR: return TryProcessMethodFromStream(value, ProcessMethod.Ignore, ref ch.ProcessingErr);

                case CURLOPT_HEADERFUNCTION: return TryProcessMethodFromCallable(value, ProcessMethod.Ignore, ref ch.ProcessingHeaders, callerCtx);
                case CURLOPT_WRITEFUNCTION: return TryProcessMethodFromCallable(value, ProcessMethod.StdOut, ref ch.ProcessingResponse, callerCtx);
                //case CURLOPT_READFUNCTION:
                //case CURLOPT_PROGRESSFUNCTION:

                case CURLOPT_USERAGENT: return SetOption<CurlOption_UserAgent, string>(ch, value.AsString());
                case CURLOPT_BINARYTRANSFER: break;   // no effect
                case CURLOPT_TCP_NODELAY: ch.SetOption(new CurlOption_DisableTcpNagle { OptionValue = value.ToBoolean() }); break;
                case CURLOPT_PRIVATE: ch.SetOption(new CurlOption_Private { OptionValue = value.GetValue().DeepCopy() }); break;
                case CURLOPT_TIMEOUT: { if (value.IsLong(out long l)) ch.Timeout = (int)l * 1000; break; }
                case CURLOPT_TIMEOUT_MS: { if (value.IsLong(out long l)) ch.Timeout = (int)l; break; }
                case CURLOPT_CONNECTTIMEOUT: break;      // TODO: is there an alternative in .NET ?
                case CURLOPT_CONNECTTIMEOUT_MS: break;   // TODO: is there an alternative in .NET ?
                case CURLOPT_BUFFERSIZE:
                    {
                        if (value.IsLong(out long l) && l < int.MaxValue && l >= 0)
                        {
                            ch.BufferSize = (int)l;
                            return true;
                        }
                        return false;
                    }
                case CURLOPT_EXPECT_100_TIMEOUT_MS: { if (value.IsLong(out long l)) ch.ContinueTimeout = (int)l; break; }
                case CURLOPT_HTTP_VERSION:
                    {
                        Version protocol;

                        switch ((int)value.ToLong())
                        {
                            case CURL_HTTP_VERSION_NONE: ch.RemoveOption<CurlOption_ProtocolVersion>(); return true;
                            case CURL_HTTP_VERSION_1_0: protocol = HttpVersion.Version10; break;
                            case CURL_HTTP_VERSION_1_1: protocol = HttpVersion.Version11; break;
                            case CURL_HTTP_VERSION_2_0: // == CURL_HTTP_VERSION_2:
                            case CURL_HTTP_VERSION_2TLS: protocol = new Version(2, 0); break; // HttpVersion.Version20
                            default: return false;
                        }

                        return SetOption<CurlOption_ProtocolVersion, Version>(ch, protocol);
                    }

                case CURLOPT_USERNAME: ch.Username = value.ToString(); break;
                case CURLOPT_USERPWD: (ch.Username, ch.Password) = SplitUserPwd(value.ToString()); break;
                // case CURLOPT_PROXYAUTH:
                // case CURLOPT_PROXY_SERVICE_NAME:
                case CURLOPT_PROXYTYPE:
                    // only http supported now
                    switch ((int)value.ToLong())
                    {
                        case CURLPROXY_HTTP:
                            ch.ProxyType = "http";
                            break;
                        default:
                            PhpException.ArgumentValueNotSupported(nameof(option), nameof(CURLOPT_PROXYTYPE));
                            break;
                    }
                    break;
                case CURLOPT_PROXY:
                    (ch.ProxyType, ch.ProxyUsername, ch.ProxyPassword, ch.ProxyHost, ch.ProxyPort) = ParseProxy(value.ToString());
                    break;
                case CURLOPT_PROXYPORT:
                    ch.ProxyPort = (int)value.ToLong();
                    break;
                case CURLOPT_PROXYUSERPWD:
                    string[] auth = value.ToString().Split(':');
                    if (auth.Length != 2)
                    {
                        PhpException.InvalidArgument(nameof(CURLOPT_PROXYUSERPWD));
                    }
                    ch.ProxyUsername = auth[0];
                    ch.ProxyPassword = auth[1];
                    break;
                case CURLOPT_PROTOCOLS: ch.Protocols = (int)value.ToLong(); break;
                case CURLOPT_REDIR_PROTOCOLS:
                    PhpException.ArgumentValueNotSupported(nameof(option), nameof(CURLOPT_REDIR_PROTOCOLS));
                    break;

                case CURLOPT_SSL_VERIFYHOST:
                case CURLOPT_SSL_VERIFYPEER:
                case CURLOPT_SSL_VERIFYSTATUS:
                    // always enabled
                    break;

                case CURLINFO_HEADER_OUT: ch.StoreRequestHeaders = value.ToBoolean(); break;
                case CURLOPT_VERBOSE: ch.Verbose = value.ToBoolean(); break;
                case CURLOPT_STDERR: ch.VerboseOutput = TryProcessMethodFromStream(value); return ch.VerboseOutput != null || Operators.IsEmpty(value);
                case CURLOPT_FAILONERROR: ch.FailOnError = value.ToBoolean(); break;

                case CURLOPT_FRESH_CONNECT: break; // ignored, let the system decide
                case CURLOPT_FORBID_REUSE: break; // ignored for now, Dispose() always

                case CURLOPT_SAFE_UPLOAD: ch.SafeUpload = value.ToBoolean(); break;

                //
                default:
                    if (option > 0)
                    {
                        PhpException.ArgumentValueNotSupported(nameof(option), TryGetOptionName(option));
                        return false;
                    }
                    else
                    {
                        // ignored option
                        return true;
                    }
            }

            return true;
        }

        /// <summary>
        /// Lookups the constant name (CURLOPT_*) with given value.
        /// </summary>
        static string TryGetOptionName(long optionValue)
        {
            var field = typeof(CURLConstants)
                .GetFields()
                .FirstOrDefault(f =>
                    f.Name.StartsWith("CURLOPT_", StringComparison.Ordinal) &&
                    f.GetRawConstantValue() switch
                    {
                        int i => i == optionValue,
                        long l => l == optionValue,
                        _ => false,
                    });

            return field != null ? field.Name : optionValue.ToString();
        }

        static (string username, string password) SplitUserPwd(string value)
        {
            int colPos = value.IndexOf(':');
            if (colPos == -1)
            {
                return (value, string.Empty);
            }
            else
            {
                return (value.Substring(0, colPos), value.Substring(colPos + 1));
            }
        }

        static (string scheme, string username, string password, string host, int port) ParseProxy(string proxy)
        {
            string proxy_string = Regex.Replace(proxy, @"\s+", "");

            string scheme = "http";
            string username = "";
            string password = "";
            string host = "";
            int port = 1080;

            if (proxy_string.IndexOf("://") != -1)
            {
                string[] pair = Regex.Split(proxy_string, "://");
                scheme = pair[0];
                proxy_string = pair[1];
            }

            string host_string = proxy_string;

            if (proxy_string.IndexOf("@") != -1)
            {
                string[] pair = Regex.Split(proxy_string, "@");
                host_string = pair[1];

                string[] auth = Regex.Split(pair[0], ":");
                username = auth[0];
                if (auth.Length == 2)
                {
                    password = auth[1];
                }
            }

            string[] address = Regex.Split(host_string, ":");

            host = address[0];

            if (address.Length == 2)
            {
                if (int.TryParse(address[1].TrimEnd('/'), out var p))
                {
                    port = p;
                }
            }

            return (scheme, username, password, host, port);
        }

        internal static bool TryGetOption(this CURLResource ch, int option, out PhpValue value)
        {
            switch (option)
            {
                default:
                    value = PhpValue.Null;
                    return false;
            }
        }

        /// <summary>
        /// Sets cURL option.
        /// </summary>
        static bool SetOption<TOption, TValue>(CURLResource resource, TValue value)
            where TOption : CurlOption<HttpWebRequest, TValue>, new()
            where TValue : class
        {
            if (value != null)
            {
                resource.SetOption<TOption>(new TOption() { OptionValue = value });
                return true;
            }

            //
            return false;
        }

        internal static string GetErrorString(this CurlMultiErrors err)
        {
            return Resources.ResourceManager.GetString(err.ToString()) ?? Resources.UnknownError;
        }

        /// <summary>Empty string is converted to <c>null</c>.</summary>
        static string EmptyToNull(this string str) => string.IsNullOrEmpty(str) ? null : str;

        /// <summary>
        /// Writes message to the verbose output, if verboseis enabled.
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="message"></param>
        internal static void VerboseOutput(this CURLResource ch, string message)
        {
            if (ch.Verbose)
            {
                if (ch.VerboseOutput != null)
                {
                    ch.VerboseOutput.WriteString(message);
                    ch.VerboseOutput.WriteString(Environment.NewLine);
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine("cURL: " + message);
                }
            }
        }

        #endregion
    }

    #region ICurlOption // cURL option setters

    /// <summary>
    /// An actual cURL option value.
    /// </summary>
    interface ICurlOption : IEquatable<ICurlOption>
    {
        int OptionId { get; }

        void Apply(Context ctx, WebRequest request);
    }

    /// <summary>
    /// An actual cURL option value for specific <see cref="WebRequest"/> (ftp, http, ...) with a value.
    /// </summary>
    /// <typeparam name="TRequest">Type of <see cref="WebRequest"/>.</typeparam>
    /// <typeparam name="TValue">Option value type.</typeparam>
    abstract class CurlOption<TRequest, TValue> : ICurlOption where TRequest : WebRequest
    {
        public abstract int OptionId { get; }

        public TValue OptionValue { get; set; }

        void ICurlOption.Apply(Context ctx, WebRequest request)
        {
            if (request is TRequest r)
            {
                Apply(ctx, r);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public abstract void Apply(Context ctx, TRequest request);

        bool IEquatable<ICurlOption>.Equals(ICurlOption other)
        {
            return other != null && other.OptionId == OptionId;
        }
    }

    sealed class CurlOption_UserAgent : CurlOption<HttpWebRequest, string>
    {
        public override int OptionId => CURLConstants.CURLOPT_USERAGENT;
        public override void Apply(Context ctx, HttpWebRequest request) => request.UserAgent = this.OptionValue;
    }

    sealed class CurlOption_Referer : CurlOption<HttpWebRequest, string>
    {
        public override int OptionId => CURLConstants.CURLOPT_REFERER;
        public override void Apply(Context ctx, HttpWebRequest request) => request.Referer = this.OptionValue;
    }

    sealed class CurlOption_ProtocolVersion : CurlOption<HttpWebRequest, Version>
    {
        public override int OptionId => CURLConstants.CURLOPT_HTTP_VERSION;
        public override void Apply(Context ctx, HttpWebRequest request) => request.ProtocolVersion = this.OptionValue;
    }

    sealed class CurlOption_Private : CurlOption<WebRequest, PhpValue>
    {
        public override int OptionId => CURLConstants.CURLOPT_PRIVATE;
        public override void Apply(Context ctx, WebRequest request) { }
    }

    /// <summary>
    /// Controls the "Accept-Encoding" header.
    /// </summary>
    sealed class CurlOption_AcceptEncoding : CurlOption<HttpWebRequest, string>
    {
        public override int OptionId => CURLConstants.CURLOPT_ACCEPT_ENCODING;

        public override void Apply(Context ctx, HttpWebRequest request)
        {
            if (this.OptionValue != null)
            {
                if (this.OptionValue.Length != 0)
                {
                    request.Headers.Set(HttpRequestHeader.AcceptEncoding, this.OptionValue);
                }
            }
            else
            {
                // NULL specifically disables sending accept-encoding header
                request.Headers.Remove(HttpRequestHeader.AcceptEncoding);
            }
        }
    }

    /// <summary>
    /// Headers to be send with the request.
    /// Keys of the array are ignored, values are in form of <c>header-name: value</c>
    /// </summary>
    sealed class CurlOption_Headers : CurlOption<HttpWebRequest, PhpArray>
    {
        public override int OptionId => CURLConstants.CURLOPT_HTTPHEADER;

        public override void Apply(Context ctx, HttpWebRequest request)
        {
            foreach (var value in this.OptionValue)
            {
                if (value.Value.IsString(out var header))
                {
                    // split into name:value once
                    string header_name, header_value;

                    var colpos = header.IndexOf(':');
                    if (colpos >= 0)
                    {
                        header_name = header.Remove(colpos);
                        header_value = header.Substring(colpos + 1);
                    }
                    else
                    {
                        header_name = header;
                        header_value = string.Empty;
                    }

                    // set the header,
                    // replace previously set header or remove header with no value

                    if (header_value.Length != 0)
                    {
                        request.Headers.Set(header_name, header_value);
                    }
                    else
                    {
                        request.Headers.Remove(header_name);
                    }
                }
            }
        }
    }

    sealed class CurlOption_DisableTcpNagle : CurlOption<HttpWebRequest, bool>
    {
        public override int OptionId => CURLConstants.CURLOPT_TCP_NODELAY;
        public override void Apply(Context ctx, HttpWebRequest request) => request.ServicePoint.UseNagleAlgorithm = !OptionValue;
    }

    /// <summary>
    /// Provides value of <see cref="CURLConstants.CURLOPT_COOKIEJAR"/> option.
    /// </summary>
    sealed class CurlOption_CookieJar : CurlOption<HttpWebRequest, string>
    {
        public override int OptionId => CURLConstants.CURLOPT_COOKIEJAR;
        public override void Apply(Context ctx, HttpWebRequest request)
        {
            // invoked when initializing WebRequest
            // do nothing
        }

        public void PrintCookies(Context ctx, CURLResource resource)
        {
            // output the cookies:

            var cookies = resource.Result?.Cookies;
            if (cookies == null)
            {
                return;
            }

            PhpStream output;
            var dispose = false;    // whether to close the stream

            if (string.Equals(OptionValue, "-", StringComparison.Ordinal))
            {
                // curl writes to STDOUT, and so do we:
                output = PhpWrappers.STDOUT;
            }
            else
            {
                // PHP-compliant file resolve function:
                output = PhpStream.Open(ctx, OptionValue, StreamOpenMode.WriteText);
                dispose = true;
            }

            if (output != null)
            {
                try
                {
                    // header:
                    output.WriteString(string.Join(Environment.NewLine, new string[] {
                        "# Netscape HTTP Cookie File",
                        "# https://curl.haxx.se/docs/http-cookies.html",
                        "# This file was generated by PeachPie! Edit at your own risk.",
                        "",
                        ""
                    }));

                    // output the cookies in netscape style:
                    foreach (var line in CURLFunctions.CookiesToNetscapeStyle(cookies))
                    {
                        output.WriteString(line);
                        output.WriteString(Environment.NewLine);
                    }
                }
                finally
                {
                    //
                    if (dispose)
                    {
                        output.Dispose();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Reads cookies from a file according to <see cref="CURLConstants.CURLOPT_COOKIEFILE"/> option.
    /// </summary>
    sealed class CurlOption_CookieFile : CurlOption<HttpWebRequest, List<string>>
    {
        public override int OptionId => CURLConstants.CURLOPT_COOKIEFILE;

        public CurlOption_CookieFile()
        {
            OptionValue = new List<string>();
        }

        public override void Apply(Context ctx, HttpWebRequest request)
        {
            // invoked when initializing WebRequest

            foreach (var fname in this.OptionValue)
            {
                using (var stream = PhpStream.Open(ctx, fname, StreamOpenMode.ReadText))
                {
                    if (stream != null)
                    {
                        LoadCookieFile(request, stream);
                    }
                }
            }
        }

        private static bool TryParseNetscapeCookie(string line, out Cookie cookie)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            cookie = null;

            // check for `HttpOnly` cookies and remove the #HttpOnly_ prefix
            const string httpOnlyString = "#HttpOnly_";
            var httpOnly = line.StartsWith(httpOnlyString, StringComparison.OrdinalIgnoreCase);
            if (httpOnly)
            {
                line = line.Substring(httpOnlyString.Length);
            }

            // we only look for non-comment, non-blank, valid cookies per lines
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.Count(c => c == '\t') != 6)
            {
                return false;
            }

            // get tokens in an array and trim them
            string[] tokens = line.Split('\t').Select(token => token.Trim()).ToArray();

            // gather current cookie information in a new cookie object
            cookie = new Cookie();

            // The domain that created AND can read the variable.
            cookie.Domain = tokens[0].TrimStart('.');

            // The path within the domain that the variable is valid for.
            cookie.Path = PathUtils.TrimEndSeparator(tokens[2]);

            if (string.IsNullOrEmpty(cookie.Path))
            {
                cookie.Path = "/";
            }

            // boolean value indicating if all machines within a given domain can access the variable.
            if (!bool.TryParse(tokens[1], out var subdomainAccess))
                return false;

            //     cookie.subdomainAccess = subdomainAccess;  // ignored

            if (!bool.TryParse(tokens[3], out var secure))
                return false;

            // boolean value indicating if a secure connection with the domain is needed to access the variable.
            cookie.Secure = secure;

            if (!long.TryParse(tokens[4], out var expires))
                return false;

            // expiration date from unix timestamp.
            // note: if the "expires" property is set to "0", set to DateTime.MinValue as it makes this a session cookie (default)
            cookie.Expires = expires == 0 ? DateTime.MinValue : DateTimeUtils.UnixTimeStampToUtc(expires);

            // set cookie name and value
            cookie.Name = tokens[5];
            cookie.Value = tokens[6];  // TODO: should HttpUtility.UrlDecode be used here? more research needed

            // Cookies marked with httpOnly are meant not to be accessible from javascripts
            cookie.HttpOnly = httpOnly;

            //
            return true;
        }

        private static void LoadCookieFile(HttpWebRequest request, PhpStream stream)
        {
            // load netscape-like or header-style cookies from stream

            // TODO: add support for header-style cookies (`Set-Cookie: ...`).
            //       currently, only curl's netscape-like cookies are supported.

            // request.CookieContainer.SetCookies( ... ) // set header style cookies

            // iterate over the cookie jar lines
            while (!stream.Eof)
            {
                // read a line, and strip off the possible end-of-line characters
                var line = stream.ReadLine(-1, null).Trim();

                if (TryParseNetscapeCookie(line, out var cookie))
                {
                    // add the parsed cookie
                    request.CookieContainer.Add(cookie);
                }
            }
        }
    }

    #endregion

    static class HttpHeaders
    {
        /// <summary>
        /// Gets response status header (the first line),
        /// ASCII only, in form of <c>HTTP/X.X CODE DESCRIPTION</c>.
        /// </summary>
        public static string StatusHeader(HttpWebResponse response) => $"HTTP/{response.ProtocolVersion.ToString(2)} {(int)response.StatusCode} {response.StatusDescription}";

        public const string HeaderSeparator = "\r\n";

        public static string HeaderString(HttpWebRequest req)
        {
            return $"{req.Method} {req.RequestUri.PathAndQuery} HTTP/{req.ProtocolVersion.ToString(2)}\r\nHost: {req.Host}\r\n{req.Headers.ToString()}\r\n";
        }
    }

    #region CurlErrors

    /// <summary>
    /// <c>CURLE_</c> constants.
    /// </summary>
    public enum CurlErrors
    {
        CURLE_ABORTED_BY_CALLBACK = 42,
        CURLE_BAD_CALLING_ORDER = 44,
        CURLE_BAD_CONTENT_ENCODING = 61,
        CURLE_BAD_DOWNLOAD_RESUME = 36,
        CURLE_BAD_FUNCTION_ARGUMENT = 43,
        CURLE_BAD_PASSWORD_ENTERED = 46,
        CURLE_COULDNT_CONNECT = 7,
        CURLE_COULDNT_RESOLVE_HOST = 6,
        CURLE_COULDNT_RESOLVE_PROXY = 5,
        CURLE_FAILED_INIT = 2,
        CURLE_FILE_COULDNT_READ_FILE = 37,
        CURLE_FTP_ACCESS_DENIED = 9,
        CURLE_FTP_BAD_DOWNLOAD_RESUME = 36,
        CURLE_FTP_CANT_GET_HOST = 15,
        CURLE_FTP_CANT_RECONNECT = 16,
        CURLE_FTP_COULDNT_GET_SIZE = 32,
        CURLE_FTP_COULDNT_RETR_FILE = 19,
        CURLE_FTP_COULDNT_SET_ASCII = 29,
        CURLE_FTP_COULDNT_SET_BINARY = 17,
        CURLE_FTP_COULDNT_STOR_FILE = 25,
        CURLE_FTP_COULDNT_USE_REST = 31,
        CURLE_FTP_PARTIAL_FILE = 18,
        CURLE_FTP_PORT_FAILED = 30,
        CURLE_FTP_QUOTE_ERROR = 21,
        CURLE_FTP_USER_PASSWORD_INCORRECT = 10,
        CURLE_FTP_WEIRD_227_FORMAT = 14,
        CURLE_FTP_WEIRD_PASS_REPLY = 11,
        CURLE_FTP_WEIRD_PASV_REPLY = 13,
        CURLE_FTP_WEIRD_SERVER_REPLY = 8,
        CURLE_FTP_WEIRD_USER_REPLY = 12,
        CURLE_FTP_WRITE_ERROR = 20,
        CURLE_FUNCTION_NOT_FOUND = 41,
        CURLE_GOT_NOTHING = 52,
        CURLE_HTTP_NOT_FOUND = 22,
        CURLE_HTTP_PORT_FAILED = 45,
        CURLE_HTTP_POST_ERROR = 34,
        CURLE_HTTP_RANGE_ERROR = 33,
        CURLE_HTTP_RETURNED_ERROR = 22,
        CURLE_LDAP_CANNOT_BIND = 38,
        CURLE_LDAP_SEARCH_FAILED = 39,
        CURLE_LIBRARY_NOT_FOUND = 40,
        CURLE_MALFORMAT_USER = 24,
        CURLE_OBSOLETE = 50,
        CURLE_OK = 0,
        CURLE_OPERATION_TIMEDOUT = 28,
        CURLE_OPERATION_TIMEOUTED = 28,
        CURLE_OUT_OF_MEMORY = 27,
        CURLE_PARTIAL_FILE = 18,
        CURLE_READ_ERROR = 26,
        CURLE_RECV_ERROR = 56,
        CURLE_SEND_ERROR = 55,
        CURLE_SHARE_IN_USE = 57,
        CURLE_SSL_CACERT = 60,
        CURLE_SSL_CERTPROBLEM = 58,
        CURLE_SSL_CIPHER = 59,
        CURLE_SSL_CONNECT_ERROR = 35,
        CURLE_SSL_ENGINE_NOTFOUND = 53,
        CURLE_SSL_ENGINE_SETFAILED = 54,
        CURLE_SSL_PEER_CERTIFICATE = 51,
        CURLE_SSL_PINNEDPUBKEYNOTMATCH = 90,
        CURLE_TELNET_OPTION_SYNTAX = 49,
        CURLE_TOO_MANY_REDIRECTS = 47,
        CURLE_UNKNOWN_TELNET_OPTION = 48,
        CURLE_UNSUPPORTED_PROTOCOL = 1,
        CURLE_URL_MALFORMAT = 3,
        CURLE_URL_MALFORMAT_USER = 4,
        CURLE_WRITE_ERROR = 23,
        CURLE_FILESIZE_EXCEEDED = 63,
        CURLE_LDAP_INVALID_URL = 62,
        CURLE_FTP_SSL_FAILED = 64,
        CURLE_SSL_CACERT_BADFILE = 77,
        CURLE_SSH = 79,
    }

    #endregion

    #region CurlMultiErrors

    /// <summary>
    /// <c>CURLM_*</c> constants.
    /// </summary>
    /// <remarks>The names correspond to resources, see <see cref="Resources"/>.</remarks>
    public enum CurlMultiErrors
    {
        CURLM_OK = 0,
        CURLM_BAD_HANDLE = 1,
        CURLM_BAD_EASY_HANDLE = 2,
        CURLM_OUT_OF_MEMORY = 3,
        CURLM_INTERNAL_ERROR = 4,
        CURLM_ADDED_ALREADY = 7,
        CURLM_CALL_MULTI_PERFORM = -1,
    }

    #endregion
}
