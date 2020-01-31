using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Pchp.Core;
using Pchp.Library.Resources;

namespace Pchp.Library
{
    /// <summary>
    /// Phpinfo library
    /// </summary>
    [PhpExtension("standard")]
    public static class PhpInfo
    {
        #region enum PhpInfoWhat

        [PhpHidden, Flags]
        public enum PhpInfoWhat : long
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
            INFO_ALL = 0xffffffff,
        }

        #endregion

        #region Constants

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
        public const long INFO_ALL = (long)PhpInfoWhat.INFO_ALL;

        #endregion

        #region InfoWriter

        abstract class InfoWriter : IDisposable
        {
            readonly protected TextWriter _output;

            protected InfoWriter(TextWriter output)
            {
                _output = output ?? throw new ArgumentNullException(nameof(output));

                BeginInfo();
            }

            /// <summary>
            /// The info header.
            /// </summary>
            protected virtual void BeginInfo() { }

            /// <summary>
            /// The info footer.
            /// </summary>
            protected virtual void EndInfo() { }

            /// <summary>
            /// A table header without header row.
            /// </summary>
            public virtual void BeginTable(int tableColumns)
            {

            }

            /// <summary>
            /// A table header.
            /// </summary>
            public virtual void BeginTable(params string[] header)
            {

            }

            /// <summary>
            /// A table footer.
            /// </summary>
            public virtual void EndTable()
            {

            }

            /// <summary>
            /// A table row.
            /// </summary>
            public abstract void TableRow(params string[] values);

            public void Table(Context ctx, IDictionary<IntStringKey, PhpValue> dict, string[] header)
            {
                Table(
                    dict.Select(pair => new[] { pair.Key.ToString(), Export(ctx, pair.Value) }),
                    header);
            }

            public void Table(IEnumerable<string[]> rows, string[] header = null)
            {
                using (var enumerator = rows.GetEnumerator())
                {
                    bool opened = false;

                    if (header != null)
                    {
                        BeginTable(header);
                        opened = true;
                    }

                    while (enumerator.MoveNext())
                    {
                        if (!opened)
                        {
                            opened = true;
                            BeginTable(enumerator.Current.Length);
                        }

                        TableRow(enumerator.Current);
                    }

                    //
                    if (opened)
                    {
                        EndTable();
                    }
                }
            }

            /// <summary>
            /// A header string.
            /// </summary>
            public abstract void Header(string header);

            /// <summary>
            /// A legal notice with optional image.
            /// </summary>
            public abstract void LegalNotice(string notice, string logo_img, string logo_url, string logo_text);

            void IDisposable.Dispose()
            {
                EndInfo();
            }
        }

        sealed class HtmlInfoWriter : InfoWriter
        {
            public HtmlInfoWriter(TextWriter output) : base(output)
            {
            }
            protected override void BeginInfo()
            {
                _output.WriteLine(@"
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">");
                _output.WriteLine("<head>");
                _output.WriteLine(InfoResources.Style);
                _output.WriteLine(@"<title>phpinfo()</title><meta name=""ROBOTS"" content=""NOINDEX, NOFOLLOW, NOARCHIVE"" />");
                _output.WriteLine("</head>");
                _output.WriteLine(@"<body><div class=""center"">");
                LegalNotice($"{InfoResources.Peachpie} {InfoResources.Version} " + Core.Utilities.ContextExtensions.GetRuntimeInformationalVersion(),
                    InfoResources.LogoSrc,
                    InfoResources.LogoHref,
                    InfoResources.LogoAlt);
            }

            protected override void EndInfo()
            {
                _output.WriteLine(@"</div></body></html>");
            }

            public override void BeginTable(int tableColumns)
            {
                _output.WriteLine("<table>");
            }

            public override void BeginTable(params string[] header)
            {
                BeginTable(header.Length);

                _output.Write("<tr class=\"h\">");
                foreach (var h in header) _output.Write($"<th>{WebUtility.HtmlEncode(h)}</th>");
                _output.WriteLine("</tr>");
            }

            public override void EndTable()
            {
                _output.WriteLine("</table>");
            }

            public override void TableRow(params string[] values)
            {
                _output.Write("<tr>");
                for (int i = 0; i < values.Length; i++)
                {
                    _output.Write($"<td class=\"{(i == 0 ? "e" : "v")}\">{WebUtility.HtmlEncode(values[i])}</td>");
                }
                _output.WriteLine("</tr>");
            }

            public override void Header(string header)
            {
                _output.WriteLine("<h1>{0}</h1>", WebUtility.HtmlEncode(header));
            }

            public override void LegalNotice(string notice, string logo_img, string logo_url, string logo_text)
            {
                _output.WriteLine(@"<table>
<tr class=""h""><td>
<a href=""{0}""><img border=""0"" src=""{1}"" alt=""{2}"" /></a><h1 class=""p"">{3}</h1>
</td></tr>
</table>", logo_url, logo_img, logo_text, WebUtility.HtmlEncode(notice));
            }
        }

        sealed class CliInfoWriter : InfoWriter
        {
            public CliInfoWriter(TextWriter output) : base(output)
            {
            }

            protected override void BeginInfo()
            {
                _output.WriteLine("phpinfo()");
                _output.WriteLine($"PHP {InfoResources.Version} => {Environment.PHP_VERSION}");
                _output.WriteLine($"{InfoResources.Peachpie} {InfoResources.Version} => {Core.Utilities.ContextExtensions.GetRuntimeInformationalVersion()}");
                _output.WriteLine();
            }

            protected override void EndInfo()
            {
            }

            public override void BeginTable(int tableColumns)
            {
            }

            public override void BeginTable(params string[] header)
            {
                BeginTable(header.Length);
                TableRow(header);
            }

            public override void EndTable()
            {
                _output.WriteLine();
            }

            public override void TableRow(params string[] values)
            {
                _output.WriteLine(string.Join(" => ", values));
            }

            public override void Header(string header)
            {
                _output.WriteLine(header);
                _output.WriteLine();
            }

            public override void LegalNotice(string notice, string logo_img, string logo_url, string logo_text)
            {
                _output.WriteLine(notice);
                _output.WriteLine();
            }
        }

        static InfoWriter/*!*/CreateInfoWriter(Context/*!*/ctx)
        {
            if (ctx.IsWebApplication)
                return new HtmlInfoWriter(ctx.Output);
            else
                return new CliInfoWriter(ctx.Output);
        }

        #endregion

        /// <summary>
        /// Outputs information about PHP's configuration
        /// </summary>
        /// <param name="ctx">The php context.</param>
        /// <param name="what">
        /// The output may be customized by passing one or more of the following constants bitwise values summed together in the optional <paramref name="what"/> parameter.
        /// One can also combine the respective constants or bitwise values together with the or operator.</param>
        public static bool phpinfo(Context ctx, PhpInfoWhat what = PhpInfoWhat.INFO_ALL)
        {
            using (var writer = CreateInfoWriter(ctx))
            {
                if ((what & PhpInfoWhat.INFO_GENERAL) != 0)
                {
                    writer.Table(General(ctx));
                }
                if ((what & PhpInfoWhat.INFO_CONFIGURATION) != 0)
                {
                    Configuration(ctx, writer);
                }
                if ((what & PhpInfoWhat.INFO_ENVIRONMENT) != 0)
                {
                    Env(ctx, writer);
                }
                if ((what & PhpInfoWhat.INFO_VARIABLES) != 0)
                {
                    Variables(ctx, writer);
                }
                if ((what & PhpInfoWhat.INFO_CREDITS) != 0)
                {
                    Credits(writer);
                }
            }

            //
            return true;
        }

        static string AsYesNo(bool value) => value ? InfoResources.Yes : InfoResources.No;
        static string Export(Context ctx, PhpValue value) => Library.Variables.print_r(ctx, value, true).ToString(ctx).Trim();

        static IEnumerable<string[]> General(Context ctx)
        {
            yield return new[] { "System", $"{GetOsName()} {System.Environment.MachineName} {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}" };
            yield return new[] { InfoResources.Compiler, $"{InfoResources.Peachpie} {Core.Utilities.ContextExtensions.GetRuntimeInformationalVersion()}" };
            yield return new[] { "Architecture", RuntimeInformation.ProcessArchitecture.ToString() };
            yield return new[] { "Debug Build", AsYesNo(Core.Utilities.ContextExtensions.IsDebugRuntime()) };
            yield return new[] { "IPv6 Support", AsYesNo(System.Net.Sockets.Socket.OSSupportsIPv6) };
            yield return new[] { "Registered PHP Streams", string.Join(", ", Streams.StreamWrapper.GetSystemWrapperSchemes()) };
            yield return new[] { "Registered Stream Filters", string.Join(", ", Streams.PhpFilter.GetFilterNames(ctx)) };
        }

        static string GetOsName()
        {
            foreach (var osDesc in typeof(OSPlatform).GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(OSPlatform)).Select(p => new { Prop = p, Val = (OSPlatform)p.GetValue(null) }))
            {
                if (RuntimeInformation.IsOSPlatform(osDesc.Val))
                {
                    return osDesc.Val.ToString();
                }
            }
            return InfoResources.UnknownOS;
        }

        static void Configuration(Context ctx, InfoWriter writer)
        {
            writer.Header(InfoResources.Configuration);

            foreach (string ext in Context.GetLoadedExtensions())
            {
                //container.EchoTag("h2", ext);
            }

            // TODO: extensions configuration
            var options = StandardPhpOptions.DumpOptions(ctx, null);
            foreach (var opts in options.GroupBy(opt => opt.ExtensionName))
            {
                foreach (var o in opts)
                {
                    //yield return new[] { o.Name, o.LocalValue.ToString(ctx), o.DefaultValue.ToString(ctx) };
                }
            }
        }

        static void Env(Context ctx, InfoWriter writer)
        {
            writer.Header(InfoResources.Environment);
            writer.Table(ctx, ctx.Env, new[] { InfoResources.Variable, InfoResources.Value });
        }

        static void Variables(Context ctx, InfoWriter writer)
        {
            writer.Header("Variables");
            writer.Table(
                ((IDictionary<IntStringKey, PhpValue>)ctx.Request).Select(pair => new[] { $"$_REQUEST['{pair.Key}']", Export(ctx, pair.Value) }).Concat(
                ((IDictionary<IntStringKey, PhpValue>)ctx.Get).Select(pair => new[] { $"$_GET['{pair.Key}']", Export(ctx, pair.Value) }).Concat(
                ((IDictionary<IntStringKey, PhpValue>)ctx.Post).Select(pair => new[] { $"$_POST['{pair.Key}']", Export(ctx, pair.Value) }).Concat(
                ((IDictionary<IntStringKey, PhpValue>)ctx.Cookie).Select(pair => new[] { $"$_COOKIE['{pair.Key}']", Export(ctx, pair.Value) }).Concat(
                ((IDictionary<IntStringKey, PhpValue>)ctx.Server).Select(pair => new[] { $"$_SERVER['{pair.Key}']", Export(ctx, pair.Value) })
                )))),
                new[] { InfoResources.Variable, InfoResources.Value });
        }

        static void Credits(InfoWriter writer)
        {
            writer.Header(InfoResources.Credits);

            // TODO: creditz, can we pull it from git?
            writer.Table(new string[][] { }, new[] { "Contribution", "Authors" });
        }
    }
}
