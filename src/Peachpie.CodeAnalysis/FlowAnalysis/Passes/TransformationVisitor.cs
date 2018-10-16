using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    internal class TransformationVisitor : GraphVisitor
    {
        private readonly SourceRoutineSymbol _routine;
        private int _visitedColor;
        private TransformationRewriter _rewriter;
        private bool _wasCfgTransformed;

        public static bool TryTransform(SourceRoutineSymbol routine)
        {
            if (routine.ControlFlowGraph != null)   // non-abstract method
            {
                var visitor = new TransformationVisitor(routine);
                visitor.VisitCFG(routine.ControlFlowGraph);

                return visitor._wasCfgTransformed || visitor._rewriter.WasTransformationPerformed;
            }
            else
            {
                return false;
            }
        }

        private TransformationVisitor(SourceRoutineSymbol routine)
        {
            _routine = routine;
            _rewriter = new TransformationRewriter();
        }

        public override void VisitCFG(ControlFlowGraph x)
        {
            Debug.Assert(x == _routine.ControlFlowGraph);

            _visitedColor = x.NewColor();
            base.VisitCFG(x);
        }

        protected override void VisitCFGBlockInternal(BoundBlock x)
        {
            if (x.Tag != _visitedColor)
            {
                x.Tag = _visitedColor;

                // TODO: Transform also conditions in edges etc.
                _rewriter.VisitBlockStatement(x);

                if (x.NextEdge != null && !IsEdgeVisitingStopped)
                    x.NextEdge.Visit(this);
            }
        }

        public override void VisitCFGConditionalEdge(ConditionalEdge x)
        {
            //_rewriter.VisitAndUpdate(x.Condition, x.SetCondition);

            if (x.Condition.ConstantValue.TryConvertToBool(out bool condValue))
            {
                var target = condValue ? x.TrueTarget : x.FalseTarget;
                x.Source.NextEdge = new SimpleEdge(x.Source, target);

                _wasCfgTransformed = true;
                _routine.ControlFlowGraph.UnreachableBlocks.Add(condValue ? x.FalseTarget : x.TrueTarget);
                Accept(target);
            }
            else
            {
                x.TrueTarget.Accept(this);
                x.FalseTarget.Accept(this);
            }
        }
    }
}
