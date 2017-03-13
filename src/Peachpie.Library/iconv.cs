using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            private static PhpValue GetSet(IPhpConfigurationService config, string option, PhpValue value, IniAction action)
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
                using (var translit = new System.IO.StringReader(Resources.LibResources.translit))
                {
                    string line;
                    while ((line = translit.ReadLine()) != null)
                    {
                        // remove comments:
                        int cut_from = line.IndexOf('#');
                        if (cut_from >= 0) line = line.Remove(cut_from);

                        // skip empty lines:
                        if (line.Length == 0) continue;

                        //
                        string[] parts = line.Split('\t');  // HEX\tTRANSLIT\t
                        Debug.Assert(parts != null && parts.Length == 3);

                        int charNumber = int.Parse(parts[0], System.Globalization.NumberStyles.HexNumber);
                        string str = parts[1];

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
        static string ParseOutputEncoding(string/*!*/encoding, out bool transliterate, out bool discard_ilseq)
        {
            Debug.Assert(encoding != null);

            if (encoding.EndsWith(TranslitEncOption, StringComparison.Ordinal))
            {
                encoding = encoding.Substring(0, encoding.Length - TranslitEncOption.Length);
                transliterate = true;
            }
            else
                transliterate = false;

            if (encoding.EndsWith(IgnoreEncOption, StringComparison.Ordinal))
            {
                encoding = encoding.Substring(0, encoding.Length - IgnoreEncOption.Length);
                discard_ilseq = true;
            }
            else
                discard_ilseq = false;

            //
            return encoding;
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
            if (str == null || str.IsEmpty)
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
            if (haystack == null || needle == null)
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
            if (haystack == null || needle == null)
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
            if (str == null)
                return null;

            if (str.ContainsBinaryData)
            {
                // encoding matters

                var encoding = ResolveEncoding(ctx, charset);
                if (encoding == null) throw new NotSupportedException("charset not supported"); // TODO: PHP friendly warning

                return Strings.substr(str.ToString(encoding), offset, length);
            }

            return Strings.substr(str.ToString(ctx), offset, length);
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
            if (str == null)
            {
                PhpException.ArgumentNull("str");
                return null;
            }
            if (out_charset == null)
            {
                PhpException.ArgumentNull("out_charset");
                return null;
            }

            // resolve out_charset
            bool transliterate, discard_ilseq;
            out_charset = ParseOutputEncoding(out_charset, out transliterate, out discard_ilseq);
            var out_encoding = ResolveEncoding(ctx, out_charset);
            if (out_encoding == null)
            {
                PhpException.Throw(PhpError.Notice, Resources.LibResources.wrong_charset, out_charset, in_charset, out_charset);
                return null;
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

            out_encoding = Encoding.GetEncoding(out_encoding.EncodingName, enc_fallback, DecoderFallback.ExceptionFallback);

            try
            {
                //
                if (str.ContainsBinaryData)
                {
                    // resolve in_charset
                    if (in_charset == null)
                    {
                        PhpException.ArgumentNull("in_charset");
                        return null;
                    }
                    var in_encoding = ResolveEncoding(ctx, in_charset);
                    if (in_encoding == null)
                    {
                        PhpException.Throw(PhpError.Notice, Resources.LibResources.wrong_charset, in_charset, in_charset, out_charset);
                        return null;
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

        //ob_iconv_handler — Convert character encoding as output buffer handler
    }
}
