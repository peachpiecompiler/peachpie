using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core
{
    #region Dictionary Comparers

    /// <summary>
	/// Compares keys of dictionary entries by specified comparer.
	/// </summary>
	public sealed class KeyComparer : IComparer<KeyValuePair<IntStringKey, PhpValue>>, IComparer<IntStringKey>
    {
        /// <summary>Regular comparer.</summary>
        public static readonly KeyComparer Default = new KeyComparer(PhpComparer.Default, false);
        /// <summary>Numeric comparer.</summary>
        public static readonly KeyComparer Numeric = new KeyComparer(PhpNumericComparer.Default, false);
        /// <summary>String comparer.</summary>
        public static KeyComparer String(Context ctx) => new KeyComparer(new PhpStringComparer(ctx), false);
        /// <summary>Array keys comparer.</summary>
        public static readonly KeyComparer ArrayKeys = new KeyComparer(PhpArrayKeysComparer.Default, false);
        /// <summary>Regular comparer with reverse order.</summary>
        public static readonly KeyComparer Reverse = new KeyComparer(PhpComparer.Default, true);
        /// <summary>Numeric comparer with reverse order.</summary>
        public static readonly KeyComparer ReverseNumeric = new KeyComparer(PhpNumericComparer.Default, true);
        /// <summary>String comparer with reverse order.</summary>
        public static KeyComparer ReverseString(Context ctx) => new KeyComparer(new PhpStringComparer(ctx), true);
        /// <summary>Locale string comparer with reverse order.</summary>
        public static readonly KeyComparer ReverseArrayKeys = new KeyComparer(PhpArrayKeysComparer.Default, true);

        /// <summary>
        /// The comparer which will be used to compare keys.
        /// </summary>
        private readonly IComparer<PhpValue>/*!*/ comparer;

        /// <summary>
        /// Plus or minus 1 depending on whether the comparer compares reversly.
        /// </summary>
        private readonly int reverse;

        /// <summary>
        /// Creates a new instance of the <see cref="KeyComparer"/>.
        /// </summary>
        /// <param name="comparer">The comparer which will be used to compare keys.</param>
        /// <param name="reverse">Whether to compare reversly.</param>
        public KeyComparer(IComparer<PhpValue>/*!*/ comparer, bool reverse)
        {
            this.comparer = comparer ?? throw new ArgumentNullException("comparer");
            this.reverse = reverse ? -1 : +1;
        }

        ///// <summary>
        ///// Compares keys only. Values are not used to compare so their order will not change if sorting is stable.
        ///// </summary>
        ///// <include file='Doc/Common.xml' path='docs/method[@name="CompareEntries"]/*'/>
        //public int Compare(object keyA, object valueA, object keyB, object valueB)
        //{
        //  return reverse * comparer.Compare(keyA, keyB);
        //}

        #region IComparer<IntStringKey>

        public int Compare(IntStringKey x, IntStringKey y)
            => reverse * comparer.Compare(PhpValue.Create(x), PhpValue.Create(y));

        #endregion

        #region IComparer<KeyValuePair<IntStringKey,PhpValue>> Members

        public int Compare(KeyValuePair<IntStringKey, PhpValue> x, KeyValuePair<IntStringKey, PhpValue> y)
            => Compare(x.Key, y.Key);

        #endregion
    }

    /// <summary>
    /// Compares values of dictionary entries by specified comparer.
    /// </summary>
    public sealed class ValueComparer : IComparer<KeyValuePair<IntStringKey, PhpValue>>, IComparer<PhpValue>
    {
        /// <summary>Regular comparer.</summary>
        public static readonly ValueComparer Default = new ValueComparer(PhpComparer.Default, false);
        /// <summary>Numeric comparer.</summary>
        public static readonly ValueComparer Numeric = new ValueComparer(PhpNumericComparer.Default, false);
        /// <summary>String comparer.</summary>
        public static ValueComparer String(Context ctx) => new ValueComparer(new PhpStringComparer(ctx), false);
        /// <summary>Regular comparer with reverse order.</summary>
        public static readonly ValueComparer Reverse = new ValueComparer(PhpComparer.Default, true);
        /// <summary>Numeric comparer with reverse order.</summary>
        public static readonly ValueComparer ReverseNumeric = new ValueComparer(PhpNumericComparer.Default, true);
        /// <summary>String comparer with reverse order.</summary>
        public static ValueComparer ReverseString(Context ctx) => new ValueComparer(new PhpStringComparer(ctx), true);

        /// <summary>The comparer which will be used to compare values.</summary>
        private IComparer<PhpValue>/*!*/ comparer;

        /// <summary>Plus or minus 1 depending on whether the comparer compares reversly.</summary>
        private int reverse;

        /// <summary>
        /// Creates a new instance of the <see cref="ValueComparer"/>.
        /// </summary>
        /// <param name="comparer">The comparer which will be used to compare values.</param>
        /// <param name="reverse">Whether to compare reversly.</param>
        public ValueComparer(IComparer<PhpValue>/*!*/ comparer, bool reverse)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            this.comparer = comparer;
            this.reverse = reverse ? -1 : +1;
        }

        ///// <summary>
        ///// Compares values only. Keys are not used to compare so their order will not change if sorting is stable.
        ///// </summary>
        ///// <include file='Doc/Common.xml' path='docs/method[@name="CompareEntries"]/*'/>
        //public int Compare(object keyA,object valueA,object keyB,object valueB)
        //{
        //  return reverse*comparer.Compare(valueA,valueB);
        //}

        #region IComparer<KeyValuePair<IntStringKey,PhpValue>> Members

        public int Compare(KeyValuePair<IntStringKey, PhpValue> x, KeyValuePair<IntStringKey, PhpValue> y) => Compare(x.Value, y.Value);

        public int Compare(PhpValue x, PhpValue y) => reverse * comparer.Compare(x, y);

        #endregion
    }

    /// <summary>
    /// Compares dictionary entries using specified value and key comparers.
    /// </summary>
    public sealed class EntryComparer : IComparer<KeyValuePair<IntStringKey, PhpValue>>
    {
        private readonly IComparer<PhpValue>/*!*/ keyComparer;
        private readonly IComparer<PhpValue>/*!*/ valueComparer;
        private readonly int keyReverse;
        private readonly int valueReverse;

        /// <summary>
        /// Creates a new instance of <see cref="EntryComparer"/> with specified value and key comparers.
        /// </summary>
        /// <param name="keyComparer">The comparer used on keys.</param>
        /// <param name="keyReverse">Whether the the result of the key comparer is inversed.</param>
        /// <param name="valueComparer">The comparer used on values.</param>
        /// <param name="valueReverse">Whether the the result of the value comparer is inversed</param>
        public EntryComparer(IComparer<PhpValue>/*!*/ keyComparer, bool keyReverse, IComparer<PhpValue>/*!*/ valueComparer, bool valueReverse)
        {
            if (keyComparer == null)
                throw new ArgumentNullException("keyComparer");

            if (valueComparer == null)
                throw new ArgumentNullException("valueComparer");

            this.keyComparer = keyComparer;
            this.valueComparer = valueComparer;
            this.keyReverse = keyReverse ? -1 : +1;
            this.valueReverse = valueReverse ? -1 : +1;
        }

        ///// <summary>
        ///// Compares two entries.
        ///// </summary>
        ///// <param name="keyA">The first entry key.</param>
        ///// <param name="valueA">The first entry value.</param>
        ///// <param name="keyB">The second entry key.</param>
        ///// <param name="valueB">The second entry value.</param>
        ///// <returns>-1, 0, +1</returns>
        //public int Compare(object keyA, object valueA, object keyB, object valueB)
        //{
        //  int kcmp = keyReverse*keyComparer.Compare(keyA,keyB);
        //  if (kcmp!=0) return kcmp;
        //  return valueReverse*valueComparer.Compare(valueA,valueB);
        //}

        #region IComparer<KeyValuePair<IntStringKey,PhpValue>> Members

        public int Compare(KeyValuePair<IntStringKey, PhpValue> x, KeyValuePair<IntStringKey, PhpValue> y)
        {
            int kcmp = keyReverse * keyComparer.Compare(PhpValue.Create(x.Key), PhpValue.Create(y.Key));
            return (kcmp != 0) ? kcmp : valueReverse * valueComparer.Compare(x.Value, y.Value);
        }

        #endregion
    }

    ///// <summary>
    ///// Implements equality comparer of objects, using given <see cref="IComparer"/>.
    ///// </summary>
    //public sealed class PhpEqualityComparer : IEqualityComparer<PhpValue>
    //{
    //    /// <summary>
    //    /// <see cref="IComparer"/> to use.
    //    /// </summary>
    //    private readonly IComparer<PhpValue>/*!*/ comparer;

    //    public PhpEqualityComparer(IComparer<PhpValue>/*!*/ comparer)
    //    {
    //        this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    //    }

    //    #region IEqualityComparer<PhpValue>

    //    bool IEqualityComparer<PhpValue>.Equals(PhpValue x, PhpValue y)
    //    {
    //        return comparer.Compare(x, y) == 0;
    //    }

    //    int IEqualityComparer<PhpValue>.GetHashCode(PhpValue obj)
    //    {
    //        return obj.GetHashCode(); // NOTICE: can't return regular HashCode here, it depends on actual comparer
    //    }

    //    #endregion
    //}

    #endregion

    #region Regular Comparer

    /// <summary>
    /// Implements PHP regular comparison.
    /// </summary>
    public sealed class PhpComparer : IComparer<PhpValue>, IEqualityComparer<PhpValue>
    {
        /// <summary>Prevents from creating instances of this class.</summary>
        private PhpComparer() { }

        /// <summary>
        /// Default comparer used to compare objects where no other comparer is provided by user.
        /// </summary>
        public static readonly PhpComparer/*!*/ Default = new PhpComparer();

        public int Compare(PhpValue x, PhpValue y) => x.Compare(y);

        public bool Equals(PhpValue x, PhpValue y) => x.Equals(y);

        internal static int GetHashCodeInternal(PhpValue obj)
        {
            // in PHP, two totally different values can be considered equal,
            // therefore hashcode is tricky to resolve in order to return the same hashcode for two equal values
            // let's return certain numbers denoting values that can't be equal to each other (?)

            if (obj.IsEmpty)
            {
                return 0;
            }

            if (obj.Object is PhpResource)
            {
                return 2;
            }

            return 1; // anything else can be equal to anything
        }

        public int GetHashCode(PhpValue obj) => GetHashCodeInternal(obj);
    }

    #endregion

    #region Numeric Comparer

    /// <summary>
    /// Implements PHP numeric comparison.
    /// </summary>
    public sealed class PhpNumericComparer : IComparer<PhpValue>, IEqualityComparer<PhpValue>
    {
        /// <summary>Prevents from creating instances of this class.</summary>
        private PhpNumericComparer() { }

        /// <summary>
        /// Default comparer used to compare objects where no other comparer is provided by user.
        /// </summary>
        public static readonly PhpNumericComparer/*!*/ Default = new PhpNumericComparer();

        /// <summary>
        /// Compares two objects in a manner of PHP numeric comparison.
        /// </summary>
        public int Compare(PhpValue x, PhpValue y)
        {
            PhpNumber numx, numy;

            var info_x = x.ToNumber(out numx);
            var info_y = y.ToNumber(out numy);

            // at least one operand has been converted to double:
            if (((info_x | info_y) & Convert.NumberInfo.Double) != 0)
                return Comparison.Compare(numx.ToDouble(), numy.ToDouble());

            // compare integers:
            return Comparison.Compare(numx.ToLong(), numy.ToLong());
        }

        public bool Equals(PhpValue x, PhpValue y) => Compare(x, y) == 0;

        public int GetHashCode(PhpValue obj)
        {
            obj.ToNumber(out var num);
            return (int)num.ToLong();
        }
    }

    #endregion

    #region String Comparer

    /// <summary>
    /// Implements PHP string comparison.
    /// </summary>
    public sealed class PhpStringComparer : IComparer<PhpValue>, IEqualityComparer<PhpValue>
    {
        readonly Context _ctx;

        /// <summary>Prevents from creating instances of this class.</summary>
        public PhpStringComparer(Context ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// Compares two objects in a manner of PHP string comparison.
        /// </summary>
        public int Compare(PhpValue x, PhpValue y)
        {
            return string.CompareOrdinal(x.ToString(_ctx), y.ToString(_ctx));
        }

        public bool Equals(PhpValue x, PhpValue y) => string.Equals(x.ToString(_ctx), y.ToString(_ctx), StringComparison.Ordinal);

        public int GetHashCode(PhpValue obj) => StringComparer.Ordinal.GetHashCode(obj.ToString(_ctx));
    }

    #endregion

    #region Locale String Comparer

    /// <summary>
    /// Implements PHP locale string comparison.
    /// </summary>
    public sealed class PhpLocaleStringComparer : IComparer<PhpValue>
    {
        readonly Context _ctx;

        /// <summary>
        /// A culture used for comparison.
        /// </summary>
        public CultureInfo Culture { get { return _culture; } }
        private readonly CultureInfo _culture;

        /// <summary>
        /// Whether the comparer is ignoring case.
        /// </summary>
        public CompareOptions CompareOptions { get { return _options; } }
        private readonly CompareOptions _options;

        /// <summary>
        /// Creates a new string comparer with a specified culture.
        /// </summary>
        public PhpLocaleStringComparer(Context ctx, CultureInfo culture, CompareOptions options)
        {
            _ctx = ctx;
            _culture = culture ?? CultureInfo.InvariantCulture;
            _options = options;
        }

        /// <summary>
        /// Compares two objects in a manner of PHP string comparison.
        /// </summary>
        public int Compare(PhpValue x, PhpValue y)
        {
            return _culture.CompareInfo.Compare(x.ToString(_ctx), y.ToString(_ctx), _options);
        }
    }

    #endregion

    #region ArrayKeys Comparer

    /// <summary>
    /// Implements comparison of PHP array keys.
    /// </summary>
    public class PhpArrayKeysComparer : IComparer<PhpValue>, IComparer<IntStringKey>
    {
        /// <summary>Prevents from creating instances of this class.</summary>
        private PhpArrayKeysComparer() { }

        /// <summary>
        /// Default comparer.
        /// </summary>
        public static readonly PhpArrayKeysComparer Default = new PhpArrayKeysComparer();

        /// <summary>
        /// Compares keys of an array.
        /// </summary>
        /// <remarks>
        /// Keys are compared as strings if at least one of them is a string 
        /// otherwise they have to be integers and so they are compared as integers.
        /// </remarks>
        public int Compare(IntStringKey x, IntStringKey y)
        {
            return x.CompareTo(y);
        }

        #region IComparer Members

        public int Compare(PhpValue x, PhpValue y)
        {
            x.TryToIntStringKey(out IntStringKey xkey);
            y.TryToIntStringKey(out IntStringKey ykey);
            return Compare(xkey, ykey);
        }

        #endregion
    }

    #endregion

    #region Natural Comparer

    /// <summary>
    /// Implements PHP natural comparison.
    /// </summary>
    public sealed class PhpNaturalComparer : IComparer<PhpValue>, IEqualityComparer<PhpValue>
    {
        readonly Context _ctx;

        /// <summary>Whether comparisons will be case insensitive.</summary>
        private bool _caseInsensitive;

        /// <summary>Prevents from creating instances of this class.</summary>
        /// <param name="ctx">Current context. Cannot be <c>null</c>.</param>
        /// <param name="caseInsensitive">Whether comparisons will be case insensitive.</param>
        public PhpNaturalComparer(Context ctx, bool caseInsensitive)
        {
            _ctx = ctx;
            _caseInsensitive = caseInsensitive;
        }

        /// <summary>
        /// Compares two objects using the natural ordering.
        /// </summary>
        public int Compare(PhpValue x, PhpValue y)
        {
            return CompareStrings(x.ToString(_ctx), y.ToString(_ctx));
        }

        public int CompareStrings(string x, string y) => CompareStrings(x, y, _caseInsensitive);

        /// <summary>
		/// Compares two strings using the natural ordering.
		/// </summary>
        public static int CompareStrings(string x, string y, bool caseInsensitive)
        {
            if (x == null) x = string.Empty;
            if (y == null) y = string.Empty;

            int length_l = x.Length, length_g = y.Length;
            if (length_l == 0 || length_g == 0) return length_l - length_g;

            int i = 0, j = 0;
            do
            {
                char lc = x[i], gc = y[j];

                // skip white spaces
                if (char.IsWhiteSpace(lc))
                {
                    i++;
                    continue;
                }
                if (char.IsWhiteSpace(gc))
                {
                    j++;
                    continue;
                }

                if (char.IsDigit(lc) && char.IsDigit(gc))
                {
                    // compare numbers
                    int result = (lc == '0' || gc == '0') ? CompareLeft(x, y, ref i, ref j) :
                        CompareRight(x, y, ref i, ref j);

                    if (result != 0) return result;
                }
                else
                {
                    // compare letters
                    if (caseInsensitive)
                    {
                        lc = char.ToLowerInvariant(lc);
                        gc = char.ToLowerInvariant(gc);
                    }

                    if (lc < gc) return -1;
                    if (lc > gc) return 1;

                    i++; j++;
                }
            }
            while (i < length_l && j < length_g);

            if (i < length_l) return 1;
            if (j < length_g) return -1;
            return 0;
        }

        /// <summary>
        /// Compares two strings with left-aligned numbers, the first to have a different value wins.
        /// </summary>
        /// <param name="x">String that contains the first number.</param>
        /// <param name="y">String that contains the second number.</param>
        /// <param name="i">Index in <paramref name="x"/> where the first number begins. Is set to the index
        /// immediately following the number after returning from this method.</param>
        /// <param name="j">Index in <paramref name="y"/> where the second number begins. Is set to the index
        /// immediately following the number after returning from this method.</param>
        /// <returns>
        /// Negative integer if the first number is less than the second number, 
        /// zero if the two numbers are equal and
        /// positive integer if the first number is greater than the second number.</returns>
        /// <remarks>Assumes neither <paramref name="x"/> nor <paramref name="y"/> parameter is null.</remarks>
        private static int CompareLeft(string x, string y, ref int i, ref int j)
        {
            Debug.Assert(x != null && y != null);

            int length_l = x.Length, length_g = y.Length;

            while (true)
            {
                bool bl = (i == length_l || !char.IsDigit(x[i]));
                bool bg = (j == length_g || !char.IsDigit(y[j]));

                if (bl && bg) return 0;
                if (bl) return -1;
                if (bg) return 1;

                if (x[i] < y[j]) return -1;
                if (x[i] > y[j]) return 1;

                i++; j++;
            }
        }

        /// <summary>
        /// Compares two strings with right-aligned numbers, The longest run of digits wins.
        /// </summary>
        /// <param name="x">String that contains the first number.</param>
        /// <param name="y">String that contains the second number.</param>
        /// <param name="i">Index in <paramref name="x"/> where the first number begins. Is set to the index
        /// immediately following the number after returning from this method.</param>
        /// <param name="j">Index in <paramref name="y"/> where the second number begins. Is set to the index
        /// immediately following the number after returning from this method.</param>
        /// <returns>
        /// Negative integer if the first number is less than the second number, 
        /// zero if the two numbers are equal and
        /// positive integer if the first number is greater than the second number.</returns>
        /// <remarks>Assumes neither <paramref name="x"/> nor <paramref name="y"/> parameter is null.</remarks>
        internal static int CompareRight(string x, string y, ref int i, ref int j)
        {
            Debug.Assert(x != null && y != null);

            int length_l = x.Length, length_g = y.Length;

            // That aside, the greatest value wins, but we can't know that it will until we've scanned both numbers to
            // know that they have the same magnitude, so we remember it in "bias".
            int bias = 0;

            while (true)
            {
                bool bl = (i == length_l || !Char.IsDigit(x[i]));
                bool bg = (j == length_g || !Char.IsDigit(y[j]));

                if (bl && bg) return bias;
                if (bl) return -1;
                if (bg) return 1;

                if (x[i] < y[j])
                {
                    if (bias == 0) bias = -1;
                }
                else if (x[i] > y[j])
                {
                    if (bias == 0) bias = 1;
                }

                i++; j++;
            }
        }

        public bool Equals(PhpValue x, PhpValue y) => Compare(x, y) == 0;

        public int GetHashCode(PhpValue obj)
        {
            // take only letters into account,
            // ignoring case

            int hashcode = 0;

            var str = obj.ToString(_ctx);

            for (int i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                if (char.IsWhiteSpace(ch) || char.IsDigit(ch))
                {
                    continue;
                }

                hashcode ^= ((int)char.ToLowerInvariant(ch) << (i % 32));
            }

            return hashcode;
        }
    }

    #endregion

    #region User Comparer

    /// <summary>
    /// Implements PHP numeric comparison.
    /// </summary>
    public sealed class PhpUserComparer : IComparer<PhpValue>
    {
        /// <summary>User defined PHP method used to compare given objects.</summary>
        readonly IPhpCallable _compare;

        readonly Context _ctx;

        /// <summary>
        /// Creates a new instance of a comparer using <see cref="PhpCallback"/> for comparisons.
        /// </summary>
        /// <param name="ctx">Current context. Cannot be <c>null</c>.</param>
        /// <param name="compare">User callback which provides comparing functionality.</param>
        /// <remarks>
        /// <para>
        /// Callback should have the signature <c>object(object,object)</c> and should already be bound.
        /// </para>
        /// <para>
        /// The result of calback's invocation is converted to a double by <see cref="PhpValue.ToDouble"/>
        /// and than the sign is taken as a result of the comparison.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="compare"/> is a <B>null</B> reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="compare"/> callback is not bound.</exception>
        public PhpUserComparer(Context ctx, IPhpCallable compare)
        {
            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            _compare = compare;
            _ctx = ctx;
        }

        /// <summary>
        /// Compares two objects in a manner of PHP numeric comparison.
        /// </summary>
        public int Compare(PhpValue x, PhpValue y)
        {
            return Comparison.Compare(_compare.Invoke(_ctx, x, y).ToDouble(), 0.0);
        }
    }

    #endregion
}
