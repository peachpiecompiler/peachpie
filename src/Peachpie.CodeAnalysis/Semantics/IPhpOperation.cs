using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics
{
    public interface IPhpOperation : IOperation
    {
        /// <summary>
        /// Corresponding syntax node.
        /// </summary>
        LangElement PhpSyntax { get; }

        /// <summary>
        /// Visitor implementation.
        /// </summary>
        /// <param name="visitor">A reference to <see cref="PhpOperationVisitor"/> instance.</param>
        void Accept(PhpOperationVisitor visitor);
    }

    /// <summary>
    /// Abstract PHP expression semantic.
    /// </summary>
    public interface IPhpExpression : IPhpOperation, IExpression
    {
        /// <summary>
        /// Analysed type information.
        /// The type is bound to a <see cref="TypeRefContext"/> associated with containing routine.
        /// </summary>
        TypeRefMask TypeRefMask { get; set; }

        /// <summary>
        /// Expression access information, whether it is being read, written to and what is the expected result type.
        /// </summary>
        BoundAccess Access { get; }

        /// <summary>
        /// Whether the expression needs current <see cref="Pchp.Core.Context"/> to be evaluated.
        /// If not, the expression can be evaluated in compile time or in app context.
        /// </summary>
        bool RequiresContext { get; }
    }

    public interface IPhpStatement : IPhpOperation, IStatement
    {

    }
}
