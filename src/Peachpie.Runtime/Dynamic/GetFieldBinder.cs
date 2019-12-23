using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    class GetFieldBinder : DynamicMetaObjectBinder
    {
        readonly string _name;
        readonly Type _classContext;
        readonly Type _returnType;
        readonly AccessMask _access;

        protected virtual bool IsClassConst => false;

        public GetFieldBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessMask access)
        {
            _name = name;
            _returnType = Type.GetTypeFromHandle(returnType); // should correspond to AccessFlags
            _classContext = Type.GetTypeFromHandle(classContext);
            _access = access & (~AccessMask.WriteMask); // Read|Write => Read
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            bool hasTargetInstance = (target.LimitType != typeof(TargetTypeParam));

            var bound = new CallSiteContext(!hasTargetInstance)
            {
                ClassContext = _classContext,
                Name = _name
            }
            .ProcessArgs(target, args, hasTargetInstance);

            if (hasTargetInstance)
            {
                var isobject = bound.TargetType != null;
                if (isobject == false)
                {
                    var defaultexpr = ConvertExpression.BindDefault(_returnType);

                    if (!_access.Quiet())
                    {
                        // PhpException.VariableMisusedAsObject(target, _access.ReadRef)
                        var throwcall = BinderHelpers.VariableMisusedAsObject(target.Expression, _access.EnsureAlias());
                        defaultexpr = Expression.Block(throwcall, defaultexpr);
                    }

                    return new DynamicMetaObject(defaultexpr, bound.Restrictions);
                }

                // instance := (T)instance
                bound.TargetInstance = Expression.Convert(bound.TargetInstance, bound.TargetType.Type);
            }

            Debug.Assert(IsClassConst ? (bound.TargetInstance == null) : true);

            //
            var getter = IsClassConst
                ? BinderHelpers.BindClassConstant(bound.TargetType, bound.ClassContext, bound.Name, bound.Context)
                : BinderHelpers.BindField(bound.TargetType, bound.ClassContext, bound.TargetInstance, bound.Name, bound.Context, _access, null);

            if (getter != null)
            {
                //
                return new DynamicMetaObject(ConvertExpression.Bind(getter, _returnType, bound.Context), bound.Restrictions);
            }

            if (IsClassConst)
            {
                // error: constant not defined
                // ...
            }

            // unreachable: property not found
            throw new InvalidOperationException($"{bound.TargetType.Name}::{bound.Name} could not be resolved.");
        }
    }

    class GetClassConstBinder : GetFieldBinder
    {
        public GetClassConstBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, AccessMask access)
            : base(name, classContext, returnType, access)
        {
        }

        protected override bool IsClassConst => true;
    }
}
