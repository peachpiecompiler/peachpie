using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CommandLine
{
    public static class PhpCompilerDriver
    {
        /// <summary>
        /// Run by <c>Peachpie.NET.Sdk</c>.<c>BuildTask</c>
        /// </summary>
        public static int Main(string[] args)
        {
            Debugger.Launch();

            // $"/baseDirectory:{BasePath}",
            // $"/sdkDirectory:{NetFrameworkPath}",
            // $"/additionalReferenceDirectories:{libs}",
            // $"responseFile:{ResponseFilePath}"

            string responseFile = null;
            string baseDirectory = null;
            string sdkDirectory = null;
            string additionalReferenceDirectories = null;

            // arguments passed to the {CommandLineParser}
            var passedArgs = new List<string>(args.Length);

            for (int i = 0; i < args.Length; i++)
            {
                if (PhpCommandLineParser.TryParseOption2(
                    args[i],
                    out var name,
                    out var value
                ))
                {
                    if (string.Equals(name, nameof(responseFile), StringComparison.OrdinalIgnoreCase))
                    {
                        responseFile = value;
                        continue;
                    }
                    if (string.Equals(name, nameof(baseDirectory), StringComparison.OrdinalIgnoreCase))
                    {
                        baseDirectory = value;
                        continue;
                    }
                    if (string.Equals(name, nameof(sdkDirectory), StringComparison.OrdinalIgnoreCase))
                    {
                        sdkDirectory = value;
                        continue;
                    }
                    if (string.Equals(name, nameof(additionalReferenceDirectories), StringComparison.OrdinalIgnoreCase))
                    {
                        additionalReferenceDirectories = value;
                        continue;
                    }
                }

                //
                passedArgs.Add(args[i]);
            }

            return Run(
                PhpCommandLineParser.Default,
                responseFile: responseFile,
                args: passedArgs.ToArray(),
                clientDirectory: null,
                baseDirectory: baseDirectory,
                sdkDirectory: sdkDirectory,
                additionalReferenceDirectories: additionalReferenceDirectories,
                analyzerLoader: new SimpleAnalyzerAssemblyLoader(),
                output: Console.Out,
                cancellationToken: default(CancellationToken) // TODO: from input
            );
        }

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
