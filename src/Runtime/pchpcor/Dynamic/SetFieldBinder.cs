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
            Expression setter;

            //
            var fld = runtime_type.GetRuntimeField(fldName);
            if (fld != null)
            {
                // TODO: check context and accessibility

                Expression lvalue = Expression.Field(target_expr, fld);
                
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
                else if (_access.Unset())
                {
                    Debug.Assert(value == null);

                    setter = Expression.Assign(lvalue, ConvertExpression.BindDefault(fld.FieldType));
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
            }
            else
            {
                // TODO: __set($name, $value)

                // PhpArray __peach__runtimeFields
                var __peach__runtimeFields = BinderHelpers.LookupRuntimeFields(runtime_type);
                if (__peach__runtimeFields != null)
                {
                    var __runtimeflds_field = Expression.Field(target_expr, __peach__runtimeFields);

                    if (_access.WriteAlias())
                    {
                        // write alias

                        Debug.Assert(value.Type == typeof(PhpAlias));
                        value = ConvertExpression.Bind(value, typeof(PhpAlias));

                        // <target>.RuntimeFields.SetItemAlias(name, alias);
                        setter = Expression.Call(__runtimeflds_field, Cache.Operators.PhpArray_SetItemAlias,
                            Expression.Constant(new IntStringKey(fldName)), value);
                    }
                    else if (_access.Unset())
                    {
                        Debug.Assert(value == null);

                        // remove key

                        // <target>.RuntimeFields.RemoveKey(name)
                        setter = Expression.Call(__runtimeflds_field, Cache.Operators.PhpArray_RemoveKey, Expression.Constant(new IntStringKey(fldName)));
                    }
                    else
                    {
                        // write by value

                        value = ConvertExpression.Bind(value, typeof(PhpValue));

                        // <target>.RuntimeFields.SetItemValue(name, value);
                        setter = Expression.Call(__runtimeflds_field, Cache.Operators.PhpArray_SetItemValue,
                            Expression.Constant(new IntStringKey(fldName)), value);
                    }

                    //

                    if (!_access.Unset())
                    {
                        // prepend ensure __peach__runtimeFields != null
                        setter = Expression.Block(
                            BinderHelpers.EnsureNotNullPhpArray(__runtimeflds_field),
                            setter);
                    }
                }
                else
                {
                    // field cannot be set
                    throw new NotImplementedException();
                }
            }

            //
            return new DynamicMetaObject(setter, restrictions);
        }
    }
}
