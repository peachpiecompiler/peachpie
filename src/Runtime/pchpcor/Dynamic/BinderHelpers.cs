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
    internal static class BinderHelpers
    {
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

        //public static MethodBase ResolveOverload(IEnumerable<MethodBase> candidates, string name, TypeInfo classContext, IEnumerable<Expression> arguments)
        //{

        //}

        public static PhpCallable BindToPhpCallable(MethodInfo target)
        {
            // (Context ctx, PhpValue[] arguments)
            var ps = new ParameterExpression[] { Expression.Parameter(typeof(Context), "ctx"), Expression.Parameter(typeof(PhpValue[]), "arguments") };
            var target_ps = target.GetParameters();

            // Convert( Expression.Call( method, Convert(args[0]), Convert(args[1]), ... ), PhpValue)

            // TODO: bind arguments properly, merge with CallFunctionBinder, handle vararg, handle missing mandatory args

            // bind parameters
            var args = new List<Expression>(target_ps.Length);
            int source_index = 0;
            foreach (var p in target_ps)
            {
                if (args.Count == 0)
                {
                    if (p.ParameterType == typeof(Context))
                    {
                        args.Add(ps[0]);
                        continue;
                    }
                }

                //
                args.Add(ConvertExpression.Bind(Expression.ArrayIndex(ps[1], Expression.Constant(source_index++, typeof(int))), p.ParameterType));
            }

            // invoke target
            var invocation = ConvertExpression.Bind(Expression.Call(target, args), typeof(PhpValue));

            // compile & create delegate
            var lambda = Expression.Lambda<PhpCallable>(invocation, target.Name, true, ps);
            return lambda.Compile();
        }
    }
}
