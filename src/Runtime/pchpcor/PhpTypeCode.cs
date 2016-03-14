using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    /// <summary>
    /// Pchp type hierarchy type codes.
    /// </summary>
    public enum PhpTypeCode : int
    {
        /// <summary>
        /// An invalid value.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// The value is of type boolean.
        /// </summary>
        Boolean,

        /// <summary>
        /// 32-bit integer value.
        /// </summary>
        Int32,

        /// <summary>
        /// 64-bit integer value.
        /// </summary>
        Long,

        /// <summary>
        /// 64-bit floating point number.
        /// </summary>
        Double,

        /// <summary>
        /// A PHP array.
        /// </summary>
        PhpArray,

        /// <summary>
        /// Unicode string value.
        /// </summary>
        String,

        /// <summary>
        /// Binary string value.
        /// </summary>
        BinaryString,

        /// <summary>
        /// A result of strings concatenation, binary nor unicode.
        /// </summary>
        PhpStringBuilder,

        /// <summary>
        /// A class type, including <c>NULL</c>, <c>resource</c>, <c>Closure</c> or generic <c>Object</c>.
        /// </summary>
        Object,
    }

    /// <summary>
    /// Helper class providing methods for <see cref="PhpTypeCode"/>.
    /// </summary>
    public static class PhpTypeCodes
    {
        ///// <summary>
        ///// Gets value indicating whether given type is a nullable type.
        ///// </summary>
        //public static bool IsNullable(this PhpTypeCode code)
        //{
        //    return code == PhpTypeCode.Object || code == PhpTypeCode.PhpArray;
        //}
    }
}
