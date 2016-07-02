using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.CommandLine
{
    class Program
    {
        static int Main(string[] args)
        {
            var compiler = new Pchp(args, new SimpleAnalyzerAssemblyLoader());
            return compiler.Run(Console.Out);
        }
    }
}
