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
            var value = this.ConstantValue.Value;

            if (value == null)
            {
                return typeCtx.GetNullTypeMask();
            }
            else
            {
                if (value is long || value is int)
                {
                    return typeCtx.GetLongTypeMask();
                }
                else if (value is string)
                {
                    return typeCtx.GetStringTypeMask();
                }
                else if (value is bool)
                {
                    return typeCtx.GetBooleanTypeMask();
                }
                else if (value is double)
                {
                    return typeCtx.GetDoubleTypeMask();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
