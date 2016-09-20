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
        /// Performs bitwise or operation.
        /// </summary>
        internal static PhpValue BitOr(ref PhpValue x, ref PhpValue y)
        {
            var xtype = x.TypeCode;            
            if (xtype == PhpTypeCode.String || xtype == PhpTypeCode.WritableString)
            {
                var ytype = y.TypeCode;
                if (ytype == PhpTypeCode.String || ytype == PhpTypeCode.WritableString)
                {
                    throw new NotImplementedException();
                }
            }

            //
            return PhpValue.Create(x.ToLong() | y.ToLong());
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
        /// Assigns a PHP value according to the PHP semantics.
        /// </summary>
        /// <param name="target">Target of the assignment.</param>
        /// <param name="value">Value to be assigned. Caller ensures the value is not an alias.</param>
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
        /// <param name="value">Value to be assigned. Caller ensures the value is not an alias.</param>
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
        /// Implementation of PHP <c>isset</c> operator.
        /// </summary>
        public static bool IsSet(PhpValue value) => value.IsSet && !value.IsNull;

        /// <summary>
        /// Implements <c>empty</c> operator.
        /// </summary>
        public static bool IsEmpty(PhpValue value) => !value.IsSet || value.IsEmpty;

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

    /// <summary>
    /// Common operations over arrays.
    /// </summary>
    public interface IPhpArrayOperators
    {
        /// <summary>
        /// Gets value at given index.
        /// Gets <c>void</c> value in case the key is not found.
        /// </summary>
        PhpValue GetItemValue(IntStringKey key);

        /// <summary>
        /// Sets value at specific index. Value must not be an alias.
        /// </summary>
        void SetItemValue(IntStringKey key, PhpValue value);

        /// <summary>
        /// Writes aliased value at given index.
        /// </summary>
        void SetItemAlias(IntStringKey key, PhpAlias alias);

        /// <summary>
        /// Add a value to the end of array.
        /// Value can be an alias.
        /// </summary>
        void AddValue(PhpValue value);

        /// <summary>
        /// Ensures the item at given index is alias.
        /// </summary>
        PhpAlias EnsureItemAlias(IntStringKey key);

        /// <summary>
        /// Ensures the item at given index is class object.
        /// </summary>
        object EnsureItemObject(IntStringKey key);

        /// <summary>
        /// Ensures the item at given index is array.
        /// </summary>
        PhpArray EnsureItemArray(IntStringKey key);
    }
}
