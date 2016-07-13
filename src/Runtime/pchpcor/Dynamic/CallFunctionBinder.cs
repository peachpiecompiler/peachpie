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
    public class CallFunctionBinder : DynamicMetaObjectBinder
    {
        protected readonly Type _returnType;
        protected readonly int _genericParamsCount;
        protected readonly string _nameOpt, _fallbackNameOpt;

        #region Factory

        protected CallFunctionBinder(string nameOpt, string fallbackNameOpt, RuntimeTypeHandle returnType, int genericParams)
        {
            _nameOpt = nameOpt;
            _fallbackNameOpt = fallbackNameOpt;
            _returnType = Type.GetTypeFromHandle(returnType);
            _genericParamsCount = genericParams;
        }

        public static CallFunctionBinder Create(string nameOpt, string fallbackNameOpt, RuntimeTypeHandle returnType, int genericParams)
        {
            return new CallFunctionBinder(nameOpt, fallbackNameOpt, returnType, genericParams);
        }

        #endregion

        #region DynamicMetaObjectBinder

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            BindingRestrictions restrictions = BindingRestrictions.Empty;
            var argsList = new List<DynamicMetaObject>(args);

            // cut off special callsite arguments:

            // [] Context <ctx>
            var ctx = target;
            Debug.Assert(ctx != null && ctx.HasValue && ctx.Value != null);
            var ctxInstance = (Context)ctx.Value;

            // [] Name
            var methodName = _nameOpt;

            // [] PhpArray <locals>

            // resolve overload:
            var routine = ctxInstance.GetDeclaredFunction(_nameOpt);
            if (routine == null)
            {
                // TODO: ErrCode method not found
                throw new NotImplementedException("Function not found!");
            }

            // TODO: restriction <ctx>.DeclaredFunction[idx] == handle

            var methods = routine.Handles.Select(MethodBase.GetMethodFromHandle).ToArray();

            var expr_args = argsList.Select(x => x.Expression).ToArray();
            var invocation = OverloadBinder.BindOverloadCall(_returnType, null, methods, ctx.Expression, expr_args);

            // TODO: by alias or by value
            return new DynamicMetaObject(invocation, restrictions);
        }

        #endregion
    }
}
