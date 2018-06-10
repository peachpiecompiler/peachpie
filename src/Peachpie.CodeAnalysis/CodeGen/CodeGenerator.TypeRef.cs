﻿using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.CodeGen
{
    partial class CodeGenerator
    {
        /// <summary>
        /// Gets value indicating the given type represents a double and nothing else.
        /// </summary>
        internal bool IsDoubleOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeRefContext.IsDouble(tmask) && !tmask.IsRef;
        }

        /// <summary>
        /// Gets value indicating the given type represents a long and nothing else.
        /// </summary>
        internal bool IsLongOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeRefContext.IsLong(tmask) && !tmask.IsRef;
        }

        /// <summary>The given type represents only an integer, long or double.</summary>
        internal bool IsNumberOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && this.TypeRefContext.GetTypes(tmask).All(TypeHelpers.IsNumber) && !tmask.IsRef;
        }

        /// <summary>
        /// Gets value indicating the given type represents a long and nothing else.
        /// </summary>
        internal bool IsBooleanOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeRefContext.IsBoolean(tmask) && !tmask.IsRef;
        }

        /// <summary>
        /// Gets value indicating the given type represents UTF16 readonly string and nothing else.
        /// </summary>
        internal bool IsReadonlyStringOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeRefContext.IsReadonlyString(tmask) && !tmask.IsRef;
        }

        /// <summary>
        /// Gets value indicating the given type represents only class types.
        /// </summary>
        internal bool IsClassOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && this.TypeRefContext.GetObjectsFromMask(tmask) == tmask.TypesMask && !tmask.IsRef;
        }

        /// <summary>
        /// Gets value indicating the given type represents only PHP Array.
        /// </summary>
        internal bool IsArrayOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && this.TypeRefContext.GetTypes(tmask).All(x => x.IsArray) && !tmask.IsRef;
        }

        /// <summary>
        /// Gets value indicating the type can be <c>null</c>.
        /// </summary>
        internal bool CanBeNull(TypeRefMask tmask)
        {
            return tmask.IsAnyType || tmask.IsRef || tmask.IsUninitialized || this.TypeRefContext.IsNull(tmask);
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
