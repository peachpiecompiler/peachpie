using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Operations;
using System.Diagnostics;
using Pchp.CodeAnalysis.Semantics;
using Devsense.PHP.Text;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    public abstract class AnalysisWalker<TState, TResult> : GraphWalker<TResult>
    {
        #region Nested enum: AnalysisFlags

        /// <summary>
        /// Analysis progress flags.
        /// </summary>
        protected enum AnalysisFlags
        {
            /// <summary>
            /// The analysis has been canceled since internal state has changed.
            /// </summary>
            IsCanceled = 1,
        }

        #endregion

        #region Fields

        /// <summary>
        /// Current flow state.
        /// </summary>
        internal TState State { get; private protected set; }

        /// <summary>
        /// Gets reference to current block.
        /// </summary>
        internal BoundBlock CurrentBlock { get; private set; }

        /// <summary>
        /// Gathered analysis progress.
        /// </summary>
        protected AnalysisFlags _flags;

        #endregion

        #region State and worklist handling

        protected abstract bool IsStateInitialized(TState state);

        protected abstract bool AreStatesEqual(TState a, TState b);

        protected abstract TState GetState(BoundBlock block);

        protected abstract void SetState(BoundBlock block, TState state);

        protected abstract TState CloneState(TState state);

        protected abstract TState MergeStates(TState a, TState b);

        protected abstract void SetStateUnknown(ref TState state);

        protected abstract void EnqueueBlock(BoundBlock block);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Debug assert the state is initialized therefore we are in the middle on a block analysis.
        /// </summary>
        void AssertState()
        {
            Debug.Assert(State != null);
        }

        /// <summary>
        /// Helper method that merges state with the target block and determines whether to continue by visiting the target block.
        /// </summary>
        /// <param name="edgeLabel">Label identifying incoming edge.</param>
        /// <param name="state">Locals state in which we are entering the target.</param>
        /// <param name="target">Target block.</param>
        /// <remarks>Only for traversing into blocks within the same routine (same type context).</remarks>
        private void TraverseToBlock(object edgeLabel, TState/*!*/state, BoundBlock/*!*/target)
        {
            if (!IsStateInitialized(state))
                throw new ArgumentException(nameof(state)); // state should be already set by previous block

            var targetState = GetState(target);
            if (targetState != null)
            {
                // block was visited already,
                // merge and check whether state changed
                state = MergeStates(state, targetState);   // merge states into new one

                if (AreStatesEqual(state, targetState) && !target.ForceRepeatedAnalysis)
                {
                    // state converged, we don't have to analyse the target block again
                    // unless it is specially needed (e.g. ExitBlock)
                    return;
                }
            }
            else
            {
                // block was not visited yet
                state = CloneState(state);              // copy state into new one
            }

            // update target block state
            SetState(target, state);

            //
            EnqueueBlock(target);
        }

        /// <summary>
        /// Called to initialize <see cref="VisitCFGBlock"/> call.
        /// Sets <see cref="State"/> to known initial block state.
        /// </summary>
        protected virtual void VisitCFGBlockInit(BoundBlock/*!*/x)
        {
            var state = GetState(x);

            if (!IsStateInitialized(state))
                throw new ArgumentException(nameof(x));     // state should be already set by previous edge

            State = CloneState(state);     // TState for the statements in the block

            this.CurrentBlock = x;
        }

        /// <summary>
        /// Updates the expression access and visits it.
        /// </summary>
        /// <param name="x">The expression.</param>
        /// <param name="access">New access.</param>
        protected void Visit(BoundExpression x, BoundAccess access)
        {
            x.Access = access;
            Accept(x);
        }

        #endregion

        #region Short-Circuit Evaluation

        /// <summary>
        /// Visits condition used to branch execution to true or false branch.
        /// </summary>
        /// <returns>Value indicating whether branch was used.</returns>
        /// <remarks>
        /// Because of minimal evaluation there is different state for true and false branches,
        /// AND and OR operators have to take this into account.
        /// 
        /// Also some other constructs may have side-effect for known branch,
        /// eg. <c>($x instanceof X)</c> implies ($x is X) in True branch.
        /// </remarks>
        internal bool VisitCondition(BoundExpression condition, ConditionBranch branch)
        {
            Contract.ThrowIfNull(condition);

            if (branch != ConditionBranch.AnyResult)
            {
                if (condition is BoundBinaryEx)
                {
                    Visit((BoundBinaryEx)condition, branch);
                    return true;
                }
                if (condition is BoundUnaryEx unaryEx)
                {
                    Visit(unaryEx, branch);
                    return true;
                }
                if (condition is BoundGlobalFunctionCall)
                {
                    VisitGlobalFunctionCall((BoundGlobalFunctionCall)condition, branch);
                    return true;
                }
                if (condition is BoundInstanceOfEx)
                {
                    Visit((BoundInstanceOfEx)condition, branch);
                    return true;
                }
                if (condition is BoundIsSetEx)
                {
                    Visit((BoundIsSetEx)condition, branch);
                    return true;
                }
                //if (condition is EmptyEx)
                //{
                //    VisitEmptyEx((EmptyEx)condition, branch);
                //    return false;
                //}
            }

            // no effect
            condition.Accept(this);
            return false;
        }

        public sealed override TResult VisitBinaryExpression(BoundBinaryEx x)
        {
            Visit(x, ConditionBranch.Default);

            return default;
        }

        protected virtual void Visit(BoundBinaryEx x, ConditionBranch branch)
        {
            base.VisitBinaryExpression(x);
        }

        public sealed override TResult VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            VisitGlobalFunctionCall(x, ConditionBranch.Default);

            return default;
        }

        public virtual void VisitGlobalFunctionCall(BoundGlobalFunctionCall x, ConditionBranch branch)
        {
            base.VisitGlobalFunctionCall(x);
        }

        public sealed override TResult VisitUnaryExpression(BoundUnaryEx x)
        {
            Visit(x, ConditionBranch.Default);

            return default;
        }

        protected virtual void Visit(BoundUnaryEx x, ConditionBranch branch)
        {
            base.VisitUnaryExpression(x);
        }

        public sealed override TResult VisitInstanceOf(BoundInstanceOfEx x)
        {
            Visit(x, ConditionBranch.Default);

            return default;
        }

        protected virtual void Visit(BoundInstanceOfEx x, ConditionBranch branch)
        {
            base.VisitInstanceOf(x);
        }

        public sealed override TResult VisitIsSet(BoundIsSetEx x)
        {
            Visit(x, ConditionBranch.Default);

            return default;
        }

        protected virtual void Visit(BoundIsSetEx x, ConditionBranch branch)
        {
            base.VisitIsSet(x);
        }

        #endregion

        #region Specific

        /// <summary>
        /// Handles use of variable as foreach iterator value.
        /// </summary>
        /// <param name="varuse"></param>
        /// <returns>Derivate type of iterated values.</returns>
        protected virtual TypeRefMask HandleTraversableUse(BoundExpression/*!*/varuse)
        {
            return TypeRefMask.AnyType;
        }

        #endregion

        #region GraphVisitor Members

        public override TResult VisitCFG(ControlFlowGraph x)
        {
            Contract.ThrowIfNull(x);
            Debug.Assert(IsStateInitialized(GetState(x.Start)), "Start block has to have an initial state set.");

            EnqueueBlock(x.Start);

            return default;
        }

        protected override TResult AcceptEdge(BoundBlock fromBlock, Edge edge)
        {
            if ((_flags & AnalysisFlags.IsCanceled) == 0)
            {
                return base.AcceptEdge(fromBlock, edge);
            }
            else
            {
                return default;
            }
        }

        public override TResult VisitCFGBlock(BoundBlock x)
        {
            VisitCFGBlockInit(x);
            DefaultVisitBlock(x);   // modifies State, traverses to the edge

            return default;
        }

        public override TResult VisitCFGCaseBlock(CaseBlock x)
        {
            VisitCFGBlockInit(x);
            if (!x.CaseValue.IsOnlyBoundElement) { VisitCFGBlock(x.CaseValue.PreBoundBlockFirst); }
            if (!x.CaseValue.IsEmpty) { Accept(x.CaseValue.BoundElement); }
            DefaultVisitBlock(x);

            return default;
        }

        public override TResult VisitCFGCatchBlock(CatchBlock x)
        {
            VisitCFGBlockInit(x);

            Accept(x.TypeRef);
            Accept(x.Variable);

            //
            DefaultVisitBlock(x);

            return default;
        }

        public override TResult VisitCFGSimpleEdge(SimpleEdge x)
        {
            TraverseToBlock(x, State, x.NextBlock);

            return default;
        }

        public override TResult VisitCFGConditionalEdge(ConditionalEdge x)
        {
            // build state for TrueBlock and FalseBlock properly, take minimal evaluation into account
            var state = State;

            // true branch
            State = CloneState(state);
            VisitCondition(x.Condition, ConditionBranch.ToTrue);
            TraverseToBlock(x, State, x.TrueTarget);

            // false branch
            State = CloneState(state);
            VisitCondition(x.Condition, ConditionBranch.ToFalse);
            TraverseToBlock(x, State, x.FalseTarget);

            return default;
        }

        public override TResult VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x)
        {
            Accept(x.Enumeree);
            VisitCFGSimpleEdge(x);

            return default;
        }

        public override TResult VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            var state = State;
            // get type information from Enumeree to determine types value variable
            var elementType = HandleTraversableUse(x.EnumereeEdge.Enumeree);
            if (elementType.IsVoid) elementType = TypeRefMask.AnyType;

            // Body branch
            State = CloneState(state);
            // set key variable and value variable at current state

            var valueVar = x.ValueVariable;
            var islistunpacking = valueVar is BoundListEx;

            // analyse Value
            Visit(valueVar, valueVar.Access.WithWrite(valueVar.Access.IsWriteRef ? elementType.WithRefFlag : elementType));

            // analyse Key
            var keyVar = x.KeyVariable;
            if (keyVar != null)
            {
                Visit(keyVar, keyVar.Access.WithWrite(TypeRefMask.AnyType));
            }

            TraverseToBlock(x, State, x.BodyBlock);

            // End branch
            State = CloneState(state);
            TraverseToBlock(x, State, x.NextBlock);

            return default;
        }

        public override TResult VisitCFGSwitchEdge(SwitchEdge x)
        {
            Accept(x.SwitchValue);

            var state = State;

            foreach (var c in x.CaseBlocks)
            {
                if (!c.CaseValue.IsOnlyBoundElement)
                {
                    TraverseToBlock(x, state, c.CaseValue.PreBoundBlockFirst);
                }

                //
                TraverseToBlock(x, state, c);
            }

            return default;
        }

        public override TResult VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            var state = State;

            // TODO: any expression inside try{} block can traverse to catch{} or finally{}.

            //
            TraverseToBlock(x, state, x.BodyBlock);

            //
            SetStateUnknown(ref state);  // TODO: traverse from all states in try{} instead of setting variables unknown here

            foreach (var c in x.CatchBlocks)
            {
                TraverseToBlock(x, state, c);
            }

            if (x.FinallyBlock != null)
            {
                TraverseToBlock(x, state, x.FinallyBlock);
            }

            return default;
        }

        #endregion
    }
}
