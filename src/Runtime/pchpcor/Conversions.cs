using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        //PhpBytes ToBinaryString();

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
        public static string ToString(bool value)
        {
            return value ? "1" : string.Empty;
        }

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
        public static string ToString(double value, Context ctx)
        {
            // TODO: according to ctx culture
            return value.ToString("G", NumberFormatInfo.InvariantInfo);
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
        /// Converts value to boolean according to PHP.
        /// </summary>
        public static bool ToBoolean(PhpValue value) => value.ToBoolean();

        /// <summary>
        /// Converts class instance to boolean according to PHP.
        /// </summary>
        public static bool ToBoolean(object value)
        {
            var convertible = value as IPhpConvertible;
            return value != null && (convertible == null || convertible.ToBoolean());
        }

        #endregion

        #region AsObject, AsArray, ToObject

        /// <summary>
        /// Gets underlaying class instance or <c>null</c>.
        /// </summary>
        public static object AsObject(PhpValue value) => value.AsObject();

        /// <summary>
        /// Gets the array access object.
        /// </summary>
        public static object AsArray(PhpValue value) => value.AsArray();

        /// <summary>
        /// Converts given value to a class object.
        /// </summary>
        public static object ToClass(PhpValue value) => value.ToClass();

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

        #endregion

        #region ToIntStringKey

        /// <summary>
        /// Converts given value to an array key.
        /// </summary>
        public static IntStringKey ToIntStringKey(PhpValue value) => value.ToIntStringKey();

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

        #endregion
    }

    #endregion
}
