using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class MethodSymbolExtensions
    {
        public static bool IsParams(this MethodSymbol method)
        {
            return method.ParameterCount != 0 && method.Parameters[method.ParameterCount - 1].IsParams;
        }

        public static TypeSymbol[] ParametersType(this MethodSymbol method)
        {
            return method.Parameters.Select(p => p.Type).ToArray();
        }
    }
}
