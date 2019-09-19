using System;
using System.Collections.Generic;
using System.Text;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    partial class TransformationRewriter
    {
        /// <summary>
        /// A general structure to record bit information about variables in the code.
        /// </summary>
        private struct VariableMask
        {
            private ulong _mask;

            public void Set(VariableHandle handle)
            {
                var varindex = handle.Slot;
                if (varindex >= 0 && varindex < FlowContext.BitsCount)
                {
                    _mask |= 1ul << varindex;
                }
            }

            public void SetAll()
            {
                _mask = ~0ul;
            }

            public bool Get(VariableHandle handle)
            {
                var varindex = handle.Slot;
                if (varindex >= 0 && varindex < FlowContext.BitsCount)
                {
                    return (_mask & (1ul << varindex)) != 0;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Used to gather and store additional flow-insensitive information about variables.
        /// </summary>
        private struct VariableInfos
        {
            /// <summary>
            /// Records variables whose value might change during the method execution.
            /// </summary>
            public VariableMask MightChange;
        }

        /// <summary>
        /// Gathers additional flow-insensitive information about variables.
        /// </summary>
        private class VariableInfoWalker : GraphExplorer<VoidStruct>
        {
            private VariableInfos _infos;
            private readonly FlowContext _flowContext;

            private VariableInfoWalker(FlowContext flowContext) : base()
            {
                _flowContext = flowContext;
            }

            /// <summary>
            /// Gathers information about the variables in the routine.
            /// </summary>
            public static VariableInfos Analyze(SourceRoutineSymbol routine)
            {
                var cfg = routine.ControlFlowGraph;
                var walker = new VariableInfoWalker(cfg.FlowContext);
                walker.VisitCFG(cfg);

                return walker._infos;
            }

            public override VoidStruct VisitVariableRef(BoundVariableRef x)
            {
                if (x.Access.MightChange)
                {
                    if (!x.Name.IsDirect)
                    {
                        // In the worst case, any variable can be targeted
                        _infos.MightChange.SetAll();
                    }
                    else
                    {
                        var varindex = _flowContext.GetVarIndex(x.Name.NameValue);
                        if (!_flowContext.IsReference(varindex))
                        {
                            // Mark only the specific variable as possibly being changed
                            _infos.MightChange.Set(varindex);
                        }
                        else
                        {
                            // TODO: Mark only those that can be referenced
                            _infos.MightChange.SetAll();
                        }
                    }
                }

                return base.VisitVariableRef(x);
            }
        }
    }
}
