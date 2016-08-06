using Pchp.Core.Reflection;
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
            return p.IsContextParameter();

            // TODO: <locals>, <caller>, <this>
        }

        public static bool IsContextParameter(this ParameterInfo p)
        {
            return p.Position == 0
                && p.ParameterType == typeof(Context)
                && (p.Name == "ctx" || p.Name == "<ctx>" || p.Name == "context");
        }

        /// <summary>
        /// Determines the parameter does not have a default value explicitly specified.
        /// </summary>
        public static bool IsMandatoryParameter(this ParameterInfo p)
        {
            return !p.HasDefaultValue && !p.IsOptional && !p.IsParamsParameter();
        }

        /// <summary>
        /// Gets <see cref="Context.GetStatic{T}"/> method bound to a type.
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
                throw new NotImplementedException();    // TODO: call on NULL
            }

            for (;;)
            {
                if (target_expr.Type == typeof(PhpValue))
                {
                    // Template: target.Object // target.IsObject
                    var value = (PhpValue)target_value;
                    if (value.IsNull)
                    {
                        throw new NotImplementedException();    // TODO: call on NULL
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
                    else
                    {
                        throw new NotImplementedException();    // TODO: scalar
                    }
                }
                else if (target_expr.Type == typeof(PhpAlias))
                {
                    // dereference
                    target_value = (PhpAlias)target_value;
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

        /// <summary>
        /// Find field corresponding to object's runtime fields.
        /// </summary>
        public static FieldInfo LookupRuntimeFields(Type target)
        {
            foreach (var fld in target.GetRuntimeFields())
            {
                // TODO: lookup custom attribute [CompilerGenerated]
                if (fld.Name == "__peach__runtimeFields" || fld.Name == "<runtime_fields>")
                {
                    if (fld.FieldType == typeof(PhpArray) && !fld.IsPublic && !fld.IsStatic)
                    {
                        return fld;
                    }
                }
            }

            //
            return null;
        }

        static Expression BindAccess(Expression expr, Expression ctx, AccessFlags access, Expression rvalue)
        {
            if (access.EnsureObject())
            {
                if (expr.Type == typeof(PhpAlias))
                {
                    // ((PhpAlias)fld).EnsureObject(ctx)
                    expr = Expression.Call(expr, Cache.Operators.PhpAlias_EnsureObject_Context);
                }
                else if (expr.Type == typeof(PhpValue))
                {
                    // ((PhpValue)fld).EnsureObject(ctx)
                    expr = Expression.Call(expr, Cache.Operators.PhpValue_EnsureObject_Context);
                }
                else
                {
                    // getter // TODO: ensure it is not null
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

        public static Expression BindField(PhpTypeInfo type, Type classCtx, Expression target, string field, Expression ctx, AccessFlags access, Expression rvalue)
        {
            if (access.Write() != (rvalue != null))
            {
                throw new ArgumentException();
            }

            // lookup a declared field
            for (var t = type; t != null; t = t.BaseType)
            {
                var expr = t.DeclaredFields.Bind(field, classCtx, target, ctx, (target != null) ? TypeFields.FieldKind.InstanceField : TypeFields.FieldKind.StaticField);
                if (expr != null)
                {
                    return BindAccess(expr, ctx, access, rvalue);
                }
            }

            // lookup __get, __set, ...
            TypeMethods.MagicMethods magic;
            if (access.Write())
                magic = TypeMethods.MagicMethods.__set;
            else if (access.Unset())
                magic = TypeMethods.MagicMethods.__unset;
            else if (access.Isset())
                magic = TypeMethods.MagicMethods.__isset;
            else
                magic = TypeMethods.MagicMethods.__get;

            for (var t = type; t != null; t = t.BaseType)
            {
                var m = (PhpMethodInfo)t.DeclaredMethods[magic];
                if (m != null)
                {
                    if (m.Methods.Length == 1 && m.Methods[0].IsVisible(classCtx))
                    {
                        if (access.Write())
                        {
                            // __set(name, value)
                            return OverloadBinder.BindOverloadCall(rvalue.Type, target, m.Methods, ctx, new Expression[] { Expression.Constant(field), rvalue });
                        }
                        else
                        {
                            // __get(name), __unset(name), __isset(name)
                            return OverloadBinder.BindOverloadCall(m.Methods[0].ReturnType, target, m.Methods, ctx, new Expression[] { Expression.Constant(field) });
                        }
                    }

                    break;
                }
            }

            // runtime fields
            var __runtimeflds = type.RuntimeFieldsHolder;
            if (__runtimeflds != null)
            {
                var __runtimeflds_field = Expression.Field(target, __runtimeflds);
                var key = Expression.Constant(new IntStringKey(field));

                return BindArrayAccess(__runtimeflds_field, key, ctx, access, rvalue);
            }

            // TODO: dynamic

            //
            return null;
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
                        boundargs[i] = ctx;
                    else
                        throw new NotImplementedException();
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

            //
            return Expression.Call(instance, (MethodInfo)method, boundargs);
        }

        public static PhpCallable BindToPhpCallable(MethodBase target) => BindToPhpCallable(new[] { target });

        public static PhpCallable BindToPhpCallable(MethodBase[] targets)
        {
            // (Context ctx, PhpValue[] arguments)
            var ps = new ParameterExpression[] { Expression.Parameter(typeof(Context), "ctx"), Expression.Parameter(typeof(PhpValue[]), "argv") };

            // invoke targets
            var invocation = OverloadBinder.BindOverloadCall(typeof(PhpValue), null, targets, ps[0], ps[1]);
            Debug.Assert(invocation.Type == typeof(PhpValue));

            // compile & create delegate
            var lambda = Expression.Lambda<PhpCallable>(invocation, targets[0].Name + "#" + targets.Length, true, ps);
            return lambda.Compile();
        }

        public static TObjectCreator BindToCreator(ConstructorInfo[] ctors)
        {
            // (Context ctx, PhpValue[] arguments)
            var ps = new ParameterExpression[] { Expression.Parameter(typeof(Context), "ctx"), Expression.Parameter(typeof(PhpValue[]), "argv") };

            // invoke targets
            var invocation = OverloadBinder.BindOverloadCall(typeof(object), null, ctors, ps[0], ps[1]);
            Debug.Assert(invocation.Type == typeof(object));

            // compile & create delegate
            var lambda = Expression.Lambda<TObjectCreator>(invocation, ctors[0].Name + "#" + ctors.Length, true, ps);
            return lambda.Compile();
        }
    }
}
