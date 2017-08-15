using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;
using static Pchp.Library.StandardPhpOptions;

namespace Pchp.Library
{
    [PhpExtension("mbstring", Registrator = typeof(MultiByteString.Registrator))]
    public static class MultiByteString
    {
        #region IconvConfig, Options

        sealed class MbConfig : IPhpConfiguration
        {
            public Encoding InternalEncoding { get; set; }

            public IPhpConfiguration Copy() => (MbConfig)this.MemberwiseClone();

            public string ExtensionName => "mbstring";

            /// <summary>
            /// Gets or sets a value of a legacy configuration option.
            /// </summary>
            private static PhpValue GetSet(IPhpConfigurationService config, string option, PhpValue value, IniAction action)
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
                    //mbstring.func_overload  "0" PHP_INI_SYSTEM PHP_INI_PERDIR from PHP 4.3 to 5.2.6, otherwise PHP_INI_SYSTEM. Available since PHP 4.2.0.Deprecated in PHP 7.2.0.
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
                //const string s = "mbstring";
                //GetSetDelegate d = new GetSetDelegate(GetSet);

                //Register("mbstring.internal_encoding", IniFlags.Supported | IniFlags.Local, d, s);
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
        }

        public const int MB_CASE_UPPER = (int)CaseConstants.MB_CASE_UPPER;
        public const int MB_CASE_LOWER = (int)CaseConstants.MB_CASE_LOWER;
        public const int MB_CASE_TITLE = (int)CaseConstants.MB_CASE_TITLE;

        #endregion

        #region Encodings

        sealed class PhpEncodingProvider : EncodingProvider
        {
            public override Encoding GetEncoding(int codepage)
            {
                return null;
            }

            public override Encoding GetEncoding(string name)
            {
                // encoding names used in PHP

                //enc["pass"] = Encoding.Default; // TODO: "pass" encoding
                if (name.EqualsOrdinalIgnoreCase("auto")) return Encoding.UTF8;
                if (name.EqualsOrdinalIgnoreCase("wchar")) return Encoding.Unicode;
                //byte2be
                //byte2le
                //byte4be
                //byte4le
                //BASE64
                //UUENCODE
                //HTML-ENTITIES
                //Quoted-Printable
                //7bit
                //8bit
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
                if (name.EqualsOrdinalIgnoreCase("UTF-16LE")) return Encoding.Unicode;// alias UTF-16
                if (name.EqualsOrdinalIgnoreCase("UTF-8")) return Encoding.UTF8;// alias UTF8
                //UTF-7
                //UTF7-IMAP
                if (name.EqualsOrdinalIgnoreCase("ASCII")) return Encoding.ASCII;  // alias us-ascii
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

                //
                return null;
            }
        }

        /// <summary>
        /// Get encoding based on the PHP name. Can return null is such encoding is not defined.
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
                case PhpTypeCode.WritableString: return ToString(ctx, value.WritableString, forceencoding);
                case PhpTypeCode.Alias: return ToString(ctx, value.Alias.Value, forceencoding);
                default: return value.ToStringOrThrow(ctx);
            }
        }

        static string ToString(Context ctx, PhpString value, string forceencoding = null)
        {
            return value.ContainsBinaryData
                ? value.ToString(GetEncoding(forceencoding) ?? ctx.StringEncoding)
                : value.ToString(ctx);  // no bytes have to be converted anyway
        }

        static byte[] ToBytes(Context ctx, PhpString value, string forceencoding = null)
        {
            return value.ToBytes(GetEncoding(forceencoding) ?? ctx.StringEncoding);
        }

        #endregion

        #region mb_internal_encoding, mb_preferred_mime_name

        /// <summary>
        /// Get encoding used by default in the extension.
        /// </summary>
        public static string mb_internal_encoding(Context ctx)
        {
            return (ctx.Configuration.Get<MbConfig>().InternalEncoding ?? ctx.StringEncoding).WebName;
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
        /// <param name="encoding_name">The encoding being checked. Its WebName or PHP/Phalanger name.</param>
        /// <returns>The MIME charset string for given character encoding.</returns>
        public static string mb_preferred_mime_name(string encoding_name)
        {
            Encoding encoding;

            if (
                (encoding = Encoding.GetEncoding(encoding_name)) == null && // .NET encodings (by their WebName)
                (encoding = GetEncoding(encoding_name)) == null //try PHP internal encodings too (by PHP/Phalanger name)
                )
            {
                PhpException.ArgumentValueNotSupported(nameof(encoding_name), encoding);
                return null;
            }

            //return encoding.BodyName;   // it seems to return right MIME
            return encoding.WebName;
        }

        #endregion

        #region mb_substr, mb_strcut

        [return: CastToFalse]
        public static string mb_substr(Context ctx, PhpValue str, int start, int length = -1, string encoding = null)
            => SubString(ctx, str, start, length, encoding);

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
        public static string mb_strcut(Context ctx, PhpValue str, int start, int length = -1, string encoding = null)
            => SubString(ctx, str, start, length, encoding);

        static string SubString(Context ctx, PhpValue str, int start, int length, string encoding)
        {
            // get the Unicode representation of the string
            string ustr = ToString(ctx, str, encoding);

            // start counting from the end of the string
            if (start < 0)
                start = ustr.Length + start;    // can result in negative start again -> invalid

            if (length == -1)
                length = ustr.Length;

            // check boundaries
            if (start >= ustr.Length || length < 0 || start < 0)
                return null;

            if (length == 0)
                return string.Empty;

            // return the substring
            return (start + length > ustr.Length) ? ustr.Substring(start) : ustr.Substring(start, length);
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

        #region mb_strtoupper, mb_strtolower

        public static string mb_strtoupper(Context ctx, PhpValue str, string encoding = null)
            => ToString(ctx, str, encoding).ToUpperInvariant();

        public static string mb_strtolower(Context ctx, PhpValue str, string encoding = null)
            => ToString(ctx, str, encoding).ToLowerInvariant();

        #endregion

        #region mb_strlen

        /// <summary>
        /// Counts characters in a Unicode string or multi-byte string in PhpBytes.
        /// </summary>
        public static int mb_strlen(Context ctx, PhpValue str, string encoding = null) => ToString(ctx, str, encoding).Length;

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

        #region mb_convert_encoding 

        public static string mb_convert_encoding(Context ctx, PhpString str, string to_encoding)
        {
            return mb_convert_encoding(ctx, str, to_encoding, PhpValue.Void /*mb_internal_encoding*/);
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
        public static string mb_convert_encoding(Context ctx, PhpString str, string to_encoding, PhpValue from_encoding)
        {
            PhpException.FunctionNotSupported("mb_convert_encoding");

            if (from_encoding.IsNull)
            {
                // from_encoding = mb_internal_encoding;
            }

            var target_enc = GetEncoding(to_encoding) ?? ctx.StringEncoding;
            var bytes = ToBytes(ctx, str/*, from_encoding*/);   // TODO: try all encodings in {from_encoding}
            return target_enc.GetString(bytes);
        }

        #endregion
    }
}
