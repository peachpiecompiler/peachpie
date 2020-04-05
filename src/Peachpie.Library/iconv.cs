using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Utilities;
using static Pchp.Library.StandardPhpOptions;

namespace Pchp.Library
{
    [PhpExtension("iconv", Registrator = typeof(PhpIconv.Registrator))]
    public static class PhpIconv
    {
        #region IconvConfig, Options

        sealed class IconvConfig : IPhpConfiguration
        {
            public string InputEncoding = "ISO-8859-1";
            public string InternalEncoding = "ISO-8859-1";
            public string OutputEncoding = "ISO-8859-1";

            public IPhpConfiguration Copy() => (IconvConfig)this.MemberwiseClone();
            public string ExtensionName => "iconv";

            /// <summary>
            /// Gets or sets a value of a legacy configuration option.
            /// </summary>
            private static PhpValue GetSet(Context ctx, IPhpConfigurationService config, string option, PhpValue value, IniAction action)
            {
                var local = config.Get<IconvConfig>();
                if (local == null)
                {
                    return PhpValue.Null;
                }

                switch (option)
                {
                    case "iconv.input_encoding":
                        return (PhpValue)StandardPhpOptions.GetSet(ref local.InputEncoding, "ISO-8859-1", value, action);

                    case "iconv.internal_encoding":
                        return (PhpValue)StandardPhpOptions.GetSet(ref local.InternalEncoding, "ISO-8859-1", value, action);

                    case "iconv.output_encoding":
                        return (PhpValue)StandardPhpOptions.GetSet(ref local.OutputEncoding, "ISO-8859-1", value, action);
                }

                Debug.Fail("Option '" + option + "' is not currently supported.");
                return PhpValue.Null;
            }

            /// <summary>
            /// Registers legacy ini-options.
            /// </summary>
            internal static void RegisterLegacyOptions()
            {
                const string s = "iconv";
                GetSetDelegate d = new GetSetDelegate(GetSet);

                Register("iconv.input_encoding", IniFlags.Supported | IniFlags.Local, d, s);
                Register("iconv.internal_encoding", IniFlags.Supported | IniFlags.Local, d, s);
                Register("iconv.output_encoding", IniFlags.Supported | IniFlags.Local, d, s);
            }
        }

        static IconvConfig GetConfig(Context ctx) => ctx.Configuration.Get<IconvConfig>();

        internal class Registrator
        {
            public Registrator()
            {
                Context.RegisterConfiguration(new IconvConfig());
                IconvConfig.RegisterLegacyOptions();
            }
        }

        #endregion

        #region StopEncoderFallback

        internal sealed class EncoderResult
        {
            public int firstFallbackCharIndex = -1;
        }

        internal sealed class StopEncoderFallback : EncoderFallback
        {
            internal EncoderResult result;
            public StopEncoderFallback(EncoderResult result)
            {
                this.result = result;
            }

            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                return new StopEncoderFallbackBuffer(this);
            }

            public override int MaxCharCount
            {
                get { return 0; }
            }


        }

        internal sealed class StopEncoderFallbackBuffer : EncoderFallbackBuffer
        {
            private EncoderResult/*!*/result;

            public StopEncoderFallbackBuffer(StopEncoderFallback fallback)
            {
                this.result = fallback.result ?? new EncoderResult();
            }

            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
            {
                return Fallback(charUnknownHigh, index);
            }

            public override bool Fallback(char charUnknown, int index)
            {
                if (result.firstFallbackCharIndex < 0)
                {
                    // TODO: Stop encoding the remaining characters
                    result.firstFallbackCharIndex = index;
                }

                return true;
            }

            public override char GetNextChar()
            {
                return '\0';
            }

            public override bool MovePrevious()
            {
                return false;
            }

            public override int Remaining
            {
                get { return 0; }
            }
        }

        #endregion

        #region IgnoreEncoderFallback

        internal sealed class IgnoreEncoderFallback : EncoderFallback
        {
            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                return new IgnoreEncoderFallbackBuffer(this);
            }

            public override int MaxCharCount
            {
                get { return 0; }
            }
        }

        internal sealed class IgnoreEncoderFallbackBuffer : EncoderFallbackBuffer
        {
            public IgnoreEncoderFallbackBuffer(IgnoreEncoderFallback fallback)
            {

            }

            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
            {
                return true;
            }

            public override bool Fallback(char charUnknown, int index)
            {
                return true;
            }

            public override char GetNextChar()
            {
                return '\0';
            }

            public override bool MovePrevious()
            {
                return false;
            }

            public override int Remaining
            {
                get { return 0; }
            }
        }

        #endregion

        #region TranslitEncoderFallback

        internal class TranslitEncoderFallback : EncoderFallback
        {
            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                return new TranslitEncoderFallbackBuffer(this);
            }

            public override int MaxCharCount
            {
                get { return TranslitEncoderFallbackBuffer.transliterationsMaxCharCount; }
            }
        }

        internal class TranslitEncoderFallbackBuffer : EncoderFallbackBuffer
        {
            /// <summary>
            /// String that will be returned as the replacement for the fallbacked character.
            /// </summary>
            private string currentReplacement = null;

            /// <summary>
            /// Index in the <see cref="currentReplacement"/>.
            /// </summary>
            private int currentReplacementIndex;

            private bool IsIndexValid(int index)
            {
                return (currentReplacement != null && index >= 0 && index < currentReplacement.Length);
            }

            private static Dictionary<char, string>/*!!*/transliterations;
            internal static int transliterationsMaxCharCount;

            static TranslitEncoderFallbackBuffer()
            {
                transliterations = new Dictionary<char, string>(3900);

                // initialize the transliterations table:

                // load "translit.def" file content:
                using (var translit = new System.IO.StreamReader(typeof(PhpIconv).Assembly.GetManifestResourceStream("Pchp.Library.Resources.translit.def")))
                {
                    string line;
                    while ((line = translit.ReadLine()) != null)
                    {
                        if (line.Length == 0 || line[0] == '#')
                        {
                            continue;
                        }

                        // HEX\tTRANSLIT\t#comment
                        var t1 = line.IndexOf('\t');
                        Debug.Assert(t1 > 0);
                        var t2 = line.IndexOf('\t', t1 + 1);
                        Debug.Assert(t2 > 0);

                        string
                            strChar = line.Substring(0, t1),
                            str = line.Substring(t1 + 1, t2 - t1);

                        int charNumber = int.Parse(strChar, System.Globalization.NumberStyles.HexNumber);

                        if (transliterationsMaxCharCount < str.Length)
                            transliterationsMaxCharCount = str.Length;

                        transliterations[(char)charNumber] = str;
                    }
                }
            }

            public TranslitEncoderFallbackBuffer(TranslitEncoderFallback fallback)
            {

            }

            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
            {
                return false;
            }

            public override bool Fallback(char charUnknown, int index)
            {
                if (transliterations.TryGetValue(charUnknown, out currentReplacement))
                {
                    currentReplacementIndex = -1;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override char GetNextChar()
            {
                ++currentReplacementIndex;

                if (IsIndexValid(currentReplacementIndex))
                    return currentReplacement[currentReplacementIndex];
                else
                    return '\0';
            }

            public override bool MovePrevious()
            {
                if (currentReplacementIndex >= 0 && currentReplacement != null)
                {
                    currentReplacementIndex--;
                    return true;
                }

                return false;
            }

            public override int Remaining
            {
                get { return IsIndexValid(currentReplacementIndex + 1) ? (currentReplacement.Length - currentReplacementIndex - 1) : 0; }
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// An optional string that can be appended to the output encoding name. Causes transliteration of characters that cannot be converted to the output encoding.
        /// </summary>
        const string TranslitEncOption = "//TRANSLIT";

        /// <summary>
        /// An optional string that can be appended to the output encoding name (before <see cref="TranslitEncOption"/> if both are specified). Causes ignoring of characters that cannot be converted to the output encoding.
        /// </summary>
        const string IgnoreEncOption = "//IGNORE";

        /// <summary>
        /// Remove optional encoding options such as <see cref="TranslitEncOption"/> or <see cref="IgnoreEncOption"/>.
        /// </summary>
        /// <param name="encoding">Original output encoding stirng.</param>
        /// <param name="transliterate">Is set to <c>true</c> if <see cref="TranslitEncOption"/> was specified.</param>
        /// <param name="discard_ilseq">Is set to <c>true</c> if <see cref="IgnoreEncOption"/> was specified.</param>
        /// <returns><paramref name="encoding"/> without optional options.</returns>
        static string ParseOutputEncoding(ReadOnlySpan<char>/*!*/encoding, out bool transliterate, out bool discard_ilseq)
        {
            Debug.Assert(encoding != null);

            transliterate = false;
            discard_ilseq = false;

            for (; ; )
            {
                if (encoding.EndsWith(TranslitEncOption.AsSpan(), StringComparison.Ordinal))
                {
                    encoding = encoding.Slice(0, encoding.Length - TranslitEncOption.Length);
                    transliterate = true;
                }
                else if (encoding.EndsWith(IgnoreEncOption.AsSpan(), StringComparison.Ordinal))
                {
                    encoding = encoding.Slice(0, encoding.Length - IgnoreEncOption.Length);
                    discard_ilseq = true;
                }
                else
                {
                    break;
                }
            }

            //
            return encoding.ToString();
        }

        /// <summary>
        /// Try to find <see cref="Encoding"/> by its PHP name.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="encoding">Encoding name.</param>
        /// <returns><see cref="Encoding"/> instance or <c>null</c> if nothing was found.</returns>
        static Encoding ResolveEncoding(Context ctx, string encoding)
        {
            return MultiByteString.GetEncoding(encoding ?? GetConfig(ctx).InternalEncoding);
        }

        #endregion

        #region PHP constants

        /// <summary>
        /// The implementation name
        /// </summary>
        public const string ICONV_IMPL = ".NET Iconv";

        /// <summary>
        /// The implementation version
        /// </summary>
        public const string ICONV_VERSION = "1.0.0";

        public const int ICONV_MIME_DECODE_STRICT = 1;

        public const int ICONV_MIME_DECODE_CONTINUE_ON_ERROR = 2;

        #endregion

        /// <summary>
        /// Retrieve internal configuration variables of iconv extension.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="type">
        /// The value of the optional type can be:
        /// - all
        /// - input_encoding
        /// - output_encoding
        /// - internal_encoding
        /// </param>
        /// <returns>Returns the current value of the internal configuration variable if successful or <c>false</c> on failure.
        /// If <paramref name="type"/> is omitted or set to <c>all</c>, iconv_get_encoding() returns an array that stores all these variables.</returns>
        public static PhpValue iconv_get_encoding(Context ctx, string type = "all")
        {
            if (type.EqualsOrdinalIgnoreCase("all"))
                return (PhpValue)GetIconvEncodingAll(ctx);

            // 
            var local = GetConfig(ctx);

            if (type.EqualsOrdinalIgnoreCase("input_encoding"))
                return (PhpValue)local.InputEncoding;

            if (type.EqualsOrdinalIgnoreCase("output_encoding"))
                return (PhpValue)local.OutputEncoding;

            if (type.EqualsOrdinalIgnoreCase("internal_encoding"))
                return (PhpValue)local.InternalEncoding;

            return PhpValue.False;
        }

        static PhpArray/*!*/GetIconvEncodingAll(Context ctx)
        {
            var local = GetConfig(ctx);

            var ret = new PhpArray(3);
            ret.Add("input_encoding", local.InputEncoding);
            ret.Add("output_encoding", local.OutputEncoding);
            ret.Add("internal_encoding", local.InternalEncoding);
            return ret;
        }

        /// <summary>
        /// Set current setting for character encoding conversion.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="type">The value of type can be any one of these:
        /// - input_encoding
        /// - output_encoding
        /// - internal_encoding
        /// </param>
        /// <param name="charset">The character set.</param>
        /// <returns>Returns <c>TRUE</c> on success or <c>FALSE</c> on failure.</returns>
        public static bool iconv_set_encoding(Context ctx, string type, string charset)
        {
            var encoding = ResolveEncoding(ctx, charset);
            if (encoding == null)
            {
                PhpException.InvalidArgument(nameof(charset));    // TODO: PHP error message
                return false;
            }

            // 
            var local = GetConfig(ctx);

            if (type.EqualsOrdinalIgnoreCase("input_encoding"))
            {
                local.InputEncoding = charset;
            }
            else if (type.EqualsOrdinalIgnoreCase("output_encoding"))
            {
                local.OutputEncoding = charset;
            }
            else if (type.EqualsOrdinalIgnoreCase("internal_encoding"))
            {
                local.InternalEncoding = charset;
            }
            else
            {
                PhpException.InvalidArgument("type");
                return false;
            }

            return true;
        }

        //iconv_mime_decode_headers — Decodes multiple MIME header fields at once
        //iconv_mime_decode — Decodes a MIME header field
        //iconv_mime_encode — Composes a MIME header field

        /// <summary>
        /// Returns the character count of string.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="str">The string.</param>
        /// <param name="charset">If charset parameter is omitted, <paramref name="str"/> is assumed to be encoded in iconv.internal_encoding.</param>
        /// <returns>Returns the character count of str, as an integer.</returns>
        public static int iconv_strlen(Context ctx, PhpString str, string charset = null/*=iconv.internal_encoding*/)
        {
            if (str.IsEmpty)
            {
                return 0;
            }

            if (str.ContainsBinaryData)
            {
                var encoding = ResolveEncoding(ctx, charset);
                if (encoding == null) throw new NotSupportedException("charset not supported"); // TODO: PHP friendly warning

                return encoding.GetCharCount(str.ToBytes(ctx));
            }
            else
            {
                return str.Length;
            }
        }

        /// <summary>
        /// Finds position of first occurrence of a needle within a haystack.
        /// In contrast to strpos(), the return value of iconv_strpos() is the number of characters that appear before the needle, rather than the offset in bytes to the position where the needle has been found. The characters are counted on the basis of the specified character set charset.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="haystack">The entire string.</param>
        /// <param name="needle">The searched substring.</param>
        /// <param name="offset">The optional offset parameter specifies the position from which the search should be performed.</param>
        /// <param name="charset">If charset parameter is omitted, string are assumed to be encoded in iconv.internal_encoding.</param>
        /// <returns>Returns the numeric position of the first occurrence of needle in haystack. If needle is not found, iconv_strpos() will return FALSE.</returns>
        [return: CastToFalse]
        public static int iconv_strpos(Context ctx, PhpString haystack, PhpString needle, int offset = 0, string charset = null/*= ini_get("iconv.internal_encoding")*/)
        {
            if (haystack.IsEmpty || needle.IsEmpty)
                return -1;

            var encoding = ResolveEncoding(ctx, charset);
            string haystackstr = haystack.ToString(encoding);
            string needlestr = needle.ToString(encoding);

            return haystackstr.IndexOf(needlestr, offset, StringComparison.Ordinal);
        }

        /// <summary>
        /// Finds the last occurrence of a needle within a haystack.
        /// In contrast to strrpos(), the return value of iconv_strrpos() is the number of characters that appear before the needle, rather than the offset in bytes to the position where the needle has been found. The characters are counted on the basis of the specified character set charset.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="haystack">The entire string.</param>
        /// <param name="needle">The searched substring.</param>
        /// <param name="charset">If charset parameter is omitted, string are assumed to be encoded in iconv.internal_encoding.</param>
        /// <returns>Returns the numeric position of the last occurrence of needle in haystack. If needle is not found, iconv_strpos() will return FALSE.</returns>
        [return: CastToFalse]
        public static int iconv_strrpos(Context ctx, PhpString haystack, PhpString needle, string charset = null /*= ini_get("iconv.internal_encoding")*/)
        {
            if (haystack.IsEmpty || needle.IsEmpty)
                return -1;

            var encoding = ResolveEncoding(ctx, charset);
            string haystackstr = haystack.ToString(encoding);
            string needlestr = needle.ToString(encoding);

            return Strings.strrpos(haystackstr, (PhpValue)needlestr);
        }

        /// <summary>
        /// Cuts a portion of <paramref name="str"/> specified by the <paramref name="offset"/> and <paramref name="length"/> parameters.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="str">The original string.</param>
        /// <param name="offset">If offset is non-negative, iconv_substr() cuts the portion out of str beginning at offset'th character, counting from zero.
        /// If offset is negative, iconv_substr() cuts out the portion beginning at the position, offset characters away from the end of str.</param>
        /// <param name="length">If length is given and is positive, the return value will contain at most length characters of the portion that begins at offset (depending on the length of string).
        /// If negative length is passed, iconv_substr() cuts the portion out of str from the offset'th character up to the character that is length characters away from the end of the string. In case offset is also negative, the start position is calculated beforehand according to the rule explained above.</param>
        /// <param name="charset">If charset parameter is omitted, string are assumed to be encoded in iconv.internal_encoding.
        /// Note that offset and length parameters are always deemed to represent offsets that are calculated on the basis of the character set determined by charset, whilst the counterpart substr() always takes these for byte offsets.</param>
        /// <returns>Returns the portion of str specified by the offset and length parameters.
        /// If str is shorter than offset characters long, FALSE will be returned.</returns>
        [return: CastToFalse]
        public static string iconv_substr(Context ctx, PhpString str, int offset, int length = int.MaxValue /*= iconv_strlen($str, $charset)*/ , string charset = null /*= ini_get("iconv.internal_encoding")*/)
        {
            if (str.IsEmpty)
                return null;

            if (str.ContainsBinaryData)
            {
                // encoding matters

                var encoding = ResolveEncoding(ctx, charset);
                if (encoding == null) throw new NotSupportedException("charset not supported"); // TODO: PHP friendly warning

                return Strings.substr(str.ToString(encoding), offset, length).ToString(ctx);
            }

            return Strings.substr(str.ToString(ctx), offset, length).ToString(ctx);
        }

        /// <summary>
        /// Performs a character set conversion on the string str from in_charset to out_charset.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="in_charset">The input charset.</param>
        /// <param name="out_charset">The output charset.
        /// 
        /// If you append the string //TRANSLIT to out_charset transliteration is activated.
        /// This means that when a character can't be represented in the target charset,
        /// it can be approximated through one or several similarly looking characters.
        /// 
        /// If you append the string //IGNORE, characters that cannot be represented in the target
        /// charset are silently discarded. Otherwise, <paramref name="str"/> is cut from the first
        /// illegal character and an E_NOTICE is generated.</param>
        /// <param name="str"></param>
        /// <returns></returns>
        [return: CastToFalse]
        public static PhpString iconv(Context ctx, string in_charset, string out_charset, PhpString str)
        {
            // check args
            if (str.IsDefault)
            {
                PhpException.ArgumentNull(nameof(str));
                return default; // FALSE
            }
            if (out_charset == null)
            {
                PhpException.ArgumentNull(nameof(out_charset));
                return default; // FALSE
            }

            // resolve out_charset
            out_charset = ParseOutputEncoding(out_charset.AsSpan(), out var transliterate, out var discard_ilseq);
            var out_encoding = ResolveEncoding(ctx, out_charset);
            if (out_encoding == null)
            {
                PhpException.Throw(PhpError.Notice, Resources.LibResources.wrong_charset, out_charset, in_charset, out_charset);
                return default; // FALSE
            }

            // encoding fallback
            EncoderFallback enc_fallback;
            var out_result = new EncoderResult();

            if (transliterate)
                enc_fallback = new TranslitEncoderFallback();   // transliterate unknown characters
            else if (discard_ilseq)
                enc_fallback = new IgnoreEncoderFallback();    // ignore character and continue
            else
                enc_fallback = new StopEncoderFallback(out_result);    // throw notice and discard all remaining characters

            //// out_encoding.Clone() ensures it is NOT readOnly
            //// then set EncoderFallback to catch handle unconvertable characters

            //out_encoding = (Encoding)out_encoding.Clone();
            out_encoding = Encoding.GetEncoding(out_encoding.CodePage, enc_fallback, DecoderFallback.ExceptionFallback);

            try
            {
                //
                if (str.ContainsBinaryData)
                {
                    // resolve in_charset
                    if (in_charset == null)
                    {
                        PhpException.ArgumentNull("in_charset");
                        return default(PhpString);
                    }
                    var in_encoding = ResolveEncoding(ctx, in_charset);
                    if (in_encoding == null)
                    {
                        PhpException.Throw(PhpError.Notice, Resources.LibResources.wrong_charset, in_charset, in_charset, out_charset);
                        return default(PhpString);
                    }

                    // TODO: in_encoding.Clone() ensures it is NOT readOnly, then set DecoderFallback to catch invalid byte sequences

                    // convert <in_charset> to <out_charset>
                    return new PhpString(out_encoding.GetBytes(str.ToString(in_encoding)));
                }
                else
                {
                    // convert UTF16 to <out_charset>
                    return new PhpString(str.ToBytes(out_encoding));
                }
            }
            finally
            {
                if (out_result.firstFallbackCharIndex >= 0)
                {
                    // Notice: iconv(): Detected an illegal character in input string
                    PhpException.Throw(Core.PhpError.Notice, Resources.LibResources.illegal_character);
                }
            }
        }

        ///// <summary>
        ///// Convert character encoding as output buffer handler.
        ///// </summary>
        ///// <param name="contents"></param>
        ///// <param name="status">Bitmask of PHP_OUTPUT_HANDLER_* constants.</param>
        ///// <returns></returns>
        //public static PhpString ob_iconv_handler(PhpString contents , int status )
        //{

        //}

        /// <summary>
        /// rfc2047, allows for the encoded-text to be multilined with leading white-spaces.
        /// </summary>
        readonly static Lazy<Regex> s_mime_header_regex = new Lazy<Regex>(() => new Regex(@"=\?[\w-]+\?[QB]\?([^\s\?]+([\r\n]+[ \t]*)?)+\?=", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline));

        readonly static char[] s_whitespaces = new[] { ' ', '\t', '\n', '\r' };

        /// <summary>
        /// Strip a headers and decode values.
        /// </summary>
        static IEnumerable<string> decode_headers(string encoded)
        {
            int headerstart = 0;// current header start
            Match match = null; // next match

            for (int i = 0; i < encoded.Length; i++)
            {
                var nl = StringUtils.IsNewLine(encoded, i);
                if (nl != 0 && i + nl < encoded.Length && char.IsWhiteSpace(encoded[i + nl]))
                {
                    // line continues with an indented text
                    // trim leading whitespaces and replace with a single space
                    int end = i + nl;
                    do { end++; } while (end < encoded.Length && char.IsWhiteSpace(encoded, end));

                    encoded = encoded.Remove(i) + " " + encoded.Substring(end); // TODO: we can do it later in `yield`, with a smaller portion of text

                    //
                    i--;
                    continue;
                }

                if (nl != 0 || i == encoded.Length - 1)
                {
                    // end of line,
                    // decode values within
                    for (int startindex = headerstart; ;)
                    {
                        match ??= s_mime_header_regex.Value.Match(encoded, startindex);

                        if (!match.Success || match.Index > i)
                            break; // this match will be used in the next header

                        // the value can be split into more lines,
                        // make it singleline
                        var value = match.Value.RemoveAny(s_whitespaces);

                        // decode the value
                        var decoded_value = System.Net.Mail.Attachment.CreateAttachmentFromString("", value).Name;

                        // trims whitespaces between matches (startindex..match.Index)
                        var replaceFrom = encoded.AsSpan(startindex, match.Index - startindex).IsWhiteSpace()
                            ? startindex
                            : match.Index;

                        // replace with decoded value
                        encoded = encoded.Remove(replaceFrom) + decoded_value + encoded.Substring(match.Index + match.Length);

                        //
                        startindex = replaceFrom + decoded_value.Length;
                        i += decoded_value.Length - match.Length + replaceFrom - match.Index;

                        //
                        match = null;
                    }

                    // trim whitespaces from end of the value:
                    int end = i;
                    while (end > headerstart && char.IsWhiteSpace(encoded[end - 1])) end--;

                    // submit header line:
                    yield return encoded.Substring(headerstart, end - headerstart);

                    //
                    i += nl;
                    headerstart = i;
                }
            }
        }

        /// <summary>
        /// Decodes a MIME header field.
        /// </summary>
        public static string iconv_mime_decode(string encoded_header, int mode = 0, string charset = null /*= ini_get("iconv.internal_encoding")*/ )
        {
            if (encoded_header == null)
            {
                return string.Empty;
            }

            return decode_headers(encoded_header).FirstOrDefault();
        }

        /// <summary>
        /// Decodes multiple MIME header fields at once.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray iconv_mime_decode_headers(string encoded_headers, int mode = 0, string charset = null)
        {
            if (encoded_headers == null)
            {
                return null;
            }

            var result = new PhpArray();

            foreach (var header in decode_headers(encoded_headers))
            {
                var col = header.IndexOf(':');
                if (col >= 0)
                {
                    var header_name = Core.Convert.StringToArrayKey(header.Remove(col));

                    // trim leading value whitespaces
                    do { col++; } while (col < header.Length && char.IsWhiteSpace(header, col));

                    var header_value = header.Substring(col);

                    if (result.TryGetValue(header_name, out var existing))
                    {
                        if (existing.IsPhpArray(out var subarray))
                        {
                            subarray.Add(header_value);
                        }
                        else
                        {
                            result[header_name] = new PhpArray(2)
                            {
                                existing,
                                header_value,
                            };
                        }
                    }
                    else
                    {
                        result[header_name] = header_value;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Composes a MIME header field.
        /// </summary>
        public static string iconv_mime_encode(string field_name, string field_value, PhpArray preferences = null)
        {
            // process parameters:

            const char Base64Scheme = 'B';
            const char QuotedPrintableScheme = 'Q';

            char scheme = Base64Scheme;  // B, Q
            //string input_charset = null;
            //string output_charset = null;
            int line_length = 76;
            string line_break_chars = "\r\n";
            var encoding = Encoding.UTF8;

            if (preferences != null)
            {
                if (preferences.TryGetValue("line-break-chars", out var val) && val.IsString(out var str))
                {
                    line_break_chars = str;
                }

                if (preferences.TryGetValue("scheme", out val))
                {
                    // PHP ignores any invalid value
                    if (val.IsString(out str) && str.Length >= 1)
                    {
                        scheme = char.ToUpperInvariant(str[0]);
                    }
                }

                if (preferences.TryGetValue("line-length", out val) && val.IsLong(out var l))
                {
                    line_length = (int)l;
                }
            }

            //
            // internal System.Net.Mime.MimeBasePart.EncodeHeaderValue(field_value, Encoding.UTF8, scheme == 'B', line_length);
            //

            string header = $"=?{encoding.HeaderName.ToUpperInvariant()}?{scheme}?";
            string footer = "?=";

            //
            var result = StringBuilderUtilities.Pool.Get();

            result.Append(field_name);
            result.Append(':');
            result.Append(' ');

            result.Append(header);

            if (line_length < result.Length)
            {
                // adjust max line length,
                // cannot be smaller then preamble
                line_length = result.Length;
            }

            var bytes = encoding.GetBytes(field_value);
            //string encoded;

            Func<byte[], int, int, int> count_func; // how many bytes will be consumed
            //Func<ReadOnlySpan<byte>, string> encode_func;
            Func<byte[], int, int, string> encode_func; // encodes bytes into string

            if (scheme == QuotedPrintableScheme)
            {
                count_func = (_bytes, _from, _maxchars) => QuotedPrintableCount(_bytes, _from, _maxchars);
                encode_func = (_bytes, _from, _count) => QuotedPrintableEncode(_bytes, _from, _count);
            }
            else
            {
                // 3 bytes are encoded as 4 chars
                count_func = (_bytes, _from, _maxchars) => _maxchars * 3 / 4;
                encode_func = (_bytes, _from, _count) => System.Convert.ToBase64String(_bytes, _from, _count);
            }

            int bytes_from = 0;
            int line_remaining = line_length - result.Length;

            for (; bytes_from < bytes.Length;)
            {
                var remaining = line_remaining - footer.Length - line_break_chars.Length; // how many chars we can output
                var bytes_count = Math.Min(bytes.Length - bytes_from, count_func(bytes, bytes_from, remaining));
                var encoded = encode_func(bytes, bytes_from, bytes_count);

                result.Append(encoded);

                bytes_from += bytes_count;

                if (bytes_from < bytes.Length)
                {
                    // NEW LINE
                    result.Append(footer);
                    result.Append(line_break_chars);
                    result.Append(' ');
                    result.Append(header);

                    line_remaining = line_length - 1 - header.Length;
                }
            }

            result.Append(footer);

            //
            return StringBuilderUtilities.GetStringAndReturn(result);
        }

        /// <summary>
        /// Counts bytes to be encoded in order to get <paramref name="maxchars"/> characters.
        /// </summary>
        static int QuotedPrintableCount(byte[] bytes, int from, int maxchars)
        {
            int chars = 0;

            for (int i = from; i < bytes.Length; i++)
            {
                if (chars == maxchars)
                {
                    return i - from;
                }
                else if (chars > maxchars)
                {
                    return i - from - 1;
                }

                var b = bytes[i];

                if (b > 0x20 && b < 0x80)
                {
                    chars++;
                }
                else
                {
                    // =XX
                    chars += 3;
                }
            }

            //
            return bytes.Length - from;
        }

        /// <summary>
        /// Encode bytes as quoted-printable string.
        /// </summary>
        static string QuotedPrintableEncode(byte[] bytes, int from, int count)
        {
            var result = StringBuilderUtilities.Pool.Get();

            for (int i = from; i < bytes.Length && count > 0; i++, count--)
            {
                var b = bytes[i];

                if (b > 0x20 && b < 0x80)
                {
                    //
                    result.Append((char)b);
                }
                else
                {
                    // =XX
                    const string digits = "0123456789ABCDEF";
                    result.Append('=');
                    result.Append(digits[(b >> 4) & 0x0f]);
                    result.Append(digits[(b) & 0x0f]);
                }
            }

            //
            return StringBuilderUtilities.GetStringAndReturn(result);
        }
    }
}
