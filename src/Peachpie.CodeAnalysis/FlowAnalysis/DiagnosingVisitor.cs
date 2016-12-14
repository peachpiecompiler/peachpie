using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Pchp.CodeAnalysis.Errors;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    internal class DiagnosingVisitor : GraphVisitor
    {
        private readonly DiagnosticBag _diagnostics;

        private int _visitedColor;

        public DiagnosingVisitor(DiagnosticBag diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public override void VisitCFG(ControlFlowGraph x)
        {
            _visitedColor = x.NewColor();
            base.VisitCFG(x);
        }

        public override void VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            CheckUndefinedFunctionCall(x);
            base.VisitGlobalFunctionCall(x);
        }

        protected override void VisitCFGBlockInternal(BoundBlock x)
        {
            if (x.Tag != _visitedColor)
            {
                x.Tag = _visitedColor;
                base.VisitCFGBlockInternal(x); 
            }
        }

        private void CheckUndefinedFunctionCall(BoundGlobalFunctionCall x)
        {
            if (x.TargetMethod == null)
            {
                var tree = new SyntaxTreeAdapter(x.PhpSyntax.ContainingSourceUnit);
                var span = x.PhpSyntax.Span;
                var location = new SourceLocation(tree, new TextSpan(span.Start, span.Length));
                var diag = MessageProvider.Instance.CreateDiagnostic(
                    ErrorCode.WRN_UndefinedFunctionCall,
                    location,
                    new object[] { x.Name.NameValue.ToString() });
                _diagnostics.Add(diag);
            }
        }
    }
}
