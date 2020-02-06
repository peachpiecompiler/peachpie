using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    internal class FlowState : IFlowState<FlowState>
    {
        #region Fields & Properties

        /// <summary>
        /// Gets flow context.
        /// </summary>
        public FlowContext/*!*/FlowContext { get; }

        /// <summary>
        /// Gets type context.
        /// </summary>
        public TypeRefContext/*!*/TypeRefContext => FlowContext.TypeRefContext;

        /// <summary>
        /// Source routine.
        /// Can be <c>null</c>.
        /// </summary>
        public SourceRoutineSymbol Routine => FlowContext.Routine;

        /// <summary>
        /// Types of variables in this state.
        /// </summary>
        TypeRefMask[]/*!*/_varsType;

        /// <summary>
        /// Mask of initialized variables in this state.
        /// </summary>
        /// <remarks>
        /// Single bits indicates the corresponding variable was set.
        /// <c>0</c> determines the variable was not set in any code path.
        /// <c>1</c> determines the variable may be set.
        /// </remarks>
        ulong _initializedMask;

        /// <summary>
        /// Version of the analysis this state was created for.
        /// </summary>
        internal int Version { get; }

        #endregion

        #region Construction & Copying

        /// <summary>
        /// Merge constructor.
        /// </summary>
        public FlowState(FlowState state1, FlowState state2)
        {
            Debug.Assert(state1 != null);
            Debug.Assert(state2 != null);
            Debug.Assert(state1.FlowContext == state2.FlowContext);
            Debug.Assert(state1.Version == state2.Version);

            //
            FlowContext = state1.FlowContext;
            _varsType = EnumeratorExtension.MergeArrays(state1._varsType, state2._varsType, MergeType);
            _initializedMask = state1._initializedMask | state2._initializedMask;

            // intersection of other variable flags
            if (state1._notes != null && state2._notes != null)
            {
                _notes = new HashSet<NoteData>(state1._notes);
                _notes.Intersect(state2._notes);
            }

            Version = state1.Version;

            //// merge variables kind,
            //// conflicting kinds are not allowed currently!
            //if (state1._varKindMap != null || state1._varKindMap != null)
            //{
            //    _varKindMap = new Dictionary<VariableName, VariableKind>();
            //    if (state1._varKindMap != null) state1._varKindMap.Foreach(k => SetVarKind(k.Key, k.Value));
            //    if (state2._varKindMap != null) state2._varKindMap.Foreach(k => SetVarKind(k.Key, k.Value));
            //}
        }

        /// <summary>
        /// Initial locals state for the Start block.
        /// </summary>
        internal FlowState(FlowContext/*!*/flowCtx)
        {
            Contract.ThrowIfNull(flowCtx);

            FlowContext = flowCtx;
            _initializedMask = (ulong)0;

            // initial size of the array
            var countHint = (flowCtx.Routine != null)
                ? flowCtx.VarsType.Length
                : 0;
            _varsType = new TypeRefMask[countHint];

            Version = flowCtx.Version;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public FlowState(FlowState/*!*/other)
            : this(other.FlowContext, other._varsType)
        {
            // clone internal state

            _initializedMask = other._initializedMask;

            if (other._notes != null)
            {
                _notes = new HashSet<NoteData>(other._notes);
            }

            Version = other.Version;

            //if (other._varKindMap != null)
            //{
            //    _varKindMap = new Dictionary<VariableName, VariableKind>(other._varKindMap);
            //}
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        private FlowState(FlowContext/*!*/flowCtx, TypeRefMask[]/*!*/varsType)
        {
            Contract.ThrowIfNull(flowCtx);
            Contract.ThrowIfNull(varsType);

            FlowContext = flowCtx;
            _varsType = (TypeRefMask[])varsType.Clone();
            Version = flowCtx.Version;
        }

        #endregion

        #region IEquatable<FlowState> Members

        public bool Equals(FlowState other)
        {
            if (object.ReferenceEquals(this, other))
                return true;

            if (other == null ||
                other.FlowContext != FlowContext ||
                other._initializedMask != _initializedMask)
                return false;

            return EnumeratorExtension.EqualEntries(_varsType, other._varsType);
        }

        public override int GetHashCode()
        {
            var hash = this.FlowContext.GetHashCode();
            foreach (var t in _varsType)
            {
                hash ^= t.GetHashCode();
            }

            return hash;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FlowState);
        }

        public bool Equals(IFlowState<FlowState> other)
        {
            return Equals(other as FlowState);
        }

        #endregion

        #region IFlowState<FlowState>

        /// <summary>
        /// Creates copy of this state.
        /// </summary>
        public FlowState Clone() => new FlowState(this);

        /// <summary>
        /// Creates new state as a merge of this one and the other.
        /// </summary>
        public FlowState Merge(FlowState other) => new FlowState(this, other);

        /// <summary>
        /// Gets variable handle use for other variable operations.
        /// </summary>
        public VariableHandle/*!*/GetLocalHandle(VariableName varname)
        {
            return FlowContext.GetVarIndex(varname);
        }

        /// <summary>
        /// Sets variable type in this state.
        /// </summary>
        /// <param name="handle">Variable handle.</param>
        /// <param name="tmask">Variable type. If <c>uninitialized</c>, the variable is set as not initialized in this state.</param>
        public void SetLocalType(VariableHandle handle, TypeRefMask tmask)
        {
            handle.ThrowIfInvalid();

            if (handle >= _varsType.Length)
            {
                Array.Resize(ref _varsType, handle + 1);
            }

            _varsType[handle] = tmask;

            this.FlowContext.AddVarType(handle, tmask);    // TODO: collect merged type information at the end of analysis

            // update the _initializedMask
            SetVarInitialized(handle);
        }

        /// <summary>
        /// Sets variable type with byref flag in this state.
        /// </summary>
        /// <param name="handle">Variable handle.</param>
        public void SetLocalRef(VariableHandle handle)
        {
            SetLocalType(handle, GetLocalType(handle).WithRefFlag);
        }

        /// <summary>
        /// Gets type of variable at this state.
        /// </summary>
        public TypeRefMask GetLocalType(VariableHandle handle)
        {
            handle.ThrowIfInvalid();
            return (handle < _varsType.Length) ? _varsType[handle] : GetUnknownLocalType(handle);
        }

        TypeRefMask GetUnknownLocalType(VariableHandle handle)
        {
            return IsLocalSet(handle)
                ? TypeRefMask.AnyType.WithRefFlag   // <= SetAllUnknown() called
                : 0;                                // variable was not initialized in the state yet
        }

        /// <summary>
        /// Marks variable as being referenced.
        /// </summary>
        public void MarkLocalByRef(VariableHandle handle)
        {
            handle.ThrowIfInvalid();

            this.FlowContext.SetReference(handle);
            this.FlowContext.SetUsed(handle);
            this.SetVarInitialized(handle);
        }

        /// <summary>
        /// Handles use of a local variable.
        /// </summary>
        public void VisitLocal(VariableHandle handle)
        {
            handle.ThrowIfInvalid();
            FlowContext.SetUsed(handle);
        }

        /// <summary>
        /// Sets all variables as initialized at this state and with a <c>mixed</c> type.
        /// </summary>
        public void SetAllUnknown(bool maybeRef)
        {
            var tmask = maybeRef
                ? TypeRefMask.AnyType.WithRefFlag
                : TypeRefMask.AnyType;

            foreach (var v in FlowContext.EnumerateVariables())
            {
                SetLocalType(v, tmask);
            }

            // all initialized
            _initializedMask = ~0u;
        }

        /// <summary>
        /// Gets value indicating the variable may be set in some code paths.
        /// Gets also <c>true</c> if we don't known.
        /// </summary>
        public bool IsLocalSet(VariableHandle handle)
        {
            handle.ThrowIfInvalid();
            return handle.Slot >= FlowContext.BitsCount || (_initializedMask & (1ul << handle)) != 0;
        }

        public void FlowThroughReturn(TypeRefMask type)
        {
            FlowContext.ReturnType |= type;
        }

        public void SetVarInitialized(VariableHandle handle)
        {
            int varindex = handle.Slot;
            if (varindex >= 0 && varindex < FlowContext.BitsCount)
            {
                _initializedMask |= 1ul << varindex;
            }
        }

        public void SetVarUninitialized(VariableHandle handle)
        {
            var varindex = handle.Slot;
            if (varindex >= 0 && varindex < FlowContext.BitsCount)
            {
                _initializedMask &= ~(1ul << varindex);
            }
        }

        #endregion

        #region Constraints (Notes about variables)

        enum NoteKind
        {
            /// <summary>
            /// Noting that variable is less than Long.Max.
            /// </summary>
            LessThanLongMax,

            /// <summary>
            /// Noting that variable is greater than Long.Min.
            /// </summary>
            GreaterThanLongMin,
        }

        struct NoteData : IEquatable<NoteData>
        {
            public VariableHandle Variable;
            public NoteKind Kind;

            public NoteData(VariableHandle variable, NoteKind kind)
            {
                this.Variable = variable;
                this.Kind = kind;
            }

            public override int GetHashCode() => Variable.GetHashCode() ^ (int)Kind;

            public bool Equals(NoteData other) => this.Variable == other.Variable && this.Kind == other.Kind;
        }
        HashSet<NoteData> _notes;

        bool HasConstrain(VariableHandle variable, NoteKind kind) => _notes != null && _notes.Contains(new NoteData(variable, kind));

        void SetConstrain(VariableHandle variable, NoteKind kind, bool set)
        {
            if (set) AddConstrain(variable, kind);
            else RemoveConstrain(variable, kind);
        }

        void AddConstrain(VariableHandle variable, NoteKind kind)
        {
            if (variable.IsValid)
            {
                var notes = _notes;
                if (notes == null)
                {
                    _notes = notes = new HashSet<NoteData>();
                }

                notes.Add(new NoteData(variable, kind));
            }
        }

        void RemoveConstrain(VariableHandle variable, NoteKind kind)
        {
            if (variable.IsValid)
            {
                var notes = _notes;
                if (notes != null)
                {
                    notes.Remove(new NoteData(variable, kind));

                    if (notes.Count == 0)
                    {
                        _notes = null;
                    }
                }
            }
        }

        /// <summary>
        /// Sets or removes LTInt64 flag for a variable.
        /// </summary>
        public void SetLessThanLongMax(VariableHandle handle, bool lt) => SetConstrain(handle, NoteKind.LessThanLongMax, lt);

        /// <summary>
        /// Gets LTInt64 flag for a variable.
        /// </summary>
        public bool IsLessThanLongMax(VariableHandle handle) => HasConstrain(handle, NoteKind.LessThanLongMax);

        /// <summary>
        /// Sets or removes GTInt64 flag for a variable.
        /// </summary>
        public void SetGreaterThanLongMin(VariableHandle handle, bool lt) => SetConstrain(handle, NoteKind.GreaterThanLongMin, lt);

        /// <summary>
        /// Gets GTInt64 flag for a variable.
        /// </summary>
        public bool IsGreaterThanLongMin(VariableHandle handle) => HasConstrain(handle, NoteKind.GreaterThanLongMin);

        #endregion

        /// <summary>
        /// Gets merged return value type.
        /// </summary>
        public TypeRefMask GetReturnType()
        {
            return FlowContext.ReturnType;
        }

        /// <summary>
        /// Declares variable with a specific kind (static or global).
        /// </summary>
        public void SetVarKind(VariableHandle handle, VariableKind kind)
        {
            //if (_varKindMap == null)
            //{
            //    _varKindMap = new Dictionary<VariableName, VariableKind>();
            //}
            //else
            //{
            //    VariableKind old;
            //    if (_varKindMap.TryGetValue(varname, out old))
            //    {
            //        if (old != kind) throw new ArgumentException("redeclaration with a different kind not supported", nameof(kind));
            //    }
            //}

            //_varKindMap[varname] = kind;
        }

        /// <summary>
        /// Merges given types coming from two flows.
        /// Uninitialized value (<c>0L</c>) is treated as <c>NULL</c>.
        /// </summary>
        TypeRefMask MergeType(TypeRefMask t1, TypeRefMask t2)
        {
            var result = t1 | t2;

            if (t1.IsDefault || t2.IsDefault)
            {
                result |= TypeRefContext.GetNullTypeMask();
            }

            return result;
        }
    }
}
