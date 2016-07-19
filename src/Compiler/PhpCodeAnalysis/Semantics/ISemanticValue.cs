using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics.Graph;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents type of a value.
    /// </summary>
    internal interface ISemanticValue
    {
        /// <summary>
        /// Gets type of a result value.
        /// </summary>
        /// <param name="ctx">Type context. Cannot be <c>null</c>.</param>
        /// <returns>Type mask of result values. Can be <c>uninitialized</c> in case the value was not analyzed yet.</returns>
        TypeRefMask GetResultType(TypeRefContext/*!*/ctx);
    }

    /// <summary>
    /// Represents a function.
    /// </summary>
    internal interface ISemanticFunction : ISemanticValue, IMethodSymbol
    {
        /// <summary>
        /// Optional. Gets control flow graph(s) of the function.
        /// Used for interprocedural analysis.
        /// </summary>
        /// <remarks>Wrapping routine symbols have more than one CFG.</remarks>
        ImmutableArray<ControlFlowGraph> CFG { get; }

        /// <summary>
        /// Gets expected type of parameter at <paramref name="index"/>.
        /// </summary>
        /// <param name="ctx">Type context.</param>
        /// <param name="index">Parameter index.</param>
        /// <returns>Expected type of parameter at index. Can be uninitialized.</returns>
        TypeRefMask GetExpectedParamType(TypeRefContext/*!*/ctx, int index);

        /// <summary>
        /// Gets value indicating whether parameter at <paramref name="index"/> will be passed by reference.
        /// </summary>
        /// <param name="index">Paramater index.</param>
        /// <returns>True if parameter at <paramref name="index"/> is being passed by reference.</returns>
        bool IsParamByRef(int index);

        /// <summary>
        /// Gets value indicating whether paremeter at <paramref name="index"/> is variadic (<c>params</c>),
        /// so all passed parameters passed starting at this index will be packed into an array.
        /// </summary>
        /// <param name="index">Parameter index.</param>
        /// <returns>True if parameter is variadic.</returns>
        bool IsParamVariadic(int index);
    }

    ///// <summary>
    ///// Represents a class field.
    ///// </summary>
    //internal interface ISemanticField : ISemanticValue
    //{
    //    bool MergeType(TypeRefContext ctx, TypeRefMask type);
    //}

    ///// <summary>
    ///// Represents a variable, local or global.
    ///// </summary>
    //internal interface ISemanticVariable : ISemanticValue
    //{
    //    bool MergeType(TypeRefContext ctx, TypeRefMask type);
    //}

    //internal interface ISemanticConstant : ISemanticValue
    //{

    //}
}
