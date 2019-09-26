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
            private readonly Dictionary<BoundCopyValue, int> _copyIndices = new Dictionary<BoundCopyValue, int>();
            private readonly FlowContext _flowContext;

            private CopyAnalysisState _state;
            private BitMask _neededCopies;

            private int VariableCount => _flowContext.VarsType.Length;

            private CopyAnalysisContext(FlowContext flowContext)
            {
                _flowContext = flowContext;
            }

            public static HashSet<BoundCopyValue> TryGetUnnecessaryCopies(SourceRoutineSymbol routine)
            {
                var cfg = routine.ControlFlowGraph;
                var context = new CopyAnalysisContext(cfg.FlowContext);
                var analysis = new FixPointAnalysis<CopyAnalysisContext, CopyAnalysisState>(context, routine);
                analysis.Run();

                HashSet<BoundCopyValue> result = null;
                foreach (var kvp in context._copyIndices)
                {
                    if (!context._neededCopies.Get(kvp.Value))
                    {
                        if (result == null)
                            result = new HashSet<BoundCopyValue>();

                        result.Add(kvp.Key);
                    }
                }

                return result;
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
                if (x.IsDefault)
                    return y;
                else if (y.IsDefault)
                    return x;

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
                    && !_flowContext.IsReference(trgHandle = _flowContext.GetVarIndex(trgVarRef.Name.NameValue)))
                {
                    VariableHandle srcHandle;
                    if (MatchExprSkipCopy(assign.Value, out BoundVariableRef srcVarRef, out bool isCopied)
                        && srcVarRef.Name.IsDirect
                        && !_flowContext.IsReference(srcHandle = _flowContext.GetVarIndex(srcVarRef.Name.NameValue)))
                    {
                        if (isCopied)
                        {
                            // Make the assignment a candidate for copy removal, possibly causing aliasing.
                            // It is removed if either trgVar or srcVar are modified later.
                            int assignIndex = EnsureCopyIndex((BoundCopyValue)assign.Value);
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
                        _neededCopies |= _state.Data[i];
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
                        var varindex = _flowContext.GetVarIndex(x.Name.NameValue);
                        if (!_flowContext.IsReference(varindex))
                        {
                            _neededCopies |= _state.Data[varindex];
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

            private int EnsureCopyIndex(BoundCopyValue copy)
            {
                if (!_copyIndices.TryGetValue(copy, out int index))
                {
                    index = _copyIndices.Count;
                    _copyIndices.Add(copy, index);
                }

                return index;
            }
        }
    }
}
