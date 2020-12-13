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
        /// Gets value indicating the given type represents a double and nothing else.
        /// </summary>
        internal bool IsDoubleOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && !tmask.IsRef && this.TypeRefContext.IsDouble(tmask);
        }

        /// <summary>
        /// Gets value indicating the given type represents a long and nothing else.
        /// </summary>
        internal bool IsLongOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && !tmask.IsRef && this.TypeRefContext.IsLong(tmask);
        }

        /// <summary>The given type represents only an integer, long or double.</summary>
        internal bool IsNumberOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && !tmask.IsRef && this.TypeRefContext.GetTypes(tmask).AllIsNumber();
        }

        /// <summary>
        /// Gets value indicating the given type represents a boolean and nothing else.
        /// </summary>
        internal bool IsBooleanOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && !tmask.IsRef && this.TypeRefContext.IsBoolean(tmask);
        }

        /// <summary>
        /// Gets value indicating the given type represents UTF16 readonly string and nothing else.
        /// </summary>
        internal bool IsReadonlyStringOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && !tmask.IsRef && this.TypeRefContext.IsReadonlyString(tmask);
        }

        /// <summary>
        /// Gets value indicating the given type represents only class types.
        /// </summary>
        internal bool IsClassOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsRef && this.TypeRefContext.IsObjectOnly(tmask); // .GetTypes(tmask).AllIsObject();
        }

        /// <summary>
        /// Gets value indicating the given type represents only PHP Array.
        /// </summary>
        internal bool IsArrayOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && !tmask.IsRef && this.TypeRefContext.GetTypes(tmask).AllIsArray();
        }

        /// <summary>
        /// Gets value indicating the type can be <c>null</c>.
        /// </summary>
        internal bool CanBeNull(TypeRefMask tmask)
        {
            return tmask.IsAnyType  // mixed
                || tmask.IsRef      // &
                || this.TypeRefContext.IsNullOrVoid(tmask); // type analysis determined there might be NULL
        }

        /// <summary>
        /// Gets value indicating the type can hold <c>null</c>.
        /// </summary>
        internal bool CanBeNull(TypeSymbol type)
        {
            Debug.Assert(type.IsValidType());

            return
                type.IsReferenceType ||
                type == CoreTypes.PhpValue ||
                type == CoreTypes.PhpAlias;
        }

        /// <summary>
        /// Determines whether the type needs to be copied when passing by value.
        /// </summary>
        internal bool IsCopiable(TypeSymbol t)
        {
            return
                t == CoreTypes.PhpValue ||
                t == CoreTypes.PhpString ||
                t == CoreTypes.PhpArray;
        }

        /// <summary>
        /// Determines whether the type needs to be copied when passing by value.
        /// </summary>
        internal bool IsCopiable(TypeRefMask tmask)
        {
            return
                tmask.IsAnyType ||
                tmask.IsUninitialized ||
                tmask.IsRef ||
                this.TypeRefContext.IsArray(tmask) ||
                this.TypeRefContext.IsWritableString(tmask);
        }
    }
}
