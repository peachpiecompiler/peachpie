using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
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
        #region Nested class: CommonState

        /// <summary>
        /// Common state shared across different local states (within the same FlowContext).
        /// </summary>
        public sealed class CommonState
        {
            /// <summary>
            /// Size of ulong bit array (<c>64</c>).
            /// </summary>
            internal const int BitsCount = sizeof(ulong) * 8;

            /// <summary>
            /// Unknown variable index.
            /// </summary>
            public const int UndefinedVarIndex = -1;

            /// <summary>
            /// Locals table reference.
            /// </summary>
            public FlowContext/*!*/FlowContext => _flowcontext;

            readonly FlowContext/*!*/_flowcontext;

            /// <summary>
            /// Reference to corresponding routine symbol. Cannot be null.
            /// </summary>
            public Symbols.SourceRoutineSymbol/*!*/Routine => _routine;

            readonly Symbols.SourceRoutineSymbol/*!*/_routine;

            /// <summary>
            /// Map of variables name and their index.
            /// </summary>
            readonly Dictionary<string, int>/*!*/_varsIndex;

            /// <summary>
            /// Bit mask of variables where bit with value <c>1</c> signalizes that variables with index corresponding to the bit number has been used.
            /// </summary>
            ulong _usedMask;

            public CommonState(FlowContext/*!*/flowcontext, Symbols.SourceRoutineSymbol/*!*/routine)
            {
                _flowcontext = flowcontext;
                _routine = routine;

                var locals = flowcontext.Locals;
                var dict = new Dictionary<string, int>(locals.Length, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < locals.Length; i++)
                {
                    dict[locals[i].Name] = i;
                }

                _varsIndex = dict;
            }

            /// <summary>
            /// Gets index of variable within the context.
            /// </summary>
            public int GetVarIndex(string name)
            {
                int index;
                if (!_varsIndex.TryGetValue(name, out index))
                {
                    index = UndefinedVarIndex;
                }

                return index;
            }

            /// <summary>
            /// Sets specified variable as being used.
            /// </summary>
            public void SetUsed(int varindex)
            {
                if (varindex >= 0 && varindex < BitsCount)
                    _usedMask |= (ulong)1 << varindex;
            }

            /// <summary>
            /// Marks all local variables as used.
            /// </summary>
            public void SetAllUsed()
            {
                _usedMask = ~(ulong)0;
            }

            public bool IsUsed(int varindex)
            {
                // anything >= 64 is used
                return varindex < 0 || varindex >= BitsCount || (_usedMask & (ulong)1 << varindex) != 0;
            }

            /// <summary>
            /// Enumerates unused variables.
            /// </summary>
            public IEnumerable<BoundVariable>/*!!*/GetUnusedVars()
            {
                var locals = _flowcontext.Locals;

                if (_usedMask == ~((ulong)0) || _usedMask == ((ulong)1 << locals.Length) - 1)
                    yield break;

                for (int i = 0; i < locals.Length; i++)
                    if ((_usedMask & ((ulong)1 << i)) == 0 && i < BitsCount)
                        yield return locals[i];
            }
        }

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Gets flow context.
        /// </summary>
        public FlowContext/*!*/FlowContext => _common.FlowContext;

        /// <summary>
        /// Gets type context.
        /// </summary>
        public TypeRefContext/*!*/TypeRefContext => _common.FlowContext.TypeRefContext;

        /// <summary>
        /// Gets information common across all states within the routine.
        /// </summary>
        public CommonState/*!*/Common => _common;

        readonly CommonState/*!*/_common;
        readonly TypeRefMask[]/*!*/_varsType;
        ulong _initializedMask;

        #endregion

        #region Construction & Copying

        /// <summary>
        /// Merge constructor.
        /// </summary>
        public FlowState(FlowState state1, FlowState state2)
        {
            Contract.ThrowIfNull(state1);
            Contract.ThrowIfNull(state2);
            Debug.Assert(state1._common == state2._common);

            //
            _varsType = EnumeratorExtension.MixArrays(state1._varsType, state2._varsType, TypeRefMask.Or);
            _common = state1._common;
            _initializedMask = state1._initializedMask & state2._initializedMask;

            // intersection of other variable flags
            if (state1._lessThanLongMax != null && state2._lessThanLongMax != null)
            {
                _lessThanLongMax = new HashSet<string>(state1._lessThanLongMax);
                _lessThanLongMax.Intersect(state2._lessThanLongMax);
            }
        }

        /// <summary>
        /// Initial locals state for the Start block.
        /// </summary>
        public FlowState(FlowContext/*!*/context, Symbols.SourceRoutineSymbol/*!*/routine)
            : this(context, new CommonState(context, routine))
        {
        }

        /// <summary>
        /// Initial locals state for the Start block.
        /// </summary>
        internal FlowState(FlowContext/*!*/flowcontext, CommonState/*!*/common)
        {
            Contract.ThrowIfNull(flowcontext);

            _common = common;
            _initializedMask = (ulong)0;

            var count = flowcontext.Locals.Length;
            _varsType = new TypeRefMask[count];
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public FlowState(FlowState/*!*/other)
            : this(other._common, other._varsType)
        {
            _initializedMask = other._initializedMask;

            if (other._lessThanLongMax != null)
                _lessThanLongMax = new HashSet<string>(other._lessThanLongMax);
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        private FlowState(CommonState/*!*/common, TypeRefMask[]/*!*/varsType)
        {
            Contract.ThrowIfNull(common);
            Contract.ThrowIfNull(varsType);

            _common = common;
            _varsType = (TypeRefMask[])varsType.Clone();
        }

        #endregion

        #region IEquatable<FlowState> Members

        public bool Equals(FlowState other)
        {
            if (object.ReferenceEquals(this, other))
                return true;

            if (other == null ||
                other._common != _common ||
                other._initializedMask != _initializedMask)
                return false;

            return EnumeratorExtension.Equals(_varsType, other._varsType);
        }

        public override int GetHashCode()
        {
            var hash = this.FlowContext.GetHashCode();
            foreach (var t in _varsType)
                hash ^= t.GetHashCode();

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

        public FlowState Clone() => new FlowState(this);

        public FlowState Merge(FlowState other) => new FlowState(this, other);

        /// <summary>
        /// Get merged variable value type.
        /// </summary>
        public TypeRefMask GetVarType(string name)
        {
            var index = _common.GetVarIndex(name);
            var types = _varsType;
            return (index >= 0 && index < types.Length)
                ? types[index]
                : TypeRefMask.AnyType;
        }

        /// <summary>
        /// Gets merged return value type.
        /// </summary>
        public TypeRefMask GetReturnType()
        {
            var index = _common.FlowContext.ReturnVarIndex;
            return (index >= 0)
                ? _varsType[index]      // merged return type
                : default(TypeRefMask); // void
        }
        
        public void SetAllInitialized()
        {
            _initializedMask = ~(ulong)0;
        }

        public void SetVarInitialized(string name)
        {
            SetVarInitialized(_common.GetVarIndex(name));
        }

        internal void SetVarInitialized(int varindex)
        {
            if (varindex >= 0 && varindex < CommonState.BitsCount)
                _initializedMask |= (ulong)1 << varindex;
        }

        public void SetVar(string name, TypeRefMask type)
        {
            SetVar(_common.GetVarIndex(name), type);
        }

        internal void SetVar(int varindex, TypeRefMask type)
        {
            var types = _varsType;
            if (varindex >= 0 && varindex < types.Length)
            {
                types[varindex] = type;
                this.SetVarInitialized(varindex);
                this.FlowContext.AddVarType(varindex, type);    // TODO: collect merged type information at the end of analysis
            }
        }

        /// <summary>
        /// Sets the variable is used by reference.
        /// </summary>
        public void SetVarRef(string name)
        {
            SetVarRef(_common.GetVarIndex(name));
        }

        /// <summary>
        /// Sets the variable is used by reference.
        /// </summary>
        internal void SetVarRef(int varindex)
        {
            this.FlowContext.SetReference(varindex);
            this.SetVarInitialized(varindex);
            _common.SetUsed(varindex);
        }

        public void SetVarUsed(string name)
        {
            SetVarUsed(_common.GetVarIndex(name));
        }

        internal void SetVarUsed(int varindex)
        {
            _common.SetUsed(varindex);
        }

        public void FlowThroughReturn(TypeRefMask type)
        {
            var index = _common.FlowContext.ReturnVarIndex;
            if (index < 0)
                throw new InvalidOperationException();

            SetVar(index, type);
        }

        #endregion

        #region Constraints

        HashSet<string> _lessThanLongMax;
        
        /// <summary>
        /// Sets or removes LTInt64 flag for a variable.
        /// </summary>
        public void LTInt64Max(string varname, bool lt)
        {
            if (lt)
            {
                if (_lessThanLongMax == null) _lessThanLongMax = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _lessThanLongMax.Add(varname);
            }
            else
            {
                if (_lessThanLongMax != null)
                    _lessThanLongMax.Remove(varname);
            }
        }

        /// <summary>
        /// Gets LTInt64 flag for a variable.
        /// </summary>
        /// <param name="varname"></param>
        /// <returns></returns>
        public bool IsLTInt64Max(string varname) => _lessThanLongMax != null && _lessThanLongMax.Contains(varname);

        #endregion
    }
}
