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
                 args,
                 AppDomain.CurrentDomain.BaseDirectory,
                 Directory.GetCurrentDirectory(),
                 Environment.GetEnvironmentVariable("LIB"),
                 analyzerLoader)
        {
            
        }
    }
}
