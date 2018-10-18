using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devsense.PHP.Syntax.Ast;
using Pchp.CodeAnalysis.Errors;
using Pchp.CodeAnalysis.Semantics.Graph;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    internal partial class DiagnosticWalker
    {
        int _visitedColor;
        BoundBlock _currentBlock;

        Queue<BoundBlock> _unreachables = new Queue<BoundBlock>();

        private void InitializeReachabilityInfo(ControlFlowGraph x)
        {
            _visitedColor = x.NewColor();
        }

        protected override EmptyStruct DefaultVisitBlock(BoundBlock x)
        {
            if (x.Tag != _visitedColor)
            {
                x.Tag = _visitedColor;
                _currentBlock = x;
                base.DefaultVisitBlock(x);
            }

            return default;
        }

        public override EmptyStruct VisitCFGConditionalEdge(ConditionalEdge x)
        {
            Accept(x.Condition);

            if (x.Condition.ConstantValue.TryConvertToBool(out bool value))
            {
                var reachable = value ? x.TrueTarget : x.FalseTarget;
                var unreachable = value ? x.FalseTarget : x.TrueTarget;

                reachable.Accept(this);  // Process only the reachable branch
                _unreachables.Enqueue(unreachable); // remember possible unreachable block
            }
            else
	        {
                x.TrueTarget.Accept(this);
                x.FalseTarget.Accept(this);
            }

            return default;
        }

        private void CheckUnreachableCode(ControlFlowGraph graph)
        {
            graph.UnreachableBlocks.ForEach(_unreachables.Enqueue);

            while (_unreachables.Count != 0)
            {
                var block = _unreachables.Dequeue();

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
                    _diagnostics.Add(_routine, syntax, ErrorCode.WRN_UnreachableCode);
                }
                else
                {
                    // If there is no statement to report the diagnostic for, search further
                    // - needed for while, do while and scenarios such as if (...) { return; } else { return; } ...
                    block.NextEdge?.Targets.ForEach(_unreachables.Enqueue);
                }
            }
        }

        private static LangElement PickFirstSyntaxNode(BoundBlock block)
        {
            var syntax = block.Statements.FirstOrDefault(st => st.PhpSyntax != null && !(st.PhpSyntax is PHPDocStmt))?.PhpSyntax;
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
