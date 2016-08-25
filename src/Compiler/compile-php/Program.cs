using Pchp.CodeAnalysis.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Peachpie.NETCore.Compiler
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return PhpCompilerDriver.Run(PhpCommandLineParser.Default, null, args, null, null, null, null, new SimpleAnalyzerAssemblyLoader(), TextWriter.Null);
        }

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
    }
}
