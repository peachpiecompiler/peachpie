using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    internal class TransformationRewriter : GraphRewriter
    {
        private readonly SourceRoutineSymbol _routine;

        public int TransformationCount { get; private set; }

        public static bool TryTransform(SourceRoutineSymbol routine)
        {
            if (routine.ControlFlowGraph != null)   // non-abstract method
            {
                var rewriter = new TransformationRewriter(routine);
                var updatedCFG = (ControlFlowGraph)rewriter.VisitCFG(routine.ControlFlowGraph);

                if (updatedCFG != routine.ControlFlowGraph)
                {
                    Debug.Assert(rewriter.TransformationCount > 0);
                    routine.UpdateControlFlowGraph(updatedCFG);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private TransformationRewriter(SourceRoutineSymbol routine)
        {
            _routine = routine;
        }

        protected override void OnVisitCFG(ControlFlowGraph x)
        {
            Debug.Assert(_routine.ControlFlowGraph == x);
        }

        public override object VisitConditional(BoundConditionalEx x)
        {
            x = (BoundConditionalEx)base.VisitConditional(x);

            if (x.IfTrue != null
                && x.IfTrue.ConstantValue.IsBool(out bool trueVal)
                && x.IfFalse.ConstantValue.IsBool(out bool falseVal))
            {
                if (trueVal && !falseVal)
                {
                    // A ? true : false => (bool)A
                    TransformationCount++;
                    return
                        new BoundUnaryEx(x.Condition, Devsense.PHP.Syntax.Ast.Operations.BoolCast)
                        .WithContext(x);
                }

                // TODO: Other possibilities
            }

            return x;
        }

        public override object VisitCFGConditionalEdge(ConditionalEdge x)
        {
            if (x.Condition.ConstantValue.TryConvertToBool(out bool condValue))
            {
                TransformationCount++;
                NotePossiblyUnreachable(condValue ? x.FalseTarget : x.TrueTarget);
                var target = condValue ? x.TrueTarget : x.FalseTarget;
                return new SimpleEdge((BoundBlock)Accept(target));
            }

            return base.VisitCFGConditionalEdge(x);
        }
    }
}
