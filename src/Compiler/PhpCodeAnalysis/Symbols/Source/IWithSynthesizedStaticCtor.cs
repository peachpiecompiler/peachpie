using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal interface IWithSynthesizedStaticCtor
    {
        /// <summary>
        /// Gets or initializes static constructor symbol.
        /// </summary>
        /// <returns></returns>
        MethodSymbol GetOrCreateStaticCtorSymbol();
    }
}
