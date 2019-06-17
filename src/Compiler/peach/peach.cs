using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.CommandLine
{
    class Pchp : PhpCompiler
    {
        public Pchp(string[] args, IAnalyzerAssemblyLoader analyzerLoader)
            :base(
                 PhpCommandLineParser.Default,
                 Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ResponseFileName),
                 CreateArgs(args),
                 new BuildPaths(
                     clientDir: AppDomain.CurrentDomain.BaseDirectory,
                     workingDir: System.IO.Directory.GetCurrentDirectory(),
                     sdkDir: System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),
                     tempDir: null),
                 ReferenceDirectories,
                 analyzerLoader)
        {
            
        }

        static string ReferenceDirectories
        {
            get
            {
                var libs = Environment.GetEnvironmentVariable("LIB");
                var gac = Environment.ExpandEnvironmentVariables(@"%windir%\Microsoft.NET\assembly\GAC_MSIL");
                return libs + ";" + gac;
            }
        }

        static string[] CreateArgs(string[] args)
        {
            // implicit references
            var assemblies = new List<Assembly>()
            {
                typeof(object).Assembly,            // mscorlib (or System.Runtime)
                typeof(HashSet<>).Assembly,         // System.Core
                typeof(System.ComponentModel.EditorBrowsableAttribute).Assembly, // System.Runtime
                typeof(Core.Context).Assembly,      // Peachpie.Runtime
                typeof(Library.Strings).Assembly,   // Peachpie.Library
                typeof(Peachpie.Library.XmlDom.DOMDocument).Assembly,   // Peachpie.Library.XmlDom
                typeof(Peachpie.Library.Scripting.PhpFunctions).Assembly,   // Peachpie.Library.Scripting
                typeof(Peachpie.Library.Network.CURLFunctions).Assembly, // cURL
                typeof(Peachpie.Library.Graphics.PhpGd2).Assembly, // GD2, Image
                typeof(Peachpie.Library.MySql.MySql).Assembly,  // MySql
                typeof(Peachpie.Library.MsSql.MsSql).Assembly,  // MsSql
            };
            var refs = assemblies.Distinct().Select(ass => "/r:" + ass.Location);

            Debug.Assert(refs.Any(r => r.Contains("System.Core")));
            Debug.Assert(refs.Any(r => r.Contains("Peachpie.Runtime")));
            Debug.Assert(refs.Any(r => r.Contains("Peachpie.Library")));

            //
            return refs.Concat(args).ToArray();
        }
    }
}
