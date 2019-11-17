using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    class SetFieldBinder : DynamicMetaObjectBinder
    {
        readonly string _name;
        readonly Type _classContext;
        readonly AccessMask _access;

        public SetFieldBinder(string name, RuntimeTypeHandle classContext, AccessMask access)
        {
            _name = name;
            _classContext = Type.GetTypeFromHandle(classContext);
            _access = access & AccessMask.WriteMask;
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            bool hasTargetInstance = (target.LimitType != typeof(TargetTypeParam));

            var bound = new CallSiteContext(!hasTargetInstance)
            {
                ClassContext = _classContext,
                Name = _name,
            }
            .ProcessArgs(target, args, hasTargetInstance);

            //
            if (hasTargetInstance)
            {
                var isobject = bound.TargetType != null;
                if (isobject == false)
                {
                    // VariableMisusedAsObject
                    return new DynamicMetaObject(
                        BinderHelpers.VariableMisusedAsObject(target.Expression, false),
                        bound.Restrictions);
                }

                // instance := (T)instance
                bound.TargetInstance = Expression.Convert(bound.TargetInstance, bound.TargetType.Type.AsType());
            }

            //
            var setter = BinderHelpers.BindField(bound.TargetType, bound.ClassContext, bound.TargetInstance, bound.Name, bound.Context, _access, bound.Arguments.FirstOrDefault());
            if (setter == null)
            {
                // unexpected
                throw new InvalidOperationException();
            }

            return new DynamicMetaObject(setter, bound.Restrictions);
        }
    }
}
