using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CommandLine
{
    internal sealed class PhpCommandLineArguments : CommandLineArguments
    {
        protected override CompilationOptions CompilationOptionsCore
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override ParseOptions ParseOptionsCore
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
