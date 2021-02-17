using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.FlowAnalysis;
using Ast = Devsense.PHP.Syntax.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Peachpie.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics
{
    public interface IPhpOperation : IOperation
    {
        /// <summary>
        /// Corresponding syntax node.
        /// </summary>
        Ast.LangElement PhpSyntax { get; set; }

        /// <summary>
        /// Visitor with return value implementation.
        /// </summary>
        /// <typeparam name="TResult">Result type of the <paramref name="visitor"/>, <see cref="VoidStruct"/> if none.</typeparam>
        /// <param name="visitor">A reference to <see cref="PhpOperationVisitor{TResult}"/> instance.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor);
    }

    /// <summary>
    /// Abstract PHP expression semantic.
    /// </summary>
    public interface IPhpExpression : IPhpOperation
    {
        /// <summary>
        /// Analysed type information.
        /// The type is bound to a <see cref="TypeRefContext"/> associated with containing routine.
        /// </summary>
        TypeRefMask TypeRefMask { get; set; }

        /// <summary>
        /// The way the expression is accessed.
        /// May specify an additional operation or conversion.
        /// May specify the type that the expression will be converted to.
        /// </summary>
        BoundAccess Access { get; }

        /// <summary>
        /// Whether the expression needs current <c>Context</c> to be evaluated.
        /// If not, the expression can be evaluated in compile time or in app context.
        /// </summary>
        bool RequiresContext { get; }

        /// <summary>
        /// Decides whether an expression represented by this operation should be copied if it is passed by value (assignment, return).
        /// </summary>
        bool IsDeeplyCopied { get; }
    }

    public interface IPhpStatement : IPhpOperation
    {

    }

    public interface IPhpArgumentOperation : IPhpOperation // , IArgumentOperation
    {
        /// <summary>
        /// Variable unpacking in PHP, the triple-dot syntax.
        /// </summary>
        bool IsUnpacking { get; }

        /// <summary>
        /// If not <c>null</c>, specifies the named argument.
        /// </summary>
        string ParameterName { get; }
    }
}
