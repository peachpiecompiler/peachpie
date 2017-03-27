using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Semantics;
using System.Diagnostics;
using Pchp.CodeAnalysis.Semantics;
using Devsense.PHP.Text;
using Devsense.PHP.Syntax;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    public partial class AnalysisVisitor : GraphVisitor
    {
        #region Fields

        /// <summary>
        /// The worklist to be used to enqueue next blocks.
        /// </summary>
        internal Worklist<BoundBlock> Worklist => _worklist;
        readonly Worklist<BoundBlock> _worklist;

        /// <summary>
        /// Gets current type context for type masks resolving.
        /// </summary>
        internal TypeRefContext TypeCtx => _state.TypeRefContext;

        /// <summary>
        /// Current naming context. Can be a <c>null</c> reference.
        /// </summary>
        public NamingContext Naming => CurrentBlock?.Naming;

        /// <summary>
        /// Current flow state.
        /// </summary>
        internal FlowState State
        {
            get { return _state; }
            set { _state = value; }
        }
        FlowState _state;

        /// <summary>
        /// Gets reference to current block.
        /// </summary>
        internal BoundBlock CurrentBlock { get; private set; }

        #endregion

        #region Construction

        /// <summary>
        /// Creates an instance of <see cref="AnalysisVisitor"/> that can analyse a block.
        /// </summary>
        /// <param name="worklist">The worklist to be used to enqueue next blocks.</param>
        internal AnalysisVisitor(Worklist<BoundBlock> worklist)
        {
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
                Debug.Assert(targetState.FlowContext == state.FlowContext);

                // block was visited already,
                // merge and check whether state changed
                state = state.Merge(targetState);   // merge states into new one
                if (state.Equals(targetState))
                {
                    return; // state convergated, we don't have to analyse target block again
                }
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

            this.CurrentBlock = x;
        }

        #endregion

        #region Short-Circuit Evaluation

        /// <summary>
        /// Visits condition used to branch execution to true or false branch.
        /// </summary>
        /// <remarks>
        /// Because of minimal evaluation there is different FlowState for true and false branches,
        /// AND and OR operators have to take this into account.
        /// 
        /// Also some other constructs may have side-effect for known branch,
        /// eg. <c>($x instanceof X)</c> implies ($x is X) in True branch.
        /// </remarks>
        internal void VisitCondition(BoundExpression condition, ConditionBranch branch)
        {
            Contract.ThrowIfNull(condition);

            if (branch != ConditionBranch.AnyResult)
            {
                if (condition is BoundBinaryEx)
                {
                    Visit((BoundBinaryEx)condition, branch);
                    return;
                }
                if (condition is BoundUnaryEx)
                {
                    Visit((BoundUnaryEx)condition, branch);
                    return;
                }
                //if (condition is DirectFcnCall)
                //{
                //    VisitDirectFcnCall((DirectFcnCall)condition, branch);
                //    return;
                //}
                if (condition is BoundInstanceOfEx)
                {
                    Visit((BoundInstanceOfEx)condition, branch);
                    return;
                }
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

        public sealed override void VisitBinaryExpression(BoundBinaryEx x) => Visit(x, ConditionBranch.Default);

        protected virtual void Visit(BoundBinaryEx x, ConditionBranch branch)
        {
            base.VisitBinaryExpression(x);
        }

        public sealed override void VisitUnaryExpression(BoundUnaryEx x) => Visit(x, ConditionBranch.Default);

        protected virtual void Visit(BoundUnaryEx x, ConditionBranch branch)
        {
            base.VisitUnaryExpression(x);
        }

        public sealed override void VisitInstanceOf(BoundInstanceOfEx x) => Visit(x, ConditionBranch.Default);

        protected virtual void Visit(BoundInstanceOfEx x, ConditionBranch branch)
        {
            base.VisitInstanceOf(x);
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

        public override void VisitCFGExitBlock(ExitBlock x)
        {
            VisitCFGBlock(x);

            // TODO: EdgeToCallers:
            EnqueueSubscribers(x);
        }

        protected void EnqueueSubscribers(ExitBlock exit)
        {
            if (exit != null)
            {
                var rtype = _state.GetReturnType();
                if (rtype != exit._lastReturnTypeMask)
                {
                    exit._lastReturnTypeMask = rtype;
                    exit.Subscribers.ForEach(_worklist.Enqueue);
                }
            }
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
            Accept(x.Variable);
            VisitTypeRef(x.TypeRef);
            State.SetLocalType(State.GetLocalHandle(x.Variable.Name.NameValue.Value), TypeCtx.GetTypeMask(x.TypeRef.TypeRef));
            
            //
            x.Variable.ResultType = x.TypeRef.ResolvedType;

            //
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
            if (valueVar is BoundListEx)
            {
                throw new NotImplementedException();
                //VisitListEx(valueVar.List, elementType);
            }
            else
            {
                valueVar.Access = valueVar.Access.WithWrite(valueVar.Access.IsWriteRef ? elementType.WithRefFlag : elementType);
                Accept(valueVar);

                //
                var keyVar = x.KeyVariable;
                if (keyVar != null)
                {
                    keyVar.Access = keyVar.Access.WithWrite(TypeRefMask.AnyType);
                    Accept(keyVar);
                }
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

            //
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
