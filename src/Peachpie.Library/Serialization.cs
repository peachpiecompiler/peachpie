using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;
using System.Text;
using System.Globalization;
using Pchp.Library.Resources;
using System.IO;
using System.Diagnostics;

namespace Pchp.Library
{
    public static class PhpSerialization
    {
        #region Serializer

        internal abstract class Serializer
        {
            /// <summary>
            /// Gets the serializer name.
            /// </summary>
            public abstract string Name { get; }

            public override string ToString() => Name;

            /// <summary>
            /// Serializes a graph of connected objects to a string (unicode or binary) using a given formatter.
            /// </summary>
            /// <param name="ctx">Runtime context.</param>
            /// <param name="variable">The variable to serialize.</param>
            /// <param name="caller">Caller's class context.</param>
            /// <returns>
            /// The serialized representation of the <paramref name="variable"/> or a <B>null</B> reference on error.
            /// </returns>
            /// <exception cref="PhpException">Serialization failed (Notice).</exception>
            public PhpString Serialize(Context ctx, PhpValue variable, RuntimeTypeHandle caller)
            {
                try
                {
                    return CommonSerialize(ctx, variable, caller);
                }
                catch (Exception e)
                {
                    PhpException.Throw(PhpError.Notice, LibResources.GetString("serialization_failed", e.Message));
                    return null;
                }
            }

            protected abstract PhpString CommonSerialize(Context ctx, PhpValue variable, RuntimeTypeHandle caller);

            /// <summary>
            /// Deserializes a graph of connected object from a byte array using a given formatter.
            /// </summary>
            /// <param name="ctx">Runtime context.</param>
            /// <param name="data">The string to deserialize the graph from.</param>
            /// <param name="caller">Caller's class context.</param>
            /// <returns>The deserialized object graph or <B>false</B> on error.</returns>
            public PhpValue Deserialize(Context ctx, PhpString data, RuntimeTypeHandle caller /*, allowed_classes */)
            {
                var stream = new MemoryStream(data.ToBytes(ctx));

                try
                {
                    return CommonDeserialize(ctx, stream, caller);
                }
                catch (Exception e)
                {
                    PhpException.Throw(PhpError.Notice, LibResources.GetString("deserialization_failed", e.Message, stream.Position, stream.Length));
                    return PhpValue.False;
                }
            }

            protected abstract PhpValue CommonDeserialize(Context ctx, Stream data, RuntimeTypeHandle caller);
        }

        #endregion

        #region PhpSerializer

        internal sealed class PhpSerializer : Serializer
        {
            #region Tokens

            /// <summary>
            /// Contains definition of (one-character) tokens that constitute PHP serialized data.
            /// </summary>
            internal class Tokens
            {
                public const char BraceOpen = '{';
                public const char BraceClose = '}';
                public const char Colon = ':';
                public const char Semicolon = ';';
                public const char Quote = '"';

                public const char Null = 'N';
                public const char Boolean = 'b';
                public const char Integer = 'i';
                public const char Double = 'd';
                public const char String = 's';
                public const char Array = 'a';
                public const char Object = 'O'; // instance of a class that does not implement SPL.Serializable
                public const char ObjectSer = 'C'; // instance of a class that implements SPL.Serializable
                
                public const char Reference = 'R'; // &-like reference
                public const char ObjectRef = 'r'; // same instance reference (PHP5 object semantics)
            }

            #endregion

            #region ObjectWriter

            sealed class ObjectWriter : PhpVariableVisitor
            {
                /// <summary>
                /// Object ID counter used by the <B>r</B> and <B>R</B> tokens.
                /// </summary>
                int sequenceNumber;

                /// <summary>
                /// Maintains a sequence number for every <see cref="object"/> and <see cref="PhpAlias"/> that have already been serialized.
                /// </summary>
                Dictionary<object, int> serializedRefs { get { return _serializedRefs ?? (_serializedRefs = new Dictionary<object, int>()); } }
                Dictionary<object, int> _serializedRefs;

                Encoding Encoding => Encoding.UTF8;

                /// <summary>
                /// Result data.
                /// </summary>
                readonly PhpString _result = new PhpString();

                void Write(char ch) => Write(ch.ToString());
                void Write(string str) => _result.Append(str);
                void Write(byte[] bytes) => _result.Append(bytes);

                private ObjectWriter() { }

                public static PhpString Serialize(PhpValue variable)
                {
                    ObjectWriter writer;
                    variable.Accept(writer = new ObjectWriter());
                    return writer._result;
                }

                public override void AcceptNull()
                {
                    Write(Tokens.Null);
                    Write(Tokens.Semicolon);
                }

                public override void Accept(bool value)
                {
                    Write(Tokens.Boolean);
                    Write(Tokens.Colon);
                    Write(value ? '1' : '0');
                    Write(Tokens.Semicolon);
                }

                public override void Accept(long value)
                {
                    Write(Tokens.Integer);
                    Write(Tokens.Colon);
                    Write(value.ToString());
                    Write(Tokens.Semicolon);
                }

                public override void Accept(double value)
                {
                    Write(Tokens.Double);
                    Write(Tokens.Colon);

                    // handle NaN, +Inf, -Inf
                    if (double.IsNaN(value)) Write("NAN");
                    else if (double.IsPositiveInfinity(value)) Write("INF");
                    else if (double.IsNegativeInfinity(value)) Write("-INF");
                    else Write(value.ToString("R", NumberFormatInfo.InvariantInfo));

                    Write(Tokens.Semicolon);
                }

                void Accept(byte[] bytes)
                {
                    Write(Tokens.String);
                    Write(Tokens.Colon);
                    Write(bytes.Length.ToString());
                    Write(Tokens.Colon);
                    Write(Tokens.Quote);
                    Write(bytes);
                    Write(Tokens.Quote);
                    Write(Tokens.Semicolon);
                }

                public override void Accept(string value)
                {
                    Accept(this.Encoding.GetBytes(value));
                }

                public override void Accept(PhpString obj)
                {
                    Accept(obj.ToBytes(this.Encoding));
                }

                public override void Accept(PhpArray value)
                {
                    serializedRefs[value] = sequenceNumber;

                    Write(Tokens.Array);
                    Write(Tokens.Colon);
                    Write(value.Count.ToString());
                    Write(Tokens.Colon);
                    Write(Tokens.BraceOpen);

                    // write out array items in the correct order
                    base.Accept(value);

                    Write(Tokens.BraceClose);
                }

                public override void AcceptArrayItem(KeyValuePair<IntStringKey, PhpValue> entry)
                {
                    // key
                    if (entry.Key.IsInteger)
                        Accept(entry.Key.Integer);
                    else
                        Accept(entry.Key.String);

                    // value
                    sequenceNumber--;   // don't assign a seq number to array keys
                    entry.Value.Accept(this);
                }

                public override void Accept(PhpAlias value)
                {
                    sequenceNumber--;
                    if (value.ReferenceCount == 0)
                    {
                        value.Value.Accept(this);
                    }
                    else
                    {
                        int seq;
                        if (serializedRefs.TryGetValue(value, out seq))
                        {
                            // this reference has already been serialized -> write out its seq. number
                            Write(Tokens.Reference);
                            Write(Tokens.Colon);
                            Write(seq.ToString());
                            Write(Tokens.Semicolon);
                        }
                        else
                        {
                            serializedRefs.Add(value, sequenceNumber + 1);
                            value.Value.Accept(this);
                        }
                    }
                }

                public override void AcceptObject(object value)
                {
                    if (value is PhpResource)
                    {
                        // resources are serialized as 0
                        Accept(0L);
                    }
                    else
                    {
                        int seq;
                        if (serializedRefs.TryGetValue(value, out seq))
                        {
                            // this object instance has already been serialized -> write out its seq. number
                            Write(Tokens.ObjectRef);
                            Write(Tokens.Colon);
                            Write(seq.ToString());
                            Write(Tokens.Semicolon);
                            sequenceNumber--;
                        }
                        else
                        {
                            serializedRefs.Add(value, sequenceNumber);
                            throw new NotImplementedException();
                        }
                    }
                }
            }

            #endregion

            #region ObjectReader

            sealed class ObjectReader
            {
                readonly Context _ctx;
                readonly Stream _stream;
                readonly RuntimeTypeHandle _caller;

                public ObjectReader(Context ctx, Stream stream, RuntimeTypeHandle caller)
                {
                    Debug.Assert(ctx != null);
                    Debug.Assert(stream != null);

                    _ctx = ctx;
                    _stream = stream;
                    _caller = caller;
                }

                #region Throw helpers

                /// <summary>
                /// Throws a <see cref="SerializationException"/> due to an unexpected character.
                /// </summary>
                private void ThrowUnexpected()
                {
                    throw new InvalidDataException(LibResources.unexpected_character_in_stream);
                }

                /// <summary>
                /// Throws a <see cref="SerializationException"/> due to an unexpected end of stream.
                /// </summary>
                private void ThrowEndOfStream()
                {
                    throw new InvalidDataException(LibResources.unexpected_end_of_stream);
                }

                /// <summary>
                /// Throws a <see cref="SerializationException"/> due to an data type.
                /// </summary>
                private void ThrowInvalidDataType()
                {
                    throw new InvalidDataException(LibResources.invalid_data_bad_type);
                }

                /// <summary>
                /// Throws a <see cref="SerializationException"/> due to an invalid length marker.
                /// </summary>
                private void ThrowInvalidLength()
                {
                    throw new InvalidDataException(LibResources.invalid_data_bad_length);
                }

                /// <summary>
                /// Throws a <see cref="SerializationException"/> due to an invalid back-reference.
                /// </summary>
                private void ThrowInvalidReference()
                {
                    throw new InvalidDataException(LibResources.invalid_data_bad_back_reference);
                }

                #endregion

                #region Utils

                /// <summary>
                /// Quickly check if the look ahead byte is digit. Assumes the value is in range 0x00 - 0xff.
                /// </summary>
                /// <param name="ch">The byte value.</param>
                /// <returns>True if value is in range '0'-'9'.</returns>
                static bool IsDigit(char ch)
                {
                    return Digit(ch) != -1;
                }

                /// <summary>
                /// Quickly determine the numeric value of given <paramref name="ch"/> byte.
                /// </summary>
                /// <param name="ch">The byte value.</param>
                /// <returns>Digit value or <c>-1</c> if <paramref name="ch"/> is not a digit.</returns>
                static int Digit(char ch)
                {
                    int num = unchecked((int)ch - (int)'0');
                    return (num >= 0 && num <= 9) ? num : -1;
                }

                /// <summary>
                /// Temporarily used <see cref="StringBuilder"/>. Remember it to save GC.
                /// This method always returns the same instance of <see cref="StringBuilder"/>, it will always reset its <see cref="StringBuilder.Length"/> to <c>0</c>.
                /// </summary>
                StringBuilder/*!*/GetTemporaryStringBuilder(int initialCapacity)
                {
                    var tmp = _tmpStringBuilder;

                    if (tmp != null)
                    {
                        tmp.Length = 0;
                    }
                    else
                    {
                        _tmpStringBuilder = tmp = new StringBuilder(initialCapacity, int.MaxValue);
                    }

                    return tmp;
                }
                StringBuilder _tmpStringBuilder;

                #endregion

                #region Consume

                /// <summary>
                /// Consumes the look ahead character and moves to the next character in the input stream.
                /// </summary>
                /// <returns>The old (consumed) look ahead character.</returns>
                /// <remarks>The consumed value is 8-bit, always in range 0x00 - 0xff.</remarks>
                char Consume()
                {
                    var b = _stream.ReadByte();
                    if (b == -1)
                    {
                        ThrowEndOfStream();
                    }

                    return (char)b;
                }

                /// <summary>
                /// Consumes a given look ahead character and moves to the next character in the input stream.
                /// </summary>
                /// <param name="expected">The character that should be consumed.</param>
                /// <remarks>If <paramref name="expected"/> does not match current look ahead character, <see cref="ThrowUnexpected"/> is called.</remarks>
                void Consume(char expected)
                {
                    var ch = Consume();
                    if (ch != expected)
                    {
                        ThrowUnexpected();
                    }
                }

                #endregion

                /// <summary>
                /// Reads a signed 64-bit integer number from the <see cref="_stream"/>.
                /// </summary>
                /// <returns>The integer.</returns>
                long ReadInteger()
                {
                    // pattern:
                    // [+-]?[0-9]+

                    long number = 0;

                    // [+-]?
                    var ch = Consume();
                    bool minus;
                    if (ch == '-')
                    {
                        minus = true;
                        ch = Consume();
                    }
                    else if (ch == '+')
                    {
                        minus = false;
                        ch = Consume();
                    }
                    else
                    {
                        minus = false;
                    }

                    // [0-9]+

                    int digit = Digit(ch);
                    if (digit == -1)
                    {
                        ThrowUnexpected();
                    }

                    do
                    {
                        // let it overflow just as PHP does
                        number = unchecked((10 * number) + digit);
                        ch = Consume();

                    } while ((digit = Digit(ch)) != -1);

                    // seek one back
                    _stream.Seek(-1, SeekOrigin.Current);

                    //
                    return minus ? unchecked(-number) : number;
                }

                /// <summary>
                /// Deserializes object from given stream.
                /// </summary>
                public PhpValue Deserialize() => Parse();

                PhpValue Parse()
                {
                    switch (Consume())
                    {
                        case Tokens.Null: ParseNull(); return PhpValue.Null;
                        case Tokens.Boolean: return PhpValue.Create(ParseBoolean());
                        case Tokens.Integer: return PhpValue.Create(ParseInteger());
                        case Tokens.Double: return PhpValue.Create(ParseDouble());
                        case Tokens.String: return ParseString();
                        case Tokens.Array: return PhpValue.Create(ParseArray());
                        case Tokens.Object: throw new NotImplementedException();
                        case Tokens.ObjectSer: throw new NotImplementedException();
                        case Tokens.Reference: throw new NotImplementedException();
                        case Tokens.ObjectRef: throw new NotImplementedException();

                        default:
                            ThrowUnexpected();
                            throw new ArgumentException();  // unreachable
                    }
                }

                void ParseNull()
                {
                    Consume(Tokens.Semicolon);
                }

                /// <summary>
                /// Parses the <B>b</B> token.
                /// </summary>
                bool ParseBoolean()
                {
                    bool value = false;

                    Consume(Tokens.Colon);
                    switch (Consume())
                    {
                        case '0': break;
                        case '1': value = true; break;
                        default: ThrowUnexpected(); break;
                    }
                    Consume(Tokens.Semicolon);

                    //
                    return value;
                }

                /// <summary>
                /// Parses the <B>i</B> token.
                /// </summary>
                long ParseInteger()
                {
                    Consume(Tokens.Colon);
                    long i = ReadInteger();                    
                    Consume(Tokens.Semicolon);

                    //
                    return i;
                }

                /// <summary>
                /// Parses the <B>d</B> token.
                /// </summary>
                double ParseDouble()
                {
                    Consume(Tokens.Colon);

                    // pattern:
                    // NAN
                    // [+-]INF
                    // [+-]?[0-9]*[.]?[0-9]*([eE][+-]?[0-9]+)?

                    var ch = Consume();

                    // NaN
                    if (ch == 'N')
                    {
                        Consume('A');
                        Consume('N');
                        return Double.NaN;
                    }

                    // mantissa + / -
                    int sign = 1;
                    if (ch == '+') ch = Consume();
                    else if (ch == '-')
                    {
                        sign = -1;
                        ch = Consume();
                    }

                    // Infinity
                    if (ch == 'I')
                    {
                        Consume('N');
                        Consume('F');
                        return sign > 0 ? double.PositiveInfinity : double.NegativeInfinity;
                    }

                    // reconstruct the number:
                    var number = GetTemporaryStringBuilder(16);
                    if (sign < 0)
                    {
                        number.Append('-');
                    }

                    // [^;]*;
                    while (Tokens.Semicolon != ch)
                    {
                        number.Append(ch);
                        ch = Consume();
                    }

                    double result;
                    if (!double.TryParse(number.ToString(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result))
                    {
                        ThrowUnexpected();
                    }

                    return result;
                }

                PhpValue ParseString()
                {
                    // :
                    Consume(Tokens.Colon);

                    // <length>
                    int length = (unchecked((int)ReadInteger()));
                    if (length < 0) ThrowInvalidLength();

                    if (length != 0)
                    {
                        var bytes = new byte[length];

                        // :"<bytes>";
                        Consume(Tokens.Colon);
                        Consume(Tokens.Quote);
                        if (_stream.Read(bytes, 0, length) != length)
                        {
                            ThrowEndOfStream();
                        }
                        Consume(Tokens.Quote);
                        Consume(Tokens.Semicolon);

                        //
                        try
                        {
                            // unicode string
                            return PhpValue.Create(_ctx.StringEncoding.GetString(bytes));
                        }
                        catch (DecoderFallbackException)
                        {
                            // binary string
                            return PhpValue.Create(new PhpString(bytes));
                        }
                    }
                    else
                    {
                        // :"";
                        Consume(Tokens.Colon);
                        Consume(Tokens.Quote);
                        Consume(Tokens.Quote);
                        Consume(Tokens.Semicolon);

                        //
                        return PhpValue.Create(string.Empty);
                    }
                }

                PhpArray ParseArray()
                {
                    Consume(Tokens.Colon);
                    int length = unchecked((int)ReadInteger());
                    if (length < 0) ThrowInvalidLength();

                    var arr = (length == 0) ? PhpArray.NewEmpty() : new PhpArray(length);

                    Consume(Tokens.Colon);
                    Consume(Tokens.BraceOpen);

                    while (length-- > 0)
                    {
                        var key = Parse();
                        var value = Parse();

                        //
                        arr.Add(key.ToIntStringKey(), value);
                    }
                    
                    Consume(Tokens.BraceClose);

                    //
                    return arr;
                }
            }

            #endregion

            public static readonly PhpSerializer Instance = new PhpSerializer();

            public override string Name => "php";

            protected override PhpString CommonSerialize(Context ctx, PhpValue variable, RuntimeTypeHandle caller)
            {
                return ObjectWriter.Serialize(variable);
            }

            protected override PhpValue CommonDeserialize(Context ctx, Stream stream, RuntimeTypeHandle caller)
            {
                return new ObjectReader(ctx, stream, caller).Deserialize();
            }
        }

        #endregion

        #region serialize, unserialize

        /// <summary>
        /// Generates a storable representation of a value.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="caller">Caller class context.</param>
        /// <param name="value">The value to be serialized.</param>
        /// <returns></returns>
        public static PhpString serialize(Context ctx, [ImportCallerClass]RuntimeTypeHandle caller, PhpValue value)
        {
            return PhpSerializer.Instance.Serialize(ctx, value, caller);
        }

        /// <summary>
        /// Creates a PHP value from a stored representation.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="caller">Class context.</param>
        /// <param name="str">The serialized string.</param>
        /// <param name="options">Any options to be provided to unserialize(), as an associative array.</param>
        /// <returns>
        /// The converted value is returned, and can be a boolean, integer, float, string, array or object.
        /// In case the passed string is not unserializeable, <c>FALSE</c> is returned and <b>E_NOTICE</b> is issued.
        /// </returns>
        public static PhpValue unserialize(Context ctx, [ImportCallerClass]RuntimeTypeHandle caller, PhpString str, PhpArray options = null)
        {
            return PhpSerializer.Instance.Deserialize(ctx, str, caller);
        }

        #endregion

    }
}
