using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Library
{
    /// <summary>
    /// Manages association of PHP option names and flags.
    /// </summary>
    [PhpHidden]
    public static class StandardPhpOptions
    {
        public delegate PhpValue GetSetDelegate(IPhpConfigurationService config, string option, PhpValue value, IniAction action);

        /// <summary>
        /// An action which can be performed on option.
        /// </summary>
        public enum IniAction
        {
            Set, Get
        }

        public static bool IsGet(this IniAction action) => action == IniAction.Get;

        public static bool IsSet(this IniAction action) => action == IniAction.Set;

        [Flags]
        public enum IniFlags : byte
        {
            Unsupported = 0,
            Global = 0,

            Supported = 1,
            Local = 2,
            Http = 4,
        }

        [Flags]
        public enum IniAccessability
        {
            User = 1,
            PerDirectory = 2,
            System = 4,
            All = 7,

            Global = PerDirectory | System,
            Local = All
        }

        /// <summary>
        /// Holds information about the standard PHP option.
        /// </summary>
        public struct OptionDefinition
        {
            public readonly IniFlags Flags;
            public readonly GetSetDelegate Gsr;
            public readonly string Extension;

            internal OptionDefinition(IniFlags flags, GetSetDelegate gsr, string extension)
            {
                this.Flags = flags;
                this.Gsr = gsr;
                this.Extension = extension;
            }
        }

        /// <summary>
        /// Provides state, definition and values of an option.
        /// </summary>
        public struct OptionDump
        {
            /// <summary>
            /// Option PHP name.
            /// </summary>
            public string Name;

            /// <summary>
            /// Corresponding extension name.
            /// </summary>
            public string ExtensionName => Definition.Extension;

            /// <summary>
            /// Internal option definition.
            /// </summary>
            public OptionDefinition Definition;

            /// <summary>
            /// Actual option value.
            /// </summary>
            public PhpValue
                LocalValue,
                DefaultValue;
        }

        static readonly GetSetDelegate EmptyGsr = new GetSetDelegate((s, name, value, action) => PhpValue.Null);

        static Dictionary<string, OptionDefinition> _options = new Dictionary<string, OptionDefinition>(150, StringComparer.Ordinal);

        /// <summary>
        /// Gets a number of registered options.
        /// </summary>
        public static int Count => _options.Count;

        static StandardPhpOptions()
        {
            RegisterStandard();
        }

        static void RegisterStandard()
        {
            Register("allow_call_time_pass_reference", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("allow_url_fopen", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("allow_webdav_methods", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("always_populate_raw_post_data", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("arg_separator.input", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("arg_separator.output", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("asp_tags", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("assert.active", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("assert.bail", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("assert.callback", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("assert.quiet_eval", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("assert.warning", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("async_send", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("auto_append_file", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("auto_detect_line_endings", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("auto_prepend_file", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("browscap", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("cgi.force_redirect", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("cgi.redirect_status_env", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("cgi.rfc2616_headers", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("child_terminate", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("debugger.enabled", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("debugger.host", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("debugger.port", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("default_charset", IniFlags.Supported | IniFlags.Local | IniFlags.Http, EmptyGsr);
            Register("default_mimetype", IniFlags.Supported | IniFlags.Local | IniFlags.Http, EmptyGsr);
            Register("default_socket_timeout", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("define_syslog_variables", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("disable_classes", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("disable_functions", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("display_errors", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("display_startup_errors", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("doc_root", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("enable_dl", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("engine", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("error_append_string", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("error_log", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("error_prepend_string", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("error_reporting", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("expose_php", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("extension_dir", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("fastcgi.impersonate", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("file_uploads", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("from", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("gpc_order", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("html_errors", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("ignore_repeated_errors", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("ignore_repeated_source", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("ignore_user_abort", IniFlags.Supported | IniFlags.Local | IniFlags.Http, EmptyGsr);
            Register("implicit_flush", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("include_path", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("last_modified", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("log_errors", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("log_errors_max_len", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("magic_quotes_gpc", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("magic_quotes_runtime", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("magic_quotes_sybase", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("max_execution_time", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("max_input_time", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("memory_limit", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("mime_magic.magicfile", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("open_basedir", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("output_buffering", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("output_handler", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("post_max_size", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("precision", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("register_argc_argv", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("register_globals", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("register_long_arrays", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("report_memleaks", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("safe_mode", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("safe_mode_allowed_env_vars", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("safe_mode_exec_dir", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("safe_mode_gid", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("safe_mode_include_dir", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("safe_mode_protected_env_vars", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("session.auto_start", IniFlags.Supported | IniFlags.Global | IniFlags.Http, EmptyGsr);
            Register("session.save_handler", IniFlags.Supported | IniFlags.Local | IniFlags.Http, EmptyGsr);
            Register("session.name", IniFlags.Supported | IniFlags.Global | IniFlags.Http, EmptyGsr);
            Register("short_open_tag", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("sql.safe_mode", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("track_errors", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("unserialize_callback_func", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("upload_max_filesize", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("upload_tmp_dir", IniFlags.Supported | IniFlags.Global, EmptyGsr);
            Register("url_rewriter.tags", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("user_agent", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("user_dir", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("variables_order", IniFlags.Supported | IniFlags.Local, EmptyGsr);
            Register("warn_plus_overloading", IniFlags.Unsupported | IniFlags.Global, EmptyGsr);
            Register("xbithack", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("y2k_compliance", IniFlags.Unsupported | IniFlags.Local, EmptyGsr);
            Register("zend.ze1_compatibility_mode", IniFlags.Supported | IniFlags.Local, EmptyGsr);
        }

        /// <summary>
        /// Registeres a standard configuration option.
        /// Not thread safe.
        /// </summary>
        /// <param name="name">A case-sensitive unique option name.</param>
        /// <param name="flags">Flags.</param>
        /// <param name="gsr">A delegate pointing to a method which will perform option's value getting, setting, and restoring.</param>
        /// <param name="extension">A case-sensitive name of the extension which the option belongs to. Can be a <B>null</B> reference.</param>
        /// <remarks>
        /// Registered options are known to <c>ini_get</c>, <c>ini_set</c>, and <c>ini_restore</c> PHP functions.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is a <B>null</B> reference.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="gsr"/> is a <B>null</B> reference.</exception>
        /// <exception cref="ArgumentException">An option with specified name has already been registered.</exception>
        public static void Register(string name, IniFlags flags, GetSetDelegate gsr, string extension = null)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (gsr == null) throw new ArgumentNullException(nameof(gsr));

            _options.Add(name, new OptionDefinition(flags, gsr, extension));
        }

        public static OptionDefinition GetOption(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            OptionDefinition value;
            return _options.TryGetValue(name, out value) ? value : default(OptionDefinition);
        }

        /// <summary>
		/// Tries to get or set an option given its PHP name and value.
		/// </summary>
        /// <param name="config">Configuration to be read or set.</param>
		/// <param name="name">The option name.</param>
		/// <param name="value">The option new value if applicable.</param>
		/// <param name="action">The action to be taken.</param>
		/// <param name="error"><B>true</B>, on failure.</param>
		/// <returns>The option old value.</returns>
		/// <exception cref="PhpException">The option not supported (Warning).</exception>
		/// <exception cref="PhpException">The option is read only but action demands write access (Warning).</exception>
		internal static PhpValue TryGetSet(IPhpConfigurationService config, string name, PhpValue value, IniAction action, out bool error)
        {
            Debug.Assert(name != null);
            error = true;

            var def = GetOption(name);

            // option not found:
            if (def.Gsr == null)
            {
                PhpException.Throw(PhpError.Warning, string.Format(Resources.LibResources.unknown_option, name));
                return PhpValue.Null;
            }

            // the option is known but not supported:
            if ((def.Flags & IniFlags.Supported) == 0)
            {
                PhpException.Throw(PhpError.Warning, string.Format(Resources.LibResources.option_not_supported, name));
                return PhpValue.Null;
            }

            // the option is global thus cannot be changed:
            if ((def.Flags & IniFlags.Local) == 0 && action != IniAction.Get)
            {
                PhpException.Throw(PhpError.Warning, string.Format(Resources.LibResources.option_readonly, name));
                return PhpValue.Null;
            }

            error = false;
            return def.Gsr(config, name, value, action);
        }

        /// <summary>
		/// Formats a state of the specified option into <see cref="PhpArray"/>.
		/// </summary>
		/// <param name="flags">The option's flag.</param>
		/// <param name="defaultValue">A default value of the option.</param>
		/// <param name="localValue">A script local value of the option.</param>
		/// <returns>An array containig keys <c>"global_value"</c>, <c>"local_value"</c>, <c>"access"</c>.</returns>
		public static PhpArray FormatOptionState(IniFlags flags, PhpValue defaultValue, PhpValue localValue)
        {
            var result = new PhpArray(3);

            result.Add("global_value", defaultValue);
            result.Add("local_value", localValue);
            result.Add("access", (int)((flags & IniFlags.Local) != 0 ? IniAccessability.Local : IniAccessability.Global));

            return result;
        }

        /// <summary>
		/// Gets an array of options states formatted by <see cref="FormatOptionState"/>.
		/// </summary>
        /// <param name="ctx">Current runtime context.</param>
		/// <param name="extension">An extension which options to retrieve.</param>
		/// <param name="result">A dictionary where to add options.</param>
		/// <returns>An array of option states.</returns>
		/// <remarks>Options already contained in <paramref name="result"/> are overwritten.</remarks>
		internal static PhpArray GetAllOptionStates(Context ctx, string extension, PhpArray result)
        {
            Debug.Assert(ctx != null && ctx.Configuration != null);
            Debug.Assert(result != null);

            foreach (var opt in DumpOptions(ctx, extension))
            {
                result[opt.Name] = (PhpValue)FormatOptionState(opt.Definition.Flags, opt.DefaultValue, opt.LocalValue);
            }

            //
            return result;
        }

        /// <summary>
        /// Enumerates all registered options (optionally restricted by the <paramref name="extension"/> name), their definition and local and global values.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="extension">Optional. Extension name restriction.</param>
        public static IEnumerable<OptionDump> DumpOptions(Context ctx, string extension = null)
        {
            var config = ctx.Configuration;

            foreach (var entry in _options)
            {
                var name = entry.Key;
                var def = entry.Value;

                // skips configuration which don't belong to the specified extension:
                if ((extension == null || extension.Equals(def.Extension, StringComparison.Ordinal)))
                {
                    var opt = new OptionDump() { Name = name, Definition = def };

                    if ((def.Flags & IniFlags.Supported) == 0)
                    {
                        opt.LocalValue = opt.DefaultValue = (PhpValue)"Not Supported";
                    }
                    else if ((def.Flags & IniFlags.Http) != 0 && !ctx.IsWebApplication)
                    {
                        opt.LocalValue = opt.DefaultValue = (PhpValue)"Http Context Required";
                    }
                    else
                    {
                        opt.DefaultValue = def.Gsr(config.Parent, name, PhpValue.Null, IniAction.Get);
                        opt.LocalValue = def.Gsr(config, name, PhpValue.Null, IniAction.Get);
                    }

                    yield return opt;
                }
            }
        }

        #region GetSet

        /// <summary>
        /// Gets or sets option.
        /// </summary>
        public static bool GetSet(ref bool option, bool defaultValue, PhpValue newValue, IniAction action)
        {
            var oldValue = option;

            if (action == IniAction.Set)
            {
                option = newValue.ToBoolean();
            }

            return oldValue;
        }

        /// <summary>
		/// Gets or sets option.
		/// </summary>
		public static int GetSet(ref int option, int defaultValue, PhpValue newValue, IniAction action)
        {
            var oldValue = option;

            if (action == IniAction.Set)
            {
                option = (int)newValue.ToLong();
            }

            return oldValue;
        }

        /// <summary>
		/// Gets or sets option.
		/// </summary>
		public static double GetSet(ref double option, int defaultValue, PhpValue newValue, IniAction action)
        {
            var oldValue = option;

            if (action == IniAction.Set)
            {
                option = (int)newValue.ToDouble();
            }

            return oldValue;
        }

        /// <summary>
		/// Gets or sets option.
		/// </summary>
		public static string GetSet(ref string option, string defaultValue, PhpValue newValue, IniAction action)
        {
            var oldValue = option;

            if (action == IniAction.Set)
            {
                option = newValue.AsString();
            }

            return oldValue;
        }

        /// <summary>
		/// Gets or sets option.
		/// </summary>
		public static IPhpCallable GetSet(ref IPhpCallable option, IPhpCallable defaultValue, PhpValue newValue, IniAction action)
        {
            var oldValue = option;

            if (action == IniAction.Set)
            {
                option = newValue.AsCallable();
            }

            return oldValue;
        }

        #endregion
    }

    public static class Options
    {
        #region ini_get, ini_set, ini_restore, get_cfg_var, ini_alter, ini_get_all

        /// <summary>
        /// Gets the value of a configuration option.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        /// <returns>The option old value conveted to string or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public static string ini_get(Context ctx, string option)
        {
            bool error;
            var result = StandardPhpOptions.TryGetSet(ctx.Configuration, option, PhpValue.Void, StandardPhpOptions.IniAction.Get, out error);
            if (error)
            {
                return null;
            }
            else
            {
                return result.ToString(ctx);
            }
        }

        /// <summary>
        /// Sets the value of a configuration option.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        /// <param name="value">The option new value.</param>
        /// <returns>The option old value converted to string or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public static string ini_set(Context ctx, string option, PhpValue value)
        {
            bool error;
            var old = StandardPhpOptions.TryGetSet(ctx.Configuration, option, PhpValue.Void, StandardPhpOptions.IniAction.Set, out error);
            if (error)
            {
                return null;
            }
            else
            {
                return old.ToString(ctx);
            }
        }

        /// <summary>
        /// Restores the value of a configuration option to its global value.
        /// No value is returned.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        public static void ini_restore(Context ctx, string option)
        {
            var def = StandardPhpOptions.GetOption(option);
            if (def.Gsr != null)
            {
                var @default = def.Gsr(ctx.Configuration.Parent, option, PhpValue.Null, StandardPhpOptions.IniAction.Get);
                def.Gsr(ctx.Configuration, option, @default, StandardPhpOptions.IniAction.Set);
            }
            else
            {
                // TODO: Err, see TryGetSet() errors
                throw new ArgumentException("option_not_supported");
            }
        }

        /// <summary>
        /// Gets the value of a configuration option (alias for <see cref="Get"/>).
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        /// <returns>The option old value conveted to string or <B>false</B> on error.</returns>
        [return: CastToFalse]
        public static string get_cfg_var(Context ctx, string option) => ini_get(ctx, option);

        /// <summary>
        /// Sets the value of a configuration option (alias for <see cref="Set"/>).
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="option">The option name (case sensitive).</param>
        /// <param name="value">The option new value converted to string.</param>
        /// <returns>The option old value.</returns>
        [return: CastToFalse]
        public static string ini_alter(Context ctx, string option, PhpValue value) => ini_set(ctx, option, value);

        /// <summary>
        /// Retrieves an array of configuration entries of a specified extension.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="extension">Optional. The PHP internal extension name.</param>
        /// <param name="details">Retrieve details settings or only the current value for each setting. Default is TRUE.</param>
        /// <remarks>
        /// For each supported configuration option an entry is added to the resulting array.
        /// The key is the name of the option and the value is an array having three entries: 
        /// <list type="bullet">
        ///   <item><c>global_value</c> - global value of the option</item>
        ///   <item><c>local_value</c> - local value of the option</item>
        ///   <item><c>access</c> - 7 (PHP_INI_ALL), 6 (PHP_INI_PERDIR | PHP_INI_SYSTEM) or 4 (PHP_INI_SYSTEM)</item>
        /// </list>
        /// </remarks>
        public static PhpArray ini_get_all(Context ctx, string extension = null, bool details = true)
        {
            return StandardPhpOptions.GetAllOptionStates(ctx, extension, new PhpArray());
        }

        #endregion

        /// <summary>
        /// Gets the current configuration setting of magic_quotes_gpc.
        /// Always returns <c>false</c>.
        /// </summary>
        /// <returns>Always <c>false</c>.</returns>
        public static bool get_magic_quotes_gpc() => false;
    }
}
