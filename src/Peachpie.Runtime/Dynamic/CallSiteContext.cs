using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Provides information about the call.
    /// </summary>
    [DebuggerNonUserCode]
    internal class CallSiteContext
    {
        public CallSiteContext(bool isStaticSyntax)
        {
            IsStaticSyntax = isStaticSyntax;
        }

        public CallSiteContext ProcessArgs(DynamicMetaObject target, DynamicMetaObject[] args, bool hasTargetInstance)
        {
            if (hasTargetInstance)
            {
                ProcessTarget(target);
            }
            else
            {
                ProcessArg(target);
            }

            int taken = 0;
            while (taken < args.Length && ProcessArg(args[taken]))
            {
                taken++;
            }

            // bind trait definition !TSelf eventually
            BindTraitSelf();

            // Arguments = args[taken..count]
            if (taken == args.Length)
            {
                this.Arguments = Array.Empty<Expression>();
            }
            else
            {
                this.Arguments = new Expression[args.Length - taken];
                for (int i = taken; i < args.Length; i++)
                {
                    this.Arguments[i - taken] = args[i].Expression;
                }
            }

            //
            return this;
        }

        void ProcessTarget(DynamicMetaObject target)
        {
            Debug.Assert(target != null);

            if (BinderHelpers.TryTargetAsObject(target, out target))
            {
                this.TargetInstance = target.Expression;
                this.CurrentTargetInstance = target.Value;
                if (this.TargetType == null && target.HasValue)
                {
                    this.TargetType = target.RuntimeType.GetPhpTypeInfo();
                }
            }

            // merge restrictions:
            this.AddRestriction(target.Restrictions);
        }

        bool ProcessArg(DynamicMetaObject arg)
        {
            if (arg.Value is ISpecialParamHolder sp && sp.IsImplicit)
            {
                // load information from special parameter into this
                // and updates binding restrictions
                sp.Process(this, Expression.Field(arg.Expression, "Value"));
                return true;
            }
            else if (this.Context == null && arg.Value is Context ctx)
            {
                this.Context = arg.Expression;
                this.CurrentContext = ctx;
                return true;
            }
            else if (arg.Value is Type[] typeargs)
            {
                ProcessTypeArguments(typeargs, arg.Expression);
                return true;
            }

            return false;
        }

        void ProcessTypeArguments(Type[] typeargs, Expression arg)
        {
            // 
            this.TypeArguments = typeargs;

            // restriction: ArrayUtils.Equals<Type>( arg, typeargs )
            AddRestriction(Expression.Call(
                new Func<Type[], Type[], bool>(Utilities.ArrayUtils.Equals<System.Type>).Method,
                arg0: arg,
                arg1: Expression.Constant(typeargs)));
        }

        /// <summary>
        /// In case <see cref="TargetType"/> is a trait definition,
        /// this method binds its <c>TSelf</c> generic parameter to current class context.
        /// </summary>
        void BindTraitSelf()
        {
            if (TargetType != null && TargetType.Type.IsGenericTypeDefinition && TargetType.IsTrait)
            {
                var TSelf = TargetType.Type.MakeGenericType(typeof(object));
                TargetType = TargetType.Type.MakeGenericType(TSelf).GetPhpTypeInfo();
            }
        }

        internal void AddRestriction(Expression restriction)
        {
            AddRestriction(BindingRestrictions.GetExpressionRestriction(restriction));
        }

        internal void AddRestriction(BindingRestrictions restriction)
        {
            this.Restrictions = this.Restrictions.Merge(restriction);
        }

        public bool IsStaticSyntax { get; }

        /// <summary>
        /// Resolved actual arguments.
        /// </summary>
        public Expression[]/*!!*/Arguments { get; set; }

        /// <summary>
        /// Gets value indicating there is an argument unpacking (<c>...</c>) in <see cref="Arguments"/> list.
        /// </summary>
        public bool HasArgumentUnpacking => Arguments.Length != 0 && Arguments.Any(BinderHelpers.IsArgumentUnpacking);

        /// <summary>
        /// Resolved binding restrictions.
        /// </summary>
        public BindingRestrictions Restrictions/*!*/{ get; private set; } = BindingRestrictions.Empty;

        /// <summary>
        /// Runtime context expression.
        /// </summary>
        public Expression Context { get; set; }

        /// <summary>
        /// Current runtime context.
        /// </summary>
        public Context CurrentContext { get; private set; }

        /// <summary>
        /// Either instance or null.
        /// </summary>
        public Expression TargetInstance { get; set; }

        /// <summary>
        /// Optional. Late static bound type, results in <see cref="PhpTypeInfo"/>.
        /// </summary>
        public Expression LateStaticType { get; set; }

        /// <summary>
        /// Current target instance or <c>null</c>.
        /// </summary>
        public object CurrentTargetInstance { get; set; }

        /// <summary>
        /// Gets <see cref="PhpTypeInfo"/> of target if any.
        /// </summary>
        public PhpTypeInfo TargetType { get; set; }

        /// <summary>
        /// Current class context.
        /// </summary>
        public Type ClassContext { get; set; }

        /// <summary>
        /// Name specified indirectly.
        /// </summary>
        public Expression IndirectName { get; set; }

        /// <summary>
        /// Gets member name if set in compile time or if provided by <see cref="IndirectName"/>.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type arguments in case of a generic method call.
        /// Can be <c>null</c> or empty array.
        /// </summary>
        public Type[] TypeArguments { get; set; }
    }
}
