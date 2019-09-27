using System;
using System.Collections.Generic;
using System.Text;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;

namespace Peachpie.DiagnosticTests
{
    /// <summary>
    /// Helper class to gather all the operations of interesting types.
    /// </summary>
    class OperationSelector : GraphExplorer<VoidStruct>
    {
        /// <summary>
        /// Gets the resulting list.
        /// </summary>
        readonly List<IPhpOperation> _result = new List<IPhpOperation>(16);

        private OperationSelector() { }

        public static List<IPhpOperation> Select(SourceRoutineSymbol routine)
        {
            var visitor = new OperationSelector();
            visitor.VisitCFG(routine.ControlFlowGraph);
            return visitor._result;
        }

        public override VoidStruct VisitCopyValue(BoundCopyValue x)
        {
            _result.Add(x);
            return base.VisitCopyValue(x);
        }
    }
}
