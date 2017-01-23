using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    public static class Operators
    {
        #region Numeric

        /// <summary>
        /// Bit mask corresponding to the sign in <see cref="long"/> value.
        /// </summary>
        internal const long LONG_SIGN_MASK = (1L << (8 * sizeof(long) - 1));

        /// <summary>
        /// Performs bitwise and operation.
        /// </summary>
        internal static PhpValue BitAnd(ref PhpValue x, ref PhpValue y)
        {
            var bx = x.BytesOrNull();
            if (bx != null)
            {
                var by = y.BytesOrNull();
                if (by != null)
                {
                    throw new NotImplementedException();
                }
            }

            //
            return PhpValue.Create(x.ToLong() & y.ToLong());
        }

        /// <summary>
        /// Performs bitwise or operation.
        /// </summary>
        internal static PhpValue BitOr(ref PhpValue x, ref PhpValue y)
        {
            var bx = x.BytesOrNull();
            if (bx != null)
            {
                var by = y.BytesOrNull();
                if (by != null)
                {
                    throw new NotImplementedException();
                }
            }

            //
            return PhpValue.Create(x.ToLong() | y.ToLong());
        }

        /// <summary>
        /// Performs exclusive or operation.
        /// </summary>
        internal static PhpValue BitXor(ref PhpValue x, ref PhpValue y)
        {
            var bx = x.BytesOrNull();
            if (bx != null)
            {
                var by = y.BytesOrNull();
                if (by != null)
                {
                    return PhpValue.Create(new PhpString(BitXor(bx, by)));
                }
            }

            //
            return PhpValue.Create(x.ToLong() ^ y.ToLong());
        }

        static byte[] BitXor(byte[] bx, byte[] by)
        {
            int length = Math.Min(bx.Length, by.Length);
            byte[] result = new byte[length];

            return BitXor(result, bx, by);
        }

        /// <summary>
        /// Performs specified binary operation on arrays of bytes.
        /// </summary>
        /// <param name="result">An array where to store the result. Data previously stored here will be overwritten.</param>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand</param>
        /// <returns>The reference to the the <paramref name="result"/> array.</returns>
        static byte[] BitXor(byte[]/*!*/ result, byte[]/*!*/ x, byte[]/*!*/ y)
        {
            Debug.Assert(result != null && x != null && y != null && result.Length <= x.Length && result.Length <= y.Length);

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = unchecked((byte)(x[i] ^ y[i]));
            }

            // remaining bytes are ignored //
            return result;
        }

        /// <summary>
        /// Performs bitwise negation.
        /// </summary>
        internal static PhpValue BitNot(ref PhpValue x)
        {
            switch (x.TypeCode)
            {
                case PhpTypeCode.Long: return PhpValue.Create(~x.Long);

                case PhpTypeCode.Int32: return PhpValue.Create(~x.ToLong());

                case PhpTypeCode.Alias: return BitNot(ref x.Alias.Value);

                case PhpTypeCode.String:
                case PhpTypeCode.WritableString:
                    throw new NotImplementedException();    // bitwise negation of each character in string

                case PhpTypeCode.Object:
                    if (x.Object == null)
                    {
                        return PhpValue.Null;
                    }
                    goto default;

                default:
                    // TODO: Err UnsupportedOperandTypes
                    return PhpValue.Null;
            }
        }

        /// <summary>
        /// Performs division according to PHP semantics.
        /// </summary>
        /// <remarks>The division operator ("/") returns a float value unless the two operands are integers
        /// (or strings that get converted to integers) and the numbers are evenly divisible,
        /// in which case an integer value will be returned.</remarks>
        internal static PhpNumber Div(ref PhpValue x, ref PhpValue y)
        {
            PhpNumber nx, ny;
            var info = x.ToNumber(out nx) | y.ToNumber(out ny);

            if ((info & Convert.NumberInfo.IsPhpArray) != 0)
            {
                //PhpException.UnsupportedOperandTypes();
                //return PhpNumber.Create(0.0);
                throw new NotImplementedException();     // PhpException
            }

            // TODO: // division by zero:
            //if (y == 0)
            //{
            //    PhpException.Throw(PhpError.Warning, CoreResources.GetString("division_by_zero"));
            //    return false;
            //}

            return nx / ny;
        }

        #endregion

        #region Assignment

        /// <summary>
        /// Assigns a PHP value by value according to the PHP semantics.
        /// </summary>
        /// <param name="target">Target of the assignment.</param>
        /// <param name="value">Value to be assigned.</param>
        public static void SetValue(ref PhpValue target, PhpValue value)
        {
            Debug.Assert(!value.IsAlias);
            if (target.IsAlias)
            {
                target.Alias.Value = value;
            }
            else
            {
                target = value;
            }
        }

        /// <summary>
        /// Assigns a PHP value to an aliased place.
        /// </summary>
        /// <param name="target">Target of the assignment.</param>
        /// <param name="value">Value to be assigned.</param>
        public static void SetValue(PhpAlias target, PhpValue value)
        {
            Debug.Assert(!value.IsAlias);
            target.Value = value;
        }

        #endregion

        #region Ensure

        /// <summary>
        /// Ensures given variable is not <c>null</c>.
        /// </summary>
        public static object EnsureObject(ref object obj) => obj ?? (obj = new stdClass());

        /// <summary>
        /// Ensures given variable is not <c>null</c>.
        /// </summary>
        public static PhpArray EnsureArray(ref PhpArray arr) => arr ?? (arr = new PhpArray());

        /// <summary>
        /// Ensures given variable is not <c>null</c>.
        /// </summary>
        public static IPhpArray EnsureArray(ref IPhpArray arr) => arr ?? (arr = new PhpArray());

        /// <summary>
        /// Implementation of PHP <c>isset</c> operator.
        /// </summary>
        public static bool IsSet(PhpValue value) => value.IsSet && !value.IsNull;   // TODO: !Alias.IsNull

        /// <summary>
        /// Implements <c>empty</c> operator.
        /// </summary>
        public static bool IsEmpty(PhpValue value) => !value.IsSet || value.IsEmpty;

        #endregion

        #region Array Access

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="string"/>.
        /// </summary>
        /// <param name="value">String to be accessed as array.</param>
        /// <param name="index">Index.</param>
        /// <returns>Character on index or empty string if index is our of range.</returns>
        public static string GetItemValue(string value, int index)
        {
            return (value != null && index >= 0 && index < value.Length)
                ? value[index].ToString()
                : string.Empty;
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="string"/>.
        /// </summary>
        public static string GetItemValue(string value, IntStringKey key)
        {
            int index = key.IsInteger
                ? key.Integer
                : (int)Convert.StringToLongInteger(key.String);

            return GetItemValue(value, index);
        }

        /// <summary>
        /// Implements <c>[]</c> operator on <see cref="PhpValue"/>.
        /// </summary>
        public static PhpValue GetItemValue(PhpValue value, IntStringKey key, bool quiet = false) => value.GetArrayItem(key, quiet);

        /// <summary>
        /// Implements <c>&amp;[]</c> operator on <see cref="PhpValue"/>.
        /// </summary>
        public static PhpAlias EnsureItemAlias(PhpValue value, IntStringKey key, bool quiet = false) => value.EnsureItemAlias(key, quiet);

        #endregion

        #region Copy

        /// <summary>
        /// Gets copy of given value.
        /// </summary>
        public static PhpValue DeepCopy(PhpValue value) => value.DeepCopy();

        #endregion

        #region Enumerator

        /// <summary>
        /// Gets enumerator object for given value.
        /// </summary>
        public static IPhpEnumerator GetForeachEnumerator(PhpValue value, bool aliasedValues, RuntimeTypeHandle caller) => value.GetForeachEnumerator(aliasedValues, caller);

        #endregion
    }
}
