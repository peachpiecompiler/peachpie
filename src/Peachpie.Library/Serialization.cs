using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using Pchp.Core.Reflection;
using System.Text;
using System.Globalization;

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
                catch (Exception)
                {
                    //PhpException.Throw(PhpError.Notice, LibResources.GetString("serialization_failed", e.Message));
                    //return null;
                    throw;  // TODO: Err
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
                try
                {
                    return CommonDeserialize(ctx, data, caller);
                }
                catch (Exception)
                {
                    //PhpException.Throw(PhpError.Notice, LibResources.GetString("deserialization_failed", e.Message, stream.Position, stream.Length));
                    //return new PhpReference(false);

                    throw;  // TODO: Err
                }
            }

            protected abstract PhpValue CommonDeserialize(Context ctx, PhpString data, RuntimeTypeHandle caller);
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
                public const char ClrObject = 'T';    // instance of CLR object, serialized using binary formatter

                public const char Reference = 'R'; // &-like reference
                public const char ObjectRef = 'r'; // same instance reference (PHP5 object semantics)
            }

            #endregion

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

            public static readonly PhpSerializer Instance = new PhpSerializer();

            public override string Name => "php";

            protected override PhpString CommonSerialize(Context ctx, PhpValue variable, RuntimeTypeHandle caller)
            {
                return ObjectWriter.Serialize(variable);
            }

            protected override PhpValue CommonDeserialize(Context ctx, PhpString data, RuntimeTypeHandle caller)
            {
                throw new NotImplementedException();
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
        /// <param name="str">The serialized string.</param>
        /// <param name="options">Any options to be provided to unserialize(), as an associative array.</param>
        /// <returns>
        /// The converted value is returned, and can be a boolean, integer, float, string, array or object.
        /// In case the passed string is not unserializeable, <c>FALSE</c> is returned and <b>E_NOTICE</b> is issued.
        /// </returns>
        public static PhpValue unserialize(PhpString str, PhpArray options = null)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
