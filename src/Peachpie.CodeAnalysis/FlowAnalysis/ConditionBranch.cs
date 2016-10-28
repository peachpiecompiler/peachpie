using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Whether the expression is evaluated as a part of branch condition.
    /// May have side effect of minimal evaluation (short-circuit evaluation) to current _state.
    /// Affects <c>&amp;&amp;</c> and <c>||</c> operators, optionally other conditions that might infer more information to true or false branch.
    /// </summary>
    public enum ConditionBranch
    {
        AnyResult = 0,
        ToTrue = +1,
        ToFalse = -1,

        Default = AnyResult,
    }

    /// <summary>
    /// Helper methods for <see cref="ConditionBranch"/>.
    /// </summary>
    internal static class ConditionBranchEnum
    {
        /// <summary>
        /// Switches <see cref="ConditionBranch.ToTrue"/> and <see cref="ConditionBranch.ToFalse"/>.
        /// </summary>
        public static ConditionBranch NegativeBranch(ConditionBranch branch)
        {
            return (ConditionBranch)(-((int)branch));
        }
    }
}
