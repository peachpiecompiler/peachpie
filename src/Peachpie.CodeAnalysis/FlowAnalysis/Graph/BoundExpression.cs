using Pchp.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    partial class BoundLiteral
    {
        /// <summary>
        /// Gets type mask of the literal within given type context.
        /// </summary>
        internal TypeRefMask ResolveTypeMask(TypeRefContext typeCtx)
        {
            Debug.Assert(this.ConstantValue.HasValue);
            
            return this.ConstantValue.Value switch
            {
                null => typeCtx.GetNullTypeMask(),
                int => typeCtx.GetLongTypeMask(),
                long => typeCtx.GetLongTypeMask(),
                string => typeCtx.GetStringTypeMask(),
                bool => typeCtx.GetBooleanTypeMask(),
                double => typeCtx.GetDoubleTypeMask(),
                byte[] => typeCtx.GetWritableStringTypeMask(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
