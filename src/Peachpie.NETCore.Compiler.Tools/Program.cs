using Pchp.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.NETCore.Compiler.Tools
{
    /// <summary>
    /// Main class.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// <c>dotnet-compile-php</c> entry point.
        /// </summary>
        /// <param name="args">Arguments passed from <c>dotnet build</c>.</param>
        public static int Main(string[] args)
        {
            args = ProcessArguments(args);

            // compile
            return PhpCompilerDriver.Run(PhpCommandLineParser.Default, null, args, null, Directory.GetCurrentDirectory(), null, null, new SimpleAnalyzerAssemblyLoader(), Console.Out);
        }

        #region ProcessArguments

        // TODO: CommonCompilerOptionsCommandLine:

        static string[] ProcessArguments(string[] args)
        {
            var todo = new Queue<string>(args);
            var newargs = new List<string>(args);

            while (todo.Count != 0)
            {
                var arg = todo.Dequeue();
                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                if (arg[0] == '@')
                {
                    foreach (var tmp in ProcessRsp(arg.Substring(1)))
                    {
                        todo.Enqueue(tmp);
                    }
                }
                else
                {
                    var opt = ParseOption(arg);
                    if (opt.HasValue)
                    {
                        var value = opt.Value.Value.ToLowerInvariant();

                        switch (opt.Value.Key.ToLowerInvariant())
                        {
                            case "emit-entry-point":
                                newargs.Insert(0, "/target:" + ((value == "true") ? "exe" : "library"));
                                break;

                            case "define": // DEBUG => /debug+ /debug:portable (portable PDB)
                                if (value == "debug")
                                {
                                    newargs.InsertRange(0, new[] { "/debug+", "/debug:portable" });
                                }
                                break;
                        }
                    }
                }
            }

            //
            return newargs.ToArray();
        }

        static KeyValuePair<string, string>? ParseOption(string arg)
        {
            string value;
            var colon = arg.IndexOf(':');
            if (colon >= 0)
            {
                value = arg.Substring(colon + 1);
                arg = arg.Remove(colon);
            }
            else
            {
                value = null;
            }

            if (arg.StartsWith("--"))
            {
                return new KeyValuePair<string, string>(arg.Substring(2), value);
            }
            else if (arg.StartsWith("-"))
            {
                return new KeyValuePair<string, string>(arg.Substring(1), value);
            }
            else
            {
                return null; // ignore /, \\, may be a file name, we don't have tro handle this in here
            }
        }

        static IEnumerable<string> ProcessRsp(string fname)
        {
            try
            {
                return File.ReadAllLines(fname);
            }
            catch
            {
                return new string[0];
            }
        }

        #endregion

        #region SimpleAnalyzerAssemblyLoader (NotImplementedException)

        class SimpleAnalyzerAssemblyLoader : Microsoft.CodeAnalysis.IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
                throw new NotImplementedException();
            }

            public System.Reflection.Assembly LoadFromPath(string fullPath)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
