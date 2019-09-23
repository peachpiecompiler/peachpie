using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.FlowAnalysis.Passes
{
    partial class TransformationRewriter
    {
        private struct CopyAnalysisState
        {
            public BitMask[] Data;

            public CopyAnalysisState(int varCount)
            {
                Data = new BitMask[varCount];
            }

            public bool IsDefault => Data == null;

            public CopyAnalysisState Clone()
            {
                var clone = new CopyAnalysisState(this.Data.Length);
                this.Data.CopyTo(clone.Data, 0);

                return clone;
            }
        }

        private class CopyAnalysisContext : SingleBlockWalker<VoidStruct>, IFixPointAnalysisContext<CopyAnalysisState>
        {
            private readonly SourceRoutineSymbol _routine;

            private CopyAnalysisState _state;
            private Dictionary<BoundAssignEx, int> _assignmentIndices = new Dictionary<BoundAssignEx, int>();
            private BitMask _copyNeededAssignments;

            private int VariableCount => _routine.ControlFlowGraph.FlowContext.VarsType.Length;

            private FlowContext FlowContext => _routine.ControlFlowGraph.FlowContext;

            public CopyAnalysisContext(SourceRoutineSymbol routine)
            {
                _routine = routine;
            }

            public bool StatesEqual(CopyAnalysisState x, CopyAnalysisState y)
            {
                if (x.IsDefault != y.IsDefault)
                    return false;

                if (x.IsDefault && y.IsDefault || x.Data == y.Data)
                    return true;

                Debug.Assert(x.Data.Length == y.Data.Length);

                for (int i = 0; i < x.Data.Length; i++)
                {
                    if (x.Data[i] != y.Data[i])
                        return false;
                }

                return true;
            }

            public CopyAnalysisState GetInitialState()
            {
                return new CopyAnalysisState(VariableCount);
            }

            public CopyAnalysisState MergeStates(CopyAnalysisState x, CopyAnalysisState y)
            {
                //if (x.Data == y.Data)
                //    return x;

                var merged = new CopyAnalysisState(VariableCount);
                for (int i = 0; i < merged.Data.Length; i++)
                {
                    merged.Data[i] = x.Data[i] | y.Data[i];
                }

                return merged;
            }

            public CopyAnalysisState ProcessBlock(BoundBlock block, CopyAnalysisState state)
            {
                _state = state.Clone();
                block.Accept(this);
                return _state;
            }

            public override VoidStruct VisitAssign(BoundAssignEx assign)
            {
                // TODO: Fix to prevent aliasing from both sides (trg and src)
                // TODO: Assignment from a different source than another variable should just set the trg set to empty (not passing to VisitVariableRef)
                // TODO: Handle behaviour in case of aliases
                if (assign.Target is BoundVariableRef trgVarRef && trgVarRef.Name.IsDirect
                    && MatchExprSkipCopy(assign.Value, out BoundVariableRef srcVarRef, out bool isCopied))
                {
                    var trgHandle = FlowContext.GetVarIndex(trgVarRef.Name.NameValue);
                    if (isCopied)
                    {
                        int assignIndex = GetAssignmentIndex(assign);
                        _state.Data[trgHandle] = BitMask.FromSingleValue(assignIndex);
                    }
                    else
                    {
                        if (srcVarRef.Name.IsDirect)
                        {
                            var srcHandle = FlowContext.GetVarIndex(srcVarRef.Name.NameValue);
                            _state.Data[trgHandle] = _state.Data[srcHandle];
                        }
                    }
                }

                return base.VisitAssign(assign);
            }

            public override VoidStruct VisitVariableRef(BoundVariableRef x)
            {
                void MarkAllKnownAssignments()
                {
                    for (int i = 0; i < _state.Data.Length; i++)
                    {
                        _copyNeededAssignments |= _state.Data[i];
                    }
                }

                base.VisitVariableRef(x);

                if (x.Access.MightChange)
                {
                    if (!x.Name.IsDirect)
                    {
                        MarkAllKnownAssignments();
                    }
                    else
                    {
                        var varindex = FlowContext.GetVarIndex(x.Name.NameValue);
                        if (!FlowContext.IsReference(varindex))
                        {
                            _copyNeededAssignments |= _state.Data[varindex];
                        }
                        else
                        {
                            // TODO: Mark only those that can be referenced
                            MarkAllKnownAssignments();
                        }
                    } 
                }

                return default;
            }

            private int GetAssignmentIndex(BoundAssignEx assign)
            {
                int index;
                if (!_assignmentIndices.TryGetValue(assign, out index))
                {
                    index = _assignmentIndices.Count;
                    _assignmentIndices.Add(assign, index);
                }

                return index;
            }
        }
    }
}
