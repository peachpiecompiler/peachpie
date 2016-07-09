using Pchp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Library
{
    public static class Strings
    {
        #region Character map

        [ThreadStatic]
        private static CharMap _charmap;

        /// <summary>
        /// Get clear <see cref="CharMap"/> to be used by current thread. <see cref="_charmap"/>.
        /// </summary>
        internal static CharMap InitializeCharMap()
        {
            CharMap result = _charmap;

            if (result == null)
                _charmap = result = new CharMap(0x0800);
            else
                result.ClearAll();

            return result;
        }

        #endregion

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

        #region explode, implode

        /// <summary>
        /// Splits a string by string separators.
        /// </summary>
        /// <param name="separator">The substrings separator. Must not be empty.</param>
        /// <param name="str">The string to be split.</param>
        /// <returns>The array of strings.</returns>
        //[return: CastToFalse]
        public static PhpArray explode(string separator, string str) => explode(separator, str, int.MaxValue);

        /// <summary>
        /// Splits a string by string separators with limited resulting array size.
        /// </summary>
        /// <param name="separator">The substrings separator. Must not be empty.</param>
        /// <param name="str">The string to be split.</param>
        /// <param name="limit">
        /// The maximum number of elements in the resultant array. Zero value is treated in the same way as 1.
        /// If negative, then the number of separators found in the string + 1 is added to the limit.
        /// </param>
        /// <returns>The array of strings.</returns>
        /// <remarks>
        /// If <paramref name="str"/> is empty an array consisting of exacty one empty string is returned.
        /// If <paramref name="limit"/> is zero
        /// </remarks>
        /// <exception cref="PhpException">Thrown if the <paramref name="separator"/> is null or empty or if <paramref name="limit"/>is not positive nor -1.</exception>
        //[return: CastToFalse]
        public static PhpArray explode(string separator, string str, int limit)
        {
            // validate parameters:
            if (string.IsNullOrEmpty(separator))
            {
                //PhpException.InvalidArgument("separator", LibResources.GetString("arg:null_or_empty"));
                //return null;
                throw new ArgumentException();
            }

            if (str == null) str = String.Empty;

            bool last_part_is_the_rest = limit >= 0;

            if (limit == 0)
                limit = 1;
            else if (limit < 0)
                limit += SubstringCountInternal(str, separator, 0, str.Length) + 2;

            // splits <str> by <separator>:
            int sep_len = separator.Length;
            int i = 0;                        // start searching at this position
            int pos;                          // found separator's first character position
            PhpArray result = new PhpArray(); // creates integer-keyed array with default capacity

            var/*!*/compareInfo = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;

            while (--limit > 0)
            {
                pos = compareInfo.IndexOf(str, separator, i, str.Length - i, System.Globalization.CompareOptions.Ordinal);

                if (pos < 0) break; // not found

                result.AddValue(PhpValue.Create(str.Substring(i, pos - i))); // faster than Add()
                i = pos + sep_len;
            }

            // Adds last chunk. If separator ends the string, it will add empty string (as PHP do).
            if (i <= str.Length && last_part_is_the_rest)
            {
                result.AddValue(PhpValue.Create(str.Substring(i)));
            }

            return result;
        }

        /// <summary>
        /// Concatenates items of an array into a string separating them by a glue.
        /// </summary>
        /// <param name="pieces">The array to be impleded.</param>
        /// <returns>The glued string.</returns>
        public static PhpString join(PhpArray pieces) => implode(pieces);

        /// <summary>
        /// Concatenates items of an array into a string separating them by a glue.
        /// </summary>
        /// <param name="pieces">The array to be impleded.</param>
        /// <param name="glue">The glue string.</param>
        /// <returns>The glued string.</returns>
        /// <exception cref="PhpException">Thrown if neither <paramref name="glue"/> nor <paramref name="pieces"/> is not null and of type <see cref="PhpArray"/>.</exception>
        public static PhpString join(PhpValue glue, PhpValue pieces) => implode(glue, pieces);

        /// <summary>
        /// Concatenates items of an array into a string.
        /// </summary>
        /// <param name="pieces">The <see cref="PhpArray"/> to be imploded.</param>
        /// <returns>The glued string.</returns>
        public static PhpString implode(PhpArray pieces)
        {
            if (pieces == null)
            {
                //PhpException.ArgumentNull("pieces");
                //return null;
                throw new ArgumentException();
            }

            return ImplodeInternal(PhpValue.Void, pieces);
        }

        /// <summary>
        /// Concatenates items of an array into a string separating them by a glue.
        /// </summary>
        /// <param name="glue">The glue of type <see cref="string"/> or <see cref="PhpArray"/> to be imploded.</param>
        /// <param name="pieces">The <see cref="PhpArray"/> to be imploded or glue of type <see cref="string"/>.</param>
        /// <returns>The glued string.</returns>
        /// <exception cref="PhpException">Thrown if neither <paramref name="glue"/> nor <paramref name="pieces"/> is not null and of type <see cref="PhpArray"/>.</exception>
        public static PhpString implode(PhpValue glue, PhpValue pieces)
        {
            if (pieces != null && pieces.IsArray)
                return ImplodeInternal(glue, pieces.AsArray());

            if (glue.IsArray)
                return ImplodeInternal(pieces, glue.AsArray());

            return ImplodeGenericEnumeration(glue, pieces);
        }

        private static PhpString ImplodeGenericEnumeration(PhpValue glue, PhpValue pieces)
        {
            IEnumerable enumerable;

            if (pieces.IsObject && (enumerable = pieces.Object as IEnumerable) != null)
                return ImplodeInternal(glue, new PhpArray(enumerable));

            if (glue.IsObject && (enumerable = glue.Object as IEnumerable) != null)
                return ImplodeInternal(pieces, new PhpArray(enumerable));

            ////
            //PhpException.InvalidArgument("pieces");
            //return null;
            throw new ArgumentException();
        }

        /// <summary>
        /// Concatenates items of an array into a string separating them by a glue.
        /// </summary>
        /// <param name="glue">The glue string.</param>
        /// <param name="pieces">The enumeration to be imploded.</param>
        /// <returns>The glued string.</returns>           
        /// <remarks>
        /// Items of <paramref name="pieces"/> are converted to strings in the manner of PHP 
        /// (i.e. by <see cref="PHP.Core.Convert.ObjectToString"/>).
        /// </remarks>
        /// <exception cref="PhpException">Thrown if <paramref name="pieces"/> is null.</exception>
        private static PhpString ImplodeInternal(PhpValue glue, PhpArray/*!*/pieces)
        {
            Debug.Assert(pieces != null);

            // handle empty pieces:
            if (pieces.Count == 0)
                return PhpString.Empty;

            // check whether we have to preserve a binary string
            //bool binary = glue != null && glue.GetType() == typeof(PhpBytes);
            //if (!binary)    // try to find any binary string within pieces:
            //    using (var x = pieces.GetFastEnumerator())
            //        while (x.MoveNext())
            //            if (x.CurrentValue.IsBinaryString)
            //            {
            //                binary = true;
            //                break;
            //            }

            // concatenate pieces and glue:

            bool not_first = false;                       // not the first iteration

            //if (binary)
            //{
            //    Debug.Assert(pieces.Count > 0);

            //    PhpBytes gluebytes = PHP.Core.Convert.ObjectToPhpBytes(glue);
            //    PhpBytes[] piecesBytes = new PhpBytes[pieces.Count + pieces.Count - 1]; // buffer of PhpBytes to be concatenated
            //    int p = 0;

            //    using (var x = pieces.GetFastEnumerator())
            //        while (x.MoveNext())
            //        {
            //            if (not_first) piecesBytes[p++] = gluebytes;
            //            else not_first = true;

            //            piecesBytes[p++] = PHP.Core.Convert.ObjectToPhpBytes(x.CurrentValue);
            //        }

            //    return PhpBytes.Concat(piecesBytes, 0, piecesBytes.Length);
            //}
            //else
            {
                string gluestr = glue.ToString();

                var result = new PhpString(/*pieces.Count * 2*/);

                using (var x = pieces.GetFastEnumerator())
                    while (x.MoveNext())
                    {
                        if (not_first) result.Append(gluestr);
                        else not_first = true;

                        result.Append(x.CurrentValue.ToString());
                    }

                return result;
            }
        }

        #endregion

        #region strtr, str_rot13

        /// <summary>
        /// Replaces specified characters in a string with another ones.
        /// </summary>
        /// <param name="str">A string where to do the replacement.</param>
        /// <param name="from">Characters to be replaced.</param>
        /// <param name="to">Characters to replace those in <paramref name="from"/> with.</param>
        /// <returns>
        /// A copy of <paramref name="str"/> with all occurrences of each character in <paramref name="from"/> 
        /// replaced by the corresponding character in <paramref name="to"/>.
        /// </returns>
        /// <remarks>
        /// <para>If <paramref name="from"/> and <paramref name="to"/> are different lengths, the extra characters 
        /// in the longer of the two are ignored.</para>
        /// </remarks>
        public static string strtr(string str, string from, string to)
        {
            if (String.IsNullOrEmpty(str) || from == null || to == null) return String.Empty;

            int min_length = Math.Min(from.Length, to.Length);
            Dictionary<char, char> ht = new Dictionary<char, char>(min_length);

            // adds chars to the hashtable:
            for (int i = 0; i < min_length; i++)
                ht[from[i]] = to[i];

            // creates result builder:
            StringBuilder result = new StringBuilder(str.Length, str.Length);
            result.Length = str.Length;

            // translates:
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                char h;
                result[i] = ht.TryGetValue(c, out h) ? h : c;

                // obsolete:
                // object h = ht[c];
                // result[i] = (h==null) ? c : h;
            }

            return result.ToString();
        }

        /// <summary>
        /// Compares objects according to the length of their string representation
        /// as the primary criteria and the alphabetical order as the secondary one.
        /// </summary>
        private sealed class KeyLengthComparer : IComparer<KeyValuePair<string, string>>
        {
            /// <summary>
            /// Performs length and alphabetical comparability backwards (longer first).
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                int rv = x.Key.Length - y.Key.Length;
                if (rv == 0) return -string.CompareOrdinal(x.Key, y.Key);
                else return -rv;
            }
        }

        /// <summary>
        /// Replaces substrings according to a dictionary.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <param name="replacePairs">
        /// An dictionary that contains <see cref="string"/> to <see cref="string"/> replacement mapping.
        /// </param>
        /// <returns>A copy of str, replacing all substrings (looking for the longest possible match).</returns>
        /// <remarks>This function will not try to replace stuff that it has already worked on.</remarks>
        /// <exception cref="PhpException">Thrown if the <paramref name="replacePairs"/> argument is null.</exception>
        //[return: CastToFalse]
        public static string strtr(string str, PhpArray replacePairs)
        {
            if (replacePairs == null)
            {
                //PhpException.ArgumentNull("replacePairs");
                //return null;
                throw new ArgumentException();
            }

            if (string.IsNullOrEmpty(str))
                return String.Empty;

            // sort replacePairs according to the key length, longer first
            var count = replacePairs.Count;
            var sorted = new KeyValuePair<string, string>[count];
            
            int i = 0;
            var replacePairsEnum = replacePairs.GetFastEnumerator();
            while (replacePairsEnum.MoveNext())
            {
                var key = replacePairsEnum.CurrentKey.ToString();
                var value = replacePairsEnum.CurrentValue.ToString();

                if (key.Length == 0)
                {
                    //// TODO: an exception ?
                    //return null;
                    throw new ArgumentException();
                }

                sorted[i++] = new KeyValuePair<string, string>(key, value);
            }

            Array.Sort<KeyValuePair<string, string>>(sorted, new KeyLengthComparer());

            // perform replacement
            StringBuilder result = new StringBuilder(str);
            StringBuilder temp = new StringBuilder(str);
            int length = str.Length;
            int[] offset = new int[length];

            for (i = 0; i < sorted.Length; i++)
            {
                var key = sorted[i].Key;
                int index = 0;

                while ((index = temp.ToString().IndexOf(key, index, StringComparison.Ordinal)) >= 0)   // ordinal search, because of exotic Unicode characters are find always at the beginning of the temp
                {
                    var value = sorted[i].Value;
                    var keyLength = key.Length;
                    int replaceAtIndex = index + offset[index];

                    // replace occurrence in result
                    result.Remove(replaceAtIndex, keyLength);
                    result.Insert(replaceAtIndex, value);

                    // Pack the offset array (drop the items removed from temp)
                    for (int j = index + keyLength; j < offset.Length; j++)
                        offset[j - keyLength] = offset[j];

                    // Ensure that we don't replace stuff that we already have worked on by
                    // removing the replaced substring from temp.
                    temp.Remove(index, keyLength);
                    for (int j = index; j < length; j++) offset[j] += value.Length;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// GetUserEntryPoint encode a string by shifting every letter (a-z, A-Z) by 13 places in the alphabet.
        /// </summary>
        /// <param name="str">The string to be encoded.</param>
        /// <returns>The string with characters rotated by 13 places.</returns>
        public static string str_rot13(string str)
        {
            return strtr(str,
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "nopqrstuvwxyzabcdefghijklmNOPQRSTUVWXYZABCDEFGHIJKLM");
        }

        #endregion

        #region substr_count internals

        private static bool SubstringCountInternalCheck(string needle)
        {
            if (String.IsNullOrEmpty(needle))
            {
                //PhpException.InvalidArgument("needle", LibResources.GetString("arg:null_or_empty"));
                //return false;
                throw new ArgumentException();
            }

            return true;
        }
        private static bool SubstringCountInternalCheck(string haystack, int offset)
        {
            if (offset < 0)
            {
                //PhpException.InvalidArgument("offset", LibResources.GetString("substr_count_offset_zero"));
                //return false;
                throw new ArgumentException();
            }
            if (offset > haystack.Length)
            {
                //PhpException.InvalidArgument("offset", LibResources.GetString("substr_count_offset_exceeds", offset));
                //return false;
                throw new ArgumentException();
            }

            return true;
        }
        private static bool SubstringCountInternalCheck(string haystack, int offset, int length)
        {
            if (!SubstringCountInternalCheck(haystack, offset))
                return false;

            if (length == 0)
            {
                //PhpException.InvalidArgument("length", LibResources.GetString("substr_count_zero_length"));
                //return false;
                throw new ArgumentException();
            }
            if (offset + length > haystack.Length)
            {
                //PhpException.InvalidArgument("length", LibResources.GetString("substr_count_length_exceeds", length));
                //return false;
                throw new ArgumentException();
            }

            return true;
        }

        /// <summary>
        /// Count the number of substring occurrences. Expects correct argument values.
        /// </summary>
        internal static int SubstringCountInternal(string/*!*/ haystack, string/*!*/ needle, int offset, int end)
        {
            int result = 0;

            if (needle.Length == 1)
            {
                while (offset < end)
                {
                    if (haystack[offset] == needle[0]) result++;
                    offset++;
                }
            }
            else
            {
                while ((offset = haystack.IndexOf(needle, offset, end - offset)) != -1)
                {
                    offset += needle.Length;
                    result++;
                }
            }
            return result;
        }

        #endregion

        #region strtok

        /// <summary>
        /// Holds a context of <see cref="strtok(Context, string)"/> method.
        /// </summary>
        private sealed class TokenizerContext
        {
            /// <summary>
            /// The <b>str</b> parameter of last <see cref="Tokenize"/> method call.
            /// </summary>
            public string String;

            /// <summary>
            /// Current position in <see cref="TokenizerContext"/>.
            /// </summary>
            public int Position;

            /// <summary>
            /// The length of <see cref="TokenizerContext"/>.
            /// </summary>
            public int Length;

            /// <summary>
            /// Initializes the context.
            /// </summary>
            /// <param name="str"></param>
            public void Initialize(string str)
            {
                Debug.Assert(str != null);

                this.String = str;
                this.Length = str.Length;
                this.Position = 0;
            }

            /// <summary>
            /// Splits current string from current position into tokens using given set of delimiter characters.
            /// Tokenizes the string that was passed to a previous call of <see cref="Initialize"/>.
            /// </summary>
            public string Tokenize(string delimiters)
            {
                if (this.Position >= this.Length) return null;
                if (delimiters == null) delimiters = String.Empty;

                int index;
                char[] delChars = delimiters.ToCharArray();
                while ((index = this.String.IndexOfAny(delChars, this.Position)) == this.Position)
                {
                    if (this.Position == this.Length - 1) return null; // last char is delimiter
                    this.Position++;
                }

                string token;
                if (index == -1) // delimiter not found
                {
                    token = this.String.Substring(this.Position);
                    this.Position = this.Length;
                    return token;
                }

                token = this.String.Substring(this.Position, index - this.Position);
                this.Position = index + 1;
                return token;
            }

            /// <summary>
            /// Empty constructor.
            /// </summary>
            public TokenizerContext() { }
        }

        /// <summary>
        /// Splits a string into tokens using given set of delimiter characters. Tokenizes the string
        /// that was passed to a previous call of the two-parameter version.
        /// </summary>
        /// <param name="delimiters">Set of delimiters.</param>
        /// <returns>The next token or a <B>null</B> reference.</returns>
        /// <remarks>This method implements the behavior introduced with PHP 4.1.0, i.e. empty tokens are
        /// skipped and never returned.</remarks>
        //[return: CastToFalse]
        public static string strtok(Context ctx, string delimiters)
        {
            return ctx.GetStatic<TokenizerContext>().Tokenize(delimiters);
        }

        /// <summary>
        /// Splits a string into tokens using given set of delimiter characters.
        /// </summary>
        /// <param name="str">The string to tokenize.</param>
        /// <param name="delimiters">Set of delimiters.</param>
        /// <returns>The first token or null. Call one-parameter version of this method to get next tokens.
        /// </returns>
        /// <remarks>This method implements the behavior introduced with PHP 4.1.0, i.e. empty tokens are
        /// skipped and never returned.</remarks>
        //[return: CastToFalse]
        public static string strtok(Context ctx, string str, string delimiters)
        {
            if (str == null)
                str = String.Empty;

            var tctx = ctx.GetStatic<TokenizerContext>();
            tctx.Initialize(str);
            return tctx.Tokenize(delimiters);
        }

        #endregion

        #region trim, rtrim, ltrim, chop

        /// <summary>
        /// Strips whitespace characters from the beginning and end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <returns>The trimmed string.</returns>
        /// <remarks>This one-parameter version trims '\0', '\t', '\n', '\r', '\x0b' and ' ' (space).</remarks>
        public static string trim(string str) => trim(str, "\0\t\n\r\x0b\x20");

        /// <summary>
        /// Strips given characters from the beginning and end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <param name="whiteSpaceCharacters">The characters to strip from <paramref name="str"/>. Can contain ranges
        /// of characters, e.g. "\0x00..\0x1F".</param>
        /// <returns>The trimmed string.</returns>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> is invalid char mask. Multiple errors may be printed out.</exception>
        /// <exception cref="PhpException"><paramref name="str"/> contains Unicode characters greater than '\u0800'.</exception>
        public static string trim(string str, string whiteSpaceCharacters)
        {
            if (str == null) return String.Empty;

            // As whiteSpaceCharacters may contain intervals, I see two possible implementations:
            // 1) Call CharMap.AddUsingMask and do the trimming "by hand".
            // 2) Write another version of CharMap.AddUsingMask that would return char[] of characters
            // that fit the mask, and do the trimming with String.Trim(char[]).
            // I have chosen 1).

            CharMap charmap = InitializeCharMap();

            // may throw an exception:
            try
            {
                charmap.AddUsingMask(whiteSpaceCharacters);
            }
            catch (IndexOutOfRangeException)
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("unicode_characters"));
                //return null;
                throw;
            }

            int length = str.Length, i = 0, j = length - 1;

            // finds the new beginning:
            while (i < length && charmap.Contains(str[i])) i++;

            // finds the new end:
            while (j >= 0 && charmap.Contains(str[j])) j--;

            return (i <= j) ? str.Substring(i, j - i + 1) : String.Empty;
        }

        /// <summary>Characters treated as blanks by the PHP.</summary>
        private static char[] phpBlanks = new char[] { '\0', '\t', '\n', '\r', '\u000b', ' ' };

        /// <summary>
        /// Strips whitespace characters from the beginning of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <returns>The trimmed string.</returns>
        /// <remarks>This one-parameter version trims '\0', '\t', '\n', '\r', '\u000b' and ' ' (space).</remarks>
        public static string ltrim(string str)
        {
            return (str != null) ? str.TrimStart(phpBlanks) : String.Empty;
        }

        /// <summary>
        /// Strips given characters from the beginning of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <param name="whiteSpaceCharacters">The characters to strip from <paramref name="str"/>. Can contain ranges
        /// of characters, e.g. \0x00..\0x1F.</param>
        /// <returns>The trimmed string.</returns>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> is invalid char mask. Multiple errors may be printed out.</exception>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> contains Unicode characters greater than '\u0800'.</exception>
        public static string ltrim(string str, string whiteSpaceCharacters)
        {
            if (str == null) return String.Empty;

            CharMap charmap = InitializeCharMap();

            // may throw an exception:
            try
            {
                charmap.AddUsingMask(whiteSpaceCharacters);
            }
            catch (IndexOutOfRangeException)
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("unicode_characters"));
                //return null;
                throw;
            }

            int length = str.Length, i = 0;

            while (i < length && charmap.Contains(str[i])) i++;

            if (i < length) return str.Substring(i);
            return String.Empty;
        }

        /// <summary>
        /// Strips whitespace characters from the end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <returns>The trimmed string.</returns>
        /// <remarks>This one-parameter version trims '\0', '\t', '\n', '\r', '\u000b' and ' ' (space).</remarks>
        public static string rtrim(string str)
        {
            return (str != null) ? str.TrimEnd(phpBlanks) : String.Empty;
        }

        /// <summary>
        /// Strips given characters from the end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <param name="whiteSpaceCharacters">The characters to strip from <paramref name="str"/>. Can contain ranges
        /// of characters, e.g. \0x00..\0x1F.</param>
        /// <returns>The trimmed string.</returns>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> is invalid char mask. Multiple errors may be printed out.</exception>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> contains Unicode characters greater than '\u0800'.</exception>
        public static string rtrim(string str, string whiteSpaceCharacters)
        {
            if (str == null) return String.Empty;

            CharMap charmap = InitializeCharMap();

            try
            {
                charmap.AddUsingMask(whiteSpaceCharacters);
            }
            catch (IndexOutOfRangeException)
            {
                //PhpException.Throw(PhpError.Warning, LibResources.GetString("unicode_characters"));
                //return null;
                throw;
            }

            int j = str.Length - 1;

            while (j >= 0 && charmap.Contains(str[j])) j--;

            return (j >= 0) ? str.Substring(0, j + 1) : String.Empty;
        }

        /// <summary>
        /// Strips whitespace characters from the end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <returns>The trimmed string.</returns>
        /// <remarks>This one-parameter version trims '\0', '\t', '\n', '\r', '\u000b' and ' ' (space).</remarks>
        public static string chop(string str)
        {
            return rtrim(str);
        }

        /// <summary>
        /// Strips given characters from the end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <param name="whiteSpaceCharacters">The characters to strip from <paramref name="str"/>. Can contain ranges
        /// of characters, e.g. \0x00..\0x1F.</param>
        /// <returns>The trimmed string.</returns>
        /// <exception cref="PhpException">Thrown if <paramref name="whiteSpaceCharacters"/> is invalid char mask. Multiple errors may be printed out.</exception>
        public static string chop(string str, string whiteSpaceCharacters)
        {
            return rtrim(str, whiteSpaceCharacters);
        }

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

        #region number_format, money_format

        /// <summary>
        /// Formats a number with grouped thousands.
        /// </summary>
        /// <param name="number">The number to format.</param>
        /// <returns>String representation of the number without decimals (rounded) with comma between every group
        /// of thousands.</returns>
        public static string number_format(double number)
        {
            return number_format(number, 0, ".", ",");
        }

        /// <summary>
        /// Formats a number with grouped thousands and with given number of decimals.
        /// </summary>
        /// <param name="number">The number to format.</param>
        /// <param name="decimals">The number of decimals.</param>
        /// <returns>String representation of the number with <paramref name="decimals"/> decimals with a dot in front, and with 
        /// comma between every group of thousands.</returns>
        public static string number_format(double number, int decimals)
        {
            return number_format(number, decimals, ".", ",");
        }

        /// <summary>
        /// Formats a number with grouped thousands, with given number of decimals, with given decimal point string
        /// and with given thousand separator.
        /// </summary>
        /// <param name="number">The number to format.</param>
        /// <param name="decimals">The number of decimals within range 0 to 99.</param>
        /// <param name="decimalPoint">The string to separate integer part and decimals.</param>
        /// <param name="thousandsSeparator">The character to separate groups of thousands. Only the first character
        /// of <paramref name="thousandsSeparator"/> is used.</param>
        /// <returns>
        /// String representation of the number with <paramref name="decimals"/> decimals with <paramref name="decimalPoint"/> in 
        /// front, and with <paramref name="thousandsSeparator"/> between every group of thousands.
        /// </returns>
        /// <remarks>
        /// The <b>number_format</b> (<see cref="FormatNumber"/>) PHP function requires <paramref name="decimalPoint"/> and <paramref name="thousandsSeparator"/>
        /// to be of length 1 otherwise it uses default values (dot and comma respectively). As this behavior does
        /// not make much sense, this method has no such limitation except for <paramref name="thousandsSeparator"/> of which
        /// only the first character is used (documented feature).
        /// </remarks>
        public static string number_format(double number, int decimals, string decimalPoint, string thousandsSeparator)
        {
            System.Globalization.NumberFormatInfo format = new System.Globalization.NumberFormatInfo();

            if ((decimals >= 0) && (decimals <= 99))
            {
                format.NumberDecimalDigits = decimals;
            }
            else
            {
                //PhpException.InvalidArgument("decimals", LibResources.GetString("arg:out_of_bounds", decimals));
                throw new ArgumentException();
            }

            if (!string.IsNullOrEmpty(decimalPoint))
            {
                format.NumberDecimalSeparator = decimalPoint;
            }

            if (thousandsSeparator == null) thousandsSeparator = String.Empty;

            switch (thousandsSeparator.Length)
            {
                case 0: format.NumberGroupSeparator = String.Empty; break;
                case 1: format.NumberGroupSeparator = thousandsSeparator; break;
                default: format.NumberGroupSeparator = thousandsSeparator.Substring(0, 1); break;
            }

            return number.ToString("N", format);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public static string money_format(string format, double number)
        {
            throw new NotImplementedException();
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
