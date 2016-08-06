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
    public class SetFieldBinder : DynamicMetaObjectBinder
    {
        readonly string _name;
        readonly Type _classContext;
        readonly AccessFlags _access;

        public SetFieldBinder(string name, RuntimeTypeHandle classContext, AccessFlags access)
        {
            _name = name;
            _classContext = Type.GetTypeFromHandle(classContext);
            _access = access;
        }

        void ResolveArgs(DynamicMetaObject[] args, ref BindingRestrictions restrictions, out string fieldName, out Expression valueExpr)
        {
            if (_name != null)
            {
                fieldName = _name;
                valueExpr = (args.Length > 0) ? args[0].Expression : null;
            }
            else
            {
                Debug.Assert(args.Length >= 1 && args[0].LimitType == typeof(string));
                restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Equal(args[0].Expression, Expression.Constant(args[0].Value)))); // args[0] == "VALUE"
                fieldName = (string)args[0].Value;
                valueExpr = (args.Length > 1) ? args[1].Expression : null;
            }
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var restrictions = BindingRestrictions.Empty;
            
            Expression target_expr;
            object target_value;
            BinderHelpers.TargetAsObject(target, out target_expr, out target_value, ref restrictions);

            string fldName;
            Expression value;

            ResolveArgs(args, ref restrictions, out fldName, out value);

            var runtime_type = target_value.GetType();

            // 
            if (target_expr.Type != runtime_type)
            {
                restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target_expr, runtime_type));
                target_expr = Expression.Convert(target_expr, runtime_type);
            }

            //
            var setter = BinderHelpers.BindField(runtime_type.GetPhpTypeInfo(), _classContext, target_expr, fldName, null, _access, value);
            if (setter != null)
            {
                //
                return new DynamicMetaObject(setter, restrictions);
            }

            // field not found
            throw new NotImplementedException();
        }
    }
}
