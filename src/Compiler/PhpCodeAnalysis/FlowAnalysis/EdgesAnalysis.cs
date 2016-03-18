using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Semantics;
using System.Diagnostics;
using Pchp.CodeAnalysis.Semantics;
using Pchp.Syntax.Text;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    public class EdgesAnalysis : GraphVisitor
    {
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
        protected void VisitCondition(BoundExpression condition, ConditionBranch branch)
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
            Accept(condition);
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

        //#region Visit overrides

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
        protected virtual TypeRefMask HandleTraversableUse(BoundExpression/*!*/varuse)
        {
            return TypeRefMask.AnyType;
        }

        //#endregion

        #region Fields

        /// <summary>
        /// The worklist to be used to enqueue next blocks.
        /// </summary>
        readonly Worklist<BoundBlock> _worklist;

        /// <summary>
        /// Gets current type context for type masks resolving.
        /// </summary>
        protected TypeRefContext TypeRefContext => _state.TypeRefContext;

        /// <summary>
        /// Current flow state.
        /// </summary>
        FlowState _state;

        #endregion

        #region Construction

        internal EdgesAnalysis(Worklist<BoundBlock> worklist, OperationVisitor opvisitor)
            : base(opvisitor)
        {
            Contract.ThrowIfNull(worklist);            
            _worklist = worklist;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Debug assert the state is initialized therefore we are in the middle on a block analysis.
        /// </summary>
        void AssertState()
        {
            Debug.Assert(_state != null);
        }

        /// <summary>
        /// Sets type of local variable in current state.
        /// </summary>
        protected virtual TypeRefMask SetVar(string name, TypeRefMask typemask)
        {
            AssertState();

            _state.SetVar(name, typemask);
            return typemask;
        }

        /// <summary>
        /// Helper method that merges state with the target block and determines whether to continue by visiting the target block.
        /// </summary>
        /// <param name="state">Locals state in which we are entering the target.</param>
        /// <param name="target">Target block.</param>
        /// <remarks>Only for traversing into blocks within the same routine (same type context).</remarks>
        private void TraverseToBlock(FlowState/*!*/state, BoundBlock/*!*/target)
        {
            Contract.ThrowIfNull(state);    // state should be already set by previous block
            
            var targetState = target.FlowState;
            if (targetState != null)
            {
                Debug.Assert(targetState.Common == state.Common);

                // block was visited already,
                // merge and check whether state changed
                state = state.Merge(targetState);   // merge states into new one
                if (state.Equals(targetState))
                    return; // state convergated, we don't have to analyse target block again
            }
            else
            {
                // block was not visited yet
                state = state.Clone();              // copy state into new one
            }

            // update target block state
            target.FlowState = state;

            //
            _worklist.Enqueue(target);
        }

        /// <summary>
        /// Called to initialize <see cref="VisitCFGBlock"/> call.
        /// Sets <see cref="_state"/> to known initial block state.
        /// </summary>
        protected virtual void VisitCFGBlockInit(BoundBlock/*!*/x)
        {
            Contract.ThrowIfNull(x.FlowState);   // state should be already set by previous edge
            _state = x.FlowState.Clone();        // TFlowState for the statements in the block            
        }

        #endregion

        #region GraphVisitor Members

        public override void VisitCFG(ControlFlowGraph x)
        {
            Contract.ThrowIfNull(x);
            Debug.Assert(x.Start.FlowState != null, "Start block has to have an initial state set.");

            _worklist.Enqueue(x.Start);
        }

        public override void VisitCFGBlock(BoundBlock x)
        {
            VisitCFGBlockInit(x);
            VisitCFGBlockInternal(x);   // modifies _state, traverses to the edge
        }

        public override void VisitCFGCaseBlock(CaseBlock x)
        {
            VisitCFGBlockInit(x);
            Accept(x.CaseValue);
            VisitCFGBlockInternal(x);
        }

        public override void VisitCFGCatchBlock(CatchBlock x)
        {
            VisitCFGBlockInit(x);

            // add catch control variable to the state
            _state.SetVar(x.VariableName.Value, this.TypeRefContext.GetTypeMask(x.TypeRef, true));
            _state.SetVarUsed(x.VariableName.Value);  // marks variable as Used since in PHP you can't have catch block without specifying the variable we won't report unused catch variable
            VisitCFGBlockInternal(x);
        }

        public override void VisitCFGSimpleEdge(SimpleEdge x)
        {
            TraverseToBlock(_state, x.NextBlock);
        }

        public override void VisitCFGConditionalEdge(ConditionalEdge x)
        {
            // build state for TrueBlock and FalseBlock properly, take minimal evaluation into account
            var state = _state;

            // true branch
            _state = state.Clone();
            VisitCondition(x.Condition, ConditionBranch.ToTrue);
            TraverseToBlock(_state, x.TrueTarget);

            // false branch
            _state = state.Clone();
            VisitCondition(x.Condition, ConditionBranch.ToFalse);
            TraverseToBlock(_state, x.FalseTarget);
        }

        public override void VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x)
        {
            Accept(x.Enumeree);
            VisitCFGSimpleEdge(x);
        }

        public override void VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            var state = _state;
            // get type information from Enumeree to determine types value variable
            var elementType = HandleTraversableUse(x.EnumereeEdge.Enumeree);
            if (elementType.IsVoid) elementType = TypeRefMask.AnyType;

            // Body branch
            _state = state.Clone();
            // set key variable and value variable at current state
            var valueVar = x.ValueVariable;
            if (valueVar.List == null)
            {
                var keyVar = x.KeyVariable;
                var dvar = x.ValueVariable.Variable as Syntax.AST.DirectVarUse;
                if (dvar != null)
                {
                    this.SetVar(dvar.VarName.Value, elementType);
                    if (x.KeyVariable != null)
                        _state.SetVarUsed(dvar.VarName.Value);    // do not report value variable as unused if there is also key variable. In PHP we can't enumerate keys without enumerating value var
                }

                if (x.KeyVariable != null)
                {
                    dvar = x.KeyVariable.Variable as Syntax.AST.DirectVarUse;
                    if (dvar != null)
                        this.SetVar(dvar.VarName.Value, TypeRefMask.AnyType);
                }
            }
            else
            {
                throw new NotImplementedException();
                //VisitListEx(valueVar.List, elementType);
            }
            TraverseToBlock(_state, x.BodyBlock);

            // End branch
            _state = state.Clone();
            TraverseToBlock(_state, x.NextBlock);
        }

        public override void VisitCFGSwitchEdge(SwitchEdge x)
        {
            Accept(x.SwitchValue);

            var state = _state;

            foreach (var c in x.CaseBlocks)
            {
                TraverseToBlock(state, c);
            }
        }

        public override void VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            var state = _state;

            TraverseToBlock(state, x.BodyBlock);

            foreach (var c in x.CatchBlocks)
            {
                TraverseToBlock(state, c);
            }

            if (x.FinallyBlock != null)
            {
                TraverseToBlock(state, x.FinallyBlock);
            }
        }

        #endregion
    }
}
