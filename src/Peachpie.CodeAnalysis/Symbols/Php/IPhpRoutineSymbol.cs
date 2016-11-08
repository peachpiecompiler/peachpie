using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// A symbol representing PHP routine (function or method) in CLR.
    /// </summary>
    public interface IPhpRoutineSymbol : IMethodSymbol, IPhpValue
    {
        /// <summary>
        /// Gets value indicating whether <see cref="Core.CastToFalse"/> attribute applies to this routine and
        /// <c>null</c> reference or negative number must be converted to <c>false</c>.
        /// </summary>
        /// <remarks>Applies to library functions only.</remarks>
        bool CastToFalse { get; }

        /// <summary>
        /// For source routines, gets their control flow graph.
        /// Can be <c>null</c> for routines from PE.
        /// </summary>
        ControlFlowGraph ControlFlowGraph { get; }

        /// <summary>
        /// Gets a global function and a method name,
        /// otherwise an empty string.
        /// </summary>
        string RoutineName { get; }
    }
}
