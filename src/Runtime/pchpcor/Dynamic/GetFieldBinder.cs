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

            //
            if (target_expr.Type != runtime_type)
            {
                restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target_expr, runtime_type));
                target_expr = Expression.Convert(target_expr, runtime_type);
            }

            //
            var fld = runtime_type.GetTypeInfo().GetDeclaredField(fldName);
            if (fld != null)
            {
                Expression getter = Expression.Field(target_expr, fld);

                // Ensure Object
                if (_access.EnsureObject())
                {
                    if (fld.FieldType == typeof(PhpAlias))
                    {
                        // ((PhpAlias)fld).EnsureObject(ctx)
                        getter = Expression.Call(getter, Cache.Operators.PhpAlias_EnsureObject_Context, Expression.Constant(null, typeof(Context)));
                    }
                    else if (fld.FieldType == typeof(PhpValue))
                    {
                        // ((PhpValue)fld).EnsureObject(ctx)
                        getter = Expression.Call(getter, Cache.Operators.PhpValue_EnsureObject_Context, Expression.Constant(null, typeof(Context)));
                    }
                    else
                    {
                        // getter // TODO: ensure it is not null
                    }
                }
                else if (_access.EnsureArray())
                {
                    if (fld.FieldType == typeof(PhpAlias))
                    {
                        // ((PhpAlias)fld).EnsureArray()
                        getter = Expression.Call(getter, Cache.Operators.PhpAlias_EnsureArray);
                    }
                    else if (fld.FieldType == typeof(PhpValue))
                    {
                        // ((PhpValue)fld).EnsureArray()
                        getter = Expression.Call(getter, Cache.Operators.PhpValue_EnsureArray);
                    }
                    else if (fld.FieldType == typeof(PhpArray))
                    {
                        // (PhpArray)fld // TODO: ensure it is not null
                    }
                    else
                    {
                        // getter
                    }
                }
                else if (_access.EnsureAlias())
                {
                    Debug.Assert(_returnType == typeof(PhpAlias));

                    if (fld.FieldType == typeof(PhpAlias))
                    {
                        // (PhpAlias)getter
                    }
                    else if (fld.FieldType == typeof(PhpValue))
                    {
                        // ((PhpValue)fld).EnsureAlias()
                        getter = Expression.Call(getter, Cache.Operators.PhpValue_EnsureAlias);
                    }
                    else
                    {
                        // getter // cannot read as reference
                    }
                }

                //
                return new DynamicMetaObject(ConvertExpression.Bind(getter, _returnType), restrictions);
            }
            else
            {
                // TODO: __get(name)

                // PhpArray __peach__runtimeFields
                var __peach__runtimeFields = BinderHelpers.LookupRuntimeFields(runtime_type);
                if (__peach__runtimeFields != null)
                {
                    var __runtimeflds_field = Expression.Field(target_expr, __peach__runtimeFields);
                    var key = Expression.Constant(new IntStringKey(fldName));

                    Expression getter;

                    if (_access.EnsureObject())
                    {
                        getter = Expression.Call(__runtimeflds_field, Cache.Operators.PhpArray_EnsureItemObject, key, Expression.Constant(null, typeof(Context)));

                        // if (__runtimeflds_field == null) __runtimeflds_field = [];
                        // return getter
                        getter = Expression.Block(_returnType,
                            BinderHelpers.EnsureNotNullPhpArray(__runtimeflds_field),
                            getter);
                    }
                    else if (_access.EnsureArray())
                    {
                        getter = Expression.Call(__runtimeflds_field, Cache.Operators.PhpArray_EnsureItemArray, key);

                        // if (__runtimeflds_field == null) __runtimeflds_field = [];
                        // return getter
                        getter = Expression.Block(_returnType,
                            BinderHelpers.EnsureNotNullPhpArray(__runtimeflds_field),
                            getter);
                    }
                    else if (_access.EnsureAlias())
                    {
                        getter = Expression.Call(__runtimeflds_field, Cache.Operators.PhpArray_EnsureItemAlias, key);

                        // if (__runtimeflds_field == null) __runtimeflds_field = [];
                        // return getter
                        getter = Expression.Block(_returnType,
                            BinderHelpers.EnsureNotNullPhpArray(__runtimeflds_field),
                            getter);
                    }
                    else
                    {
                        getter = Expression.Call(__runtimeflds_field, Cache.Operators.PhpArray_GetItemValue, key);
                        // TODO: (__runtimeflds_field != null) ? getter : ERROR;
                    }

                    //
                    return new DynamicMetaObject(ConvertExpression.Bind(getter, _returnType), restrictions);
                }

                // field not found
                throw new NotImplementedException();
            }
        }
    }
}
