using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Pchp.Core;
using Pchp.Library.Resources;

namespace Pchp.Library
{
    [PhpExtension("ereg")]
    public static class RegExpPosix
    {
        #region split, spliti

        /// <summary>
        /// Splits string <c>str</c> to arrays of strings by regular expression <c>pattern</c>.
        /// Returned array contains max. <c>limit</c> elements. If there is more substrings in <c>str</c>,
        /// array contains only first <c>limit-1</c> substrings and last element contains the whole rest
        /// of input <c>string</c>.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="pattern">Regular expression matching string delimiters.</param>
        /// <param name="str">String to split according to regular expression.</param>
        /// <param name="limit">Max number of elements in returned array.</param>
        /// <returns>Array of substrings.</returns>
        [Obsolete]
        public static PhpArray split(Context ctx, string pattern, string str, int limit = -1)
        {
            PhpException.FunctionDeprecated("split");

            return DoSplit(
                ctx,
                pattern,
                str,
                limit,
                limit >= 0,   // use limit (previous parameter)
                false         // case sensitive
                );
        }

        /// <summary>
        /// Splits string <c>str</c> to arrays of strings by regular expression <c>pattern</c>
        /// in case insensitive way.
        /// Returned array contains max. <c>limit</c> elements. If there is more substrings in <c>str</c>,
        /// array contains only first <c>limit-1</c> substrings and last element contains the whole rest
        /// of input <c>string</c>.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="pattern">Regular expression matching string delimiters.</param>
        /// <param name="str">String to split according to regular expression.</param>
        /// <param name="limit">Max number of elements in returned array.</param>
        /// <returns>Array of substrings.</returns>
        [Obsolete]
        public static PhpArray spliti(Context ctx, string pattern, string str, int limit = -1)
        {
            PhpException.FunctionDeprecated("spliti");

            return DoSplit(
                ctx,
                pattern,
                str,
                limit,
                limit >= 0,   // use limit (previous parameter)
                true          // ignore case
                );
        }

        /// <summary>
        /// Implementation of functions family "split"
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="pattern">POSIX regular expression that match delimiter.</param>
        /// <param name="str">String to split.</param>
        /// <param name="limit">Maximum elements of output array.</param>
        /// <param name="useLimit">True if you want to use previous parameter.</param>
        /// <param name="ignoreCase">True if <c>str</c> is matched case insensitive, false otherwise.</param>
        /// <returns>Array containing parts of str</returns>
        private static PhpArray DoSplit(Context ctx, string pattern, string str, int limit, bool useLimit, bool ignoreCase)
        {
            System.Array sAr;

            if (useLimit)
            {
                // in PHP limit < 1 means the same as 1, no error or warning is written
                if (limit < 1)
                {
                    PhpException.Throw(PhpError.Warning, LibResources.limit_less_than_one);
                    limit = 1;
                }

                // there is no static method with parameter "limit", we must instantiate Regex
                var reg = new Regex(ConvertPosix2DotNetExpr(ctx, pattern));
                sAr = reg.Split(str, limit);
            }
            else
            {
                // we can use static method
                sAr = Regex.Split(str, ConvertPosix2DotNetExpr(ctx, pattern));
            }

            return new PhpArray(sAr, 0, sAr.Length);
        }

        #endregion

        #region BracketExpression class

        /// <summary>
        /// Class representing one bracket expression ([...]) in whole regular expression.
        /// </summary>
        /// <remarks>
        /// While converting POSIX regular expression to framework regexp if we determine start
        /// of bracket expression, characters are written to this class and it controls regexp processing.
        /// Nothing is written to output while processing, results are stored in this class and the
        /// converted bracket expression is written at once after to the output.
        /// </remarks>
        internal class BracketExpression
        {
            /* vocabulary used in function naming:
			 * COUNT sth. means get some string to appropriate input (character class name, range endpoints...)
			 * UNROLL sth. means replace some parts in buffer with COUNTED string
			 * OPTIMIZE means make some change in buffer that doesn't change meaning of reg. expr.
			 */

            // string containing partially processed regular expression
            // in this string are written several parts, which substrings are which parts indicates parts array 
            private StringBuilder _buffer;

            const int initialNumberOfParts = 30;
            private int[] _parts; // -1 means no part
            private PartType[] _partTypes;
            private int currentPartNumber;

            private bool negation;

            readonly Context _ctx;

            /// <summary>
            /// indicates whether or not some failure occurs
            /// </summary>
            private bool allOK;

            public enum PartType
            {
                Normal, NormalNoRangeNext, NormalBackslash, NormalBackslashNoRangeNext,
                CollatingElement, UnrolledCollatingElement,
                CharacterClass, UnrolledCharacterClass, WordBoundary,
                EquivalenceClass, UnrolledEquivalenceClass,
                Range, UnrolledRange
            };

            /// <summary>
            /// indicates if there is a part that cannot be converted to single [...] expression, for example
            /// [xyz[.abcd.]opq]. Is set to "true" in optimization if some part is marked as <c>UnrolledCollatingElement</c>
            /// or <c>UnrolledEquivalenceClass</c>
            /// </summary>
            private bool specialTranslationNeeded;

            /// <summary>
            /// Indicates that this bracket expression has '^' as the first character.
            /// </summary>
            public bool Negation
            {
                set { negation = value; }
            }

            public PartType CurrentPartType
            {
                set
                {
                    if (value == PartType.Range)
                    {
                        StartNextPart();
                        _partTypes[currentPartNumber] = value;
                        _buffer.Append('-'); // so that the part wasn't empty and we can simply convert it to PartType.Normal
                        StartNextPart();
                    }
                    else
                    {
                        _partTypes[currentPartNumber] = value;
                    }
                }
            }

            /// <summary>
            /// According to currentPartNumber part makes some changes at the end of buffer. Assumes that
            /// currentPartNumber is last in buffer.
            /// Sets allOK to false if something is wrong.
            /// 
            /// BEFORE optimization there can be:
            /// Range, Normal, NormalBackslash, CharacterClass, CollatingElement, EquivalenceClass
            ///
            /// AFTER optimization there can be:
            /// Range, UnrolledRange, Normal, NormalBackslash, NormalBackslashNoRangeNext, NormalNoRangeNext,
            /// UnrolledCharacterClass, WordBoundary, UnrolledCollatingElement, UnrolledEquivalenceClass
            /// </summary>
            private void OptimizeParts()
            {
                switch (_partTypes[currentPartNumber])
                {
                    case PartType.Range:
                        OptimizeRange(); // transformed to Range or UnrolledRange
                        break;

                    case PartType.Normal:
                        OptimizeNormal();
                        break;

                    case PartType.NormalBackslash:
                        OptimizeNormalBackslash();
                        break;

                    case PartType.CharacterClass: //transformed to UnrolledCharacterClass or WordBoundary
                        OptimizeCharacterClass();
                        break;

                    case PartType.CollatingElement: // transformed to Normal, NormalBackslash or UnrolledCollatingElement
                        OptimizeCollatingElement();
                        break;

                    case PartType.EquivalenceClass:
                        OptimizeEquivalenceClass();
                        break;

                    default:
                        Debug.Fail("Unexpected part type in OptimizeParts().");
                        break;

                        /* These possibilities cannot occur here, they arise during optimization
                         * case PartType.UnrolledRange:
                         * case PartType.NormalNoRangeNext:
                         * case PartType.NormalBackslashNoRangeNext;
                         * case PartType.UnrolledCharacterClass:
                         * case PartType.WordBoundary:
                         * case PartType.UnrolledEquivalenceClass:
                        */
                }

            }

            /// <summary>
            /// Assume that currentPartNumber is set to Range and is last in buffer.
            /// According to previous part type changes types and groups some parts.
            /// Sets allOK to false if something is wrong.
            /// </summary>
            private void OptimizeRange()
            {
                if (currentPartNumber == 0)
                {
                    _partTypes[currentPartNumber] = PartType.NormalNoRangeNext; // '-' is first character in expression
                }
                else
                {
                    switch (_partTypes[currentPartNumber - 1])
                    {
                        case PartType.Range: // replace [x-y] with correct chars
                            string countedRange = CountRangeEscaped(_buffer[_parts[currentPartNumber - 2]], '-');
                            currentPartNumber = currentPartNumber - 2;
                            _parts[currentPartNumber + 1] = -1;
                            _buffer.Remove(_parts[currentPartNumber], _buffer.Length - _parts[currentPartNumber]);
                            _buffer.Append(countedRange);
                            _partTypes[currentPartNumber] = PartType.UnrolledRange;

                            break;

                        case PartType.UnrolledRange:
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("range_shared_endpoint"));
                            allOK = false;
                            break;

                        case PartType.Normal:
                            if ((_parts[currentPartNumber] - _parts[currentPartNumber - 1]) > 1)
                            // divide part in order that we have one character before range
                            {
                                // create next part, but do not optimize to avoid infinite recursive call
                                StartNextPart(false); // currentPartNumber is now greater by 1
                                _parts[currentPartNumber] = _parts[currentPartNumber - 1];
                                _partTypes[currentPartNumber] = _partTypes[currentPartNumber - 1];
                                _parts[currentPartNumber - 1]--;
                                _partTypes[currentPartNumber - 1] = PartType.Normal;
                            }
                            break;

                        case PartType.NormalBackslash:
                            break;

                        case PartType.NormalNoRangeNext:
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("range_not_allowed"));
                            allOK = false;
                            break;

                        case PartType.NormalBackslashNoRangeNext:
                            goto case PartType.NormalNoRangeNext;

                        case PartType.UnrolledCharacterClass:
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("first_endpoint_character_class"));
                            allOK = false;
                            break;

                        case PartType.WordBoundary:
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("word_boundary_not_single_bracket_expr"));
                            allOK = false;
                            break;

                        case PartType.UnrolledCollatingElement:
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("bad_collating_el_at_first_endpoint_of_range"));
                            allOK = false;
                            break;

                        case PartType.UnrolledEquivalenceClass:
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("equivalence_class_at_first_endpoint_of_range"));
                            allOK = false;
                            break;

                        default:
                            Debug.Fail("Internal error - default in OptimizeRange().");
                            break;

                    }
                }
            }

            private void OptimizeNormal()
            {
                if (currentPartNumber == 0) // if this is the first part, there is nothing to optimize
                    return;

                switch (_partTypes[currentPartNumber - 1])
                {
                    case PartType.Range:
                        string countedRange = CountRangeEscaped(_buffer[_parts[currentPartNumber - 2]], _buffer[_parts[currentPartNumber]]);
                        string normalLeft = _buffer.ToString(_parts[currentPartNumber] + 1, _buffer.Length - _parts[currentPartNumber] - 1);

                        _parts[currentPartNumber] = -1;
                        currentPartNumber = currentPartNumber - 1;
                        _buffer.Remove(_parts[currentPartNumber - 1], _buffer.Length - _parts[currentPartNumber - 1]);
                        _buffer.Append(countedRange);
                        _partTypes[currentPartNumber - 1] = PartType.UnrolledRange;
                        _parts[currentPartNumber] = _buffer.Length;
                        _buffer.Append(normalLeft);
                        _partTypes[currentPartNumber] = PartType.Normal;
                        break;

                    case PartType.WordBoundary:
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("word_boundary_not_single_bracket_expr"));
                        allOK = false;
                        break;
                }
            }

            private void OptimizeNormalBackslash()
            {
                if (currentPartNumber == 0)
                    return;

                switch (_partTypes[currentPartNumber - 1])
                {
                    case PartType.Range:
                        string countedRange = CountRangeEscaped(_buffer[_parts[currentPartNumber - 2]], '\\');
                        currentPartNumber = currentPartNumber - 2;
                        _parts[currentPartNumber + 1] = -1;
                        _buffer.Remove(_parts[currentPartNumber], _buffer.Length - _parts[currentPartNumber]);
                        _buffer.Append(countedRange);
                        _partTypes[currentPartNumber] = PartType.UnrolledRange;
                        break;

                    case PartType.WordBoundary:
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("word_boundary_not_single_bracket_expr"));
                        allOK = false;
                        break;
                }
            }

            /// <summary>
            /// Returns string containing appropriate characters for range according to current encoding. If some
            /// characters may have special meaning in the bracket expression they are escaped.
            /// Sets private variable allOK to false if secondCharacter is before firstCharacter in current encoding
            /// or the range cannot be counted for these characters.
            /// </summary>
            /// <param name="firstCharacter">First endpoint of the range.</param>
            /// <param name="secondCharacter">Second endpoint of the range.</param>
            /// <returns>String with all range characters.</returns>
            private string CountRangeEscaped(char firstCharacter, char secondCharacter)
            {
                // error indication of range conversion
                int result;
                // range characters
                string characters;

                if (CountRange(_ctx, firstCharacter, secondCharacter, out characters, out result))
                {
                    StringBuilder sb = new StringBuilder(characters.Length);
                    EscapeBracketExpressionSpecialChars(sb, characters);
                    return sb.ToString();
                }

                // there was an error
                switch (result)
                {
                    case 1:
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("ranges_not_supported"));
                        allOK = false;
                        break;
                    case 2:
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("range_first_character_greater"));
                        allOK = false;
                        break;
                    default:
                        Debug.Fail("Unexpected result error value from CountRange().");
                        break;
                }

                return null;
            }

            /// <summary>
            /// Escapes characters that have special meaning in bracket expression to make them ordinary characters.
            /// </summary>
            /// <param name="sb"><see cref="StringBuilder"/> to output the result.</param>
            /// <param name="chars">String possibly containing characters with special meaning.</param>
            /// <returns>String with escaped characters.</returns>
            internal static void EscapeBracketExpressionSpecialChars(StringBuilder/*!*/sb, string chars)
            {
                Debug.Assert(sb != null);

                for (int i = 0; i < chars.Length; i++)
                {
                    char ch = chars[i];
                    switch (ch)
                    {
                        // case '^': // not necessary, not at the beginning have no special meaning
                        case '\\':
                        case ']':
                        case '-':
                            sb.Append('\\');
                            goto default;
                        default:
                            sb.Append(ch);
                            break;
                    }
                }
            }

            /// <summary>
            /// Takes endpoints of a range and returns string containing appropriate characters.
            /// </summary>
            /// <param name="ctx">Runtime context.</param>
            /// <param name="firstCharacter">First endpoint of a range.</param>
            /// <param name="secondCharacter">Second endpoint of a range.</param>
            /// <param name="characters">String containing all characters that are to be in the range.</param>
            /// <param name="result">Integer specifying an error. Value 1 means characters specified cannot
            /// be expressed in current encoding, value of 2 first character is greater than second.</param>
            /// <returns><B>True</B> if range was successfully counted, <B>false</B> otherwise.</returns>
            internal static bool CountRange(Context ctx, char firstCharacter, char secondCharacter, out string characters, out int result)
            {
                // initialize out parameters
                characters = null;
                result = 0;

                Encoding encoding = ctx.StringEncoding; // Configuration.Application.Globalization.PageEncoding;
                char[] chars = new char[2];
                chars[0] = firstCharacter;
                chars[1] = secondCharacter;

                byte[] two_bytes = new byte[encoding.GetMaxByteCount(2)];

                // convert endpoints and test if characters are "normal" - they can be stored in one byte
                if (encoding.GetBytes(chars, 0, 2, two_bytes, 0) != 2)
                {
                    result = 1;
                    return false;
                }

                if (two_bytes[0] > two_bytes[1])
                {
                    result = 2;
                    return false;
                }

                // array for bytes that will be converted to unicode string
                byte[] bytes = new byte[two_bytes[1] - two_bytes[0] + 1];

                int i = 0;
                for (int ch = two_bytes[0]; ch <= two_bytes[1]; i++, ch++)
                {
                    // casting to byte is OK, ch is always in byte range thanks to ch <= two_bytes[1] condition
                    bytes[i] = (byte)ch;
                }

                characters = encoding.GetString(bytes, 0, i);
                return true;
            }

            /// <summary>
            /// Takes character class name and returns string containing appropriate characters.
            /// Returns <B>null</B> if has got unknown character class name.
            /// </summary>
            /// <param name="chClassName">Character class name.</param>
            /// <returns>String containing characters from character class.</returns>
            internal static string CountCharacterClass(string chClassName)
            {
                string ret = null;

                switch (chClassName)
                {
                    case "alnum":
                        ret = @"\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}";
                        break;
                    case "digit":
                        ret = @"\p{Nd}";
                        break;
                    case "punct":
                        ret = @"\p{P}\p{S}";
                        break;
                    case "alpha":
                        ret = @"\p{Ll}\p{Lu}\p{Lt}\p{Lo}";
                        break;
                    case "graph":
                        ret = @"\p{L}\p{M}\p{N}\p{P}\p{S}";
                        break;
                    case "space":
                        ret = @"\s";
                        break;
                    case "blank":
                        ret = @" \t";
                        break;
                    case "lower":
                        ret = @"\p{Ll}";
                        break;
                    case "upper":
                        ret = @"\p{Lu}";
                        break;
                    case "cntrl":
                        ret = @"\p{Cc}";
                        break;
                    case "print":
                        ret = @"\p{L}\p{M}\p{N}\p{P}\p{S}\p{Zs}";
                        break;
                    case "xdigit":
                        ret = @"abcdefABCDEF\d";
                        break;
                    case "ascii":
                        ret = @"\u0000-\u007F";
                        break;
                    case "word":
                        ret = @"_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}";
                        break;
                }

                return ret;
            }

            /// <summary>
            /// Replaces CharacterClass name with appropriate characters and changes type to UnrolledCharacterClass.
            /// </summary>
            /// <remarks>
            /// Assumes that currentPartNumber is last part in buffer and has type CharacterClass.
            /// </remarks>
            private void OptimizeCharacterClass()
            {
                if (currentPartNumber > 0)
                {
                    if (_partTypes[currentPartNumber - 1] == PartType.Range)
                    {
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("character_class_at_second_endpoint_of_range"));
                        allOK = false;
                        return;
                    }
                    else if (_partTypes[currentPartNumber - 1] == PartType.WordBoundary)
                    {
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("word_boundary_not_single_bracket_expr"));
                        allOK = false;
                        return;
                    }
                }

                int length = _buffer.Length - _parts[currentPartNumber];
                string oldValue = _buffer.ToString(_parts[currentPartNumber], length);

                if (currentPartNumber == 0 && (oldValue == "<" || oldValue == ">"))
                {
                    _partTypes[currentPartNumber] = PartType.WordBoundary;
                    return;
                }

                string cce = CountCharacterClass(oldValue);
                if (cce == null)
                {
                    PhpException.Throw(PhpError.Warning, LibResources.GetString("unknown_character_class"));
                    allOK = false;
                    cce = "";
                }

                _partTypes[currentPartNumber] = PartType.UnrolledCharacterClass;
                _buffer.Remove(_parts[currentPartNumber], length);
                _buffer.Append(cce);
            }


            /// <summary>
            /// Takes collating element "name" (string that was enclosed in [. and .]) and returns appropriate
            /// character(s) or the original string, if there no association exist.
            /// </summary>
            /// <param name="cElement">String that was enclosed in [. and .] (collating element).</param>
            /// <returns>String associated with cElement.</returns>
            private string CountCollatingElement(string cElement)
            {
                string ret;

                switch (cElement)
                {
                    case "NUL": ret = "\x0"; break;
                    case "SOH": ret = "\x1"; break;
                    case "STX": ret = "\x2"; break;
                    case "ETX": ret = "\x3"; break;
                    case "EOT": ret = "\x4"; break;
                    case "ENQ": ret = "\x5"; break;
                    case "ACK": ret = "\x6"; break;
                    case "BEL": ret = "\x7"; break;
                    case "alert": ret = "\x7"; break;
                    case "BS": ret = "\x8"; break;
                    case "backspace": ret = "\b"; break;
                    case "HT": ret = "\x9"; break;
                    case "tab": ret = "\t"; break;
                    case "LF": ret = "\xa"; break;
                    case "newline": ret = "\n"; break;
                    case "VT": ret = "\xb"; break;
                    case "vertical-tab": ret = "\v"; break;
                    case "FF": ret = "\xc"; break;
                    case "form-feed": ret = "\f"; break;
                    case "CR": ret = "\xd"; break;
                    case "carriage-return": ret = "\r"; break;
                    case "SO": ret = "\xe"; break;
                    case "SI": ret = "\xf"; break;
                    case "DLE": ret = "\x10"; break;
                    case "DC1": ret = "\x11"; break;
                    case "DC2": ret = "\x12"; break;
                    case "DC3": ret = "\x13"; break;
                    case "DC4": ret = "\x14"; break;
                    case "NAK": ret = "\x15"; break;
                    case "SYN": ret = "\x16"; break;
                    case "ETB": ret = "\x17"; break;
                    case "CAN": ret = "\x18"; break;
                    case "EM": ret = "\x19"; break;
                    case "SUB": ret = "\x1a"; break;
                    case "ESC": ret = "\x1b"; break;
                    case "IS4": ret = "\x1c"; break;
                    case "FS": ret = "\x1c"; break;
                    case "IS3": ret = "\x1d"; break;
                    case "GS": ret = "\x1d"; break;
                    case "IS2": ret = "\x1e"; break;
                    case "RS": ret = "\x1e"; break;
                    case "IS1": ret = "\x1f"; break;
                    case "US": ret = "\x1f"; break;
                    case "space": ret = " "; break;
                    case "exclamation-mark": ret = "!"; break;
                    case "quotation-mark": ret = "\""; break;
                    case "number-sign": ret = "#"; break;
                    case "dollar-sign": ret = "$"; break;
                    case "percent-sign": ret = "%"; break;
                    case "ampersand": ret = "&"; break;
                    case "apostrophe": ret = "\'"; break;
                    case "left-parenthesis": ret = "("; break;
                    case "right-parenthesis": ret = ")"; break;
                    case "asterisk": ret = "*"; break;
                    case "plus-sign": ret = "+"; break;
                    case "comma": ret = ","; break;
                    case "hyphen": ret = "-"; break;
                    case "hyphen-minus": ret = "-"; break;
                    case "period": ret = "."; break;
                    case "full-stop": ret = "."; break;
                    case "slash": ret = "/"; break;
                    case "solidus": ret = "/"; break;
                    case "zero": ret = "0"; break;
                    case "one": ret = "1"; break;
                    case "two": ret = "2"; break;
                    case "three": ret = "3"; break;
                    case "four": ret = "4"; break;
                    case "five": ret = "5"; break;
                    case "six": ret = "6"; break;
                    case "seven": ret = "7"; break;
                    case "eight": ret = "8"; break;
                    case "nine": ret = "9"; break;
                    case "colon": ret = ":"; break;
                    case "semicolon": ret = ";"; break;
                    case "less-than-sign": ret = "<"; break;
                    case "equals-sign": ret = "="; break;
                    case "greater-than-sign": ret = ">"; break;
                    case "question-mark": ret = "?"; break;
                    case "commercial-at": ret = "@"; break;
                    case "left-square-bracket": ret = "["; break;
                    case "backslash": ret = "\\"; break;
                    case "reverse-solidus": ret = "\\"; break;
                    case "right-square-bracket": ret = "]"; break;
                    case "circumflex": ret = "^"; break;
                    case "circumflex-accent": ret = "^"; break;
                    case "underscore": ret = "_"; break;
                    case "low-line": ret = "_"; break;
                    case "grave-accent": ret = "`"; break;
                    case "left-brace": ret = "{"; break;
                    case "left-curly-bracket": ret = "{"; break;
                    case "vertical-line": ret = "|"; break;
                    case "right-brace": ret = "}"; break;
                    case "right-curly-bracket": ret = "}"; break;
                    case "tilde": ret = "~"; break;
                    case "DEL": ret = "\x7f"; break;
                    case "NULL": ret = "\x0"; break;
                    default:
                        ret = cElement;
                        break;
                }
                return ret;
            }


            /// <summary>
            /// Assumes that currentPartNumber is last part in buffer and has type CollatingElement.
            /// Changes it to Normal, NormalBackslash or UnrolledCollatingElement.
            /// </summary>
            private void OptimizeCollatingElement()
            {
                if (currentPartNumber > 0 && (_partTypes[currentPartNumber - 1] == PartType.WordBoundary))
                {
                    PhpException.Throw(PhpError.Warning, LibResources.GetString("word_boundary_not_single_bracket_expr"));
                    allOK = false;
                    return;
                }
                int length = _buffer.Length - _parts[currentPartNumber];
                string cce = CountCollatingElement(_buffer.ToString(_parts[currentPartNumber], length));
                _buffer.Remove(_parts[currentPartNumber], length);
                _buffer.Append(cce);

                if (cce.Length == 1)
                {
                    _partTypes[currentPartNumber] = PartType.Normal;
                    OptimizeNormal();
                }
                else if (cce == @"\\")
                {
                    cce = @"\";
                    _partTypes[currentPartNumber] = PartType.NormalBackslash;
                    OptimizeNormalBackslash();
                }
                else
                {
                    _partTypes[currentPartNumber] = PartType.UnrolledCollatingElement;
                    specialTranslationNeeded = true;
                }
            }


            /// <summary>
            /// Calls OptimizeCollatingElement() and sets current part type to UnrolledEquivalenceClass.
            /// </summary>
            private void OptimizeEquivalenceClass()
            {
                if (currentPartNumber > 0)
                {
                    if (_partTypes[currentPartNumber - 1] == PartType.Range)
                    {
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("equivalence_class_at_second_endpoint_of_range"));
                        allOK = false;
                        return;
                    }
                    else if (_partTypes[currentPartNumber - 1] == PartType.WordBoundary)
                    {
                        PhpException.Throw(PhpError.Warning, LibResources.GetString("word_boundary_not_single_bracket_expr"));
                        allOK = false;
                        return;
                    }
                }

                // we have no equivalence classes defined, so [= =] is the same as [. .] according to specification
                OptimizeCollatingElement();

                switch (_partTypes[currentPartNumber])
                {
                    case PartType.UnrolledCollatingElement:
                        _partTypes[currentPartNumber] = PartType.UnrolledEquivalenceClass;
                        break;
                    case PartType.Normal:
                        _partTypes[currentPartNumber] = PartType.NormalNoRangeNext;
                        break;
                    case PartType.NormalBackslash:
                        _partTypes[currentPartNumber] = PartType.NormalBackslashNoRangeNext;
                        break;
                }
            }


            /// <summary>
            /// If something was written to current part, creates next empty part.
            /// </summary>
            /// <remarks>
            /// Important to easy automaton implementation: if current part is empty, does nothing!
            /// Appending new parts is unlimited, arrays are enlarged if needed.
            /// </remarks>
            public bool StartNextPart()
            {
                return StartNextPart(true);
            }

            /// <summary>
            /// If something was written to current part, creates next empty part.
            /// </summary>
            /// <remarks>
            /// Important to easy automaton implementation: if current part is empty, does nothing!
            /// Appending new parts is unlimited, arrays are enlarged if needed.
            /// </remarks>
            /// <param name="optimize">Whether call OptimizeParts().</param>
            /// <returns>True if everything is OK.</returns>
            private bool StartNextPart(bool optimize)
            {
                if (!allOK)
                    return false;

                if (_parts[currentPartNumber] == _buffer.Length)
                    // we don't need to make new part, current part is empty
                    return true;

                if (optimize)
                    OptimizeParts();

                currentPartNumber++;
                if (currentPartNumber == _parts.Length) // array is full, enlarge it
                {
                    int[] tempArray = _parts;
                    _parts = new int[_parts.Length * 2];
                    System.Array.Copy(tempArray, _parts, tempArray.Length);

                    PartType[] tempArray2 = _partTypes;
                    _partTypes = new PartType[_parts.Length * 2];
                    System.Array.Copy(tempArray2, _partTypes, tempArray2.Length);
                }

                _parts[currentPartNumber] = _buffer.Length; // set position after last character in buffer
                _partTypes[currentPartNumber] = PartType.Normal;
                _parts[currentPartNumber + 1] = -1;

                return true;
            }

            /// <summary>
            /// Constructor. Sets all fields to appropriate empty values.
            /// </summary>
            public BracketExpression(Context ctx)
            {
                _ctx = ctx;

                _buffer = new StringBuilder();
                _parts = new int[initialNumberOfParts];
                _partTypes = new PartType[initialNumberOfParts];

                Reset();
            }

            /// <summary>
            /// Sets inner state as if the object was created.
            /// </summary>
            public void Reset()
            {
                _buffer.Remove(0, _buffer.Length);
                currentPartNumber = 0;

                _parts[0] = 0;
                _parts[1] = -1;
                _partTypes[0] = PartType.Normal;
                // if these arrays were enlarged, we keep the same lenght, shortening is pointless

                negation = false;
                allOK = true;
                specialTranslationNeeded = false;
            }

            /// <summary>
            /// Appends one character to current part of bracket expression.
            /// </summary>
            /// <param name="ch">Character to append.</param>
            public void Append(char ch)
            {
                _buffer.Append(ch);
            }

            /// <summary>
            /// Appends two characters to current part of bracket expression.
            /// </summary>
            /// <param name="ch1">First character to append.</param>
            /// <param name="ch2">Second character to append.</param>
            public void Append(char ch1, char ch2)
            {
                _buffer.Append(ch1);
                _buffer.Append(ch2);
            }

            /// <summary>
            /// Appends three characters to current part of bracket expression.
            /// </summary>
            /// <param name="ch1">First character to append.</param>
            /// <param name="ch2">Second character to append.</param>
            /// <param name="ch3">Third character to append.</param>
            public void Append(char ch1, char ch2, char ch3)
            {
                _buffer.Append(ch1);
                _buffer.Append(ch2);
                _buffer.Append(ch3);
            }


            /// <summary>
            /// 
            /// </summary>
            /// <param name="output"></param>
            private void WriteOutNoSpecial(ref StringBuilder output)
            {
                for (int i = 0; i < currentPartNumber; i++)
                {
                    if (_partTypes[i] == PartType.NormalBackslash || _partTypes[i] == PartType.NormalBackslashNoRangeNext)
                        output.Append(@"\");
                    else
                        output.Append(_buffer.ToString(_parts[i], _parts[i + 1] - _parts[i]));
                }

                // last iteration - we count part length otherwise
                if (_partTypes[currentPartNumber] == PartType.NormalBackslash
                  || _partTypes[currentPartNumber] == PartType.NormalBackslashNoRangeNext)
                    output.Append(@"\\");
                else
                    output.Append(_buffer.ToString(_parts[currentPartNumber], _buffer.Length - _parts[currentPartNumber]));
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="output"></param>
            private void WriteOutSpecial(ref StringBuilder output)
            {
                int length;

                for (int i = 0; i <= currentPartNumber; i++)
                {
                    if (i == currentPartNumber)
                        length = _buffer.Length - _parts[i];
                    else
                        length = _parts[i + 1] - _parts[i];

                    switch (_partTypes[i])
                    {
                        case PartType.Normal:
                            output.Append('[');
                            output.Append(_buffer.ToString(_parts[i], length));
                            output.Append(']');
                            break;
                        case PartType.Range: // can be only last part
                            Debug.Assert(i != currentPartNumber, "internal error WriteOutSpecial() Range isn't last.");
                            goto case PartType.Normal;
                        case PartType.UnrolledRange:
                            goto case PartType.Normal;
                        case PartType.NormalBackslash:
                            output.Append(@"[\\]");
                            break;
                        case PartType.NormalBackslashNoRangeNext:
                            goto case PartType.NormalBackslash;
                        case PartType.NormalNoRangeNext:
                            goto case PartType.Normal;
                        case PartType.UnrolledCharacterClass:
                            goto case PartType.Normal;
                        case PartType.WordBoundary:
                            Debug.Fail("internal error WriteOutSpecial() Word boundary.");
                            break;
                        case PartType.UnrolledCollatingElement:
                            output.Append(_buffer.ToString(_parts[i], length));
                            break;
                        case PartType.UnrolledEquivalenceClass:
                            goto case PartType.UnrolledCollatingElement;
                    }
                    output.Append('|');
                } // for()

                output.Remove(output.Length - 1, 1); // remove last '|' character
            }

            /// <summary>
            /// Writes to <c>output</c> Framework regular expression for beginning or end of word,
            /// according to first part in <c>parts</c> array.
            /// </summary>
            /// <param name="output"><c>StringBuilder</c> to write out.</param>
            private void WriteOutWordBoundary(ref StringBuilder output)
            {
                if (_buffer[_parts[0]] == '<')
                {   // beginning of word
                    output.Append(@"\b(?=\w)");
                }
                else
                {   // end of word
                    output.Append(@"\b(?<=\w)");
                }
            }

            /// <summary>
            /// Compose all parts and creates .NET Framework compatible regular expression.
            /// </summary>
            /// <returns>.NET Framework compatible regular expression</returns>
            public bool WriteOut(ref StringBuilder retString)
            {
                // some failure occured, nothing is written
                if (!allOK)
                    return false;

                // creates new empty part - causes optimization of previous part (the last one, that was not optimized,
                // previous parts were optimized while calling StartNextPart()).
                StartNextPart();

                StringBuilder output = new StringBuilder(2 * _buffer.Length); // estimated indigent length

                if (_partTypes[0] == PartType.WordBoundary)
                {
                    // writes word boundary and ends, if it is word boundary, nothing can follow
                    WriteOutWordBoundary(ref output);
                }
                else if (!specialTranslationNeeded)
                {
                    // regular expression can be written to single framework bracket expression [..]
                    output.Append('[');

                    if (negation)
                        output.Append('^');
                    WriteOutNoSpecial(ref output);

                    output.Append(']');
                }
                else
                {
                    // regular expression cannot be written to single framework expression,
                    // we enclose whole expression to (? .. ) parentheses and inside can be more
                    // complicated expression that match one single POSIX bracket expression
                    if (negation)
                    {
                        output.Append("(?(");
                        WriteOutSpecial(ref output);
                        output.Append(@")\b\B|.)"); // \b\B always fails
                    }
                    else
                    {
                        output.Append("(?:");
                        WriteOutSpecial(ref output);
                        output.Append(')');
                    }
                }

                retString.Append(output.ToString());
                return allOK;
            }
        }// class BracketExpression

        #endregion

        #region Regular expression conversion functions

        /// <summary>
        /// <c>CharMap</c> containing characters that must be prepended by '\'.
        /// </summary>
        private static readonly CharMap controlCharsMap = new CharMap(new uint[] { 0x0, 0x8f20001, 0x1a, 0x18 });

        /// <summary>
        /// Converts POSIX regular expression to .NET Framework regular expression
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="expr">POSIX 1003.2 regular expression</param>
        /// <returns>.NET Framework compatible regular expression</returns>
        // WORKING: change to private!
        private static string ConvertPosix2DotNetExpr(Context ctx, string expr)
        {
            if (expr == null) return "";

            // number that is not used in automaton
            const int errorState = 99;

            // 0 == initial state
            int state = 0;

            // iterator for iterating in strings
            var ch = ((IEnumerable<char>)expr).GetEnumerator();
            // true if we are at the end of bracket expression
            bool eOfExpr = false;

            // true if we have read next character and this character is not applicable for current state,
            // we change state with lambda step and there is character scanned again
            // true if we are changing state and the character should be scanned again
            bool lambdaStep = false;

            // into this StringBuilder is converted regular expression written
            // 2*expr.Length is estimated necessary length of output string
            StringBuilder output = new StringBuilder(2 * expr.Length);

            // bracket expressions are very complicated, we have made separate class for managing them
            // if we need to create bracket expression class instance, we store it in this variable
            // and reuse it after Reset() call
            BracketExpression be = null;

            eOfExpr = !ch.MoveNext();
            while (!eOfExpr)
            {
                switch (state)
                {
                    case 0: // initial state
                        switch (ch.Current)
                        {
                            case '\\':
                                state = 1;
                                break;

                            case '[':
                                state = 2;
                                if (be != null) // bracket expr. already exists, reset it only
                                    be.Reset();
                                else
                                    be = new BracketExpression(ctx);
                                break;

                            case '(':
                                state = 15;
                                break;

                            default:
                                output.Append(ch.Current);
                                break;
                        }
                        break;

                    case 1:
                        if (controlCharsMap.Contains(ch.Current))
                        {   // control character - must be preppended by '\'
                            output.Append('\\');
                        }

                        output.Append(ch.Current);
                        state = 0;
                        break;

                    case 2:
                        switch (ch.Current)
                        {
                            case '^':
                                be.Negation = true;
                                state = 3;
                                break;

                            case ']':
                                be.Append(']');
                                state = 4;
                                break;

                            default:
                                lambdaStep = true;
                                state = 4;
                                break;
                        }
                        break;

                    case 3:
                        if (ch.Current == ']')
                            be.Append(']');
                        else
                            lambdaStep = true;

                        state = 4;
                        break;

                    case 4:
                        switch (ch.Current)
                        {
                            case '[':
                                state = 5;
                                break;

                            case '\\':
                                be.StartNextPart();
                                be.Append('\\');
                                be.CurrentPartType = BracketExpression.PartType.NormalBackslash;
                                be.StartNextPart();
                                break;

                            case ']':
                                state = 0;
                                if (!be.WriteOut(ref output))
                                    state = errorState;
                                break;

                            case '-':
                                be.CurrentPartType = BracketExpression.PartType.Range;
                                break;

                            default:
                                be.Append(ch.Current);
                                break;
                        }
                        break;

                    case 5:
                        switch (ch.Current)
                        {
                            case '.':
                                be.StartNextPart();
                                be.CurrentPartType = BracketExpression.PartType.CollatingElement;
                                state = 6;
                                break;

                            case ':':
                                be.StartNextPart();
                                be.CurrentPartType = BracketExpression.PartType.CharacterClass;
                                state = 7;
                                break;

                            case '=':
                                be.StartNextPart();
                                be.CurrentPartType = BracketExpression.PartType.EquivalenceClass;
                                state = 8;
                                break;

                            case '\\':
                                be.Append('\\', '\\');
                                state = 4;
                                break;
                            default:

                                be.Append('[', ch.Current);
                                state = 4;
                                break;
                        }
                        break;

                    case 6:
                        if (ch.Current == '\\')
                            be.Append('\\', '\\');
                        else
                            be.Append(ch.Current);
                        state = 9;
                        break;

                    case 7:
                        if (ch.Current == '\\')
                            be.Append('\\', '\\');
                        else
                            be.Append(ch.Current);
                        state = 10;
                        break;

                    case 8:
                        if (ch.Current == '\\')
                            be.Append('\\', '\\');
                        else
                            be.Append(ch.Current);
                        state = 11;
                        break;

                    case 9:
                        switch (ch.Current)
                        {
                            case '\\':
                                be.Append('\\', '\\');
                                break;

                            case '.':
                                state = 12;
                                break;

                            default:
                                be.Append(ch.Current);
                                break;
                        }
                        break;

                    case 10:
                        switch (ch.Current)
                        {
                            case '\\':
                                be.Append('\\', '\\');
                                break;

                            case ':':
                                state = 13;
                                break;

                            default:
                                be.Append(ch.Current);
                                break;
                        }
                        break;

                    case 11:
                        switch (ch.Current)
                        {
                            case '\\':
                                be.Append('\\', '\\');
                                break;

                            case '=':
                                state = 14;
                                break;

                            default:
                                be.Append(ch.Current);
                                break;
                        }
                        break;

                    case 12:
                        switch (ch.Current)
                        {
                            case ']':
                                be.StartNextPart();
                                state = 4;
                                break;

                            case '\\':
                                be.Append('.', '\\', '\\');
                                state = 9;
                                break;

                            default:
                                be.Append('.', ch.Current);
                                state = 9;
                                break;
                        }
                        break;

                    case 13:
                        switch (ch.Current)
                        {
                            case ']':
                                be.StartNextPart();
                                state = 4;
                                break;

                            case '\\':
                                be.Append(':', '\\', '\\');
                                state = 10;
                                break;

                            default:
                                be.Append(':', ch.Current);
                                state = 10;
                                break;
                        }
                        break;

                    case 14:
                        switch (ch.Current)
                        {
                            case ']':
                                be.StartNextPart();
                                state = 4;
                                break;

                            case '\\':
                                be.Append('=', '\\', '\\');
                                state = 11;
                                break;

                            default:
                                be.Append('=', ch.Current);
                                state = 11;
                                break;
                        }
                        break;

                    case 15:
                        switch (ch.Current)
                        {
                            case '?':
                                PhpException.Throw(PhpError.Warning, LibResources.question_mark_folowing_nothing);
                                state = errorState;
                                break;

                            default:
                                output.Append('(');
                                output.Append(ch.Current);
                                state = 0;
                                break;
                        }
                        break;

                    default: // catch the ErrorState
                        eOfExpr = true;
                        output.Remove(0, output.Length);
                        break;
                }

                // we can eliminate this, but without lambda steps will be an automaton much more complicated
                // other solution is calling ch.MoveNext() in each case statement, this would bring error liability
                // but can save a few instructions in each iteration
                if (!eOfExpr && !lambdaStep)
                    eOfExpr = !ch.MoveNext();

                lambdaStep = false;
            }

            // check where an automaton has finished
            switch (state)
            {
                case 0:
                    return output.ToString();

                case 1:
                    PhpException.Throw(PhpError.Warning, LibResources.regexp_cannot_end_with_two_backslashes);
                    return null;

                case 15:
                    PhpException.Throw(PhpError.Warning, LibResources.regexp_cannot_end_with_open_bracket);
                    return null;

                default:
                    PhpException.Throw(PhpError.Warning, LibResources.unenclosed_bracket_expression);
                    return null;
            }
        }


        /// <summary>
        /// Converts string that represents replacement and can be used with regular expression and
        /// contain references to parenthesized substrings in that regular expression.
        /// </summary>
        /// <param name="replacement">String to convert</param>
        /// <param name="substrNumbers">Array containig numbers of parenthesized substrings in matching regular expression.</param>
        /// <returns>Converted .NET Framework compatible regular expression to replacement.</returns>
        private static string ConvertPosix2DotNetRepl(string replacement, int[] substrNumbers)
        {
            if (replacement == null) return "";

            int state = 0;
            StringBuilder output = new StringBuilder((int)(replacement.Length * 1.5));

            foreach (char ch in replacement)
            {
                switch (state)
                {
                    case 0:
                        switch (ch)
                        {
                            case '\\':
                                state = 1;
                                break;

                            case '$':
                                // 'normal' dollar must be doubled to prevent back reference meaning
                                output.Append("$$");
                                break;

                            default:
                                output.Append(ch);
                                break;
                        }
                        break;

                    case 1:
                        state = 0; // always return to state 0
                        if (ch == '$')
                        {
                            // 'normal' dollar must be doubled to prevent back reference meaning
                            output.Append("\\$$");
                        }
                        else if (ch >= '0' && ch <= '9')
                        {
                            // back reference number 0 .. 9

                            // write back reference only if exists
                            if (((System.Collections.IList)substrNumbers).Contains(Int32.Parse(ch.ToString())))
                                output.Append('$');
                            else
                                output.Append('\\');

                            output.Append(ch);
                        }
                        else
                        {
                            // other characters - leave unchanged
                            output.Append('\\');
                            output.Append(ch);
                        }
                        break;
                }
            }

            if (state == 1)
                output.Append('\\');

            return output.ToString();
        }

        #endregion
    }
}
