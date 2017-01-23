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
    #region CallBinderFactory

    public abstract class CallBinderFactory
    {
        public static CallSiteBinder Function(string name, string nameOpt, RuntimeTypeHandle returnType, int genericParams)
        {
            return new CallFunctionBinder(name, nameOpt, returnType, genericParams);
        }

        public static CallSiteBinder InstanceFunction(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
        {
            return new CallInstanceMethodBinder(name, classContext, returnType, genericParams);
        }

        public static CallSiteBinder StaticFunction(RuntimeTypeHandle type, string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
        {
            return new CallStaticMethodBinder(type, name, classContext, returnType, genericParams);
        }
    }

    #endregion

    abstract class CallBinder : DynamicMetaObjectBinder
    {
        protected readonly Type _returnType;
        protected readonly int _genericParamsCount;

        public override Type ReturnType => _returnType;

        protected CallBinder(RuntimeTypeHandle returnType, int genericParams)
        {
            Debug.Assert(genericParams >= 0);

            _returnType = Type.GetTypeFromHandle(returnType);
            _genericParamsCount = genericParams;
        }

        /// <summary>
        /// Gets value indicating whether the function has a target (instance or type). Otherwise <c>target</c> is actually a context.
        /// </summary>
        public abstract bool HasTarget { get; }

        /// <summary>
        /// Resolves methods to be called.
        /// </summary>
        /// <param name="ctx">Actual context.</param>
        /// <param name="target">Target expression.</param>
        /// <param name="args">Argument expressions.
        /// If some arguments are special and used to resolve methods, they shall be removed from the list.
        /// Remaining arguments are used as actual method call arguments.</param>
        /// <param name="restrictions">Binding restictions.</param>
        /// <returns>Array of methods.</returns>
        protected abstract MethodBase[] ResolveMethods(DynamicMetaObject ctx, ref DynamicMetaObject target, /*in, out*/IList<DynamicMetaObject> args, ref BindingRestrictions restrictions);

        protected virtual Expression BindMissingMethod(DynamicMetaObject ctx, DynamicMetaObject target, /*in, out*/IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            // TODO: ErrCode method not found
            throw new ArgumentException("Function not found!");
        }

        protected void Combine(ref BindingRestrictions restrictions, BindingRestrictions restriction)
        {
            restrictions = restrictions.Merge(restriction);
        }

        #region DynamicMetaObjectBinder

        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var restrictions = BindingRestrictions.Empty;
            var argsList = new List<DynamicMetaObject>(args);

            // get target and context

            DynamicMetaObject ctx;

            if (HasTarget)
            {
                ctx = args[0];
                argsList.RemoveAt(0);
            }
            else
            {
                ctx = target;
                target = null;
            }

            //
            Expression invocation;

            var methods = ResolveMethods(ctx, ref target, argsList, ref restrictions);
            if (methods != null && methods.Length != 0)
            {
                var expr_args = argsList.Select(x => x.Expression).ToArray();
                invocation = OverloadBinder.BindOverloadCall(_returnType, target?.Expression, methods, ctx.Expression, expr_args);
            }
            else
            {
                invocation = BindMissingMethod(ctx, target, argsList, ref restrictions);
            }

            // TODO: by alias or by value
            return new DynamicMetaObject(invocation, restrictions);
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

        public override bool HasTarget => false;

        internal CallFunctionBinder(string name, string nameOpt, RuntimeTypeHandle returnType, int genericParams)
            : base(returnType, genericParams)
        {
            _name = name;
            _nameOpt = nameOpt;
        }

        protected override MethodBase[] ResolveMethods(DynamicMetaObject ctx, ref DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(target == null);
            Debug.Assert(args != null);

            if (_name != null)
            {
                return ResolveMethods(ctx, ref target, _name, _nameOpt, ref restrictions);
            }
            else
            {
                // dynamic name, first args[0]
                Debug.Assert(args.Count >= 1);
                var nameObj = args[0];
                args.RemoveAt(0);

                return ResolveMethods(ctx, ref target, nameObj.Expression, nameObj.Value, ref restrictions);
            }
        }

        MethodBase[] ResolveMethods(DynamicMetaObject ctx, ref DynamicMetaObject target, string name, string nameOpt, ref BindingRestrictions restrictions)
        {
            var ctxInstance = (Context)ctx.Value;
            var routine = ctxInstance.GetDeclaredFunction(name) ?? ((nameOpt != null) ? ctxInstance.GetDeclaredFunction(nameOpt) : null);

            if (routine == null)
            {
                return null;
            }

            if (routine is PhpRoutineInfo || routine is DelegateRoutineInfo)
            {
                Debug.Assert(routine.Index != 0);
            
                // restriction: ctx.CheckFunctionDeclared(index, routine.GetHashCode())
                var checkExpr = Expression.Call(
                    ctx.Expression,
                    typeof(Context).GetMethod("CheckFunctionDeclared", typeof(int), typeof(int)),
                    Expression.Constant(routine.Index), Expression.Constant(routine.GetHashCode()));

                Combine(ref restrictions, BindingRestrictions.GetExpressionRestriction(checkExpr));
            }
            else if (routine is ClrRoutineInfo)
            {
                // CLR routines persist across whole app, no restriction needed
            }

            // 
            var targetInstance = routine.Target;
            if (targetInstance != null)
            {
                target = new DynamicMetaObject(Expression.Constant(targetInstance), BindingRestrictions.Empty, targetInstance);
            }

            //
            return routine.Methods;
        }

        MethodBase[] ResolveMethods(DynamicMetaObject ctx, ref DynamicMetaObject target, Expression nameExpr, object nameObj, ref BindingRestrictions restrictions)
        {
            if (nameObj == null)
            {
                Combine(ref restrictions, BindingRestrictions.GetInstanceRestriction(nameExpr, Expression.Constant(null)));
                // TODO: Err invalid callback
                return null;
            }

            // see PhpCallback for options:

            Combine(ref restrictions, BindingRestrictions.GetTypeRestriction(nameExpr, nameObj.GetType()));

            // string
            if (nameObj is string)
            {
                // restriction: nameExpr == "name"
                Combine(ref restrictions, BindingRestrictions.GetExpressionRestriction(Expression.Equal(nameExpr, Expression.Constant((string)nameObj))));   // TODO: ignore case
                return ResolveMethods(ctx, ref target, (string)nameObj, null, ref restrictions);
            }

            // array[2]

            // object with __invoke

            // IPhpCallable

            // delegate

            // PhpString

            // PhpValue
            if (nameObj is PhpValue)
            {
                var value = (PhpValue)nameObj;
                if (value.Object != null)
                {
                    // ((PhpValue)name).Object
                    var nameObjectExpr = Expression.Property(Expression.Convert(nameExpr, typeof(PhpValue)), "Object");
                    return ResolveMethods(ctx, ref target, nameObjectExpr, value.Object, ref restrictions);
                }
            }

            // PhpAlias

            throw new NotImplementedException(nameObj.GetType().ToString());
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

        public override bool HasTarget => true;

        internal CallInstanceMethodBinder(string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
            : base(returnType, genericParams)
        {
            _name = name;
            _classCtx = classContext.Equals(default(RuntimeTypeHandle)) ? null : Type.GetTypeFromHandle(classContext);
        }

        protected override MethodBase[] ResolveMethods(DynamicMetaObject ctx, ref DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            if (target.Value == null)
            {
                Combine(ref restrictions, BindingRestrictions.GetInstanceRestriction(target.Expression, Expression.Constant(null)));
                // TODO: Err instance is null
                return null;
            }

            // resolve target expression:
            Expression target_expr;
            object target_value;
            BinderHelpers.TargetAsObject(target, out target_expr, out target_value, ref restrictions);

            // target restrictions
            if (target_value != null && !target_expr.Type.GetTypeInfo().IsSealed)
            {
                restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target_expr, target_value.GetType()));
                target_expr = Expression.Convert(target_expr, target_value.GetType());
            }

            target = new DynamicMetaObject(target_expr, target.Restrictions, target_value);

            string name = _name;

            if (_name == null)
            {
                // indirect function name

                Debug.Assert(args.Count >= 1 && args[0].LimitType == typeof(string));
                Debug.Assert(args[0].Value is string);

                restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Equal(args[0].Expression, Expression.Constant(args[0].Value)))); // args[0] == "VALUE"

                name = (string)args[0].Value;
                args.RemoveAt(0);
            }

            // candidates:
            var candidates = target.RuntimeType.SelectCandidates().SelectByName(name).SelectVisible(_classCtx).ToList();
            return candidates.ToArray();
        }

        protected override Expression BindMissingMethod(DynamicMetaObject ctx, DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            var tinfo = target.RuntimeType.GetPhpTypeInfo();
            var call = BinderHelpers.FindMagicMethod(tinfo, TypeMethods.MagicMethods.__call);
            if (call != null)
            {
                if (_name == null)
                {
                    throw new NotImplementedException();
                }

                // target.__call(name, array)
                var call_args = new Expression[]
                {
                    Expression.Constant(_name),
                    BinderHelpers.NewPhpArray(ctx.Expression, args.Select(a => a.Expression)),
                };
                return OverloadBinder.BindOverloadCall(_returnType, target.Expression, call.Methods, ctx.Expression, call_args);
            }

            return base.BindMissingMethod(ctx, target, args, ref restrictions);
        }
    }

    #endregion

    #region CallStaticMethodBinder

    /// <summary>
    /// Binder to an instance function call.
    /// </summary>
    class CallStaticMethodBinder : CallBinder
    {
        readonly Type _type;
        readonly string _name;
        readonly Type _classCtx;

        public override bool HasTarget => _type == null;    // target is a type name in case it is known in compile time

        internal CallStaticMethodBinder(RuntimeTypeHandle type, string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
            : base(returnType, genericParams)
        {
            _type = type.Equals(default(RuntimeTypeHandle)) ? null : Type.GetTypeFromHandle(type);
            _name = name;
            _classCtx = classContext.Equals(default(RuntimeTypeHandle)) ? null : Type.GetTypeFromHandle(classContext);
        }

        Type ResolveType(Context ctx, Expression target)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(target == null || target.Type == typeof(PhpTypeInfo));

            var t = _type;
            if (t == null)
            {
                throw new NotImplementedException();
            }

            return t;
        }

        protected override MethodBase[] ResolveMethods(DynamicMetaObject ctx, ref DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            var t = ResolveType((Context)ctx.Value, target?.Expression);

            if (_name != null)
            {
                // candidates:
                var candidates = t.SelectCandidates().SelectByName(_name).SelectVisible(_classCtx).SelectStatic().ToList();
                return candidates.ToArray();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected override Expression BindMissingMethod(DynamicMetaObject ctx, DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            var tinfo = ResolveType((Context)ctx.Value, target?.Expression).GetPhpTypeInfo();
            var call = BinderHelpers.FindMagicMethod(tinfo, TypeMethods.MagicMethods.__callstatic);
            if (call != null)
            {
                if (_name == null)
                {
                    throw new NotImplementedException();
                }

                // T.__callStatic(name, array)
                var call_args = new Expression[]
                {
                    Expression.Constant(_name),
                    BinderHelpers.NewPhpArray(ctx.Expression, args.Select(a => a.Expression)),
                };
                return OverloadBinder.BindOverloadCall(_returnType, null, call.Methods, ctx.Expression, call_args);
            }

            //
            return base.BindMissingMethod(ctx, target, args, ref restrictions);
        }
    }

    #endregion
}
