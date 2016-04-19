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
    public class GetFieldBinder : DynamicMetaObjectBinder
    {
        readonly string _name;
        readonly Type _classContext;
        readonly Type _returnType;
        readonly int _flags;

        public GetFieldBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int flags)
        {
            _name = name;
            _returnType = Type.GetTypeFromHandle(returnType);
            _classContext = Type.GetTypeFromHandle(classContext);
            _flags = flags;
        }

        string ResolveName(DynamicMetaObject[] args, ref BindingRestrictions restrictions)
        {
            if (_name != null)
            {
                return _name;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var restrictions = BindingRestrictions.Empty;

            var target_expr = target.Expression;
            var target_value = target.Value;

            if (target.Value == null)
            {
                throw new NotImplementedException();    // TODO: call on NULL
            }

            var fldName = ResolveName(args, ref restrictions);

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
            
            var runtime_type = target_value.GetType();

            var fld = runtime_type.GetTypeInfo().GetDeclaredField(fldName);
            if (fld != null)
            {
                if (target_expr.Type != runtime_type)
                {
                    restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target_expr, runtime_type));
                    target_expr = Expression.Convert(target_expr, runtime_type);
                }

                var getter = Expression.Field(target_expr, fld);
                // TODO: _flags // ensure array, object, alias
                return new DynamicMetaObject(ConvertExpression.Bind(getter, _returnType), restrictions);
            }
            else
            {
                throw new NotImplementedException();    // runtime field or __get
            }
        }
    }
}
