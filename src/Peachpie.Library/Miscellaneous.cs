using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Pchp.Library
{
    public static class Miscellaneous
    {
        // [return: CastToFalse] // once $extension will be supported
        public static string phpversion(string extension = null)
        {
            if (extension != null)
            {
                throw new NotImplementedException(nameof(extension));
            }

            return Environment.PHP_MAJOR_VERSION + "." + Environment.PHP_MINOR_VERSION + "." + Environment.PHP_RELEASE_VERSION;
        }

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
        public static PhpArray get_extension_funcs(string extension)
        {
            var result = new PhpArray();
            foreach (var e in Context.GetRoutinesByExtension(extension))
            {
                result.Add(PhpValue.Create(e));
            }

            // gets NULL (FALSE) if there are no functions
            return result.Count != 0 ? result : null;
        }

        #region gethostname, php_uname, memory_get_usage, php_sapi_name

        /// <summary>
        /// gethostname() gets the standard host name for the local machine. 
        /// </summary>
        /// <returns>Returns a string with the hostname on success, otherwise FALSE is returned. </returns>
        [return: CastToFalse]
        public static string gethostname()
        {
            string host = null;

            try { host = System.Net.Dns.GetHostName(); }
            catch { }

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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) system = "Windows NT";
            //else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) system = "OSX";
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

        ///// <summary>
        ///// Retrieves the size of the current process working set in bytes.
        ///// </summary>
        ///// <param name="real_usage">
        ///// "Set this to TRUE to get the real size of memory allocated from system.
        ///// If not set or FALSE only the memory used by emalloc() is reported."</param>
        ///// <returns>The size.</returns>
        //public static long memory_get_usage(bool real_usage = false)
        //{
        //    //if (real_usage == false)// TODO: real_usage = false
        //    //    PhpException.ArgumentValueNotSupported("real_usage");

        //    long ws = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
        //    if (ws > Int32.MaxValue) return Int32.MaxValue;
        //    return (int)ws;
        //}

        ///// <summary>
        ///// Returns the peak of memory, in bytes, that's been allocated to the PHP script.
        ///// </summary>
        ///// <param name="real_usage">
        ///// Set this to TRUE to get the real size of memory allocated from system.
        ///// If not set or FALSE only the memory used by emalloc() is reported.</param>
        ///// <returns>The size.</returns>
        //public static long memory_get_peak_usage(bool real_usage = false)
        //{
        //    //if (real_usage == false)// TODO: real_usage = false
        //    //    PhpException.ArgumentValueNotSupported("real_usage");

        //    long ws = System.Diagnostics.Process.GetCurrentProcess().NonpagedSystemMemorySize64;    // can't get current thread's memory
        //    if (ws > Int32.MaxValue) return Int32.MaxValue;
        //    return (int)ws;
        //}

        /// <summary>
        /// Returns the type of interface between web server and Phalanger. 
        /// </summary>
        /// <returns>The "isapi" string if runned under webserver (ASP.NET works via ISAPI) or "cli" otherwise.</returns>
        public static string php_sapi_name(Context ctx)
        {
            return (ctx.IsWebApplication) ? "isapi" : "cli";    // TODO: Context.SapiName
        }

        #endregion

        #region getmypid, getlastmod, get_current_user, getmyuid

        /// <summary>
        /// Returns the PID of the current process. 
        /// </summary>
        /// <returns>The PID.</returns>
        public static int getmypid()
        {
            // return System.Diagnostics.Process.GetCurrentProcess().Id;
            throw new NotImplementedException("System.Diagnostics.Process");
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
        public static string get_current_user()
        {
            throw new NotImplementedException();
        }

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

        #region set_time_limit, ignore_user_abort

        /// <summary>
        /// Sets the request time-out in seconds (configuration option "max_execution_time").
        /// No value is returned.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="seconds">The time-out setting for request.</param>
        public static void set_time_limit(Context ctx, int seconds)
        {
            //ctx.ApplyExecutionTimeout(seconds);
        }


        /// <summary>
        /// Get a value of a configuration option "ignore_user_abort".
        /// </summary>
        /// <returns>The current value of the option.</returns>
        public static bool ignore_user_abort(Context ctx)
        {
            return ctx.Configuration.Core.IgnoreUserAbort;
        }

        /// <summary>
        /// Sets a value of a configuration option "ignore_user_abort".
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="value">The new value of the option.</param>
        /// <returns>The previous value of the option.</returns>
        /// <exception cref="PhpException">Web request PHP context is not available (Warning).</exception>
        public static bool ignore_user_abort(Context ctx, bool value)
        {
            if (!ctx.IsWebApplication) return true;

            bool result = ctx.Configuration.Core.IgnoreUserAbort;
            ctx.Configuration.Core.IgnoreUserAbort = value;

            //// enables/disables disconnection tracking:
            //ctx.TrackClientDisconnection = !value;

            return result;
        }

        #endregion

        public static bool gc_enabled()
        {
            return true;    // status of the circular reference collector
        }
    }
}
