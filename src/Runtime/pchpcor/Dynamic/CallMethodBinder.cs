using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.Core.Dynamic
{
    public class CallMethodBinder : DynamicMetaObjectBinder
    {
        protected readonly Type _returnType;
        protected readonly int _genericParamsCount;
        protected readonly Type _classContext;
        protected readonly string _nameOpt;

        protected string ResolveName(IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            if (_nameOpt != null)
            {
                return _nameOpt;
            }
            else
            {
                var namearg = args[0];
                args.RemoveAt(0);

                // TODO: restrictions = restrictions.Merge(BindingRestrictions.)

                //return namearg.Value.ToString();
                throw new NotImplementedException();
            }
        }

        #region Factory

        protected CallMethodBinder(string nameOpt, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
        {
            _nameOpt = nameOpt;
            _returnType = Type.GetTypeFromHandle(returnType);
            _genericParamsCount = genericParams;
            _classContext = Type.GetTypeFromHandle(classContext);
        }

        public static CallMethodBinder Create(string nameOpt, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
        {
            return new CallMethodBinder(nameOpt, classContext, returnType, genericParams);
        }

        #endregion

        #region DynamicMetaObjectBinder

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            BindingRestrictions restrictions = BindingRestrictions.Empty;
            var argsList = new List<DynamicMetaObject>(args);

            // cut off special callsite arguments:

            // [] Context <ctx>
            var ctx = argsList[0];
            argsList.RemoveAt(0);

            // [] Name
            var methodName = ResolveName(argsList, ref restrictions);

            // [] Type <classCtx>
            // [] Type <static>
            // [] PhpArray <locals>

            // resolve instance expression:
            Expression target_expr;
            object target_value;
            BinderHelpers.TargetAsObject(target, out target_expr, out target_value, ref restrictions);
            var runtime_type = target_value.GetType();

            // resolve overload:
            var candidates = runtime_type.SelectCandidates().SelectByName(methodName).ToList();

            if (!target_expr.Type.GetTypeInfo().IsSealed)
            {
                restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target_expr, runtime_type));
                target_expr = Expression.Convert(target_expr, runtime_type);
            }

            var expr_args = argsList.Select(x => x.Expression).ToArray();
            var invocation = OverloadBinder.BindOverloadCall(_returnType, target_expr, candidates.ToArray(), ctx.Expression, expr_args);

            // TODO: by alias or by value
            return new DynamicMetaObject(invocation, restrictions);
        }

        #endregion
    }
}
