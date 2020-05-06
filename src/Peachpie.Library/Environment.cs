using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    /// <summary>
    /// Environment constants and functions.
    /// </summary>
    [PhpExtension("Core")]
    public static class Environment
    {
        public const int PHP_MAJOR_VERSION = 7;
        public const int PHP_MINOR_VERSION = 3;
        public const int PHP_RELEASE_VERSION = 69;
        public const int PHP_VERSION_ID = PHP_MAJOR_VERSION * 10000 + PHP_MINOR_VERSION * 100 + PHP_RELEASE_VERSION;
        public static readonly string PHP_VERSION = PHP_MAJOR_VERSION + "." + PHP_MINOR_VERSION + "." + PHP_RELEASE_VERSION + PHP_EXTRA_VERSION;

        public const string PHP_EXTRA_VERSION = "-peachpie";
        public static string PHP_OS => CurrentPlatform.IsWindows ? "WINNT" : CurrentPlatform.IsLinux ? "Linux" : CurrentPlatform.IsOsx ? "Darwin" : "Unix";

        public static readonly string PEACHPIE_VERSION = ContextExtensions.GetRuntimeInformationalVersion();

        /// <summary>
        /// The operating system family PHP was built for.
        /// Either of 'Windows', 'BSD', 'OSX', 'Solaris', 'Linux' or 'Unknown'.
        /// </summary>
        /// <remarks>Available as of PHP 7.2.0.</remarks>
        public static string PHP_OS_FAMILY => CurrentPlatform.IsWindows ? "Windows" : CurrentPlatform.IsLinux ? "Linux" : CurrentPlatform.IsOsx ? "OSX" : "Unknown";

        //_constants.Add("PHP_SAPI", (System.Web.HttpContext.Current == null) ? "cli" : "isapi", false); // "hardcoded" in Context
        //_constants.Add("DIRECTORY_SEPARATOR", FullPath.DirectorySeparatorString, false);
        public static readonly string DIRECTORY_SEPARATOR = CurrentPlatform.DirectorySeparator.ToString();
        //_constants.Add("PATH_SEPARATOR", Path.PathSeparator.ToString(), false);
        public static readonly string PATH_SEPARATOR = CurrentPlatform.PathSeparator.ToString();

        public const long PHP_INT_SIZE = sizeof(long);
        public const long PHP_INT_MIN = long.MinValue;
        public const long PHP_INT_MAX = long.MaxValue;

        public const double PHP_FLOAT_MIN = double.MinValue;
        public const double PHP_FLOAT_MAX = double.MaxValue;

        /// <summary>Smallest representable positive number x, so that x + 1.0 != 1.0.</summary>
        public const double PHP_FLOAT_EPSILON = double.Epsilon;
        /// <summary>Number of decimal digits that can be rounded into a float and back without precision loss.</summary>
        public const double PHP_FLOAT_DIG = 15;

        public const object NULL = null;
        public const bool TRUE = true;
        public const bool FALSE = false;

        public static string PHP_EOL => System.Environment.NewLine;

        public const int PHP_MAXPATHLEN = 260; // The max path length on CLR is limited to 260 (see System.IO.Path.MaxPath)

        public static readonly int PHP_DEBUG = ContextExtensions.IsDebugRuntime() ? 1 : 0;

        public const int PHP_ZTS = 1;

        /// <summary>This is a workstation system, specified as a value of <c>PHP_WINDOWS_VERSION_PRODUCTTYPE</c> constant.</summary>
        public const int PHP_WINDOWS_NT_WORKSTATION = 1;

        /// <summary>This is a server system, specified as a value of <c>PHP_WINDOWS_VERSION_PRODUCTTYPE</c> constant.</summary>
        public const int PHP_WINDOWS_NT_SERVER = 3;

        /// <summary>This is a domain controller, specified as a value of <c>PHP_WINDOWS_VERSION_PRODUCTTYPE</c> constant.</summary>
        public const int PHP_WINDOWS_NT_DOMAIN_CONTROLLER = 2;

        /// <summary>
        /// Default FD_SETSIZE is 64.
        /// Mono is compiled with FD_SETSIZE=1024.
        /// <c>Socket.Select</c> on Unix uses poll instead of system select (*remarks).
        /// WinSock2 does not seem to have the limitation either (???).
        /// Socket.Select is limited to 65536 items in check lists.
        /// And there is no way of determining the constant in rutnime AFAIK.
        /// </summary>
        /// <remarks>
        /// https://github.com/dotnet/runtime/blob/ca1a6842d796d95b44a64222b023263f023a6c5e/src/libraries/System.Net.Sockets/src/System/Net/Sockets/SocketPal.Unix.cs#L1491
        /// </remarks>
        public const int PHP_FD_SETSIZE = 65536;
    }
}
