using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.FlowAnalysis;

namespace Pchp.CodeAnalysis.Semantics
{
    #region BoundExpression

    public abstract class BoundExpression : IExpression
    {
        //public TypeRefMask TypeRefMask { get; set; } = TypeRefMask.Void;
        
        public virtual Optional<object> ConstantValue => default(Optional<object>);

        public virtual bool IsInvalid => false;

        public abstract OperationKind Kind { get; }

        public virtual ITypeSymbol ResultType { get; set; }

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
        protected ImmutableArray<BoundExpression> _arguments;

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

    #region BoundEcho

    public sealed class BoundEcho : BoundFunctionCall
    {
        public BoundEcho(ImmutableArray<BoundExpression> arguments)
        {
            _arguments = arguments;
        }
    }

    #endregion

    #region BoundLiteral

    public class BoundLiteral : BoundExpression, ILiteralExpression
    {
        Optional<object> _value;

        public string Spelling => this.ConstantValue.Value.ToString();

        public override Optional<object> ConstantValue => _value;

        public override OperationKind Kind => OperationKind.LiteralExpression;

        public BoundLiteral(object value)
        {
            _value = new Optional<object>(value);
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitLiteralExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitLiteralExpression(this, argument);
    }

    #endregion
}
