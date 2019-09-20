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
            /// Records parameters which need to be deep copied and dealiased upon routine start.
            /// </summary>
            public VariableMask NeedPassValueParams;
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

            public override VoidStruct VisitArgument(BoundArgument x)
            {
                VariableHandle varindex;

                if (x.Value is BoundVariableRef varRef
                    && varRef.Variable is ParameterReference
                    && varRef.Name.IsDirect
                    && !_flowContext.IsReference(varindex = _flowContext.GetVarIndex(varRef.Name.NameValue))
                    && !varRef.Access.MightChange)
                {
                    // Passing a parameter as an argument by value to another function is a safe use which does not
                    // require it to be deeply copied (the called function will do it on its own if necessary)
                    return default;
                }
                else
                {
                    return base.VisitArgument(x);
                }
            }

            public override VoidStruct VisitVariableRef(BoundVariableRef x)
            {
                // Other usage than being passed as an argument to another function requires a parameter to be deeply copied
                if (!x.Name.IsDirect)
                {
                    // In the worst case, any variable can be targeted
                    _infos.NeedPassValueParams.SetAll();
                }
                else
                {
                    var varindex = _flowContext.GetVarIndex(x.Name.NameValue);
                    if (!_flowContext.IsReference(varindex))
                    {
                        // Mark only the specific variable as possibly being changed
                        _infos.NeedPassValueParams.Set(varindex);
                    }
                    else
                    {
                        // TODO: Mark only those that can be referenced
                        _infos.NeedPassValueParams.SetAll();
                    }
                }

                return base.VisitVariableRef(x);
            }
        }
    }
}
