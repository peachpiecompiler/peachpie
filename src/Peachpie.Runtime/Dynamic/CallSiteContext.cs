using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Pchp.Core.Reflection;

namespace Pchp.Core.Dynamic
{
    /// <summary>
    /// Provides information about the call.
    /// </summary>
    internal class CallSiteContext
    {
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

            // args[taken..count]
            this.Arguments = (taken == args.Length)
                ? Array.Empty<Expression>()
                : args.Skip(taken).Select(x => x.Expression).ToArray();

            //
            return this;
        }

        void ProcessTarget(DynamicMetaObject target)
        {
            Debug.Assert(target != null);

            if (BinderHelpers.TryTargetAsObject(target, out target))
            {
                this.AddRestriction(target.Restrictions);
                this.TargetInstance = target.Expression;
                this.CurrentTargetInstance = target.Value;
                if (this.TargetType == null && target.Value != null)
                {
                    this.TargetType = target.Value.GetPhpTypeInfo();
                }
            }
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
            else if (this.Context == null && arg.Value is Context)
            {
                this.Context = arg.Expression;
                this.CurrentContext = (Context)arg.Value;
                return true;
            }

            return false;
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

        /// <summary>
        /// Resolved actual arguments.
        /// </summary>
        public Expression[]/*!!*/Arguments { get; set; }

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
    }
}
