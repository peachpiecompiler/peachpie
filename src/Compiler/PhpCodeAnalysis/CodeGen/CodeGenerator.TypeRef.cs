using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Semantics.Graph;
using System.Reflection.Metadata;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.CodeGen
{
    partial class CodeGenerator
    {
        /// <summary>
        /// Gets value indicating whether given type represents a double ant nothing else.
        /// </summary>
        internal bool IsDoubleOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && _routine.TypeRefContext.IsDouble(tmask);
        }
    }
}
