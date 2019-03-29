using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CommandLine
{
    public static class PhpCompilerDriver
    {
        public static int Run(CommandLineParser parser, string responseFile, string[] args,
            string clientDirectory, string baseDirectory, string sdkDirectory,
            string additionalReferenceDirectories,
            IAnalyzerAssemblyLoader analyzerLoader,
            TextWriter output, CancellationToken cancellationToken)
        {
            var buildPaths = new BuildPaths(clientDirectory, baseDirectory, sdkDirectory, null);
            return
                new PhpCompiler(parser, responseFile, args, buildPaths, additionalReferenceDirectories, analyzerLoader)
                .Run(output, cancellationToken);
        }

        public static int Run(PhpCommandLineParser @default)
        {
            throw new NotImplementedException();
        }
    }
}
