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
        const string BasedirOption = "/basedir:";

        public static int Main(string[] args)
        {
            // /basedir:
            string basedir = args.FirstOrDefault(x => x.StartsWith(BasedirOption));
            if (basedir != null) basedir = basedir.Substring(BasedirOption.Length);

            // compile
            return PhpCompilerDriver.Run(PhpCommandLineParser.Default, null, args, null, basedir, null, null, new SimpleAnalyzerAssemblyLoader(), TextWriter.Null);
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
