using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.IO;

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
                 Directory.GetCurrentDirectory(),
                 System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),
                 Environment.GetEnvironmentVariable("LIB") + @";C:\Windows\Microsoft.NET\assembly\GAC_MSIL",
                 analyzerLoader)
        {
            
        }

        static string[] CreateArgs(string[] args)
        {
            var basedir = AppDomain.CurrentDomain.BaseDirectory;
            var list = new List<string>()
            {
                "/r:" + @"C:\WINDOWS\Microsoft.Net\assembly\GAC_MSIL\System.Runtime\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Runtime.dll",
                "/r:" + @"C:\WINDOWS\Microsoft.Net\assembly\GAC_MSIL\System.Core\v4.0_4.0.0.0__b77a5c561934e089\System.Core.dll",
                "/r:" + Path.Combine(basedir, "Peachpie.Runtime.dll"),
                "/r:" + Path.Combine(basedir, "Peachpie.NETStandard.Library.dll")
            };

            //
            list.AddRange(args);
            return list.ToArray();
        }
    }
}
