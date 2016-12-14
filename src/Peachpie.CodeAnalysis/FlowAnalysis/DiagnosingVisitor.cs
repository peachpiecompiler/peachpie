using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Errors;
using Microsoft.CodeAnalysis.Text;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    internal class DiagnosingVisitor : GraphVisitor
    {
        private readonly ISemanticModel _semanticModel;
        private readonly DiagnosticBag _diagnostics;

        private int _visitedColor = int.MinValue;

        public DiagnosingVisitor(ISemanticModel semanticModel, DiagnosticBag diagnostics)
        {
            _semanticModel = semanticModel;
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
            if (x.Tag < _visitedColor)
            {
                x.Tag = _visitedColor;
                base.VisitCFGBlockInternal(x); 
            }
        }
    }
}
