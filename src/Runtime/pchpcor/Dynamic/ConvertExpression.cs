using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Dynamic
{
    internal static class ConvertExpression
    {
        public static Expression Bind(Expression arg, Type target)
        {
            if (arg.Type == target)
                return arg;

            // dereference
            if (arg.Type == typeof(PhpAlias))
            {
                arg = Expression.PropertyOrField(arg, "Value");

                if (target == typeof(PhpValue))
                    return arg;
            }

            //
            if (target == typeof(long)) return BindToLong(arg);
            if (target == typeof(double)) return BindToDouble(arg);
            if (target == typeof(string)) return BindToString(arg);
            if (target == typeof(PhpNumber)) return BindToNumber(arg);
            if (target == typeof(PhpValue)) return BindToValue(arg);
            if (target == typeof(void)) return BindToVoid(arg);
            if (target == typeof(object)) return BindToClass(arg);
            if (target == typeof(PhpArray)) return BindAsArray(arg);

            //
            throw new NotImplementedException(target.ToString());
        }

        private static Expression BindToLong(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int)) return Expression.Convert(expr, typeof(long));
            if (source == typeof(long)) return expr;    // unreachable
            if (source == typeof(PhpNumber)) return Expression.Convert(expr, typeof(long), typeof(PhpNumber).GetMethod("ToLong", Cache.Types.Empty));

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindToDouble(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int)) return Expression.Convert(expr, typeof(double));
            if (source == typeof(long)) return Expression.Convert(expr, typeof(double));
            if (source == typeof(PhpNumber)) return Expression.Convert(expr, typeof(long), typeof(PhpNumber).GetMethod("ToDouble", Cache.Types.Empty));

            // TODO: following conversions may fail, we should report it failed and throw an error
            if (source == typeof(PhpValue)) return Expression.Call(expr, typeof(PhpValue).GetMethod("ToDouble", Cache.Types.Empty));

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindToString(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int) ||
                source == typeof(long) ||
                source == typeof(double))   // TODO: Convert.ToString(double, context)
                return Expression.Call(expr, Cache.Object.ToString);

            if (source == typeof(string))
                return expr;

            if (source == typeof(void))
                return Expression.Constant(string.Empty, typeof(string));

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindToNumber(Expression expr)
        {
            var source = expr.Type;

            //
            if (source == typeof(int))
            {
                source = typeof(long);
                expr = Expression.Convert(expr, typeof(long));
            }

            //
            if (source == typeof(long)) return Expression.Call(typeof(PhpNumber).GetMethod("Create", Cache.Types.Long), expr);
            if (source == typeof(double)) return Expression.Call(typeof(PhpNumber).GetMethod("Create", Cache.Types.Double), expr);

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindToValue(Expression expr)
        {
            var source = expr.Type;

            //
            if (source == typeof(bool)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Bool), expr);
            if (source == typeof(int)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Int), expr);
            if (source == typeof(long)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Long), expr);
            if (source == typeof(double)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Double), expr);
            if (source == typeof(string)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.String), expr);
            if (source == typeof(PhpString)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.PhpString), expr);
            

            if (source.GetTypeInfo().IsValueType)
            {
                throw new NotImplementedException(source.FullName);
            }
            else
            {
                // TODO: FromClr
                return Expression.Call(typeof(PhpValue).GetMethod("FromClass", Cache.Types.Object), expr);
            }
        }

        private static Expression BindToClass(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(PhpValue)) return Expression.Call(expr, Cache.Operators.PhpValue_ToClass, Expression.Constant(null, typeof(Context))); // TODO: Context
            if (source == typeof(PhpArray)) throw new NotImplementedException(source.FullName);
            

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindAsArray(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(PhpValue)) return Expression.Call(expr, Cache.Operators.PhpValue_AsArray);

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindToVoid(Expression expr)
        {
            var source = expr.Type;

            if (source != typeof(void))
            {
                return Expression.Block(expr);
            }
            else
            {
                return expr;
            }
        }
    }
}
