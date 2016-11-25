using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.Core
{
    partial class Context
    {
        /// <summary>
        /// Implementation of a console application runtime context.
        /// </summary>
		sealed class ConsoleContext : Context
        {
            readonly string _rootPath;

            public ConsoleContext(params string[] args)
            {   
                _textSink = Console.Out;
                _streamSink = Console.OpenStandardOutput();
                _rootPath = Directory.GetCurrentDirectory() + "\\";
                IsOutputBuffered = false;   // initializes Output

                // TODO: Globals
            }

            public override string RootPath => _rootPath;

            public override string WorkingDirectory => Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Creates context to be used within a console application.
        /// </summary>
        public static Context CreateConsole(params string[] args)
        {
            return new ConsoleContext(args);
        }
    }
}
