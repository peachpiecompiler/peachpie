using Microsoft.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.FlowAnalysis;
using System.Diagnostics;
using Pchp.Syntax.AST;

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

    public abstract partial class BoundExpression : IExpression
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

    public partial class BoundArgument : IArgument
    {
        public ArgumentKind ArgumentKind => ArgumentKind.Positional;    // TODO: DefaultValue, ParamArray

        public IExpression InConversion => null;

        public bool IsInvalid => false;

        public OperationKind Kind => OperationKind.Argument;

        public IExpression OutConversion => null;

        public IParameterSymbol Parameter { get; set; }

        public SyntaxNode Syntax => null;

        IExpression IArgument.Value => Value;

        public BoundExpression Value { get; set; }

        public BoundArgument(BoundExpression value)
        {
            this.Value = value;
        }

        public void Accept(OperationVisitor visitor)
            => visitor.VisitArgument(this);

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitArgument(this, argument);

    }

    /// <summary>
    /// Represents a function call.
    /// </summary>
    public partial class BoundFunctionCall : BoundExpression, IInvocationExpression
    {
        protected ImmutableArray<BoundArgument> _arguments;

        ImmutableArray<IArgument> IInvocationExpression.ArgumentsInParameterOrder => StaticCast<IArgument>.From(_arguments);

        ImmutableArray<IArgument> IInvocationExpression.ArgumentsInSourceOrder => StaticCast<IArgument>.From(_arguments);

        public ImmutableArray<BoundArgument> ArgumentsInSourceOrder => _arguments;

        public IArgument ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            foreach (var arg in _arguments)
                if (arg.Parameter == parameter)
                    return arg;

            return null;
        }

        public virtual IExpression Instance => null;

        public bool IsVirtual => false;

        public override OperationKind Kind => OperationKind.InvocationExpression;
        
        public IMethodSymbol TargetMethod { get; set; }

        public BoundFunctionCall(ImmutableArray<BoundArgument> arguments)
        {
            Debug.Assert(!arguments.IsDefault);
            _arguments = arguments;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitInvocationExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitInvocationExpression(this, argument);
    }

    #endregion

    #region BoundEcho

    public sealed partial class BoundEcho : BoundFunctionCall
    {
        public BoundEcho(ImmutableArray<BoundArgument> arguments)
            :base(arguments)
        {
        }
    }

    #endregion

    #region BoundLiteral

    public partial class BoundLiteral : BoundExpression, ILiteralExpression
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

    public sealed partial class BoundBinaryEx : BoundExpression, IBinaryOperatorExpression
    {
        public BinaryOperationKind BinaryOperationKind { get { throw new NotSupportedException(); } }

        public override OperationKind Kind => OperationKind.BinaryOperatorExpression;

        public Operations Operation { get; private set; }

        public BoundExpression Left { get; private set; }
        public BoundExpression Right { get; private set; }

        IExpression IBinaryOperatorExpression.Left => Left;

        public IMethodSymbol Operator { get; set; }

        IExpression IBinaryOperatorExpression.Right => Right;

        public bool UsesOperatorMethod => this.Operator != null;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitBinaryOperatorExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitBinaryOperatorExpression(this, argument);

        internal BoundBinaryEx(BoundExpression left, BoundExpression right, Operations op)
        {
            this.Left = left;
            this.Right = right;
            this.Operation = op;
        }
    }

    #endregion

    #region BoundUnaryEx

    public partial class BoundUnaryEx : BoundExpression, IUnaryOperatorExpression
    {
        public Operations Operation { get; private set; }

        public BoundExpression Operand { get; set; }

        public override OperationKind Kind => OperationKind.UnaryOperatorExpression;

        IExpression IUnaryOperatorExpression.Operand => Operand;

        public IMethodSymbol Operator => null;

        public bool UsesOperatorMethod => Operator != null;

        public UnaryOperationKind UnaryOperationKind { get { throw new NotSupportedException(); } }

        public BoundUnaryEx(BoundExpression operand, Operations op)
        {
            Contract.ThrowIfNull(operand);
            this.Operand = operand;
            this.Operation = op;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitUnaryOperatorExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitUnaryOperatorExpression(this, argument);
    }

    #endregion

    #region BoundIncDecEx

    public partial class BoundIncDecEx : BoundCompoundAssignEx, IIncrementExpression
    {
        public UnaryOperationKind IncrementKind { get; private set; }

        public override OperationKind Kind => OperationKind.IncrementExpression;

        public BoundIncDecEx(BoundReferenceExpression target, UnaryOperationKind kind)
            :base(target, new BoundLiteral(1L).WithAccess(AccessType.Read), Operations.IncDec)
        {
            Debug.Assert(
                kind == UnaryOperationKind.OperatorPostfixDecrement ||
                kind == UnaryOperationKind.OperatorPostfixIncrement ||
                kind == UnaryOperationKind.OperatorPrefixDecrement||
                kind == UnaryOperationKind.OperatorPrefixIncrement);

            this.IncrementKind = kind;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitIncrementExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitIncrementExpression(this, argument);
    }

    #endregion

    #region BoundAssignEx, BoundCompoundAssignEx

    public partial class BoundAssignEx : BoundExpression, IAssignmentExpression
    {
        #region IAssignmentExpression

        IReferenceExpression IAssignmentExpression.Target => Target;

        IExpression IAssignmentExpression.Value => Value;

        #endregion

        public override OperationKind Kind => OperationKind.AssignmentExpression;

        public BoundReferenceExpression Target { get; set; }

        public BoundExpression Value { get; set; }        

        public BoundAssignEx(BoundReferenceExpression target, BoundExpression value)
        {
            this.Target = target;
            this.Value = value;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitAssignmentExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitAssignmentExpression(this, argument);
    }

    public partial class BoundCompoundAssignEx : BoundAssignEx, ICompoundAssignmentExpression
    {
        public BinaryOperationKind BinaryKind { get { throw new NotSupportedException(); } }

        public override OperationKind Kind => OperationKind.CompoundAssignmentExpression;

        public IMethodSymbol Operator { get; set; }

        public bool UsesOperatorMethod => this.Operator != null;

        public Operations Operation { get; private set; }

        protected BoundCompoundAssignEx(BoundReferenceExpression target, BoundExpression value, Operations op)
            :base(target, value)
        {
            this.Operation = op;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitCompoundAssignmentExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitCompoundAssignmentExpression(this, argument);
    }

    #endregion

    #region BoundReferenceExpression

    public abstract partial class BoundReferenceExpression : BoundExpression, IReferenceExpression
    {
        
    }

    #endregion

    #region BoundVariableRef

    /// <summary>
    /// A variable reference that can be read or written to.
    /// </summary>
    public partial class BoundVariableRef : BoundReferenceExpression, ILocalReferenceExpression
    {
        readonly string _name;
        BoundVariable _variable;
        
        /// <summary>
        /// Name of the variable.
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// Resolved variable source.
        /// </summary>
        public BoundVariable Variable => _variable;

        public override OperationKind Kind => OperationKind.LocalReferenceExpression;

        /// <summary>
        /// Local in case of the variable is resolved local variable.
        /// </summary>
        ILocalSymbol ILocalReferenceExpression.Local => _variable?.Symbol as ILocalSymbol;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitLocalReferenceExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitLocalReferenceExpression(this, argument);

        public BoundVariableRef(string name)
        {
            _name = name;
        }
        
        public void Update(BoundVariable variable)
        {
            Debug.Assert(variable != null);
            _variable = variable;
        }
    }

    #endregion
}
