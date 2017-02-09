using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;

namespace Pchp.Library
{
    static partial class PhpSerialization
    {
        #region JsonLastError

        class JsonLastError
        {
            public int LastError;
        }

        internal static int GetLastJsonError(Context ctx)
        {
            var p = ctx.TryGetProperty<JsonLastError>();
            return (p != null) ? p.LastError : 0;
        }

        #endregion

        #region JsonSerializer

        internal sealed class JsonSerializer : Serializer
        {
            #region Tokens

            /// <summary>
            /// Contains definition of (one-character) tokens that constitute PHP serialized data.
            /// </summary>
            internal class Tokens
            {
                internal const char ObjectOpen = '{';
                internal const char ObjectClose = '}';
                internal const char ItemsSeparator = ',';
                internal const char PropertyKeyValueSeparator = ':';

                internal const char Quote = '"';
                internal const char Escape = '\\';

                internal const string EscapedNewLine = @"\n";
                internal const string EscapedCR = @"\r";
                internal const string EscapedTab = @"\t";
                internal const string EscapedBackspace = @"\b";
                internal const string EscapedQuote = "\\\"";
                internal const string EscapedReverseSolidus = @"\\";
                internal const string EscapedSolidus = @"\/";
                internal const string EscapedFormFeed = @"\f";
                internal const string EscapedUnicodeChar = @"\u";   // 4-digit number follows

                internal const char ArrayOpen = '[';
                internal const char ArrayClose = ']';

                internal const string NullLiteral = "null";
                internal const string TrueLiteral = "true";
                internal const string FalseLiteral = "false";

            }

            #endregion

            #region ObjectWriter

            sealed class ObjectWriter : PhpVariableVisitor
            {

            }

            #endregion

            #region Serializer

            public override string Name => "JSON";

            protected override PhpValue CommonDeserialize(Context ctx, Stream data, RuntimeTypeHandle caller)
            {
                var jsonerror = ctx.GetStatic<JsonLastError>();

                var options = _decodeOptions ?? new DecodeOptions();
                var scanner = new Json.JsonScanner(new StreamReader(data), options);
                var parser = new Json.Parser(options) { Scanner = scanner };

                jsonerror.LastError = JsonSerialization.JSON_ERROR_NONE;

                if (!parser.Parse())
                {
                    jsonerror.LastError = JsonSerialization.JSON_ERROR_SYNTAX;
                    return PhpValue.Null;
                }

                //
                return parser.Result;
            }

            protected override PhpString CommonSerialize(Context ctx, PhpValue variable, RuntimeTypeHandle caller)
            {
                throw new NotImplementedException();
            }

            #endregion

            public JsonSerializer(DecodeOptions decodeOptions = null, EncodeOptions encodeOptions = null)
            {
                _decodeOptions = decodeOptions;
                _encodeOptions = encodeOptions;
            }

            #region Options

            /// <summary>
            /// Encode (serialize) options. All false.
            /// </summary>
            public class EncodeOptions
            {
                public bool HexTag = false, HexAmp = false, HexApos = false, HexQuot = false, ForceObject = false, NumericCheck = false;
            }

            /// <summary>
            /// Decode (unserialize) options.
            /// </summary>
            public class DecodeOptions
            {
                public bool BigIntAsString = false;

                /// <summary>
                /// When TRUE, returned object s will be converted into associative array s. 
                /// </summary>
                public bool Assoc = false;

                /// <summary>
                /// User specified recursion depth. 
                /// </summary>
                public int Depth = 512;
            }

            readonly DecodeOptions _decodeOptions;
            readonly EncodeOptions _encodeOptions;

            #endregion
        }

        #endregion
    }

    [PhpExtension("json")]
    public static class JsonSerialization
    {
        #region Constants

        // 
        // Values returned by json_last_error function.
        //
        /// <summary>
        /// No error has occurred  	 
        /// </summary>
        public const int JSON_ERROR_NONE = 0;

        /// <summary>
        /// The maximum stack depth has been exceeded  	 
        /// </summary>
        public const int JSON_ERROR_DEPTH = 1;

        /// <summary>
        /// Occurs with underflow or with the modes mismatch.
        /// </summary>
        public const int PHP_JSON_ERROR_STATE_MISMATCH = 2;

        /// <summary>
        /// Control character error, possibly incorrectly encoded  	 
        /// </summary>
        public const int JSON_ERROR_CTRL_CHAR = 3;

        /// <summary>
        /// Syntax error  	 
        /// </summary>
        public const int JSON_ERROR_SYNTAX = 4;

        /// <summary>
        /// 
        /// </summary>
        public const int JSON_ERROR_UTF8 = 5;

        /// <summary>
        /// Options given to json_encode function.
        /// </summary>
        [PhpHidden]
        public enum JsonEncodeOptions
        {
            /// <summary>
            /// No options specified.
            /// </summary>
            Default = 0,

            /// <summary>
            /// All &lt; and &gt; are converted to \u003C and \u003E. 
            /// </summary>
            JSON_HEX_TAG = 1,

            /// <summary>
            /// All &amp;s are converted to \u0026. 
            /// </summary>
            JSON_HEX_AMP = 2,

            /// <summary>
            /// All ' are converted to \u0027. 
            /// </summary>
            JSON_HEX_APOS = 4,

            /// <summary>
            /// All " are converted to \u0022. 
            /// </summary>
            JSON_HEX_QUOT = 8,

            /// <summary>
            /// Outputs an object rather than an array when a non-associative array is used. Especially useful when the recipient of the output is expecting an object and the array is empty. 
            /// </summary>
            JSON_FORCE_OBJECT = 16,

            /// <summary>
            /// Encodes numeric strings as numbers. 
            /// </summary>
            JSON_NUMERIC_CHECK = 32,
        }

        public const int JSON_HEX_TAG = (int)JsonEncodeOptions.JSON_HEX_TAG;
        public const int JSON_HEX_AMP = (int)JsonEncodeOptions.JSON_HEX_AMP;
        public const int JSON_HEX_APOS = (int)JsonEncodeOptions.JSON_HEX_APOS;
        public const int JSON_HEX_QUOT = (int)JsonEncodeOptions.JSON_HEX_QUOT;
        public const int JSON_FORCE_OBJECT = (int)JsonEncodeOptions.JSON_FORCE_OBJECT;
        public const int JSON_NUMERIC_CHECK = (int)JsonEncodeOptions.JSON_NUMERIC_CHECK;

        /// <summary>
        /// Options given to json_decode function.
        /// </summary>
        [PhpHidden]
        public enum JsonDecodeOptions
        {
            Default = 0,

            /// <summary>
            /// Big integers represent as strings rather than floats.
            /// </summary>
            JSON_BIGINT_AS_STRING = 1,
        }

        public const int JSON_BIGINT_AS_STRING = (int)JsonDecodeOptions.JSON_BIGINT_AS_STRING;

        #endregion

        #region json_encode, json_decode, json_last_error

        public static PhpString json_encode(Context ctx, PhpValue value, JsonEncodeOptions options = JsonEncodeOptions.Default)
        {
            var encodeoptions = (options != JsonEncodeOptions.Default)
                ? new PhpSerialization.JsonSerializer.EncodeOptions()
                {
                    ForceObject = (options & JsonEncodeOptions.JSON_FORCE_OBJECT) != 0,
                    HexAmp = (options & JsonEncodeOptions.JSON_HEX_AMP) != 0,
                    HexApos = (options & JsonEncodeOptions.JSON_HEX_APOS) != 0,
                    HexQuot = (options & JsonEncodeOptions.JSON_HEX_QUOT) != 0,
                    HexTag = (options & JsonEncodeOptions.JSON_HEX_TAG) != 0,
                    NumericCheck = (options & JsonEncodeOptions.JSON_NUMERIC_CHECK) != 0,
                }
                : null;

            return new PhpSerialization.JsonSerializer(encodeOptions: encodeoptions).Serialize(ctx, value, default(RuntimeTypeHandle));
        }

        /// <summary>
        /// Takes a JSON encoded string and converts it into a PHP variable.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="json"></param>
        /// <param name="assoc">When TRUE, returned object's will be converted into associative array s. </param>
        /// <param name="depth">User specified recursion depth. </param>
        /// <param name="options"></param>
        /// <returns>Returns the value encoded in json in appropriate PHP type. Values true, false and null are returned as TRUE, FALSE and NULL respectively. NULL is returned if the json cannot be decoded or if the encoded data is deeper than the recursion limit.</returns>
        public static PhpValue json_decode(Context ctx, PhpString json, bool assoc = false, int depth = 512, JsonDecodeOptions options = JsonDecodeOptions.Default)
        {
            if (json.IsEmpty) return PhpValue.Null;

            var decodeoptions = new PhpSerialization.JsonSerializer.DecodeOptions()
            {
                Assoc = assoc,
                Depth = depth,
                BigIntAsString = (options & JsonDecodeOptions.JSON_BIGINT_AS_STRING) != 0
            };

            return new PhpSerialization.JsonSerializer(decodeOptions: decodeoptions).Deserialize(ctx, json, default(RuntimeTypeHandle));
        }

        public static int json_last_error(Context ctx) => PhpSerialization.GetLastJsonError(ctx);

        #endregion
    }
}
