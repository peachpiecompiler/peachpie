using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            /// <summary>
            /// Gets server type interface name.
            /// </summary>
            public override string ServerApi => "cli";

            /// <summary>
            /// Initializes the console context.
            /// </summary>
            /// <param name="mainscript">Entry script file name.</param>
            /// <param name="args">Command line arguments.</param>
            public ConsoleContext(string mainscript, params string[] args)
            {
                RootPath = Directory.GetCurrentDirectory();
                WorkingDirectory = RootPath;

                //
                InitOutput(Console.OpenStandardOutput(), Console.Out);

                // Globals
                InitSuperglobals();
                InitializeServerVars(mainscript);
                InitializeArgvArgc(args);
            }
        }

        /// <summary>Initialize additional <c>$_SERVER</c> entries.</summary>
        /// <param name="mainscript"></param>
        protected void InitializeServerVars(string mainscript)
        {
            var server = this.Server;

            // initialize server variables in order:

            server[CommonPhpArrayKeys.PHP_SELF] = (PhpValue)mainscript;
            server[CommonPhpArrayKeys.SCRIPT_NAME] = (PhpValue)mainscript;
            server[CommonPhpArrayKeys.SCRIPT_FILENAME] = (PhpValue)mainscript;
            server[CommonPhpArrayKeys.PATH_TRANSLATED] = (PhpValue)mainscript;
            server[CommonPhpArrayKeys.DOCUMENT_ROOT] = (PhpValue)string.Empty;
            server[CommonPhpArrayKeys.REQUEST_TIME_FLOAT] = (PhpValue)DateTimeUtils.UtcToUnixTimeStampFloat(DateTime.UtcNow);
            server[CommonPhpArrayKeys.REQUEST_TIME] = (PhpValue)DateTimeUtils.UtcToUnixTimeStamp(DateTime.UtcNow);
        }

        /// <summary>Initializes global $argv and $argc variables and corresponding $_SERVER entries.</summary>
        protected void InitializeArgvArgc(params string[] args)
        {
            Debug.Assert(args != null);

            // command line argc, argv:
            // adds all arguments to the array (the 0-th argument is not '-' as in PHP but the program file):
            var argv = new PhpArray(args);

            this.Globals["argv"] = (this.Server["argv"] = (PhpValue)argv).DeepCopy();
            this.Globals["argc"] = this.Server["argc"] = (PhpValue)args.Length;
        }

        /// <summary>
        /// Creates context to be used within a console application.
        /// </summary>
        public static Context CreateConsole(string mainscript, params string[] args)
        {
            return new ConsoleContext(mainscript, args);
        }
    }
}
