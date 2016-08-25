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
            return
                new Pchp(args, new SimpleAnalyzerAssemblyLoader())
                .Run(Console.Out);
        }
    }
}
