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
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax;
using Pchp.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CodeGen;

namespace Pchp.CodeAnalysis.Semantics
{
    #region BoundAccess, AccessMask

    [Flags]
    public enum AccessMask
    {
        /// <summary>
        /// Serves for case when Expression is body of a ExpressionStmt.
        /// It is useless to push its value on the stack in that case.
        /// </summary>
        None = 0,

        /// <summary>
        /// The result value will be read first.
        /// </summary>
        Read = 1,

        /// <summary>
        /// A value will be written to the place.
        /// Only available for VariableUse (variables, fields, properties, array items, references).
        /// </summary>
        Write = 2,

        /// <summary>
        /// The expression will be aliased and the alias will be read.
        /// </summary>
        ReadRef = 4 | Read,

        /// <summary>
        /// An aliased value will be written to the place.
        /// Only available for VariableUse (variables, fields, properties, array items, references).
        /// </summary>
        WriteRef = 8 | Write,

        /// <summary>
        /// The expression is accessed as a part of chain,
        /// its member field will be written to.
        /// E.g. (EnsureObject)->Field = Value
        /// </summary>
        EnsureObject = 16 | Read,

        /// <summary>
        /// The expression is accessed as a part of chain,
        /// its item entry will be written to.
        /// E.g. (EnsureArray)[] = Value
        /// </summary>
        EnsureArray = 32 | Read,

        /// <summary>
        /// Read is check only and won't result in an exception in case the variable does not exist.
        /// </summary>
        ReadQuiet = 64,

        /// <summary>
        /// The variable will be unset. Combined with <c>quiet</c> flag, valid for variables, array entries and fields.
        /// </summary>
        Unset = 128,

        // NOTE: WriteAndReadRef has to be constructed by semantic binder as bound expression with Write and another bound expression with ReadRef
        // NOTE: ReadAndWriteAndReadRef has to be constructed by semantic binder as bound expression with Read|Write and another bound expression with ReadRef
    }

    /// <summary>
    /// Expression access information.
    /// Describes the context in which an expression is used.
    /// </summary>
    [DebuggerDisplay("[{_flags}]")]
    public struct BoundAccess
    {
        /// <summary>
        /// 
        /// </summary>
        AccessMask _flags;

        ///// <summary>
        ///// Optional. Type the expression result will be converted to.
        ///// </summary>
        //TypeRefMask _readTypeMask;

        /// <summary>
        /// Optional. Type the expression will be converted to.
        /// </summary>
        TypeSymbol _targetType;

        /// <summary>
        /// Type information for the write access (right value of the assignment).
        /// In case of <see cref="EnsureArray"/>, the type represents the written element type.
        /// </summary>
        TypeRefMask _writeTypeMask;

        #region Properties

        /// <summary>
        /// In case the expression's value will be read.
        /// </summary>
        public bool IsRead => (_flags & AccessMask.Read) != 0;

        /// <summary>
        /// In case a value will be written to the variable.
        /// </summary>
        public bool IsWrite => (_flags & AccessMask.Write) != 0;

        /// <summary>
        /// In case a variable will be unset.
        /// </summary>
        public bool IsUnset => (_flags & AccessMask.Unset) != 0;

        /// <summary>
        /// Gets type of value to be written.
        /// </summary>
        public TypeRefMask WriteMask => _writeTypeMask;

        /// <summary>
        /// Optional. Type the expression will be converted to.
        /// </summary>
        internal TypeSymbol TargetType => _targetType;

        /// <summary>
        /// Gets inyternal access flags.
        /// </summary>
        public AccessMask Flags => _flags;

        /// <summary>
        /// The variable will be aliased and read.
        /// </summary>
        public bool IsReadRef => (_flags & AccessMask.ReadRef) == AccessMask.ReadRef;

        /// <summary>
        /// A reference will be written.
        /// </summary>
        public bool IsWriteRef => (_flags & AccessMask.WriteRef) == AccessMask.WriteRef;

        /// <summary>
        /// The expression won't be read or written to.
        /// </summary>
        public bool IsNone => (_flags == 0);

        /// <summary>
        /// The read is for check purposes only and won't result in a warning in case the variable does not exist.
        /// </summary>
        public bool IsQuiet => (_flags & AccessMask.ReadQuiet) != 0;

        /// <summary>
        /// In case we might change the variable content to array, object or an alias (we may need write access).
        /// </summary>
        public bool IsEnsure => (_flags & ~AccessMask.Read & (AccessMask.ReadRef | AccessMask.EnsureObject | AccessMask.EnsureArray)) != 0;

        /// <summary>
        /// In case an alias will be written to the variable.
        /// </summary>
        public bool WriteRef => (_flags & AccessMask.WriteRef) == AccessMask.WriteRef;

        /// <summary>
        /// In case the expression has to read as an object to allow writing its fields.
        /// In case of a variable, created object has to be written back.
        /// </summary>
        public bool EnsureObject => (_flags & AccessMask.EnsureObject) == AccessMask.EnsureObject;

        /// <summary>
        /// In case the expression has to read as an array to allow accessing its elements.
        /// In case of a variable, created array has to be written back.
        /// </summary>
        public bool EnsureArray => (_flags & AccessMask.EnsureArray) == AccessMask.EnsureArray;

        /// <summary>
        /// Gets AccesFlags to be used at runtime.
        /// </summary>
        public Core.Dynamic.AccessFlags AccessFlags
        {
            get
            {
                // TODO: use AccessMask instead
                Core.Dynamic.AccessFlags result = Core.Dynamic.AccessFlags.Default;

                if (EnsureObject) result |= Core.Dynamic.AccessFlags.EnsureObject;
                if (EnsureArray) result |= Core.Dynamic.AccessFlags.EnsureArray;
                if (IsReadRef) result |= Core.Dynamic.AccessFlags.EnsureAlias;
                if (IsQuiet) result |= Core.Dynamic.AccessFlags.CheckOnly;
                if (IsUnset) result |= Core.Dynamic.AccessFlags.Unset;

                return result;
            }
        }

        #endregion

        #region Construction

        private BoundAccess(AccessMask flags, TypeSymbol targetType, TypeRefMask writeTypeMask)
        {
            _flags = flags;
            _writeTypeMask = writeTypeMask;
            _targetType = targetType;

            Debug.Assert(EnsureArray ^ EnsureObject ^ IsReadRef || !IsEnsure);  // only single ensure is possible
        }

        public BoundAccess WithRead()
        {
            return new BoundAccess(_flags | AccessMask.Read, _targetType, _writeTypeMask);
        }

        public BoundAccess WithWrite(TypeRefMask writeTypeMask)
        {
            return new BoundAccess(_flags | AccessMask.Write, _targetType, _writeTypeMask | writeTypeMask);
        }

        public BoundAccess WithWriteRef(TypeRefMask writeTypeMask)
        {
            return new BoundAccess(_flags | AccessMask.WriteRef, _targetType, _writeTypeMask | writeTypeMask);
        }

        public BoundAccess WithReadRef()
        {
            return new BoundAccess(_flags | AccessMask.ReadRef, _targetType, _writeTypeMask);
        }

        internal BoundAccess WithRead(TypeSymbol target)
        {
            Contract.ThrowIfNull(target);
            return new BoundAccess(_flags | AccessMask.Read, target, _writeTypeMask);
        }

        public BoundAccess WithQuiet()
        {
            return new BoundAccess(_flags | AccessMask.ReadQuiet, _targetType, _writeTypeMask);
        }

        public BoundAccess WithEnsureObject()
        {
            return new BoundAccess(_flags | AccessMask.EnsureObject, _targetType, _writeTypeMask);
        }

        public BoundAccess WithEnsureArray()
        {
            return new BoundAccess(_flags | AccessMask.EnsureArray, _targetType, _writeTypeMask);
        }

        /// <summary>
        /// Simple read access.
        /// </summary>
        public static BoundAccess Read => new BoundAccess(AccessMask.Read, null, 0);

        /// <summary>
        /// Read as a reference access.
        /// </summary>
        public static BoundAccess ReadRef => new BoundAccess(AccessMask.Read | AccessMask.ReadRef, null, 0);

        /// <summary>
        /// Simple write access without bound write type mask.
        /// </summary>
        public static BoundAccess Write => new BoundAccess(AccessMask.Write, null, 0);

        /// <summary>
        /// Unset variable.
        /// </summary>
        public static BoundAccess Unset => new BoundAccess(AccessMask.Unset | AccessMask.ReadQuiet, null, 0);

        /// <summary>
        /// Expression won't be read or written to.
        /// </summary>
        public static BoundAccess None => new BoundAccess(AccessMask.None, null, 0);

        /// <summary>
        /// Read and write without bound write type mask
        /// </summary>
        public static BoundAccess ReadAndWrite => new BoundAccess(AccessMask.Read | AccessMask.Write, null, 0);

        #endregion
    }

    #endregion

    #region BoundExpression

    public abstract partial class BoundExpression : IExpression
    {
        public TypeRefMask TypeRefMask { get; set; } = default(TypeRefMask);

        public BoundAccess Access { get; internal set; }

        public virtual Optional<object> ConstantValue => default(Optional<object>);

        public virtual bool IsInvalid => false;

        public abstract OperationKind Kind { get; }

        ITypeSymbol IExpression.ResultType => ResultType;

        /// <summary>
        /// The expression result type.
        /// Can be <c>null</c> until emit.
        /// </summary>
        internal TypeSymbol ResultType { get; set; }

        public SyntaxNode Syntax => null;

        internal LangElement PhpSyntax { get; set; }

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    #endregion

    #region BoundFunctionCall, BoundArgument, BoundEcho, BoundConcatEx, BoundNewEx

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
            Contract.ThrowIfNull(value);
            Debug.Assert(value.Access.IsRead);  // we do not support OUT parameters in PHP I guess, just aliasing ~ IsReadRef

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
    public abstract partial class BoundRoutineCall : BoundExpression, IInvocationExpression
    {
        protected ImmutableArray<BoundArgument> _arguments;

        ImmutableArray<IArgument> IInvocationExpression.ArgumentsInParameterOrder => StaticCast<IArgument>.From(_arguments);

        ImmutableArray<IArgument> IInvocationExpression.ArgumentsInSourceOrder => StaticCast<IArgument>.From(_arguments);

        public ImmutableArray<BoundArgument> ArgumentsInSourceOrder => _arguments;

        public IArgument ArgumentMatchingParameter(IParameterSymbol parameter)
        {
            foreach (var arg in _arguments)
            {
                if (arg.Parameter == parameter)
                    return arg;
            }

            return null;
        }

        IExpression IInvocationExpression.Instance => Instance;

        IMethodSymbol IInvocationExpression.TargetMethod => (Overloads != null && Overloads.IsFinal && Overloads.Candidates.Length == 1) ? Overloads.Candidates[0] : null;

        /// <summary>
        /// <c>this</c> argument to be supplied to the method.
        /// </summary>
        public abstract BoundExpression Instance { get; }

        /// <summary>
        /// Resolved overloads to be called.
        /// </summary>
        internal OverloadsList Overloads { get; set; }

        public virtual bool IsVirtual => false;

        public override OperationKind Kind => OperationKind.InvocationExpression;

        public BoundRoutineCall(ImmutableArray<BoundArgument> arguments)
        {
            Debug.Assert(!arguments.IsDefault);
            _arguments = arguments;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitInvocationExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitInvocationExpression(this, argument);
    }

    /// <summary>
    /// Call to a global function.
    /// </summary>
    public partial class BoundFunctionCall : BoundRoutineCall
    {
        QualifiedName _qname;
        QualifiedName? _qnameAlt;

        /// <summary>
        /// Gets the function name.
        /// </summary>
        public QualifiedName Name => _qname;

        public QualifiedName? AlternativeName => _qnameAlt;

        public override BoundExpression Instance => null;

        public BoundFunctionCall(QualifiedName qname, QualifiedName? qnameAlt, ImmutableArray<BoundArgument> arguments)
            : base(arguments)
        {
            _qname = qname;
            _qnameAlt = qnameAlt;
        }
    }

    /// <summary>
    /// Instance call to a method.
    /// </summary>
    public partial class BoundInstanceMethodCall : BoundRoutineCall
    {
        Name _name;
        BoundExpression _instance;

        /// <summary>
        /// Gets the method name.
        /// </summary>
        public Name Name => _name;

        public override BoundExpression Instance => _instance;

        public BoundInstanceMethodCall(BoundExpression instance, Name name, ImmutableArray<BoundArgument> arguments)
            : base(arguments)
        {
            _instance = instance;
            _name = name;
        }
    }

    /// <summary>
    /// Static call to a method.
    /// </summary>
    public partial class BoundStMethodCall : BoundRoutineCall
    {
        Name _name;
        GenericQualifiedName _containingtype;

        /// <summary>
        /// Gets the method name.
        /// </summary>
        public Name Name => _name;

        /// <summary>
        /// Gets the containing type name.
        /// </summary>
        public GenericQualifiedName ContainingType => _containingtype;

        public override BoundExpression Instance => null;

        public BoundStMethodCall(GenericQualifiedName containingType, Name name, ImmutableArray<BoundArgument> arguments)
            : base(arguments)
        {
            _containingtype = containingType;
            _name = name;
        }
    }

    /// <summary>
    /// Specialized <c>echo</c> function call.
    /// To be replaced with <c>Context.Echo</c> once overload resolution is implemented.
    /// </summary>
    public sealed partial class BoundEcho : BoundRoutineCall
    {
        public override BoundExpression Instance => null;

        public BoundEcho(ImmutableArray<BoundArgument> arguments)
            : base(arguments)
        {
        }
    }

    /// <summary>
    /// Represents a string concatenation.
    /// </summary>
    public partial class BoundConcatEx : BoundRoutineCall
    {
        public override BoundExpression Instance => null;

        public BoundConcatEx(ImmutableArray<BoundArgument> arguments)
            : base(arguments)
        {
        }
    }

    /// <summary>
    /// Direct new expression with a constructor call.
    /// </summary>
    public partial class BoundNewEx : BoundRoutineCall
    {
        /// <summary>
        /// Instantiated class type name.
        /// </summary>
        public QualifiedName TypeName => _qname;
        readonly QualifiedName _qname;

        public override BoundExpression Instance => null;

        public BoundNewEx(QualifiedName qname, ImmutableArray<BoundArgument> arguments)
            : base(arguments)
        {
            _qname = qname;
        }
    }

    /// <summary>
    /// A script inclusion.
    /// </summary>
    public partial class BoundIncludeEx : BoundRoutineCall
    {
        public override BoundExpression Instance => null;

        /// <summary>
        /// Gets value indicating the target is resolved at compile time,
        /// so it will be called statically.
        /// </summary>
        public bool IsResolved => Target != null;

        /// <summary>
        /// In case the inclusion target is resolved, gets reference to the <c>Main</c> method of the included script.
        /// </summary>
        internal MethodSymbol Target
        {
            get
            {
                return (MethodSymbol)((IInvocationExpression)this).TargetMethod;
            }
            set
            {
                this.Overloads = (value != null)
                    ? new OverloadsList(WellKnownPchpNames.GlobalRoutineName, new[] { value }) { IsFinal = true }   // single final overload == TargetMethod
                    : null;
            }
        }

        /// <summary>
        /// Type of inclusion, <c>include</c>, <c>require</c>, <c>include_once</c>, <c>require_once</c>.
        /// </summary>
        public InclusionTypes InclusionType { get; private set; }

        public BoundIncludeEx(BoundExpression target, InclusionTypes type)
            : base(ImmutableArray.Create(new BoundArgument(target)))
        {
            Debug.Assert(target.Access.IsRead);

            this.InclusionType = type;
        }
    }

    /// <summary>
    /// <c>exit</c> construct.
    /// </summary>
    public sealed partial class BoundExitEx : BoundRoutineCall
    {
        public override BoundExpression Instance => null;

        public BoundExitEx(BoundExpression value = null)
            : base(value != null ? ImmutableArray.Create(new BoundArgument(value)) : ImmutableArray<BoundArgument>.Empty)
        {
            Debug.Assert(value.Access.IsRead);
        }
    }

    #endregion

    #region BoundLiteral

    public partial class BoundLiteral : BoundExpression, ILiteralExpression
    {
        Optional<object> _value;

        public string Spelling => this.ConstantValue.Value != null ? this.ConstantValue.Value.ToString() : "NULL";

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
            : base(target, new BoundLiteral(1L).WithAccess(BoundAccess.Read), Operations.IncDec)
        {
            Debug.Assert(
                kind == UnaryOperationKind.OperatorPostfixDecrement ||
                kind == UnaryOperationKind.OperatorPostfixIncrement ||
                kind == UnaryOperationKind.OperatorPrefixDecrement ||
                kind == UnaryOperationKind.OperatorPrefixIncrement);

            this.IncrementKind = kind;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitIncrementExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitIncrementExpression(this, argument);
    }

    #endregion

    #region BoundConditionalEx

    public partial class BoundConditionalEx : BoundExpression, IConditionalChoiceExpression
    {
        IExpression IConditionalChoiceExpression.Condition => Condition;
        IExpression IConditionalChoiceExpression.IfFalse => IfFalse;
        IExpression IConditionalChoiceExpression.IfTrue => IfTrue;

        public BoundExpression Condition { get; private set; }
        public BoundExpression IfFalse { get; private set; }
        public BoundExpression IfTrue { get; private set; }

        public override OperationKind Kind => OperationKind.ConditionalChoiceExpression;

        public BoundConditionalEx(BoundExpression condition, BoundExpression iftrue, BoundExpression iffalse)
        {
            Contract.ThrowIfNull(condition);
            // Contract.ThrowIfNull(iftrue); // iftrue allowed to be null, condition used instead (condition ?: iffalse)
            Contract.ThrowIfNull(iffalse);

            this.Condition = condition;
            this.IfTrue = iftrue;
            this.IfFalse = iffalse;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitConditionalChoiceExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.VisitConditionalChoiceExpression(this, argument);
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

        public BoundCompoundAssignEx(BoundReferenceExpression target, BoundExpression value, Operations op)
            : base(target, value)
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
        /// <summary>
        /// Name of the variable.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Resolved variable source.
        /// </summary>
        public BoundVariable Variable { get; set; }

        public override OperationKind Kind => OperationKind.LocalReferenceExpression;

        /// <summary>
        /// Local in case of the variable is resolved local variable.
        /// </summary>
        ILocalSymbol ILocalReferenceExpression.Local => this.Variable?.Symbol as ILocalSymbol;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitLocalReferenceExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitLocalReferenceExpression(this, argument);

        public BoundVariableRef(string name)
        {
            this.Name = name;
        }
    }

    #endregion

    #region BoundListEx

    /// <summary>
    /// PHP <c>list</c> expression that can be written to.
    /// </summary>
    public partial class BoundListEx : BoundReferenceExpression
    {
        public override OperationKind Kind => OperationKind.None;

        public BoundListEx()
        {
            throw new NotImplementedException();
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);
    }

    #endregion

    #region BoundFieldRef

    public partial class BoundFieldRef : BoundReferenceExpression, IFieldReferenceExpression
    {
        ISymbol IMemberReferenceExpression.Member => Field;

        IFieldSymbol IFieldReferenceExpression.Field => Field;

        IExpression IMemberReferenceExpression.Instance => Instance;

        public BoundExpression Instance { get; set; }

        internal FieldSymbol Field { get; set; }

        public VariableName Name { get; private set; }

        public override OperationKind Kind => OperationKind.FieldReferenceExpression;

        public BoundFieldRef(VariableName name, BoundExpression instance)
        {
            this.Name = name;
            this.Instance = instance;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitFieldReferenceExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitFieldReferenceExpression(this, argument);
    }

    #endregion

    #region BoundArrayEx

    public partial class BoundArrayEx : BoundExpression, IArrayCreationExpression
    {
        public class BoundArrayInitializer : BoundExpression, IArrayInitializer
        {
            readonly BoundArrayEx _array;

            public override OperationKind Kind => OperationKind.ArrayInitializer;

            ImmutableArray<IExpression> IArrayInitializer.ElementValues => _array._items.Select(x => x.Value).Cast<IExpression>().AsImmutable();

            public BoundArrayInitializer(BoundArrayEx array)
            {
                _array = array;
            }

            public override void Accept(OperationVisitor visitor)
                => visitor.VisitArrayInitializer(this);

            public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
                => visitor.VisitArrayInitializer(this, argument);
        }

        public override OperationKind Kind => OperationKind.ArrayCreationExpression;

        ITypeSymbol IArrayCreationExpression.ElementType
        {
            get
            {
                // TODO: PhpValue
                throw new NotImplementedException();
            }
        }

        ImmutableArray<IExpression> IArrayCreationExpression.DimensionSizes => ImmutableArray.Create<IExpression>(new BoundLiteral(_items.Length));

        IArrayInitializer IArrayCreationExpression.Initializer => new BoundArrayInitializer(this);

        /// <summary>
        /// Array items.
        /// </summary>
        public ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>> Items => _items;
        ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>> _items;

        public BoundArrayEx(IEnumerable<KeyValuePair<BoundExpression, BoundExpression>> items)
        {
            _items = items.ToImmutableArray();
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitArrayCreationExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitArrayCreationExpression(this, argument);
    }

    #endregion

    #region BoundArrayItemEx

    /// <summary>
    /// Array item access.
    /// </summary>
    public partial class BoundArrayItemEx : BoundReferenceExpression, IArrayElementReferenceExpression
    {
        public BoundExpression Array
        {
            get { return _array; }
            set { _array = value; }
        }
        BoundExpression _array;

        public BoundExpression Index
        {
            get { return _index; }
            set { _index = value; }
        }
        BoundExpression _index;

        public override OperationKind Kind => OperationKind.ArrayElementReferenceExpression;

        IExpression IArrayElementReferenceExpression.ArrayReference => _array;

        ImmutableArray<IExpression> IArrayElementReferenceExpression.Indices
            => (_index != null) ? ImmutableArray.Create((IExpression)_index) : ImmutableArray<IExpression>.Empty;

        public BoundArrayItemEx(BoundExpression array, BoundExpression index)
        {
            Contract.ThrowIfNull(array);

            _array = array;
            _index = index;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitArrayElementReferenceExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitArrayElementReferenceExpression(this, argument);
    }

    #endregion

    #region BoundInstanceOfEx
    
    public partial class BoundInstanceOfEx : BoundExpression, IIsExpression
    {
        #region IIsExpression

        IExpression IIsExpression.Operand => Operand;

        ITypeSymbol IIsExpression.IsType => IsTypeResolved;

        #endregion
        
        /// <summary>
        /// The value to be checked.
        /// </summary>
        public BoundExpression Operand { get; private set; }

        /// <summary>
        /// The type to check operand against.
        /// </summary>
        public QualifiedName IsTypeDirect { get; set; }

        /// <summary>
        /// The type to check operand against.
        /// </summary>
        public BoundExpression IsTypeIndirect { get; set; }

        /// <summary>
        /// <see cref="IsType"/> bound to a type symbol if possible.
        /// </summary>
        internal TypeSymbol IsTypeResolved { get; set; }

        public BoundInstanceOfEx(BoundExpression operand)
        {
            Contract.ThrowIfNull(operand);
            
            this.Operand = operand;
        }

        public override OperationKind Kind => OperationKind.IsExpression;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitIsExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitIsExpression(this, argument);
    }

    #endregion

    #region BoundGlobalConst

    public partial class BoundGlobalConst : BoundExpression
    {
        public override OperationKind Kind => OperationKind.None;

        /// <summary>
        /// Constant name.
        /// </summary>
        public string Name { get; private set; }

        public override Optional<object> ConstantValue => _boundValue;

        Optional<object> _boundValue = default(Optional<object>);

        internal void SetConstantValue(object value)
        {
            _boundValue = new Optional<object>(value);
        }

        public BoundGlobalConst(string name)
        {
            this.Name = name;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);
    }

    #endregion

    #region BoundPseudoConst

    public partial class BoundPseudoConst : BoundExpression
    {
        public override OperationKind Kind => OperationKind.None;

        public readonly PseudoConstUse.Types Type;

        public BoundPseudoConst(PseudoConstUse.Types type)
        {
            this.Type = type;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);
    }

    #endregion

    #region BoundIsSetEx, BoundUnsetEx

    public partial class BoundIsSetEx : BoundExpression
    {
        public override OperationKind Kind => OperationKind.None;

        /// <summary>
        /// Reference to be checked if it is set.
        /// </summary>
        public ImmutableArray<BoundReferenceExpression> VarReferences { get; set; }

        public BoundIsSetEx(ImmutableArray<BoundReferenceExpression> vars)
        {
            this.VarReferences = vars;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);
    }

    public partial class BoundUnset : BoundStatement
    {
        public override OperationKind Kind => OperationKind.None;

        /// <summary>
        /// Reference to be unset.
        /// </summary>
        public ImmutableArray<BoundReferenceExpression> VarReferences { get; set; }

        public BoundUnset(ImmutableArray<BoundReferenceExpression> vars)
        {
            this.VarReferences = vars;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);
    }

    #endregion
}
