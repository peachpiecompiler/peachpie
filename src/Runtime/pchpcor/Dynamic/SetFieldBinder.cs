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
    public class SetFieldBinder : DynamicMetaObjectBinder
    {
        readonly string _name;
        readonly Type _classContext;
        readonly int _flags;

        public SetFieldBinder(string name, RuntimeTypeHandle classContext, int flags)
        {
            _name = name;
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
            var value = args[0].Expression;

            if (target.Value == null)
            {
                throw new NotImplementedException();    // TODO: call on NULL
            }

            var fldName = ResolveName(args, ref restrictions);
            var targetType = target.Value.GetType();
            var fld = targetType.GetTypeInfo().GetDeclaredField(fldName);
            if (fld != null)
            {
                restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.RuntimeType));

                // TODO: value restrictions

                Expression lvalue = Expression.Field(Expression.Convert(target.Expression, targetType), fld);
                Expression setter;

                if (value.Type == typeof(PhpAlias))
                {
                    // assigning alias
                    throw new NotImplementedException();    // AssignRef
                }
                else
                {
                    // assigning to alias <=> fld.FieldType is PhpAlias || fld.FieldType is PhpValue && fld.Value.IsAlias
                    if (fld.FieldType == typeof(PhpAlias))
                    {
                        // Template: fld.Value = (PhpValue)value
                        setter = Expression.Assign(Expression.PropertyOrField(lvalue, "Value"), ConvertExpression.Bind(value, typeof(PhpValue)));
                    }
                    else if (fld.FieldType == typeof(PhpValue))
                    {
                        // Template: Operators.SetValue(ref fld, (PhpValue)value)
                        setter = Expression.Call(Cache.Operators.SetValue_PhpValueRef_PhpValue, lvalue, ConvertExpression.Bind(value, typeof(PhpValue)));
                    }
                    else
                    {
                        // Template: fld = value
                        // default behaviour by value to value
                        setter = Expression.Assign(lvalue, ConvertExpression.Bind(value, fld.FieldType));
                    }

                    //
                    return new DynamicMetaObject(setter, restrictions);
                }
            }
            else
            {
                throw new NotImplementedException();    // runtime field or __set
            }
        }
    }
}
