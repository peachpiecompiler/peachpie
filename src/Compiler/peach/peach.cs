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
                 AppDomain.CurrentDomain.BaseDirectory,
                 System.IO.Directory.GetCurrentDirectory(),
                 System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),
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
                typeof(Core.Context).Assembly,      // Peachpie.Runtime
                typeof(Library.Strings).Assembly,   // Peachpie.Library
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
