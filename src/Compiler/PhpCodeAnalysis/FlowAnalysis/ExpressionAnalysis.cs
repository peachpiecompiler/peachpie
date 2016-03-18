using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Syntax.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Visits single expressions and project transformations to flow state.
    /// </summary>
    internal class ExpressionAnalysis : OperationVisitor
    {
        #region Fields

        EdgesAnalysis _analysis;

        /// <summary>
        /// Gets current type context for type masks resolving.
        /// </summary>
        protected TypeRefContext TypeRefContext => _analysis.TypeRefContext;

        /// <summary>
        /// Current flow state.
        /// </summary>
        protected FlowState State => _analysis.State;

        #endregion

        #region Short-Circuit Evaluation

        /// <summary>
        /// Visits condition used to branch execution to true or false branch.
        /// </summary>
        /// <remarks>
        /// Because of minimal evaluation there is different TFlowState for true and false branches,
        /// AND and OR operators have to take this into account.
        /// 
        /// Also some other constructs may have side-effect for known branch,
        /// eg. <c>($x instanceof X)</c> implies ($x is X) in True branch.
        /// </remarks>
        internal void AcceptCondition(BoundExpression condition, ConditionBranch branch)
        {
            Contract.ThrowIfNull(condition);

            if (branch != ConditionBranch.AnyResult)
            {
                //if (condition is BoundBinaryEx)
                //{
                //    VisitBinaryEx((BinaryEx)condition, branch);
                //    return;
                //}
                //if (condition is BoundUnaryEx)
                //{
                //    VisitUnaryEx((UnaryEx)condition, branch);
                //    return;
                //}
                //if (condition is DirectFcnCall)
                //{
                //    VisitDirectFcnCall((DirectFcnCall)condition, branch);
                //    return;
                //}
                //if (condition is InstanceOfEx)
                //{
                //    VisitInstanceOfEx((InstanceOfEx)condition, branch);
                //    return;
                //}
                //if (condition is IssetEx)
                //{
                //    VisitIssetEx((IssetEx)condition, branch);
                //    return;
                //}
                //if (condition is EmptyEx)
                //{
                //    VisitEmptyEx((EmptyEx)condition, branch);
                //    return;
                //}
            }

            // no effect
            condition.Accept(this);
        }

        #endregion

        #region AccessType

        /// <summary>
        /// Access type - describes context within which an expression is used.
        /// </summary>
        [Flags]
        protected enum AccessFlags : byte
        {
            /// <summary>
            /// Expression is being read from.
            /// </summary>
            Read = 0,

            /// <summary>
            /// Expression is LValue of an assignment. Do not report uninitialized var use.
            /// </summary>
            Write = 1,

            /// <summary>
            /// Expression is LValue accessed with array item operator.
            /// </summary>
            EnsureArray = 2,

            /// <summary>
            /// Expression is LValue accessed by object operator (<c>-></c>).
            /// </summary>
            EnsureProperty = 4,

            /// <summary>
            /// Expression is being checked within <c>isset</c> function call. Do not report uninitialized var use.
            /// </summary>
            IsCheck = 8,

            /// <summary>
            /// Expression is used as a statement, return value is not used.
            /// </summary>
            None = Read,
        }

        #endregion

        #region Access

        protected struct Access
        {
            public bool IsRead { get { return !IsWrite; } }
            public bool IsWrite { get { return (flags & AccessFlags.Write) != 0; } }
            public bool IsCheck { get { return (flags & AccessFlags.IsCheck) != 0; } }
            public bool IsEnsureArray { get { return (flags & AccessFlags.EnsureArray) != 0; } }
            public bool IsEnsureHasProperty { get { return (flags & AccessFlags.EnsureProperty) != 0; } }

            /// <summary>
            /// Type of expression access.
            /// </summary>
            private AccessFlags flags;

            /// <summary>
            /// In case of Write* access, specifies the type being written.
            /// </summary>
            public TypeRefMask RValueType;

            /// <summary>
            /// Span of right value for error reporting.
            /// </summary>
            public Span RValueSpan;

            public static Access Read { get { return new Access() { flags = AccessFlags.Read }; } }
            public static Access Check { get { return new Access() { flags = AccessFlags.IsCheck | AccessFlags.Read }; } }
            public static Access Write(TypeRefMask rValueType, Span rValueSpan) { return new Access() { flags = AccessFlags.Write, RValueType = rValueType, RValueSpan = rValueSpan }; }
            public static Access EnsureArray(TypeRefMask elementType) { return new Access() { flags = AccessFlags.EnsureArray | AccessFlags.Read, RValueType = elementType }; }

            /// <summary>
            /// Creates new <see cref="Access"/> with given check state.
            /// </summary>
            public static Access operator &(Access a, Access check)
            {
                a.flags |= (check.flags & AccessFlags.IsCheck);
                return a;
            }
        }

        #endregion

        #region Visit overrides

        //protected virtual void VisitBinaryEx(BinaryEx x, ConditionBranch branch)
        //{
        //    // to be overriden in derived class
        //    base.VisitBinaryEx(x);
        //}

        //protected virtual void VisitUnaryEx(UnaryEx x, ConditionBranch branch)
        //{
        //    // to be overriden in derived class
        //    base.VisitUnaryEx(x);
        //}

        //protected virtual void VisitInstanceOfEx(InstanceOfEx x, ConditionBranch branch)
        //{
        //    // to be overriden in derived class
        //    base.VisitInstanceOfEx(x);
        //}

        //protected virtual void VisitDirectFcnCall(DirectFcnCall x, ConditionBranch branch)
        //{
        //    // to be overriden in derived class
        //    base.VisitDirectFcnCall(x);
        //}
        //public virtual void VisitIssetEx(IssetEx x, ConditionBranch branch)
        //{
        //    base.VisitIssetEx(x);
        //}

        //public virtual void VisitEmptyEx(EmptyEx x, ConditionBranch branch)
        //{
        //    base.VisitEmptyEx(x);
        //}

        ///// <summary>
        ///// Explicitly given R-Value for the list. Used when traversing <c>foreach</c> and nested <c>list</c>.
        ///// Standard <c>list() = expr;</c> fallbacks to this one as well.
        ///// </summary>
        //protected virtual void VisitListEx(ListEx list, TypeRefMask rValueType)
        //{
        //    // to be overriden in derived class            
        //}

        //public sealed override void VisitListEx(ListEx x)
        //{
        //    // standard list expression, not foreach or nested one
        //    Contract.ThrowIfNull(x.RValue);
        //    VisitElement(x.RValue);
        //    VisitListEx(x, (x.RValue != null) ? (TypeRefMask)x.RValue.TypeInfoValue : TypeRefMask.AnyType);
        //}

        //public sealed override void VisitUnaryEx(UnaryEx x)
        //{
        //    VisitUnaryEx(x, ConditionBranch.Default);
        //}

        //public sealed override void VisitBinaryEx(BinaryEx x)
        //{
        //    VisitBinaryEx(x, ConditionBranch.Default);
        //}

        //public sealed override void VisitInstanceOfEx(InstanceOfEx x)
        //{
        //    VisitInstanceOfEx(x, ConditionBranch.AnyResult);
        //}

        //public sealed override void VisitDirectFcnCall(DirectFcnCall x)
        //{
        //    VisitDirectFcnCall(x, ConditionBranch.AnyResult);
        //}

        //public sealed override void VisitIssetEx(IssetEx x)
        //{
        //    VisitIssetEx(x, ConditionBranch.AnyResult);
        //}

        //public sealed override void VisitEmptyEx(EmptyEx x)
        //{
        //    VisitEmptyEx(x, ConditionBranch.AnyResult);
        //}

        /// <summary>
        /// Handles use of variable as foreach iterator value.
        /// </summary>
        /// <param name="varuse"></param>
        /// <returns>Derivate type of iterated values.</returns>
        internal virtual TypeRefMask HandleTraversableUse(BoundExpression/*!*/varuse)
        {
            return TypeRefMask.AnyType;
        }

        #endregion

        #region Construction

        public ExpressionAnalysis()
        {
        }

        internal void SetAnalysis(EdgesAnalysis analysis)
        {
            _analysis = analysis;
        }

        #endregion
    }
}
