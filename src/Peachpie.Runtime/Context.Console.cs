using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Utilities;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Implementation of a console application runtime context.
        /// </summary>
		sealed class ConsoleContext : Context
        {
            sealed class OSEncodingProvider : EncodingProvider
            {
                public Encoding ProvidedEncoding { get; }

                public OSEncodingProvider(Encoding providedEncoding)
                {
                    ProvidedEncoding = providedEncoding ?? throw new ArgumentNullException(nameof(providedEncoding));
                }

                public override Encoding GetEncoding(int codepage)
                {
                    return (codepage == ProvidedEncoding.CodePage) ? ProvidedEncoding : null;
                }

                public override Encoding GetEncoding(string name)
                {
                    return null;
                }
            }

            /// <summary>
            /// Gets server type interface name.
            /// </summary>
            public override string ServerApi => "cli";

            public override Encoding StringEncoding => Console.OutputEncoding;

            /// <summary>
            /// Initializes the console context.
            /// </summary>
            public ConsoleContext(string mainscript, string rootPath, Stream output, params string[] args)
                : base(null)
            {
                RootPath = WorkingDirectory = rootPath ?? Directory.GetCurrentDirectory();

                //
                if (output != null)
                {
                    // use provided output stream
                    InitOutput(output);
                }
                else
                {
                    //Console.OutputEncoding = Encoding.UTF8;
                    //Console.Write("\xfeff"); // bom = byte order mark

                    // use the default Console output stream
                    InitOutput(Console.OpenStandardOutput(), Console.Out);
                }

                // Globals
                InitSuperglobals();
                InitializeServerVars(mainscript);
                InitializeArgvArgc(mainscript, args);

                if (CurrentPlatform.IsWindows)
                {
                    // VT100
                    WindowsPlatform.Enable_VT100();
                }

                // (sometimes??) the Encoding used by Console cannot be resolved by Encoding.GetEncoding(),
                // register it for sure:
                Encoding.RegisterProvider(new OSEncodingProvider(Console.OutputEncoding));
            }
        }

        /// <summary>Initialize additional <c>$_SERVER</c> entries.</summary>
        /// <param name="mainscript"></param>
        protected void InitializeServerVars(string mainscript)
        {
            var server = this.Server;

            // initialize server variables in order:

            server[CommonPhpArrayKeys.PHP_SELF] = mainscript;
            server[CommonPhpArrayKeys.SCRIPT_NAME] = mainscript;
            server[CommonPhpArrayKeys.SCRIPT_FILENAME] = mainscript;
            server[CommonPhpArrayKeys.PATH_TRANSLATED] = mainscript;
            server[CommonPhpArrayKeys.DOCUMENT_ROOT] = string.Empty;
            server[CommonPhpArrayKeys.REQUEST_TIME_FLOAT] = DateTimeUtils.UtcToUnixTimeStampFloat(DateTime.UtcNow);
            server[CommonPhpArrayKeys.REQUEST_TIME] = DateTimeUtils.UtcToUnixTimeStamp(DateTime.UtcNow);
        }

        /// <summary>Initializes global $argv and $argc variables and corresponding $_SERVER entries.</summary>
        protected void InitializeArgvArgc(string mianscript, params string[] args)
        {
            Debug.Assert(args != null);
            
            // PHP array with command line arguments
            // including 0-th argument corresponding to program executable
            var argv = new PhpArray(1 + args.Length);

            argv.Add(mianscript ?? "-");
            argv.AddRange(args);

            // command line argc, argv:
            this.Globals["argv"] = (this.Server["argv"] = argv).DeepCopy();
            this.Globals["argc"] = this.Server["argc"] = argv.Count;
        }

        /// <summary>
        /// Creates context to be used within a console application.
        /// </summary>
        public static Context CreateConsole(string mainscript) => CreateConsole(mainscript, args: Array.Empty<string>());

        /// <summary>
        /// Creates context to be used within a console application.
        /// </summary>
        public static Context CreateConsole(string mainscript, params string[] args) => CreateConsole(mainscript, null, null, args);

        /// <summary>
        /// Creates context to be used within a console application.
        /// </summary>
        /// <param name="mainscript">Informational. Used for <c>PHP_SELF</c> global variable and related variables.</param>
        /// <param name="rootPath">
        /// The path to be recognized as the application root path.
        /// Also used as the internal current working directory.
        /// Can be changed by setting <see cref="Context.RootPath"/> and <see cref="Context.WorkingDirectory"/>.
        /// </param>
        /// <param name="output">Optional. The output stream.</param>
        /// <param name="args">Optional. Application arguments. Used to set <c>$argv</c> and <c>$argc</c> global variable.</param>
        public static Context CreateConsole(string mainscript, string rootPath, Stream output, params string[] args)
        {
            return new ConsoleContext(mainscript, rootPath, output, args);
        }
    }
}
