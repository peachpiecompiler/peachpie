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
    public partial class AnalysisWalker<T> : GraphWalker<T>
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
        /// Creates an instance of <see cref="AnalysisWalker{T}"/> that can analyse a block.
        /// </summary>
        /// <param name="worklist">The worklist to be used to enqueue next blocks.</param>
        internal AnalysisWalker(Worklist<BoundBlock> worklist)
        {
            Debug.Assert(worklist != null);
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
        /// <param name="edgeLabel">Label identifying incoming edge.</param>
        /// <param name="state">Locals state in which we are entering the target.</param>
        /// <param name="target">Target block.</param>
        /// <remarks>Only for traversing into blocks within the same routine (same type context).</remarks>
        private void TraverseToBlock(object edgeLabel, FlowState/*!*/state, BoundBlock/*!*/target)
        {
            Contract.ThrowIfNull(state);    // state should be already set by previous block

            var targetState = target.FlowState;
            if (targetState != null)
            {
                Debug.Assert(targetState.FlowContext == state.FlowContext);

                // block was visited already,
                // merge and check whether state changed
                state = state.Merge(targetState);   // merge states into new one

                if (state.Equals(targetState) && !target.ForceRepeatedAnalysis)
                {
                    // state converged, we don't have to analyse the target block again
                    // unless it is specially needed (e.g. ExitBlock)
                    return;
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

        /// <summary>
        /// Gets value indicating the given type represents a double and nothing else.
        /// </summary>
        protected bool IsDoubleOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeCtx.IsDouble(tmask);
        }

        /// <summary>
        /// Gets value indicating the given type represents a double and nothing else.
        /// </summary>
        protected bool IsDoubleOnly(BoundExpression x) => IsDoubleOnly(x.TypeRefMask);

        /// <summary>
        /// Gets value indicating the given type represents a long and nothing else.
        /// </summary>
        protected bool IsLongOnly(TypeRefMask tmask)
        {
            return tmask.IsSingleType && this.TypeCtx.IsLong(tmask);
        }

        /// <summary>
        /// Gets value indicating the given type represents a long and nothing else.
        /// </summary>
        protected bool IsLongOnly(BoundExpression x) => IsLongOnly(x.TypeRefMask);

        /// <summary>
        /// Gets value indicating the given type is long or double or both but nothing else.
        /// </summary>
        /// <param name="tmask"></param>
        /// <returns></returns>
        protected bool IsNumberOnly(TypeRefMask tmask)
        {
            if (TypeCtx.IsLong(tmask) || TypeCtx.IsDouble(tmask))
            {
                if (tmask.IsSingleType)
                {
                    return true;
                }

                return !tmask.IsAnyType && TypeCtx.GetTypes(tmask).All(TypeHelpers.IsNumber);
            }

            return false;
        }

        /// <summary>
        /// Gets value indicating the given type represents only class types.
        /// </summary>
        protected bool IsClassOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && TypeCtx.GetTypes(tmask).All(x => x.IsObject);
        }

        /// <summary>
        /// Gets value indicating the given type represents only array types.
        /// </summary>
        protected bool IsArrayOnly(TypeRefMask tmask)
        {
            return !tmask.IsVoid && !tmask.IsAnyType && TypeCtx.GetTypes(tmask).All(x => x.IsArray);
        }

        /// <summary>
        /// Gets value indicating the given type is long or double or both but nothing else.
        /// </summary>
        protected bool IsNumberOnly(BoundExpression x) => IsNumberOnly(x.TypeRefMask);

        #endregion

        #region Short-Circuit Evaluation

        /// <summary>
        /// Visits condition used to branch execution to true or false branch.
        /// </summary>
        /// <returns>Value indicating whether branch was used.</returns>
        /// <remarks>
        /// Because of minimal evaluation there is different FlowState for true and false branches,
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

        public sealed override T VisitBinaryExpression(BoundBinaryEx x)
        {
            Visit(x, ConditionBranch.Default);

            return default;
        }

        protected virtual void Visit(BoundBinaryEx x, ConditionBranch branch)
        {
            base.VisitBinaryExpression(x);
        }

        public sealed override T VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            VisitGlobalFunctionCall(x, ConditionBranch.Default);

            return default;
        }

        public virtual void VisitGlobalFunctionCall(BoundGlobalFunctionCall x, ConditionBranch branch)
        {
            base.VisitGlobalFunctionCall(x);
        }

        public sealed override T VisitUnaryExpression(BoundUnaryEx x)
        {
            Visit(x, ConditionBranch.Default);

            return default;
        }

        protected virtual void Visit(BoundUnaryEx x, ConditionBranch branch)
        {
            base.VisitUnaryExpression(x);
        }

        public sealed override T VisitInstanceOf(BoundInstanceOfEx x)
        {
            Visit(x, ConditionBranch.Default);

            return default;
        }

        protected virtual void Visit(BoundInstanceOfEx x, ConditionBranch branch)
        {
            base.VisitInstanceOf(x);
        }

        public sealed override T VisitIsSet(BoundIsSetEx x)
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

        public override T VisitCFG(ControlFlowGraph x)
        {
            Contract.ThrowIfNull(x);
            Debug.Assert(x.Start.FlowState != null, "Start block has to have an initial state set.");

            _worklist.Enqueue(x.Start);

            return default;
        }

        public override T VisitCFGBlock(BoundBlock x)
        {
            VisitCFGBlockInit(x);
            DefaultVisitBlock(x);   // modifies _state, traverses to the edge

            return default;
        }

        public override T VisitCFGExitBlock(ExitBlock x)
        {
            VisitCFGBlock(x);

            // TODO: EdgeToCallers:
            PingSubscribers(x);

            return default;
        }

        protected void PingSubscribers(ExitBlock exit)
        {
            if (exit != null)
            {
                bool wasNotAnalysed = false;
                if (_state.Routine?.IsReturnAnalysed == false)
                {
                    wasNotAnalysed = true;
                    _state.Routine.IsReturnAnalysed = true;
                }

                // Ping the subscribers either if the return type has changed or
                // it is the first time the analysis reached the routine exit
                var rtype = _state.GetReturnType();
                if (rtype != exit._lastReturnTypeMask || wasNotAnalysed)
                {
                    exit._lastReturnTypeMask = rtype;
                    var subscribers = exit.Subscribers;
                    if (subscribers.Count != 0)
                    {
                        lock (subscribers)
                        {
                            foreach (var subscriber in subscribers)
                            {
                                _worklist.PingReturnUpdate(exit, subscriber);
                            }
                        }
                    }
                }
            }
        }

        public override T VisitCFGCaseBlock(CaseBlock x)
        {
            VisitCFGBlockInit(x);
            if (!x.CaseValue.IsOnlyBoundElement) { VisitCFGBlock(x.CaseValue.PreBoundBlockFirst); }
            if (!x.CaseValue.IsEmpty) { Accept(x.CaseValue.BoundElement); }
            DefaultVisitBlock(x);

            return default;
        }

        public override T VisitCFGCatchBlock(CatchBlock x)
        {
            VisitCFGBlockInit(x);

            // add catch control variable to the state
            x.TypeRef.Accept(this);
            x.Variable.Access = BoundAccess.Write.WithWrite(x.TypeRef.GetTypeRefMask(TypeCtx));
            State.SetLocalType(State.GetLocalHandle(x.Variable.Name.NameValue), x.Variable.Access.WriteMask);

            Accept(x.Variable);

            //
            x.Variable.ResultType = (Symbols.TypeSymbol)x.TypeRef.Type;

            //
            DefaultVisitBlock(x);

            return default;
        }

        public override T VisitCFGSimpleEdge(SimpleEdge x)
        {
            TraverseToBlock(x, _state, x.NextBlock);

            return default;
        }

        public override T VisitCFGConditionalEdge(ConditionalEdge x)
        {
            // build state for TrueBlock and FalseBlock properly, take minimal evaluation into account
            var state = _state;

            // true branch
            _state = state.Clone();
            VisitCondition(x.Condition, ConditionBranch.ToTrue);
            TraverseToBlock(x, _state, x.TrueTarget);

            // false branch
            _state = state.Clone();
            VisitCondition(x.Condition, ConditionBranch.ToFalse);
            TraverseToBlock(x, _state, x.FalseTarget);

            return default;
        }

        public override T VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x)
        {
            Accept(x.Enumeree);
            VisitCFGSimpleEdge(x);

            return default;
        }

        public override T VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            var state = _state;
            // get type information from Enumeree to determine types value variable
            var elementType = HandleTraversableUse(x.EnumereeEdge.Enumeree);
            if (elementType.IsVoid) elementType = TypeRefMask.AnyType;

            // Body branch
            _state = state.Clone();
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

            TraverseToBlock(x, _state, x.BodyBlock);

            // End branch
            _state = state.Clone();
            TraverseToBlock(x, _state, x.NextBlock);

            return default;
        }

        public override T VisitCFGSwitchEdge(SwitchEdge x)
        {
            Accept(x.SwitchValue);

            var state = _state;

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

        public override T VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            var state = _state;

            // TODO: any expression inside try{} block can traverse to catch{} or finally{}.

            //
            TraverseToBlock(x, state, x.BodyBlock);

            //
            state.SetAllUnknown(true);  // TODO: traverse from all states in try{} instead of setting variables unknown here

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
