using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Semantics
{
    #region BoundExpression

    public abstract class BoundExpression : IExpression
    {
        //public TypeRefMask TypeRefMask { get; set; } = TypeRefMask.Void;
        
        public virtual Optional<object> ConstantValue => default(Optional<object>);

        public virtual bool IsInvalid => false;

        public abstract OperationKind Kind { get; }

        public virtual ITypeSymbol ResultType => null;

        public SyntaxNode Syntax => null;

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    #endregion

    #region BoundFunctionCall

    /// <summary>
    /// Represents a function call.
    /// </summary>
    public class BoundFunctionCall : BoundExpression, IInvocationExpression
    {
        public ImmutableArray<IArgument> ArgumentsInParameterOrder => ArgumentsInSourceOrder;

        public ImmutableArray<IArgument> ArgumentsInSourceOrder
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IArgument ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            throw new NotImplementedException();
        }

        public virtual IExpression Instance => null;

        public bool IsVirtual => false;

        public override OperationKind Kind => OperationKind.InvocationExpression;
        
        public IMethodSymbol TargetMethod { get; set; }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitInvocationExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitInvocationExpression(this, argument);
    }

    #endregion

    #region BoundEchoStatement

    public sealed class BoundEchoStatement : BoundFunctionCall
    {

    }

    #endregion
}
