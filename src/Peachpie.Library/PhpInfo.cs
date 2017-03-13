using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Pchp.Library
{
    /// <summary>
    /// Phpinfo library
    /// </summary>
    public static partial class PhpInfo
    {
        [PhpHidden, Flags]
        public enum PhpInfoWhat
        {
            /// <summary>
            /// The configuration line, php.ini location, build date, Web Server, System and more
            /// </summary>
            INFO_GENERAL = 1,
            /// <summary>
            /// PHP Credits. See also phpcredits().
            /// </summary>
            INFO_CREDITS = 2,
            /// <summary>
            /// Current Local and Master values for PHP directives. See also ini_get().
            /// </summary>
            INFO_CONFIGURATION = 4,
            /// <summary>
            /// Loaded modules and their respective settings. See also get_loaded_extensions().
            /// </summary>
            INFO_MODULES = 8,
            /// <summary>
            /// Environment Variable information that's also available in $_ENV.
            /// </summary>
            INFO_ENVIRONMENT = 16,
            /// <summary>
            /// Shows all predefined variables from EGPCS (Environment, GET, POST, Cookie, Server).
            /// </summary>
            INFO_VARIABLES = 32,
            /// <summary>
            /// PHP License information. See also the » license FAQ.
            /// </summary>
            INFO_LICENSE = 64,
            /// <summary>
            /// Shows all of the above
            /// </summary>
            INFO_ALL = -1,
        }
        /// <summary>
        /// The configuration line, php.ini location, build date, Web Server, System and more
        /// </summary>
        public const int INFO_GENERAL = (int)PhpInfoWhat.INFO_GENERAL;
        /// <summary>
        /// PHP Credits. See also phpcredits().
        /// </summary>
        public const int INFO_CREDITS = (int)PhpInfoWhat.INFO_CREDITS;
        /// <summary>
        /// Current Local and Master values for PHP directives. See also ini_get().
        /// </summary>
        public const int INFO_CONFIGURATION = (int)PhpInfoWhat.INFO_CONFIGURATION;
        /// <summary>
        /// Loaded modules and their respective settings. See also get_loaded_extensions().
        /// </summary>
        public const int INFO_MODULES = (int)PhpInfoWhat.INFO_MODULES;
        /// <summary>
        /// Environment Variable information that's also available in $_ENV.
        /// </summary>
        public const int INFO_ENVIRONMENT = (int)PhpInfoWhat.INFO_ENVIRONMENT;
        /// <summary>
        /// Shows all predefined variables from EGPCS (Environment, GET, POST, Cookie, Server).
        /// </summary>
        public const int INFO_VARIABLES = (int)PhpInfoWhat.INFO_VARIABLES;
        /// <summary>
        /// PHP License information. See also the » license FAQ.
        /// </summary>
        public const int INFO_LICENSE = (int)PhpInfoWhat.INFO_LICENSE;
        /// <summary>
        /// Shows all of the above
        /// </summary>
        public const int INFO_ALL = (int)PhpInfoWhat.INFO_ALL;

        /// <summary>
        /// Outputs information about PHP's configuration
        /// </summary>
        /// <param name="ctx">The php context.</param>
        /// <param name="what">
        /// The output may be customized by passing one or more of the following constants bitwise values summed together in the optional <paramref name="what"/> parameter.
        /// One can also combine the respective constants or bitwise values together with the or operator.</param>
        public static bool phpinfo(Context ctx, PhpInfoWhat what = PhpInfoWhat.INFO_ALL)
        {
            // TODO: ctx.IsWebApplication == false => text output
            // TODO: 'HtmlTagWriter' -> 'PhpInfoWriter', two implementations of PhpInfoWriter: "HtmlInfoWriter", "TextInfoWriter"

            // TODO: Localize

            ctx.Echo(@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""DTD/xhtml1-transitional.dtd"">");
            using (var html = ctx.Tag("html", new { xmlns = "http://www.w3.org/1999/xhtml" }))
            {
                using (var head = html.Tag("head"))
                {
                    using (var style = head.Tag("style", new { type = "text/css" }))
                    {
                        style.EchoRaw(Resources.InfoResources.Style);
                    }
                    head.EchoTag("title", "phpinfo()");
                    head.EchoTagSelf("meta", new { name = "ROBOTS", content = "NOINDEX,NOFOLLOW,NOARCHIVE" });
                }
                using (var body = html.Tag("body"))
                using (var center = body.Tag("div", new { @class = "center" }))
                {
                    PageTitle(center);

                    if ((what & PhpInfoWhat.INFO_GENERAL) != 0)
                    {
                        General(center);
                    }
                    if ((what & PhpInfoWhat.INFO_CONFIGURATION) != 0)
                    {
                        Configuration(center, ctx);
                    }
                    if ((what & PhpInfoWhat.INFO_ENVIRONMENT) != 0)
                    {
                        Env(center);
                    }
                    if ((what & PhpInfoWhat.INFO_VARIABLES) != 0)
                    {
                        Variables(center);
                    }
                    if ((what & PhpInfoWhat.INFO_CREDITS) != 0)
                    {
                        Credits(center);
                    }
                }
            }

            //
            return true;
        }

        private static void PageTitle(HtmlTagWriter container)
        {
            using (var table = container.Tag("table"))
            using (var tr = table.Tag("tr", new { @class = "h" }))
            using (var td = tr.Tag("td"))
            {
                using (var a = td.Tag("a", new { href = Resources.InfoResources.LogoHref, target = "_blank" }))
                {
                    a.EchoTagSelf("img", new { border = "0", src = Resources.InfoResources.LogoSrc, alt = Resources.InfoResources.LogoAlt });
                }
                using (var title = td.Tag("h1", new { @class = "p" }))
                {
                    title.EchoEscaped("Peachpie Version " + typeof(Context).GetTypeInfo().Assembly.GetName().Version.ToString(3));  // TODO: suffix (-preview...)
                }
            }
        }

        private static void General(HtmlTagWriter container)
        {
            using (var table = container.Tag("table"))
            {
                Action<string, string> Line = (name, value) =>
                {
                    using (var tr = table.Tag("tr"))
                    {
                        tr.EchoTag("td", name, new { @class = "e" });
                        tr.EchoTag("td", value, new { @class = "v" });
                    }
                };

                Line("System", $"{GetOsName()} {System.Environment.MachineName} {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}");
                Line("Architecture", RuntimeInformation.ProcessArchitecture.ToString());
                Line("Debug build",
#if DEBUG
                    true
#else
                    false
#endif
                    ? "yes" : "no");
                Line("IPv6 Support", System.Net.Sockets.Socket.OSSupportsIPv6 ? "yes" : "no");
                Line("Registered PHP Streams", string.Join(", ", Streams.StreamWrapper.SystemStreamWrappers.Keys));
                Line("Registered Stream Filters", string.Join(", ", Streams.PhpFilter.GetFilterNames()));
            }
        }

        private static string GetOsName()
        {
            foreach (var osDesc in typeof(OSPlatform).GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(OSPlatform)).Select(p => new { Prop = p, Val = (OSPlatform)p.GetValue(null) }))
            {
                if (RuntimeInformation.IsOSPlatform(osDesc.Val))
                {
                    return osDesc.Val.ToString();
                }
            }
            return "Unknown";
        }

        private static void Configuration(HtmlTagWriter container, Context ctx)
        {
            container.EchoTag("h1", "Configuration");

            var extensions = Context.GetLoadedExtensions();
            foreach (string ext in extensions)
            {
                container.EchoTag("h2", ext);
            }

            // TODO: extensions configuration
            var options = StandardPhpOptions.DumpOptions(ctx, null);
            foreach (var extensionopt in options.GroupBy(opt => opt.ExtensionName))
            {

            }
        }

        private static void Env(HtmlTagWriter container)
        {
            container.EchoTag("h2", "Environment");
            using (var table = container.Tag("table"))
            {
                using (var tr = table.Tag("tr", new { @class = "h" }))
                {
                    tr.EchoTag("th", "Variable");
                    tr.EchoTag("th", "Value");
                }

                Action<string, string> Line = (name, value) =>
                {
                    using (var tr = table.Tag("tr", new { @class = "h" }))
                    {
                        tr.EchoTag("td", name, new { @class = "e" });
                        using (var td = tr.Tag("td", new { @class = "v" }))
                        {
                            if (string.IsNullOrEmpty(value))
                            {
                                td.EchoRaw("&nbsp;");
                            }
                            else
                            {
                                td.EchoEscaped(value);
                            }
                        }
                    }
                };

                foreach (var entry in container.Context.Env.Keys)
                {
                    Line(entry.ToString(), container.Context.Env[entry].ToStringOrNull());
                }
            }
        }

        private static void Variables(HtmlTagWriter container)
        {
            container.EchoTag("h2", "PHP Variables");
            using (var table = container.Tag("table"))
            {
                using (var tr = table.Tag("tr", new { @class = "h" }))
                {
                    tr.EchoTag("th", "Variable");
                    tr.EchoTag("th", "Value");
                }

                Action<string, string> Line = (name, value) =>
                {
                    using (var tr = table.Tag("tr", new { @class = "h" }))
                    {
                        tr.EchoTag("td", name, new { @class = "e" });
                        using (var td = tr.Tag("td", new { @class = "v" }))
                        {
                            if (string.IsNullOrEmpty(value))
                            {
                                td.EchoRaw("&nbsp;");
                            }
                            else
                            {
                                td.EchoEscaped(value);
                            }
                        }
                    }
                };

                Action<PhpArray, string> DumpArray = (arr, name) =>
                {
                    foreach (var entry in arr.Keys)
                    {
                        Line($"{name}[{entry}]", arr[entry].ToStringOrNull());
                    }
                };

                DumpArray(container.Context.Cookie, "_COOKIE");
                DumpArray(container.Context.Server, "_SERVER");
            }
        }

        private static void Credits(HtmlTagWriter container)
        {
            container.EchoTag("h1", "Credits");
            
            // TODO: creditz, can we pull it from git?
        }
    }
}
