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
        /// <summary>
        /// Each state in copy analysis maps each variable to a set of <see cref="BoundCopyValue"/> instances.
        /// Whenever a variable is modified, all the mapped copy operations are considered unavailable for removal
        /// due to the possible aliasing.
        /// </summary>
        private struct CopyAnalysisState
        {
            private BitMask[] _data;

            public CopyAnalysisState(int varCount)
            {
                _data = new BitMask[varCount];
            }

            public bool IsDefault => _data == null;

            public int VariableCount => _data.Length;

            public CopyAnalysisState Clone()
            {
                var clone = new CopyAnalysisState(_data.Length);
                _data.CopyTo(clone._data, 0);

                return clone;
            }

            public bool Equals(CopyAnalysisState other)
            {
                if (this.IsDefault != other.IsDefault)
                    return false;

                if ((this.IsDefault && other.IsDefault) || _data == other._data)
                    return true;

                // We are supposed to compare only states from the same routine
                Debug.Assert(_data.Length == other._data.Length);

                for (int i = 0; i < other._data.Length; i++)
                {
                    if (_data[i] != other._data[i])
                        return false;
                }

                return true;
            }

            public BitMask GetValue(int varIndex) => _data[varIndex];

            public CopyAnalysisState WithMerge(CopyAnalysisState other)
            {
                if (this.IsDefault)
                    return other;
                else if (other.IsDefault)
                    return this;

                if (this.Equals(other))
                {
                    return this;
                }

                var merged = new CopyAnalysisState(_data.Length);
                for (int i = 0; i < merged._data.Length; i++)
                {
                    merged._data[i] = _data[i] | other._data[i];
                }

                return merged;
            }

            public CopyAnalysisState WithValue(int varIndex, BitMask value)
            {
                Debug.Assert(!IsDefault);

                if (_data[varIndex] == value)
                {
                    return this;
                }
                else
                {
                    var result = Clone();
                    result._data[varIndex] = value;
                    return result;
                }
            }

            public CopyAnalysisState WithCopyAssignment(int trgVarIndex, int srcVarIndex, int copyIndex)
            {
                Debug.Assert(!IsDefault);

                var copyMask = BitMask.FromSingleValue(copyIndex);

                if (_data[trgVarIndex] != copyMask || _data[srcVarIndex] != (_data[srcVarIndex] | copyMask))
                {
                    var result = Clone();
                    result._data[trgVarIndex] = copyMask;
                    result._data[srcVarIndex] |= copyMask;
                    return result;
                }
                else
                {
                    return this;
                }
            }
        }

        /// <summary>
        /// Implements copy analysis using <see cref="CopyAnalysisState"/>, producing a set of <see cref="BoundCopyValue"/>
        /// instances available for removal.
        /// </summary>
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

            public bool StatesEqual(CopyAnalysisState x, CopyAnalysisState y) => x.Equals(y);

            public CopyAnalysisState GetInitialState() => new CopyAnalysisState(VariableCount);

            public CopyAnalysisState MergeStates(CopyAnalysisState x, CopyAnalysisState y) => x.WithMerge(y);

            public CopyAnalysisState ProcessBlock(BoundBlock block, CopyAnalysisState state)
            {
                _state = state;
                block.Accept(this);
                return _state;
            }

            public override VoidStruct VisitAssign(BoundAssignEx assign)
            {
                // Handle assignment to a variable
                VariableHandle trgHandle;
                if (assign.Target is BoundVariableRef trgVarRef
                    && trgVarRef.Name.IsDirect && !trgVarRef.Name.NameValue.IsAutoGlobal
                    && !_flowContext.IsReference(trgHandle = _flowContext.GetVarIndex(trgVarRef.Name.NameValue)))
                {
                    VariableHandle srcHandle;
                    if (MatchExprSkipCopy(assign.Value, out BoundVariableRef srcVarRef, out bool isCopied)
                        && srcVarRef.Name.IsDirect && !srcVarRef.Name.NameValue.IsAutoGlobal
                        && !_flowContext.IsReference(srcHandle = _flowContext.GetVarIndex(srcVarRef.Name.NameValue)))
                    {
                        if (isCopied)
                        {
                            // Make the assignment a candidate for copy removal, possibly causing aliasing.
                            // It is removed if either trgVar or srcVar are modified later.
                            int copyIndex = EnsureCopyIndex((BoundCopyValue)assign.Value);
                            _state = _state.WithCopyAssignment(trgHandle, srcHandle, copyIndex);
                        }
                        else
                        {
                            // The copy was removed by a previous transformation, making them aliases sharing the assignments
                            _state = _state.WithValue(trgHandle, _state.GetValue(srcHandle));
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
                        _state = _state.WithValue(trgHandle, 0);

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
                    for (int i = 0; i < _state.VariableCount; i++)
                    {
                        _neededCopies |= _state.GetValue(i);
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
                    else if (!x.Name.NameValue.IsAutoGlobal)
                    {
                        var varindex = _flowContext.GetVarIndex(x.Name.NameValue);
                        if (!_flowContext.IsReference(varindex))
                        {
                            _neededCopies |= _state.GetValue(varindex);
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
