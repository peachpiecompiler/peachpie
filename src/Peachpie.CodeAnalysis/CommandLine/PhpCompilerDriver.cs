using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.CommandLine
{
    public static class PhpCompilerDriver
    {
        public static int Run(CommandLineParser parser, string responseFile, string[] args,
            string clientDirectory, string baseDirectory, string sdkDirectory,
            string additionalReferenceDirectories,
            IAnalyzerAssemblyLoader analyzerLoader,
            TextWriter output)
        {
            return
                new PhpCompiler(parser, responseFile, args, clientDirectory, baseDirectory, sdkDirectory, additionalReferenceDirectories, analyzerLoader)
                .Run(output);
        }

        public static int Run(PhpCommandLineParser @default)
        {
            throw new NotImplementedException();
        }
    }
}
