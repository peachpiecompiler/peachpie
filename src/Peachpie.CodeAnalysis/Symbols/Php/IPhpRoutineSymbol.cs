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
        /// Gets value indicating whether <c>CastToFalseAttribute</c> applies to this routine and
        /// <c>null</c> reference or negative number must be converted to <c>false</c>.
        /// </summary>
        /// <remarks>Applies to library functions only.</remarks>
        bool CastToFalse { get; }

        /// <summary>
        /// Gets value indicating the .ctor only initializes fields, and does not call __construct.
        /// Applicable only to instance constructors.
        /// </summary>
        bool IsInitFieldsOnly { get; }

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

        /// <summary>
        /// Gets value indicating the routine represents a global code.
        /// </summary>
        bool IsGlobalScope { get; }
    }
}
