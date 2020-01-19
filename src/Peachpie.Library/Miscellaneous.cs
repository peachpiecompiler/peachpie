#nullable enable

using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    [PhpExtension("Core")]
    public static class Miscellaneous
    {
        // [return: CastToFalse] // once $extension will be supported
        public static string phpversion(string? extension = null)
        {
            if (extension != null)
            {
                throw new NotImplementedException(nameof(extension));
            }

            return Environment.PHP_MAJOR_VERSION + "." + Environment.PHP_MINOR_VERSION + "." + Environment.PHP_RELEASE_VERSION;
        }

        /// <summary>
        /// Gets "zend" engine version compatible version string.
        /// </summary>
        public static string zend_version() => "3." + Environment.PHP_MINOR_VERSION + ".0" + Core.Utilities.ContextExtensions.GetRuntimeVersionSuffix();

        #region Helpers

        /// <summary>
        /// Compares parts of varsions delimited by '.'.
        /// </summary>
        /// <param name="part1">A part of the first version.</param>
        /// <param name="part2">A part of the second version.</param>
        /// <returns>The result of parts comparison (-1,0,+1).</returns>
        static int CompareParts(string part1, string part2)
        {
            string[] parts = { "dev", "alpha", "a", "beta", "b", "RC", " ", "#", "pl", "p" };
            int[] order = { -1, 0, 1, 1, 2, 2, 3, 4, 5, 6, 6 };

            int i = Array.IndexOf(parts, part1);
            int j = Array.IndexOf(parts, part2);
            return Math.Sign(order[i + 1] - order[j + 1]);
        }

        /// <summary>
		/// Parses a version and splits it into an array of parts.
		/// </summary>
		/// <param name="version">The version to be parsed (can be a <B>null</B> reference).</param>
		/// <returns>An array of parts.</returns>
		/// <remarks>
		/// Non-alphanumeric characters are eliminated.
		/// The version is split in between a digit following a non-digit and by   
		/// characters '.', '-', '+', '_'. 
		/// </remarks>
		static string[] VersionToArray(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return Array.Empty<string>();
            }

            var sb = new StringBuilder(version.Length);
            char last = '\0';

            for (int i = 0; i < version.Length; i++)
            {
                var ch = version[i];
                if (ch == '-' || ch == '+' || ch == '_' || ch == '.')
                {
                    if (last != '.')
                    {
                        if (sb.Length == 0)
                        {
                            sb.Append('0'); // prepend leading '.' with '0' // TODO: test case and rewrite 'version_compare()'
                        }

                        sb.Append(last = '.');
                    }
                }
                else if (i > 0 && (char.IsDigit(ch) ^ char.IsDigit(version[i - 1])))
                {
                    if (last != '.')
                    {
                        sb.Append('.');
                    }
                    sb.Append(last = ch);
                }
                else if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(last = ch);
                }
                else
                {
                    if (last != '.')
                    {
                        sb.Append(last = '.');
                    }
                }
            }

            if (last == '.')
            {
                sb.Length--;
            }

            return sb.ToString().Split('.');
        }

        #endregion

        /// <summary>
        /// Compares two "PHP-standardized" version number strings.
        /// </summary>
        public static int version_compare(string version1, string version2)
        {
            string[] v1 = VersionToArray(version1);
            string[] v2 = VersionToArray(version2);
            int result;

            for (int i = 0; i < Math.Max(v1.Length, v2.Length); i++)
            {
                string item1 = (i < v1.Length) ? v1[i] : " ";
                string item2 = (i < v2.Length) ? v2[i] : " ";

                if (char.IsDigit(item1[0]) && char.IsDigit(item2[0]))
                {
                    result = Comparison.Compare(Core.Convert.StringToLongInteger(item1), Core.Convert.StringToLongInteger(item2));
                }
                else
                {
                    result = CompareParts(char.IsDigit(item1[0]) ? "#" : item1, char.IsDigit(item2[0]) ? "#" : item2);
                }

                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        /// <summary>
        /// Compares two "PHP-standardized" version number strings.
        /// </summary>
        public static bool version_compare(string version1, string version2, string op)
        {
            var compare = version_compare(version1, version2);

            switch (op)
            {
                case "<":
                case "lt": return compare < 0;

                case "<=":
                case "le": return compare <= 0;

                case ">":
                case "gt": return compare > 0;

                case ">=":
                case "ge": return compare >= 0;

                case "==":
                case "=":
                case "eq": return compare == 0;

                case "!=":
                case "<>":
                case "ne": return compare != 0;
            }

            throw new ArgumentException();  // TODO: return NULL
        }

        /// <summary>
        /// Loads extension dynamically.
        /// </summary>
        public static bool dl(string library)
        {
            PhpException.FunctionNotSupported("dl");
            return false;
        }

        /// <summary>
        /// Find out whether an extension is loaded.
        /// </summary>
        /// <param name="name">The extension name.</param>
        /// <returns>Returns <c>TRUE</c> if the extension identified by name is loaded, <c>FALSE</c> otherwise.</returns>
        public static bool extension_loaded(string name) => Context.IsExtensionLoaded(name);

        /// <summary>
        /// Returns an array with names of all loaded native extensions.
        /// </summary>
        /// <param name="zend_extensions">Only return Zend extensions.</param>
        public static PhpArray get_loaded_extensions(bool zend_extensions = false)
        {
            if (zend_extensions)
            {
                throw new NotImplementedException(nameof(zend_extensions));
            }

            var extensions = Context.GetLoadedExtensions();
            var result = new PhpArray(extensions.Count);

            foreach (var e in extensions)
            {
                result.Add(PhpValue.Create(e));
            }

            return result;
        }

        /// <summary>
		/// Returns an array with names of the functions of a native extension.
		/// </summary>
        /// <param name="extension">Internal extension name (e.g. <c>sockets</c>).</param>
        /// <returns>The array of function names or <c>null</c> if the <paramref name="extension"/> is not loaded.</returns>
        [return: CastToFalse]
        public static PhpArray? get_extension_funcs(string extension)
        {
            var result = new PhpArray();
            foreach (var e in Context.GetRoutinesByExtension(extension))
            {
                result.Add(PhpValue.Create(e.Name));
            }

            // gets NULL (FALSE) if there are no functions
            return result.Count != 0 ? result : null;
        }

        /// <summary>
        /// Checks the given <paramref name="assertion"/> and take appropriate action if its result is <c>FALSE</c>.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="assertion"></param>
        /// <param name="action"></param>
        /// <returns>Assertion value.</returns>
        public static bool assert(Context ctx, PhpValue assertion, PhpValue action = default)
        {
            // TODO: check assertion is enabled

            if (assertion.IsString())
            {
                PhpException.InvalidArgumentType(nameof(assertion), PhpVariable.TypeNameBoolean);
                return true;
            }

            return ctx.Assert(assertion, action);
        }

        ///// <summary>
        ///// Returns an array of all currently active resources, optionally filtered by resource type.
        ///// </summary>
        ///// <param name="ctx">Runtime context.</param>
        ///// <param name="type">
        ///// If defined, this will cause get_resources() to only return resources of the given type. A list of resource types is available.
        ///// If the string <code>Unknown</code> is provided as the type, then only resources that are of an unknown type will be returned.
        ///// If omitted, all resources will be returned.
        ///// </param>
        ///// <returns>Returns an array of currently active resources, indexed by resource number.</returns>
        //[return: NotNull]
        //public static PhpArray get_resources(Context ctx, string type = null)
        //{
        //    throw new NotSupportedException();
        //}

        #region gethostname, php_uname, memory_get_usage, php_sapi_name

        /// <summary>
        /// gethostname() gets the standard host name for the local machine. 
        /// </summary>
        /// <returns>Returns a string with the hostname on success, otherwise FALSE is returned. </returns>
        [return: CastToFalse]
        public static string? gethostname()
        {
            string? host;
            try
            {
                host = System.Net.Dns.GetHostName();
            }
            catch
            {
                host = null;
            }

            return host;
        }

        /// <summary>
        /// Retrieves specific version information about OS.
        /// </summary>
        /// <param name="mode">
        /// <list type="bullet">
        /// <term>'a'</term><description>This is the default. Contains all modes in the sequence "s n r v m".</description>
        /// <term>'s'</term><description>Operating system name, e.g. "Windows NT", "Windows 9x".</description>
        /// <term>'n'</term><description>Host name, e.g. "www.php-compiler.net".</description>
        /// <term>'r'</term><description>Release name, e.g. "5.1".</description>
        /// <term>'v'</term><description>Version information. Varies a lot between operating systems, e.g. "build 2600".</description>
        /// <term>'m'</term><description>Machine type. eg. "i586".</description>
        /// </list>
        /// </param>
        /// <returns>OS version.</returns>
        public static string php_uname(string mode = "a")
        {
            string system, host, release, version, machine;

            if (CurrentPlatform.IsWindows) system = "Windows NT";
            else if (CurrentPlatform.IsOsx) system = "OSX";
            else system = "Unix";

            host = System.Net.Dns.GetHostName();

            machine = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            if (machine == "x86") machine = "i586";    // TODO

            release = "0"; // String.Concat(Environment.OSVersion.Version.Major, ".", Environment.OSVersion.Version.Minor);
            version = "0"; // String.Concat("build ", Environment.OSVersion.Version.Build);

            //
            if (!string.IsNullOrEmpty(mode))
            {
                switch (mode[0])
                {
                    case 's': return system;
                    case 'r': return release;
                    case 'v': return version;
                    case 'm': return machine;
                    case 'n': return host;
                }
            }

            //
            return $"{system} {host} {release} {version} {machine}";
        }

        /// <summary>
        /// Retrieves the size of the current process working set in bytes.
        /// </summary>
        /// <param name="real_usage">
        /// "Set this to TRUE to get the real size of memory allocated from system.
        /// If not set or FALSE only the memory used by emalloc() is reported."</param>
        /// <returns>The size.</returns>
        public static long memory_get_usage(bool real_usage = false)
        {
            //if (real_usage == false)// TODO: real_usage = false
            //    PhpException.ArgumentValueNotSupported("real_usage");

            long ws = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            if (ws > int.MaxValue) return int.MaxValue;
            return (int)ws;
        }

        /// <summary>
        /// Returns the peak of memory, in bytes, that's been allocated to the PHP script.
        /// </summary>
        /// <param name="real_usage">
        /// Set this to TRUE to get the real size of memory allocated from system.
        /// If not set or FALSE only the memory used by emalloc() is reported.</param>
        /// <returns>The size.</returns>
        public static long memory_get_peak_usage(bool real_usage = false)
        {
            //if (real_usage == false)// TODO: real_usage = false
            //    PhpException.ArgumentValueNotSupported("real_usage");

            long ws = System.Diagnostics.Process.GetCurrentProcess().NonpagedSystemMemorySize64;    // can't get current thread's memory
            if (ws > int.MaxValue) return int.MaxValue;
            return (int)ws;
        }

        /// <summary>
        /// Returns the type of web server interface.
        /// </summary>
        /// <returns>The "isapi" string if runned under webserver (ASP.NET works via ISAPI) or "cli" otherwise.</returns>
        public static string php_sapi_name(Context ctx) => ctx.ServerApi;

        #endregion

        #region getmypid, getlastmod, get_current_user, getmyuid, posix_getpid

        /// <summary>
        /// Returns the PID of the current process. 
        /// </summary>
        /// <returns>The PID.</returns>
        /// <remarks>
        /// The current thread ID instead of process ID - 
        /// oftenly used to get a unique ID of the current "request" 
        /// but PID is always the same in .NET. 
        /// In this way we get different values for different requests and also we don't expose system process ID
        /// </remarks>
        public static int getmypid()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
            //return System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        /// <summary>
        /// Return the process identifier of the current process.
        /// </summary>
        /// <remarks>
        /// The current thread ID instead of process ID - 
        /// oftenly used to get a unique ID of the current "request" 
        /// but PID is always the same in .NET. 
        /// In this way we get different values for different requests and also we don't expose system process ID
        /// </remarks>
        public static int posix_getpid()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
            //return System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        /// <summary>
        /// Gets time of last page modification. 
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <returns>The UNIX timestamp or -1 on error.</returns>
        [return: CastToFalse]
        public static long getlastmod(Context ctx)
        {
            try
            {
                var file = ctx.MainScriptFile.Path;
                if (file == null)
                {
                    return -1;
                }

                return DateTimeUtils.UtcToUnixTimeStamp(System.IO.File.GetLastWriteTime(System.IO.Path.Combine(ctx.RootPath, file)).ToUniversalTime());
            }
            catch (System.Exception)
            {
                return -1;
            }
        }


        /// <summary>
        /// Gets the name of the current user.
        /// </summary>
        /// <returns>The name of the current user.</returns>
        public static string get_current_user() => System.Environment.UserName;

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <returns>Zero.</returns>
        [return: CastToFalse]
        public static int getmyuid()
        {
            PhpException.FunctionNotSupported("getmyuid");
            return -1;
        }

        #endregion

        #region get_required_files, get_included_files

        /// <summary>
        /// Returns an array of included file paths.
        /// </summary>
        /// <returns>The array of paths to included files (without duplicates).</returns>
        public static PhpArray get_required_files(Context ctx) => get_included_files(ctx);

        /// <summary>
        /// Returns an array of included file paths.
        /// </summary>
        /// <returns>The array of paths to included files (without duplicates).</returns>
        public static PhpArray get_included_files(Context ctx)
        {
            var result = new PhpArray();

            foreach (var script in ctx.GetIncludedScripts())
            {
                result.Add((PhpValue)System.IO.Path.GetFullPath(System.IO.Path.Combine(ctx.RootPath, script.Path)));
            }

            //
            return result;
        }

        #endregion

        /// <summary>
        /// This function flushes all response data to the client and finishes the request.
        /// This allows for time consuming tasks to be performed without leaving the connection to the client open.
        /// </summary>
        public static bool fastcgi_finish_request(Context ctx)
        {
            var webctx = ctx.HttpPhpContext;
            if (webctx != null)
            {
                webctx.Flush();

                // TODO: finish the request

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool gc_enabled()
        {
            return true;    // status of the circular reference collector
        }

        /// <summary>
        /// Forces collection of any existing garbage cycles.
        /// </summary>
        public static void gc_collect_cycles() => GC.Collect();

        /// <summary>Ignored.</summary>
        public static void gc_enable() { }

        /// <summary>Ignored.</summary>
        public static void gc_disable() { }

        /// <summary>Ignored.</summary>
        public static int gc_mem_caches() => 0;

        /// <summary>
        /// Returns a unique identifier for the current thread.
        /// </summary>
        public static int zend_thread_id() => System.Threading.Thread.CurrentThread.ManagedThreadId;

        #region usleep, sleep

        /// <summary>
        /// Delay execution in microseconds.
        /// </summary>
        public static void usleep(long microseconds)
        {
            if (microseconds < 0) throw new ArgumentOutOfRangeException();
            System.Threading.Thread.Sleep((int)(microseconds / 1000L));
        }

        /// <summary>
        /// Delay execution.
        /// </summary>
        //[return: CastToFalse]
        public static int sleep(long seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException();
            System.Threading.Thread.Sleep((int)(seconds * 1000L));
            return 0;
        }

        /// <summary>
        /// Delay for a number of seconds and nanoseconds.
        /// </summary>
        //[return: CastToFalse]
        public static bool time_nanosleep(long seconds, long nanoseconds)
        {
            if (seconds < 0 || nanoseconds < 0) throw new ArgumentOutOfRangeException();
            System.Threading.Thread.Sleep((int)(seconds * 1000L + nanoseconds / 1000000L));
            return true;
        }

        /// <summary>
        /// Makes the script sleep until the specified <paramref name="timestamp"/>.
        /// </summary>
        /// <param name="timestamp">The timestamp when the script should wake.</param>
        /// <returns>Returns <c>TRUE</c> on success or <c>FALSE</c> on failure.</returns>
        /// <exception cref="PhpException">If the specified timestamp is in the past, this function will generate a <c>E_WARNING</c>.</exception>
        public static bool time_sleep_until(double timestamp)
        {
            var now = (System.DateTime.UtcNow - DateTimeUtils.UtcStartOfUnixEpoch).TotalSeconds;    // see microtime(TRUE)
            var sleep_ms = (timestamp - now) * 1000.0;

            if (sleep_ms > 0.0)
            {
                System.Threading.Thread.Sleep((int)sleep_ms);
                return true;
            }
            else
            {
                // note: php throws warning even if time is current time

                PhpException.Throw(PhpError.Warning, Resources.LibResources.time_sleep_until_in_past);
                return false;
            }
        }

        #endregion
    }
}
