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
            
            Expression target_expr;
            object target_value;
            BinderHelpers.TargetAsObject(target, out target_expr, out target_value, ref restrictions);

            var fldName = ResolveName(args, ref restrictions);

            var runtime_type = target_value.GetType();
            var value = args[0].Expression;

            var fld = runtime_type.GetTypeInfo().GetDeclaredField(fldName);
            if (fld != null)
            {
                if (target_expr.Type != runtime_type)
                {
                    restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target_expr, runtime_type));
                    target_expr = Expression.Convert(target_expr, runtime_type);
                }
                
                Expression lvalue = Expression.Field(target_expr, fld);
                Expression setter;

                if (_access.WriteAlias())
                {
                    // write alias

                    Debug.Assert(value.Type == typeof(PhpAlias));
                    value = ConvertExpression.Bind(value, typeof(PhpAlias));

                    if (fld.FieldType == typeof(PhpAlias))
                    {
                        setter = Expression.Assign(lvalue, value);
                    }
                    else if (fld.FieldType == typeof(PhpValue))
                    {
                        // fld = PhpValue.Create(alias)
                        value = Expression.Call(typeof(PhpValue).GetMethod("Create", Cache.Types.PhpAlias), value);
                        setter = Expression.Assign(lvalue, value);
                    }
                    else
                    {
                        // fld is not aliasable
                        Debug.Assert(false, "Cannot assign aliased value to field " + fld.FieldType.ToString() + " " + fld.Name);
                        setter = Expression.Assign(lvalue, ConvertExpression.Bind(value, fld.FieldType));
                    }
                }
                else
                {
                    // write by value

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
                }

                //
                return new DynamicMetaObject(setter, restrictions);
            }
            else
            {
                throw new NotImplementedException();    // runtime field or __set
            }
        }
    }
}
