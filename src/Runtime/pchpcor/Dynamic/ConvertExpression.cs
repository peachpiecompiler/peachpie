using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Conversion value used for overload resolution.
    /// </summary>
    [Flags]
    public enum ConversionCost : ushort
    {
        /// <summary>
        /// No conversion is needed. Best case.
        /// </summary>
        Pass = 0,

        /// <summary>
        /// The operation is costly but the value is kept without loosing precision.
        /// </summary>
        PassCostly = 1,

        /// <summary>
        /// Conversion using implicit cast without loosing precision.
        /// </summary>
        ImplicitCast = 2,

        /// <summary>
        /// Conversion using explicit cast that may loose precision.
        /// </summary>
        LoosingPrecision = 4,

        /// <summary>
        /// Conversion is possible but the value is lost and warning should be generated.
        /// </summary>
        Warning = 8,

        /// <summary>
        /// Implicit value will be used, argument is missing and parameter is optional.
        /// </summary>
        DefaultValue = 16,

        /// <summary>
        /// Too many arguments provided. Arguments will be omitted.
        /// </summary>
        TooManyArgs = 32,

        /// <summary>
        /// Missing mandatory arguments, default values will be used instead.
        /// </summary>
        MissingArgs = 64,

        /// <summary>
        /// Conversion does not exist.
        /// </summary>
        NoConversion = 128,

        /// <summary>
        /// Unspecified error.
        /// </summary>
        Error = 256,
    }

    /// <summary>
    /// Helper methods for converting expressions.
    /// </summary>
    internal static class ConvertExpression
    {
        #region Bind

        public static Expression Bind(DynamicMetaObject arg, ref int cost, ref BindingRestrictions restrictions, Type target)
        {
            var result = Bind(arg.Expression, target);

            if (arg.Expression != result)
            {
                switch (result.NodeType)
                {
                    case ExpressionType.Call:
                        cost += 2;
                        break;
                    case ExpressionType.Convert:
                        // TODO: conversion cost

                        // int -> long is not considered as a conversion
                        if (arg.Expression.Type == typeof(int) && target == typeof(long))
                            break;

                        //
                        goto default;
                    case ExpressionType.Constant:
                        break;
                    default:
                        cost++;
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Creates expression that converts <paramref name="arg"/> to <paramref name="target"/> type.
        /// </summary>
        /// <param name="arg">Source expression to be converted.</param>
        /// <param name="target">Target type.</param>
        /// <returns>Expression converting <paramref name="arg"/> to <paramref name="target"/> type.</returns>
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
            if (target == typeof(int)) return Expression.Convert(BindToLong(arg), target);
            if (target == typeof(double)) return BindToDouble(arg);
            if (target == typeof(string)) return BindToString(arg);
            if (target == typeof(bool)) return BindToBool(arg);
            if (target == typeof(PhpNumber)) return BindToNumber(arg);
            if (target == typeof(PhpValue)) return BindToValue(arg);
            if (target == typeof(void)) return BindToVoid(arg);
            if (target == typeof(object)) return BindToClass(arg);
            if (target == typeof(PhpArray)) return BindAsArray(arg);
            if (target == typeof(IPhpCallable)) return BindAsCallable(arg);

            //
            throw new NotImplementedException(target.ToString());
        }

        private static Expression BindToLong(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int)) return Expression.Convert(expr, typeof(long));
            if (source == typeof(long)) return expr;    // unreachable
            if (source == typeof(double)) return Expression.Convert(expr, typeof(long));
            if (source == typeof(PhpNumber)) return Expression.Convert(expr, typeof(long), typeof(PhpNumber).GetMethod("ToLong", Cache.Types.Empty));
            if (source == typeof(PhpArray)) return Expression.Call(expr, typeof(PhpArray).GetMethod("ToLong", Cache.Types.Empty));
            if (source == typeof(void)) return VoidAsConstant(expr, 0L, typeof(long));

            // TODO: following conversions may fail, we should report it failed and throw an error
            if (source == typeof(PhpValue)) return Expression.Call(expr, typeof(PhpValue).GetMethod("ToLong", Cache.Types.Empty));

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindToDouble(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int)) return Expression.Convert(expr, typeof(double));
            if (source == typeof(long)) return Expression.Convert(expr, typeof(double));
            if (source == typeof(PhpNumber)) return Expression.Convert(expr, typeof(double), typeof(PhpNumber).GetMethod("ToDouble", Cache.Types.Empty));
            if (source == typeof(PhpArray)) return Expression.Call(expr, typeof(PhpArray).GetMethod("ToDouble", Cache.Types.Empty));
            if (source == typeof(void)) return VoidAsConstant(expr, 0.0, typeof(double));
            if (source == typeof(double)) return expr;

            // TODO: following conversions may fail, we should report it failed and throw an error
            if (source == typeof(PhpValue)) return Expression.Call(expr, typeof(PhpValue).GetMethod("ToDouble", Cache.Types.Empty));

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindToBool(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int)) return Expression.Convert(expr, typeof(bool));
            if (source == typeof(long)) return Expression.Convert(expr, typeof(bool));
            if (source == typeof(PhpNumber)) return Expression.Call(expr, typeof(PhpNumber).GetMethod("ToBoolean", Cache.Types.Empty));
            if (source == typeof(PhpArray)) return Expression.Call(expr, typeof(PhpArray).GetMethod("ToBoolean", Cache.Types.Empty));
            if (source == typeof(PhpValue)) return Expression.Call(expr, typeof(PhpValue).GetMethod("ToBoolean", Cache.Types.Empty));
            if (source == typeof(void)) return VoidAsConstant(expr, false, typeof(bool));
            if (source == typeof(bool)) return expr;

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

            if (source == typeof(PhpValue))
                return Expression.Call(expr, Cache.Operators.PhpValue_ToString_Context, Expression.Constant(null, typeof(Context))); // TODO: Context

            if (source == typeof(void))
                return VoidAsConstant(expr, string.Empty, typeof(string));

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
            if (source == typeof(void)) return VoidAsConstant(expr, PhpNumber.Default, typeof(PhpNumber));
            if (source == typeof(PhpNumber)) return expr;
            if (source == typeof(PhpValue)) return Expression.Convert(expr, typeof(PhpNumber));

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
            if (source == typeof(PhpNumber)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.PhpNumber), expr);
            if (source == typeof(PhpValue)) return expr;

            if (source.GetTypeInfo().IsValueType)
            {
                if (source == typeof(void)) return VoidAsConstant(expr, PhpValue.Void, Cache.Types.PhpValue[0]);

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

            if (source == typeof(PhpValue)) return Expression.Call(expr, Cache.Operators.PhpValue_ToClass);
            if (source == typeof(PhpArray)) return Expression.Call(expr, Cache.Operators.PhpArray_ToClass);
            if (source == typeof(PhpNumber)) return Expression.Call(expr, typeof(PhpNumber).GetMethod("ToClass", Cache.Types.Empty));

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindAsArray(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(PhpArray)) return expr;
            if (source == typeof(PhpValue)) return Expression.Call(expr, Cache.Operators.PhpValue_AsArray);

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindAsCallable(Expression expr)
        {
            var source = expr.Type;

            if (typeof(IPhpCallable).GetTypeInfo().IsAssignableFrom(source.GetTypeInfo())) return expr;

            return Expression.Call(BindToValue(expr), Cache.Operators.PhpValue_AsCallable);
        }

        private static Expression BindToVoid(Expression expr)
        {
            var source = expr.Type;

            if (source != typeof(void))
            {
                return Expression.Block(typeof(void), expr);
            }
            else
            {
                return expr;
            }
        }

        private static Expression VoidAsConstant(Expression expr, object value, Type type)
        {
            Debug.Assert(expr.Type == typeof(void));

            // block{ expr; return constant; }

            var constant = Expression.Constant(value, type);

            return Expression.Block(expr, constant);
        }

        #endregion

        #region BindCost

        /// <summary>
        /// Creates expression that calculates cost of conversion from <paramref name="arg"/> to type <paramref name="target"/>.
        /// In some cases, returned expression is a constant and can be used in compile time.
        /// </summary>
        /// <param name="arg">Expression to be converted.</param>
        /// <param name="target">Target type.</param>
        /// <returns>Expression calculating the cost of conversion.</returns>
        public static Expression BindCost(Expression arg, Type target)
        {
            if (arg == null || target == null)
                throw new ArgumentNullException();

            var t = arg.Type;
            if (t == target)
                return Expression.Constant(ConversionCost.Pass);

            if (t == typeof(PhpValue)) return BindCostFromValue(arg, target);
            if (t == typeof(double)) return Expression.Constant(BindCostFromDouble(arg, target));
            if (t == typeof(long)) return Expression.Constant(BindCostFromLong(arg, target));

            //
            throw new NotImplementedException($"costof({t} -> {target})");
        }

        static Expression BindCostFromValue(Expression arg, Type target)
        {
            Debug.Assert(arg.Type == typeof(PhpValue));

            // constant cases
            if (target == typeof(PhpValue)) return Expression.Constant(ConversionCost.Pass);

            //
            if (!target.GetTypeInfo().IsValueType)
            {
                // ...
            }

            // fallback
            return Expression.Call(typeof(CostOf).GetMethod("To" + target.Name, arg.Type), arg);
        }

        static ConversionCost BindCostFromDouble(Expression arg, Type target)
        {
            Debug.Assert(arg.Type == typeof(double));

            if (target == typeof(double)) return (ConversionCost.Pass);
            if (target == typeof(PhpNumber)) return (ConversionCost.PassCostly);
            if (target == typeof(long)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(bool)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(PhpValue)) return (ConversionCost.PassCostly);
            if (target == typeof(string)) return (ConversionCost.ImplicitCast);
            if (target == typeof(PhpArray)) return (ConversionCost.Warning);

            throw new NotImplementedException($"costof(double -> {target})");
        }

        static ConversionCost BindCostFromLong(Expression arg, Type target)
        {
            Debug.Assert(arg.Type == typeof(double));

            if (target == typeof(long)) return (ConversionCost.Pass);
            if (target == typeof(PhpNumber)) return (ConversionCost.PassCostly);
            if (target == typeof(double)) return (ConversionCost.ImplicitCast);
            if (target == typeof(bool)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(PhpValue)) return (ConversionCost.PassCostly);
            if (target == typeof(string)) return (ConversionCost.ImplicitCast);
            if (target == typeof(PhpArray)) return (ConversionCost.Warning);

            throw new NotImplementedException($"costof(long -> {target})");
        }

        #endregion
    }

    /// <summary>
    /// Runtime routines that calculates cost of conversion.
    /// </summary>
    public static class CostOf
    {
        /// <summary>
        /// Gets minimal value of given operands.
        /// </summary>
        public static ConversionCost Min(ConversionCost a, ConversionCost b) => (a < b) ? a : b;

        public static ConversionCost Or(ConversionCost a, ConversionCost b) => a | b;

        #region CostOf

        public static ConversionCost ToInt32(PhpValue value) => ToInt64(value);

        public static ConversionCost ToInt64(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long:
                    return ConversionCost.Pass;

                case PhpTypeCode.Boolean:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.Double:
                case PhpTypeCode.WritableString:
                case PhpTypeCode.String:
                    return ConversionCost.LoosingPrecision;

                case PhpTypeCode.PhpArray:
                    return ConversionCost.Warning;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToString(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long:
                case PhpTypeCode.Boolean:
                case PhpTypeCode.Double:
                case PhpTypeCode.Object:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.WritableString:
                case PhpTypeCode.String:
                    return ConversionCost.Pass;

                case PhpTypeCode.PhpArray:
                    return ConversionCost.Warning;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToDouble(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long:
                case PhpTypeCode.Boolean:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.Double:
                    return ConversionCost.Pass;

                case PhpTypeCode.WritableString:
                case PhpTypeCode.String:
                    return ConversionCost.LoosingPrecision;

                case PhpTypeCode.PhpArray:
                    return ConversionCost.Warning;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToPhpNumber(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Int32:
                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                    return ConversionCost.Pass;

                case PhpTypeCode.Boolean:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.WritableString:
                case PhpTypeCode.String:
                    return ConversionCost.LoosingPrecision;

                case PhpTypeCode.PhpArray:
                    return ConversionCost.Warning;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        #endregion
    }
}
