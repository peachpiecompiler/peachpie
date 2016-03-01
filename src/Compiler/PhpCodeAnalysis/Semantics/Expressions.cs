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
    #region AccessType

    /// <summary>
    /// Access type - describes context within which an expression is used.
    /// </summary>
    public enum AccessType : byte
    {
        None,          // serves for case when Expression is body of a ExpressionStmt.
                       // It is useless to push its value on the stack in that case
        Read,
        Write,         // this access can only have VariableUse of course
        ReadAndWrite,  // dtto, it serves for +=,*=, etc.
        ReadRef,       // this access can only have VarLikeConstructUse and RefAssignEx (eg. f($a=&$b); where decl. is: function f(&$x) {} )
        WriteRef,      // this access can only have VariableUse of course
        ReadUnknown,   // this access can only have VarLikeConstructUse and NewEx, 
                       // when they are act. param whose related formal param is not known
        WriteAndReadRef,        /*this access can only have VariableUse, it is used in case like:
													function f(&$x) {}
													f($a=$b);
                                */
        WriteAndReadUnknown, //dtto, but it is used when the signature of called function is not known 
            /* It is because of implementation of code generation that we
             * do not use an AccessType WriteRefAndReadRef in case of ReafAssignEx
             * f(&$x){} 
             * f($a=&$b)
             */
        ReadAndWriteAndReadRef, //for f($a+=$b);
        ReadAndWriteAndReadUnknown
    }

    #endregion

    #region BoundExpression

    public abstract class BoundExpression : IExpression
    {
        public TypeRefMask TypeRefMask { get; set; } = default(TypeRefMask);

        public AccessType Access { get; internal set; }

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

    #region BoundBinaryEx

    public sealed class BoundBinaryEx : BoundExpression, IBinaryOperatorExpression
    {
        public BinaryOperationKind BinaryOperationKind { get; private set; }

        public override OperationKind Kind => OperationKind.BinaryOperatorExpression;

        public IExpression Left { get; private set; }

        public IMethodSymbol Operator { get; set; }

        public IExpression Right { get; private set; }

        public bool UsesOperatorMethod => this.Operator != null;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitBinaryOperatorExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitBinaryOperatorExpression(this, argument);

        internal BoundBinaryEx(IExpression left, IExpression right, BinaryOperationKind kind)
        {
            this.Left = left;
            this.Right = right;
            this.BinaryOperationKind = kind;
        }
    }

    #endregion
}
