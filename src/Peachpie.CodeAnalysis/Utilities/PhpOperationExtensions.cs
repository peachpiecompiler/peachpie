using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics
{
    public static class PhpOperationExtensions
    {
        /// <summary>
        /// Returns whether the expression has constant value.
        /// </summary>
        public static bool IsConstant(this BoundExpression expr) => expr.ConstantValue.HasValue;

        /// <summary>
        /// Gets value indicating the expression is a logic negation.
        /// </summary>
        public static bool IsLogicNegation(this BoundExpression expr, out BoundExpression operand)
        {
            if (expr is BoundUnaryEx unary && unary.Operation == Operations.LogicNegation)
            {
                operand = unary.Operand;
                return true;
            }
            else
            {
                operand = null;
                return false;
            }
        }

        static void PhpSerialize(StringBuilder result, object value)
        {
            if (value == null)
            {
                result.Append('N');
            }
            else if (value is string s)
            {
                result.Append('s');
                result.Append(':');
                result.Append(s.Length);    // TODO: encode to byte[] first
                result.Append(':');
                result.Append('"');
                result.Append(s);
                result.Append('"');
            }
            else if (value is int i)
            {
                PhpSerialize(result, (long)i);
                return;
            }
            else if (value is long l)
            {
                result.Append('i');
                result.Append(':');
                result.Append(l);
            }
            else if (value is bool b)
            {
                result.Append('b');
                result.Append(':');
                result.Append(b ? '1' : '0');
            }
            else if (value is double d)
            {
                result.Append('d');
                result.Append(':');
                result.Append(d.ToString("R", NumberFormatInfo.InvariantInfo));
            }
            else
            {
                throw new ArgumentException();
            }

            result.Append(';');
        }

        static void PhpSerialize(StringBuilder result, BoundArrayEx array)
        {
            int idx = 0;

            result.Append('a');
            result.Append(':');
            result.Append(array.Items.Length);
            result.Append(':');
            result.Append('{');
            foreach (var item in array.Items)
            {
                // key
                PhpSerialize(result, item.Key != null ? item.Key.ConstantValue.Value : (idx++));

                // value
                if (item.Value is BoundArrayEx nestedArr)
                    PhpSerialize(result, nestedArr);
                else if (item.Value.ConstantValue.HasValue)
                    PhpSerialize(result, item.Value.ConstantValue.Value);
                else
                    throw new ArgumentException();
            }
            result.Append('}');
        }

        /// <summary>
        /// Serialize array using the PHP serialization format.
        /// </summary>
        public static string PhpSerializeOrThrow(this BoundArrayEx array)
        {
            var result = new StringBuilder(16);
            PhpSerialize(result, array);
            return result.ToString();
        }

        /// <summary>
        /// Simple unserialize PHP object. Supports only arrays and literals.
        /// </summary>
        static BoundExpression PhpUnserializeOrThrow(ImmutableArray<byte> bytes)
        {
            var arr = new byte[bytes.Length];
            bytes.CopyTo(arr);

            int start = 0;
            var obj = PhpUnserializeOrThrow(arr, ref start);
            Debug.Assert(start == arr.Length);
            return obj;
        }

        static BoundExpression PhpUnserializeOrThrow(byte[] bytes, ref int start)
        {
            if (bytes == null) throw ExceptionUtilities.ArgumentNull(nameof(bytes));
            if (bytes.Length == 0) throw ExceptionUtilities.UnexpectedValue(bytes);
            if (start >= bytes.Length) throw new ArgumentOutOfRangeException();

            char t = (char)bytes[start++];

            switch (t)
            {
                case 'a':   // array
                    if (bytes[start++] != (byte)':') throw null;
                    var from = start;
                    while (bytes[start++] != (byte)':') { }
                    var length = int.Parse(Encoding.UTF8.GetString(bytes, from, start - from - 1));
                    if (bytes[start++] != (byte)'{') throw null;
                    var items = new List<KeyValuePair<BoundExpression, BoundExpression>>(length);
                    while (length-- > 0)
                    {
                        items.Add(new KeyValuePair<BoundExpression, BoundExpression>(
                            PhpUnserializeOrThrow(bytes, ref start),
                            PhpUnserializeOrThrow(bytes, ref start)));
                    }
                    if (bytes[start++] != (byte)'}') throw null;
                    return new BoundArrayEx(items).WithAccess(BoundAccess.Read);

                case 'N':   // NULL
                    if (bytes[start++] != (byte)';') throw null;
                    return new BoundLiteral(null).WithAccess(BoundAccess.Read);

                case 'b':   // bool
                    if (bytes[start++] != (byte)':') throw null;
                    var b = bytes[start++];
                    if (bytes[start++] != (byte)';') throw null;
                    return new BoundLiteral((b != 0).AsObject()).WithAccess(BoundAccess.Read);

                case 'i':   // int
                case 'd':   // double
                    if (bytes[start++] != (byte)':') throw null;
                    from = start;
                    while (bytes[start++] != (byte)';') { }
                    var value = Encoding.UTF8.GetString(bytes, from, start - from - 1);
                    var literal = (t == 'i') ? long.Parse(value) : double.Parse(value, CultureInfo.InvariantCulture);
                    return new BoundLiteral(literal).WithAccess(BoundAccess.Read);

                case 's':   // string
                    if (bytes[start++] != (byte)':') throw null;
                    from = start;
                    while (bytes[start++] != (byte)':') { }
                    length = int.Parse(Encoding.UTF8.GetString(bytes, from, start - from - 1));
                    if (bytes[start++] != (byte)'"') throw null;
                    from = start;   // from points after the first "
                    start += length;
                    while (bytes[start++] != (byte)'"') { } // start points after the closing " // even we know the length, just look for the " to be sure (UTF8 etc..)
                    value = Encoding.UTF8.GetString(bytes, from, start - from - 1);
                    if (bytes[start++] != (byte)';') throw null;
                    return new BoundLiteral(value).WithAccess(BoundAccess.Read);

                default:
                    throw ExceptionUtilities.UnexpectedValue(t);
            }
        }

        /// <summary>
        /// Decodes [DefaultValueAttribute].
        /// </summary>
        public static BoundExpression FromDefaultValueAttribute(AttributeData defaultValueAttribute)
        {
            if (defaultValueAttribute == null || defaultValueAttribute.NamedArguments.IsDefaultOrEmpty)
            {
                return null;
            }

            //[DefaultValue(Type = PhpArray, SerializedValue = new byte[]{ ... })]
            var p = defaultValueAttribute.NamedArguments;

            // SerializedValue?
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Key == "SerializedValue" && !p[i].Value.IsNull)
                {
                    var data = p[i].Value.Values.SelectAsArray(c => (byte)c.Value);
                    if (data.Length != 0)
                    {
                        // unserialize {bytes}
                        return PhpUnserializeOrThrow(data);
                    }
                }
            }

            // Type?
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Key == "Type" && p[i].Value.Value is int itype)
                {
                    switch (itype)
                    {
                        case 1: // PhpArray
                            return new BoundArrayEx(Array.Empty<KeyValuePair<BoundExpression, BoundExpression>>()).WithAccess(BoundAccess.Read);
                        default:
                            throw new ArgumentException();
                    }
                }
            }

            //
            throw new ArgumentException();
        }
    }
}
