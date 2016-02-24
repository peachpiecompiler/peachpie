using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis
{
    partial class PhpCompilation
    {
        /// <summary>
        /// Gets <see cref="INamedTypeSymbol"/> best fitting given type mask.
        /// </summary>
        internal INamedTypeSymbol GetTypeByTypeRef(TypeRefContext typeCtx, TypeRefMask typeMask)
        {
            throw new NotImplementedException();
        }
    }
}
