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
        public FlowContext/*!*/FlowContext => _flowCtx;
        readonly FlowContext _flowCtx;

        /// <summary>
        /// Gets type context.
        /// </summary>
        public TypeRefContext/*!*/TypeRefContext => _flowCtx.TypeRefContext;

        /// <summary>
        /// Source routine.
        /// Can be <c>null</c>.
        /// </summary>
        public SourceRoutineSymbol Routine => _flowCtx.Routine;

        /// <summary>
        /// Types of variables in this state.
        /// </summary>
        TypeRefMask[]/*!*/_varsType;

        /// <summary>
        /// Mask of initialized variables in this state.
        /// </summary>
        ulong _initializedMask;

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

            //
            _varsType = EnumeratorExtension.MixArrays(state1._varsType, state2._varsType, TypeRefMask.Or);
            _flowCtx = state1._flowCtx;
            _initializedMask = state1._initializedMask & state2._initializedMask;

            // intersection of other variable flags
            if (state1._lessThanLongMax != null && state2._lessThanLongMax != null)
            {
                _lessThanLongMax = new HashSet<VariableHandle>(state1._lessThanLongMax);
                _lessThanLongMax.Intersect(state2._lessThanLongMax);
            }

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

            _flowCtx = flowCtx;
            _initializedMask = (ulong)0;

            // initial size of the array
            var countHint = (flowCtx.Routine != null)
                ? flowCtx.Routine.LocalsTable.Count
                : 0;
            _varsType = new TypeRefMask[countHint];
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public FlowState(FlowState/*!*/other)
            : this(other.FlowContext, other._varsType)
        {
            // clone internal state

            _initializedMask = other._initializedMask;

            if (other._lessThanLongMax != null)
            {
                _lessThanLongMax = new HashSet<VariableHandle>(other._lessThanLongMax);
            }

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

            _flowCtx = flowCtx;
            _varsType = (TypeRefMask[])varsType.Clone();
        }

        #endregion

        #region IEquatable<FlowState> Members

        public bool Equals(FlowState other)
        {
            if (object.ReferenceEquals(this, other))
                return true;

            if (other == null ||
                other._flowCtx != _flowCtx ||
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
        public VariableHandle/*!*/GetLocalHandle(string varname)
        {
            return _flowCtx.GetVarIndex(new VariableName(varname));
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
            if (tmask.IsUninitialized) { SetVarUninitialized(handle); }
            else { SetVarInitialized(handle); }
        }

        /// <summary>
        /// Gets type of variable at this state.
        /// </summary>
        public TypeRefMask GetLocalType(VariableHandle handle)
        {
            handle.ThrowIfInvalid();
            return (handle < _varsType.Length) ? _varsType[handle] : 0;
        }

        /// <summary>
        /// Marks variable as being referenced.
        /// </summary>
        public void MarkLocalByRef(VariableHandle handle)
        {
            handle.ThrowIfInvalid();

            this.FlowContext.SetReference(handle);
            this.SetVarInitialized(handle);
            _flowCtx.SetUsed(handle);
        }

        /// <summary>
        /// Handles use of a local variable.
        /// </summary>
        public void VisitLocal(VariableHandle handle)
        {
            handle.ThrowIfInvalid();
            _flowCtx.SetUsed(handle);
        }

        /// <summary>
        /// Sets all variables as initialized at this state and with a <c>mixed</c> type.
        /// </summary>
        public void SetAllUnknown(bool maybeRef)
        {
            var tmask = maybeRef
                ? TypeRefMask.AnyType.WithRefFlag
                : TypeRefMask.AnyType;

            foreach (var v in _flowCtx.EnumerateVariables())
            {
                SetLocalType(v, tmask);
            }

            // all initialized
            _initializedMask = ~0u;
        }

        /// <summary>
        /// Gets value indicating the variable is set in all code paths.
        /// Gets also <c>true</c> if we don't known.
        /// </summary>
        public bool IsLocalSet(VariableHandle handle)
        {
            handle.ThrowIfInvalid();
            return handle.Slot >= FlowContext.BitsCount || (_initializedMask & (1u << handle)) != 0;
        }

        public void FlowThroughReturn(TypeRefMask type)
        {
            _flowCtx.ReturnType |= type;
        }

        void SetVarInitialized(int varindex)
        {
            if (varindex >= 0 && varindex < FlowContext.BitsCount)
            {
                _initializedMask |= 1u << varindex;
            }
        }

        void SetVarUninitialized(int varindex)
        {
            if (varindex >= 0 && varindex < FlowContext.BitsCount)
            {
                _initializedMask &= ~(1u << varindex);
            }
        }

        #endregion

        #region Constraints

        HashSet<VariableHandle> _lessThanLongMax;

        /// <summary>
        /// Sets or removes LTInt64 flag for a variable.
        /// </summary>
        public void LTInt64Max(VariableHandle handle, bool lt)
        {
            if (lt)
            {
                if (_lessThanLongMax == null) _lessThanLongMax = new HashSet<VariableHandle>();
                _lessThanLongMax.Add(handle);
            }
            else
            {
                if (_lessThanLongMax != null)
                {
                    _lessThanLongMax.Remove(handle);
                }
            }
        }

        /// <summary>
        /// Gets LTInt64 flag for a variable.
        /// </summary>
        public bool IsLTInt64Max(VariableHandle handle) => _lessThanLongMax != null && _lessThanLongMax.Contains(handle);

        #endregion

        /// <summary>
        /// Gets merged return value type.
        /// </summary>
        public TypeRefMask GetReturnType()
        {
            return _flowCtx.ReturnType;
        }
        
        //#region Variable Kind

        //Dictionary<VariableName, VariableKind> _varKindMap;

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
        /// Gets kind of variable declaration in this state.
        /// </summary>
        public VariableKind GetVarKind(VariableHandle handle)
        {
            handle.ThrowIfInvalid();

            //// explicit variable declaration
            //if (_varKindMap != null)
            //{
            //    VariableKind kind = VariableKind.LocalVariable;

            //    if (_varKindMap.TryGetValue(varname, out kind))
            //    {
            //        return kind;
            //    }
            //}

            // already declared on locals label
            return Routine.LocalsTable.GetVariableKind(handle.Name);
        }

        //#endregion
    }
}
