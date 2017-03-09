using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Core
{
    #region IPhpConvertible

    /// <summary>
    /// Interface provides methods for conversion between PHP.NET types.
    /// </summary>
    public interface IPhpConvertible
    {
        /// <summary>
        /// Gets the object type code.
        /// </summary>
        PhpTypeCode TypeCode { get; }

        /// <summary>
        /// Converts the object to <see cref="double"/>.
        /// </summary>
        /// <returns></returns>
        double ToDouble();

        /// <summary>
        /// Converts the object to <see cref="long"/>.
        /// </summary>
        /// <returns></returns>
        long ToLong();

        /// <summary>
        /// Converts the object to <see cref="bool"/>.
        /// </summary>
        /// <returns></returns>
        bool ToBoolean();

        //byte[] ToBytes();

        /// <summary>
        /// Converts the object to a number.
        /// </summary>
        Convert.NumberInfo ToNumber(out PhpNumber number);

        /// <summary>
        /// Converts instance to its string representation according to PHP conversion algorithm.
        /// </summary>
        string ToString(Context ctx);

        /// <summary>
		/// Converts instance to its string representation according to PHP conversion algorithm.
		/// </summary>
		string ToStringOrThrow(Context ctx);

        /// <summary>
        /// In case of a non class object, boxes value to an object.
        /// </summary>
        object ToClass();
    }

    #endregion

    #region Convert

    [DebuggerNonUserCode]
    public static class Convert
    {
        #region ToString

        /// <summary>
        /// Gets string representation of a boolean value (according to PHP, it is <c>"1"</c> or <c>""</c>).
        /// </summary>
        public static string ToString(bool value) => value ? "1" : string.Empty;

        /// <summary>
        /// Gets string representation of an integer value.
        /// </summary>
        public static string ToString(long value) => value.ToString();

        /// <summary>
        /// Gets string representation of an integer value.
        /// </summary>
        public static string ToString(int value) => value.ToString();

        /// <summary>
        /// Gets string representation of a floating point number value.
        /// </summary>
        public static string ToString(double value) => value.ToString("G", NumberFormatInfo.InvariantInfo);

        /// <summary>
        /// Gets string representation of a floating point number value.
        /// </summary>
        public static string ToString(double value, Context ctx) => value.ToString("G", ctx.NumberFormat);

        /// <summary>
        /// Converts class instance to a string.
        /// </summary>
        public static string ToStringOrThrow(object value, Context ctx)
        {
            if (value is IPhpConvertible)   // TODO: should be sufficient to call just ToString(), implementations of IPhpConvertible override ToString always
            {
                return ((IPhpConvertible)value).ToStringOrThrow(ctx);
            }
            else
            {
                return value.ToString();
            }
        }

        #endregion

        #region ToBoolean

        /// <summary>
        /// Converts string to boolean according to PHP.
        /// </summary>
        public static bool ToBoolean(string value)
        {
            // not null
            // not empty
            // not "0"
            return value != null && value.Length != 0 && (value.Length != 1 || value[0] != '0');
        }

        /// <summary>
        /// Converts string to boolean according to PHP.
        /// </summary>
        public static bool ToBoolean(byte[] value)
        {
            // not null
            // not empty
            // not "0"
            return value != null && value.Length != 0 && (value.Length != 1 || value[0] != '0');
        }

        /// <summary>
        /// Converts string to boolean according to PHP.
        /// </summary>
        public static bool ToBoolean(char[] value)
        {
            // not null
            // not empty
            // not "0"
            return value != null && value.Length != 0 && (value.Length != 1 || value[0] != '0');
        }

        /// <summary>
        /// Converts class instance to boolean according to PHP.
        /// </summary>
        public static bool ToBoolean(object value)
        {
            IPhpConvertible convertible;
            return value != null && ((convertible = value as IPhpConvertible) == null || convertible.ToBoolean());
        }

        /// <summary>
        /// Converts to boolean according to PHP.
        /// </summary>
        public static bool ToBoolean(IPhpConvertible value) => value != null && value.ToBoolean();

        #endregion

        #region AsObject, ToArray, ToPhpString, ToClass, AsCallable

        /// <summary>
        /// Gets underlaying class instance or <c>null</c>.
        /// </summary>
        public static object AsObject(PhpValue value) => value.AsObject();

        /// <summary>
        /// Converts value to an array.
        /// </summary>
        public static PhpArray ToArray(PhpValue value) => value.ToArray();

        /// <summary>
        /// Casts value to <see cref="PhpArray"/> or <c>null</c>.
        /// </summary>
        public static PhpArray AsArray(PhpValue value) => value.AsArray();
        /// <summary>
        /// Creates <see cref="PhpArray"/> from object's properties.
        /// </summary>
        /// <param name="obj">Object instance.</param>
        /// <returns>Array containing given object properties keyed according to PHP specifications.</returns>
        public static PhpArray ClassToArray(object obj)
        {
            if (object.ReferenceEquals(obj, null))
            {
                return PhpArray.NewEmpty();
            }
            else if (obj.GetType() == typeof(stdClass))
            {
                // special case,
                // object is stdClass, we can simply copy its runtime fields
                return ((stdClass)obj).GetRuntimeFields().DeepCopy();
            }
            else
            {
                if (obj is Array)
                {
                    // [] -> array
                    return new PhpArray((Array)obj);
                }
                // TODO: IList
                else
                {
                    // obj -> array
                    var arr = new PhpArray();
                    TypeMembersUtils.InstanceFieldsToPhpArray(obj, arr);
                    return arr;
                }
            }
        }

        /// <summary>
        /// Gets the underlying PHP string if present, a new PHP string representing the value otherwise.
        /// </summary>
        public static PhpString ToPhpString(PhpValue value, Context ctx)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Alias:
                    return ToPhpString(value.Alias.Value, ctx);

                case PhpTypeCode.WritableString:
                    return value.WritableString;
                case PhpTypeCode.String:
                    return new PhpString(value.String);
                default:
                    return new PhpString(value.ToString(ctx));
            }
        }

        /// <summary>
        /// Converts given value to a class object.
        /// </summary>
        public static object ToClass(PhpValue value) => value.ToClass();

        /// <summary>
        /// Converts given array object to <see cref="stdClass"/>.
        /// </summary>
        /// <param name="array">Non-null reference to a PHP array.</param>
        /// <returns>New instance of <see cref="stdClass"/> with runtime fields filled from given array.</returns>
        public static object ToClass(IPhpArray array)
        {
            var convertible = array as IPhpConvertible;
            if (convertible != null)
            {
                return convertible.ToClass();
            }
            else
            {
                //return new stdClass()
                //{
                //    __peach__runtimeFields = new PhpArray(array);
                //};
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets value as a callable object that can be invoked dynamically.
        /// </summary>
        public static IPhpCallable AsCallable(PhpValue value) => value.AsCallable();

        /// <summary>
        /// Creates a callable object from string value.
        /// </summary>
        public static IPhpCallable AsCallable(string value) => PhpCallback.Create(value);

        /// <summary>
        /// Resolves whether given instance <paramref name="obj"/> is of given type <paramref name="tinfo"/>.
        /// </summary>
        /// <param name="obj">Value to be checked.</param>
        /// <param name="tinfo">Type descriptor.</param>
        /// <returns>Whether <paramref name="obj"/> is of type <paramref name="tinfo"/>.</returns>
        public static bool IsInstanceOf(object obj, Reflection.PhpTypeInfo tinfo)
        {
            // note: if tinfo is null =>
            // type was not declared =>
            // value cannot be its instance because there is no way how to instantiate it
            // ignoring the case when object is passed from CLR

            return obj != null && tinfo != null && tinfo.Type.IsInstanceOfType(obj);
        }

        #endregion

        #region ToNumber

        public static NumberInfo ToNumber(string str, out PhpNumber number)
        {
            long l;
            double d;
            var info = StringToNumber(str, out l, out d);
            number = ((info & NumberInfo.Double) != 0) ? PhpNumber.Create(d) : PhpNumber.Create(l);
            return info;
        }

        /// <summary>
        /// Converts given string to a number.
        /// </summary>
        public static PhpNumber ToNumber(string str)
        {
            long l;
            double d;
            var info = StringToNumber(str, out l, out d);
            return ((info & NumberInfo.Double) != 0) ? PhpNumber.Create(d) : PhpNumber.Create(l);
        }

        /// <summary>
        /// Performs conversion of a value to a number.
        /// Additional conversion warnings may be thrown.
        /// </summary>
        public static PhpNumber ToNumber(PhpValue value)
        {
            PhpNumber n;
            if ((value.ToNumber(out n) & NumberInfo.IsNumber) == 0)
            {
                // TODO: Err
            }

            return n;
        }

        #endregion

        #region ToIntStringKey

        /// <summary>
        /// Converts given value to an array key.
        /// </summary>
        public static IntStringKey ToIntStringKey(PhpValue value) => value.ToIntStringKey();

        /// <summary>
        /// Tries conversion to an array key.
        /// </summary>
        public static bool TryToIntStringKey(PhpValue value, out IntStringKey key)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                case PhpTypeCode.String:
                case PhpTypeCode.WritableString:
                case PhpTypeCode.Boolean:
                    key = value.ToIntStringKey();
                    return true;

                case PhpTypeCode.Alias:
                    return TryToIntStringKey(value.Alias.Value, out key);

                default:
                    key = default(IntStringKey);
                    return false;
            }
        }

        /// <summary>
		/// Converts a string to an appropriate integer.
		/// </summary>
		/// <param name="str">The string in "{0 | -?[1-9][0-9]*}" format.</param>
		/// <returns>The array key (integer or string).</returns>
		public static IntStringKey StringToArrayKey(string/*!*/str)
        {
            Debug.Assert(str != null, "str == null");

            // empty string:
            if (str.Length == 0)
                return new IntStringKey(string.Empty);

            // starts with minus sign?
            bool sign = false;
            int index = 0;

            // check first character:
            switch (str[0])
            {
                case '-':
                    // negative number starting with zero is always a string key (-0, -0123)
                    if (str.Length == 1 || str[1] == '0')    // str = "-" or '-0' or '-0...'
                        return new IntStringKey(str);

                    // str = "-..." // continue to <int> parsing
                    index = 1;
                    sign = true;
                    break;

                case '0':
                    // (non-negative) number starting with '0' is considered as a string,
                    // iff there is more than just a '0'
                    if (str.Length == 1)
                        return new IntStringKey(0); // just a zero -> convert to int
                    else
                        return new IntStringKey(str);   // number starting with zero -> string key
            }

            Debug.Assert(index < str.Length, "str == {" + str + "}");

            // simple <int> parser:
            long result = (int)str[index] - '0';
            Debug.Assert(result != 0, "str == {" + str + "}");

            if (result < 0 || result > 9)   // not a number
                return new IntStringKey(str);

            while (++index < str.Length)
            {
                int c = (int)str[index] - '0';
                if (c >= 0 && c <= 9)
                {
                    // update <result>
                    result = unchecked(c + result * 10);

                    // <int> range check
                    if (Utilities.NumberUtils.IsInt32(result))
                        continue;   // still in <int> range
                }

                //
                return new IntStringKey(str);
            }

            if (sign)
            {
                result = -result;
                if (!Utilities.NumberUtils.IsInt32(result))
                    return new IntStringKey(str);
            }

            // <int> parsed properly:
            return new IntStringKey(unchecked((int)result));
        }

        #endregion

        #region String To Number

        /// <summary>
        /// A result of number conversion algorithm.
        /// </summary>
        [Flags]
        public enum NumberInfo
        {
            LongInteger = 2,
            Double = 4,
            Unconvertible = 16,

            TypeMask = LongInteger | Double | Unconvertible,

            IsNumber = 64,
            IsHexadecimal = 128,

            /// <summary>
            /// The original object was PHP array. This has an effect on most PHP arithmetic operators.
            /// </summary>
            IsPhpArray = 256,
        }

        /// <summary>
        /// Converts a character to a digit.
        /// </summary>
        /// <param name="c">The character [0-9A-Za-z].</param>
        /// <returns>The digit represented by the character or <see cref="Int32.MaxValue"/> 
        /// on non-alpha-numeric characters.</returns>
        public static int AlphaNumericToDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return (int)(c - '0');

            if (c >= 'a' && c <= 'z')
                return (int)(c - 'a') + 10;

            if (c >= 'A' && c <= 'Z')
                return (int)(c - 'A') + 10;

            return Int32.MaxValue;
        }

        /// <summary>
		/// Converts a string to integer value and double value and decides whether it represents a number as a whole.
		/// </summary>
		/// <param name="s">The string to convert.</param>
		/// <param name="limit">Maximum zero-based index within given <paramref name="s"/> to be proccessed.
        /// Must be greater than or equal <c>0</c> and less than or equal to string length.</param>
        /// <param name="from">
		/// A position where to start parsing.
		/// </param>
		/// <param name="l">
		/// Returns a position where long-integer-parsing ended 
		/// (the first character not included in the resulting double value).
		/// </param>
		/// <param name="d">
		/// Returns a position where double-parsing ended
		/// (the first character not included in the resulting double value).
		/// </param>
		/// <param name="longValue">Result of the conversion to long integer.</param>
		/// <param name="doubleValue">Result of the conversion to double.</param>
		/// <returns>
		/// Information about parsed number including its type, which is the narrowest one that fits.
		/// E.g. 
		/// IsNumber("10 xyz", ...) includes NumberInfo.Integer,
		/// IsNumber("10000000000000 xyz", ...) includes NumberInfo.LongInteger,
		/// IsNumber("10.9 xyz", ...) includes NumberInfo.Double,
		/// IsNumber("10.9", ...) includes NumberInfo.IsNumber and NumberInfo.Double.
		/// 
		/// The return value always includes one of NumberInfo.Integer, NumberInfo.LongInteger, NumberInfo.Double
		/// and never NumberInfo.Unconvertible (as each string is convertible to a number).
		/// </returns>
        internal static NumberInfo IsNumber(string s, int limit, int from,
            out int l, out int d,
            out long longValue, out double doubleValue)
        {
            if (string.IsNullOrEmpty(s))
            {
                l = d = 0;
                longValue = 0;
                doubleValue = 0.0;
                return NumberInfo.LongInteger;
            }

            // invariant after return: 0 <= i <= l <= d <= p <= old(p) + length - 1.
            NumberInfo result = 0;

            Debug.Assert(from >= 0);
            //if (from < 0) from = 0;

            //Debug.Assert(length >= 0 && length <= s.Length - from);
            //if (length < 0 || length > s.Length - from) length = s.Length - from;

            Debug.Assert(limit >= from && limit <= s.Length);
            //int limit = from + length;

            // long:
            longValue = 0;                      // long integer value of already read part of the string
            l = -1;                             // last position of an long integer part of the string

            // double:
            doubleValue = 0.0;                  // double value; initialized at the end
            d = -1;                             // last position where the double has ended
            int e = -1;                         // position where the exponent has started by 'e', 'E', 'd', or 'D'

            // common:
            bool contains_digit = false;        // whether a digit is contained in the string (in the integral and fraction part of the nummber, not an exponent)
            bool sign = false;                  // whether a sign of whole number is minus
            int state = 0;                      // automaton state
            int p = from;                       // current index within parsed string

            // patterns and states:
            // [:white:]*[+-]?0?[0-9]*[.]?[0-9]*([eE][+-]?[0-9]+)?
            //  0000000   11  2  222   2   333    4444  55   666     
            // [:white:]*[+-]?0(x|X)[0-9A-Fa-f]*    // TODO: PHP does not resolve [+-] at the beginning, however Phalanger does
            //  0000000   11  2 777  888888888  

            while (p < limit)
            {
                char c = s[p];  // TODO: *fixed, no range check

                switch (state)
                {
                    case 0: // expecting whitespaces to be skipped
                        {
                            if (!Char.IsWhiteSpace(c))
                            {
                                state = 1;
                                goto case 1;
                            }
                            break;
                        }

                    case 1: // expecting result + or - or .
                        {
                            if (c >= '0' && c <= '9')
                            {
                                state = 2;
                                goto case 2;
                            }

                            if (c == '-')
                            {
                                sign = true;// -1;
                                state = 2;
                                break;
                            }

                            if (c == '+')
                            {
                                state = 2;
                                break;
                            }

                            // ends reading (long) integer:
                            l = p;
                            // doubleValue = 0.0; // already zeroed

                            // switch to decimals in next turn:
                            if (c == '.')
                            {
                                state = 3;
                                break;
                            }

                            // unexpected character:
                            goto Done;
                        }

                    case 2: // expecting result
                        {
                            Debug.Assert(l == -1, "Reading long.");

                            // a single leading zero:
                            if (c == '0' && !contains_digit)
                            {
                                contains_digit = true;
                                state = 7;
                                break;
                            }

                            if (c >= '0' && c <= '9')
                            {
                                int num = (int)(c - '0');
                                contains_digit = true;

                                if (longValue < Int64.MaxValue / 10 || (longValue == Int64.MaxValue / 10 && num <= Int64.MaxValue % 10))
                                {
                                    // still fits long
                                    longValue = longValue * 10 + num;
                                    break;
                                }
                                else
                                {
                                    // long not big enough ...

                                    // last long integer position:
                                    l = p;

                                    // fix for long.MinValue (which integral part cannot be hold as position long)
                                    if (sign && num == -(Int64.MinValue % 10))
                                    {
                                        // parsed number is still valid long (Int64.MinValue)
                                        ++l; // move the long position after this character
                                    }

                                    longValue = sign ? Int64.MinValue : Int64.MaxValue;

                                    // continue reading as double:
                                    state = 3;   // => doubleValue will be initialized at the end
                                    break;
                                }
                            }

                            // ends reading (long) integer:

                            // last long integer position:
                            l = p;
                            if (sign) longValue *= -1;

                            // switch to decimals in next turn:
                            if (c == '.')
                            {
                                state = 3;  // => doubleValue will be initialized at the end
                                break;
                            }

                            // switch to exponent in next turn:
                            if ((c == 'e' || c == 'E') && contains_digit)
                            {
                                e = p;
                                state = 4;  // => doubleValue will be initialized at the end
                                break;
                            }

                            doubleValue = unchecked((double)longValue);

                            // unexpected character:
                            goto Done;
                        }

                    case 3: // expecting decimals
                        {
                            Debug.Assert(l >= 0, "Reading double.");

                            // reading decimals:
                            if (c >= '0' && c <= '9')
                            {
                                contains_digit = true;
                                break;
                            }

                            // switch to exponent in next turn:
                            if ((c == 'e' || c == 'E') && contains_digit)
                            {
                                e = p;
                                state = 4;
                                break;
                            }

                            // unexpected character:
                            goto Done;
                        }

                    case 4: // expecting exponent + or -
                        {
                            Debug.Assert(l >= 0, "Reading double.");

                            // switch to exponent immediately:
                            if (c >= '0' && c <= '9')
                            {
                                state = 6;
                                goto case 6;
                            }

                            // switch to exponent in next turn:
                            if (c == '-')
                            {
                                //expBase = 0.1;
                                state = 5;
                                break;
                            }

                            // switch to exponent in next turn:
                            if (c == '+')
                            {
                                state = 5;
                                break;
                            }

                            // unexpected characters:
                            goto Done;
                        }

                    case 5: // expecting exponent after the sign
                        {
                            state = 6;
                            goto case 6;
                        }

                    case 6: // expecting exponent without the sign
                        {
                            if (c >= '0' && c <= '9')
                            {
                                break;
                            }

                            // unexpected character:
                            goto Done;
                        }

                    case 7: // a single leading zero read:
                        {
                            // check for hexa integer:
                            if (c == 'x' || c == 'X')
                            {
                                // end of double reading:
                                d = p;

                                state = 8;
                                break;
                            }

                            // other cases -> back to integer reading:
                            state = 2;
                            goto case 2;
                        }

                    case 8: // hexa integer
                        {
                            result |= NumberInfo.IsHexadecimal;

                            int num = AlphaNumericToDigit(c);

                            // unexpected character:
                            if (num <= 15)
                            {
                                if (l == -1)
                                {
                                    if (longValue < Int64.MaxValue / 16 || (longValue == Int64.MaxValue / 16 && num <= Int64.MaxValue % 16))
                                    {
                                        longValue = longValue * 16 + num;
                                        break;
                                    }
                                    else
                                    {
                                        // last hexa long integer position:
                                        doubleValue = unchecked((double)longValue);
                                        if (sign)
                                        {
                                            doubleValue = unchecked(-doubleValue);
                                            longValue = Int64.MinValue;
                                        }
                                        else
                                        {
                                            longValue = Int64.MaxValue;
                                        }
                                        // fallback to double behaviour below...
                                    }
                                }

                                l = p;  // last position is advanced even the long is too long?
                                doubleValue = unchecked(doubleValue * 16.0 + (double)num);

                                break;
                            }

                            goto Done;
                        }
                }
                p++;
            }

            Done:

            // an exponent ends with 'e', 'E', '-', or '+':
            if (state == 4 || state == 5)
            {
                Debug.Assert(l >= 0 && e >= 0, "Reading exponent of double.");

                // shift back:
                p = e;
                state = 3;
            }

            // if long/integer index hasn't stopped:
            // - the sign hasn't been applied yet
            // - doubleValue hasn't been initialized yet
            if (l == -1)
            {
                l = p;

                if (sign)
                    longValue = unchecked(-longValue);

                doubleValue = unchecked((double)longValue);
            }

            //
            result |= NumberInfo.LongInteger;
            
            // double parsing states
            if (state >= 3 && state <= 6)
            {
                Debug.Assert(p >= from);            // something was parsed
                Debug.Assert(doubleValue == 0.0);   // doubleValue not changed yet

                if (contains_digit) // otherwise 0.0
                    ParseDouble((from == 0 && p == s.Length) ? s : s.Substring(from, p - from), sign, out doubleValue);
            }

            // if double index hasn't stopped:
            if (d == -1)
            {
                // last double value position:
                d = p;
            }

            // determine the double type comparing strictly d, l:
            if (d > l)
                result = result & ~NumberInfo.TypeMask | NumberInfo.Double;  // remove LongInteger, add Double

            // the string is a number if it was entirely parsed and contains a digit:
            if (contains_digit && p == limit)
                result |= NumberInfo.IsNumber;

            //
            return result;
        }

        /// <summary>
        /// Parses given string as a <see cref="Double"/>, using invariant culture and proper number styles.
        /// </summary>
        private static void ParseDouble(string str, bool sign, out double doubleValue)
        {
            Debug.Assert(str != null);

            if (!double.TryParse(
                str,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingWhite,
                CultureInfo.InvariantCulture,
                out doubleValue))
            {
                // overflow: (the only other fail would be format exception which is not possible)
                //#if DEBUG
                //                try { double.Parse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingWhite, CultureInfo.InvariantCulture); }
                //                catch (OverflowException) { /* expected */ }
                //                catch { Debug.Fail("Unexpected double.Parse() exception!"); }
                //#endif
                doubleValue = sign ? double.NegativeInfinity : double.PositiveInfinity;
            }
        }

        /// <summary>
		/// Converts string into integer, long integer and double value using conversion algorithm in a manner of PHP. 
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <param name="longValue">The result of conversion to long integer.</param>
		/// <param name="doubleValue">The result of conversion to double.</param>
		/// <returns><see cref="NumberInfo"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="str"/> is a <B>null</B> reference.</exception>
		public static NumberInfo StringToNumber(string str, out long longValue, out double doubleValue)
        {
            int l, d;
            return IsNumber(str, (str != null) ? str.Length : 0, 0, out l, out d, out longValue, out doubleValue);
        }

        /// <summary>
        /// Converts a string to long integer using conversion algorithm in a manner of PHP.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The result of conversion.</returns>
        public static long StringToLongInteger(string str)
        {
            int l, d;
            double dval;
            long lval;
            IsNumber(str, (str != null) ? str.Length : 0, 0, out l, out d, out lval, out dval);

            return lval;
        }

        /// <summary>
        /// Converts a string to double using conversion algorithm in a manner of PHP.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The result of conversion.</returns>
        public static double StringToDouble(string str)
        {
            int l, d;
            double dval;
            long lval;
            IsNumber(str, (str != null) ? str.Length : 0, 0, out l, out d, out lval, out dval);

            return dval;
        }

        /// <summary>
        /// Converts a part of a string starting on a specified position to a long integer.
        /// </summary>
        /// <param name="str">The string to be parsed.</param>
        /// <param name="length">Maximal length of the substring to parse.</param>
        /// <param name="position">
        /// The position where to start. Points to the first character after the substring storing the integer
        /// when returned.
        /// </param>
        /// <returns>The integer stored in the <paramref name="str"/>.</returns>
        public static long SubstringToLongInteger(string str, int length, ref int position)
        {
            int d;
            long lval;
            double dval;
            IsNumber(str, position + length, position, out position, out d, out lval, out dval);

            return lval;
        }

        /// <summary>
        /// Converts a part of a string starting on a specified position to a double.
        /// </summary>
        /// <param name="str">The string to be parsed. Cannot be <c>null</c>.</param>
        /// <param name="length">Maximal length of the substring to parse.</param>
        /// <param name="position">
        /// The position where to start. Points to the first character after the substring storing the double
        /// when returned.
        /// </param>
        /// <returns>The double stored in the <paramref name="str"/>.</returns>
        public static double SubstringToDouble(string/*!*/str, int length, ref int position)
        {
            Debug.Assert(str != null && position + length <= str.Length);

            int l;
            long lval;
            double dval;
            IsNumber(str, position + length, position, out l, out position, out lval, out dval);

            return dval;
        }

        /// <summary>
		/// Converts a substring to almost long integer in a specified base.
		/// Stops parsing if result overflows unsigned integer.
		/// </summary>
		public static long SubstringToLongStrict(string str, int length, int @base, long maxValue, ref int position)
        {
            if (maxValue <= 0)
                throw new ArgumentOutOfRangeException("maxValue");

            if (@base < 2 || @base > 'Z' - 'A' + 1)
            {
                throw new ArgumentException(Resources.ErrResources.invalid_base, nameof(@base));
            }

            if (str == null) str = "";
            if (position < 0) position = 0;
            if (length < 0 || length > str.Length - position) length = str.Length - position;
            if (length == 0) return 0;

            long result = 0;
            int sign = +1;

            // reads a sign:
            if (str[position] == '+')
            {
                position++;
                length--;
            }
            else if (str[position] == '-')
            {
                position++;
                length--;
                sign = -1;
            }

            long max_div, max_rem;
            max_div = Utilities.NumberUtils.DivRem(maxValue, @base, out max_rem);

            while (length-- > 0)
            {
                int digit = AlphaNumericToDigit(str[position]);
                if (digit >= @base) break;

                if (!(result < max_div || (result == max_div && digit <= max_rem)))
                {
                    // reads remaining digits:
                    while (length-- > 0 && AlphaNumericToDigit(str[position]) < @base) position++;

                    return (sign == -1) ? Int64.MinValue : Int64.MaxValue;
                }

                result = result * @base + digit;
                position++;
            }

            return result * sign;
        }

        #endregion
    }

    #endregion
}
