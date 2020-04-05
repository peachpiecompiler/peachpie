using Pchp.Core.Reflection;
using Pchp.Core.Utilities;
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
    [DebuggerNonUserCode]
    abstract class CallBinder : DynamicMetaObjectBinder
    {
        protected readonly Type _returnType;

        public override Type ReturnType => _returnType;

        protected abstract CallSiteContext CreateContext();

        protected CallBinder(RuntimeTypeHandle returnType)
        {
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
            /* Template:
             * PhpException.UndefinedFunctionCalled(name);
             * return NULL;
            */
            var throwcall = bound.TargetType != null
                ? Expression.Call(typeof(PhpException), "UndefinedMethodCalled", Array.Empty<Type>(), Expression.Constant(bound.TargetType.Name), bound.IndirectName ?? Expression.Constant(bound.Name))
                : Expression.Call(typeof(PhpException), "UndefinedFunctionCalled", Array.Empty<Type>(), bound.IndirectName ?? Expression.Constant(bound.Name));

            return Expression.Block(throwcall, ConvertExpression.BindDefault(this.ReturnType));
        }

        /// <summary>
        /// Checks the method's signature is in format:
        /// - <c>(name, params T[])</c>
        /// - <c>(name, ...)</c>
        /// Ignores implicit arguments at the beginning of the signature.
        /// </summary>
        protected static bool IsClrMagicCallWithParams(MethodInfo method)
        {
            var ps = method.GetParameters();

            // ignore implicit parameters at beginning of the routine
            var first = ps.TakeWhile(BinderHelpers.IsImplicitParameter).Count();

            var count = ps.Length - first;
            if (count >= 1)
            {
                if (!method.DeclaringType.GetPhpTypeInfo().IsPhpType) // only methods declared outside PHP code
                {
                    if (count > 2) return true;
                    if (ps.Last().IsParamsParameter()) return true;
                }
            }

            return false;
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
                // late static bound type, 'static' if available, otherwise the target type
                var lateStaticTypeArg = (object)bound.LateStaticType ?? bound.TargetType;

                if (bound.HasArgumentUnpacking)
                {
                    var args_var = Expression.Variable(typeof(PhpValue[]), "args_array");

                    /*
                     * args_var = ArgumentsToArray()
                     * call(...args_var...)
                     */

                    invocation = Expression.Block(new[] { args_var },
                            Expression.Assign(args_var, BinderHelpers.UnpackArgumentsToArray(methods, bound.Arguments, bound.Context, bound.ClassContext)),
                            OverloadBinder.BindOverloadCall(_returnType, bound.TargetInstance, methods, bound.Context, args_var, bound.IsStaticSyntax, lateStaticType: lateStaticTypeArg)
                        );
                }
                else
                {
                    invocation = OverloadBinder.BindOverloadCall(_returnType, bound.TargetInstance, methods, bound.Context, bound.Arguments, bound.IsStaticSyntax, lateStaticType: lateStaticTypeArg, classContext: bound.ClassContext);
                }
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
    [DebuggerNonUserCode]
    class CallFunctionBinder : CallBinder
    {
        protected string _name;
        protected string _nameOpt;

        protected override bool HasTarget => false;

        protected override CallSiteContext CreateContext() => new CallSiteContext(false) { Name = _name };

        internal CallFunctionBinder(string name, string nameOpt, RuntimeTypeHandle returnType)
            : base(returnType)
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

            if (bound.TypeArguments != null && bound.TypeArguments.Length != 0)
            {
                // global functions cannot be (should not be) generic!
                throw new InvalidOperationException();  // NS
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
    [DebuggerNonUserCode]
    class CallInstanceMethodBinder : CallBinder
    {
        readonly string _name;
        readonly Type _classCtx;

        protected override CallSiteContext CreateContext() => new CallSiteContext(false) { ClassContext = _classCtx, Name = _name };

        protected override bool HasTarget => true;

        internal CallInstanceMethodBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType)
            : base(returnType)
        {
            _name = name;
            _classCtx = classContext.Equals(default) ? null : Type.GetTypeFromHandle(classContext);
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
                .SelectRuntimeMethods(bound.Name, bound.ClassContext)
                .NonStaticPreferably()
                .SelectVisible(bound.ClassContext)
                .Construct(bound.TypeArguments)
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
                 * PhpException.MethodOnNonObject(name_expr);
                 * return NULL;
                 */
                var throwcall = Expression.Call(typeof(PhpException), "MethodOnNonObject", Array.Empty<Type>(), ConvertExpression.Bind(name_expr, typeof(string), bound.Context));
                return Expression.Block(throwcall, ConvertExpression.BindDefault(this.ReturnType));
            }

            var call = BinderHelpers.FindMagicMethod(bound.TargetType, TypeMethods.MagicMethods.__call);
            if (call != null)
            {
                Expression[] call_args;

                if (call.Methods.All(IsClrMagicCallWithParams))
                {
                    // Template: target.__call(name, arg1, arg2, ...)
                    // flatterns the arguments:
                    call_args = ArrayUtils.AppendRange(name_expr, bound.Arguments);
                }
                else
                {
                    // Template: target.__call(name, array)
                    // regular PHP behavior:
                    call_args = new Expression[]
                    {
                        name_expr,
                        BinderHelpers.NewPhpArray(bound.Arguments, bound.Context, bound.ClassContext),
                    };
                }

                return OverloadBinder.BindOverloadCall(_returnType, bound.TargetInstance, call.Methods, bound.Context, call_args, false);
            }

            return base.BindMissingMethod(bound);
        }
    }

    #endregion

    #region CallStaticMethodBinder

    /// <summary>
    /// Binder to an instance function call.
    /// </summary>
    [DebuggerNonUserCode]
    class CallStaticMethodBinder : CallBinder
    {
        readonly PhpTypeInfo _type;
        readonly string _name;
        readonly Type _classCtx;

        protected override CallSiteContext CreateContext() => new CallSiteContext(true) { ClassContext = _classCtx, TargetType = _type, Name = _name };

        protected override bool HasTarget => true; // there is caller instance or null as a target

        internal CallStaticMethodBinder(RuntimeTypeHandle type, string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType)
            : base(returnType)
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
                IEnumerable<MethodBase> candidates =
                    bound.TargetType
                    .SelectRuntimeMethods(bound.Name, bound.ClassContext)
                    .SelectVisible(bound.ClassContext);

                if (bound.TargetInstance == null)
                {
                    candidates = candidates.SelectStatic();
                }

                //
                return candidates.Construct(bound.TypeArguments).ToArray();
            }
            else
            {
                throw new ArgumentException();
            }
        }

        protected override Expression BindMissingMethod(CallSiteContext bound)
        {
            var type = bound.TargetType;
            if (type == null)   // already reported - class cannot be found
            {
                return ConvertExpression.BindDefault(this.ReturnType);
            }

            if (bound.TargetInstance != null && bound.CurrentTargetInstance != null) // it has been checked it is a subclass of TargetType
            {
                // ensure current scope's __call() is favoured over the specified class
                type = bound.CurrentTargetInstance.GetPhpTypeInfo();
            }

            // try to find __call() first if we have $this
            var call = (bound.TargetInstance != null) ? BinderHelpers.FindMagicMethod(type, TypeMethods.MagicMethods.__call) : null;
            if (call == null)
            {
                // look for __callStatic()
                call = BinderHelpers.FindMagicMethod(type, TypeMethods.MagicMethods.__callstatic);
            }

            if (call != null)
            {
                Expression[] call_args;

                var name_expr = (_name != null) ? Expression.Constant(_name) : bound.IndirectName;

                if (call.Methods.All(IsClrMagicCallWithParams))
                {
                    // Template: target.__call(name, arg1, arg2, ...)
                    // flatterns the arguments:
                    call_args = ArrayUtils.AppendRange(name_expr, bound.Arguments);
                }
                else
                {
                    // Template: target.__call(name, array)
                    // regular PHP behavior:
                    call_args = new Expression[]
                    {
                        name_expr,
                        BinderHelpers.NewPhpArray(bound.Arguments, bound.Context, bound.ClassContext),
                    };
                }

                return OverloadBinder.BindOverloadCall(_returnType, bound.TargetInstance, call.Methods, bound.Context, call_args,
                    isStaticCallSyntax: true,
                    lateStaticType: bound.TargetType,
                    classContext: bound.ClassContext);
            }

            //
            return base.BindMissingMethod(bound);
        }
    }

    #endregion
}
