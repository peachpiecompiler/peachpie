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
                // Handle assignment to a variable
                VariableHandle trgHandle;
                if (assign.Target is BoundVariableRef trgVarRef && trgVarRef.Name.IsDirect
                    && !FlowContext.IsReference(trgHandle = FlowContext.GetVarIndex(trgVarRef.Name.NameValue)))
                {
                    VariableHandle srcHandle;
                    if (MatchExprSkipCopy(assign.Value, out BoundVariableRef srcVarRef, out bool isCopied)
                        && srcVarRef.Name.IsDirect
                        && !FlowContext.IsReference(srcHandle = FlowContext.GetVarIndex(srcVarRef.Name.NameValue)))
                    {
                        if (isCopied)
                        {
                            // Make the assignment a candidate for copy removal, possibly causing aliasing.
                            // It is removed if either trgVar or srcVar are modified later.
                            int assignIndex = GetAssignmentIndex(assign);
                            var assignMask = BitMask.FromSingleValue(assignIndex);
                            _state.Data[trgHandle] = assignMask;
                            _state.Data[srcHandle] |= assignMask;
                        }
                        else
                        {
                            // The copy was removed by a previous transformation, making them aliases sharing the assignments
                            _state.Data[trgHandle] = _state.Data[srcHandle];
                        }

                        // Visiting trgVar would destroy the effort (due to the assignment it's considered as MightChange),
                        // visiting srcVar is unnecessary
                        return default;
                    }
                    else
                    {
                        // Analyze the assigned expression
                        assign.Value.Accept(this);

                        // Do not attempt to remove copying from any other expression, just clear the assignment set for trgVar
                        _state.Data[trgHandle] = 0;

                        // Prevent from visiting trgVar due to its MightChange property
                        return default;
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

                // If a variable is modified, disable the deletion of all its current assignments
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
