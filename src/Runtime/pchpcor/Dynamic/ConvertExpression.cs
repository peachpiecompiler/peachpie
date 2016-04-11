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

            //
            if (target == typeof(long)) return BindToLong(arg);
            if (target == typeof(PhpNumber)) return BindToNumber(arg);
            if (target == typeof(PhpValue)) return BindToValue(arg);

            //
            throw new NotImplementedException();
        }

        private static Expression BindToLong(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int)) return Expression.Convert(expr, typeof(long));
            if (source == typeof(long)) return expr;    // unreachable
            if (source == typeof(PhpNumber)) return Expression.Convert(expr, typeof(long), typeof(PhpNumber).GetMethod("ToLong", Cache.Types.Empty));

            throw new NotImplementedException();
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

            throw new NotImplementedException(source.FullName);
        }
    }
}
