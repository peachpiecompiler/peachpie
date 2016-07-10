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

            var boundcandidates = routine.Handles.Select(MethodBase.GetMethodFromHandle).Select(m =>
            {
                try
                {
                    return m.TryBindArguments(argsList, ctx.Expression);
                }
                catch
                {
                    return null;
                }
            })
            .Where(x => x != null)
            //.Where(x => x.ErrCode == 0)
            .OrderBy(binding => binding.Cost)
            .ToList();

            if (boundcandidates.Count == 0)
            {
                // TODO: ErrCode no overload with specified arguments
                throw new NotImplementedException("Cannot bind arguments to parameters!");
            }

            if (boundcandidates.Count > 1 && boundcandidates[0].Cost == boundcandidates[1].Cost)
            {
                // TODO: ErrCode ambiguous call
                throw new NotImplementedException("Call is ambiguous!");
            }

            var bound = boundcandidates[0];
            restrictions = restrictions.Merge(bound.Restrictions);
            var invocation = Expression.Call((MethodInfo)bound.Method, bound.Arguments);

            // TODO: by alias or by value
            return new DynamicMetaObject(ConvertExpression.Bind(invocation, _returnType), restrictions);
        }

        #endregion
    }
}
