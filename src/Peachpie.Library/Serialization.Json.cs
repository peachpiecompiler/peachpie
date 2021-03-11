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
using System.Text.Json;
using Pchp.Library.Spl;

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

        internal static void SetLastJsonError(Context ctx, int err = JSON_ERROR_NONE)
        {
            if (err == 0)
            {
                if (ctx.TryGetStatic<JsonLastError>(out var jsonerror))
                {
                    jsonerror.LastError = err;
                }
            }
            else
            {
                ctx.GetStatic<JsonLastError>().LastError = err;
            }
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

            #region ObjectWriter

            internal sealed class ObjectWriter : PhpVariableVisitor
            {
                #region Nested struct: StackHelper

                /// <summary>
                /// Helper data structure maintaining "Stack" of objects.
                /// Does not allocate for 0 to 3 items.
                /// </summary>
                readonly struct StackHelper
                {
                    readonly object _0, _1, _2;
                    readonly object[] _3; // additional set of items > 3, gets actually modified across operations

                    /// <summary>
                    /// Count of objects in the set.
                    /// </summary>
                    readonly int _count;

                    StackHelper(object o0, object o1, object o2, object[] orest, int count)
                    {
                        _0 = o0;
                        _1 = o1;
                        _2 = o2;
                        _3 = orest;
                        _count = count;
                        Debug.Assert(_count >= 0);
                        Debug.Assert(_count <= 3 || _3.Length >= _count - 3);
                    }

                    /// <summary>
                    /// Gets count of objects.
                    /// </summary>
                    public int Count() => _count;

                    /// <summary>
                    /// Counts object refereces in the set.
                    /// </summary>
                    public readonly int Count(object obj)
                    {
                        int count = 0;

                        for (int i = 0; i < _count; i++)
                        {
                            if (this[i] == obj)
                            {
                                count++;
                            }
                        }

                        return count;
                    }

                    public readonly object this[int index]
                    {
                        get
                        {
                            return index switch
                            {
                                0 => _0,
                                1 => _1,
                                2 => _2,
                                _ => _3?[index - 3],
                            };
                        }
                    }

                    /// <summary>
                    /// Creates set with the given object on top.
                    /// </summary>
                    public readonly StackHelper Push(object obj)
                    {
                        switch (_count)
                        {
                            case 0: return new StackHelper(obj, null, null, _3, 1);
                            case 1: return new StackHelper(_0, obj, null, _3, 2);
                            case 2: return new StackHelper(_0, _1, obj, _3, 3);
                            default:
                                var index = _count - 3;
                                var array = _3 ?? new object[4];
                                if (array.Length <= index)
                                    Array.Resize(ref array, index * 2);
                                array[index] = obj;

                                return new StackHelper(_0, _1, _2, array, _count + 1);
                        }
                    }

                    /// <summary>
                    /// Creates set without the last object.
                    /// </summary>
                    public readonly StackHelper Pop(out object popped)
                    {
                        switch (_count)
                        {
                            case 0:
                                popped = null;
                                return this;
                            case 1:
                                popped = _0;
                                return new StackHelper(null, null, null, _3, 0);
                            case 2:
                                popped = _1;
                                return new StackHelper(_0, null, null, _3, 1);
                            case 3:
                                popped = _2;
                                return new StackHelper(_0, _1, null, _3, 2);
                            default:
                                var index = _count - 1 - 3;
                                popped = _3[index];
                                _3[index] = null;
                                return new StackHelper(_0, _1, _2, _3, _count - 1);
                        }
                    }
                }

                #endregion

                /// <summary>
                /// Binary strings passed to json_encode are required to be in UTF-8, we can enforce it using this special encoding instance.
                /// </summary>
                private static readonly Encoding Utf8CheckedEncoding =
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

                /// <summary>
                /// An invariant number format to be used.
                /// </summary>
                static System.Globalization.NumberFormatInfo NumberFormatInfo => Context.InvariantNumberFormatInfo;

                //Encoding Encoding => _ctx.StringEncoding;

                StackHelper _recursion;

                /// <summary>
                /// Result data.
                /// </summary>
                readonly StringBuilder _result;

                readonly Context _ctx;
                //readonly RuntimeTypeHandle _caller;
                readonly JsonEncodeOptions _encodeOptions;
                readonly IPrettyPrinter _pretty;
                readonly int _maxdepth;

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
                bool PartialOutputOnError => (_encodeOptions & JsonEncodeOptions.JSON_PARTIAL_OUTPUT_ON_ERROR) != 0;
                bool ThrowOnError => (_encodeOptions & JsonEncodeOptions.JSON_THROW_ON_ERROR) != 0;

                #endregion

                private ObjectWriter(Context ctx, StringBuilder result, JsonEncodeOptions encodeOptions, RuntimeTypeHandle caller, long depth)
                {
                    Debug.Assert(ctx != null);
                    // Debug.Assert(depth >= 0); // NOTE: negative values allowed

                    _ctx = ctx;
                    _encodeOptions = encodeOptions;
                    _result = result ?? throw new ArgumentNullException(nameof(result));
                    //_caller = caller;
                    _pretty = HasPrettyPrint ? (IPrettyPrinter)new PrettyPrintOn(_result) : PrettyPrintOff.Instance;
                    _maxdepth = depth < 0 ? 0 : depth < int.MaxValue ? (int)depth : int.MaxValue;
                }

                public static string Serialize(Context ctx, PhpValue variable, JsonEncodeOptions encodeOptions, RuntimeTypeHandle caller, long depth)
                {
                    var str = StringBuilderUtilities.Pool.Get();

                    try
                    {
                        variable.Accept(new ObjectWriter(ctx, str, encodeOptions, caller, depth));

                        return str.ToString();
                    }
                    finally
                    {
                        StringBuilderUtilities.Pool.Return(str);
                    }
                }

                /// <summary>
                /// handles the error - either remembers its code or throws exception.
                /// </summary>
                /// <param name="code">Error code.</param>
                /// <param name="message">Error message.</param>
                void HandleError(JsonError code, string message)
                {
                    if (PartialOutputOnError)
                    {
                        SetLastJsonError(_ctx/*, message*/, (int)code);
                    }
                    else
                    {
                        throw new JsonException(message, (long)code);
                    }
                }

                bool PushObject(object obj)
                {
                    if (_recursion.Count(obj) < 2)
                    {
                        _recursion = _recursion.Push(obj);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                void PopObject(object obj)
                {
                    _recursion = _recursion.Pop(out var popped);
                    Debug.Assert(popped == obj);
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
                    WriteRaw(obj.ToString(NumberFormatInfo));
                }

                public override void Accept(string obj)
                {
                    WriteString(obj);
                }

                public override void Accept(PhpString obj)
                {
                    try
                    {
                        // TODO: escape single-byte characters properly
                        WriteString(obj.ToString(Utf8CheckedEncoding));
                    }
                    catch (DecoderFallbackException)
                    {
                        HandleError(JsonError.Utf8, Resources.LibResources.serialization_json_utf8_error);
                        WriteNull();
                    }
                }

                public override void Accept(double obj)
                {
                    var aslong = unchecked((long)obj);

                    if (HasPreserveZeroFraction && aslong == obj)
                    {
                        WriteRaw(aslong.ToString(NumberFormatInfo));
                        WriteRaw(".0"); // as PHP does
                    }
                    else if (double.IsNaN(obj) || double.IsInfinity(obj))
                    {
                        HandleError(JsonError.InfOrNan, Resources.LibResources.serialization_json_inf_nan_error);
                        WriteRaw("0"); // always "0", without .0 fraction
                    }
                    else
                    {
                        // "G" format specifier,
                        // does not append floating point if .0
                        WriteRaw(obj.ToString(NumberFormatInfo));
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
                    if (_recursion.Count() >= _maxdepth)
                    {
                        HandleError(JsonError.Depth, Resources.LibResources.serialization_max_depth);
                        // TODO: on partial output, write top level elements
                        WriteNull();
                    }
                    else if (PushObject(array))
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
                        HandleError(JsonError.Recursion, Resources.LibResources.recursion_detected);
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

                    if (_recursion.Count() >= _maxdepth)
                    {
                        HandleError(JsonError.Depth, Resources.LibResources.serialization_max_depth);
                        // TODO: on partial output, write top level properties
                        WriteNull();
                    }
                    else if (obj is PhpResource)
                    {
                        HandleError(JsonError.UnsupportedType, string.Format(Resources.LibResources.serialization_unsupported_type, PhpResource.PhpTypeName));
                        WriteNull();
                    }
                    else if (PushObject(obj))
                    {
                        WriteObject(JsonObjectProperties(obj));
                        PopObject(obj);
                    }
                    else
                    {
                        HandleError(JsonError.Recursion, Resources.LibResources.recursion_detected);
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
                            if ((result & Core.Convert.NumberInfo.LongInteger) != 0) WriteRaw(l.ToString(NumberFormatInfo));
                            if ((result & Core.Convert.NumberInfo.Double) != 0) WriteRaw(d.ToString(NumberFormatInfo));
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
                    if (obj is IPhpJsonSerializable serializable_internal)
                    {
                        return serializable_internal.Properties;
                    }
                    else
                    {
                        return TypeMembersUtils.EnumerateInstanceFields(obj, TypeMembersUtils.s_propertyName, TypeMembersUtils.s_keyToString);
                    }
                }
            }

            #endregion

            #region ObjectReader

            internal sealed class ObjectReader
            {
                /// <summary>
                /// Deserializes the value, sets json_last_error or throws <see cref="JsonException"/> eventually.
                /// </summary>
                public static PhpValue DeserializeWithError(Context ctx, ReadOnlySpan<byte> utf8bytes, JsonReaderOptions options, JsonDecodeOptions phpoptions)
                {
                    try
                    {
                        var value = Deserialize(utf8bytes, options, phpoptions);

                        SetLastJsonError(ctx, JSON_ERROR_NONE);

                        return value;
                    }
                    catch (JsonException jsonex)
                    {
                        if ((phpoptions & JsonDecodeOptions.JSON_THROW_ON_ERROR) == 0)
                        {
                            SetLastJsonError(ctx, jsonex.getCode());
                            return PhpValue.Null;
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // internal error,
                        // treat as syntax error

                        if ((phpoptions & JsonDecodeOptions.JSON_THROW_ON_ERROR) == 0)
                        {
                            SetLastJsonError(ctx, JSON_ERROR_SYNTAX);
                            return PhpValue.Null;
                        }
                        else
                        {
                            throw new JsonException(ex.Message, JSON_ERROR_SYNTAX, ex as Throwable);
                        }
                    }
                }

                public static PhpValue Deserialize(ReadOnlySpan<byte> utf8bytes, JsonReaderOptions options, JsonDecodeOptions phpoptions)
                {
                    var reader = new Utf8JsonReader(utf8bytes, options);
                    return ReadValue(ref reader, phpoptions);
                }

                static PhpValue ReadValue(ref Utf8JsonReader reader, JsonDecodeOptions phpoptions)
                {
                    if (reader.Read())
                    {
                        return GetValue(ref reader, phpoptions);
                    }

                    // EOF
                    return PhpValue.Null;
                }

                static PhpValue GetValue(ref Utf8JsonReader reader, JsonDecodeOptions phpoptions)
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.StartObject:
                            var props = new PhpArray(ReadObject(ref reader, phpoptions));
                            return (phpoptions & JsonDecodeOptions.JSON_OBJECT_AS_ARRAY) != 0
                                ? PhpValue.Create(props)
                                : PhpValue.FromClass(props.AsStdClass());

                        case JsonTokenType.StartArray:
                            return new PhpArray(ReadArray(ref reader, phpoptions));

                        case JsonTokenType.String:
                            return reader.GetString();

                        case JsonTokenType.Number:
                            if (reader.TryGetInt64(out var l)) return l;
                            if ((phpoptions & JsonDecodeOptions.JSON_BIGINT_AS_STRING) != 0 && IsIntegerType(reader.ValueSpan))
                            {
                                // big int encode as string
                                //return Encoding.ASCII.GetString(reader.ValueSpan); // NETSTANDARD2.1: ReadOnlySpan<byte>
                                return JsonEncodedText.Encode(reader.ValueSpan).ToString();
                            }
                            if (reader.TryGetDouble(out var d)) return d;
                            ThrowError(JSON_ERROR_INF_OR_NAN);
                            break;

                        case JsonTokenType.True:
                            return PhpValue.True;

                        case JsonTokenType.False:
                            return PhpValue.False;

                        case JsonTokenType.Null:
                            return PhpValue.Null;
                    }

                    //
                    ThrowError(JSON_ERROR_SYNTAX);
                    return PhpValue.Null;
                }

                static bool IsIntegerType(ReadOnlySpan<byte> value)
                {
                    // -?[0-9]
                    foreach (var c in value)
                    {
                        if (c >= '0' && c <= '9')
                        {
                            // ok
                        }
                        else if (c == '-')
                        {
                            // ok
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return true;
                }

                static OrderedDictionary ReadObject(ref Utf8JsonReader reader, JsonDecodeOptions phpoptions)
                {
                    var props = new OrderedDictionary();

                    // read properties until EndObject
                    for (; ; )
                    {
                        if (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                props[reader.GetString()] = ReadValue(ref reader, phpoptions);
                                continue;
                            }
                            else if (reader.TokenType == JsonTokenType.EndObject)
                            {
                                break;
                            }
                        }

                        ThrowError(JSON_ERROR_SYNTAX);
                    }

                    //
                    return props;
                }

                static OrderedDictionary ReadArray(ref Utf8JsonReader reader, JsonDecodeOptions phpoptions)
                {
                    var props = new OrderedDictionary();

                    // read values until EndArray
                    for (; ; )
                    {
                        if (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray)
                            {
                                break;
                            }
                            else
                            {
                                props.Add(GetValue(ref reader, phpoptions));
                                continue;
                            }
                        }

                        ThrowError(JSON_ERROR_SYNTAX);
                    }

                    //
                    return props;
                }

                static void ThrowError(int code)
                {
                    throw new JsonException(string.Empty, code);
                }
            }

            #endregion

            #region Serializer

            public override string Name => "JSON";

            protected override PhpValue CommonDeserialize(Context ctx, Stream data, RuntimeTypeHandle caller)
            {
                return ObjectReader.DeserializeWithError(ctx, StreamToSpan(data), new JsonReaderOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    MaxDepth = this._decodeOptions.Depth,
                }, this._decodeOptions.Options);
            }

            static ReadOnlySpan<byte> StreamToSpan(Stream data)
            {
                var ms = new MemoryStream();
                data.CopyTo(ms);
                return ms.GetBuffer().AsSpan(0, (int)ms.Length);
            }

            protected override PhpString CommonSerialize(Context ctx, PhpValue variable, RuntimeTypeHandle caller)
            {
                SetLastJsonError(ctx, 0);

                return ObjectWriter.Serialize(ctx, variable, _encodeOptions, caller, 512);
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

    /// <summary>
    /// Provides explicit object behavior for <see cref="JsonSerialization.json_encode"/>.
    /// </summary>
    public interface IPhpJsonSerializable
    {
        /// <summary>
        /// Returns properties to be serialized.
        /// </summary>
        IEnumerable<KeyValuePair<string, PhpValue>> Properties { get; }
    }

    [PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Json)]
    public static class JsonSerialization
    {
        #region Constants

        // 
        // Values returned by json_last_error function.
        //
        internal enum JsonError
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
            /// Substitute some unencodable values instead of failing.
            /// </summary>
            JSON_PARTIAL_OUTPUT_ON_ERROR = 512,

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
        public const int JSON_PARTIAL_OUTPUT_ON_ERROR = (int)JsonEncodeOptions.JSON_PARTIAL_OUTPUT_ON_ERROR;
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
        [return: CastToFalse]
        public static string json_encode(Context ctx, PhpValue value, JsonEncodeOptions options = JsonEncodeOptions.Default, long depth = 512)
        {
            // TODO: depth

            PhpSerialization.SetLastJsonError(ctx, 0);

            //return new PhpSerialization.JsonSerializer(encodeOptions: options).Serialize(ctx, value, default);

            try
            {
                return PhpSerialization.JsonSerializer.ObjectWriter.Serialize(ctx, value, options, default, depth);
            }
            catch (JsonException jsonex)
            {
                if ((options & JsonEncodeOptions.JSON_THROW_ON_ERROR) != 0)
                {
                    throw;
                }

                PhpSerialization.SetLastJsonError(ctx, jsonex.getCode());
                return null;
            }
        }

        /// <summary>
        /// Takes a JSON encoded string and converts it into a PHP variable.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="json"></param>
        /// <param name="assoc">When TRUE, returned objects will be converted into associative arrays. </param>
        /// <param name="depth">User specified recursion depth. </param>
        /// <param name="options"></param>
        /// <returns>Returns the value encoded in json in appropriate PHP type. Values true, false and null are returned as TRUE, FALSE and NULL respectively. NULL is returned if the json cannot be decoded or if the encoded data is deeper than the recursion limit.</returns>
        public static PhpValue json_decode(Context ctx, PhpString json, bool assoc = false, int depth = 512, JsonDecodeOptions options = JsonDecodeOptions.Default)
        {
            if (json.IsEmpty)
            {
                return PhpValue.Null;
            }

            if (assoc)
            {
                options |= JsonDecodeOptions.JSON_OBJECT_AS_ARRAY;
            }

            //var decodeoptions = new PhpSerialization.JsonSerializer.DecodeOptions()
            //{
            //    Depth = depth,
            //    Options = options,
            //};

            //return new PhpSerialization.JsonSerializer(decodeOptions: decodeoptions).Deserialize(ctx, new PhpString(json.ToBytes(Encoding.UTF8)), default);

            //
            return PhpSerialization.JsonSerializer.ObjectReader.DeserializeWithError(
                ctx,
                json.ToBytes(Encoding.UTF8).AsSpan(),
                new JsonReaderOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                    MaxDepth = depth,
                },
                options);
        }

        public static int json_last_error(Context ctx) => PhpSerialization.GetLastJsonError(ctx);

        /// <summary>
        /// Returns the error string of the last <see cref="json_encode"/> or <see cref="json_decode"/> call.
        /// </summary>
        public static string json_last_error_msg(Context ctx)
        {
            var error = (JsonError)PhpSerialization.GetLastJsonError(ctx);

            // TODO: to resources

            return error switch
            {
                JsonError.None => "No error",
                JsonError.Depth => Resources.LibResources.serialization_max_depth,
                JsonError.StateMismatch => "State mismatch (invalid or malformed JSON)",
                JsonError.CtrlChar => "Control character error, possibly incorrectly encoded",
                JsonError.Syntax => "Syntax error",
                JsonError.Utf8 => "Malformed UTF-8 characters, possibly incorrectly encoded",
                JsonError.Recursion => Resources.LibResources.recursion_detected,
                JsonError.InfOrNan => Resources.LibResources.serialization_json_inf_nan_error,
                JsonError.UnsupportedType => error.ToString(),
                JsonError.InvalidPropertyName => error.ToString(),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        #endregion
    }

    /// <summary>
    /// Exception thrown by <see cref="json_encode"/> and <see cref="json_decode"/> in case of an error,
    /// if <see cref="JSON_THROW_ON_ERROR"/> flag is specified.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    [PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Json)]
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
[PhpExtension(PhpExtensionAttribute.KnownExtensionNames.Json)]
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