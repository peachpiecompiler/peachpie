using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;
using static Pchp.Library.JsonSerialization;
using Microsoft.Extensions.ObjectPool;
using Pchp.Core.Utilities;

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
            return ctx.TryGetStatic<JsonLastError>(out var p) ? p.LastError : 0;
        }

        #endregion

        #region JsonSerializer

        internal sealed class JsonSerializer : Serializer
        {
            #region Tokens

            /// <summary>
            /// Contains definition of tokens that constitute PHP serialized data.
            /// </summary>
            internal class Tokens
            {
                internal const char ObjectOpen = '{';
                internal const char ObjectClose = '}';

                internal const char ItemsSeparator = ',';
                internal const char PropertyKeyValueSeparator = ':';

                internal const char Quote = '"';
                internal const string DoubleQuoteString = "\"\""; // ""
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

                internal static char PrettySpace => ' '; // whitespace for pretty printing
                internal static char PrettyNewLine => '\n'; // whitespace for pretty printing
            }

            #endregion

            #region Nested struct: MiniSet // helper data structure

            /// <summary>
            /// Helper data structure maintaining "Stack" of objects.
            /// Does not allocate for less than 2 objects.
            /// </summary>
            struct MiniSet
            {
                /// <summary>
                /// Internal data, either object reference or reference to <see cref="object"/>[] representing the set.
                /// </summary>
                object value;

                /// <summary>
                /// Stack size if <see cref="value"/> referes to <see cref="object"/>[].
                /// </summary>
                int top;

                /// <summary>
                /// Counts object refereces in the set;
                /// </summary>
                public int Count(object obj)
                {
                    if (ReferenceEquals(value, obj))
                    {
                        return 1;
                    }
                    else if (value is object[] array)
                    {
                        Debug.Assert(top <= array.Length);

                        int count = 0;
                        for (int i = 0; i < top; i++)
                        {
                            if (ReferenceEquals(array[i], obj))
                            {
                                count++;
                            }
                        }
                        return count;
                    }

                    //
                    return 0;
                }

                public static void Push(ref MiniSet set, object obj)
                {
                    if (ReferenceEquals(set.value, null))
                    {
                        set.value = obj;
                    }
                    else if (set.value is object[] array)
                    {
                        Debug.Assert(set.top <= array.Length);
                        if (set.top == array.Length)
                        {
                            Array.Resize(ref array, array.Length * 2);
                            set.value = array;
                        }

                        array[set.top++] = obj;
                    }
                    else
                    {
                        // upgrade _value to object[4]
                        set.value = new[] { set.value, obj, null, null, };
                        set.top = 2;
                    }
                }

                /// <summary>
                /// Removes one occurence of the given object reference from the set.
                /// </summary>
                public static bool Pop(ref MiniSet set, object obj)
                {
                    if (ReferenceEquals(set.value, obj))
                    {
                        set.value = null;
                        return true;
                    }
                    else if (
                        set.top > 0 &&
                        set.value is object[] array &&
                        ReferenceEquals(array[set.top - 1], obj))
                    {
                        set.top--;
                        return true;
                    }

                    // ERR
                    return false;
                }
            }

            #endregion

            #region ObjectWriter

            internal sealed class ObjectWriter : PhpVariableVisitor
            {
                //Encoding Encoding => _ctx.StringEncoding;

                MiniSet _recursion;

                /// <summary>
                /// Result data.
                /// </summary>
                readonly StringBuilder _result;

                readonly Context _ctx;
                //readonly RuntimeTypeHandle _caller;
                readonly JsonEncodeOptions _encodeOptions;
                readonly IPrettyPrinter _pretty;

                const int LastAsciiCharacter = 0x7F;

                #region IPrettyPrinter

                /// <summary>
                /// Used to output pretty printing whitespaces if enabled.
                /// </summary>
                interface IPrettyPrinter
                {
                    void Indent();
                    void Unindent();

                    /// <summary>Writes a single whitespace, if <see cref="JSON_PRETTY_PRINT"/> is enabled.</summary>
                    void Space();

                    /// <summary>Writes new line and indentation, if <see cref="JSON_PRETTY_PRINT"/> is enabled.</summary>
                    void NewLine();
                }

                sealed class PrettyPrintOff : IPrettyPrinter
                {
                    public static PrettyPrintOff Instance { get; } = new PrettyPrintOff();

                    private PrettyPrintOff() { }

                    public void Indent() { }
                    public void NewLine() { }
                    public void Space() { }
                    public void Unindent() { }
                }

                sealed class PrettyPrintOn : IPrettyPrinter
                {
                    int _indent;
                    readonly StringBuilder _output;

                    public PrettyPrintOn(StringBuilder output)
                    {
                        _output = output ?? throw new ArgumentNullException();
                    }

                    public void Indent()
                    {
                        _indent++;
                    }

                    public void NewLine()
                    {
                        _output.Append(Tokens.PrettyNewLine);
                        _output.Append(' ', _indent * 4);
                    }

                    public void Space()
                    {
                        _output.Append(Tokens.PrettySpace);
                    }

                    public void Unindent()
                    {
                        _indent--;
                        Debug.Assert(_indent >= 0);
                    }
                }

                #endregion

                #region Options

                bool HasForceObject => (_encodeOptions & JsonEncodeOptions.JSON_FORCE_OBJECT) != 0;
                bool HasHexAmp => (_encodeOptions & JsonEncodeOptions.JSON_HEX_AMP) != 0;
                bool HasHexApos => (_encodeOptions & JsonEncodeOptions.JSON_HEX_APOS) != 0;
                bool HasHexQuot => (_encodeOptions & JsonEncodeOptions.JSON_HEX_QUOT) != 0;
                bool HasHexTag => (_encodeOptions & JsonEncodeOptions.JSON_HEX_TAG) != 0;
                bool HasNumericCheck => (_encodeOptions & JsonEncodeOptions.JSON_NUMERIC_CHECK) != 0;
                bool HasPrettyPrint => (_encodeOptions & JsonEncodeOptions.JSON_PRETTY_PRINT) != 0;
                bool HasUnescapedSlashes => (_encodeOptions & JsonEncodeOptions.JSON_UNESCAPED_SLASHES) != 0;
                bool HasUnescapedUnicode => (_encodeOptions & JsonEncodeOptions.JSON_UNESCAPED_UNICODE) != 0;
                bool HasPreserveZeroFraction => (_encodeOptions & JsonEncodeOptions.JSON_PRESERVE_ZERO_FRACTION) != 0;

                #endregion

                private ObjectWriter(Context ctx, StringBuilder result, JsonEncodeOptions encodeOptions, RuntimeTypeHandle caller)
                {
                    Debug.Assert(ctx != null);
                    _ctx = ctx;
                    _encodeOptions = encodeOptions;
                    _result = result ?? throw new ArgumentNullException(nameof(result));
                    //_caller = caller;
                    _pretty = HasPrettyPrint ? (IPrettyPrinter)new PrettyPrintOn(_result) : PrettyPrintOff.Instance;
                }

                public static string Serialize(Context ctx, PhpValue variable, JsonEncodeOptions encodeOptions, RuntimeTypeHandle caller)
                {
                    var str = StringBuilderUtilities.Pool.Get();

                    variable.Accept(new ObjectWriter(ctx, str, encodeOptions, caller));

                    return StringBuilderUtilities.GetStringAndReturn(str); // note: str is cleared
                }

                bool PushObject(object obj)
                {
                    if (_recursion.Count(obj) < 2)
                    {
                        MiniSet.Push(ref _recursion, obj);
                        return true;
                    }
                    else
                    {
                        PhpException.Throw(PhpError.Warning, Resources.LibResources.recursion_detected);
                        return false;
                    }
                }

                void PopObject(object obj)
                {
                    MiniSet.Pop(ref _recursion, obj);
                }

                void WriteRaw(string str) => _result.Append(str);
                void WriteRaw(char c) => _result.Append(c);

                public override void AcceptNull()
                {
                    WriteNull();
                }

                public override void Accept(bool obj)
                {
                    WriteBoolean(obj);
                }

                public override void Accept(long obj)
                {
                    WriteRaw(obj.ToString());
                }

                public override void Accept(string obj)
                {
                    WriteString(obj);
                }

                public override void Accept(PhpString obj)
                {
                    // TODO: escape single-byte characters properly
                    WriteString(obj.ToString(_ctx));
                }

                public override void Accept(double obj)
                {
                    var aslong = unchecked((long)obj);

                    if (HasPreserveZeroFraction && aslong == obj)
                    {
                        WriteRaw(aslong.ToString());
                        WriteRaw(".0"); // as PHP does
                    }
                    else
                    {
                        // "G" format specifier,
                        // does not append floating point if .0
                        WriteRaw(obj.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }

                /// <summary>
                /// Determines if the array is integer indexed in sequnece from 0 without "holes".
                /// </summary>
                /// <remarks>Determines if the array can be encoded as JSON array.</remarks>
                static bool IsSequentialArray(PhpArray/*!*/array)
                {
                    // "Packed" array is sequential array with ordered set of integer key without "holes"

                    if (array.Count != 0 && !array.IsPacked())
                    {
                        int next = 0;
                        var enumerator = array.GetFastEnumerator();
                        while (enumerator.MoveNext())
                        {
                            var key = enumerator.CurrentKey;
                            if (key.IsString || key.Integer != next++)
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }

                public override void Accept(PhpArray array)
                {
                    if (PushObject(array))
                    {
                        if (HasForceObject || !IsSequentialArray(array))
                        {
                            // array are encoded as objects or there are keyed values that has to be encoded as object
                            //WriteObject(JsonArrayProperties(array));
                            WriteArrayAsObject(array);
                        }
                        else
                        {
                            // we can write JSON array
                            WriteArray(array);
                        }

                        PopObject(array);
                    }
                    else
                    {
                        WriteNull();
                    }
                }

                public override void AcceptObject(object obj)
                {
                    if (obj is JsonSerializable)
                    {
                        var data = ((JsonSerializable)obj).jsonSerialize();
                        if ((obj = data.AsObject()) == null)
                        {
                            data.Accept(this);
                            return;
                        }
                        else
                        {
                            // serialize `obj` without checking JsonSerializable interface ... 
                        }
                    }

                    if (obj is PhpResource)
                    {
                        WriteUnsupported(PhpResource.PhpTypeName);
                        return;
                    }

                    if (PushObject(obj))
                    {
                        WriteObject(JsonObjectProperties(obj));
                        PopObject(obj);
                    }
                    else
                    {
                        WriteNull();
                    }
                }

                #region encoding strings

                /// <summary>
                /// Determines if given character is printable character. Otherwise it must be encoded.
                /// </summary>
                private bool CharIsPrintable(char c)
                {
                    return
                        (c <= LastAsciiCharacter || HasUnescapedUnicode) &&   // ASCII
                        !(c >= 9 && c <= 13) && // not BS, HT, LF, Vertical Tab, Form Feed, CR
                        !char.IsControl(c); // not control
                }

                /// <summary>
                /// Determines if given character should be encoded.
                /// </summary>
                bool CharShouldBeEncoded(char c)
                {
                    switch (c)
                    {
                        case '/':
                        case Tokens.Quote:
                        case Tokens.Escape:
                            return true;

                        case '\'':
                            return HasHexApos;

                        case '<':
                        case '>':
                            return HasHexTag;

                        case '&':
                            return HasHexAmp;

                        default:
                            return
                                (c <= 0x1f) ||
                                (c > 0x7f && HasUnescapedUnicode);
                    }
                }

                /// <summary>
                /// Determines if some chars in given string should be encoded.
                /// </summary>
                private bool StringShouldBeEncoded(string str)
                {
                    Debug.Assert(str != null);

                    for (int i = 0; i < str.Length; i++)
                    {
                        if (CharShouldBeEncoded(str[i]))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                /// <summary>
                /// Convert 16b character into json encoded character.
                /// </summary>
                /// <param name="value">The full string to be encoded.</param>
                private void EncodeStringIncremental(string value)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        char c = value[i];

                        switch (c)
                        {
                            case '\n':
                                _result.Append(Tokens.EscapedNewLine);
                                break;
                            case '\r':
                                _result.Append(Tokens.EscapedCR);
                                break;
                            case '\t':
                                _result.Append(Tokens.EscapedTab);
                                break;
                            case '/':
                                if (HasUnescapedSlashes) { goto default; }
                                _result.Append(Tokens.EscapedSolidus);
                                break;
                            case Tokens.Escape:
                                _result.Append(Tokens.EscapedReverseSolidus);
                                break;
                            case '\b':
                                _result.Append(Tokens.EscapedBackspace);
                                break;
                            case '\f':
                                _result.Append(Tokens.EscapedFormFeed);
                                break;
                            case Tokens.Quote:
                                _result.Append(HasHexQuot ? (Tokens.EscapedUnicodeChar + "0022") : Tokens.EscapedQuote);
                                break;
                            case '\'':
                                _result.Append(HasHexApos ? (Tokens.EscapedUnicodeChar + "0027") : "'");
                                break;
                            case '<':
                                _result.Append(HasHexTag ? (Tokens.EscapedUnicodeChar + "003C") : "<");
                                break;
                            case '>':
                                _result.Append(HasHexTag ? (Tokens.EscapedUnicodeChar + "003E") : ">");
                                break;
                            case '&':
                                _result.Append(HasHexAmp ? (Tokens.EscapedUnicodeChar + "0026") : "&");
                                break;
                            default:
                                {
                                    if (CharIsPrintable(c))
                                    {
                                        int start = i++;
                                        while (i < value.Length && !CharShouldBeEncoded(value[i]))
                                            ++i;

                                        _result.Append(value, start, (i--) - start);   // accumulate characters, mostly it is entire string value (faster)
                                    }
                                    else
                                    {
                                        _result.Append(Tokens.EscapedUnicodeChar + ((int)c).ToString("X4"));
                                    }
                                    break;
                                }
                        }
                    }
                }

                #endregion

                /// <summary>
                /// Serializes null and throws an exception.
                /// </summary>
                /// <param name="TypeName"></param>
                private void WriteUnsupported(string TypeName)
                {
                    PhpException.Throw(PhpError.Warning, Resources.LibResources.serialization_unsupported_type, TypeName);
                    WriteNull();
                }

                void WriteNull()
                {
                    WriteRaw(Tokens.NullLiteral);
                }

                void WriteBoolean(bool value)
                {
                    WriteRaw(value ? Tokens.TrueLiteral : Tokens.FalseLiteral);
                }

                /// <summary>
                /// Serializes JSON string.
                /// </summary>
                /// <param name="value">The string.</param>
                void WriteString(string value)
                {
                    if (HasNumericCheck)
                    {
                        long l;
                        double d;
                        var result = Core.Convert.StringToNumber(value, out l, out d);
                        if ((result & Core.Convert.NumberInfo.IsNumber) != 0)
                        {
                            if ((result & Core.Convert.NumberInfo.LongInteger) != 0) WriteRaw(l.ToString());
                            if ((result & Core.Convert.NumberInfo.Double) != 0) WriteRaw(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            return;
                        }
                    }

                    if (value.Length == 0)
                    {
                        // empty string
                        WriteRaw(Tokens.DoubleQuoteString);   // ""
                    }
                    else if (!StringShouldBeEncoded(value)) // most common case
                    {
                        // string can be appended as it is
                        WriteRaw(Tokens.Quote);
                        WriteRaw(value);
                        WriteRaw(Tokens.Quote);
                    }
                    else
                    {
                        _result.Append(Tokens.Quote);

                        EncodeStringIncremental(value);

                        _result.Append(Tokens.Quote);
                    }
                }

                void WriteArray(PhpArray array)
                {
                    // [
                    WriteRaw(Tokens.ArrayOpen);

                    //
                    if (array.Count != 0)
                    {
                        _pretty.Indent();

                        bool bFirst = true;

                        var enumerator = array.GetFastEnumerator();
                        while (enumerator.MoveNext())
                        {
                            // ,
                            if (bFirst) bFirst = false;
                            else WriteRaw(Tokens.ItemsSeparator);

                            _pretty.NewLine();

                            Debug.Assert(enumerator.CurrentKey.IsInteger);

                            // value
                            Accept(enumerator.CurrentValue);
                        }

                        _pretty.Unindent();
                        _pretty.NewLine();
                    }

                    // ]
                    WriteRaw(Tokens.ArrayClose);
                }

                void WriteArrayAsObject(PhpArray array)
                {
                    // [
                    WriteRaw(Tokens.ObjectOpen);

                    //
                    if (array.Count != 0)
                    {
                        _pretty.Indent();

                        bool bFirst = true;

                        var enumerator = array.GetFastEnumerator();
                        while (enumerator.MoveNext())
                        {
                            // ,
                            if (bFirst) bFirst = false;
                            else WriteRaw(Tokens.ItemsSeparator);

                            _pretty.NewLine();

                            // "key": value
                            WriteString(enumerator.CurrentKey.ToString());
                            WriteRaw(Tokens.PropertyKeyValueSeparator);
                            _pretty.Space();
                            Accept(enumerator.CurrentValue);
                        }

                        _pretty.Unindent();

                        if (!bFirst)
                        {
                            _pretty.NewLine();
                        }
                    }

                    // ]
                    WriteRaw(Tokens.ObjectClose);
                }

                void WriteObject(IEnumerable<KeyValuePair<string, PhpValue>> properties)
                {
                    // {
                    WriteRaw(Tokens.ObjectOpen);

                    _pretty.Indent();

                    bool bFirst = true;
                    foreach (var pair in properties)
                    {
                        // ,
                        if (bFirst)
                        {
                            bFirst = false;
                        }
                        else
                        {
                            WriteRaw(Tokens.ItemsSeparator);
                        }

                        _pretty.NewLine();

                        // "key": value
                        WriteString(pair.Key);
                        WriteRaw(Tokens.PropertyKeyValueSeparator);
                        _pretty.Space();
                        Accept(pair.Value);
                    }

                    _pretty.Unindent();

                    if (!bFirst)
                    {
                        _pretty.NewLine();
                    }

                    // }
                    WriteRaw(Tokens.ObjectClose);
                }

                // static IEnumerable<KeyValuePair<string, PhpValue>> JsonArrayProperties(PhpArray array)
                // {
                //     var enumerator = array.GetFastEnumerator();
                //     while (enumerator.MoveNext())
                //     {
                //         var current = enumerator.Current;
                //         yield return new KeyValuePair<string, PhpValue>(current.Key.ToString(), current.Value);
                //     }
                // }

                static IEnumerable<KeyValuePair<string, PhpValue>> JsonObjectProperties(object/*!*/obj)
                {
                    return TypeMembersUtils.EnumerateInstanceFields(obj, TypeMembersUtils.s_propertyName, TypeMembersUtils.s_keyToString);
                }
            }

            #endregion

            #region Serializer

            public override string Name => "JSON";

            protected override PhpValue CommonDeserialize(Context ctx, Stream data, RuntimeTypeHandle caller)
            {

                var options = _decodeOptions ?? new DecodeOptions();
                var scanner = new Json.JsonScanner(new StreamReader(data), options);
                var parser = new Json.Parser(options) { Scanner = scanner };

                if (parser.Parse())
                {
                    if (ctx.TryGetStatic<JsonLastError>(out var jsonerror))
                    {
                        jsonerror.LastError = JSON_ERROR_NONE;
                    }
                }
                else
                {
                    var errorcode = JSON_ERROR_SYNTAX;

                    if ((options.Options & JsonDecodeOptions.JSON_THROW_ON_ERROR) == 0)
                    {
                        ctx.GetStatic<JsonLastError>().LastError = errorcode;
                        return PhpValue.Null;
                    }
                    else
                    {
                        throw new JsonException(code: errorcode);
                    }
                }

                //
                return parser.Result;
            }

            protected override PhpString CommonSerialize(Context ctx, PhpValue variable, RuntimeTypeHandle caller)
            {
                return ObjectWriter.Serialize(ctx, variable, _encodeOptions, caller);
            }

            #endregion

            public JsonSerializer(DecodeOptions decodeOptions = null, JsonEncodeOptions encodeOptions = JsonEncodeOptions.Default)
            {
                _decodeOptions = decodeOptions;
                _encodeOptions = encodeOptions;
            }

            #region Options

            /// <summary>
            /// Decode (unserialize) options.
            /// </summary>
            public class DecodeOptions
            {
                public JsonDecodeOptions Options = JsonDecodeOptions.Default;

                /// <summary>
                /// When TRUE, returned object s will be converted into associative array s. 
                /// </summary>
                public bool Assoc => (Options & JsonDecodeOptions.JSON_OBJECT_AS_ARRAY) != 0;

                /// <summary>
                /// User specified recursion depth. 
                /// </summary>
                public int Depth = 512;

                public bool BigIntAsString => (Options & JsonDecodeOptions.JSON_BIGINT_AS_STRING) != 0;
            }

            readonly DecodeOptions _decodeOptions;
            readonly JsonEncodeOptions _encodeOptions;

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
        enum JsonError
        {
            None = JSON_ERROR_NONE,
            Depth = JSON_ERROR_DEPTH,
            StateMismatch = JSON_ERROR_STATE_MISMATCH,
            CtrlChar = JSON_ERROR_CTRL_CHAR,
            Syntax = JSON_ERROR_SYNTAX,
            Utf8 = JSON_ERROR_UTF8,
            Recursion = JSON_ERROR_RECURSION,
            InfOrNan = JSON_ERROR_INF_OR_NAN,
            UnsupportedType = JSON_ERROR_UNSUPPORTED_TYPE,
            InvalidPropertyName = JSON_ERROR_INVALID_PROPERTY_NAME,
        }

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
        public const int JSON_ERROR_STATE_MISMATCH = 2;

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
        /// 
        /// </summary>
        public const int JSON_ERROR_RECURSION = 6;

        /// <summary>
        /// 
        /// </summary>
        public const int JSON_ERROR_INF_OR_NAN = 7;

        /// <summary>
        /// 
        /// </summary>
        public const int JSON_ERROR_UNSUPPORTED_TYPE = 8;

        /// <summary>
        /// 
        /// </summary>
        public const int JSON_ERROR_INVALID_PROPERTY_NAME = 9;

        /// <summary>
        /// 
        /// </summary>
        public const int JSON_ERROR_UTF16 = 10;

        /// <summary>
        /// 
        /// </summary>
        public const int JSON_THROW_ON_ERROR = 1 << 22;

        /// <summary>
        /// Options given to json_encode function.
        /// </summary>
        [PhpHidden]
        [Flags]
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

            /// <summary>
            /// Don't escape /.
            /// </summary>
            JSON_UNESCAPED_SLASHES = 64,

            /// <summary>
            /// Use whitespace in returned data to format it.
            /// </summary>
            JSON_PRETTY_PRINT = 128,

            /// <summary>
            /// Encode multibyte Unicode characters literally (default is to escape as \uXXXX).
            /// </summary>
            JSON_UNESCAPED_UNICODE = 256,

            /// <summary>
            /// Ensures that float values are always encoded as a float value.
            /// </summary>
            JSON_PRESERVE_ZERO_FRACTION = 1024,

            JSON_THROW_ON_ERROR = JsonSerialization.JSON_THROW_ON_ERROR,
        }

        public const int JSON_HEX_TAG = (int)JsonEncodeOptions.JSON_HEX_TAG;
        public const int JSON_HEX_AMP = (int)JsonEncodeOptions.JSON_HEX_AMP;
        public const int JSON_HEX_APOS = (int)JsonEncodeOptions.JSON_HEX_APOS;
        public const int JSON_HEX_QUOT = (int)JsonEncodeOptions.JSON_HEX_QUOT;
        public const int JSON_FORCE_OBJECT = (int)JsonEncodeOptions.JSON_FORCE_OBJECT;
        public const int JSON_NUMERIC_CHECK = (int)JsonEncodeOptions.JSON_NUMERIC_CHECK;
        public const int JSON_UNESCAPED_SLASHES = (int)JsonEncodeOptions.JSON_UNESCAPED_SLASHES;
        public const int JSON_PRETTY_PRINT = (int)JsonEncodeOptions.JSON_PRETTY_PRINT;
        public const int JSON_UNESCAPED_UNICODE = (int)JsonEncodeOptions.JSON_UNESCAPED_UNICODE;
        public const int JSON_PRESERVE_ZERO_FRACTION = (int)JsonEncodeOptions.JSON_PRESERVE_ZERO_FRACTION;

        /// <summary>
        /// Options given to json_decode function.
        /// </summary>
        [PhpHidden]
        [Flags]
        public enum JsonDecodeOptions
        {
            Default = 0,

            /// <summary>
            /// Decodes JSON objects as PHP array.
            /// This option can be added automatically by calling <see cref="json_decode"/>() with the second parameter equal to <c>TRUE</c>.
            /// </summary>
            JSON_OBJECT_AS_ARRAY = 1,

            /// <summary>
            /// Big integers represent as strings rather than floats.
            /// </summary>
            JSON_BIGINT_AS_STRING = 2,

            JSON_THROW_ON_ERROR = JsonSerialization.JSON_THROW_ON_ERROR,
        }

        public const int JSON_OBJECT_AS_ARRAY = (int)JsonDecodeOptions.JSON_OBJECT_AS_ARRAY;

        public const int JSON_BIGINT_AS_STRING = (int)JsonDecodeOptions.JSON_BIGINT_AS_STRING;

        #endregion

        #region json_encode, json_decode, json_last_error

        /// <summary>
        /// Returns the JSON representation of a value.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="value">The value being encoded. Can be any type except a <see cref="PhpResource"/>.
        /// All string data must be UTF-8 encoded.</param>
        /// <param name="options"></param>
        /// <param name="depth">Set the maximum depth. Must be greater than zero.</param>
        public static string json_encode(Context ctx, PhpValue value, JsonEncodeOptions options = JsonEncodeOptions.Default, int depth = 512)
        {
            // TODO: depth

            //return new PhpSerialization.JsonSerializer(encodeOptions: options).Serialize(ctx, value, default);
            return PhpSerialization.JsonSerializer.ObjectWriter.Serialize(ctx, value, options, default);
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
                Depth = depth,
                Options = options,
            };

            if (assoc)
            {
                decodeoptions.Options |= JsonDecodeOptions.JSON_OBJECT_AS_ARRAY;
            }

            return new PhpSerialization.JsonSerializer(decodeOptions: decodeoptions).Deserialize(ctx, json, default);
        }

        public static int json_last_error(Context ctx) => PhpSerialization.GetLastJsonError(ctx);

        /// <summary>
        /// Returns the error string of the last <see cref="json_encode"/> or <see cref="json_decode"/> call.
        /// </summary>
        public static string json_last_error_msg(Context ctx)
        {
            var error = (JsonError)PhpSerialization.GetLastJsonError(ctx);

            // TODO: to resources

            switch (error)
            {
                case JsonError.None: return "No error";
                case JsonError.Depth: return "Maximum stack depth exceeded";
                case JsonError.StateMismatch: return "State mismatch (invalid or malformed JSON)";
                case JsonError.CtrlChar: return "Control character error, possibly incorrectly encoded";
                case JsonError.Syntax: return "Syntax error";
                case JsonError.Utf8: return "Malformed UTF-8 characters, possibly incorrectly encoded";
                case JsonError.Recursion:
                case JsonError.InfOrNan:
                case JsonError.UnsupportedType:
                case JsonError.InvalidPropertyName:
                    return error.ToString();

                default: throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }

    /// <summary>
    /// Exception thrown by <see cref="json_encode"/> and <see cref="json_decode"/> in case of an error,
    /// if <see cref="JSON_THROW_ON_ERROR"/> flag is specified.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension("json")]
    public class JsonException : Spl.Exception
    {
        [PhpFieldsOnlyCtor]
        protected JsonException() { }

        public JsonException(string message = "", long code = 0, Spl.Throwable previous = null)
            : base(message, code, previous)
        {
        }
    }
}

/// <summary>
/// Objects implementing JsonSerializable can customize their JSON representation when encoded with <see cref="Pchp.Library.JsonSerialization.json_encode"/>.
/// </summary>
[PhpType(PhpTypeAttribute.InheritName)]
[PhpExtension("json")]
public interface JsonSerializable
{
    /// <summary>
    /// Serializes the object to a value that can be serialized natively by <see cref="Pchp.Library.JsonSerialization.json_encode"/>.
    /// </summary>
    /// <returns>
    /// Returns data which can be serialized by <see cref="Pchp.Library.JsonSerialization.json_encode"/>,
    /// which is a value of any type other than a resource.
    /// </returns>
    PhpValue jsonSerialize();
}