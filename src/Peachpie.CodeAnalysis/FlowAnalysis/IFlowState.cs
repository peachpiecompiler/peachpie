using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Updates variable information within this state.
        /// </summary>
        void SetVar(string name, TypeRefMask type);

        /// <summary>
        /// Gets type of variable at this state.
        /// Variable is expected to be local, not a member of chained expression.
        /// </summary>
        TypeRefMask GetVarType(string/*!*/name);

        /// <summary>
        /// Marks variable as a reference.
        /// </summary>
        void SetVarRef(string name);

        /// <summary>
        /// Marks variable as used.
        /// </summary>
        void SetVarUsed(string name);

        /// <summary>
        /// Sets all variables as initialized at this state.
        /// </summary>
        void SetAllInitialized();

        /// <summary>
        /// Records return value type.
        /// </summary>
        void FlowThroughReturn(TypeRefMask type);
    }
}
