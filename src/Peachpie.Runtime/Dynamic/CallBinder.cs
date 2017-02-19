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
        /// Gets value indicating whether the function has a target (instance or type).
        /// </summary>
        public abstract bool HasTarget { get; }

        /// <summary>
        /// Resolves indirect function name.
        /// </summary>
        protected abstract DynamicMetaObject PopNameExpression(/*in, out*/IList<DynamicMetaObject> args, ref BindingRestrictions restrictions);

        protected virtual PhpTypeInfo PopTypeInfoOrNull(/*in, out*/IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            return null;
        }

        /// <summary>
        /// Gets known functio name.
        /// </summary>
        protected abstract string NameValue { get; }

        /// <summary>
        /// Resolves methods to be called.
        /// </summary>
        /// <param name="ctx">Actual context.</param>
        /// <param name="tinfo">Type as resolved by <see cref="PopTypeInfoOrNull"/>.</param>
        /// <param name="nameExpr">Name as resolved by <see cref="PopNameExpression"/>.</param>
        /// <param name="target">Target expression.</param>
        /// <param name="args">Argument expressions.
        /// If some arguments are special and used to resolve methods, they shall be removed from the list.
        /// Remaining arguments are used as actual method call arguments.</param>
        /// <param name="restrictions">Binding restictions.</param>
        /// <returns>Array of methods.</returns>
        protected abstract MethodBase[] ResolveMethods(DynamicMetaObject ctx, PhpTypeInfo tinfo, DynamicMetaObject nameExpr, ref DynamicMetaObject target, /*in, out*/IList<DynamicMetaObject> args, ref BindingRestrictions restrictions);

        protected virtual Expression BindMissingMethod(DynamicMetaObject ctx, PhpTypeInfo tinfo, DynamicMetaObject nameExpr, DynamicMetaObject target, /*in, out*/IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            string nameText = NameValue;

            if (nameText == null)
            {
                if (nameExpr != null)
                {
                    nameText = nameExpr.Value.ToString();
                }
                else
                {
                    nameText = "<unknown>";
                }
            }

            // TODO: ErrCode method not found
            throw new ArgumentException(string.Format("Function '{0}' not found!", nameText));
        }

        protected void Combine(ref BindingRestrictions restrictions, BindingRestrictions restriction)
        {
            restrictions = restrictions.Merge(restriction);
        }

        DynamicMetaObject PopTargetOrNull(/*in, out*/IList<DynamicMetaObject> args)
        {
            DynamicMetaObject target = null;

            if (HasTarget)
            {
                Debug.Assert(args.Count != 0);
                target = args[0];
                args.RemoveAt(0);
            }

            return target;
        }

        #region DynamicMetaObjectBinder

        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var restrictions = BindingRestrictions.Empty;
            var argsList = new List<DynamicMetaObject>(args);

            // ctx, [Target], [TypeName], [RoutineName], [args...]
            Debug.Assert(target?.Value is Context);

            var ctx = target;
            target = PopTargetOrNull(argsList);
            var tinfo = PopTypeInfoOrNull(argsList, ref restrictions);
            var nameExpr = PopNameExpression(argsList, ref restrictions);

            //
            Expression invocation;

            //
            var methods = ResolveMethods(ctx, tinfo, nameExpr, ref target, argsList, ref restrictions);
            if (methods != null && methods.Length != 0)
            {
                // TODO: visibility

                var expr_args = argsList.Select(x => x.Expression).ToArray();
                invocation = OverloadBinder.BindOverloadCall(_returnType, target?.Expression, methods, ctx.Expression, expr_args);
            }
            else
            {
                invocation = BindMissingMethod(ctx, tinfo, nameExpr, target, argsList, ref restrictions);
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

        protected override string NameValue => _name;

        protected override DynamicMetaObject PopNameExpression(IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            if (_name == null)
            {
                // dynamic name, first args[0]
                Debug.Assert(args.Count >= 1);
                var nameExpr = args[0];
                args.RemoveAt(0);

                // dereference expression to a callable
                return ResolveNameExpression(nameExpr, ref restrictions);
            }

            return null;
        }

        DynamicMetaObject ResolveNameExpression(DynamicMetaObject nameExpr, ref BindingRestrictions restrictions)
        {
            var nameObj = nameExpr.Value;
            if (nameObj == null)
            {
                Combine(ref restrictions, BindingRestrictions.GetInstanceRestriction(nameExpr.Expression, Expression.Constant(null)));
                // TODO: Err invalid callback
                return null;
            }

            // see PhpCallback for options:

            Combine(ref restrictions, BindingRestrictions.GetTypeRestriction(nameExpr.Expression, nameExpr.RuntimeType));

            // string
            if (nameObj is string)
            {
                // restriction: nameExpr == "name"  // TODO: ignore case
                Combine(ref restrictions, BindingRestrictions.GetExpressionRestriction(Expression.Equal(nameExpr.Expression, Expression.Constant((string)nameObj))));
                return nameExpr;
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
                    return ResolveNameExpression(
                        new DynamicMetaObject(
                            Expression.Property(Expression.Convert(nameExpr.Expression, typeof(PhpValue)), "Object"),
                            BindingRestrictions.Empty,
                            value.Object),
                        ref restrictions);
                }
            }

            // PhpAlias

            throw new NotImplementedException(nameObj.GetType().ToString());
        }
        
        protected override MethodBase[] ResolveMethods(DynamicMetaObject ctx, PhpTypeInfo tinfo, DynamicMetaObject nameExpr, ref DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(target == null);
            Debug.Assert(args != null);

            object name = _name ?? nameExpr?.Value;

            if (name is string)
            {
                return ResolveMethods(ctx, ref target, (string)name, _nameOpt, ref restrictions);
            }
            else
            {
                throw new NotImplementedException();
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

        protected override string NameValue => _name;

        protected override DynamicMetaObject PopNameExpression(IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            if (_name == null)
            {
                // indirect function name

                Debug.Assert(args.Count >= 1 && args[0].LimitType == typeof(string));
                Debug.Assert(args[0].Value is string);

                var nameExpr = args[0];
                args.RemoveAt(0);

                restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Equal(nameExpr.Expression, Expression.Constant(nameExpr.Value)))); // args[0] == "VALUE"

                return nameExpr;
            }

            return null;
        }

        protected override MethodBase[] ResolveMethods(DynamicMetaObject ctx, PhpTypeInfo tinfo, DynamicMetaObject nameExpr, ref DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            // resolve target expression:
            Expression target_expr;
            object target_value;
            BinderHelpers.TargetAsObject(target, out target_expr, out target_value, ref restrictions);

            // (NULL)->
            if (target_value == null)
            {
                return null;    // no methods
            }

            // target restrictions
            if (target_value != null && !target_expr.Type.GetTypeInfo().IsSealed)
            {
                restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(target_expr, target_value.GetType()));
                target_expr = Expression.Convert(target_expr, target_value.GetType());
            }

            target = new DynamicMetaObject(target_expr, target.Restrictions, target_value);

            string name = _name ?? (string)nameExpr.Value;

            // candidates:
            var candidates = target.RuntimeType.GetPhpTypeInfo().SelectRuntimeMethods(name).SelectVisible(_classCtx).ToList();
            return candidates.ToArray();
        }

        protected override Expression BindMissingMethod(DynamicMetaObject ctx, PhpTypeInfo tinfo, DynamicMetaObject nameMeta, DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            var name_expr = (_name != null) ? Expression.Constant(_name) : nameMeta?.Expression;

            // resolve target expression:
            Expression target_expr;
            object target_value;
            BinderHelpers.TargetAsObject(target, out target_expr, out target_value, ref restrictions);

            if (target_value == null)
            {
                /* Template:
                 * PhpException.MethodOnNonObject(name_expr); // aka PhpException.Throw(Error, method_called_on_non_object, name_expr)
                 * return NULL;
                 */
                var throwcall = Expression.Call(typeof(PhpException), "MethodOnNonObject", Array.Empty<Type>(), ConvertExpression.Bind(name_expr, typeof(string), ctx.Expression));
                return Expression.Block(throwcall, ConvertExpression.BindDefault(this.ReturnType));
            }

            Debug.Assert(ReflectionUtils.IsClassType(target_value.GetType().GetTypeInfo()));

            tinfo = target_value.GetPhpTypeInfo();
            
            var call = BinderHelpers.FindMagicMethod(tinfo, TypeMethods.MagicMethods.__call);
            if (call != null)
            {
                // target.__call(name, array)
                var call_args = new Expression[]
                {
                    name_expr,
                    BinderHelpers.NewPhpArray(ctx.Expression, args.Select(a => a.Expression)),
                };
                return OverloadBinder.BindOverloadCall(_returnType, target.Expression, call.Methods, ctx.Expression, call_args);
            }

            return base.BindMissingMethod(ctx, tinfo, nameMeta, target, args, ref restrictions);
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

        public override bool HasTarget => true; // there is caller instance or null as a target

        protected override string NameValue => _name;

        internal CallStaticMethodBinder(RuntimeTypeHandle type, string name, RuntimeTypeHandle classContext, RuntimeTypeHandle returnType, int genericParams)
            : base(returnType, genericParams)
        {
            _type = type.Equals(default(RuntimeTypeHandle)) ? null : Type.GetTypeFromHandle(type);
            _name = name;
            _classCtx = classContext.Equals(default(RuntimeTypeHandle)) ? null : Type.GetTypeFromHandle(classContext);
        }

        protected override PhpTypeInfo PopTypeInfoOrNull(IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            var t = _type;
            if (t == null)
            {
                Debug.Assert(args.Count >= 1);
                Debug.Assert(args[0].LimitType == typeof(PhpTypeInfo));

                var tExpr = args[0];
                args.RemoveAt(0);

                restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(tExpr.Expression, tExpr.Value)); // args[0] == PhpTypeInfo

                return (PhpTypeInfo)tExpr.Value;
            }

            return t.GetPhpTypeInfo();
        }

        protected override DynamicMetaObject PopNameExpression(IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            if (_name == null)
            {
                // args[0]

                // indirect function name

                Debug.Assert(args.Count >= 1 && args[0].LimitType == typeof(string));
                Debug.Assert(args[0].Value is string);

                var nameExpr = args[0];
                args.RemoveAt(0);

                restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Expression.Equal(nameExpr.Expression, Expression.Constant(nameExpr.Value)))); // args[0] == "VALUE"

                return nameExpr;
            }

            return null;
        }

        protected override MethodBase[] ResolveMethods(DynamicMetaObject ctx, PhpTypeInfo tinfo, DynamicMetaObject nameExpr, ref DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            // check tinfo is assignable from target
            if (target?.Value == null || !tinfo.Type.IsAssignableFrom(target.LimitType))
            {
                target = null;
            }

            //
            var nameStr = _name ?? (string)nameExpr.Value;
            if (nameStr != null)
            {
                // candidates:
                var candidates = tinfo.SelectRuntimeMethods(nameStr).SelectVisible(_classCtx).SelectStatic().ToList();
                return candidates.ToArray();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected override Expression BindMissingMethod(DynamicMetaObject ctx, PhpTypeInfo tinfo, DynamicMetaObject nameMeta, DynamicMetaObject target, IList<DynamicMetaObject> args, ref BindingRestrictions restrictions)
        {
            var call = BinderHelpers.FindMagicMethod(tinfo, target == null ? TypeMethods.MagicMethods.__callstatic : TypeMethods.MagicMethods.__call);
            if (call != null)
            {
                var name_expr = (_name != null) ? Expression.Constant(_name) : nameMeta?.Expression;
                
                // T.__callStatic(name, array)
                var call_args = new Expression[]
                {
                    name_expr,
                    BinderHelpers.NewPhpArray(ctx.Expression, args.Select(a => a.Expression)),
                };
                return OverloadBinder.BindOverloadCall(_returnType, target?.Expression, call.Methods, ctx.Expression, call_args);
            }

            //
            return base.BindMissingMethod(ctx, tinfo, nameMeta, target, args, ref restrictions);
        }
    }

    #endregion
}
