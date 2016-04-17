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
                var getter = Expression.Field(Expression.Convert(target.Expression, targetType), fld);
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
