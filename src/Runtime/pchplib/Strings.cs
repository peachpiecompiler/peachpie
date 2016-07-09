using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class Strings
    {
        #region strrev, strspn, strcspn

        /// <summary>
        /// Reverses the given string.
        /// </summary>
        /// <param name="str">The string to be reversed.</param>
        /// <returns>The reversed string or empty string if <paramref name="str"/> is null.</returns>
        public static string strrev(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            //
            var chars = str.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        ///// <summary>
        ///// Finds a length of an initial segment consisting entirely of specified characters.
        ///// </summary>
        ///// <param name="str">The string to be searched in.</param>
        ///// <param name="acceptedChars">Accepted characters.</param>
        ///// <returns>
        ///// The length of the initial segment consisting entirely of characters in <paramref name="acceptedChars"/>
        ///// or zero if any argument is null.
        ///// </returns>
        //[ImplementsFunction("strspn")]
        //public static int StrSpn(string str, string acceptedChars)
        //{
        //    return StrSpnInternal(str, acceptedChars, 0, int.MaxValue, false);
        //}

        ///// <summary>
        ///// Finds a length of a segment consisting entirely of specified characters.
        ///// </summary>
        ///// <param name="str">The string to be searched in.</param>
        ///// <param name="acceptedChars">Accepted characters.</param>
        ///// <param name="offset">The relativized offset of the first item of the slice.</param>
        ///// <returns>
        ///// The length of the substring consisting entirely of characters in <paramref name="acceptedChars"/> or 
        ///// zero if any argument is null. Search starts from absolutized <paramref name="offset"/>
        ///// (see <see cref="PhpMath.AbsolutizeRange"/> where <c>length</c> is infinity).
        ///// </returns>
        //[ImplementsFunction("strspn")]
        //public static int StrSpn(string str, string acceptedChars, int offset)
        //{
        //    return StrSpnInternal(str, acceptedChars, offset, int.MaxValue, false);
        //}

        ///// <summary>
        ///// Finds a length of a segment consisting entirely of specified characters.
        ///// </summary>
        ///// <param name="str">The string to be searched in.</param>
        ///// <param name="acceptedChars">Accepted characters.</param>
        ///// <param name="offset">The relativized offset of the first item of the slice.</param>
        ///// <param name="length">The relativized length of the slice.</param>
        ///// <returns>
        ///// The length of the substring consisting entirely of characters in <paramref name="acceptedChars"/> or 
        ///// zero if any argument is null. Search starts from absolutized <paramref name="offset"/>
        ///// (see <see cref="PhpMath.AbsolutizeRange"/> and takes at most absolutized <paramref name="length"/> characters.
        ///// </returns>
        //[ImplementsFunction("strspn")]
        //public static int StrSpn(string str, string acceptedChars, int offset, int length)
        //{
        //    return StrSpnInternal(str, acceptedChars, offset, length, false);
        //}

        ///// <summary>
        ///// Finds a length of an initial segment consisting entirely of any characters excpept for specified ones.
        ///// </summary>
        ///// <param name="str">The string to be searched in.</param>
        ///// <param name="acceptedChars">Accepted characters.</param>
        ///// <returns>
        ///// The length of the initial segment consisting entirely of characters not in <paramref name="acceptedChars"/>
        ///// or zero if any argument is null.
        ///// </returns>
        //[ImplementsFunction("strcspn")]
        //public static int StrCSpn(string str, string acceptedChars)
        //{
        //    return StrSpnInternal(str, acceptedChars, 0, int.MaxValue, true);
        //}

        ///// <summary>
        ///// Finds a length of a segment consisting entirely of any characters excpept for specified ones.
        ///// </summary>
        ///// <param name="str">The string to be searched in.</param>
        ///// <param name="acceptedChars">Accepted characters.</param>
        ///// <param name="offset">The relativized offset of the first item of the slice.</param>
        ///// <returns>
        ///// The length of the substring consisting entirely of characters not in <paramref name="acceptedChars"/> or 
        ///// zero if any argument is null. Search starts from absolutized <paramref name="offset"/>
        ///// (see <see cref="PhpMath.AbsolutizeRange"/> where <c>length</c> is infinity).
        ///// </returns>
        //[ImplementsFunction("strcspn")]
        //public static int StrCSpn(string str, string acceptedChars, int offset)
        //{
        //    return StrSpnInternal(str, acceptedChars, offset, int.MaxValue, true);
        //}

        ///// <summary>
        ///// Finds a length of a segment consisting entirely of any characters except for specified ones.
        ///// </summary>
        ///// <param name="str">The string to be searched in.</param>
        ///// <param name="acceptedChars">Accepted characters.</param>
        ///// <param name="offset">The relativized offset of the first item of the slice.</param>
        ///// <param name="length">The relativized length of the slice.</param>
        ///// <returns>
        ///// The length of the substring consisting entirely of characters not in <paramref name="acceptedChars"/> or 
        ///// zero if any argument is null. Search starts from absolutized <paramref name="offset"/>
        ///// (see <see cref="PhpMath.AbsolutizeRange"/> and takes at most absolutized <paramref name="length"/> characters.
        ///// </returns>
        //[ImplementsFunction("strcspn")]
        //public static int StrCSpn(string str, string acceptedChars, int offset, int length)
        //{
        //    return StrSpnInternal(str, acceptedChars, offset, length, true);
        //}

        ///// <summary>
        ///// Internal version of <see cref="StrSpn"/> (complement off) and <see cref="StrCSpn"/> (complement on).
        ///// </summary>
        //internal static int StrSpnInternal(string str, string acceptedChars, int offset, int length, bool complement)
        //{
        //    if (str == null || acceptedChars == null) return 0;

        //    PhpMath.AbsolutizeRange(ref offset, ref length, str.Length);

        //    char[] chars = acceptedChars.ToCharArray();
        //    Array.Sort(chars);

        //    int j = offset;

        //    if (complement)
        //    {
        //        while (length > 0 && ArrayUtils.BinarySearch(chars, str[j]) < 0) { j++; length--; }
        //    }
        //    else
        //    {
        //        while (length > 0 && ArrayUtils.BinarySearch(chars, str[j]) >= 0) { j++; length--; }
        //    }

        //    return j - offset;
        //}

        #endregion

        #region sprintf, vsprintf

        /// <summary>
        /// Default number of decimals when formatting floating-point numbers (%f in printf).
        /// </summary>
        internal const int printfFloatPrecision = 6;

        /// <summary>
        /// Returns a formatted string.
        /// </summary>
        /// <param name="format">The format string. 
        /// See <A href="http://www.php.net/manual/en/function.sprintf.php">PHP manual</A> for details.
        /// Besides, a type specifier "%C" is applicable. It converts an integer value to Unicode character.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The formatted string or null if there is too few arguments.</returns>
        /// <remarks>Assumes that either <paramref name="format"/> nor <paramref name="arguments"/> is null.</remarks>
        internal static string FormatInternal(Context ctx, string format, PhpValue[] arguments)
        {
            Debug.Assert(format != null && arguments != null);

            Encoding encoding = ctx.StringEncoding;
            StringBuilder result = new StringBuilder();
            int state = 0, width = 0, precision = -1, seqIndex = 0, swapIndex = -1;
            bool leftAlign = false;
            bool plusSign = false;
            char padChar = ' ';

            // process the format string using a 6-state finite automaton
            int length = format.Length;
            for (int i = 0; i < length; i++)
            {
                char c = format[i];

                Lambda:
                switch (state)
                {
                    case 0: // the initial state
                        {
                            if (c == '%')
                            {
                                width = 0;
                                precision = -1;
                                swapIndex = -1;
                                leftAlign = false;
                                plusSign = false;
                                padChar = ' ';
                                state = 1;
                            }
                            else result.Append(c);
                            break;
                        }

                    case 1: // % character encountered, expecting format
                        {
                            switch (c)
                            {
                                case '-': leftAlign = true; break;
                                case '+': plusSign = true; break;
                                case ' ': padChar = ' '; break;
                                case '\'': state = 2; break;
                                case '.': state = 4; break;
                                case '%': result.Append(c); state = 0; break;
                                case '0': padChar = '0'; state = 3; break;

                                default:
                                    {
                                        if (Char.IsDigit(c)) state = 3;
                                        else state = 5;
                                        goto Lambda;
                                    }
                            }
                            break;
                        }

                    case 2: // ' character encountered, expecting padding character
                        {
                            padChar = c;
                            state = 1;
                            break;
                        }

                    case 3: // number encountered, expecting width or argument number
                        {
                            switch (c)
                            {
                                case '$':
                                    {
                                        swapIndex = width;
                                        if (swapIndex == 0)
                                        {
                                            //PhpException.Throw(PhpError.Warning, LibResources.GetString("zero_argument_invalid"));
                                            //return result.ToString();
                                            throw new ArgumentException();
                                        }

                                        width = 0;
                                        state = 1;
                                        break;
                                    }

                                case '.':
                                    {
                                        state = 4;
                                        break;
                                    }

                                default:
                                    {
                                        if (Char.IsDigit(c)) width = width * 10 + (int)Char.GetNumericValue(c);
                                        else
                                        {
                                            state = 5;
                                            goto Lambda;
                                        }
                                        break;
                                    }
                            }
                            break;
                        }

                    case 4: // number after . encountered, expecting precision
                        {
                            if (precision == -1) precision = 0;
                            if (Char.IsDigit(c)) precision = precision * 10 + (int)Char.GetNumericValue(c);
                            else
                            {
                                state = 5;
                                goto case 5;
                            }
                            break;
                        }

                    case 5: // expecting type specifier
                        {
                            int index = (swapIndex <= 0 ? seqIndex++ : swapIndex - 1);
                            if (index >= arguments.Length)
                            {
                                // few arguments:
                                return null;
                            }

                            var obj = arguments[index];
                            string app = null;
                            char sign = '\0';

                            switch (c)
                            {
                                case 'b': // treat as integer, present as binary number without a sign
                                    app = System.Convert.ToString(obj.ToLong(), 2);
                                    break;

                                case 'c': // treat as integer, present as character
                                    app = encoding.GetString(new byte[] { unchecked((byte)obj.ToLong()) }, 0, 1);
                                    break;

                                case 'C': // treat as integer, present as Unicode character
                                    app = new String(unchecked((char)obj.ToLong()), 1);
                                    break;

                                case 'd': // treat as integer, present as signed decimal number
                                    {
                                        // use long to prevent overflow in Math.Abs:
                                        long ivalue = obj.ToLong();
                                        if (ivalue < 0) sign = '-'; else if (ivalue >= 0 && plusSign) sign = '+';

                                        app = Math.Abs((long)ivalue).ToString();
                                        break;
                                    }

                                case 'u': // treat as integer, present as unsigned decimal number, without sign
                                    app = unchecked((uint)obj.ToLong()).ToString();
                                    break;

                                case 'e':
                                    {
                                        double dvalue = obj.ToDouble();
                                        if (dvalue < 0) sign = '-'; else if (dvalue >= 0 && plusSign) sign = '+';

                                        string f = String.Concat("0.", new String('0', precision == -1 ? printfFloatPrecision : precision), "e+0");
                                        app = Math.Abs(dvalue).ToString(f);
                                        break;
                                    }

                                case 'f': // treat as float, present locale-aware floating point number
                                    {
                                        double dvalue = obj.ToDouble();
                                        if (dvalue < 0) sign = '-'; else if (dvalue >= 0 && plusSign) sign = '+';

                                        app = Math.Abs(dvalue).ToString("F" + (precision == -1 ? printfFloatPrecision : precision));
                                        break;
                                    }

                                case 'F': // treat as float, present locale-unaware floating point number with '.' decimal separator (PHP 5.0.3+ feature)
                                    {
                                        double dvalue = obj.ToDouble();
                                        if (dvalue < 0) sign = '-'; else if (dvalue >= 0 && plusSign) sign = '+';

                                        app = Math.Abs(dvalue).ToString("F" + (precision == -1 ? printfFloatPrecision : precision),
                                          System.Globalization.NumberFormatInfo.InvariantInfo);
                                        break;
                                    }

                                case 'o': // treat as integer, present as octal number without sign
                                    app = System.Convert.ToString(obj.ToLong(), 8);
                                    break;

                                case 'x': // treat as integer, present as hex number (lower case) without sign
                                    app = obj.ToLong().ToString("x");
                                    break;

                                case 'X': // treat as integer, present as hex number (upper case) without sign
                                    app = obj.ToLong().ToString("X");
                                    break;

                                case 's': // treat as string, present as string
                                    app = obj.ToString(ctx);

                                    // undocumented feature:
                                    if (precision != -1) app = app.Remove(Math.Min(precision, app.Length));

                                    break;
                            }

                            if (app != null)
                            {
                                // pad:
                                if (leftAlign)
                                {
                                    if (sign != '\0') result.Append(sign);
                                    result.Append(app);
                                    for (int j = width - app.Length; j > ((sign != '\0') ? 1 : 0); j--)
                                        result.Append(padChar);
                                }
                                else
                                {
                                    if (sign != '\0' && padChar == '0')
                                        result.Append(sign);

                                    for (int j = width - app.Length; j > ((sign != '\0') ? 1 : 0); j--)
                                        result.Append(padChar);

                                    if (sign != '\0' && padChar != '0')
                                        result.Append(sign);

                                    result.Append(app);
                                }
                            }

                            state = 0;
                            break;
                        }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns a formatted string.
        /// </summary>
        /// <param name="format">The format string. For details, see PHP manual.</param>
        /// <param name="arguments">The arguments.
        /// See <A href="http://www.php.net/manual/en/function.sprintf.php">PHP manual</A> for details.
        /// Besides, a type specifier "%C" is applicable. It converts an integer value to Unicode character.</param>
        /// <returns>The formatted string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="arguments"/> parameter is null.</exception>
        /// <exception cref="PhpException">Thrown when there is less arguments than expeceted by formatting string.</exception>
        //[return: CastToFalse]
        public static string sprintf(Context ctx, string format, params PhpValue[] arguments)
        {
            if (format == null) return string.Empty;

            // null arguments would be compiler's error (or error of the user):
            if (arguments == null) throw new ArgumentNullException("arguments");

            var result = FormatInternal(ctx, format, arguments);
            if (result == null)
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("too_few_arguments"));

                // TODO: return FALSE
                throw new ArgumentException();
            }
            return result;
        }

        /// <summary>
        /// Returns a formatted string.
        /// </summary>
        /// <param name="format">The format string. For details, see PHP manual.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The formatted string.</returns>
        /// <exception cref="PhpException">Thrown when there is less arguments than expeceted by formatting string.</exception>
        //[return: CastToFalse]
        public static string vsprintf(Context ctx, string format, PhpArray arguments)
        {
            if (format == null) return string.Empty;

            PhpValue[] array;
            if (arguments != null && arguments.Count != 0)
            {
                array = new PhpValue[arguments.Count];
                arguments.Values.CopyTo(array, 0);
            }
            else
            {
                array = Core.Utilities.ArrayUtils.EmptyValues;
            }

            var result = FormatInternal(ctx, format, array);
            if (result == null)
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("too_few_arguments"));

                // TODO: return FALSE
                throw new ArgumentException();
            }
            return result;
        }

        #endregion

        #region str_pad

        /// <summary>
        /// Type of padding.
        /// </summary>
        public enum PaddingType
        {
            /// <summary>Pad a string from the left.</summary>
            Left = 0,

            /// <summary>Pad a string from the right.</summary>
            Right = 1,

            /// <summary>Pad a string from both sides.</summary>
            Both = 2
        }

        public const int STR_PAD_LEFT = (int)PaddingType.Left;
        public const int STR_PAD_RIGHT = (int)PaddingType.Right;
        public const int STR_PAD_BOTH = (int)PaddingType.Both;

        /// <summary>
        /// Pads a string to a certain length with spaces.
        /// </summary>
        /// <param name="str">The string to pad.</param>
        /// <param name="totalWidth">Desired length of the returned string.</param>
        /// <returns><paramref name="str"/> padded on the right with spaces.</returns>
        public static string str_pad(string str, int totalWidth)
        {
            //if (str is PhpBytes)
            //    return Pad(str, totalWidth, new PhpBytes(32));
            //else
            return str_pad(str, totalWidth, " ");
        }

        /// <summary>
        /// Pads a string to certain length with another string.
        /// </summary>
        /// <param name="str">The string to pad.</param>
        /// <param name="totalWidth">Desired length of the returned string.</param>
        /// <param name="paddingString">The string to use as the pad.</param>
        /// <returns><paramref name="str"/> padded on the right with <paramref name="paddingString"/>.</returns>
        /// <exception cref="PhpException">Thrown if <paramref name="paddingString"/> is null or empty.</exception>
        public static string str_pad(string str, int totalWidth, string paddingString)
        {
            return str_pad(str, totalWidth, paddingString, PaddingType.Right);
        }

        /// <summary>
        /// Pads a string to certain length with another string.
        /// </summary>
        /// <param name="str">The string to pad.</param>
        /// <param name="totalWidth">Desired length of the returned string.</param>
        /// <param name="paddingString">The string to use as the pad.</param>
        /// <param name="paddingType">Specifies whether the padding should be done on the left, on the right,
        /// or on both sides of <paramref name="str"/>.</param>
        /// <returns><paramref name="str"/> padded with <paramref name="paddingString"/>.</returns>
        /// <exception cref="PhpException">Thrown if <paramref name="paddingType"/> is invalid or <paramref name="paddingString"/> is null or empty.</exception>
        public static string str_pad(string str, int totalWidth, string paddingString, PaddingType paddingType)
        {
            //PhpBytes binstr = str as PhpBytes;
            //if (str is PhpBytes)
            //{
            //    PhpBytes binPaddingString = Core.Convert.ObjectToPhpBytes(paddingString);

            //    if (binPaddingString == null || binPaddingString.Length == 0)
            //    {
            //        PhpException.InvalidArgument("paddingString", LibResources.GetString("arg:null_or_empty"));
            //        return null;
            //    }
            //    if (binstr == null) binstr = PhpBytes.Empty;

            //    int length = binstr.Length;
            //    if (totalWidth <= length) return binstr;

            //    int pad = totalWidth - length, padLeft = 0, padRight = 0;

            //    switch (paddingType)
            //    {
            //        case PaddingType.Left: padLeft = pad; break;
            //        case PaddingType.Right: padRight = pad; break;

            //        case PaddingType.Both:
            //            padLeft = pad / 2;
            //            padRight = pad - padLeft;
            //            break;

            //        default:
            //            PhpException.InvalidArgument("paddingType");
            //            break;
            //    }

            //    // if paddingString has length 1, use String.PadLeft and String.PadRight
            //    int padStrLength = binPaddingString.Length;

            //    // else build the resulting string manually
            //    byte[] result = new byte[totalWidth];

            //    int position = 0;

            //    // pad left
            //    while (padLeft > padStrLength)
            //    {
            //        Buffer.BlockCopy(binPaddingString.ReadonlyData, 0, result, position, padStrLength);
            //        padLeft -= padStrLength;
            //        position += padStrLength;
            //    }

            //    if (padLeft > 0)
            //    {
            //        Buffer.BlockCopy(binPaddingString.ReadonlyData, 0, result, position, padLeft);
            //        position += padLeft;
            //    }

            //    Buffer.BlockCopy(binstr.ReadonlyData, 0, result, position, binstr.Length);
            //    position += binstr.Length;

            //    // pad right
            //    while (padRight > padStrLength)
            //    {
            //        Buffer.BlockCopy(binPaddingString.ReadonlyData, 0, result, position, padStrLength);
            //        padRight -= padStrLength;
            //        position += padStrLength;
            //    }

            //    if (padRight > 0)
            //    {
            //        Buffer.BlockCopy(binPaddingString.ReadonlyData, 0, result, position, padRight);
            //        position += padRight;
            //    }

            //    return new PhpBytes(result);
            //}

            string unistr = str; // Core.Convert.ObjectToString(str);
            if (unistr != null)
            {
                string uniPaddingString = paddingString; // Core.Convert.ObjectToString(paddingString);

                if (string.IsNullOrEmpty(uniPaddingString))
                {
                    //PhpException.InvalidArgument("paddingString", LibResources.GetString("arg:null_or_empty"));
                    //return null;
                    throw new ArgumentException();
                }

                int length = unistr.Length;
                if (totalWidth <= length) return unistr;

                int pad = totalWidth - length, padLeft = 0, padRight = 0;

                switch (paddingType)
                {
                    case PaddingType.Left: padLeft = pad; break;
                    case PaddingType.Right: padRight = pad; break;

                    case PaddingType.Both:
                        padLeft = pad / 2;
                        padRight = pad - padLeft;
                        break;

                    default:
                        //PhpException.InvalidArgument("paddingType");
                        //break;
                        throw new ArgumentException();
                }

                // if paddingString has length 1, use String.PadLeft and String.PadRight
                int padStrLength = uniPaddingString.Length;
                if (padStrLength == 1)
                {
                    char c = uniPaddingString[0];
                    if (padLeft > 0) unistr = unistr.PadLeft(length + padLeft, c);
                    if (padRight > 0) unistr = unistr.PadRight(totalWidth, c);

                    return unistr;
                }

                // else build the resulting string manually
                StringBuilder result = new StringBuilder(totalWidth);

                // pad left
                while (padLeft > padStrLength)
                {
                    result.Append(uniPaddingString);
                    padLeft -= padStrLength;
                }
                if (padLeft > 0) result.Append(uniPaddingString.Substring(0, padLeft));

                result.Append(unistr);

                // pad right
                while (padRight > padStrLength)
                {
                    result.Append(uniPaddingString);
                    padRight -= padStrLength;
                }
                if (padRight > 0) result.Append(uniPaddingString.Substring(0, padRight));

                return result.ToString();
            }

            return null;
        }

        #endregion
    }
}
