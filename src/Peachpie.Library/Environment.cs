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

        public static readonly string PEACHPIE_VERSION = typeof(Core.Context).GetTypeInfo().Assembly.GetName().Version.ToString();

        /// <summary>
        /// The operating system family PHP was built for.
        /// Either of 'Windows', 'BSD', 'OSX', 'Solaris', 'Linux' or 'Unknown'.
        /// </summary>
        /// <remarks>Available as of PHP 7.2.0.</remarks>
        public static string PHP_OS_FAMILY => CurrentPlatform.IsWindows ? "Windows" : CurrentPlatform.IsLinux ? "Linux" : CurrentPlatform.IsOsx ? "OSX" : "Unknown";

        //_constants.Add("PHP_SAPI", (System.Web.HttpContext.Current == null) ? "cli" : "isapi", false);
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
    }
}
