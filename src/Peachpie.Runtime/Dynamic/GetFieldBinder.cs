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
    public class GetFieldBinder : DynamicMetaObjectBinder
    {
        readonly string _name;
        readonly Type _classContext;
        readonly Type _returnType;
        readonly AccessFlags _access;

        public GetFieldBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessFlags access)
        {
            _name = name;
            _returnType = Type.GetTypeFromHandle(returnType); // should correspond to AccessFlags
            _classContext = Type.GetTypeFromHandle(classContext);
            _access = access;
        }

        string ResolveName(DynamicMetaObject[] args, ref BindingRestrictions restrictions)
        {
            if (_name != null)
            {
                return _name;
            }
            else
            {
                Debug.Assert(args.Length >= 1 && args[0].LimitType == typeof(string));
                restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Equal(args[0].Expression, Expression.Constant(args[0].Value)))); // args[0] == "VALUE"
                return (string)args[0].Value;
            }
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var restrictions = BindingRestrictions.Empty;

            PhpTypeInfo phptype;
            Expression target_expr;

            //
            var fldName = ResolveName(args, ref restrictions);

            //
            if (target.LimitType == typeof(PhpTypeInfo))    // static field
            {
                target_expr = null;
                phptype = (PhpTypeInfo)target.Value;

                // 
                restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(target.Expression, target_expr));
            }
            else
            {
                // instance field
                object target_value;
                BinderHelpers.TargetAsObject(target, out target_expr, out target_value, ref restrictions);

                var runtime_type = target_value.GetType();

                //
                if (target_expr.Type != runtime_type)
                {
                    restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target_expr, runtime_type));
                    target_expr = Expression.Convert(target_expr, runtime_type);
                }

                phptype = runtime_type.GetPhpTypeInfo();
            }

            //
            var getter = BinderHelpers.BindField(phptype, _classContext, target_expr, fldName, null, _access, null);
            if (getter != null)
            {
                //
                return new DynamicMetaObject(ConvertExpression.Bind(getter, _returnType), restrictions);
            }

            // field not found
            throw new NotImplementedException();
        }
    }
}
