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
            string rspfile = CreateRspFile(args, out string sdkdir);

            string libs = Environment.GetEnvironmentVariable("LIB") + @";C:\Windows\Microsoft.NET\assembly\GAC_MSIL";

            // compile
            return PhpCompilerDriver.Run(PhpCommandLineParser.Default, null, new[] { "@" + rspfile }, null, System.IO.Directory.GetCurrentDirectory(), sdkdir, libs, new SimpleAnalyzerAssemblyLoader(), Console.Out);
        }

        #region ProcessArguments

        // TODO: CommonCompilerOptionsCommandLine:


        /// <summary>
        /// Parses given arguments and gets new set of arguments to be passed to our compiler driver.
        /// </summary>
        /// <param name="args">Original set of arguments.</param>
        /// <param name="sdkdir"><c>sdk-dir</c> argument value.</param>
        /// <returns>New set of arguments.</returns>
        static string CreateRspFile(string[] args, out string sdkdir)
        {
            var todo = new Queue<string>(args);
            var newargs = new List<string>();
            var sourcefiles = new List<string>();

            string tmpoutput = System.IO.Directory.GetCurrentDirectory(); // temp output to place new RSP file into
            sdkdir = null;

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

                            case "optimize":
                                newargs.Insert(0, "/o" + ((value == "true") ? "+" : "-"));
                                break;

                            case "debug-type": // --debug-type:portable => /debug:portable
                                newargs.Insert(0, "/debug:" + value);
                                break;

                            case "define": // DEBUG => /debug+ /debug:portable (portable PDB)
                                if (value == "debug")
                                {
                                    newargs.Insert(0, "/debug+");
                                }
                                break;
                            case "temp-output":
                                // We have to keep the correct capital letters in the path, as some operating systems distinguish them
                                tmpoutput = opt.Value.Value;
                                break;
                            case "generate-xml-documentation":
                                newargs.Add($"/doc");
                                break;
                            case "sdk-dir":
                                sdkdir = opt.Value.Value;
                                break;
                        }

                        //
                        var rspvalue = opt.Value.Value;
                        if (rspvalue.IndexOfAny(new[] { ' ', '\t' }) >= 0)
                            rspvalue = "\"" + rspvalue + "\"";  // enclose into quotes so 'ParseResponseLines' won't split it into words

                        //
                        newargs.Add($"--{opt.Value.Key}:{rspvalue}");
                    }
                    else
                    {
                        sourcefiles.Add($"\"{arg}\"");
                    }
                }
            }

            //
            var alllines = newargs.Concat(sourcefiles);
            var rspfile = Path.Combine(tmpoutput, "dotnet-compile-php.rsp");
            File.WriteAllLines(rspfile, alllines);
            return rspfile;
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
