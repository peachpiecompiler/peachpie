using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Pchp.Core;
using Pchp.Core.Reflection;
using static Pchp.Library.StandardPhpOptions;

namespace Pchp.Library
{
    [PhpExtension(ExtensionName, Registrator = typeof(Registrator))]
    public static class MultiByteString
    {
        const string ExtensionName = "mbstring";

        #region MbConfig, Options

        sealed class MbConfig : IPhpConfiguration
        {
            public Encoding InternalEncoding { get; set; }

            public Encoding RegexEncoding { get; set; }

            public Encoding HttpOutputEncoding { get; set; }

            public IPhpConfiguration Copy() => (MbConfig)this.MemberwiseClone();

            public string ExtensionName => MultiByteString.ExtensionName;

            public Encoding[] DetectOrder { get; set; } = new[] { Encoding.ASCII, Encoding.UTF8 };

            /// <summary>
            /// Gets or sets a value of a legacy configuration option.
            /// </summary>
            private static PhpValue GetSet(Context ctx, IPhpConfigurationService config, string option, PhpValue value, IniAction action)
            {
                var local = config.Get<MbConfig>();
                if (local == null)
                {
                    return PhpValue.Null;
                }

                switch (option)
                {
                    //mbstring.language   "neutral"   PHP_INI_ALL Available since PHP 4.3.0.PHP_INI_PERDIR in PHP <= 5.2.6
                    //mbstring.detect_order NULL    PHP_INI_ALL Available since PHP 4.0.6.
                    //mbstring.http_input "pass"  PHP_INI_ALL Available since PHP 4.0.6.Deprecated in PHP 5.6.0.
                    //mbstring.http_output    "pass"  PHP_INI_ALL Available since PHP 4.0.6.Deprecated in PHP 5.6.0.
                    //mbstring.script_encoding    NULL PHP_INI_ALL Available since PHP 4.3.0.Removed in PHP 5.4.0.Use zend.script_encoding instead.
                    //mbstring.substitute_character NULL    PHP_INI_ALL Available since PHP 4.0.6.

                    case "mbstring.func_overload":
                        // "0" PHP_INI_SYSTEM PHP_INI_PERDIR from PHP 4.3 to 5.2.6, otherwise PHP_INI_SYSTEM. Available since PHP 4.2.0.Deprecated in PHP 7.2.0.
                        return (PhpValue)0;

                    //mbstring.encoding_translation   "0" PHP_INI_PERDIR Available since PHP 4.3.0.
                    //mbstring.strict_detection   "0" PHP_INI_ALL Available since PHP 5.1.2.
                    //mbstring.internal_encoding  NULL PHP_INI_ALL Available since PHP 4.0.6.Deprecated in PHP 5.6.0.

                    default:
                        break;
                }

                Debug.Fail("Option '" + option + "' is not currently supported.");
                return PhpValue.Null;
            }

            /// <summary>
            /// Registers legacy ini-options.
            /// </summary>
            internal static void RegisterLegacyOptions()
            {
                var d = new GetSetDelegate(GetSet);

                //Register("mbstring.internal_encoding", IniFlags.Supported | IniFlags.Local, d, s);
                Register("mbstring.func_overload", IniFlags.Supported | IniFlags.Local, d, MultiByteString.ExtensionName);
            }
        }

        static MbConfig GetConfig(Context ctx) => ctx.Configuration.Get<MbConfig>();

        internal class Registrator
        {
            public Registrator()
            {
                Context.RegisterConfiguration(new MbConfig());
                MbConfig.RegisterLegacyOptions();
                Encoding.RegisterProvider(new PhpEncodingProvider());
            }
        }

        #endregion

        #region Constants

        [Flags, PhpHidden]
        public enum OverloadConstants
        {
            MB_OVERLOAD_MAIL = 1,

            MB_OVERLOAD_STRING = 2,

            MB_OVERLOAD_REGEX = 4,
        }

        public const int MB_OVERLOAD_MAIL = (int)OverloadConstants.MB_OVERLOAD_MAIL;
        public const int MB_OVERLOAD_STRING = (int)OverloadConstants.MB_OVERLOAD_STRING;
        public const int MB_OVERLOAD_REGEX = (int)OverloadConstants.MB_OVERLOAD_REGEX;

        [Flags, PhpHidden]
        public enum CaseConstants
        {
            MB_CASE_UPPER = 0,

            MB_CASE_LOWER = 1,

            MB_CASE_TITLE = 2,

            MB_CASE_FOLD = 3,

            MB_CASE_UPPER_SIMPLE = 4,

            MB_CASE_LOWER_SIMPLE = 5,

            MB_CASE_TITLE_SIMPLE = 6,

            MB_CASE_FOLD_SIMPLE = 7,
        }

        public const int MB_CASE_UPPER = (int)CaseConstants.MB_CASE_UPPER;
        public const int MB_CASE_LOWER = (int)CaseConstants.MB_CASE_LOWER;
        public const int MB_CASE_TITLE = (int)CaseConstants.MB_CASE_TITLE;
        public const int MB_CASE_FOLD = (int)CaseConstants.MB_CASE_FOLD;
        public const int MB_CASE_UPPER_SIMPLE = (int)CaseConstants.MB_CASE_UPPER_SIMPLE;
        public const int MB_CASE_LOWER_SIMPLE = (int)CaseConstants.MB_CASE_LOWER_SIMPLE;
        public const int MB_CASE_TITLE_SIMPLE = (int)CaseConstants.MB_CASE_TITLE_SIMPLE;
        public const int MB_CASE_FOLD_SIMPLE = (int)CaseConstants.MB_CASE_FOLD_SIMPLE;

        #endregion

        #region Encodings

        /// <summary>
        /// Encoding name.
        /// Provides case insensitive comparison.
        /// </summary>
        struct EncodingName : IEquatable<EncodingName>, IEquatable<string>
        {
            public string Name { get; set; }

            static readonly HashSet<string> s_unicodenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "utf8", "utf-8", "utf16", "utf-16", "utf-16le", "utf-16be", "unicode", "utf",
            };

            public EncodingName(string name)
            {
                Name = name;
            }

            public static implicit operator EncodingName(string name) => new EncodingName(name);

            public bool IsCodePageEncoding(out int codepage) => IsCodePageEncoding(Name, out codepage);

            public static bool IsCodePageEncoding(string name, out int codepage)
            {
                if (name != null)
                {
                    // cp{CodePage}
                    if (name.StartsWith("cp", StringComparison.OrdinalIgnoreCase) &&
                        name.Length > 2 &&
                        int.TryParse(name.Substring(2), out codepage)) // TODO: netstandard2.1 ReadOnlySpan
                    {
                        return true;
                    }

                    // Codepage - {CodePage}
                    const string CodepagePrefix = "Codepage - ";
                    if (name.StartsWith(CodepagePrefix, StringComparison.OrdinalIgnoreCase) &&
                        name.Length > CodepagePrefix.Length &&
                        int.TryParse(name.Substring(CodepagePrefix.Length), out codepage))  // TODO: netstandard2.1 ReadOnlySpan
                    {
                        return true;
                    }
                }

                //
                codepage = 0;
                return false;
            }

            public bool Is8bit() => Equals("8bit");

            public bool IsUtfEncoding() => IsUtfEncoding(Name);

            public static bool IsUtfEncoding(string name) => name != null && s_unicodenames.Contains(name);

            static bool Equals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

            public bool Equals(EncodingName other) => Equals(Name, other.Name);

            public bool Equals(string other) => Equals(Name, other);

            public override bool Equals(object obj)
                => obj is EncodingName encname ? Equals(encname)
                : obj is string str ? Equals(new EncodingName { Name = str })
                : false;

            public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? "");

        }

        sealed class PhpEncodingProvider : EncodingProvider
        {
            static readonly Dictionary<EncodingName, Func<Encoding>> s_encmap = new Dictionary<EncodingName, Func<Encoding>>()
            {
                //enc["pass"] = Encoding.Default; // TODO: "pass" encoding
                { "auto", () => Encoding.UTF8 },
                { "wchar", () => Encoding.Unicode },
                //byte2be
                //byte2le
                //byte4be
                //byte4le
                //BASE64
                //UUENCODE
                //HTML-ENTITIES
                //Quoted-Printable
                //7bit
                //8bit // EightBitEncoding.Instance
                //UCS-4
                //UCS-4BE
                //UCS-4LE
                //UCS-2
                //UCS-2BE
                //UCS-2LE
                //UTF-32
                //UTF-32BE
                //UTF-32LE
                //UTF-16
                //UTF-16BE
                { "UTF-16LE", () => Encoding.Unicode }, // alias UTF-16
                { "UTF-8", () => Encoding.UTF8 },       // alias UTF8
                //UTF-7
                //UTF7-IMAP
                { "ASCII", () => Encoding.ASCII },      // alias us-ascii
                //EUC-JP
                //SJIS
                //eucJP-win
                //SJIS-win
                //CP51932
                //JIS
                //ISO-2022-JP
                //ISO-2022-JP-MS
                //Windows-1252
                //Windows-1254
                //ISO-8859-1
                //ISO-8859-2
                //ISO-8859-3
                //ISO-8859-4
                //ISO-8859-5
                //ISO-8859-6
                //ISO-8859-7
                //ISO-8859-8
                //ISO-8859-9
                //ISO-8859-10
                //ISO-8859-13
                //ISO-8859-14
                //ISO-8859-15
                //ISO-8859-16
                //EUC-CN
                //CP936
                //HZ
                //EUC-TW
                //BIG-5
                //EUC-KR
                //UHC
                //ISO-2022-KR
                //Windows-1251
                //CP866
                //KOI8-R
                //KOI8-U
                //ArmSCII-8
                //CP850
            };


            public override Encoding GetEncoding(int codepage)
            {
                return null;
            }

            public static bool IsUtfEncoding(Encoding enc)
            {
                return enc is UnicodeEncoding || enc == Encoding.UTF8 || enc == Encoding.UTF32;
            }

            public override Encoding GetEncoding(string name)
            {
                var encname = new EncodingName(name);

                // encoding names used in PHP
                if (s_encmap.TryGetValue(encname, out var getter))
                {
                    return getter?.Invoke();
                }

                // cp{CodePage}
                // Codepage - {CodePage}
                if (encname.IsCodePageEncoding(out var codepage))
                {
                    return Encoding.GetEncoding(codepage);
                }

                //
                return null;
            }

            ///// <summary>
            ///// 8-bit PHP encoding.
            ///// Converts bytes to string by simply extending each byte to two-byte char.
            ///// .NET string is converted to bytes using UTF-8.
            ///// </summary>
            //sealed class EightBitEncoding : Encoding
            //{
            //    public static readonly Encoding Instance = new EightBitEncoding();

            //    private EightBitEncoding() { }

            //    public override int GetCharCount(byte[] bytes, int index, int count) => count;

            //    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            //    {
            //        for (int i = 0; i < byteCount; i++)
            //        {
            //            chars[charIndex + i] = (char)bytes[byteIndex + i];
            //        }

            //        return byteCount;
            //    }

            //    public override int GetMaxCharCount(int byteCount) => byteCount;

            //    public override int GetByteCount(char[] chars, int index, int count) => UTF8.GetByteCount(chars, index, count);

            //    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
            //        => UTF8.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

            //    public override int GetMaxByteCount(int charCount) => UTF8.GetMaxByteCount(charCount);
            //}
        }

        /// <summary>
        /// Get encoding based on the PHP name. Can return <c>null</c> if such encoding is not defined.
        /// </summary>
        /// <param name="encodingName"></param>
        /// <returns></returns>
        internal static Encoding GetEncoding(string encodingName)
        {
            Encoding encoding = null;
            if (encodingName != null)
            {
                try
                {
                    encoding = Encoding.GetEncoding(encodingName);
                }
                catch (Exception)
                { }
            }
            return encoding;
        }

        static string ToString(Context ctx, PhpValue value, string forceencoding = null)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.String: return value.String;
                case PhpTypeCode.MutableString: return ToString(ctx, value.MutableString, forceencoding);
                case PhpTypeCode.Alias: return ToString(ctx, value.Alias.Value, forceencoding);
                default: return value.ToStringOrThrow(ctx);
            }
        }

        static string ToString(Context ctx, PhpString value, string forceencoding = null)
        {
            return value.ContainsBinaryData
                ? value.ToString(GetEncoding(forceencoding) ?? GetInternalEncoding(ctx))
                : value.ToString(ctx);  // no bytes have to be converted anyway
        }

        static byte[] ToBytes(Context ctx, PhpString value, string forceencoding = null)
        {
            return value.ToBytes(GetEncoding(forceencoding) ?? GetInternalEncoding(ctx));
        }

        #endregion

        #region mb_internal_encoding, mb_preferred_mime_name

        /// <summary>
        /// Get encoding used by default in the extension.
        /// </summary>
        public static string mb_internal_encoding(Context ctx)
        {
            return GetInternalEncoding(ctx).WebName;
        }

        /// <summary>
        /// Set the encoding used by the extension.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="encodingName"></param>
        /// <returns>True is encoding was set, otherwise false.</returns>
        public static bool mb_internal_encoding(Context ctx, string encodingName)
        {
            Encoding enc = GetEncoding(encodingName);

            if (enc != null)
            {
                ctx.Configuration.Get<MbConfig>().InternalEncoding = enc;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get a MIME charset string for a specific encoding.
        /// </summary>
        /// <param name="encoding_name">The encoding being checked. Its BodyName or PHP/Phalanger name.</param>
        /// <returns>The MIME charset string for given character encoding.</returns>
        public static string mb_preferred_mime_name(string encoding_name)
        {
            var enc = GetEncoding(encoding_name);

            // try PHP internal encodings (by PHP name) and .NET encodings name
            if (enc == null)
            {
                PhpException.ArgumentValueNotSupported(nameof(encoding_name), encoding_name);
                return null;
            }

            return enc.BodyName;// it seems to return right MIME
        }

        #endregion

        #region mb_list_encodings

        /// <summary>
        /// Returns an array of all supported encodings.
        /// </summary>
        public static PhpArray mb_list_encodings()
        {
            return new PhpArray(64)
            {
                "auto",
                "wchar",
                //byte2be
                //byte2le
                //byte4be
                //byte4le
                //BASE64
                //UUENCODE
                //HTML-ENTITIES
                //Quoted-Printable
                //7bit
                //8bit,
                //UCS-4
                //UCS-4BE
                //UCS-4LE
                //UCS-2
                //UCS-2BE
                //UCS-2LE
                "UTF-16",
                "UTF-16LE",
                "UTF-16BE",
                "UTF-32BE",
                //UTF-32LE
                "UTF-32",
                "UTF-8",
                "UTF-7",
                //UTF7-IMAP
                "ASCII",
                //"CP51932",
                //"CP936",
                //"CP866",
                //"CP850",
                //EUC-JP
                //SJIS
                //eucJP-win
                //SJIS-win
                //JIS
                //ISO-2022-JP
                //ISO-2022-JP-MS
                //"Windows-1252",
                //"Windows-1254",
                //ISO-8859-1
                //ISO-8859-2
                //ISO-8859-3
                //ISO-8859-4
                //ISO-8859-5
                //ISO-8859-6
                //ISO-8859-7
                //ISO-8859-8
                //ISO-8859-9
                //ISO-8859-10
                //ISO-8859-13
                //ISO-8859-14
                //ISO-8859-15
                //ISO-8859-16
                //EUC-CN
                //HZ
                //EUC-TW
                //BIG-5
                //EUC-KR
                //UHC
                //ISO-2022-KR
                //"Windows-1251",
                //KOI8-R
                //KOI8-U
                //ArmSCII-8
            };
        }

        #endregion

        #region mb_substr, mb_strcut

        [return: CastToFalse]
        public static string mb_substr(Context ctx, PhpString str, int start, int? length = default, string encoding = null)
            => SubString(ctx, str, start, length.HasValue ? length.Value : int.MaxValue, encoding);

        /// <summary>
        ///
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="str"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        /// <remarks>in PHP it behaves differently, but in .NET it is an alias for mb_substr</remarks>
        [return: CastToFalse]
        public static string mb_strcut(Context ctx, PhpString str, int start, int? length = default, string encoding = null)
            => SubString(ctx, str, start, length.HasValue ? length.Value : -1, encoding);

        static string SubString(Context ctx, PhpString str, int start, int length, string encoding)
        {
            var ustr = ToString(ctx, str, encoding);

            if (PhpMath.AbsolutizeRange(ref start, ref length, ustr.Length))
            {
                return ustr.Substring(start, length);
            }
            else
            {
                return null; // FALSE
            }
        }

        #endregion

        #region mb_substr_count

        /// <summary>
        /// Alias to <see cref="Strings.substr_count(string, string, int, int)"/>.
        /// </summary>
        public static int mb_substr_count(string haystack, string needle, string encoding = null)
        {
            return Strings.substr_count(haystack, needle);
        }

        #endregion

        #region mb_str_split

        /// <summary>
        /// Performs string splitting to an array of chunks of size <c>1</c>.
        /// </summary>
        public static PhpArray mb_str_split(string @string) => Strings.str_split(@string);

        /// <summary>
        /// Performs string splitting to an array of defined size chunks.
        /// </summary>
        public static PhpArray mb_str_split(Context ctx, PhpString @string, int split_length = 1, string encoding = null)
        {
            return Strings.str_split(ctx, @string, split_length);

            // NOTE: then we should encode it back to byte[] using:
            // Encoding encode = GetEncoding(encoding) ?? ctx.Configuration.Get<MbConfig>().InternalEncoding
        }

        #endregion

        #region mb_strtoupper, mb_strtolower

        public static string mb_strtoupper(Context ctx, PhpValue str, string encoding = null)
            => ToString(ctx, str, encoding).ToUpperInvariant();

        public static string mb_strtolower(Context ctx, PhpValue str, string encoding = null)
            => ToString(ctx, str, encoding).ToLowerInvariant();

        #endregion

        #region mb_strlen, mb_strwidth

        /// <summary>
        /// Counts characters in a Unicode string or multi-byte string in PhpBytes.
        /// </summary>
        public static int mb_strlen(Context ctx, PhpString str, string encoding = null)
        {
            if (encoding != null && ((EncodingName)encoding).Is8bit())
            {
                // gets byte count in str
                return str.GetByteCount(ctx.StringEncoding);
            }

            // encode to unicode string using provided encoding,
            // return characters count
            return ToString(ctx, str, encoding).Length;
        }

        /// <summary>
        /// Return width of string.
        /// </summary>
        public static int mb_strwidth(Context ctx, PhpValue str, string encoding = null)
        {
            /*
             * Chars                Width
             * U+0000 - U+0019	    0
             * U+0020 - U+1FFF	    1
             * U+2000 - U+FF60	    2
             * U+FF61 - U+FF9F	    1
             * U+FFA0 -	            2
             */

            var text = ToString(ctx, str, encoding);
            int width = 0;

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c >= '\x20')
                {
                    if (c < '\x2000')
                    {
                        width += 1;
                    }
                    else
                    {
                        if (c < '\xff61')
                        {
                            width += 2;
                        }
                        else
                        {
                            if (c < '\xffa0')
                            {
                                width += 1;
                            }
                            else
                            {
                                width += 2;
                            }
                        }
                    }
                }
            }

            //
            return width;
        }

        #endregion

        #region mb_strpos, mb_stripos

        [return: CastToFalse]
        public static int mb_strpos(Context ctx, PhpValue haystack, PhpValue needle, int offset, string encoding)
        {
            return strpos(ToString(ctx, haystack, encoding), ToString(ctx, needle, encoding), offset, StringComparison.Ordinal);
        }

        [return: CastToFalse]
        public static int mb_strpos(string haystack, string needle, int offset = 0)
        {
            return strpos(haystack, needle, offset, StringComparison.Ordinal);
        }

        [return: CastToFalse]
        public static int mb_stripos(Context ctx, PhpValue haystack, PhpValue needle, int offset, string encoding)
        {
            return strpos(ToString(ctx, haystack, encoding), ToString(ctx, needle, encoding), offset, StringComparison.OrdinalIgnoreCase);
        }

        [return: CastToFalse]
        public static int mb_stripos(string haystack, string needle, int offset = 0)
        {
            return strpos(haystack, needle, offset, StringComparison.OrdinalIgnoreCase);
        }

        internal static int strpos(string haystack, string needle, int offset, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(haystack))
            {
                return -1;
            }

            if (string.IsNullOrEmpty(needle))
            {
                PhpException.InvalidArgument(nameof(needle), Resources.LibResources.arg_empty);
                return -1;
            }

            if (offset < 0)
            {
                offset += haystack.Length;
            }

            if (offset < 0 || offset >= haystack.Length)
            {
                if (offset != haystack.Length)
                {
                    PhpException.InvalidArgument("offset", Resources.LibResources.arg_out_of_bounds);
                }

                return -1;
            }

            return haystack.IndexOf(needle, offset, comparison);
        }

        #endregion

        #region mb_parse_str

        public static bool mb_parse_str(string encoded_string, out PhpArray array)
        {
            array = new PhpArray();
            UriUtils.ParseQuery(encoded_string, array.AddVariable);
            return true;
        }

        public static bool mb_parse_str(Context ctx, string encoded_string)
        {
            UriUtils.ParseQuery(encoded_string, ctx.Globals.AddVariable);
            return true;
        }

        #endregion

        #region mb_convert_case

        /// <summary>
        /// Perform case folding on a string.
        /// </summary>
        public static string mb_convert_case(/*Context ctx,*/ string str, CaseConstants mode, string encoding = default)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            // var enc = GetEncoding(encoding) ?? GetInternalEncoding(ctx);

            switch (mode)
            {
                case CaseConstants.MB_CASE_UPPER_SIMPLE:
                case CaseConstants.MB_CASE_UPPER:
                    return str.ToUpper();

                case CaseConstants.MB_CASE_LOWER_SIMPLE:
                case CaseConstants.MB_CASE_LOWER:
                    return str.ToLower();

                case CaseConstants.MB_CASE_TITLE_SIMPLE:
                case CaseConstants.MB_CASE_TITLE:
                    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);

                default: throw new ArgumentException();
            }
        }

        #endregion

        #region mb_convert_encoding, mb_convert_variables

        static bool TryGetEncodingsFromStringOrArray(PhpValue from_encoding, out Encoding enc, out Encoding[] enc_array)
        {
            enc = null;
            enc_array = null;
            int invalid = 0;

            if (from_encoding.IsString(out var str))
            {
                // TODO: "auto"

                if (str.IndexOf(',') >= 0)
                {
                    // comma separated list (string)
                    var split = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length != 0)
                    {
                        enc_array = new Encoding[split.Length];
                        for (int i = 0; i < split.Length; i++)
                        {
                            var x = GetEncoding(split[i].Trim());
                            if (x == null) invalid++;

                            enc_array[i] = x;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // string
                    enc = GetEncoding(str.Trim());
                    return enc != null;
                }
            }
            else if (from_encoding.IsPhpArray(out var arr))
            {
                if (arr.Count == 0)
                {
                    // empty array
                    return false;
                }
                else
                {
                    var e = arr.GetFastEnumerator();
                    if (arr.Count == 1)
                    {
                        enc = e.MoveNext() ? GetEncoding(e.CurrentValue.ToString().Trim()) : null;
                        return enc != null;
                    }
                    else
                    {
                        enc_array = new Encoding[arr.Count];
                        int i = 0;
                        while (e.MoveNext())
                        {
                            var x = GetEncoding(e.CurrentValue.ToString().Trim());
                            if (x == null) invalid++;
                            enc_array[i++] = x;
                        }
                        Debug.Assert(i == arr.Count);
                    }
                }
            }

            //
            if (enc_array != null && enc_array.Length != 0)
            {
                if (invalid == 0)
                {
                    return true;
                }
                else
                {
                    enc_array = enc_array.Where(x => x != null).ToArray();
                    return enc_array.Length != 0;
                }
            }
            else
            {
                return false;
            }
        }

        static bool TryDetectEncoding(PhpString value, Encoding[] encodings, out Encoding detectedEncoding, out string convertedValue)
        {
            if (encodings != null)
            {
                for (int i = 0; i < encodings.Length; i++)
                {
                    var enc = encodings[i];
                    if (enc != null)
                    {
                        try
                        {
                            // NOTE: See DetectByteOrderMarkAsync() function here for possible solution:
                            // https://github.com/AngleSharp/AngleSharp/blob/master/src/AngleSharp/TextSource.cs

                            convertedValue = value.ToString(Encoding.GetEncoding(enc.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback));

                            // succeeded
                            detectedEncoding = enc;
                            return true;
                        }
                        catch
                        {
                            // nope
                        }
                    }
                }
            }

            //
            detectedEncoding = default;
            convertedValue = default;
            return false;
        }

        /// <summary>
        /// Converts the character encoding of <paramref name="str"/> to <paramref name="to_encoding"/>.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="str">Input string.</param>
        /// <param name="to_encoding">Target encoding.</param>
        /// <param name="from_encoding">
        /// Encodings to try for decoding <paramref name="str"/>.
        /// It is either an array, or a comma separated enumerated list. If from_encoding is not specified, the internal encoding will be used.
        /// </param>
        /// <returns>Converted string.</returns>
        public static PhpString mb_convert_encoding(Context ctx, PhpString str, string to_encoding, PhpValue from_encoding = default(PhpValue))
        {
            string decoded;

            if (str.ContainsBinaryData)
            {
                // source encoding
                if (!TryGetEncodingsFromStringOrArray(from_encoding, out var from_enc, out var from_encs))
                {
                    throw new ArgumentException(nameof(from_encoding));
                }

                if (from_enc != null)
                {
                    decoded = str.ToString(from_enc);
                }
                else if (!TryDetectEncoding(str, from_encs, out from_enc, out decoded))
                {
                    throw new ArgumentException(nameof(from_encoding));
                }
            }
            else
            {
                // already in UTF16
                decoded = str.ToString();
            }

            // target encoding:
            // only convert non-unicode encodings, otherwise keep the `System.String`
            var target_enc = GetEncoding(to_encoding);
            if (target_enc == null || PhpEncodingProvider.IsUtfEncoding(target_enc))
            {
                return new PhpString(decoded);
            }
            else
            {
                return new PhpString(target_enc.GetBytes(decoded));
            }
        }

        static PhpValue convert_variable(PhpValue value, PhpValue from_encoding, ref Encoding from_enc, ref Encoding[] from_encs, Encoding to_enc, ref HashSet<object> visited)
        {
            object obj;

            if (value.IsUnicodeString(out var str))
            {
                return (to_enc != null) // change from Unicode string to single-byte string
                    ? PhpValue.Create(new PhpString(to_enc.GetBytes(str)))
                    : PhpValue.Create(str);
            }
            else if (value.IsBinaryString(out var phpstr))
            {
                // we need from_encoding

                if (from_enc == default && from_encs == default && !TryGetEncodingsFromStringOrArray(from_encoding, out from_enc, out from_encs))
                {
                    throw new ArgumentException(nameof(from_encoding));
                }

                if (from_enc == null)
                {
                    // detect encoding & decode
                    if (TryDetectEncoding(phpstr, from_encs, out from_enc, out var decoded))
                    {
                        return to_enc != null
                            ? PhpValue.Create(new PhpString(to_enc.GetBytes(decoded)))
                            : PhpValue.Create(decoded);
                    }
                    else
                    {
                        throw new ArgumentException(nameof(from_encoding)); // cannot detect encoding or encodings not provided
                    }
                }
                else
                {
                    // decode
                    var decoded = phpstr.ToString(from_enc);
                    return to_enc != null
                        ? PhpValue.Create(new PhpString(to_enc.GetBytes(decoded)))
                        : PhpValue.Create(decoded);
                }
            }
            else if (value.IsPhpArray(out var arr))
            {
                if (visited == null)
                    visited = new HashSet<object>();

                if (visited.Add(arr))
                {
                    // TODO: arr.EnsureWritable() !!

                    var e = arr.GetFastEnumerator();
                    while (e.MoveNext())
                    {
                        ref var oldvalue = ref e.CurrentValue;
                        var newvalue = convert_variable(oldvalue, from_encoding, ref from_enc, ref from_encs, to_enc, ref visited);

                        Operators.SetValue(ref oldvalue, newvalue);
                    }
                }

                //
                return arr;
            }
            else if ((obj = value.AsObject()) != null && !(obj is PhpResource))
            {
                if (visited == null)
                    visited = new HashSet<object>();

                if (visited.Add(obj))
                {
                    var tinfo = obj.GetPhpTypeInfo();
                    foreach (var p in tinfo.GetDeclaredProperties())
                    {
                        if (p.IsStatic) continue;

                        var oldvalue = p.GetValue(null/*only for static properties*/, obj);
                        var newvalue = convert_variable(oldvalue, from_encoding, ref from_enc, ref from_encs, to_enc, ref visited);
                        if (oldvalue.IsAlias)
                        {
                            oldvalue.Alias.Value = newvalue;
                        }
                        else
                        {
                            p.SetValue(null, obj, newvalue);
                        }
                    }

                    var runtimefields = tinfo.GetRuntimeFields(obj);
                    if (runtimefields != null && runtimefields.Count != 0)
                    {
                        convert_variable(runtimefields, from_encoding, ref from_enc, ref from_encs, to_enc, ref visited);
                    }
                }

                return value;
            }
            else
            {
                throw new ArgumentException(nameof(value));
            }
        }

        /// <summary>
        /// Convert character code in variables.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="to_encoding">The encoding that the string is being converted to.</param>
        /// <param name="from_encoding">Array or comma separated list of encodings. If <c>NULL</c>, the <c>detect_order</c> is used.</param>
        /// <param name="vars">Reference to the variable being converted. String, Array and Object are accepted.</param>
        /// <returns>The character encoding before conversion for success, or <c>FALSE</c> for failure.</returns>
        [return: CastToFalse]
        public static string mb_convert_variables(Context ctx, string to_encoding, PhpValue from_encoding, params PhpAlias[] vars)
        {
            // source encoding,
            // initialized when first needed (when there is non-unicode string)
            Encoding from_enc = null;
            Encoding[] from_encs = Operators.IsSet(from_encoding) ? null : GetConfig(ctx).DetectOrder;

            HashSet<object> visited = null;

            // only convert non-unicode encodings, otherwise keep the `System.String`
            var to_enc = string.IsNullOrEmpty(to_encoding) || EncodingName.IsUtfEncoding(to_encoding)
                ? null
                : (ctx.StringEncoding.WebName == to_encoding/*internal encoding might not be registered but it's there*/ ? ctx.StringEncoding : GetEncoding(to_encoding));

            for (int i = 0; i < vars.Length; i++)
            {
                var obj = vars[i];

                if (obj != null)
                {
                    obj.Value = convert_variable(obj.Value, from_encoding, ref from_enc, ref from_encs, to_enc, ref visited);
                }
            }

            //
            return from_enc != null ? from_enc.WebName : ctx.StringEncoding.WebName;
        }

        #endregion

        #region mb_check_encoding

        /// <summary>
        /// Check if the string is valid for the specified encoding
        /// </summary>
        public static bool mb_check_encoding(Context ctx, PhpString var = default(PhpString), string encoding = null/*mb_internal_encoding()*/)
        {
            if (var.IsDefault)
            {
                // NS: check all the input from the beginning of the request
                throw new NotSupportedException();
            }

            if (var.ContainsBinaryData)
            {
                var enc = GetEncoding(encoding) ?? ctx.StringEncoding;

                // create encoding with exception fallbacks:
                enc = Encoding.GetEncoding(enc.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

                try
                {
                    var.ToString(enc);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region mb_substitute_character

        public static object mb_substitute_character()
        {
            PhpException.FunctionNotSupported("mb_substitute_character");
            return false;
        }

        public static object mb_substitute_character(object substrchar)
        {
            PhpException.FunctionNotSupported("mb_substitute_character");
            return "none";
        }

        #endregion

        #region mb_strimwidth implementation

        public static string mb_strimwidth(Context ctx, string str, int start, int width, string trimmarker = null, string encoding = null)
        {
            return StringTrimByWidth(
                str,
                start,
                width,
                trimmarker,
                () => string.IsNullOrEmpty(encoding) ? GetInternalEncoding(ctx) : GetEncoding(encoding));
        }

        static string StringTrimByWidth(string str, int start, int width, string trimmarker, Func<Encoding> encodingGetter)
        {
            string ustr = str; // ObjectToString(str, encodingGetter);

            if (start >= ustr.Length)
                return string.Empty;

            ustr = ustr.Substring(start);
            int ustrWidth = StringWidth(ustr);

            if (ustrWidth <= width)
                return ustr;

            // trim the string
            int trimmarkerWidth = StringWidth(trimmarker);

            width -= trimmarkerWidth;
            string trimmedStr = StringTrimByWidth(ustr, ref width);
            width += trimmarkerWidth;
            string trimmedTrimMarker = StringTrimByWidth(trimmarker, ref width);

            //
            return trimmedStr + trimmedTrimMarker;
        }

        #endregion

        #region mb_strstr, mb_stristr

        [return: CastToFalse]
        public static string mb_strstr(Context ctx, string haystack, string needle, bool part = false, string encoding = null)
        {
            return StrStr(
                haystack,
                needle,
                part,
                () => string.IsNullOrEmpty(encoding) ? GetInternalEncoding(ctx) : GetEncoding(encoding),
                false);
        }

        [return: CastToFalse]
        public static string mb_stristr(Context ctx, string haystack, string needle, bool part = false, string encoding = null)
        {
            return StrStr(
                haystack,
                needle,
                part,
                () => string.IsNullOrEmpty(encoding) ? GetInternalEncoding(ctx) : GetEncoding(encoding),
                true);
        }

        /// <summary>
        /// mb_strstr() finds the first occurrence of needle in haystack  and returns the portion of haystack. If needle is not found, it returns FALSE.
        /// </summary>
        /// <param name="haystack">The string from which to get the first occurrence of needle</param>
        /// <param name="needle">The string to find in haystack</param>
        /// <param name="part">Determines which portion of haystack  this function returns. If set to TRUE, it returns all of haystack  from the beginning to the first occurrence of needle. If set to FALSE, it returns all of haystack  from the first occurrence of needle to the end.</param>
        /// <param name="encodingGetter">Character encoding name to use. If it is omitted, internal character encoding is used. </param>
        /// <param name="ignoreCase">Case insensitive.</param>
        /// <returns>Returns the portion of haystack, or FALSE (-1) if needle is not found.</returns>
        static string StrStr(string haystack, string needle, bool part/* = false*/  , Func<Encoding> encodingGetter, bool ignoreCase)
        {
            string uhaystack = haystack; //ObjectToString(haystack, encodingGetter);
            string uneedle = needle; //ObjectToString(needle, encodingGetter);

            if (uhaystack == null || uneedle == null)   // never happen
                return null;

            if (uneedle == string.Empty)
            {
                PhpException.InvalidArgument(nameof(needle), Resources.LibResources.arg_empty);
                return null;
            }

            int index = (ignoreCase) ? uhaystack.ToLower().IndexOf(uneedle.ToLower()) : uhaystack.IndexOf(uneedle);
            return (index == -1) ? null : (part ? uhaystack.Substring(0, index) : uhaystack.Substring(index));
        }

        #endregion

        #region mb_strrpos

        [return: CastToFalse]
        public static int mb_strrpos(Context ctx, string haystack, string needle, int offset = 0, string encoding = null)
        {
            return Strrpos(
                haystack,
                needle,
                offset,
                () => string.IsNullOrEmpty(encoding) ? GetInternalEncoding(ctx) : GetEncoding(encoding),
                false);
        }

        #endregion

        #region mb_strripos

        [return: CastToFalse]
        public static int mb_strripos(Context ctx, string haystack, string needle, int offset = 0, string encoding = null)
        {
            return Strrpos(
                haystack,
                needle,
                offset,
                () => string.IsNullOrEmpty(encoding) ? GetInternalEncoding(ctx) : GetEncoding(encoding),
                true);
        }

        #endregion

        #region mb_strrchr, mb_strrichr

        [return: CastToFalse]
        public static string mb_strrchr(/*Context ctx,*/ string haystack, string needle, bool part = false, string encoding = null)
        {
            return StrrChr(
                haystack,
                needle,
                part,
                ignoreCase: false
                //() => string.IsNullOrEmpty(encoding) ? GetInternalEncoding(ctx) : GetEncoding(encoding)
                );
        }

        [return: CastToFalse]
        public static string mb_strrichr(/*Context ctx,*/ string haystack, string needle, bool part = false, string encoding = null)
        {
            return StrrChr(
                haystack,
                needle,
                part,
                ignoreCase: true
                //() => string.IsNullOrEmpty(encoding) ? GetInternalEncoding(ctx) : GetEncoding(encoding),
                );
        }

        #endregion

        #region mb_ord, mb_chr

        /// <summary>
        /// Returns a code point of character or  on failure.
        /// </summary>
        [return: CastToFalse]
        public static int mb_ord(Context ctx, PhpString str, string encoding = null)
        {
            var value = ToString(ctx, str, encoding);
            if (string.IsNullOrEmpty(value))
            {
                return -1; // FALSE
            }

            //
            return value[0];
        }

        /// <summary>
        /// Returns a specific character or <c>FALSE</c> on failure.
        /// </summary>
        public static string mb_chr(int cp, string encoding = null)
        {
            return unchecked((char)cp).ToString();
        }

        #endregion

        #region mb_language, mb_send_mail

        /// <summary>
        /// Get language used by mail functions.
        /// </summary>
        /// <returns></returns>
        public static string mb_language()
        {
            return "uni"; //MailLanguage;
        }

        /// <summary>
        /// Set the language used by mail functions.
        /// </summary>
        /// <param name="language"></param>
        /// <returns>True if language was set, otherwise false.</returns>
        public static bool mb_language(string language)
        {
            PhpException.FunctionNotSupported("mb_language");
            return false;
        }

        #region mb_send_mail(), TODO: use mb_language

        public static bool mb_send_mail(Context ctx, string to, string subject, string message, string additional_headers = null, string additional_parameter = null)
        {
            return Mail.mail(
                ctx,
                to,
                subject,
                message,
                additional_headers,
                additional_parameter);
        }

        #endregion

        #endregion

        #region mb_regex_encoding

        /// <summary>
        /// Get encoding used by regex in the extension.
        /// </summary>
        /// <returns></returns>
        public static string mb_regex_encoding(Context ctx)
        {
            return (ctx.Configuration.Get<MbConfig>().RegexEncoding ?? ctx.StringEncoding).WebName;
        }

        /// <summary>
        /// Set the encoding used by the extension in regex functions.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="encodingName"></param>
        /// <returns>True is encoding was set, otherwise false.</returns>
        public static bool mb_regex_encoding(Context ctx, string encodingName)
        {
            Encoding enc = GetEncoding(encodingName);

            if (enc != null)
            {
                ctx.Configuration.Get<MbConfig>().RegexEncoding = enc;

                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Returns the internal setting parameters of mbstring.
        /// </summary>
        public static PhpValue mb_get_info(Context ctx, string type = null)
        {
            var config = GetConfig(ctx);

            if (string.IsNullOrEmpty(type) || type == "all")
            {
                // "internal_encoding", "http_output", "http_input", "func_overload", "mail_charset", "mail_header_encoding", "mail_body_encoding"
                return new PhpArray()
                {
                    { "internal_encoding", (config.InternalEncoding ?? ctx.StringEncoding).WebName},
                    { "http_output", (config.HttpOutputEncoding ?? ctx.StringEncoding).WebName},
                    { "http_input", ctx.StringEncoding.WebName },
                    { "func_overload", 0 },
                    { "mail_charset", ctx.StringEncoding.WebName },
                    // mail_header_encoding
                    // mail_body_encoding
                };
            }
            else
            {
                // "http_output", "http_input", "internal_encoding", "func_overload"
                if (type == "http_output") return (config.HttpOutputEncoding ?? ctx.StringEncoding).WebName;
                if (type == "http_input") return ctx.StringEncoding.WebName; // TODO
                if (type == "internal_encoding") return (config.InternalEncoding ?? ctx.StringEncoding).WebName;
                if (type == "func_overload") return 0; // DEPRECATED // NS

                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Detects the HTTP input character encoding.
        /// </summary>
        /// <param name="type">
        /// <para>Input string specifies the input type. "G" for GET, "P" for POST, "C" for COOKIE, "S" for string, "L" for list, and</para>
        /// <para> "I" for the whole list (will return array). If type is omitted, it returns the last input type processed.</para>
        /// </param>
        /// <returns>The character encoding name, as per the type. If mb_http_input() does not process specified HTTP input, it returns FALSE.</returns>
        public static bool mb_http_input(string type = "")
        {
            return false;
        }

        public static string mb_http_output(Context ctx)
        {
            return (ctx.Configuration.Get<MbConfig>().HttpOutputEncoding ?? ctx.StringEncoding).WebName;
        }

        public static bool mb_http_output(Context ctx, string encodingName)
        {
            Encoding enc = GetEncoding(encodingName);

            if (enc != null)
            {
                ctx.Configuration.Get<MbConfig>().HttpOutputEncoding = enc;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sets the automatic character encoding detection order to encoding_list. 
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="encoding_list">Optional. An array or comma separated list of character encoding.</param>
        /// <returns></returns>
        /// <remarks>
        /// Supported: UTF-8, UTF-7, ASCII, EUC-JP,SJIS, eucJP-win, SJIS-win, JIS, ISO-2022-JP
        /// For ISO-8859-*, mbstring always detects as ISO-8859-*.
        /// For UTF-16, UTF-32, UCS2 and UCS4, encoding detection will fail always.
        /// </remarks>
        public static PhpValue mb_detect_order(Context ctx, PhpValue encoding_list = default)
        {
            Encoding[] encodings;

            if (Operators.IsSet(encoding_list))
            {
                if (!TryGetEncodingsFromStringOrArray(encoding_list, out var encoding, out encodings))
                {
                    return PhpValue.False;
                }

                GetConfig(ctx).DetectOrder = encodings ?? new[] { encoding };
                return PhpValue.True;
            }
            else
            {
                return mb_detect_order(ctx);
            }
        }

        /// <summary>
        /// Get character encoding detection order.
        /// </summary>
        /// <returns>An ordered array of the encodings is returned.</returns>
        [return: NotNull]
        public static PhpArray/*!*/mb_detect_order(Context ctx)
        {
            return new PhpArray(GetConfig(ctx).DetectOrder.Select(enc => enc.WebName));
        }

        /// <summary>
        /// Detects character encoding in string str. 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="str">The string being detected.</param>
        /// <param name="encoding_list">
        /// <para>A list of character encoding. Encoding order may be specified by array or comma separated list string.</para>
        /// <para>If omitted, detect_order is used.</para>
        /// </param>
        /// <param name="strict">strict specifies whether to use the strict encoding detection or not. Default is FALSE.</param>
        /// <returns>The detected character encoding or FALSE if the encoding cannot be detected from the given string.</returns>
        [return: CastToFalse]
        public static string mb_detect_encoding(Context ctx, PhpString str, PhpValue encoding_list = default, bool strict = false)
        {
            if (str.ContainsBinaryData)
            {
                Encoding encoding;
                Encoding[] encodings;

                if (!Operators.IsSet(encoding_list))
                {
                    encodings = GetConfig(ctx).DetectOrder;
                }
                else
                {
                    if (!TryGetEncodingsFromStringOrArray(encoding_list, out encoding, out encodings))
                    {
                        return null;
                    }

                    if (encodings == null && encoding != null)
                    {
                        encodings = new[] { encoding };
                    }
                }

                if (TryDetectEncoding(str, encodings, out encoding, out var _))
                {
                    return encoding.WebName;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return ctx.StringEncoding.WebName; // NOTE: we should return something from encoding_order
            }
        }

        /// <summary>
        /// Implementation of <c>mb_strr[i]pos</c> functions.
        /// </summary>
        static int Strrpos(string haystack, string needle, int offset, Func<Encoding> encodingGetter, bool ignoreCase)
        {
            string uhaystack = haystack; //ObjectToString(haystack, encodingGetter);
            string uneedle = needle; //ObjectToString(needle, encodingGetter);

            if (uhaystack == null || uneedle == null)
                return -1;

            int end = uhaystack.Length - 1;
            if (offset > end || offset < -end - 1)
            {
                PhpException.InvalidArgument(nameof(offset), Resources.LibResources.arg_out_of_bounds);
                return -1;
            }

            if (offset < 0)
            {
                end += uneedle.Length + offset;
                offset = 0;
            }

            if (uneedle.Length == 0)
            {
                PhpException.InvalidArgument(nameof(needle), Resources.LibResources.arg_empty);
                return -1;
            }

            if (ignoreCase)
                return uhaystack.ToLower().LastIndexOf(uneedle.ToLower(), end, end - offset + 1);
            else
                return uhaystack.LastIndexOf(uneedle, end, end - offset + 1);
        }

        static int StringWidth(string str)
        {
            if (str == null)
                return 0;

            int width = 0;

            foreach (char c in str)
                width += CharWidth(c);

            return width;
        }

        /// <summary>
        /// Determines the char width.
        /// </summary>
        /// <param name="c">Character.</param>
        /// <returns>The width of the character.</returns>
        static int CharWidth(char c)
        {
            //Chars  	            Width
            //U+0000 - U+0019 	0
            //U+0020 - U+1FFF 	1
            //U+2000 - U+FF60 	2
            //U+FF61 - U+FF9F 	1
            //U+FFA0 - 	        2

            if (c <= 0x0019) return 0;
            else if (c <= 0x1fff) return 1;
            else if (c <= 0xff60) return 2;
            else if (c <= 0xff9f) return 1;
            else return 2;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="str"></param>
        /// <param name="width">Characters remaining.</param>
        /// <returns></returns>
        static string StringTrimByWidth(string/*!*/str, ref int width)
        {
            if (str == null)
                return null;

            int i = 0;

            foreach (char c in str)
            {
                int w = CharWidth(c);

                if (w < width)
                {
                    ++i;
                    width -= w;
                }
                else if (w == width)
                {
                    ++i;
                    width = 0;
                    break;
                }
                else
                    break;
            }

            return (i < str.Length) ? str.Remove(i) : str;
        }

        static string StrrChr(string haystack, string needle, bool beforeNeedle/*=false*/, bool ignoreCase/*, Func<Encoding> encodingGetter*/)
        {
            string uhaystack = haystack; //ObjectToString(haystack, encodingGetter);
            string uneedle = needle;
            {
                //string uneedle;

                //if (needle is string) uneedle = (string)needle;
                //else if (needle is PhpString) uneedle = ((IPhpConvertible)needle).ToString();
                //else if (needle is PhpBytes)
                //{
                //    Encoding encoding = encodingGetter();
                //    if (encoding == null)
                //        return null;

                //    PhpBytes bytes = (PhpBytes)needle;
                //    uneedle = encoding.GetString(bytes.ReadonlyData, 0, bytes.Length);
                //}
                //else
                //{   // needle as a character number
                //    Encoding encoding = encodingGetter();
                //    if (encoding == null)
                //        return null;

                //    uneedle = encoding.GetString(new byte[] { unchecked((byte)Core.Convert.ObjectToInteger(needle)) }, 0, 1);
                //}

                if (string.IsNullOrEmpty(uneedle))
                    return null;
            }

            int index = uhaystack.LastIndexOf(uneedle, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
            if (index >= 0)
            {
                return (beforeNeedle) ? uhaystack.Remove(index) : uhaystack.Substring(index);
            }
            else
            {
                return null;
            }
        }

        static Encoding GetInternalEncoding(Context ctx)
        {
            return (ctx.Configuration.Get<MbConfig>().InternalEncoding ?? ctx.StringEncoding);
        }

        static Encoding GetRegexEncoding(Context ctx)
        {
            return (ctx.Configuration.Get<MbConfig>().RegexEncoding ?? ctx.StringEncoding);
        }

        ///// <summary>
        ///// Converts PhpBytes using specified encoding. If any other object is provided, encoding is not performed.
        ///// </summary>
        ///// <param name="str"></param>
        ///// <param name="encodingGetter"></param>
        ///// <returns></returns>
        //static string ObjectToString(PhpValue str, Func<Encoding> encodingGetter)
        //{
        //    if (str is PhpBytes)
        //    {
        //        PhpBytes bytes = (PhpBytes)str;
        //        Encoding encoding = encodingGetter();

        //        if (encoding == null)
        //            return null;

        //        return encoding.GetString(bytes.ReadonlyData, 0, bytes.Length);
        //    }
        //    else
        //    {
        //        // .NET String should be always UTF-16, given encoding is irrelevant
        //        return PHP.Core.Convert.ObjectToString(str);
        //    }
        //}
    }
}