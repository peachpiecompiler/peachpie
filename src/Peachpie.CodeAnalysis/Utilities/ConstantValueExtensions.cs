using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis
{
    internal static class ConstantValueExtensions
    {
        /// <summary>
        /// Tries to convert <paramref name="value"/> to a <see cref="ConstantValue"/> if possible.
        /// Argument that doesn't have value or values which cannot be represented as <see cref="ConstantValue"/> causes a <c>null</c> reference to be returned.
        /// </summary>
        /// <param name="value">Optional boced value.</param>
        /// <returns><see cref="ConstantValue"/> instance if possible. Otherwise a <c>null</c> reference.</returns>
        public static ConstantValue ToConstantValueOrNull(this Optional<object> value)
        {
            if (value.HasValue)
            {
                var obj = value.Value;
                if (obj == null) return ConstantValue.Null;
                if (obj is int) return ConstantValue.Create((int)obj);
                if (obj is long) return ConstantValue.Create((long)obj);
                if (obj is string) return ConstantValue.Create((string)obj);
                if (obj is bool) return ConstantValue.Create((bool)obj);
                if (obj is double) return ConstantValue.Create((double)obj);
                if (obj is float) return ConstantValue.Create((float)obj);
                if (obj is decimal) return ConstantValue.Create((decimal)obj);
                if (obj is ulong) return ConstantValue.Create((ulong)obj);
                if (obj is uint) return ConstantValue.Create((uint)obj);
                if (obj is sbyte) return ConstantValue.Create((sbyte)obj);
                if (obj is short) return ConstantValue.Create((short)obj);
                if (obj is DateTime) return ConstantValue.Create((DateTime)obj);
            }

            return null;
        }

        /// <summary>
        /// Gets value indicating the constant value is set and its value is <c>null</c>.
        /// </summary>
        public static bool IsNull(this Optional<object> value)
        {
            return value.HasValue && value.Value == null;
        }

        /// <summary>
        /// Determines whether the specified optional value is equal to the current one. If <see cref="Optional{T}.HasValue"/>
        /// of both is set to false, they are considered equal.
        /// </summary>
        public static bool EqualsOptional(this Optional<object> value, Optional<object> other)
        {
            if (value.HasValue != other.HasValue)
            {
                return false;
            }
            else if (!value.HasValue)
            {
                return true;
            }
            else if (value.Value == null)
            {
                return other.Value == null;
            }
            else if (other.Value == null)
            {
                return false;
            }
            else
            {
                return value.Value.Equals(other.Value);
            }
        }

        /// <summary>
        /// PHP safe implicit conversion to <c>long</c> (null|long|double to long).
        /// </summary>
        public static bool TryConvertToLong(this ConstantValue value, out long result)
        {
            result = 0;

            if (value == null) return false;
            else if (value.IsNull) result = 0;
            else if (value.IsIntegral) result = value.Int64Value;
            else if (value.IsFloating) result = (long)value.DoubleValue;
            else
            {
                return false;
            }

            return true;
        }

        public static bool TryConvertToBool(this ConstantValue value, out bool result)
        {
            // TODO: Convert also strings
            if (value.IsBoolean)
            {
                result = value.BooleanValue;
                return true;
            }
            else if (value.IsNumeric)
            {
                // False if zero, true otherwise
                result = !value.IsDefaultValue;
                return true;
            }
            else
            {
                result = false;
                return false;
            }
        }
    }
}
