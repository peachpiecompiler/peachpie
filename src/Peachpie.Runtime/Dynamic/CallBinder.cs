using Pchp.Core.Reflection;
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
    abstract class CallBinder : DynamicMetaObjectBinder
    {
        protected readonly Type _returnType;
        
        public override Type ReturnType => _returnType;

        protected abstract CallSiteContext CreateContext();

        protected CallBinder(RuntimeTypeHandle returnType, int genericParams)
        {
            Debug.Assert(genericParams >= 0);

            _returnType = Type.GetTypeFromHandle(returnType);
        }

        /// <summary>
        /// Gets value indicating whether the function has a target instance.
        /// </summary>
        protected abstract bool HasTarget { get; }

        /// <summary>
        /// Resolves methods to be called.
        /// </summary>
        protected abstract MethodBase[] ResolveMethods(CallSiteContext bound);

        protected virtual Expression BindMissingMethod(CallSiteContext bound)
        {
            string nameText = bound.Name ?? "???";

            // TODO: ErrCode method not found
            throw new ArgumentException(string.Format("Function '{0}' not found!", nameText));
        }

        protected void Combine(ref BindingRestrictions restrictions, BindingRestrictions restriction)
        {
            restrictions = restrictions.Merge(restriction);
        }

        #region DynamicMetaObjectBinder

        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var bound = CreateContext().ProcessArgs(target, args, HasTarget);

            Expression invocation;

            //
            var methods = ResolveMethods(bound);
            if (methods != null && methods.Length != 0)
            {
                invocation = OverloadBinder.BindOverloadCall(_returnType, bound.TargetInstance, methods, bound.Context, bound.Arguments, bound.TargetType);
            }
            else
            {
                invocation = BindMissingMethod(bound);
            }

            // TODO: by alias or by value
            return new DynamicMetaObject(invocation, bound.Restrictions);
        }

        #endregion
    }

    #region CallFunctionBinder

    /// <summary>
    /// Binder to a global function call.
    /// </summary>
    class CallFunctionBinder : CallBinder
    {
        protected string _name;
        protected string _nameOpt;

        protected override bool HasTarget => false;

        protected override CallSiteContext CreateContext() => new CallSiteContext() { Name = _name };

        internal CallFunctionBinder(string name, string nameOpt, RuntimeTypeHandle returnType, int genericParams)
            : base(returnType, genericParams)
        {
            _name = name;
            _nameOpt = nameOpt;
        }

        protected override MethodBase[] ResolveMethods(CallSiteContext bound)
        {
            if (_name != null)
            {
                return ResolveMethods(bound, _name, _nameOpt);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        MethodBase[] ResolveMethods(CallSiteContext bound, string name, string nameOpt)
        {
            var routine = bound.CurrentContext.GetDeclaredFunction(name) ?? ((nameOpt != null) ? bound.CurrentContext.GetDeclaredFunction(nameOpt) : null);
            if (routine == null)
            {
                return null;
            }

            if (routine is PhpRoutineInfo || routine is DelegateRoutineInfo)
            {
                Debug.Assert(routine.Index != 0);

                // restriction: ctx.CheckFunctionDeclared(index, routine.GetHashCode())
                var checkExpr = Expression.Call(
                    bound.Context,
                    typeof(Context).GetMethod("CheckFunctionDeclared", typeof(int), typeof(int)),
                    Expression.Constant(routine.Index), Expression.Constant(routine.GetHashCode()));

                bound.AddRestriction(checkExpr);
            }
            else if (routine is ClrRoutineInfo)
            {
                // CLR routines persist across whole app, no restriction needed
            }

            // 
            var targetInstance = routine.Target;
            if (targetInstance != null)
            {
                bound.TargetInstance = Expression.Constant(targetInstance);
            }

            //
            return routine.Methods;
        }
    }

    #endregion

    #region CallInstanceMethodBinder

    /// <summary>
    /// Binder to an instance function call.
    /// </summary>
    class CallInstanceMethodBinder : CallBinder
    {
        readonly string _name;
        readonly Type _classCtx;

        protected override CallSiteContext CreateContext() => new CallSiteContext() { ClassContext = _classCtx, Name = _name };

        protected override bool HasTarget => true;

        internal CallInstanceMethodBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
            : base(returnType, genericParams)
        {
            _name = name;
            _classCtx = classContext.Equals(default(RuntimeTypeHandle)) ? null : Type.GetTypeFromHandle(classContext);
        }

        protected override MethodBase[] ResolveMethods(CallSiteContext bound)
        {
            // resolve target expression:
            var isobject = bound.TargetType != null;
            if (isobject == false)
            {
                return null;    // no methods
            }

            // candidates:
            return bound.TargetType
                .SelectRuntimeMethods(bound.Name)
                .SelectVisible(bound.ClassContext)
                .ToArray();
        }

        protected override Expression BindMissingMethod(CallSiteContext bound)
        {
            var name_expr = (_name != null) ? Expression.Constant(_name) : bound.IndirectName;

            // resolve target expression:
            var isobject = bound.TargetType != null;
            
            if (isobject == false)
            {
                /* Template:
                 * PhpException.MethodOnNonObject(name_expr); // aka PhpException.Throw(Error, method_called_on_non_object, name_expr)
                 * return NULL;
                 */
                var throwcall = Expression.Call(typeof(PhpException), "MethodOnNonObject", Array.Empty<Type>(), ConvertExpression.Bind(name_expr, typeof(string), bound.Context));
                return Expression.Block(throwcall, ConvertExpression.BindDefault(this.ReturnType));
            }

            var call = BinderHelpers.FindMagicMethod(bound.TargetType, TypeMethods.MagicMethods.__call);
            if (call != null)
            {
                // target.__call(name, array)
                var call_args = new Expression[]
                {
                    name_expr,
                    BinderHelpers.NewPhpArray(bound.Context, bound.Arguments),
                };
                return OverloadBinder.BindOverloadCall(_returnType, bound.TargetInstance, call.Methods, bound.Context, call_args);
            }

            return base.BindMissingMethod(bound);
        }
    }

    #endregion

    #region CallStaticMethodBinder

    /// <summary>
    /// Binder to an instance function call.
    /// </summary>
    class CallStaticMethodBinder : CallBinder
    {
        readonly PhpTypeInfo _type;
        readonly string _name;
        readonly Type _classCtx;

        protected override CallSiteContext CreateContext() => new CallSiteContext() { ClassContext = _classCtx, TargetType = _type, Name = _name };

        protected override bool HasTarget => true; // there is caller instance or null as a target

        internal CallStaticMethodBinder(RuntimeTypeHandle type, string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
            : base(returnType, genericParams)
        {
            _type = type.GetPhpTypeInfo();
            _name = name;
            _classCtx = Type.GetTypeFromHandle(classContext);
        }

        protected override MethodBase[] ResolveMethods(CallSiteContext bound)
        {
            // check bound.TargetType is assignable from bound.TargetInstance
            if (bound.TargetType == null ||
                bound.CurrentTargetInstance == null ||
                bound.TargetType.Type.IsAssignableFrom(bound.CurrentTargetInstance.GetType()) == false)
            {
                // target instance cannot be used
                bound.TargetInstance = null;
            }

            //
            if (bound.Name != null)
            {
                // candidates:
                return bound.TargetType
                    .SelectRuntimeMethods(bound.Name)
                    .SelectVisible(bound.ClassContext)
                    .SelectStatic()
                    .ToArray();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected override Expression BindMissingMethod(CallSiteContext bound)
        {
            if (bound.TargetType == null)   // already reported - class cannot be found
            {
                return ConvertExpression.BindDefault(this.ReturnType);
            }

            var call = BinderHelpers.FindMagicMethod(bound.TargetType, (bound.TargetInstance == null) ? TypeMethods.MagicMethods.__callstatic : TypeMethods.MagicMethods.__call);
            if (call != null)
            {
                var name_expr = (_name != null) ? Expression.Constant(_name) : bound.IndirectName;

                // T.__callStatic(name, array)
                var call_args = new Expression[]
                {
                    name_expr,
                    BinderHelpers.NewPhpArray(bound.Context, bound.Arguments),
                };
                return OverloadBinder.BindOverloadCall(_returnType, bound.TargetInstance, call.Methods, bound.Context, call_args);
            }

            //
            return base.BindMissingMethod(bound);
        }
    }

    #endregion
}
