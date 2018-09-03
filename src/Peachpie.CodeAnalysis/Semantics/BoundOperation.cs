using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.Symbols;
using Ast = Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.Semantics
{
    public abstract class BoundOperation : IOperation
    {
        #region Unsupported

        SyntaxNode IOperation.Syntax => null;

        IOperation IOperation.Parent => null;

        IEnumerable<IOperation> IOperation.Children => Array.Empty<IOperation>();

        SemanticModel IOperation.SemanticModel => null;

        #endregion

        public string Language => Constants.PhpLanguageName;

        public virtual bool IsImplicit => false;

        public abstract OperationKind Kind { get; }

        public virtual ITypeSymbol Type => null;

        /// <summary>
        /// Resolved value of the expression.
        /// </summary>
        Optional<object> IOperation.ConstantValue => ConstantValueHlp;

        protected virtual Optional<object> ConstantValueHlp => default(Optional<object>);

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }
}
