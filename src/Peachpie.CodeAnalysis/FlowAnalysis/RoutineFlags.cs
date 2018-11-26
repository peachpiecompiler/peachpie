using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Flags describing special routine needs.
    /// Collected during flow analysis.
    /// </summary>
    [Flags]
    public enum RoutineFlags
    {
        None = 0,

        HasEval = 1,
        HasInclude = 2,
        HasIndirectVar = 4,
        UsesLocals = 8,
        UsesArgs = 16,

        IsGenerator = 32,

        /// <summary>
        /// The routine uses <c>static::</c> construct to access late static bound type.
        /// </summary>
        UsesLateStatic = 64,

        /// <summary>
        /// Indicates the routine contains function calls.
        /// This can be used for analysis heuristics.
        /// </summary>
        HasUserFunctionCall = 128,

        /// <summary>
        /// Indicates that the routine declaration was proven unreachable during the analysis.
        /// </summary>
        IsUnreachable = 256,

        /// <summary>
        /// Whether the routine has to define local variables as an array instead of native local variables.
        /// </summary>
        RequiresLocalsArray = HasEval | HasInclude | HasIndirectVar | UsesLocals | IsGenerator,

        /// <summary>
        /// Whether the routine accesses its arguments dynamically we should provide params.
        /// </summary>
        RequiresVarArg = UsesArgs,
    }
}
