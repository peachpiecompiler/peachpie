using Pchp.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Dynamic
{
    internal static class BinderHelpers
    {
        public static bool IsParamsParameter(this ParameterInfo p)
        {
            return p.ParameterType.IsArray && p.CustomAttributes.Any(attr => attr.AttributeType == typeof(ParamArrayAttribute));
        }

        /// <summary>
        /// Determines the parameter is considered as implicitly passed by runtime.
        /// </summary>
        public static bool IsImplicitParameter(this ParameterInfo p)
        {
            return p.IsContextParameter() || p.IsImportLocalsParameter() || p.IsImportCallerArgsParameter() || p.IsImportCallerClassParameter();

            // TODO: classCtx, <this>
        }

        public static bool IsContextParameter(this ParameterInfo p)
        {
            return p.Position == 0
                && p.ParameterType == typeof(Context)
                && (p.Name == "ctx" || p.Name == "<ctx>" || p.Name == "context" || p.Name == ".ctx");
        }

        public static bool IsImportLocalsParameter(this ParameterInfo p)
        {
            return
                p.ParameterType == typeof(PhpArray) &&
                p.GetCustomAttribute(typeof(ImportLocalsAttribute)) != null;
        }

        public static bool IsImportCallerArgsParameter(this ParameterInfo p)
        {
            return
                p.ParameterType == typeof(PhpValue).MakeArrayType() &&
                p.GetCustomAttribute(typeof(ImportCallerArgsAttribute)) != null;
        }

        public static bool IsImportCallerClassParameter(this ParameterInfo p)
        {
            return
                (p.ParameterType == typeof(string) || p.ParameterType == typeof(RuntimeTypeHandle) || p.ParameterType == typeof(PhpTypeInfo)) &&
                p.GetCustomAttribute(typeof(ImportCallerClassAttribute)) != null;
        }

        /// <summary>
        /// Determines the parameter does not have a default value explicitly specified.
        /// </summary>
        public static bool IsMandatoryParameter(this ParameterInfo p)
        {
            return !p.HasDefaultValue && !p.IsOptional && !p.IsParamsParameter();
        }

        /// <summary>
        /// Gets <see cref="Context.GetStatic{T}()"/> method bound to a type.
        /// </summary>
        public static MethodInfo GetStatic_T_Method(Type t)
        {
            return typeof(Context).GetMethod("GetStatic", Cache.Types.Empty).MakeGenericMethod(t);
        }

        public static void TargetAsObject(DynamicMetaObject target, out Expression target_expr, out object target_value, ref BindingRestrictions restrictions)
        {
            target_expr = target.Expression;
            target_value = target.Value;

            if (target_value == null)
            {
                restrictions =  restrictions.Merge(BindingRestrictions.GetInstanceRestriction(target.Expression, Expression.Constant(null)));
                return;
            }

            for (;;)
            {
                if (target_expr.Type == typeof(PhpValue))
                {
                    // Template: target.Object // target.IsObject
                    var value = (PhpValue)target_value;
                    if (value.IsNull)
                    {
                        restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Property(target_expr, "IsNull")));

                        target_value = null;
                        target_expr = Expression.Constant(null, Cache.Types.Object[0]);
                        break;
                    }
                    else if (value.IsObject)
                    {
                        restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Property(target_expr, "IsObject")));

                        target_value = value.Object;
                        target_expr = Expression.Property(target_expr, "Object");
                        break;
                    }
                    else if (value.IsAlias)
                    {
                        restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Property(target_expr, "IsAlias")));

                        target_value = value.Alias;
                        target_expr = Expression.Property(target_expr, "Alias");
                        continue;
                    }
                    else if (value.IsScalar)
                    {
                        restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Property(target_expr, "IsScalar")));

                        target_value = null;
                        target_expr = Expression.Constant(null, Cache.Types.Object[0]);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else if (target_expr.Type == typeof(PhpAlias))
                {
                    // dereference
                    target_value = ((PhpAlias)target_value).Value;
                    target_expr = Expression.PropertyOrField(target_expr, "Value");
                    continue;
                }

                //
                break;
            }
        }

        public static Expression EnsureNotNullPhpArray(Expression variable)
        {
            // variable ?? (variable = [])
            return Expression.Coalesce(
                variable,
                Expression.Assign(variable, Expression.New(typeof(PhpArray))));
        }

        public static Expression NewPhpArray(Expression ctx, IEnumerable<Expression> values)
        {
            return Expression.Call(
                typeof(PhpArray), "New", Cache.Types.Empty, // PhpArray.New(values[])
                Expression.NewArrayInit(typeof(PhpValue), values.Select(x => ConvertExpression.Bind(x, typeof(PhpValue), ctx))));
        }

        /// <summary>
        /// Find field corresponding to object's runtime fields.
        /// </summary>
        public static FieldInfo LookupRuntimeFields(Type target)
        {
            return target.GetRuntimeFields().FirstOrDefault(ReflectionUtils.IsRuntimeFields);
        }

        static Expression BindAccess(Expression expr, Expression ctx, AccessFlags access, Expression rvalue)
        {
            if (access.EnsureObject())
            {
                if (expr.Type == typeof(PhpAlias))
                {
                    // ((PhpAlias)fld).EnsureObject()
                    expr = Expression.Call(expr, Cache.Operators.PhpAlias_EnsureObject);
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // ((PhpValue)fld).EnsureObject()
                    expr = Expression.Call(expr, Cache.Operators.PhpValue_EnsureObject);
                }
                else
                {
                    // getter // TODO: ensure it is not null
                    Debug.Assert(!expr.Type.GetTypeInfo().IsValueType);
                }
            }
            else if (access.EnsureArray())
            {
                if (expr.Type == typeof(PhpAlias))
                {
                    // ((PhpAlias)fld).EnsureArray()
                    expr = Expression.Call(expr, Cache.Operators.PhpAlias_EnsureArray);
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // ((PhpValue)fld).EnsureArray()
                    expr = Expression.Call(expr, Cache.Operators.PhpValue_EnsureArray);
                }
                else if (expr.Type == typeof(PhpArray))
                {
                    // (PhpArray)fld // TODO: ensure it is not null
                }
                else
                {
                    // getter
                }
            }
            else if (access.EnsureAlias())
            {
                if (expr.Type == typeof(PhpAlias))
                {
                    // (PhpAlias)getter
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // ((PhpValue)fld).EnsureAlias()
                    expr = Expression.Call(expr, Cache.Operators.PhpValue_EnsureAlias);
                }
                else
                {
                    // getter // cannot read as reference
                }
            }
            else if (access.WriteAlias())
            {
                // write alias

                Debug.Assert(rvalue.Type == typeof(PhpAlias));
                rvalue = ConvertExpression.Bind(rvalue, typeof(PhpAlias), ctx);

                if (expr.Type == typeof(PhpAlias))
                {
                    // ok    
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // fld = PhpValue.Create(alias)
                    rvalue = Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.PhpAlias), rvalue);
                }
                else
                {
                    // fld is not aliasable
                    Debug.Assert(false, "Cannot assign aliased value to field of type " + expr.Type.ToString());
                    rvalue = ConvertExpression.Bind(rvalue, expr.Type, ctx);
                }

                expr = Expression.Assign(expr, rvalue);
            }
            else if (access.Unset())
            {
                Debug.Assert(rvalue == null);

                expr = Expression.Assign(expr, ConvertExpression.BindDefault(expr.Type));
            }
            else if (access.Write())
            {
                // write by value

                if (expr.Type == typeof(PhpAlias))
                {
                    // Template: fld.Value = (PhpValue)value
                    expr = Expression.Assign(Expression.PropertyOrField(expr, "Value"), ConvertExpression.Bind(rvalue, typeof(PhpValue), ctx));
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // Template: Operators.SetValue(ref fld, (PhpValue)value)
                    expr = Expression.Call(Cache.Operators.SetValue_PhpValueRef_PhpValue, expr, ConvertExpression.Bind(rvalue, typeof(PhpValue), ctx));
                }
                else
                {
                    // Template: fld = value
                    // default behaviour by value to value
                    expr = Expression.Assign(expr, ConvertExpression.Bind(rvalue, expr.Type, ctx));
                }
            }

            //
            return expr;
        }

        static Expression BindArrayAccess(Expression arr, Expression key, Expression ctx, AccessFlags access, Expression rvalue)
        {
            Debug.Assert(key.Type == typeof(IntStringKey));

            if (access.EnsureObject())
            {
                // (arr ?? arr = []).EnsureItemObject(key)
                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_EnsureItemObject, key);
            }
            else if (access.EnsureArray())
            {
                // (arr ?? arr = []).EnsureItemArray(key)
                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_EnsureItemArray, key);
            }
            else if (access.EnsureAlias())
            {
                // (arr ?? arr = []).EnsureItemAlias(key)
                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_EnsureItemAlias, key);
            }
            else if (access.WriteAlias())
            {
                Debug.Assert(rvalue.Type == typeof(PhpAlias));
                rvalue = ConvertExpression.Bind(rvalue, typeof(PhpAlias), ctx);

                // (arr ?? arr = []).SetItemAlias(key, value)
                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_SetItemAlias, key, rvalue);
            }
            else if (access.Unset())
            {
                Debug.Assert(rvalue == null);

                // remove key

                // arr.RemoveKey(name)
                // TODO: if (arr != null)
                return Expression.Call(arr, Cache.Operators.PhpArray_RemoveKey, key);
            }
            else if (access.Write())
            {
                rvalue = ConvertExpression.Bind(rvalue, typeof(PhpValue), ctx);

                return Expression.Call(
                    EnsureNotNullPhpArray(arr),
                    Cache.Operators.PhpArray_SetItemValue, key, rvalue);
            }
            else
            {
                // read
                // TODO: (arr != null) ? arr[key] : (quiet ? void : ERROR)
                return Expression.Call(arr, Cache.Operators.PhpArray_GetItemValue, key);
            }
        }

        public static PhpMethodInfo FindMagicMethod(PhpTypeInfo type, TypeMethods.MagicMethods magic)
        {
            return (PhpMethodInfo)type.RuntimeMethods[magic];
        }

        static Expression BindMagicMethod(PhpTypeInfo type, Type classCtx, Expression target, Expression ctx, TypeMethods.MagicMethods magic, string field, Expression rvalue = null)
        {
            var m = FindMagicMethod(type, magic);
            if (m != null)
            {
                var methods = m.Methods.Length == 1
                    ? (m.Methods[0].IsVisible(classCtx) ? m.Methods : Array.Empty<MethodInfo>())    // optimization for array[1]
                    : m.Methods.Where(x => x.IsVisible(classCtx)).ToArray();

                if (methods.Length != 0)
                {
                    switch (magic)
                    {
                        case TypeMethods.MagicMethods.__set:
                            // __set(name, value)
                            return OverloadBinder.BindOverloadCall(typeof(void), target, methods, ctx, new Expression[] { Expression.Constant(field), rvalue });

                        default:
                            // __get(name), __unset(name), __isset(name)
                            return OverloadBinder.BindOverloadCall(methods[0].ReturnType, target, methods, ctx, new Expression[] { Expression.Constant(field) });
                    }
                }
                else
                {
                    // TODO: ERR inaccessible
                }
            }

            return null;
        }

        public static Expression BindField(PhpTypeInfo type, Type classCtx, Expression target, string field, Expression ctx, AccessFlags access, Expression rvalue)
        {
            if (access.Write() != (rvalue != null))
            {
                throw new ArgumentException();
            }

            // lookup a declared field
            for (var t = type; t != null; t = t.BaseType)
            {
                foreach (var p in t.DeclaredFields.GetPhpProperties(field))
                {
                    if (p.IsStatic == (target == null) && p.IsVisible(classCtx))
                    {
                        return BindAccess(p.Bind(ctx, target), ctx, access, rvalue);
                    }
                }
            }

            //
            // runtime fields
            //

            if (type.RuntimeFieldsHolder != null)   // we don't handle magic methods without the runtime fields
            {
                var runtimeflds = Expression.Field(target, type.RuntimeFieldsHolder);       // Template: target->__runtime_fields
                var fieldkey = Expression.Constant(new IntStringKey(field));                // Template: IntStringKey(field)
                var resultvar = Expression.Variable(Cache.Types.PhpValue[0], "result");     // Template: PhpValue result;

                // Template: runtimeflds != null && runtimeflds.TryGetValue(field, out result)
                var trygetfield = Expression.AndAlso(Expression.ReferenceNotEqual(runtimeflds, Expression.Constant(null)), Expression.Call(runtimeflds, Cache.Operators.PhpArray_TryGetValue, fieldkey, resultvar));
                var containsfield = Expression.AndAlso(Expression.ReferenceNotEqual(runtimeflds, Expression.Constant(null)), Expression.Call(runtimeflds, Cache.Operators.PhpArray_ContainsKey, fieldkey));

                Expression result;

                //
                if (access.EnsureObject())
                {
                    // (object)target->field->

                    // Template: runtimeflds.EnsureObject(key)
                    result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_EnsureItemObject, fieldkey);

                    var __get = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__get, field, null);
                    if (__get != null)
                    {
                        // Template: runtimeflds.Contains(key) ? runtimeflds.EnsureObject(key) : ( __get(key) ?? runtimeflds.EnsureObject(key))
                        return Expression.Condition(containsfield,
                                Expression.Call(runtimeflds, Cache.Operators.PhpArray_EnsureItemObject, fieldkey),
                                InvokeHandler(ctx, target, field, __get, access, result, typeof(object)));
                    }
                    else
                    {
                        return result;
                    }
                }
                else if (access.EnsureArray())
                {
                    // (IPhpArray)target->field[] =
                    result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_EnsureItemArray, fieldkey);

                    var __get = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__get, field, null);
                    if (__get != null)
                    {
                        // Template: runtimeflds.Contains(key) ? runtimeflds.EnsureArray(key) : ( __get(key) ?? runtimeflds.EnsureArray(key))
                        return Expression.Condition(containsfield,
                                Expression.Call(runtimeflds, Cache.Operators.PhpArray_EnsureItemArray, fieldkey),
                                InvokeHandler(ctx, target, field, __get, access, result, typeof(IPhpArray)));
                    }
                    else
                    {
                        // runtimeflds.EnsureItemArray(key)
                        return result;
                    }
                }
                else if (access.EnsureAlias())
                {
                    // (PhpAlias)&target->field

                    result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_EnsureItemAlias, fieldkey);

                    var __get = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__get, field, null);
                    if (__get != null)
                    {
                        // Template: runtimeflds.Contains(key) ? runtimeflds.EnsureItemAlias(key) : ( __get(key) ?? runtimeflds.EnsureItemAlias(key))
                        return Expression.Condition(containsfield,
                                Expression.Call(runtimeflds, Cache.Operators.PhpArray_EnsureItemAlias, fieldkey),
                                InvokeHandler(ctx, target, field, __get, access, result, typeof(PhpAlias)));
                    }
                    else
                    {
                        // runtimeflds.EnsureItemAlias(key)
                        return result;
                    }
                }
                else if (access.Unset())
                {
                    // unset(target->field)
                    // Template: if (!runtimeflds.RemoveKey(key)) __unset(key)

                    var removekey = Expression.Call(runtimeflds, Cache.Operators.PhpArray_RemoveKey, fieldkey);
                    var __unset = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__unset, field, null);
                    if (__unset != null)
                    {
                        return Expression.IfThen(
                            Expression.OrElse(Expression.ReferenceEqual(runtimeflds, Expression.Constant(null)), Expression.IsFalse(removekey)),
                            InvokeHandler(ctx, target, field, __unset, access, Expression.Block(), typeof(void)));
                    }
                    else
                    {
                        // if (runtimeflds != null) runtimeflds.RemoveKey(key)
                        return Expression.IfThen(
                            Expression.ReferenceNotEqual(runtimeflds, Expression.Constant(null)),
                            removekey);
                    }
                }
                else if (access.Write())
                {
                    var __set = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__set, field, rvalue);

                    if (access.WriteAlias())
                    {
                        // target->field = (PhpAlias)&rvalue
                        Debug.Assert(rvalue.Type == typeof(PhpAlias));
                        rvalue = ConvertExpression.Bind(rvalue, typeof(PhpAlias), ctx);

                        // EnsureNotNull(runtimeflds).SetItemAlias(key, rvalue)
                        result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_SetItemAlias, fieldkey, rvalue);

                        if (__set != null)
                        {
                            // if (ContainsKey(key)) ? runtimeflds.SetItemAlias(rvalue) : (__set(key, rvalue) ?? runtimeflds.SetItemAlias(key, rvalue)
                            return Expression.Condition(containsfield,
                                    Expression.Call(runtimeflds, Cache.Operators.PhpArray_SetItemAlias, fieldkey, rvalue),
                                    InvokeHandler(ctx, target, field, __set, access, result, typeof(void)));
                        }
                        else
                        {
                            return result;
                        }
                    }
                    else
                    {
                        // target->field = rvalue
                        rvalue = ConvertExpression.Bind(rvalue, typeof(PhpValue), ctx);

                        /* Template:
                         * return runtimeflds != null && runtimeflds.ContainsKey(field)
                         *   ? runtimeflds.SetItemValue(key, rvalue)
                         *   : (__set(field, value) ?? runtimeflds.SetItemValue(key, value))
                         */

                        result = Expression.Call(EnsureNotNullPhpArray(runtimeflds), Cache.Operators.PhpArray_SetItemValue, fieldkey, rvalue);

                        if (__set != null)
                        {
                            return Expression.Condition(containsfield,
                                Expression.Call(runtimeflds, Cache.Operators.PhpArray_SetItemValue, fieldkey, rvalue),
                                InvokeHandler(ctx, target, field, __set, access, result, typeof(void)));
                        }
                        else
                        {
                            return result;
                        }
                    }
                }
                else if (access.Isset())
                {
                    // isset(target->field)

                    var __isset = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__isset, field, null);

                    // Template: TryGetField(result) ? result : (__isset(key) ?? null)
                    result = Expression.Condition(trygetfield,
                        resultvar,
                        InvokeHandler(ctx, target, field, __isset, access));
                }
                else
                {
                    // = target->field

                    /* Template:
                     * return runtimeflds.TryGetValue(field, out result) ? result : (__get(field) ?? ERR);
                     */
                    var __get = BindMagicMethod(type, classCtx, target, ctx, TypeMethods.MagicMethods.__get, field, null);
                    result = Expression.Condition(trygetfield,
                        resultvar,
                        InvokeHandler(ctx, target, field, __get, access));    // TODO: @default = { ThrowError; return null; }
                }

                //
                return Expression.Block(result.Type, new[] { resultvar }, result);
            }

            // TODO: IDynamicMetaObject

            //
            return null;
        }

        public static Expression BindClassConstant(PhpTypeInfo type, Type classCtx, string constName, Expression ctx)
        {
            var p = type.GetDeclaredConstant(constName);
            if (p != null && p.IsVisible(classCtx))
            {
                return p.Bind(ctx, null);
            }

            //
            return null;
        }

        /// <summary>
        /// Binds recursion check for property magic method.
        /// </summary>
        static Expression InvokeHandler(Expression ctx, Expression target, string field, Expression getter, AccessFlags access, Expression @default = null, Type resultType = null)
        {
            // default
            resultType = resultType ?? Cache.Types.PhpValue[0];
            @default = @default ?? Expression.Field(null, Cache.Properties.PhpValue_Null);   // TODO: ERR field not found
            @default = ConvertExpression.Bind(@default, resultType, ctx);

            if (getter == null)
            {
                return @default;
            }
            else
            {
                /* Template:
                 * var token;
                 * try {
                 *   return (token = new Context.RecursionCheckToken(_ctx, target, access))).IsInRecursion)
                 *     ? default
                 *     : getter;
                 * } finally {
                 *   token.Dispose();
                 * }
                 */

                int subkey = access.Write() ? 1 : access.Unset() ? 2 : access.Isset() ? 3 : 4;  // recursion prevention scope

                // Template: RecursionCheckToken token;
                var tokenvar = Expression.Variable(typeof(Context.RecursionCheckToken), "token");

                // Template: token = new RecursionCheckToken(_ctx, (object)target, (int)access))
                var tokenassign = Expression.Assign(tokenvar, Expression.New(Cache.RecursionCheckToken.ctor_ctx_object_int,
                    ctx, Expression.Convert(target, Cache.Types.Object[0]), Expression.Constant(subkey)));

                //
                return Expression.Block(resultType,
                    new[] { tokenvar},
                    Expression.TryFinally(
                        Expression.Condition(Expression.Property(tokenassign, Cache.RecursionCheckToken.IsInRecursion),
                            @default,
                            ConvertExpression.Bind(getter, resultType, ctx)),
                        Expression.Call(tokenvar, Cache.RecursionCheckToken.Dispose)
                    ));
            }
        }

        public static Expression BindToCall(Expression instance, MethodBase method, Expression ctx, OverloadBinder.ArgumentsBinder args)
        {
            Debug.Assert(method is MethodInfo || method is ConstructorInfo);

            var ps = method.GetParameters();
            var boundargs = new Expression[ps.Length];

            int argi = 0;

            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                if (argi == 0 && p.IsImplicitParameter())
                {
                    if (p.IsContextParameter())
                    {
                        boundargs[i] = ctx;
                    }
                    else if (p.IsImportLocalsParameter())
                    {
                        // no way we can implement this
                        throw new NotImplementedException();    // TODO: empty array & report warning
                    }
                    else if (p.IsImportCallerArgsParameter())
                    {
                        // we don't have this info
                        throw new NotImplementedException();    // TODO: empty array & report warning
                    }
                    else if (p.IsImportCallerClassParameter())
                    {
                        // TODO: pass classctx from the callsite
                        throw new NotImplementedException();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    if (i == ps.Length - 1 && p.IsParamsParameter())
                    {
                        var element_type = p.ParameterType.GetElementType();
                        boundargs[i] = args.BindParams(argi, element_type);
                        break;
                    }
                    else
                    {
                        boundargs[i] = args.BindArgument(argi, p);
                    }

                    //
                    argi++;
                }
            }

            //
            Debug.Assert(boundargs.All(x => x != null));

            //
            if (method.IsStatic)
            {
                instance = null;
            }

            //
            if (method.IsConstructor)
            {
                return Expression.New((ConstructorInfo)method, boundargs);
            }

            if (instance != null)
            {
                instance = Expression.Convert(instance, method.DeclaringType);
            }

            if (instance != null && method.IsVirtual)   // NOTE: only needed for parent::foo(), static::foo() and self::foo() ?
            {
                // Ugly hack here,
                // we NEED to call the method nonvirtually, but LambdaCompiler emits .callvirt always and there is no way how to change it (except we can emit all the stuff by ourselfs).
                // We use DynamicMethod to emit .call inside, and use its MethodInfo which is static.
                // LambdaCompiler generates .call to static DynamicMethod which calls our method via .call as well,
                // after all the inlining, there should be no overhead.

                method = WrapInstanceMethodToStatic((MethodInfo)method);

                //
                var newargs = new Expression[boundargs.Length + 1];
                newargs[0] = instance;
                Array.Copy(boundargs, 0, newargs, 1, boundargs.Length);
                boundargs = newargs;
                instance = null;
            }

            //
            return Expression.Call(instance, (MethodInfo)method, boundargs);
        }

        /// <summary>
        /// Builds MethodInfo as a static method calling an instance method nonvirtually inside.
        /// </summary>
        static MethodInfo WrapInstanceMethodToStatic(MethodInfo method)
        {
            if (method.IsStatic)
            {
                return method;
            }

            var ps = method.GetParameters();

            // dynamic method parameters
            var dtypes = new Type[ps.Length + 1];
            dtypes[0] = method.DeclaringType;   // target
            for (int i = 0; i < ps.Length; i++)
            {
                dtypes[i + 1] = ps[i].ParameterType;    // parameter_i
            }

            // dynamic method
            var d = new DynamicMethod("<>." + method.Name, method.ReturnType, dtypes, method.DeclaringType, true);

            // return ARG0.{method}(ARG1, ..., ARGN)
            var il = d.GetILGenerator();

            for (int i = 0; i < dtypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.EmitCall(OpCodes.Call, method, null);    // .call instead of .callvirt
            il.Emit(OpCodes.Ret);

            //
            return d;
        }

        public static PhpCallable BindToPhpCallable(MethodBase target) => BindToPhpCallable(new[] { target });

        public static PhpCallable BindToPhpCallable(MethodBase[] targets)
        {
            Debug.Assert(targets.All(t => t.IsStatic), "Only static methods can be bound to PhpCallable delegate.");

            // (Context ctx, PhpValue[] arguments)
            var ps = new ParameterExpression[] { Expression.Parameter(typeof(Context), "ctx"), Expression.Parameter(typeof(PhpValue[]), "argv") };

            // invoke targets
            var invocation = OverloadBinder.BindOverloadCall(typeof(PhpValue), null, targets, ps[0], ps[1]);
            Debug.Assert(invocation.Type == typeof(PhpValue));

            // compile & create delegate
            var lambda = Expression.Lambda<PhpCallable>(invocation, targets[0].Name + "#" + targets.Length, true, ps);
            return lambda.Compile();
        }

        public static PhpInvokable BindToPhpInvokable(MethodInfo[] methods)
        {
            // (Context ctx, object target, PhpValue[] arguments)
            var ps = new ParameterExpression[] {
                Expression.Parameter(typeof(Context), "ctx"),
                Expression.Parameter(typeof(object), "target"),
                Expression.Parameter(typeof(PhpValue[]), "argv") };

            // invoke targets
            var invocation = OverloadBinder.BindOverloadCall(typeof(PhpValue), ps[1], methods, ps[0], ps[2]);
            Debug.Assert(invocation.Type == typeof(PhpValue));

            // compile & create delegate
            var lambda = Expression.Lambda<PhpInvokable>(invocation, methods[0].Name + "#" + methods.Length, true, ps);
            return lambda.Compile();
        }

        public static TObjectCreator BindToCreator(Type type, ConstructorInfo[] ctors)
        {
            Debug.Assert(ctors.All(ctor => ctor is ConstructorInfo));
            Debug.Assert(ctors.All(ctor => ctor.DeclaringType == type));

            // (Context ctx, PhpValue[] arguments)
            var ps = new ParameterExpression[] { Expression.Parameter(typeof(Context), "ctx"), Expression.Parameter(typeof(PhpValue[]), "argv") };

            if (ctors.Length != 0)
            {
                // invoke targets
                var invocation = OverloadBinder.BindOverloadCall(type, null, ctors, ps[0], ps[1]);
                Debug.Assert(invocation.Type == type);

                // compile & create delegate
                var lambda = Expression.Lambda<TObjectCreator>(invocation, ctors[0].Name + "#" + ctors.Length, true, ps);
                return lambda.Compile();
            }
            else
            {
                // TODO: lambda {error; NULL;}
                throw new ArgumentException("No constructor accessible for " + type.FullName);
            }
        }
    }
}
