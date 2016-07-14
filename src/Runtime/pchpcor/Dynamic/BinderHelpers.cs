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
            return Expression.IfThen(
                Expression.ReferenceEqual(variable, Expression.Constant(null)),
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

        public static Expression BindToCall(Expression instance, MethodBase method, Expression ctx, OverloadBinder.ArgumentsBinder args)
        {
            Debug.Assert(method is MethodInfo);

            // TODO: handle vararg, handle missing mandatory args
            
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
    }
}
