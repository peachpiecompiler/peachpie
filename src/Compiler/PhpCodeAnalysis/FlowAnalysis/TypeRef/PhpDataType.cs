using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// PHP value type codes.
    /// </summary>
    public enum PhpDataType : int
    {
        Undefined = 0,

        Null,
        Boolean,
        Long,
        Double,
        String,
        Array,
        Object,
        Resource,
        Callable,

        /// <summary>
        /// Amount of types within the enum.
        /// </summary>
        Count
    }

    /// <summary>
    /// Helper methods for <see cref="PhpDataType"/>.
    /// </summary>
    public static class PhpDataTypeEnum
    {
        /// <summary>
        /// Mask which bits corresponds to <see cref="PhpDataType"/> values. Determines which type can be a <c>null</c> value.
        /// </summary>
        private const int NullableTypesMask =
            (1 << (int)PhpDataType.Object) |
            (1 << (int)PhpDataType.Array) |
            (1 << (int)PhpDataType.Resource) |
            (1 << (int)PhpDataType.Callable) |
            (1 << (int)PhpDataType.Null);

        /// <summary>
        /// Gets value indicating whether given type is a nullable type.
        /// </summary>
        public static bool IsNullable(this PhpDataType code)
        {
            return (NullableTypesMask & (1 << (int)code)) != 0;
        }
    }
}
