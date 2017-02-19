using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core;

namespace Pchp.Library
{
    [PhpExtension("mbstring")]
    public static class MultiByteString
    {
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

        private static Dictionary<string, Encoding> _encodings = null;
        public static Dictionary<string, Encoding>/*!*/Encodings
        {
            get
            {
                if (_encodings == null)
                {
                    var enc = new Dictionary<string, Encoding>(180, StringComparer.OrdinalIgnoreCase);

                    // encoding names used in PHP

                    //enc["pass"] = Encoding.Default; // TODO: "pass" encoding
                    enc["auto"] = Encoding.UTF8; // Configuration.Application.Globalization.PageEncoding;
                    enc["wchar"] = Encoding.Unicode;
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
                    enc["UTF-16LE"] = Encoding.Unicode; // alias UTF-16
                    //UTF-8
                    //UTF-7
                    //UTF7-IMAP
                    enc["ASCII"] = Encoding.ASCII;  // alias us-ascii
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

                    //// .NET encodings
                    //foreach (var encoding in Encoding.GetEncodings())
                    //{
                    //    enc[encoding.Name] = encoding.GetEncoding();
                    //}

                    _encodings = enc;
                }

                return _encodings;
            }
        }

        /// <summary>
        /// Get encoding based on the PHP name. Can return null is such encoding is not defined.
        /// </summary>
        /// <param name="encodingName"></param>
        /// <returns></returns>
        internal static Encoding GetEncoding(string encodingName)
        {
            Encoding encoding;
            if (encodingName == null)
            {
                encoding = null;
            }
            else if (!Encodings.TryGetValue(encodingName, out encoding))
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
        public static string mb_convert_encoding(Context ctx, PhpString str , string to_encoding, PhpValue from_encoding)
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
