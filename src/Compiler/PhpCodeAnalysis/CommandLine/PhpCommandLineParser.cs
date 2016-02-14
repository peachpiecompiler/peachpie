using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CommandLine
{
    internal sealed class PhpCommandLineParser : CommandLineParser
    {
        public static PhpCommandLineParser Default { get; } = new PhpCommandLineParser();

        protected override string RegularFileExtension { get; } = Constants.ScriptFileExtension;
        protected override string ScriptFileExtension { get; } = Constants.ScriptFileExtension;

        internal PhpCommandLineParser()
            : base(Errors.MessageProvider.Instance, false)
        {
        }

        internal override CommandLineArguments CommonParse(IEnumerable<string> args, string baseDirectory, string sdkDirectoryOpt, string additionalReferenceDirectories)
        {
            return new PhpCommandLineArguments()
            {
                // TODO: parsed arguments
            };
        }

        internal override void GenerateErrorForNoFilesFoundInRecurse(string path, IList<Diagnostic> errors)
        {
            // nothing
        }
    }
}
