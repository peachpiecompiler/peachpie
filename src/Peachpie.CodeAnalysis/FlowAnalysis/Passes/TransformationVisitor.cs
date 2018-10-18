using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    internal class TransformationVisitor : GraphWalker
    {
        private readonly SourceRoutineSymbol _routine;
        private int _visitedColor;
        private TransformationRewriter _rewriter;
        private int _cfgTransformationCount;

        public static bool TryTransform(SourceRoutineSymbol routine)
        {
            if (routine.ControlFlowGraph != null)   // non-abstract method
            {
                var visitor = new TransformationVisitor(routine);
                visitor.VisitCFG(routine.ControlFlowGraph);

                return visitor._cfgTransformationCount + visitor._rewriter.TransformationCount > 0;
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

        public override EmptyStruct VisitCFG(ControlFlowGraph x)
        {
            Debug.Assert(x == _routine.ControlFlowGraph);

            _visitedColor = x.NewColor();
            base.VisitCFG(x);

            return default;
        }

        protected override EmptyStruct DefaultVisitBlock(BoundBlock x)
        {
            if (x.Tag != _visitedColor)
            {
                x.Tag = _visitedColor;

                // TODO: Transform also conditions in edges etc.
                _rewriter.VisitBlockStatement(x);

                if (x.NextEdge != null && !IsEdgeVisitingStopped)
                    x.NextEdge.Accept(this);
            }

            return default;
        }

        public override EmptyStruct VisitCFGConditionalEdge(ConditionalEdge x)
        {
            //_rewriter.VisitAndUpdate(x.Condition, x.SetCondition);

            if (x.Condition.ConstantValue.TryConvertToBool(out bool condValue))
            {
                var target = condValue ? x.TrueTarget : x.FalseTarget;
                x.Source.NextEdge = new SimpleEdge(x.Source, target);

                _cfgTransformationCount++;
                _routine.ControlFlowGraph.UnreachableBlocks.Add(condValue ? x.FalseTarget : x.TrueTarget);
                Accept(target);
            }
            else
            {
                x.TrueTarget.Accept(this);
                x.FalseTarget.Accept(this);
            }

            return default;
        }
    }
}
