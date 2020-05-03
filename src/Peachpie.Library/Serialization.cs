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
using System.Reflection;

namespace Pchp.Library
{
    [PhpExtension("standard")]
    public static partial class PhpSerialization
    {
        #region Serializer

        /// <summary>
        /// Abstract serializer providing extension points for PHP value serialization and deserialization.
        /// </summary>
        public abstract class Serializer
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
                catch (Spl.Exception)
                {
                    throw;
                }
                catch (Exception e)
                {
                    PhpException.Throw(PhpError.Notice, LibResources.serialization_failed, e.Message);
                    return default(PhpString);
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
                catch (Spl.Exception)
                {
                    throw;
                }
                catch (Exception e)
                {
                    PhpException.Throw(PhpError.Notice, LibResources.deserialization_failed, e.Message, stream.Position.ToString(), stream.Length.ToString());
                    return PhpValue.False;
                }
            }

            protected abstract PhpValue CommonDeserialize(Context ctx, Stream data, RuntimeTypeHandle caller);

            /// <summary>
            /// Sets property value dynamically on the newly instantiated object.
            /// </summary>
            protected static void SetProperty(object/*!*/ instance, PhpTypeInfo tinfo, string/*!*/ name, PhpValue value, Context/*!*/ context)
            {
                // the property name might encode its visibility and "classification" -> use these
                // information for suitable property desc lookups
                var property_name = Serialization.ParseSerializedPropertyName(name, out var type_name, out var visibility);

                var declarer = (type_name == null)
                    ? tinfo
                    : context.GetDeclaredType(type_name, true) ?? tinfo;

                // try to find a suitable property
                var property = tinfo.GetDeclaredProperty(property_name);
                if (property != null && !property.IsStatic && property.IsVisible(declarer.Type.AsType()))
                {
                    if (property.IsPrivate && declarer != property.ContainingType)
                    {
                        // if certain conditions are met, don't use the handle even if it was found
                        // (this is to precisely mimic the PHP behavior)
                        property = null;
                    }
                }

                if (property != null)
                {
                    property.SetValue(context, instance, value);
                }
                else if (tinfo.RuntimeFieldsHolder != null)
                {
                    // suitable CT field not found -> add it to RT fields
                    // (note: care must be taken so that the serialize(unserialize($x)) round
                    // trip returns $x even if user classes specified in $x are not declared)
                    var runtime_fields = tinfo.GetRuntimeFields(instance);
                    if (runtime_fields == null)
                    {
                        runtime_fields = new PhpArray(1);
                        tinfo.RuntimeFieldsHolder.SetValue(instance, runtime_fields);
                    }
                    runtime_fields[name] = value;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }

        #endregion

        #region PhpSerializer

        /// <summary>
        /// <see cref="Serializer"/> implementation providing legacy PHP serialization and deserialization.
        /// </summary>
        public sealed class PhpSerializer : Serializer
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
                int _seq;

                /// <summary>
                /// Maintains a sequence number for every <see cref="object"/> and <see cref="PhpAlias"/> that have already been serialized.
                /// </summary>
                Dictionary<object, int> serializedRefs { get { return _serializedRefs ?? (_serializedRefs = new Dictionary<object, int>()); } }
                Dictionary<object, int> _serializedRefs;

                Encoding Encoding => _ctx.StringEncoding;

                /// <summary>
                /// Result data.
                /// </summary>
                readonly PhpString.Blob _result = new PhpString.Blob();

                readonly Context _ctx;
                readonly RuntimeTypeHandle _caller;

                void Write(char ch) => Write(ch.ToString());
                void Write(string str) => _result.Add(str);
                void Write(byte[] bytes) => _result.Add(bytes);
                void Write(PhpString str)
                {
                    if (!str.IsEmpty)
                    {
                        if (str.ContainsBinaryData)
                            Write(str.ToBytes(Encoding));
                        else
                            Write(str.ToString(Encoding));
                    }
                }

                private ObjectWriter(Context ctx, RuntimeTypeHandle caller)
                {
                    Debug.Assert(ctx != null);
                    _ctx = ctx;
                    _caller = caller;
                }

                public static PhpString Serialize(Context ctx, PhpValue variable, RuntimeTypeHandle caller)
                {
                    ObjectWriter writer;
                    variable.Accept(writer = new ObjectWriter(ctx, caller));
                    return new PhpString(writer._result);
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
                    if (value == null)
                    {
                        AcceptNull();
                    }
                    else
                    {
                        Accept(this.Encoding.GetBytes(value));
                    }
                }

                public override void Accept(PhpString obj)
                {
                    Accept(obj.ToBytes(this.Encoding));
                }

                public override void Accept(PhpArray value)
                {
                    ++_seq;

                    if (serializedRefs.TryGetValue(value, out var seq))
                    {
                        // this shouldn't happen
                        // an array was referenced twice without being enclosed within the same alias (PhpAlias)

                        Debug.WriteLine("Multiple references to the same array instance!"); // harmless issue, handled as a regular alias

                        // this reference has already been serialized -> write out its seq. number
                        Write(Tokens.Reference);
                        Write(Tokens.Colon);
                        Write(seq.ToString());
                        Write(Tokens.Semicolon);
                    }
                    else
                    {
                        serializedRefs[value] = _seq;

                        Write(Tokens.Array);
                        Write(Tokens.Colon);
                        Write(value.Count.ToString());
                        Write(Tokens.Colon);
                        Write(Tokens.BraceOpen);

                        // write out array items in the correct order
                        base.Accept(value);

                        Write(Tokens.BraceClose);
                    }
                }

                public override void AcceptArrayItem(KeyValuePair<IntStringKey, PhpValue> entry)
                {
                    // key
                    if (entry.Key.IsInteger)
                        Accept(entry.Key.Integer);
                    else
                        Accept(entry.Key.String);

                    // value
                    entry.Value.Accept(this);
                }

                public override void Accept(PhpAlias value)
                {
                    ++_seq;

                    if (value.ReferenceCount == 0)
                    {
                        value.Value.Accept(this);
                    }
                    else
                    {
                        if (serializedRefs.TryGetValue(value, out var seq))
                        {
                            // this reference has already been serialized -> write out its seq. number
                            Write(Tokens.Reference);
                            Write(Tokens.Colon);
                            Write(seq.ToString());
                            Write(Tokens.Semicolon);
                        }
                        else
                        {
                            serializedRefs[value] = _seq;
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
                        ++_seq;

                        int seq;
                        if (serializedRefs.TryGetValue(value, out seq))
                        {
                            // this object instance has already been serialized -> write out its seq. number
                            Write(Tokens.ObjectRef);
                            Write(Tokens.Colon);
                            Write(seq.ToString());
                            Write(Tokens.Semicolon);
                        }
                        else
                        {
                            serializedRefs[value] = _seq;
                            SerializeObject(value);
                        }
                    }
                }

                void SerializeObject(object obj)
                {
                    Debug.Assert(obj != null);

                    var tinfo = obj.GetPhpTypeInfo();
                    var classname = tinfo.Name;

                    if (obj is __PHP_Incomplete_Class)
                    {
                        // classname = ((__PHP_Incomplete_Class)obj).ClassName ?? classname;
                        throw new NotImplementedException("__PHP_Incomplete_Class");
                    }

                    var classnameBytes = Encoding.GetBytes(classname);

                    PhpArray serializedArray = null;
                    byte[] serializedBytes = null;
                    List<KeyValuePair<string, PhpValue>> serializedProperties = null;

                    var __serialize = tinfo.RuntimeMethods[TypeMethods.MagicMethods.__serialize];
                    if (__serialize != null)
                    {
                        // TODO: check accessibility // CONSIDER do not reflect non-public methods in tinfo.RuntimeMethods at all!
                        var rvalue = __serialize.Invoke(_ctx, obj);
                        if (rvalue.IsPhpArray(out serializedArray) == false)
                        {
                            // FATAL ERROR
                            // {0}::__serialize must return an array
                            throw PhpException.TypeErrorException(string.Format(LibResources.__serialize_must_return_array, tinfo.Name));
                        }
                    }
                    else if (obj is global::Serializable serializable)
                    {
                        var res = serializable.serialize();
                        if (res.IsDefault)
                        {
                            AcceptNull();
                            return;
                        }

                        serializedBytes = res.ToBytes(Encoding);

                        //if (resdata == null)
                        //{
                        //    // serialize did not return NULL nor a string -> throw an exception
                        //    SPL.Exception.ThrowSplException(
                        //        _ctx => new SPL.Exception(_ctx, true),
                        //        context,
                        //        string.Format(CoreResources.serialize_must_return_null_or_string, value.TypeName), 0, null);
                        //}
                    }
                    else
                    {
                        // try to call the __sleep method
                        // otherwise list object properties

                        var __sleep = tinfo.RuntimeMethods[TypeMethods.MagicMethods.__sleep];
                        // TODO: __sleep accessibility -> ThrowMethodVisibilityError
                        if (__sleep != null)
                        {
                            var sleep_result = __sleep.Invoke(_ctx, obj).ArrayOrNull();
                            if (sleep_result == null)
                            {
                                PhpException.Throw(PhpError.Notice, Core.Resources.ErrResources.sleep_must_return_array);
                                AcceptNull();
                                return;
                            }

                            serializedProperties = EnumerateSerializableProperties(obj, tinfo, sleep_result).ToList();
                        }
                        else
                        {
                            serializedProperties = Serialization.EnumerateSerializableProperties(obj, tinfo).ToList();
                        }
                    }

                    Write((serializedBytes == null) ? Tokens.Object : Tokens.ObjectSer);
                    Write(Tokens.Colon);

                    // write out class name
                    Write(classnameBytes.Length.ToString());
                    Write(Tokens.Colon);
                    Write(Tokens.Quote);

                    Write(classnameBytes);
                    Write(Tokens.Quote);
                    Write(Tokens.Colon);

                    if (serializedBytes != null)
                    {
                        Debug.Assert(serializedProperties == null);

                        // write out the result of serialize
                        Write(serializedBytes.Length.ToString());
                        Write(Tokens.Colon);
                        Write(Tokens.BraceOpen);

                        // write serialized data
                        Write(serializedBytes);
                    }
                    else if (serializedArray != null)
                    {
                        // write out property count
                        Write(serializedArray.Count.ToString());
                        Write(Tokens.Colon);
                        Write(Tokens.BraceOpen);

                        // enumerate array items and serialize them
                        base.Accept(serializedArray);
                    }
                    else
                    {
                        // write out property count
                        Write(serializedProperties.Count.ToString());
                        Write(Tokens.Colon);
                        Write(Tokens.BraceOpen);

                        // write out properties
                        AcceptObjectProperties(serializedProperties);
                    }

                    // }
                    Write(Tokens.BraceClose);
                }

                IEnumerable<KeyValuePair<string, PhpValue>> EnumerateSerializableProperties(object obj, PhpTypeInfo tinfo, PhpArray properties)
                {
                    Debug.Assert(obj != null);
                    Debug.Assert(tinfo != null);
                    Debug.Assert(properties != null);

                    PhpArray runtime_fields = null;

                    var enumerator = properties.GetFastEnumerator();
                    while (enumerator.MoveNext())
                    {
                        FieldAttributes visibility;
                        string name = enumerator.CurrentValue.ToStringOrThrow(_ctx);
                        string declaring_type_name;
                        string property_name = Serialization.ParseSerializedPropertyName(name, out declaring_type_name, out visibility);

                        PhpTypeInfo declarer;   // for visibility check
                        if (declaring_type_name == null)
                        {
                            declarer = tinfo;
                        }
                        else
                        {
                            declarer = _ctx.GetDeclaredType(declaring_type_name);
                            if (declarer == null)
                            {
                                // property name refers to an unknown class -> value will be null
                                yield return new KeyValuePair<string, PhpValue>(name, PhpValue.Null);
                                continue;
                            }
                        }

                        // obtain the property desc and decorate the prop name according to its visibility and declaring class
                        var property = tinfo.GetDeclaredProperty(property_name);
                        if (property != null && !property.IsStatic && property.IsVisible(declarer.Type))
                        {
                            // if certain conditions are met, serialize the property as null
                            // (this is to precisely mimic the PHP behavior)
                            if ((visibility == (property.Attributes & FieldAttributes.FieldAccessMask) && visibility != FieldAttributes.Public) ||
                                (visibility == FieldAttributes.Private && declarer != property.ContainingType))
                            {
                                yield return new KeyValuePair<string, PhpValue>(name, PhpValue.Null);
                                continue;
                            }

                            name = Serialization.FormatSerializedPropertyName(property);
                        }
                        else
                        {
                            property = null; // field is not visible, try runtime fields
                        }

                        // obtain the property value
                        PhpValue val;

                        if (property != null)
                        {
                            val = property.GetValue(_ctx, obj);
                        }
                        else
                        {
                            if (runtime_fields == null)
                            {
                                runtime_fields = tinfo.GetRuntimeFields(obj) ?? PhpArray.Empty;
                            }

                            if (!runtime_fields.TryGetValue(name, out val))
                            {
                                // PHP 5.1+
                                PhpException.Throw(PhpError.Notice, string.Format(Core.Resources.ErrResources.sleep_returned_bad_field, name));
                            }
                        }

                        yield return new KeyValuePair<string, PhpValue>(name, val);
                    }
                }

                void AcceptObjectProperties(IEnumerable<KeyValuePair<string, PhpValue>> properties)
                {
                    foreach (var pair in properties)
                    {
                        // write out the property name and the property value
                        Accept(pair.Key);
                        Accept(pair.Value);
                    }
                }
            }

            #endregion

            #region ObjectReader

            internal sealed class ObjectReader
            {
                readonly Context _ctx;
                readonly Encoding _encoding;
                readonly Stream _stream;
                readonly RuntimeTypeHandle _caller;

                /// <summary>
                /// Deserialized objects map.
                /// </summary>
                Dictionary<int, PhpAlias> _lazyObjects;

                PhpAlias/*!*/AddSeq()
                {
                    if (_lazyObjects == null)
                    {
                        _lazyObjects = new Dictionary<int, PhpAlias>();
                    }

                    return (_lazyObjects[_lazyObjects.Count + 1] = new PhpAlias(PhpValue.Null));
                }

                public ObjectReader(Context ctx, Stream stream, RuntimeTypeHandle caller)
                    : this(ctx, ctx.StringEncoding, stream, caller)
                {
                }

                public ObjectReader(Context ctx, Encoding encoding, Stream stream, RuntimeTypeHandle caller)
                {
                    Debug.Assert(encoding != null);
                    Debug.Assert(stream != null);

                    _ctx = ctx;
                    _encoding = encoding;
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
                private Exception InvalidReferenceException()
                {
                    return new InvalidDataException(LibResources.invalid_data_bad_back_reference);
                }

                #endregion

                #region Utils

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
                /// Reads serialized string data in form <c>{length}:"{bytes}"</c>
                /// </summary>
                /// <returns></returns>
                PhpValue ReadString()
                {
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

                        //
                        try
                        {
                            // unicode string
                            return PhpValue.Create(_encoding.GetString(bytes));
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

                        //
                        return PhpValue.Create(string.Empty);
                    }
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
                        case Tokens.Object: return PhpValue.FromClass(ParseObject(false));
                        case Tokens.ObjectSer: return PhpValue.FromClass(ParseObject(true));
                        case Tokens.Reference: return ParseObjectRef();
                        case Tokens.ObjectRef: return ParseObjectRef();

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

                    // <length>:"string"
                    var str = ReadString();

                    // ;
                    Consume(Tokens.Semicolon);

                    return str;
                }

                PhpArray ParseArray()
                {
                    var seq = AddSeq();

                    Consume(Tokens.Colon);
                    int length = unchecked((int)ReadInteger());
                    if (length < 0) ThrowInvalidLength();

                    var arr = new PhpArray(length);

                    Consume(Tokens.Colon);
                    Consume(Tokens.BraceOpen);

                    while (length-- > 0)
                    {
                        var key = Parse();
                        var value = Parse();

                        //
                        if (key.TryToIntStringKey(out var iskey))
                        {
                            arr.Add(iskey, value);
                        }
                        else
                        {
                            this.ThrowInvalidDataType();
                        }
                    }

                    Consume(Tokens.BraceClose);

                    //
                    seq.Value = arr;
                    return arr;
                }

                /// <summary>
                /// Parses the <B>O</B> and <B>C</B> tokens.
                /// </summary>
                /// <param name="serializable">If <B>true</B>, the last token eaten was <B>C</B>, otherwise <B>O</B>.</param>
                object ParseObject(bool serializable)
                {
                    Debug.Assert(_ctx != null);

                    var seq = AddSeq();

                    // :{length}:"{classname}":
                    Consume(Tokens.Colon);  // :
                    string class_name = ReadString().AsString();   // <length>:"classname"
                    var tinfo = _ctx?.GetDeclaredType(class_name, true);

                    // :{count}:
                    Consume(Tokens.Colon);  // :
                    var count = (unchecked((int)ReadInteger()));
                    if (count < 0) ThrowInvalidLength();
                    Consume(Tokens.Colon);

                    // bind to the specified class
                    object obj;
                    if (tinfo != null)
                    {
                        obj = tinfo.CreateUninitializedInstance(_ctx);
                        if (obj == null)
                        {
                            throw new ArgumentException(string.Format(LibResources.class_instantiation_failed, class_name));
                        }
                    }
                    else
                    {
                        // TODO: DeserializationCallback
                        // __PHP_Incomplete_Class
                        obj = new __PHP_Incomplete_Class();
                        throw new NotImplementedException("__PHP_Incomplete_Class");
                    }

                    // {
                    Consume(Tokens.BraceOpen);

                    if (serializable)
                    {
                        // check whether the instance is PHP5.1 Serializable
                        if (!(obj is global::Serializable))
                        {
                            throw new ArgumentException(string.Format(LibResources.class_has_no_unserializer, class_name));
                        }

                        PhpString serializedBytes;
                        if (count > 0)
                        {
                            // add serialized representation to be later passed to unserialize
                            var buffer = new byte[count];
                            if (_stream.Read(buffer, 0, count) < count)
                            {
                                ThrowEndOfStream();
                            }

                            serializedBytes = new PhpString(buffer);
                        }
                        else
                        {
                            serializedBytes = PhpString.Empty;
                        }

                        // Template: Serializable::unserialize(data)
                        ((global::Serializable)obj).unserialize(serializedBytes);
                    }
                    else
                    {
                        var __unserialize = tinfo.RuntimeMethods[TypeMethods.MagicMethods.__unserialize];
                        var __unserialize_array = __unserialize != null ? new PhpArray(count) : null;

                        // parse properties
                        while (--count >= 0)
                        {
                            var key = Parse();
                            var value = Parse();

                            //
                            if (key.TryToIntStringKey(out var iskey))
                            {
                                if (__unserialize_array != null)
                                {
                                    __unserialize_array[iskey] = value;
                                }
                                else
                                {
                                    // set property
                                    SetProperty(obj, tinfo, iskey.ToString(), value, _ctx);
                                }
                            }
                            else
                            {
                                this.ThrowInvalidDataType();
                            }
                        }

                        if (__unserialize != null)
                        {
                            __unserialize.Invoke(_ctx, obj, __unserialize_array);
                        }
                        else
                        {
                            // __wakeup
                            var __wakeup = tinfo.RuntimeMethods[TypeMethods.MagicMethods.__wakeup];
                            if (__wakeup != null)
                            {
                                __wakeup.Invoke(_ctx, obj);
                            }
                        }
                    }

                    // }
                    Consume(Tokens.BraceClose);

                    //
                    seq.Value = PhpValue.FromClass(obj);
                    return obj;
                }

                PhpAlias ParseObjectRef()
                {
                    var seq = AddSeq();

                    Consume(Tokens.Colon);

                    var seqref = (int)ReadInteger();
                    if (_lazyObjects == null || !_lazyObjects.TryGetValue(seqref, out var alias))
                    {
                        throw InvalidReferenceException();
                    }

                    Consume(Tokens.Semicolon);

                    //
                    return alias;
                }
            }

            #endregion

            public static readonly PhpSerializer Instance = new PhpSerializer();

            public override string Name => "php";

            protected override PhpString CommonSerialize(Context ctx, PhpValue variable, RuntimeTypeHandle caller)
            {
                return ObjectWriter.Serialize(ctx, variable, caller);
            }

            protected override PhpValue CommonDeserialize(Context ctx, Stream stream, RuntimeTypeHandle caller)
            {
                return new ObjectReader(ctx, stream, caller).Deserialize();
            }

            /// <summary>
            /// Tries to deserialize the data.
            /// </summary>
            /// <returns>Retrns <c>true</c> iff the data was deserialized properly.</returns>
            public bool TryDeserialize(Context ctx, byte[] data, out PhpValue value)
            {
                try
                {
                    value = CommonDeserialize(ctx, new MemoryStream(data), default);
                }
                catch
                {
                    value = PhpValue.False;
                    return false;
                }

                //
                return true;
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
        public static PhpString serialize(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle caller, PhpValue value)
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
        public static PhpValue unserialize(Context ctx, [ImportValue(ImportValueAttribute.ValueSpec.CallerClass)]RuntimeTypeHandle caller, PhpString str, PhpArray options = null)
        {
            return PhpSerializer.Instance.Deserialize(ctx, str, caller);
        }

        #endregion
    }
}
