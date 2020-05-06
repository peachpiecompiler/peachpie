using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Helper methods for converting expressions (implicit conversions).
    /// </summary>
    internal static class ConvertExpression
    {
        #region Bind

        /// <summary>
        /// Creates expression that converts <paramref name="arg"/> to <paramref name="target"/> type.
        /// </summary>
        /// <param name="arg">Source expression to be converted.</param>
        /// <param name="target">Target type.</param>
        /// <param name="ctx">Expression with current context.</param>
        /// <returns>Expression converting <paramref name="arg"/> to <paramref name="target"/> type.</returns>
        public static Expression Bind(Expression arg, Type target, Expression ctx)
        {
            if (arg.Type == target)
            {
                return arg;
            }

            // dereference
            if (arg.Type == Cache.Types.PhpAlias[0])
            {
                return Bind(Expression.Field(arg, Cache.PhpAlias.Value), target, ctx);
            }

            // from Nullable<T>
            if (arg.Type.IsNullable_T(out var T))
            {
                // Template: arg.HasValue ? arg.Value : NULL
                return Expression.Condition(
                    test: Expression.Property(arg, "HasValue"),
                    ifTrue: Bind(Expression.Call(arg, arg.Type.GetMethod("GetValueOrDefault", Array.Empty<Type>())), target, ctx),
                    ifFalse: Bind(Expression.Field(null, Cache.Properties.PhpValue_Null), target, ctx));
            }

            // from (object)null
            if (IsNullConstant(arg) && !target.IsValueType)
            {
                if (target == typeof(PhpAlias))
                {
                    // new PhppAlias(PhpValue.Null, 1)
                    return Expression.New(Cache.PhpAlias.ctor_PhpValue_int, BindToValue(arg), Expression.Constant(1));
                }

                // (T)null
                return Expression.Constant(null, target);
            }

            // from IndirectLocal
            if (arg.Type == Cache.Types.IndirectLocal)
            {
                if (target == Cache.Types.PhpAlias[0])
                {
                    // Template: arg.EnsureAlias()
                    return Expression.Call(arg, Cache.IndirectLocal.EnsureAlias);
                }
                else
                {
                    // Template: arg.GetValue()
                    return Bind(Expression.Call(arg, Cache.IndirectLocal.GetValue), target, ctx);
                }
            }

            Debug.Assert(ctx != null, "!ctx");

            //
            if (target == typeof(long)) return BindToLong(arg);
            if (target == typeof(int) || target == typeof(uint) ||
                target == typeof(short) || target == typeof(ushort) ||
                target == typeof(byte) || target == typeof(sbyte) ||
                target == typeof(ulong)) return Expression.Convert(BindToLong(arg), target);
            if (target == typeof(double)) return BindToDouble(arg);
            if (target == typeof(float) || target == typeof(decimal)) return Expression.Convert(BindToDouble(arg), target);  // (float)double
            if (target == typeof(string)) return BindToString(arg, ctx);
            if (target == typeof(char)) return BindToChar(arg, ctx);
            if (target == typeof(bool)) return BindToBool(arg);
            if (target == typeof(PhpNumber)) return BindToNumber(arg);
            if (target == typeof(PhpValue)) return BindToValue(arg);
            if (target == typeof(void)) return BindToVoid(arg);
            if (target == typeof(object)) return BindAsObject(arg);
            //if (target == typeof(stdClass)) return BindAsStdClass(arg);
            if (target == typeof(PhpArray) ||
                target == typeof(IPhpEnumerable) ||
                target == typeof(IPhpArray) ||
                target == typeof(PhpHashtable)) return BindAsArray(arg, target);
            if (target == typeof(IntStringKey)) return BindIntStringKey(arg);
            if (target == typeof(IPhpCallable)) return BindAsCallable(arg);
            if (target == typeof(PhpString)) return BindToPhpString(arg, ctx);
            if (target == typeof(byte[])) return Expression.Call(BindToPhpString(arg, ctx), Cache.PhpString.ToBytes_Context, ctx);
            if (target == typeof(PhpAlias))
            {
                //Debug.Assert(arg.Type.IsByRef && arg.Type == typeof(PhpValue), "Variable should be PhpValue and passed by ref so things will work out!");
                if (arg.Type == typeof(PhpValue)) return Expression.Call(Cache.Operators.EnsureAlias_PhpValueRef, arg);
                return Expression.New(Cache.PhpAlias.ctor_PhpValue_int, BindToValue(arg), Expression.Constant(1));
            }

            if (target.IsByRef)
            {
                // TODO: what to do
                target = target.UnderlyingSystemType;
            }

            // enum
            if (target.IsEnum)
            {
                return Expression.Convert(BindToLong(arg), target);
            }

            // 
            if (target.IsValueType == false)
            {
                Debug.Assert(typeof(Nullable<bool>).IsValueType);

                return BindAsReferenceType(arg, target);
            }
            else
            {
                // IntPtr
                if (target == typeof(IntPtr))
                {
                    if (IsNullConstant(arg))
                    {
                        // IntPtr.Zero
                        return Expression.Field(null, typeof(IntPtr), "Zero");
                    }
                    else
                    {
                        // new IntPtr(long)
                        return Expression.New(typeof(IntPtr).GetCtor(Cache.Types.Long), BindToLong(arg));
                    }
                }

                // DateTime
                if (target == typeof(DateTime))
                {
                    return BindToDateTime(arg, ctx);
                }
                // Nullable<T>
                if (target.IsNullable_T(out T))
                {
                    return BindToNullable(arg, target, T, ctx);
                }
            }

            //
            throw new NotImplementedException(target.ToString());
        }

        private static bool IsNullConstant(Expression arg)
        {
            return arg is ConstantExpression c && ReferenceEquals(c.Value, null);
        }

        public static MethodInfo FindImplicitOperator(Type t, Type toType)
        {
            foreach (var m in t.GetRuntimeMethods())
            {
                // "public static TO op_Implicit(THIS)"
                if (m.IsPublic && m.IsStatic && m.Name == "op_Implicit" &&
                    m.ReturnParameter.ParameterType == toType)
                {
                    var ps = m.GetParameters();
                    if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(t))
                    {
                        return m;
                    }
                }
            }

            return null;
        }

        static Expression BindToDateTime(Expression arg, Expression ctx)
        {
            var impl = FindImplicitOperator(arg.Type, typeof(DateTime));
            if (impl != null)
            {
                return Expression.Convert(arg, typeof(DateTime), impl);
            }
            else
            {
                // Convert.ToDateTime( PhpValue )
                return Expression.Call(Cache.Operators.ToDateTime_PhpValue, BindToValue(arg));
            }
        }

        static Expression PhpValueIsNullOrFalse(Expression arg)
        {
            // Template: arg.IsNull || arg.IsFalse
            Debug.Assert(arg.Type == typeof(PhpValue));
            return Expression.OrElse(
                Expression.Property(arg, Cache.Properties.PhpValue_IsNull),
                Expression.Property(arg, Cache.Properties.PhpValue_IsFalse));
        }

        static Expression BindToNullable(Expression arg, Type target, Type nullable_t, Expression ctx)
        {
            Debug.Assert(target.IsNullable_T(out var tmp_t) && nullable_t == tmp_t);

            // special cases: constants NULL -> default(Nullable<T>)
            if (arg is ConstantExpression ce && (ce.Value == null))
            {
                return Expression.Default(target);
            }

            // Template: new Nullable<T>( (T)arg )
            Expression new_nullable = null;

            try
            {
                new_nullable = Expression.New(target.GetTypeInfo().DeclaredConstructors.Single(), Bind(arg, nullable_t, ctx));
            }
            catch (Exception x) when (x is NotImplementedException || x is NotSupportedException)
            {
                // conversion is not allowed
                new_nullable = Expression.Block(
                    Expression.Throw(Expression.Constant(x)),
                    Expression.Default(target));
            }

            // HasValue: !NULL && !FALSE
            Expression hasValueCheck = null;

            if (nullable_t != typeof(bool))
            {
                if (arg.Type == typeof(bool))
                {
                    // arg === FALSE
                    hasValueCheck = arg;
                }
                else if (arg.Type == typeof(PhpValue))
                {
                    // (bool)arg // implicit bool operator
                    hasValueCheck = Expression.IsFalse(PhpValueIsNullOrFalse(arg));
                }
            }

            if (!arg.Type.IsValueType)
            {
                // !NULL
                hasValueCheck = Expression.ReferenceNotEqual(arg, Expression.Constant(null, arg.Type));
            }

            // new Nullable<T>( Bind(arg, T) )
            if (hasValueCheck != null)
            {
                return Expression.Condition(
                    test: hasValueCheck,
                    ifTrue: new_nullable,
                    ifFalse: Expression.Default(target));
            }
            else
            {
                return new_nullable;
            }
        }

        private static Expression BindToLong(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int) || source == typeof(uint) ||
                source == typeof(double) || source == typeof(float))
            {
                // (long)expr
                return Expression.Convert(expr, typeof(long));
            }

            if (source == typeof(PhpNumber)) return Expression.Call(Cache.Operators.ToLong_PhpNumber, expr);
            //TypeError//if (source == typeof(PhpArray)) return Expression.Call(expr, typeof(PhpArray).GetMethod("ToLong", Cache.Types.Empty));
            if (source == typeof(string)) return Expression.Call(Cache.Operators.ToLongOrThrow_String, expr);
            if (source == typeof(PhpString)) return Expression.Call(Cache.PhpString.ToLongOrThrow, expr);
            if (source == typeof(void)) return VoidAsConstant(expr, 0L, typeof(long));
            if (source == typeof(bool)) return Expression.Call(Cache.Operators.ToLong_Boolean, expr);
            if (source == typeof(long)) return expr;    // unreachable

            // TODO: following conversions may fail, we should report it failed and throw TypeError exception

            if (IsNullConstant(expr))
            {
                // TODO: TypeError
            }

            return Expression.Call(Cache.Operators.PhpValue_ToLongOrThrow, BindToValue(expr));
        }

        private static Expression BindToDouble(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int) ||
                source == typeof(uint) ||
                source == typeof(long)) return Expression.Convert(expr, typeof(double));
            if (source == typeof(PhpNumber)) return Expression.Call(Cache.Operators.ToDouble_PhpNumber, expr);
            if (source == typeof(PhpArray)) return Expression.Call(Cache.Operators.ToDouble_PhpArray, expr);
            if (source == typeof(string)) return Expression.Call(Cache.Operators.ToDouble_String, expr);
            if (source == typeof(PhpString)) return Expression.Call(expr, Cache.PhpString.ToDouble);
            if (source == typeof(void)) return VoidAsConstant(expr, 0.0, typeof(double));
            if (source == typeof(double)) return expr;
            if (source == typeof(float)) return Expression.Convert(expr, typeof(double));

            // TODO: following conversions may fail, we should report it failed and throw an error
            if (source == typeof(PhpValue)) return Expression.Call(Cache.Operators.ToDouble_PhpValue, expr);

            throw new NotImplementedException($"{source.FullName} -> double");
        }

        public static Expression BindToBool(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int)) return Expression.NotEqual(expr, Expression.Constant(0, Cache.Types.Int[0]));    // <int> != 0
            if (source == typeof(uint)) return Expression.NotEqual(expr, Expression.Constant((uint)0, typeof(uint)));    // <uint> != 0
            if (source == typeof(long)) return Expression.NotEqual(expr, Expression.Constant(0L, Cache.Types.Long[0]));    // <long> != 0
            if (source == typeof(PhpNumber)) return Expression.Call(Cache.Operators.ToBoolean_PhpNumber, expr);
            if (source == typeof(PhpArray)) return Expression.Call(Cache.Operators.ToBoolean_PhpArray, expr);
            if (source == typeof(PhpValue)) return Expression.Call(Cache.Operators.ToBoolean_PhpValue, expr);
            if (source == typeof(PhpString)) return Expression.Call(expr, Cache.PhpString.ToBoolean);
            if (source == typeof(void)) return VoidAsConstant(expr, false, typeof(bool));
            if (source == typeof(bool)) return expr;

            return Expression.Call(Cache.Operators.ToBoolean_Object, expr);
        }

        private static Expression BindToString(Expression expr, Expression ctx)
        {
            var source = expr.Type;

            if (source == typeof(int) ||
                source == typeof(uint) ||
                source == typeof(long))
                return Expression.Call(expr, Cache.Object.ToString);

            if (source == typeof(double))
                return Expression.Call(Cache.Object.ToString_Double_Context, expr, ctx);

            if (source == typeof(float))
                return Expression.Call(Cache.Object.ToString_Double_Context, Expression.Convert(expr, typeof(double)), ctx);    // ToString((double)expr, ctx)

            if (source == typeof(bool))
                return Expression.Call(Cache.Object.ToString_Bool, expr);

            if (source == typeof(string))
                return expr;

            if (source == typeof(PhpString))
                return Expression.Call(expr, Cache.PhpString.ToString_Context, ctx);

            if (source == typeof(PhpValue))
                return Expression.Call(expr, Cache.Operators.PhpValue_AsString_Context, ctx);

            if (source == typeof(void))
                return VoidAsConstant(expr, null, typeof(string));

            if (source == typeof(PhpNumber))
                return Expression.Call(expr, Cache.Operators.PhpNumber_ToString_Context, ctx);

            if (source == typeof(object))
            {
                // NULL
                if (expr is ConstantExpression && ((ConstantExpression)expr).Value == null)
                {
                    // (string)null
                    return Expression.Constant(null, typeof(string));
                }

                // __toString is called by ToString below
            }

            var callToString = Expression.Call(expr, Cache.Object.ToString);
            if (expr.Type.IsValueType)
            {
                // ToString()
                return callToString;
            }
            else
            {
                var nullString = Expression.Constant(null, typeof(string));

                // ?ToString()
                return Expression.Condition(Expression.ReferenceEqual(expr, nullString), nullString, callToString);
            }
        }

        private static Expression BindToChar(Expression expr, Expression ctx)
        {
            var source = expr.Type;

            if (source == typeof(int) ||
                source == typeof(uint) ||
                source == typeof(long) ||
                source == typeof(char) ||
                source == typeof(float) ||
                source == typeof(byte) ||
                source == typeof(double))
            {
                return Expression.Convert(expr, typeof(char));
            }
            else if (source == typeof(string))
            {
                return Expression.Call(new Func<string, char>(Convert.ToChar).Method, expr);
            }
            else if (source == typeof(PhpString) || source == typeof(PhpValue) || source == typeof(PhpNumber))
            {
                // Template: Convert.ToChar( (PhpValue){expr} )
                return Expression.Call(new Func<PhpValue, char>(Convert.ToChar).Method, BindToValue(expr));
            }
            else
            {
                // default(char)
                return Expression.Constant(default(char), typeof(char));
            }
        }

        private static Expression BindToPhpString(Expression expr, Expression ctx)
        {
            var source = expr.Type;

            //
            if (source == typeof(PhpString))
                return expr;

            if (source == typeof(PhpValue))
                return Expression.Call(Cache.Operators.ToPhpString_PhpValue_Context, expr, ctx);    // Convert.ToPhpString(PhpValue, Context)

            if (source == typeof(byte[]))
                return Expression.New(Cache.PhpString.ctor_ByteArray, expr);     // new PhpString(byte[])

            // expr -> string -> PhpString
            return Expression.New(Cache.PhpString.ctor_String, BindToString(expr, ctx));        // new PhpString(string)
        }

        private static Expression BindIntStringKey(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(int)) return Expression.New(Cache.IntStringKey.ctor_Long, Expression.Convert(expr, Cache.Types.Long[0]));
            if (source == typeof(long)) return Expression.New(Cache.IntStringKey.ctor_Long, expr);
            if (source == typeof(string)) return Expression.New(Cache.IntStringKey.ctor_String, expr);

            // following conversions may throw an exception
            if (source == typeof(PhpValue)) return Expression.Call(expr, Cache.Operators.PhpValue_ToIntStringKey);

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindToNumber(Expression expr)
        {
            var source = expr.Type;

            //
            if (source == typeof(int) || source == typeof(uint))
            {
                source = typeof(long);
                expr = Expression.Convert(expr, typeof(long));
            }

            //
            if (source == typeof(long)) return Expression.Call(typeof(PhpNumber).GetMethod("Create", Cache.Types.Long), expr);
            if (source == typeof(double)) return Expression.Call(typeof(PhpNumber).GetMethod("Create", Cache.Types.Double), expr);
            if (source == typeof(float)) return Expression.Call(typeof(PhpNumber).GetMethod("Create", Cache.Types.Double), Expression.Convert(expr, typeof(double)));
            if (source == typeof(void)) return VoidAsConstant(expr, PhpNumber.Default, typeof(PhpNumber));
            if (source == typeof(PhpNumber)) return expr;
            if (source == typeof(PhpValue)) return Expression.Convert(expr, typeof(PhpNumber));
            if (source == typeof(string)) return Expression.Call(Cache.Operators.ToPhpNumber_String, expr);

            throw new NotImplementedException(source.FullName);
        }

        public static Expression BindToValue(Expression expr)
        {
            // known constants:
            if (IsNullConstant(expr))
            {
                // PhpValue.Null
                return Expression.Field(null, Cache.Properties.PhpValue_Null);
            }

            if (expr is ConstantExpression ce)
            {
                if (ce.Value is bool b)
                {
                    return Expression.Field(null, b ? Cache.Properties.PhpValue_True : Cache.Properties.PhpValue_False);
                }
            }

            //

            var source = expr.Type;

            //
            if (source == typeof(PhpValue)) return expr;
            if (source == typeof(bool)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Bool), expr);
            if (source == typeof(int)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Int), expr);
            if (source == typeof(long)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Long), expr);
            if (source == typeof(double)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Double), expr);
            if (source == typeof(float)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Double), Expression.Convert(expr, typeof(double)));
            if (source == typeof(string)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.String), expr);
            if (source == typeof(PhpString)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.PhpString), expr);
            if (source == typeof(PhpNumber)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.PhpNumber), expr);
            if (source == typeof(PhpArray)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.PhpArray), expr);
            if (source == typeof(PhpAlias)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.PhpAlias), expr);   // PhpValue.Create(PhpAlias)
            if (source == typeof(IndirectLocal)) return Expression.Call(expr, Cache.IndirectLocal.GetValue);   // IndirectLocal.GetValue()

            if (source.GetTypeInfo().IsValueType)
            {
                if (source == typeof(void)) return VoidAsConstant(expr, PhpValue.Null, Cache.Types.PhpValue);
                if (source == typeof(uint)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.Long), Expression.Convert(expr, typeof(long)));
                if (source == typeof(ulong)) return Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.UInt64), expr);

                throw new NotImplementedException(source.FullName);
            }
            else if (
                source == typeof(IPhpArray) ||
                source == typeof(object) ||
                typeof(ICollection).IsAssignableFrom(source) || // possibly PhpArray
                source == typeof(IPhpConvertible) ||
                source == typeof(IPhpEnumerable))
            {
                // convert dynamically to a PhpValue
                return Expression.Call(typeof(PhpValue).GetMethod("FromClr", Cache.Types.Object), expr);
            }
            else
            {
                // source is a class:
                return Expression.Call(typeof(PhpValue).GetMethod("FromClass", Cache.Types.Object), expr);
            }
        }

        private static Expression BindToClass(Expression expr)
        {
            var source = expr.Type;

            if (source == typeof(PhpValue)) return Expression.Call(expr, Cache.Operators.PhpValue_ToClass);
            if (source == typeof(PhpArray)) return Expression.Call(expr, Cache.Operators.PhpArray_ToClass);
            if (source == typeof(PhpNumber)) return Expression.Call(expr, Cache.Operators.PhpNumber_ToClass);

            if (!source.GetTypeInfo().IsValueType) return expr;

            throw new NotImplementedException(source.FullName);
        }

        static Expression BindAsObject(Expression expr)
        {
            var source = expr.Type;

            // PhpValue.AsObject
            if (source == typeof(PhpValue))
            {
                return Expression.Call(expr, Cache.Operators.PhpValue_AsObject);
            }

            var tinfo = source.GetTypeInfo();

            // <expr>
            if (!tinfo.IsValueType &&
                !tinfo.IsSubclassOf(typeof(PhpResource)) &&
                !tinfo.IsSubclassOf(typeof(IPhpArray)) &&
                !tinfo.IsSubclassOf(typeof(PhpString))
                )
            {
                return expr;
            }

            // NULL
            return Expression.Constant(null, Cache.Types.Object[0]);
        }

        static Expression BindAsReferenceType(Expression expr, Type target)
        {
            Debug.Assert(expr.Type != typeof(PhpAlias));

            // from PhpValue:
            if (expr.Type == typeof(PhpValue))
            {
                expr = Expression.Call(expr, Cache.Operators.PhpValue_GetValue);    // dereference
                expr = Expression.Property(expr, Cache.Properties.PhpValue_Object); // PhpValue.Object
            }

            // to System.Array:
            if (target.IsArray)
            {
                // Template: expr.ToArray().GetValues()
            }

            // just cast:
            return Expression.Convert(expr, target);
        }

        private static Expression BindAsArray(Expression expr, Type target)
        {
            var source = expr.Type;

            if (source == typeof(PhpArray) || source.IsSubclassOf(target)) return expr;
            if (source == typeof(PhpValue)) return Expression.Call(Cache.Operators.PhpValue_ToArrayOrThrow, expr);
            if (expr is ConstantExpression c && c.Value == null) return Expression.Constant(null, typeof(PhpArray));

            throw new NotImplementedException(source.FullName);
        }

        private static Expression BindAsCallable(Expression expr)
        {
            var source = expr.Type;

            if (typeof(IPhpCallable).IsAssignableFrom(source)) return expr;

            return Expression.Call(BindToValue(expr), Cache.Operators.PhpValue_AsCallable_RuntimeTypeHandle_Object, Expression.Default(typeof(RuntimeTypeHandle)), Expression.Default(typeof(object)));    // TODO: call context instead of default()
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

        internal static Expression VoidAsConstant(Expression expr, object value, Type type)
        {
            Debug.Assert(expr.Type == typeof(void));

            // block{ expr; return constant; }

            var constant = Expression.Constant(value, type);

            return Expression.Block(expr, constant);
        }

        #endregion

        #region BindDefault

        public static Expression BindDefault(Type t)
        {
            if (t == typeof(PhpValue)) return Expression.Field(null, Cache.Properties.PhpValue_Null);
            if (t == typeof(PhpNumber)) return Expression.Field(null, Cache.Properties.PhpNumber_Default);

            return Expression.Default(t);
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
            Debug.Assert(arg != null && target != null);

            var t = arg.Type;
            if (t == target)
            {
                return Expression.Constant(ConversionCost.Pass);
            }

            if (t == typeof(PhpAlias))
            {
                return BindCost(Expression.Field(arg, Cache.PhpAlias.Value), target); // PhpValue -> target
            }

            if (t == Cache.Types.IndirectLocal)
            {
                return BindCost(Expression.Call(arg, Cache.IndirectLocal.GetValue), target); // PhpValue -> target
            }

            if (target == typeof(PhpValue))
            {
                return Expression.Constant(ConversionCost.PassCostly);
            }

            if (target == typeof(PhpAlias))
            {
                //if (arg.Type.IsByRef && arg.Type == typeof(PhpValue))
                return Expression.Constant(ConversionCost.PassCostly);
                //else
                //    return Expression.Constant(ConversionCost.Warning);
            }

            if (target.IsNullable_T(out var nullable_t))
            {
                if (!t.IsValueType)
                {
                    // object -> Nullable<T> // only NULL is accepted
                    return Expression.Call(typeof(CostOf).GetMethod("ToNullable", Cache.Types.Object), arg);
                }

                if (t == typeof(PhpValue))
                {
                    // value is null|false ? PassCostly : costof(value -> T)
                    return Expression.Condition(
                        PhpValueIsNullOrFalse(arg),
                        Expression.Constant(ConversionCost.PassCostly),
                        BindCost(arg, nullable_t));
                }

                return BindCost(arg, nullable_t);
            }

            if (t == typeof(PhpValue)) return BindCostFromValue(arg, target);
            if (t == typeof(double) || t == typeof(float)) return Expression.Constant(BindCostFromDouble(arg, target));
            if (t == typeof(long) || t == typeof(int) || t == typeof(uint)) return Expression.Constant(BindCostFromLong(arg, target));
            if (t == typeof(bool)) return Expression.Constant(BindCostFromBool(arg, target));
            if (t == typeof(PhpNumber)) return BindCostFromNumber(arg, target);
            if (t == typeof(string)) return Expression.Constant(BindCostFromString(arg, target));
            if (t == typeof(PhpString)) return Expression.Constant(BindCostFromPhpString(arg, target));
            if (t == typeof(PhpArray)) return BindCostFromPhpArray(arg, target);

            // other types
            if (target.GetTypeInfo().IsAssignableFrom(t.GetTypeInfo())) return Expression.Constant(ConversionCost.Pass);

            // attempt to cast object:
            if (!t.IsValueType && ReflectionUtils.IsPhpClassType(target))
            {
                if (!t.IsInterface && !target.IsInterface && !target.IsAssignableFrom(t) && !t.IsAssignableFrom(target))
                {
                    // no way
                    return Expression.Constant(ConversionCost.NoConversion);
                }

                var toclass_T = typeof(CostOf).GetMethod("ToClass", Cache.Types.Object).MakeGenericMethod(target);
                return Expression.Call(toclass_T, arg); // CostOf.ToClass<T>(object)
            }

            // return Expression.Constant(ConversionCost.AttemptConvert);

            //
            throw new NotImplementedException($"costof({t} -> {target})");
        }

        static Expression BindCostFromValue(Expression arg, Type target)
        {
            // constant cases
            if (target == typeof(PhpValue) ||
                target == typeof(PhpAlias)) return Expression.Constant(ConversionCost.Pass);

            //
            if (target.IsEnum)
            {
                return Expression.Call(typeof(CostOf).GetMethod("ToInt64", arg.Type), arg);
            }

            if (target == typeof(PhpArray) || target == typeof(IPhpArray) || target == typeof(IPhpEnumerable) || target == typeof(PhpHashtable))
            {
                return Expression.Call(typeof(CostOf).GetMethod("ToPhpArray", arg.Type), arg);
            }

            //
            if (!target.IsValueType)
            {
                if (target.IsByRef)
                {
                    // ref T : cost(T) | ConversionCost.Warning
                    // CONSIDER: no warning if PhpValue is passed by ref as well and we implement this in CallBinder {ref PhpValue value; int tmp; foo(ref tmp); value = tmp;}
                    return Expression.Or(
                        BindCostFromValue(arg, target.GetElementType()),
                        Expression.Constant(ConversionCost.Warning),
                        Cache.Operators.Or_ConversionCost_ConversionCost);
                }

                if (ReflectionUtils.IsPhpClassType(target))
                {
                    var toclass_T = typeof(CostOf).GetMethod("ToClass", Cache.Types.PhpValue).MakeGenericMethod(target);
                    return Expression.Call(toclass_T, arg); // CostOf.ToClass<T>(arg)
                }

                // TODO
            }

            // fallback
            return Expression.Call(typeof(CostOf).GetMethod("To" + target.Name, arg.Type), arg);
        }

        static ConversionCost BindCostFromBool(Expression arg, Type target)
        {
            if (target == typeof(bool)) return (ConversionCost.Pass);

            if (target == typeof(PhpValue)) return (ConversionCost.PassCostly);

            if (target == typeof(double) || target == typeof(float) ||
                target == typeof(long) || target == typeof(int) || target == typeof(uint) ||
                target == typeof(string) || target == typeof(PhpString) ||
                target == typeof(PhpNumber)) return (ConversionCost.ImplicitCast);

            //
            return ConversionCost.NoConversion;
        }

        static ConversionCost BindCostFromDouble(Expression arg, Type target)
        {
            if (target == typeof(double) || target == typeof(float)) return (ConversionCost.Pass);
            if (target == typeof(PhpNumber)) return (ConversionCost.PassCostly);
            if (target == typeof(long) || target == typeof(int) || target == typeof(uint)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(bool)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(PhpValue)) return (ConversionCost.PassCostly);
            if (target == typeof(string) || target == typeof(PhpString)) return (ConversionCost.ImplicitCast);
            if (target == typeof(PhpArray)) return (ConversionCost.Warning);

            //throw new NotImplementedException($"costof(double -> {target})");
            return ConversionCost.NoConversion;
        }

        static ConversionCost BindCostFromLong(Expression arg, Type target)
        {
            if (target == typeof(int) || target == typeof(long) || target == typeof(uint)) return (ConversionCost.Pass);
            if (target == typeof(ulong)) return ConversionCost.ImplicitCast;
            if (target == typeof(PhpNumber)) return (ConversionCost.PassCostly);
            if (target == typeof(double) || target == typeof(float)) return (ConversionCost.ImplicitCast);
            if (target == typeof(bool)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(PhpValue)) return (ConversionCost.PassCostly);
            if (target == typeof(string) || target == typeof(PhpString)) return (ConversionCost.ImplicitCast);
            if (target == typeof(PhpArray)) return (ConversionCost.Warning);
            //if (target == typeof(object) || target == typeof(stdClass)) return ConversionCost.PassCostly;    // TODO: Error when passing to a PHP function

            // TODO: lookup for cast operator

            //throw new NotImplementedException($"costof(long -> {target})");
            return ConversionCost.NoConversion;
        }

        static Expression BindCostFromNumber(Expression arg, Type target)
        {
            if (target == typeof(double) || target == typeof(long) || target == typeof(int) || target == typeof(uint) || target == typeof(float))
            {
                return Expression.Call(typeof(CostOf).GetMethod("To" + target.Name, arg.Type), arg);
            }

            if (target == typeof(PhpNumber)) return Expression.Constant(ConversionCost.Pass);
            if (target == typeof(string)) return Expression.Constant(ConversionCost.ImplicitCast);
            if (target == typeof(bool)) return Expression.Constant(ConversionCost.LoosingPrecision);
            if (target == typeof(PhpValue)) return Expression.Constant(ConversionCost.PassCostly);

            return Expression.Constant(ConversionCost.Warning);
        }

        static ConversionCost BindCostFromString(Expression arg, Type target)
        {
            if (target == typeof(bool)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(long) || target == typeof(uint) || target == typeof(int)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(double) || target == typeof(float)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(PhpNumber)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(string)) return (ConversionCost.Pass);
            if (target == typeof(PhpString)) return (ConversionCost.PassCostly);
            if (target == typeof(PhpValue)) return (ConversionCost.PassCostly);
            if (target == typeof(PhpArray)) return (ConversionCost.Warning);
            if (target == typeof(object)) return ConversionCost.PassCostly;    // TODO: Error when passing to a PHP function

            var tinfo = target.GetTypeInfo();

            if (tinfo.IsAssignableFrom(typeof(IPhpCallable).GetTypeInfo())) return (ConversionCost.ImplicitCast); // string -> callable

            return ConversionCost.Error;
        }

        static ConversionCost BindCostFromPhpString(Expression arg, Type target)
        {
            if (target == typeof(bool)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(long) || target == typeof(uint) || target == typeof(int)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(double) || target == typeof(float)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(PhpNumber)) return (ConversionCost.LoosingPrecision);
            if (target == typeof(string)) return (ConversionCost.PassCostly);
            if (target == typeof(PhpString)) return (ConversionCost.Pass);
            if (target == typeof(PhpValue)) return (ConversionCost.PassCostly);
            if (target == typeof(PhpArray)) return (ConversionCost.Warning);
            if (target == typeof(object)) return ConversionCost.PassCostly;    // TODO: Error when passing to a PHP function

            var tinfo = target.GetTypeInfo();

            if (tinfo.IsAssignableFrom(typeof(IPhpCallable).GetTypeInfo())) return (ConversionCost.ImplicitCast); // string -> callable

            return ConversionCost.Error;
        }

        static Expression BindCostFromPhpArray(Expression arg, Type target)
        {
            if (target == typeof(PhpArray)) return Expression.Constant(ConversionCost.Pass);
            if (target == typeof(long) || target == typeof(int) || target == typeof(uint)) return Expression.Constant(ConversionCost.LoosingPrecision);
            if (target == typeof(string) || target == typeof(PhpString)) return Expression.Constant(ConversionCost.Warning);
            if (target == typeof(IPhpCallable)) return Expression.Constant(ConversionCost.LoosingPrecision);
            //if (target == typeof(object) || target == typeof(stdClass)) return Expression.Constant(ConversionCost.NoConversion);

            //throw new NotImplementedException($"costof(array -> {target})");
            return Expression.Constant(ConversionCost.NoConversion);
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

        /// <summary>
        /// Gets maximal value of given operands.
        /// </summary>
        public static ConversionCost Max(ConversionCost a, ConversionCost b) => (a > b) ? a : b;

        public static ConversionCost Or(ConversionCost a, ConversionCost b) => a | b;

        #region CostOf

        public static ConversionCost ToInt32(PhpNumber value) => ToInt64(value);

        public static ConversionCost ToInt64(PhpNumber value) => value.IsLong ? ConversionCost.Pass : ConversionCost.LoosingPrecision;

        public static ConversionCost ToDouble(PhpNumber value) => value.IsLong ? ConversionCost.ImplicitCast : ConversionCost.Pass;

        public static ConversionCost ToBoolean(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Boolean:
                    return ConversionCost.Pass;

                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                case PhpTypeCode.MutableString:
                case PhpTypeCode.String:
                    return ConversionCost.LoosingPrecision;

                case PhpTypeCode.PhpArray:
                    return ConversionCost.Warning;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToSByte(PhpValue value) => ToInt64(value);

        public static ConversionCost ToByte(PhpValue value) => ToInt64(value);

        public static ConversionCost ToInt16(PhpValue value) => ToInt64(value);

        public static ConversionCost ToUInt16(PhpValue value) => ToInt64(value);

        public static ConversionCost ToInt32(PhpValue value) => ToInt64(value);

        public static ConversionCost ToUInt32(PhpValue value) => ToInt64(value);

        public static ConversionCost ToUInt64(PhpValue value) => ToInt64(value);

        public static ConversionCost ToInt64(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                    return ConversionCost.Pass;

                case PhpTypeCode.Boolean:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.Double:
                case PhpTypeCode.MutableString:
                case PhpTypeCode.String:
                    return ConversionCost.LoosingPrecision;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToString(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                case PhpTypeCode.Boolean:
                case PhpTypeCode.Double:
                case PhpTypeCode.Object:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.MutableString:
                    return value.MutableString.ContainsBinaryData ? ConversionCost.LoosingPrecision : ConversionCost.PassCostly;

                case PhpTypeCode.String:
                    return ConversionCost.Pass;

                case PhpTypeCode.PhpArray:
                    return ConversionCost.Warning;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToChar(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.MutableString:
                    return value.MutableStringBlob.Length == 1
                        ? ConversionCost.Pass
                        : value.MutableStringBlob.Length == 0
                            ? ConversionCost.DefaultValue
                            : ConversionCost.LoosingPrecision;

                case PhpTypeCode.String:
                    return value.String.Length == 1
                        ? ConversionCost.Pass
                        : value.String.Length == 0
                            ? ConversionCost.DefaultValue
                            : ConversionCost.LoosingPrecision;

                case PhpTypeCode.Boolean:
                case PhpTypeCode.Double:
                case PhpTypeCode.Object:
                case PhpTypeCode.PhpArray:
                    return ConversionCost.Warning;

                case PhpTypeCode.Alias:
                    return ToChar(value.Alias.Value);

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToPhpString(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                case PhpTypeCode.Boolean:
                case PhpTypeCode.Double:
                case PhpTypeCode.Object:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.MutableString:
                    return ConversionCost.Pass;

                case PhpTypeCode.String:
                    return ConversionCost.PassCostly;

                case PhpTypeCode.PhpArray:
                    return ConversionCost.Warning;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToSingle(PhpValue value) => ToDouble(value);

        public static ConversionCost ToDecimal(PhpValue value) => ToDouble(value);

        public static ConversionCost ToDouble(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                case PhpTypeCode.Boolean:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.Double:
                    return ConversionCost.Pass;

                case PhpTypeCode.MutableString:
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
                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                    return ConversionCost.Pass;

                case PhpTypeCode.Boolean:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.MutableString:
                case PhpTypeCode.String:
                    return ConversionCost.LoosingPrecision;

                case PhpTypeCode.PhpArray:
                    return ConversionCost.Warning;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToPhpArray(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Long:
                case PhpTypeCode.Double:
                case PhpTypeCode.Boolean:
                case PhpTypeCode.MutableString:
                case PhpTypeCode.String:
                    return ConversionCost.Warning; // CONSIDER: NoConversion

                case PhpTypeCode.Null:
                    return ConversionCost.ImplicitCast;

                case PhpTypeCode.PhpArray:
                    return ConversionCost.Pass;

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToDateTime(PhpValue value) => ToClass<DateTime>(value);    // TODO: DateTime from long or string

        public static ConversionCost ToClass<T>(object value)
        {
            if (value == null)
            {
                return ConversionCost.DefaultValue;
            }

            var type = value.GetType();
            if (type == typeof(T))
            {
                return ConversionCost.Pass;
            }
            else if (typeof(T).IsAssignableFrom(type))
            {
                return ConversionCost.PassCostly;
            }
            else
            {
                return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToClass<T>(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Null:
                    return ConversionCost.DefaultValue;

                case PhpTypeCode.Object:
                    return ToClass<T>(value.Object);

                case PhpTypeCode.String:
                    if (typeof(T) == typeof(byte[])) // string -> byte[]
                    {
                        return ConversionCost.PassCostly;
                    }
                    return ConversionCost.NoConversion;
                case PhpTypeCode.MutableString:
                    if (typeof(T) == typeof(byte[])) // MutableString -> byte[]
                    {
                        return value.MutableString.ContainsBinaryData ? ConversionCost.Pass : ConversionCost.PassCostly;
                    }
                    return ConversionCost.NoConversion;

                case PhpTypeCode.Alias:
                    return ToClass<T>(value.Alias.Value);

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToIPhpCallable(object value)
        {
            return (value is IPhpCallable) ? ConversionCost.Pass : ConversionCost.NoConversion;
        }

        public static ConversionCost ToIPhpCallable(PhpValue value)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.String:
                case PhpTypeCode.MutableString:
                case PhpTypeCode.PhpArray:
                    return ConversionCost.LoosingPrecision;

                case PhpTypeCode.Object:
                    return ToIPhpCallable(value.Object);

                case PhpTypeCode.Alias:
                    return ToIPhpCallable(value.Alias.Value);

                default:
                    return ConversionCost.NoConversion;
            }
        }

        public static ConversionCost ToIntPtr(PhpValue value)
        {
            // TODO: once we'll be able to store structs
            return ConversionCost.NoConversion;
        }

        public static ConversionCost ToNullable(object obj)
        {
            return obj == null ? ConversionCost.PassCostly : ConversionCost.NoConversion;
        }

        #endregion
    }
}
