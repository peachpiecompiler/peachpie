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

        protected virtual bool IsClassConst => false;

        public GetFieldBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessFlags access)
        {
            _name = name;
            _returnType = Type.GetTypeFromHandle(returnType); // should correspond to AccessFlags
            _classContext = Type.GetTypeFromHandle(classContext);
            _access = access & AccessFlags.ReadMask;
        }

        string ResolveName(DynamicMetaObject[] args, ref BindingRestrictions restrictions)
        {
            int i = 1;  // [0] = ctx

            if (_name != null)
            {
                return _name;
            }
            else
            {
                Debug.Assert(args.Length >= i && args[i].LimitType == typeof(string));
                restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Equal(args[i].Expression, Expression.Constant(args[i].Value)))); // args[0] == "VALUE"
                return (string)args[i++].Value;
            }
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var restrictions = BindingRestrictions.Empty;

            PhpTypeInfo phptype;
            Expression target_expr;

            //
            var ctx = args[0];
            var fldName = ResolveName(args, ref restrictions);

            //
            if (target.LimitType == typeof(PhpTypeInfo))    // static field
            {
                target_expr = null;
                phptype = (PhpTypeInfo)target.Value;

                // 
                restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(target.Expression, phptype));
            }
            else
            {
                // instance field
                object target_value;
                BinderHelpers.TargetAsObject(target, out target_expr, out target_value, ref restrictions);

                if (target_value == null)
                {
                    var defaultexpr = ConvertExpression.BindDefault(_returnType);

                    if (!_access.Quiet())
                    {
                        // PhpException.VariableMisusedAsObject(target, _access.ReadRef)
                        var throwcall = Expression.Call(typeof(PhpException), "VariableMisusedAsObject", Array.Empty<Type>(),
                            ConvertExpression.BindToValue(target.Expression), Expression.Constant(_access.EnsureAlias()));
                        defaultexpr = Expression.Block(throwcall, defaultexpr);
                    }

                    return new DynamicMetaObject(defaultexpr, restrictions);
                }

                var runtime_type = target_value.GetType();

                //
                if (target_expr.Type != runtime_type)
                {
                    restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target_expr, runtime_type));
                    target_expr = Expression.Convert(target_expr, runtime_type);
                }

                phptype = runtime_type.GetPhpTypeInfo();
            }

            Debug.Assert(IsClassConst == (target_expr == null));

            //
            var getter = IsClassConst
                ? BinderHelpers.BindClassConstant(phptype, _classContext, fldName, ctx.Expression)
                : BinderHelpers.BindField(phptype, _classContext, target_expr, fldName, ctx.Expression, _access, null);

            if (getter != null)
            {
                //
                return new DynamicMetaObject(ConvertExpression.Bind(getter, _returnType, ctx.Expression), restrictions);
            }

            // field not found
            throw new NotImplementedException();
        }
    }

    public class GetClassConstBinder : GetFieldBinder
    {
        public GetClassConstBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessFlags access)
            :base(name, classContext, returnType, access)
        {
        }

        protected override bool IsClassConst => true;
    }
}
