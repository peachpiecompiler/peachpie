using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Syntax.Ast;
using Pchp.CodeAnalysis.Errors;
using Pchp.CodeAnalysis.Semantics.Graph;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    internal partial class DiagnosingVisitor
    {
        private int _visitedColor;

        Queue<BoundBlock> _unreachableQueue = new Queue<BoundBlock>();

        private void InitializeReachabilityInfo(ControlFlowGraph x)
        {
            _visitedColor = x.NewColor();
        }

        protected override void VisitCFGBlockInternal(BoundBlock x)
        {
            if (x.Tag != _visitedColor)
            {
                x.Tag = _visitedColor;
                base.VisitCFGBlockInternal(x);
            }
        }

        public override void VisitCFGConditionalEdge(ConditionalEdge x)
        {
            Accept(x.Condition);

            var constantValue = x.Condition.ConstantValue.ToConstantValueOrNull();
            if (constantValue != null && constantValue.TryConvertToBool(out bool value))
            {
                // Process only the reachable branch, let the reachability of the other be checked later
                if (value)
                {
                    _unreachableQueue.Enqueue(x.FalseTarget);
                    x.TrueTarget.Accept(this);
                }
                else
                {
                    _unreachableQueue.Enqueue(x.TrueTarget);
                    x.FalseTarget.Accept(this);
                }
            }
            else
	        {
                x.TrueTarget.Accept(this);
                x.FalseTarget.Accept(this); 
            }
        }

        private void CheckUnreachableCode(ControlFlowGraph graph)
        {
            graph.UnreachableBlocks.ForEach(_unreachableQueue.Enqueue);

            while (_unreachableQueue.Count > 0)
            {
                var block = _unreachableQueue.Dequeue();

                // Skip the block if it was either proven reachable before or if it was already processed
                if (block.Tag == _visitedColor)
                {
                    continue;
                }

                block.Tag = _visitedColor;

                var syntax = PickFirstSyntaxNode(block);
                if (syntax != null)
                {
                    // Report the diagnostic for the first unreachable statement
                    _diagnostics.Add(this._routine, syntax, ErrorCode.WRN_UnreachableCode);
                }
                else
                {
                    // If there is no statement to report the diagnostic for, search further
                    // - needed for while, do while and scenarios such as if (...) { return; } else { return; } ...
                    block.NextEdge?.Targets.ForEach(_unreachableQueue.Enqueue);
                }

            }
        }

        private static LangElement PickFirstSyntaxNode(BoundBlock block)
        {
            var syntax = block.Statements.FirstOrDefault(st => st.PhpSyntax != null)?.PhpSyntax;
            if (syntax != null)
            {
                return syntax;
            }

            // TODO: Mark the first keyword (if, switch, foreach,...) instead
            switch (block.NextEdge)
            {
                case ForeachEnumereeEdge edge:
                    return edge.Enumeree.PhpSyntax;

                case SimpleEdge edge:
                    return edge.PhpSyntax;

                case ConditionalEdge edge:
                    return edge.Condition.PhpSyntax;

                case TryCatchEdge edge:
                    return PickFirstSyntaxNode(edge.BodyBlock);

                case SwitchEdge edge:
                    return edge.SwitchValue.PhpSyntax;

                default:
                    return null;
            }
        }
    }
}
