using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    partial class TypeRefContext
    {
        /// <summary>
        /// Converts CLR type symbol to TypeRef used by flow analysis.
        /// </summary>
        internal TypeRefMask AddToContext(TypeSymbol t)
        {
            Contract.ThrowIfNull(t);

            switch (t.SpecialType)
            {
                case SpecialType.System_Void: return 0;
                case SpecialType.System_Int64: return this.GetLongTypeMask();
                case SpecialType.System_String: return this.GetStringTypeMask();
                case SpecialType.System_Double: return this.GetDoubleTypeMask();
                case SpecialType.System_Boolean: return this.GetBooleanTypeMask();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
