using System;
using System.Globalization;
using System.Text;
using Devsense.PHP.Syntax.Ast;

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
                result.Append(s.Length);
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
    }
}
