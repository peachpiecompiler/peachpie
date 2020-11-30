using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Pchp.Core.Utilities
{
    /// <summary>
    /// Provides helper method for <see cref="PhpValue"/>s.
    /// </summary>
    public static class PhpValueUtils
    {
        /// <summary>
        /// Deserializes JSON string into a <see cref="PhpValue"/>.
        /// </summary>
        public static PhpValue CreateFromJson(string value) => CreateFromJson(Encoding.UTF8.GetBytes(value).AsSpan());

        /// <summary>
        /// Deserializes JSON string into a <see cref="PhpValue"/>.
        /// </summary>
        public static PhpValue CreateFromJson(ReadOnlySpan<byte> utf8bytes)
        {
            if (utf8bytes.IsEmpty)
            {
                return PhpValue.Null;
            }
            else
            {
                var reader = new Utf8JsonReader(utf8bytes);
                return ReadValue(ref reader);
            }
        }

        static PhpValue ReadValue(ref Utf8JsonReader reader)
        {
            if (reader.Read())
            {
                return GetValue(ref reader);
            }

            // EOF
            return PhpValue.Null;
        }

        static PhpValue GetValue(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    return new PhpArray(ReadObject(ref reader)); // CONSIDER: stdClass

                case JsonTokenType.StartArray:
                    return new PhpArray(ReadArray(ref reader));

                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var l))
                    {
                        return l;
                    }

                    if (reader.TryGetDouble(out var d))
                    {
                        return d;
                    }

                    throw SyntaxException();

                case JsonTokenType.True:
                    return PhpValue.True;

                case JsonTokenType.False:
                    return PhpValue.False;

                case JsonTokenType.Null:
                    return PhpValue.Null;
            }

            //
            throw SyntaxException();
        }

        static OrderedDictionary ReadObject(ref Utf8JsonReader reader)
        {
            var props = new OrderedDictionary();

            // read properties until EndObject
            for (; ; )
            {
                if (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        props[reader.GetString()] = ReadValue(ref reader);
                        continue;
                    }
                    else if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }
                }

                throw SyntaxException();
            }

            //
            return props;
        }

        static OrderedDictionary ReadArray(ref Utf8JsonReader reader)
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
                        props.Add(GetValue(ref reader));
                        continue;
                    }
                }

                throw SyntaxException();
            }

            //
            return props;
        }

        static Exception SyntaxException() => new JsonException();
    }
}
