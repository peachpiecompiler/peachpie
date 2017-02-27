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
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    internal class DiagnosingVisitor : GraphVisitor
    {
        private readonly DiagnosticBag _diagnostics;
        private SourceRoutineSymbol _routine;

        private int _visitedColor;

        public DiagnosingVisitor(DiagnosticBag diagnostics, SourceRoutineSymbol routine)
        {
            _diagnostics = diagnostics;
            _routine = routine;
        }

        public override void VisitCFG(ControlFlowGraph x)
        {
            Debug.Assert(x == _routine.ControlFlowGraph);

            _visitedColor = x.NewColor();
            base.VisitCFG(x);
        }

        public override void VisitGlobalFunctionCall(BoundGlobalFunctionCall x)
        {
            CheckUndefinedFunctionCall(x);
            base.VisitGlobalFunctionCall(x);
        }

        public override void VisitVariableRef(BoundVariableRef x)
        {
            CheckUninitializedVariableUse(x);
            base.VisitVariableRef(x);
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
            if (x.Name.IsDirect && x.TargetMethod.IsErrorMethod())
            {
                var errmethod = (ErrorMethodSymbol)x.TargetMethod;
                if (errmethod != null && errmethod.ErrorKind == ErrorMethodSymbol.ErrorMethodKind.Missing)
                {
                    _diagnostics.Add(_routine, x, ErrorCode.WRN_UndefinedFunctionCall, x.Name.NameValue.ToString());
                }
            }
        }

        private void CheckUninitializedVariableUse(BoundVariableRef x)
        {
            if (x.MaybeUninitialized)
            {
                _diagnostics.Add(_routine, x, ErrorCode.WRN_UninitializedVariableUse, x.Name.NameValue.ToString());
            }
        }
    }
}
