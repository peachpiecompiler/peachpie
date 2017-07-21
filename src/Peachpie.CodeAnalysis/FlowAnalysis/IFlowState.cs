using Devsense.PHP.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    public interface IFlowState<T> : IEquatable<IFlowState<T>>
    {
        /// <summary>
        /// Creates copy of this state.
        /// </summary>
        T/*!*/Clone();

        /// <summary>
        /// Creates new state as a merge of this one and the other.
        /// </summary>
        T/*!*/Merge(T/*!*/other);

        #region Local Variable Handling

        /// <summary>
        /// Gets variable handle use for other variable operations.
        /// </summary>
        VariableHandle GetLocalHandle(VariableName varname);

        /// <summary>
        /// Sets variable type in this state.
        /// </summary>
        /// <param name="handle">Variable handle.</param>
        /// <param name="tmask">Variable type. If <c>void</c> or <c>uninitialized</c>, the variable is set as not initialized in this state.</param>
        void SetLocalType(VariableHandle handle, TypeRefMask tmask);

        /// <summary>
        /// Gets type of variable at this state.
        /// </summary>
        TypeRefMask GetLocalType(VariableHandle handle);

        /// <summary>
        /// Marks variable as being referenced.
        /// </summary>
        void MarkLocalByRef(VariableHandle handle);

        /// <summary>
        /// Handles use of a local variable.
        /// </summary>
        void VisitLocal(VariableHandle handle);

        /// <summary>
        /// Sets all variables as initialized at this state and with a <c>mixed</c> type.
        /// </summary>
        void SetAllUnknown(bool maybeRef);

        /// <summary>
        /// Gets value indicating the variable is set in all code paths.
        /// Gets also <c>true</c> if we don't known.
        /// </summary>
        bool IsLocalSet(VariableHandle handle);

        #endregion

        /// <summary>
        /// Records return value type.
        /// </summary>
        void FlowThroughReturn(TypeRefMask type);
    }

    /// <summary>
    /// Represents a variable in the routine context.
    /// </summary>
    [DebuggerDisplay("${Name.Value,nq}#{Slot}")]
    public struct VariableHandle : IEquatable<VariableHandle>
    {
        /// <summary>
        /// Valid indexes starts from <c>1</c>.
        /// </summary>
        int _index;

        /// <summary>
        /// The variable name.
        /// </summary>
        VariableName _name;

        /// <summary>
        /// Gets value indicating the handle is valid.
        /// </summary>
        public bool IsValid => (_index > 0);

        /// <summary>
        /// throws an exception if the handle is not valid.
        /// </summary>
        internal void ThrowIfInvalid()
        {
            if (!IsValid)
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Gets or sets internal slot within the locals variable table starting from <c>0</c>.
        /// </summary>
        public int Slot
        {
            get { return _index - 1; }
            set { _index = value + 1; }
        }

        /// <summary>
        /// The variable name.
        /// </summary>
        public VariableName Name
        {
            get { return _name; }
            internal set { _name = value; }
        }

        #region IEquatable<VariableHandle>

        bool IEquatable<VariableHandle>.Equals(VariableHandle other)
        {
            return _index == other._index;
        }

        public override int GetHashCode() => _index * 2;

        public override bool Equals(object obj) => obj is VariableHandle && ((VariableHandle)obj)._index == _index;

        #endregion

        /// <summary>
        /// Implicitly converts the handle to an integer slot index.
        /// </summary>
        public static implicit operator int(VariableHandle handle) => handle.Slot;
    }
}
