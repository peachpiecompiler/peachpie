using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Utilities;
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
            private BitMask64[] _varState;

            public CopyAnalysisState(int varCount)
            {
                _varState = new BitMask64[varCount];
            }

            public bool IsDefault => _varState == null;

            public int VariableCount => _varState.Length;

            public CopyAnalysisState Clone()
            {
                var clone = new CopyAnalysisState(_varState.Length);
                _varState.CopyTo(clone._varState, 0);

                return clone;
            }

            public bool Equals(CopyAnalysisState other)
            {
                if (this.IsDefault != other.IsDefault)
                    return false;

                if ((this.IsDefault && other.IsDefault) || _varState == other._varState)
                    return true;

                // We are supposed to compare only states from the same routine
                Debug.Assert(_varState.Length == other._varState.Length);

                for (int i = 0; i < other._varState.Length; i++)
                {
                    if (_varState[i] != other._varState[i])
                        return false;
                }

                return true;
            }

            public BitMask64 GetValue(int varIndex) => _varState[varIndex];

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

                var merged = new CopyAnalysisState(_varState.Length);
                for (int i = 0; i < merged._varState.Length; i++)
                {
                    merged._varState[i] = _varState[i] | other._varState[i];
                }

                return merged;
            }

            public CopyAnalysisState WithValue(int varIndex, BitMask64 value)
            {
                Debug.Assert(!IsDefault);

                if (_varState[varIndex] == value)
                {
                    return this;
                }
                else
                {
                    var result = Clone();
                    result._varState[varIndex] = value;
                    return result;
                }
            }

            public CopyAnalysisState WithCopyAssignment(int trgVarIndex, int srcVarIndex, int copyIndex)
            {
                Debug.Assert(!IsDefault);

                var copyMask = BitMask64.FromSingleValue(copyIndex);

                if (_varState[trgVarIndex] != copyMask || _varState[srcVarIndex] != (_varState[srcVarIndex] | copyMask))
                {
                    var result = Clone();
                    result._varState[trgVarIndex] = copyMask;
                    result._varState[srcVarIndex] |= copyMask;
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
        private class CopyAnalysis : AnalysisWalker<CopyAnalysisState, VoidStruct>
        {
            #region Fields

            private readonly Dictionary<BoundCopyValue, int> _copyIndices = new Dictionary<BoundCopyValue, int>();
            private readonly FlowContext _flowContext;

            /// <summary>
            /// Set of <see cref="BoundCopyValue"/> instances located in return statements, to be filtered in the exit node.
            /// </summary>
            private HashSet<BoundCopyValue> _lazyReturnCopies;

            private BitMask64 _neededCopies;

            private int VariableCount => _flowContext.VarsType.Length;

            private Dictionary<BoundBlock, CopyAnalysisState> _blockToStateMap = new Dictionary<BoundBlock, CopyAnalysisState>();

            private DistinctQueue<BoundBlock> _worklist = new DistinctQueue<BoundBlock>(new BoundBlock.OrdinalComparer());

            #endregion

            #region Usage

            private CopyAnalysis(FlowContext flowContext)
            {
                _flowContext = flowContext;
            }

            /// <summary>
            /// Adds the value to lazily initialized set.
            /// </summary>
            static void Add<TCollection, TValue>(ref TCollection set, TValue value) where
                TCollection : ICollection<TValue>, new()
            {
                Debug.Assert(value != null);

                set ??= new TCollection();
                set.Add(value);
            }

            public static HashSet<BoundCopyValue> TryGetUnnecessaryCopies(SourceRoutineSymbol routine)
            {
                var cfg = routine.ControlFlowGraph;
                var analysis = new CopyAnalysis(cfg.FlowContext);

                analysis._blockToStateMap[cfg.Start] = new CopyAnalysisState(analysis.VariableCount);
                analysis._worklist.Enqueue(cfg.Start);
                while (analysis._worklist.TryDequeue(out var block))
                {
                    analysis.Accept(block);
                }

                HashSet<BoundCopyValue> result = analysis._lazyReturnCopies;  // analysis won't be used anymore, no need to copy the set
                foreach (var kvp in analysis._copyIndices)
                {
                    if (!analysis._neededCopies.Get(kvp.Value))
                    {
                        Add(ref result, kvp.Key);
                    }
                }

                return result;
            }

            #endregion

            #region State handling

            protected override bool IsStateInitialized(CopyAnalysisState state) => !state.IsDefault;

            protected override bool AreStatesEqual(CopyAnalysisState a, CopyAnalysisState b) => a.Equals(b);

            protected override CopyAnalysisState GetState(BoundBlock block) => _blockToStateMap.TryGetOrDefault(block);

            protected override void SetState(BoundBlock block, CopyAnalysisState state) => _blockToStateMap[block] = state;

            protected override CopyAnalysisState CloneState(CopyAnalysisState state) => state;  // Copy-on-write semantics

            protected override CopyAnalysisState MergeStates(CopyAnalysisState a, CopyAnalysisState b) => a.WithMerge(b);

            protected override void SetStateUnknown(ref CopyAnalysisState state)
            {
                // TODO: Make more precise after the precision of try block analysis is improved in AnalysisWalker

                // Assume the worst case and require all the copy operations
                _neededCopies.SetAll();
                _lazyReturnCopies = null;
                _flags |= AnalysisFlags.IsCanceled;
            }

            protected override void EnqueueBlock(BoundBlock block) => _worklist.Enqueue(block);

            #endregion

            #region Visit expressions

            public override VoidStruct VisitAssign(BoundAssignEx assign)
            {
                ProcessAssignment(assign);
                return default;
            }

            private VariableHandle ProcessAssignment(BoundAssignEx assign)
            {
                bool CheckVariable(BoundVariableRef varRef, out VariableHandle handle)
                {
                    if (varRef.Name.IsDirect && !varRef.Name.NameValue.IsAutoGlobal
                        && !_flowContext.IsReference(handle = _flowContext.GetVarIndex(varRef.Name.NameValue)))
                    {
                        return true;
                    }
                    else
                    {
                        handle = default;
                        return false;
                    }
                }

                bool MatchSourceVarOrNestedAssignment(BoundExpression expr, out VariableHandle handle, out bool isCopied)
                {
                    if (MatchExprSkipCopy(expr, out BoundVariableRef varRef, out isCopied) && CheckVariable(varRef, out handle))
                    {
                        return true;
                    }
                    else if (MatchExprSkipCopy(expr, out BoundAssignEx nestedAssign, out isCopied))
                    {
                        handle = ProcessAssignment(nestedAssign);
                        return handle.IsValid;
                    }
                    else
                    {
                        handle = default;
                        return false;
                    }
                }

                // Handle assignment to a variable
                if (assign.Target is BoundVariableRef trgVarRef && CheckVariable(trgVarRef, out var trgHandle))
                {
                    if (MatchSourceVarOrNestedAssignment(assign.Value, out var srcHandle, out bool isCopied))
                    {
                        if (isCopied)
                        {
                            // Make the assignment a candidate for copy removal, possibly causing aliasing.
                            // It is removed if either trgVar or srcVar are modified later.
                            int copyIndex = EnsureCopyIndex((BoundCopyValue)assign.Value);
                            State = State.WithCopyAssignment(trgHandle, srcHandle, copyIndex);
                        }
                        else
                        {
                            // The copy was removed by a previous transformation, making them aliases sharing the assignments
                            State = State.WithValue(trgHandle, State.GetValue(srcHandle));
                        }

                        // Visiting trgVar would destroy the effort (due to the assignment it's considered as MightChange),
                        // visiting srcVar is unnecessary
                        return trgHandle;
                    }
                    else
                    {
                        // Analyze the assigned expression
                        assign.Value.Accept(this);

                        // Do not attempt to remove copying from any other expression, just clear the assignment set for trgVar
                        State = State.WithValue(trgHandle, 0);

                        // Prevent from visiting trgVar due to its MightChange property
                        return trgHandle;
                    }
                }

                base.VisitAssign(assign);
                return default;
            }

            public override VoidStruct VisitVariableRef(BoundVariableRef x)
            {
                void MarkAllKnownAssignments()
                {
                    for (int i = 0; i < State.VariableCount; i++)
                    {
                        _neededCopies |= State.GetValue(i);
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
                            _neededCopies |= State.GetValue(varindex);
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

            public override VoidStruct VisitReturn(BoundReturnStatement x)
            {
                if (x.Returned is BoundCopyValue copy && copy.Expression is BoundVariableRef varRef &&
                    !varRef.TypeRefMask.IsRef && // BoundCopyValue is used to dereference the alias
                    !varRef.Name.NameValue.IsAutoGlobal &&
                    varRef.Name.IsDirect)
                {
                    Add(ref _lazyReturnCopies, copy);
                }

                return base.VisitReturn(x);
            }

            public override VoidStruct VisitCFGExitBlock(ExitBlock x)
            {
                base.VisitCFGExitBlock(x);

                // Filter out the copies of variables in return statements which cannot be removed
                if (_lazyReturnCopies != null)
                {
                    List<BoundCopyValue> cannotRemove = null;

                    foreach (var returnCopy in _lazyReturnCopies)
                    {
                        var varRef = (BoundVariableRef)returnCopy.Expression;
                        var varindex = _flowContext.GetVarIndex(varRef.Name.NameValue);

                        // We cannot remove a variable which might alias any other variable due to
                        // a copying we removed earlier
                        if ((State.GetValue(varindex) & ~_neededCopies) != 0)
                        {
                            Add(ref cannotRemove, returnCopy);
                        }
                    }

                    if (cannotRemove != null)
                    {
                        _lazyReturnCopies.ExceptWith(cannotRemove);
                    }
                }

                return default;
            }

            #endregion

            #region Helpers

            private int EnsureCopyIndex(BoundCopyValue copy)
            {
                if (!_copyIndices.TryGetValue(copy, out int index))
                {
                    index = _copyIndices.Count;
                    _copyIndices.Add(copy, index);
                }

                return index;
            }

            #endregion
        }
    }
}
