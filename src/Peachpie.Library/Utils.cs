using Microsoft.Extensions.ObjectPool;
using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System_DateTime = System.DateTime;

namespace Pchp.Library
{
    internal static class PathUtils
    {
        public const char DirectorySeparator = '\\';
        public const char AltDirectorySeparator = '/';

        public static readonly char[] DirectorySeparatorChars = new[] { DirectorySeparator, AltDirectorySeparator };

        public static bool IsDirectorySeparator(this char ch) => ch == DirectorySeparator || ch == AltDirectorySeparator;
    }

    internal static class StringUtils
    {
        /// <summary>
        /// Adds slashes before characters '\\', '\0', '\'' and '"'.
        /// </summary>
        /// <param name="str">The string to add slashes in.</param>
        /// <param name="doubleQuotes">Whether to slash double quotes.</param>
        /// <param name="singleQuotes">Whether to slash single quotes.</param>
        /// <param name="nul">Whether to slash '\0' character.</param>
        /// <returns>The slashed string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is a <B>null</B> reference.</exception>
        public static string/*!*/ AddCSlashes(string/*!*/ str, bool singleQuotes = true, bool doubleQuotes = true, bool nul = true)
        {
            if (str == null) throw new ArgumentNullException("str");

            StringBuilder result = new StringBuilder(str.Length);

            string double_quotes = doubleQuotes ? "\\\"" : "\"";
            string single_quotes = singleQuotes ? @"\'" : "'";
            string slashed_nul = nul ? "\\0" : "\0";

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '\\': result.Append(@"\\"); break;
                    case '\0': result.Append(slashed_nul); break;
                    case '\'': result.Append(single_quotes); break;
                    case '"': result.Append(double_quotes); break;
                    default: result.Append(c); break;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Strips slashes from a string.
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>
        /// String where slashes are striped away.
        /// Slashed characters with special meaning ("\0") are replaced with their special value.
        /// </returns>
        public static string/*!*/ StripCSlashes(string/*!*/ str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (str.Length == 0)
            {
                return string.Empty;
            }

            var result = StringBuilderUtilities.Pool.Get();
            int from = 0;   // plain chunk start

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\\')
                {
                    // 
                    result.Append(str, from, i - from);

                    //
                    if (++i < str.Length)
                    {
                        // PHP strips all slashes, not only quotes and slash
                        // "\0" has a special meaning => '\0'
                        var slashed = str[i];
                        result.Append(slashed == '0' ? '\0' : slashed);
                    }

                    from = i + 1;
                }
            }

            //
            if (from < str.Length)
            {
                result.Append(str, from, str.Length - from);
            }

            //
            return StringBuilderUtilities.GetStringAndReturn(result);
        }

        /// <summary>
        /// Adds slash before '\0' character and duplicates apostrophes.
        /// </summary>
        /// <param name="str">The string to add slashes in.</param>
        /// <returns>The slashed string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is a <B>null</B> reference.</exception>
        public static string/*!*/ AddDbSlashes(string/*!*/ str)
        {
            if (str == null) throw new ArgumentNullException("str");

            StringBuilder result = new StringBuilder(str.Length);

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '\0': result.Append('\\'); result.Append('0'); break;
                    case '\'': result.Append('\''); result.Append('\''); break;
                    default: result.Append(c); break;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Replaces slashed 0 with null character ('\0') and double apostrophe with single apostrophe. 
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>String with replaced characters.</returns>
        public static string/*!*/ StripDbSlashes(string/*!*/ str)
        {
            if (str == null) throw new ArgumentNullException("str");

            StringBuilder result = new StringBuilder(str.Length);

            int i = 0;
            while (i < str.Length - 1)
            {
                if (str[i] == '\\' && str[i + 1] == '0')
                {
                    result.Append('\0');
                    i += 2;
                }
                else if (str[i] == '\'' && str[i + 1] == '\'')
                {
                    result.Append('\'');
                    i += 2;
                }
                else
                {
                    result.Append(str[i]);
                    i++;
                }
            }
            if (i < str.Length)
                result.Append(str[i]);

            return result.ToString();
        }

        /// <summary>
        /// Converts a string of bytes into hexadecimal representation.
        /// </summary>
        /// <param name="bytes">The string of bytes.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>Concatenation of hexadecimal values of bytes of <paramref name="bytes"/> separated by <paramref name="separator"/>.</returns>
        public static string BinToHex(byte[] bytes, string separator = null) => Core.Utilities.StringUtils.BinToHex(bytes, separator);

        /// <summary>
        /// Converts 16 based digit to decimal number.
        /// </summary>
        /// <param name="c">0-9, a-z.</param>
        /// <returns>Decimal number or <c>-1</c> if input is out of range.</returns>
        public static int HexToNumber(char c)
        {
            if (c >= '0') // '0' 48
            {
                if (c <= '9') return c - '0'; // '9' 57

                if (c >= 'A') // 'A' 65
                {
                    if (c <= 'F') return c - 'A' + 10; // 'F' 70
                    if (c >= 'a' && c <= 'f') return c - 'a' + 10; // 97 - 102
                }
            }

            //
            return -1;
        }

        /// <summary>
        /// Converts binary string <paramref name="str"/> to <see cref="string"/>.
        /// In case if binary string, the conversion routine respects given <paramref name="charSet"/>.
        /// </summary>
        /// <param name="str">String to be converted to unicode string.</param>
        /// <param name="charSet">Character set used to encode binary string to <see cref="string"/>.</param>
        /// <returns>String representation of <paramref name="str"/>.</returns>
        internal static string ToString(this PhpString str, string charSet)
        {
            if (str.IsEmpty)
            {
                return string.Empty;
            }

            Encoding encoding;

            if (str.ContainsBinaryData && !string.IsNullOrEmpty(charSet))
            {
                try
                {
                    encoding = Encoding.GetEncoding(charSet);
                }
                catch (ArgumentException)
                {
                    //throw new ArgumentException(string.Format(Strings.arg_invalid_value, "charSet", charSet), "charSet");
                    throw new ArgumentException("", nameof(charSet));   // TODO: Err
                }
            }
            else
            {
                // Encoding not needed
                encoding = Encoding.UTF8;
            }

            //
            return str.ToString(encoding);
        }

        /// <summary>
        /// Returns last character of string or -1 if empty
        /// </summary>
        /// <param name="str">String</param>
        /// <returns>Last character of string or -1 if empty</returns>
        public static int LastCharacter(this string/*!*/ str)
        {
            return str.Length == 0 ? -1 : str[str.Length - 1];
        }

        /// <summary>
        /// Counts characters within the string.
        /// </summary>
        public static int CharsCount(this string str, char c)
        {
            if (str == null)
                return 0;

            int count = 0;
            for (int i = 0; i < str.Length; i++)
                if (str[i] == c)
                    count++;

            return count;
        }

        /// <summary>
        /// Determines whether two strings are equal while ignoring casing.
        /// </summary>
        public static bool EqualsOrdinalIgnoreCase(this string str1, string str2) => Core.Utilities.StringUtils.EqualsOrdinalIgnoreCase(str1, str2);

        /// <summary>
        /// Determines whether two strings are equal while ignoring casing.
        /// </summary>
        public static bool EqualsOrdinalIgnoreCase(this ReadOnlySpan<char> str1, ReadOnlySpan<char> str2) => Core.Utilities.StringUtils.EqualsOrdinalIgnoreCase(str1, str2);

        /// <summary>
        /// Decodes given json encoded string.
        /// </summary>
        public static PhpValue JsonDecode(string value)
        {
            var options = new PhpSerialization.JsonSerializer.DecodeOptions();
            var scanner = new Json.JsonScanner(new StringReader(value), options);
            var parser = new Json.Parser(options)
            {
                Scanner = scanner,
            };

            return parser.Parse() ? parser.Result : throw new FormatException();
        }

        /// <summary>
        /// Gets newline character sequence length at given position.
        /// If there is no newline sequence, <c>0</c> is returned.
        /// </summary>
        public static int IsNewLine(string text, int index)
        {
            if (text != null && index < text.Length)
            {
                return text[index] switch
                {
                    '\n' => (index + 1 < text.Length && text[index + 1] == '\r') ? 2 : 1,
                    '\r' => (index + 1 < text.Length && text[index + 1] == '\n') ? 2 : 1,
                    '\u2028' => 1, // Unicode Line Separator
                    _ => 0,
                };
            }

            return 0;
        }

        /// <summary>
        /// Removes all occurances of characters.
        /// </summary>
        public static string RemoveAny(this string text, params char[] anyOf)
        {
            if (string.IsNullOrEmpty(text) || anyOf.Length == 0)
            {
                return text;
            }

            int startIndex = 0;
            StringBuilder tmp = null;

            for (; ; )
            {
                var idx = text.IndexOfAny(anyOf, startIndex);
                if (idx < 0)
                {
                    break;
                }

                // lazily create string builder and append text without found characters:
                tmp ??= StringBuilderUtilities.Pool.Get();
                tmp.Append(text, startIndex, idx - startIndex);

                startIndex = idx + 1;
            }

            if (tmp != null)
            {
                tmp.Append(text, startIndex, text.Length - startIndex);

                return StringBuilderUtilities.GetStringAndReturn(tmp);
            }

            //
            return text;
        }
    }

    internal static class Base64Utils
    {
        /// <summary>
        /// Decodes encoded string, ignores whitespaces and missing `=` characters.
        /// Silently ignores invalid characters;
        /// </summary>
        /// <param name="base64">The input base64 encoded string.</param>
        /// <param name="strict">If set, invalid characters cause <see cref="FormatException"/>.</param>
        /// <exception cref="FormatException">Invalid character if <paramref name="strict"/> is set.</exception>
        public static byte[] FromBase64(ReadOnlySpan<char> base64, bool strict)
        {
            // count resulting bytes:
            var count = CountResultingLength(base64, strict);
            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            int fourbytes = 0;
            int n = 0;

            var bytes = new byte[count];
            Span<byte> seq = bytes; // output current sequence of 3 bytes

            foreach (var ch in base64)
            {
                var idx = B64ToIndex(ch, false);
                if (idx < 0) continue;

                Debug.Assert(idx >= 0 && idx <= 64); // 6-bits

                fourbytes = (fourbytes << 6) | (idx);

                if (++n == 4)
                {
                    B64_24Bits_to_3Chars(fourbytes, out var a, out var b, out var c);

                    seq[0] = a;
                    seq[1] = b;
                    seq[2] = c;

                    seq = seq.Slice(3);
                    n = 0;
                    fourbytes = 0;
                }
            }

            if (n > 0)
            {
                var remaining = 4 - n;
                fourbytes <<= (6 * remaining);

                B64_24Bits_to_3Chars(fourbytes, out var a, out var b, out var c);

                seq[0] = a;
                if (n > 2) seq[1] = b;
            }

            //
            return bytes;
        }

        static int CountResultingLength(ReadOnlySpan<char> base64, bool strict)
        {
            int validbytes = 0;

            //
            foreach (var ch in base64)
            {
                var idx = B64ToIndex(ch, strict);
                if (idx >= 0)
                {
                    validbytes++;
                }
            }

            var count = 3 * Math.DivRem(validbytes, 4, out var n);

            // 
            if (n > 0)
            {
                count++;
                if (n > 2) count++;
            }

            return count;
        }

        static int B64ToIndex(char c, bool strict)
        {
            // map:
            // return map.IndexOf(c);

            // ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/
            // 65 - 90, 97 - 122, 48 - 57, 43, 47

            const int off_A = 0;
            const int off_a = 26;
            const int off_0 = 52;
            const int off_plus = 62;
            const int off_slash = 63;

            // divide intervals

            if (c >= 'A')
            {
                if (c <= 'Z')
                {
                    // 65 - 90
                    return c - 'A' + off_A;
                }

                // (90 .. )
                if (c >= 'a' && c <= 'z')
                {
                    return c - 'a' + off_a;
                }
            }
            else
            {
                // < 65

                if (c >= '0')
                {
                    if (c <= '9')
                    {
                        return c - '0' + off_0;
                    }

                    // (57 .. 65)
                    // nothing
                }
                else
                {
                    // ( .. 48)
                    if (c == '+') return off_plus;
                    if (c == '/') return off_slash;
                }
            }

            if (strict && c != '=' && !char.IsWhiteSpace(c))
            {
                ThrowBadCharacter();
            }

            return -1;
        }

        static void B64_24Bits_to_3Chars(int bits, out byte a, out byte b, out byte c)
        {
            a = (byte)(bits >> 16);
            b = (byte)(bits >> 8);
            c = (byte)(bits);
        }

        static void ThrowBadCharacter()
        {
            throw new FormatException();
        }
    }

    internal static class ArrayExtensions
    {
        /// <summary>
        /// Returns the array of unsigned bytes from which this stream was created.
        /// </summary>
        public static byte[] GetBuffer(this MemoryStream stream)
        {
            ArraySegment<byte> buffer;
            if (!stream.TryGetBuffer(out buffer)) throw new ArgumentException();    //  stream is not exposable
            return buffer.Array;
        }

        /// <summary>
        /// Gets a slice of array.
        /// </summary>
        public static T[] Slice<T>(this T[] array, int start) // TODO: Span<T>
        {
            return Slice(array, start, array.Length - start);
        }

        /// <summary>
        /// Gets a slice of array.
        /// </summary>
        public static T[] Slice<T>(this T[] array, int start, int length)   // TODO: Span<T>
        {
            var slice = new T[length];
            Buffer.BlockCopy(array, start, slice, 0, length);
            return slice;
        }
    }

    internal static class UriUtils
    {
        /// <summary>Gets the decimal value of a hexadecimal digit.</summary>
        /// <param name="digit">The hexadecimal digit (0-9, a-f, A-F) to convert. </param>
        /// <returns>An <see cref="int" /> value that contains a number from 0 to 15 that corresponds to the specified hexadecimal digit.</returns>
        public static int FromHex(char digit)
        {
            if ((digit < '0' || digit > '9') && (digit < 'A' || digit > 'F') && (digit < 'a' || digit > 'f'))
            {
                throw new ArgumentException("digit");
            }
            if (digit > '9')
            {
                return (int)(((digit <= 'F') ? (digit - 'A') : (digit - 'a')) + '\n');
            }
            return (int)(digit - '0');
        }

        /// <summary>
        /// Determines whether a specified character is a valid hexadecimal digit.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public static bool IsHexDigit(char character)
        {
            return (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F') || (character >= 'a' && character <= 'f');
        }

        /// <summary>
        /// Parse a query string into its component key and value parts.
        /// </summary>
        /// <param name="queryString">The raw query string value, with or without the leading '?'.</param>
        /// <param name="callback">Delegate invoked when a [name, value] is parsed from the query.</param>
        /// <returns>A collection of parsed keys and values, null if there are no entries.</returns>
        public static void ParseQuery(string queryString, Action<string, string> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (string.IsNullOrEmpty(queryString))
            {
                return;
            }

            int scanIndex = 0;
            if (queryString[0] == '?')
            {
                scanIndex = 1;
            }

            int textLength = queryString.Length;
            int equalIndex = queryString.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }

            while (scanIndex < textLength)
            {
                int delimiterIndex = queryString.IndexOf('&', scanIndex);
                if (delimiterIndex == -1)
                {
                    delimiterIndex = textLength;
                }
                if (equalIndex < delimiterIndex)
                {
                    // skip whitespaces
                    while (scanIndex != equalIndex && char.IsWhiteSpace(queryString[scanIndex]))
                    {
                        ++scanIndex;
                    }

                    // &name=value
                    string name = queryString.Substring(scanIndex, equalIndex - scanIndex);
                    string value = queryString.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);
                    callback(Uri.UnescapeDataString(name.Replace('+', ' ')), Uri.UnescapeDataString(value.Replace('+', ' ')));

                    //
                    equalIndex = queryString.IndexOf('=', delimiterIndex);
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
                else
                {
                    if (delimiterIndex > scanIndex)
                    {
                        // &name
                        callback(
                            queryString.Substring(scanIndex, delimiterIndex - scanIndex),
                            string.Empty);
                    }
                }
                scanIndex = delimiterIndex + 1;
            }
        }
    }

    /// <summary>
    /// Unix TimeStamp to <see cref="System_DateTime"/> conversion and vice versa
    /// </summary>
    internal static class DateTimeUtils
    {
        #region Nested Class: UtcTimeZone, GmtTimeZone

        //private sealed class _UtcTimeZone : CustomTimeZoneBase
        //{
        //    public override string DaylightName { get { return "UTC"; } }
        //    public override string StandardName { get { return "UTC"; } }

        //    public override TimeSpan GetUtcOffset(DateTime time)
        //    {
        //        return new TimeSpan(0);
        //    }

        //    public override DaylightTime GetDaylightChanges(int year)
        //    {
        //        return new DaylightTime(new DateTime(0), new DateTime(0), new TimeSpan(0));
        //    }


        //}

        //private sealed class _GmtTimeZone : CustomTimeZoneBase
        //{
        //    public override string DaylightName { get { return "GMT Daylight Time"; } }
        //    public override string StandardName { get { return "GMT Standard Time"; } }

        //    public override TimeSpan GetUtcOffset(DateTime time)
        //    {
        //        return IsDaylightSavingTime(time) ? new TimeSpan(0, +1, 0, 0, 0) : new TimeSpan(0);
        //    }
        //    public override DaylightTime GetDaylightChanges(int year)
        //    {
        //        return new DaylightTime
        //        (
        //          new DateTime(year, 3, 27, 1, 0, 0),
        //          new DateTime(year, 10, 30, 2, 0, 0),
        //          new TimeSpan(0, +1, 0, 0, 0)
        //        );
        //    }
        //}

        #endregion

        /// <summary>
        /// Time 0 in terms of Unix TimeStamp.
        /// </summary>
        public static System_DateTime/*!*/UtcStartOfUnixEpoch => Core.Utilities.DateTimeUtils.UtcStartOfUnixEpoch;

        /// <summary>
        /// UTC time zone.
        /// </summary>
        internal static TimeZoneInfo/*!*/UtcTimeZone => TimeZoneInfo.Utc;

        /// <summary>
        /// Converts <see cref="System_DateTime"/> representing UTC time to UNIX timestamp.
        /// </summary>
        /// <param name="dt">Time.</param>
        /// <returns>Unix timestamp.</returns>
        internal static long UtcToUnixTimeStamp(System_DateTime dt) => Core.Utilities.DateTimeUtils.UtcToUnixTimeStamp(dt);

        /// <summary>
        /// Converts UNIX timestamp (number of seconds from 1.1.1970) to <see cref="System_DateTime"/>.
        /// </summary>
        /// <param name="timestamp">UNIX timestamp</param>
        /// <returns><see cref="System_DateTime"/> structure representing UTC time.</returns>
        internal static System_DateTime UnixTimeStampToUtc(long timestamp)
        {
            return UtcStartOfUnixEpoch + TimeSpan.FromSeconds(timestamp);
        }

        /// <summary>
        /// Determine maximum of three given <see cref="System_DateTime"/> values.
        /// </summary>
        internal static System_DateTime Max(System_DateTime d1, System_DateTime d2)
        {
            return (d1 > d2) ? d1 : d2;
        }

        /// <summary>
        /// Determine maximum of three given <see cref="System_DateTime"/> values.
        /// </summary>
        internal static System_DateTime Max(System_DateTime d1, System_DateTime d2, System_DateTime d3)
        {
            return (d1 < d2) ? ((d2 < d3) ? d3 : d2) : ((d1 < d3) ? d3 : d1);
        }

        //		private static TimeZone GetTimeZoneFromRegistry(TimeZone/*!*/ zone)
        //		{
        //		  try
        //		  {
        //		    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
        //		      @"Software\Microsoft\Windows NT\CurrentVersion\Time Zones\" + zone.StandardName,false))
        //		    {
        //  		    if (key == null) return null;
        //		      
        //		      byte[] tzi = key.GetValue("TZI") as byte[];
        //		      if (tzi == null) continue;
        //    		    
        //    		  int bias = BitConverter.ToInt32(tzi,0);
        //    		  
        //  		  }  
        //		  }
        //		  catch (Exception)
        //		  {
        //		  }
        //
        //		  return null;
        //		}		
    }

    /// <summary>
    /// <see cref="StringBuilder"/> extensions and pooling.
    /// </summary>
    public struct StringBuilderUtilities
    {
        /// <summary>
        /// Gets object pool singleton.
        /// Uses <see cref="StringBuilderPooledObjectPolicy"/> policy (automatically clears the string builder upon return).
        /// </summary>
        public static ObjectPool<StringBuilder> Pool => s_lazyObjectPool.Value;

        static readonly Lazy<ObjectPool<StringBuilder>> s_lazyObjectPool = new Lazy<ObjectPool<StringBuilder>>(
            () => new DefaultObjectPoolProvider().Create(new StringBuilderPooledObjectPolicy()),
            System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Gets the <paramref name="sb"/> value as string and return the instance to the <see cref="Pool"/>.
        /// </summary>
        /// <param name="sb">String builder instance.</param>
        /// <returns><paramref name="sb"/> string.</returns>
        public static string GetStringAndReturn(StringBuilder sb)
        {
            Debug.Assert(sb != null);
            var value = sb.ToString();
            Pool.Return(sb);
            return value;
        }
    }
}
