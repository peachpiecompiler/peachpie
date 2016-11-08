using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Control flow graph visitor.
    /// </summary>
    /// <remarks>Visitor does not implement infinite recursion prevention.</remarks>
    public class GraphVisitor
    {
        #region Bound

        /// <summary>
        /// Visitor for bound operations.
        /// Cannot be <c>null</c>.
        /// </summary>
        public OperationVisitor Visitor
        {
            get
            {
                var visitor = _visitor;
                if (visitor == null)
                {
                    _visitor = visitor = _opVisitorFactory(this);
                    Contract.ThrowIfNull(visitor);

                    _opVisitorFactory = null;
                }

                return visitor;
            }
        }
        OperationVisitor _visitor;

        Func<GraphVisitor, OperationVisitor> _opVisitorFactory;

        /// <summary>
        /// Forwards the operation to the <see cref="OperationVisitor"/>.
        /// </summary>
        protected void Accept(IOperation op) => op?.Accept(Visitor);
        
        #endregion

        #region ControlFlowGraph

        public GraphVisitor(Func<GraphVisitor, OperationVisitor> opVisitorFactory)
        {
            Contract.ThrowIfNull(opVisitorFactory);
            _opVisitorFactory = opVisitorFactory;
        }

        public virtual void VisitCFG(ControlFlowGraph x) => x.Start.Accept(this);

        #endregion

        #region Graph.Block

        void VisitCFGBlockStatements(BoundBlock x)
        {
            for (int i = 0; i < x.Statements.Count; i++)
            {
                Accept(x.Statements[i]);
            }
        }

        /// <summary>
        /// Visits block statements and its edge to next block.
        /// </summary>
        protected virtual void VisitCFGBlockInternal(BoundBlock x)
        {
            VisitCFGBlockStatements(x);

            if (x.NextEdge != null)
                x.NextEdge.Visit(this);
        }

        public virtual void VisitCFGBlock(BoundBlock x)
        {
            VisitCFGBlockInternal(x);
        }

        public virtual void VisitCFGExitBlock(ExitBlock x)
        {
            VisitCFGBlock(x);
        }

        public virtual void VisitCFGCatchBlock(CatchBlock x)
        {
            Accept(x.Variable);
            VisitCFGBlockInternal(x);
        }

        public virtual void VisitCFGCaseBlock(CaseBlock x)
        {
            Accept(x.CaseValue);
            VisitCFGBlockInternal(x);
        }

        #endregion

        #region Graph.Edge

        public virtual void VisitCFGSimpleEdge(SimpleEdge x)
        {
            Debug.Assert(x.NextBlock != null);
            x.NextBlock.Accept(this);
        }

        public virtual void VisitCFGConditionalEdge(ConditionalEdge x)
        {
            Accept(x.Condition);

            x.TrueTarget.Accept(this);
            x.FalseTarget.Accept(this);
        }

        public virtual void VisitCFGTryCatchEdge(TryCatchEdge x)
        {
            x.BodyBlock.Accept(this);

            foreach (var c in x.CatchBlocks)
                c.Accept(this);

            if (x.FinallyBlock != null)
                x.FinallyBlock.Accept(this);
        }

        public virtual void VisitCFGForeachEnumereeEdge(ForeachEnumereeEdge x)
        {
            Accept(x.Enumeree);
            x.NextBlock.Accept(this);
        }

        public virtual void VisitCFGForeachMoveNextEdge(ForeachMoveNextEdge x)
        {
            Accept(x.ValueVariable);
            Accept(x.KeyVariable);

            x.BodyBlock.Accept(this);
            x.NextBlock.Accept(this);
        }

        public virtual void VisitCFGSwitchEdge(SwitchEdge x)
        {
            Accept(x.SwitchValue);

            //
            var arr = x.CaseBlocks;
            for (int i = 0; i < arr.Length; i++)
                arr[i].Accept(this);
        }

        #endregion
    }
}
