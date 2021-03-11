using System;
using System.Collections.Generic;
using System.Globalization;
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
            return value.HasValue && ReferenceEquals(value.Value, null);
        }

        /// <summary>
        /// Gets value indicating the given constant is number zero.
        /// </summary>
        public static bool IsZero(this Optional<object> value)
        {
            if (value.HasValue)
            {
                var obj = value.Value;
                if (obj is long l) return l == 0L;
                if (obj is int i) return i == 0;
                if (obj is double d) return d == 0.0;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified optional value is equal to the current one.
        /// If <see cref="Optional{T}.HasValue"/> of both is set to false, they are considered equal.
        /// </summary>
        public static bool EqualsOptional(this Optional<object> value, Optional<object> other)
        {
            return
                (value.HasValue == other.HasValue) &&
                (value.HasValue == false || Equals(value.Value, other.Value));
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

        /// <summary>
        /// PHP safe implicit conversion to <c>bool</c> if the value is known.
        /// </summary>
        public static bool TryConvertToBool(this Optional<object> value, out bool result)
        {
            if (!value.HasValue)
            {
                result = false;
                return false;
            }

            var obj = value.Value;

            if (obj == null) result = false;
            else if (obj is bool) result = (bool)obj;
            else if (obj is int) result = (int)obj != 0;
            else if (obj is long) result = (long)obj != 0;
            else if (obj is string) result = StringToBoolean((string)obj);
            else if (obj is double) result = (double)obj != 0.0;
            else if (obj is float) result = (float)obj != 0;
            else if (obj is decimal) result = (decimal)obj != 0;
            else if (obj is ulong) result = (ulong)obj != 0;
            else if (obj is uint) result = (uint)obj != 0;
            else if (obj is sbyte) result = (sbyte)obj != 0;
            else if (obj is short) result = (short)obj != 0;
            else
            {
                result = false;
                return false;
            }

            //
            return true;
        }

        /// <summary>
        /// PHP safe implicit conversion to <c>long</c> if the value is known.
        /// </summary>
        public static bool TryConvertToLong(this Optional<object> value, out long result)
        {
                result = 0;

            if (!value.HasValue)
            {
                return false;
            }

            var obj = value.Value;

            if (obj == null) result = 0;
            else if (obj is bool) result = (bool)obj ? 1 : 0;
            else if (obj is int) result = (int)obj;
            else if (obj is long) result = (long)obj;
            else if (obj is string && long.TryParse((string)obj, out result)) { }
            else if (obj is double) result = (long)(double)obj;
            else if (obj is float) result = (long)(float)obj;
            else if (obj is uint) result = (uint)obj;
            else if (obj is sbyte) result = (sbyte)obj;
            else if (obj is short) result = (short)obj;
            else
            {
                return false;
            }

            //
            return true;
        }

        public static bool TryConvertToString(this Optional<object> value, out string result)
        {
            result = null;

            if (value.HasValue)
            {
                var obj = value.Value;

                if (obj == null) result = string.Empty;
                else if (obj is string str) result = str;
                else if (obj is long l) result = l.ToString(CultureInfo.InvariantCulture);
                else if (obj is bool b) result = b ? "1" : string.Empty;
                else if (obj is int i) result = i.ToString(CultureInfo.InvariantCulture);
                // else if (obj is double d) result = d.ToString(CultureInfo.InvariantCulture) // NOTE: NegativeInfinitySymbol = "-INF", PositiveInfinitySymbol = "INF", NaNSymbol = "NAN"
            }

            return result != null;
        }

        static bool StringToBoolean(string value)
        {
            return !string.IsNullOrEmpty(value) && value != "0";
        }

        /// <summary>Boxed <c>boolean</c> to be re-used.</summary>
        readonly static object s_true = true;
        /// <summary>Boxed <c>boolean</c> to be re-used.</summary>
        readonly static object s_false = false;

        readonly static object s_long_zero = 0L;

        /// <summary>
        /// Boxes <see cref="bool"/> into object without memory allocation.
        /// </summary>
        public static object AsObject(this bool b) => b ? s_true : s_false;

        /// <summary>
        /// Gets <see cref="Optional{Object}"/> of <see cref="bool"/>.
        /// This method does not allocate a new boolean on heap.
        /// </summary>
        public static Optional<object> AsOptional(this bool b) => new Optional<object>(AsObject(b));

        /// <summary>
        /// Gets <see cref="Optional{Object}"/> of <see cref="long"/>.
        /// </summary>
        public static Optional<object> AsOptional(this long l) => new Optional<object>(l == 0L ? s_long_zero : (object)l);

        /// <summary>
        /// Checks if <paramref name="optional"/> contains a boolean value. If so, it retrieves it in
        /// <paramref name="value"/> and return TRUE. Otherwise, returns FALSE.
        /// </summary>
        public static bool IsBool(this Optional<object> optional, out bool value)
        {
            if (optional.HasValue && optional.Value is bool optValue)
            {
                value = optValue;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Checks if <paramref name="optional"/> contains a long or int value. If so, it retrieves it as long
        /// (possibly converting from int) in <paramref name="value"/> and returns TRUE. Otherwise, returns FALSE.
        /// </summary>
        public static bool IsInteger(this Optional<object> optional, out long value)
        {
            if (optional.HasValue)
            {
                var obj = optional.Value;
                if (obj is long l) { value = l; return true; }
                if (obj is int i) { value = i; return true; }
            }

            value = default;
            return false;
        }
    }
}
