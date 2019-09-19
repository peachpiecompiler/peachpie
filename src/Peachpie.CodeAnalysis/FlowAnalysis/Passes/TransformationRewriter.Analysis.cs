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
        /// Used to gather and store additional flow-insensitive information about variables.
        /// </summary>
        private struct VariableInfos
        {
            private ulong _mightChangeMask;

            /// <summary>
            /// Marks that the value stored in the variable might change.
            /// </summary>
            public void SetMightChange(VariableHandle handle)
            {
                var varindex = handle.Slot;
                if (varindex >= 0 && varindex < FlowContext.BitsCount)
                {
                    _mightChangeMask |= 1ul << varindex;
                }
            }

            /// <summary>
            /// Marks that the values stored in all the variables might change.
            /// </summary>
            public void SetAllMightChange()
            {
                _mightChangeMask = ~0ul;
            }

            /// <summary>
            /// Retrieves whether the value stored in the variable might change.
            /// </summary>
            public bool GetMightChange(VariableHandle handle)
            {
                var varindex = handle.Slot;
                if (varindex >= 0 && varindex < FlowContext.BitsCount)
                {
                    return (_mightChangeMask & (1ul << varindex)) != 0;
                }
                else
                {
                    return true;
                }
            }
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
                        _infos.SetAllMightChange();
                    }
                    else
                    {
                        var varindex = _flowContext.GetVarIndex(x.Name.NameValue);
                        if (!_flowContext.IsReference(varindex))
                        {
                            // Mark only the specific variable as possibly being changed
                            _infos.SetMightChange(varindex);
                        }
                        else
                        {
                            // TODO: Mark only those that can be referenced
                            _infos.SetAllMightChange();
                        }
                    }
                }

                return base.VisitVariableRef(x);
            }
        }
    }
}
