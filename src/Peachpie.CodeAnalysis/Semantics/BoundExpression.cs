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
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CodeGen;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

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
        /// The expression will be read by value and copied.
        /// </summary>
        ReadCopy = 8 | Read,

        /// <summary>
        /// An aliased value will be written to the place.
        /// Only available for VariableUse (variables, fields, properties, array items, references).
        /// </summary>
        WriteRef = 16 | Write,

        /// <summary>
        /// The expression is accessed as a part of chain,
        /// its member field will be written to.
        /// E.g. (EnsureObject)->Field = Value
        /// </summary>
        EnsureObject = 32 | Read,

        /// <summary>
        /// The expression is accessed as a part of chain,
        /// its item entry will be written to.
        /// E.g. (EnsureArray)[] = Value
        /// </summary>
        EnsureArray = 64 | Read,

        /// <summary>
        /// Read is check only and won't result in an exception in case the variable does not exist.
        /// </summary>
        ReadQuiet = 128,

        /// <summary>
        /// The variable will be unset. Combined with <c>quiet</c> flag, valid for variables, array entries and fields.
        /// </summary>
        Unset = 256,

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
        /// The variable will be read by value and copied.
        /// </summary>
        public bool IsReadCopy => (_flags & AccessMask.ReadCopy) == AccessMask.ReadCopy;

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
        /// Gets value indicating the variable might be changed in context of the access.
        /// </summary>
        public bool MightChange => IsWrite || IsUnset || IsEnsure;

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
        /// In case variable will be accessed as array in manner of setting its entries.
        /// <code>VARIABLE[] = ...</code>
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
                // TODO: if (IsReadCopy) result |= Core.Dynamic.AccessFlags.DeepCopy;
                if (IsQuiet) result |= Core.Dynamic.AccessFlags.CheckOnly;
                if (IsUnset) result |= Core.Dynamic.AccessFlags.Unset;
                if (IsWriteRef) result |= Core.Dynamic.AccessFlags.WriteAlias;
                else if (IsWrite) result |= Core.Dynamic.AccessFlags.WriteValue;
                

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

        public BoundAccess WithReadCopy()
        {
            return new BoundAccess(_flags | AccessMask.ReadCopy, _targetType, _writeTypeMask);
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

    public abstract partial class BoundExpression : IPhpExpression
    {
        public TypeRefMask TypeRefMask { get; set; } = default(TypeRefMask);

        public BoundAccess Access { get; internal set; }

        public virtual bool IsInvalid => false;

        public abstract OperationKind Kind { get; }

        ITypeSymbol IExpression.ResultType => ResultType;

        /// <summary>
        /// The expression result type.
        /// Can be <c>null</c> until emit.
        /// </summary>
        internal TypeSymbol ResultType { get; set; }

        public SyntaxNode Syntax => null;

        public LangElement PhpSyntax { get; set; }

        /// <summary>
        /// Whether the expression needs current <c>Pchp.Core.Context</c> to be evaluated.
        /// Otherwise, the expression can be evaluated in app context or in compile time.
        /// </summary>
        public virtual bool RequiresContext => true;

        /// <summary>
        /// Resolved value of the expression.
        /// </summary>
        public virtual Optional<object> ConstantValue { get; set; }

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

        public abstract void Accept(PhpOperationVisitor visitor);
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

        public void Accept(PhpOperationVisitor visitor) => visitor.VisitArgument(this);

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

        IMethodSymbol IInvocationExpression.TargetMethod => TargetMethod;

        /// <summary>
        /// <c>this</c> argument to be supplied to the method.
        /// </summary>
        public abstract BoundExpression Instance { get; }

        /// <summary>
        /// Resolved method if possible.
        /// </summary>
        internal MethodSymbol TargetMethod { get; set; }

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
    /// Direct or indirect routine name.
    /// </summary>
    [DebuggerDisplay("{DebugView,nq}")]
    public partial class BoundRoutineName
    {
        public QualifiedName NameValue => _nameValue;
        readonly QualifiedName _nameValue;

        public BoundExpression NameExpression => _nameExpression;
        readonly BoundExpression _nameExpression;

        string DebugView
        {
            get
            {
                return IsDirect ? _nameValue.ToString() : _nameExpression.ToString();
            }
        }

        public bool IsDirect => _nameExpression == null;

        public BoundRoutineName(QualifiedName name)
        {
            _nameValue = name;
            _nameExpression = null;
        }

        public BoundRoutineName(BoundExpression nameExpr)
        {
            Debug.Assert(nameExpr != null);
            _nameExpression = nameExpr;
        }
    }

    public partial class BoundGlobalFunctionCall : BoundRoutineCall
    {
        public override BoundExpression Instance => null;

        public BoundRoutineName Name => _name;
        readonly BoundRoutineName _name;

        public QualifiedName? NameOpt => _nameOpt;
        readonly QualifiedName? _nameOpt;

        public BoundGlobalFunctionCall(BoundExpression nameExpression, ImmutableArray<BoundArgument> arguments) : base(arguments)
        {
            _name = new BoundRoutineName(nameExpression);
        }

        public BoundGlobalFunctionCall(QualifiedName name, QualifiedName? nameOpt, ImmutableArray<BoundArgument> arguments) : base(arguments)
        {
            _name = new BoundRoutineName(name);
            _nameOpt = nameOpt;
        }

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitGlobalFunctionCall(this);
    }

    public partial class BoundInstanceFunctionCall : BoundRoutineCall
    {
        public override BoundExpression Instance => _instance;
        readonly BoundExpression _instance;

        public BoundRoutineName Name => _name;
        readonly BoundRoutineName _name;

        public override bool IsVirtual => this.TargetMethod.IsErrorMethod() || this.TargetMethod.IsVirtual;

        public BoundInstanceFunctionCall(BoundExpression instance, QualifiedName name, ImmutableArray<BoundArgument> arguments)
            : this(instance, new BoundRoutineName(name), arguments)
        {

        }

        public BoundInstanceFunctionCall(BoundExpression instance, BoundExpression nameExpr, ImmutableArray<BoundArgument> arguments)
            : this(instance, new BoundRoutineName(nameExpr), arguments)
        {

        }

        public BoundInstanceFunctionCall(BoundExpression instance, BoundRoutineName name, ImmutableArray<BoundArgument> arguments)
            : base(arguments)
        {
            Debug.Assert(instance != null);
            Debug.Assert(name != null);

            _instance = instance;
            _name = name;
        }

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitInstanceFunctionCall(this);
    }

    public partial class BoundStaticFunctionCall : BoundRoutineCall
    {
        public BoundTypeRef TypeRef => _typeRef;
        readonly BoundTypeRef _typeRef;

        public override BoundExpression Instance => null;

        public BoundRoutineName Name => _name;
        readonly BoundRoutineName _name;

        public BoundStaticFunctionCall(BoundTypeRef typeRef, BoundRoutineName name, ImmutableArray<BoundArgument> arguments) : base(arguments)
        {
            _typeRef = typeRef;
            _name = name;
        }

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitStaticFunctionCall(this);
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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitEcho(this);
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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitConcat(this);
    }

    /// <summary>
    /// Direct new expression with a constructor call.
    /// </summary>
    public partial class BoundNewEx : BoundRoutineCall
    {
        /// <summary>
        /// Instantiated class type name.
        /// </summary>
        public BoundTypeRef TypeRef => _typeref;
        readonly BoundTypeRef _typeref;

        public override BoundExpression Instance => null;

        public BoundNewEx(BoundTypeRef tref, ImmutableArray<BoundArgument> arguments)
            : base(arguments)
        {
            _typeref = tref;
        }

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitNew(this);
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
        public bool IsResolved => !Target.IsErrorMethod();

        /// <summary>
        /// In case the inclusion target is resolved, gets reference to the <c>Main</c> method of the included script.
        /// </summary>
        internal MethodSymbol Target
        {
            get
            {
                return TargetMethod;
            }
            set
            {
                TargetMethod = value;
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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitInclude(this);
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
            Debug.Assert(value == null || value.Access.IsRead);
        }

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitExit(this);
    }

    #endregion

    #region BoundLambda

    /// <summary>
    /// Anonymous function expression.
    /// </summary>
    public partial class BoundLambda : BoundExpression, ILambdaExpression
    {
        /// <summary>
        /// Declared use variables.
        /// </summary>
        public ImmutableArray<BoundArgument> UseVars => _usevars;
        ImmutableArray<BoundArgument> _usevars;

        public IBlockStatement Body => (BoundLambdaMethod != null) ? BoundLambdaMethod.ControlFlowGraph.Start : null;

        public IMethodSymbol Signature => BoundLambdaMethod;

        /// <summary>
        /// Reference to associated lambda method symbol.
        /// Bound during analysis.
        /// </summary>
        internal SourceLambdaSymbol BoundLambdaMethod { get; set; }

        public BoundLambda(ImmutableArray<BoundArgument> usevars)
        {
            _usevars = usevars;
        }

        public override OperationKind Kind => OperationKind.LambdaExpression;
        
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitLambda(this);

        public override void Accept(OperationVisitor visitor) => visitor.VisitLambdaExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.VisitLambdaExpression(this, argument);
    }

    #endregion

    #region BoundEvalEx

    public partial class BoundEvalEx : BoundExpression
    {
        public override OperationKind Kind => OperationKind.None;

        public BoundExpression CodeExpression { get; private set; }

        public BoundEvalEx(BoundExpression code)
        {
            Debug.Assert(code != null);
            this.CodeExpression = code;
        }

        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitEval(this);

        public override void Accept(OperationVisitor visitor) => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);
    }

    #endregion

    #region BoundLiteral

    public partial class BoundLiteral : BoundExpression, ILiteralExpression
    {
        public string Spelling => this.ConstantValue.Value?.ToString() ?? "NULL";

        public override OperationKind Kind => OperationKind.LiteralExpression;

        public override bool RequiresContext => false;

        public BoundLiteral(object value)
        {
            this.ConstantValue = value;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitLiteralExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitLiteralExpression(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitLiteral(this);
    }

    #endregion

    #region BoundBinaryEx

    public sealed partial class BoundBinaryEx : BoundExpression, IBinaryOperatorExpression
    {
        public BinaryOperationKind BinaryOperationKind { get { throw new NotSupportedException(); } }

        public override OperationKind Kind => OperationKind.BinaryOperatorExpression;

        public override bool RequiresContext => Left.RequiresContext || Right.RequiresContext;

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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitBinaryExpression(this);
    }

    #endregion

    #region BoundUnaryEx

    public partial class BoundUnaryEx : BoundExpression, IUnaryOperatorExpression
    {
        public Operations Operation { get; private set; }

        public BoundExpression Operand { get; set; }

        public override OperationKind Kind => OperationKind.UnaryOperatorExpression;

        public override bool RequiresContext => Operation == Operations.StringCast || Operation == Operations.Print || Operand.RequiresContext;

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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitUnaryExpression(this);
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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitIncDec(this);
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

        public override bool RequiresContext => Condition.RequiresContext || IfTrue.RequiresContext || IfFalse.RequiresContext;

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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitConditional(this);
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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitAssign(this);
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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitCompoundAssign(this);
    }

    #endregion

    #region BoundReferenceExpression

    public abstract partial class BoundReferenceExpression : BoundExpression, IReferenceExpression
    {
        /// <summary>
        /// Gets or sets value indicating the variable is used while it was not initialized in all code paths.
        /// </summary>
        public bool MaybeUninitialized { get; set; }
    }

    #endregion

    #region BoundVariableRef

    /// <summary>
    /// Direct or indirect variable name.
    /// </summary>
    [DebuggerDisplay("{DebugView,nq}")]
    public partial class BoundVariableName
    {
        public VariableName NameValue => _nameValue;
        readonly VariableName _nameValue;

        public BoundExpression NameExpression => _nameExpression;
        readonly BoundExpression _nameExpression;

        string DebugView
        {
            get
            {
                return IsDirect ? _nameValue.ToString() : _nameExpression.ToString();
            }
        }

        public bool IsDirect => _nameExpression == null;

        public BoundVariableName(VariableName name)
        {
            _nameValue = name;
            _nameExpression = null;
        }

        public BoundVariableName(BoundExpression nameExpr)
        {
            Debug.Assert(nameExpr != null);
            _nameExpression = nameExpr;
        }
    }

    /// <summary>
    /// A variable reference that can be read or written to.
    /// </summary>
    public partial class BoundVariableRef : BoundReferenceExpression, ILocalReferenceExpression
    {
        readonly BoundVariableName _name;

        /// <summary>
        /// Name of the variable.
        /// </summary>
        public BoundVariableName Name => _name;

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

        public BoundVariableRef(BoundVariableName name)
        {
            Debug.Assert(name != null);
            _name = name;
        }

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitVariableRef(this);
    }

    #endregion

    #region BoundListEx

    /// <summary>
    /// PHP <c>list</c> expression that can be written to.
    /// </summary>
    public partial class BoundListEx : BoundReferenceExpression
    {
        public override OperationKind Kind => OperationKind.None;

        /// <summary>
        /// Bound target variables.
        /// </summary>
        public BoundReferenceExpression[] Variables => _vars;
        readonly BoundReferenceExpression[] _vars;

        public BoundListEx(BoundReferenceExpression[] vars)
        {
            Debug.Assert(vars != null);
            Debug.Assert(vars.Length != 0);
            _vars = vars;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitList(this);
    }

    #endregion

    #region BoundFieldRef

    public partial class BoundFieldRef : BoundReferenceExpression, IFieldReferenceExpression
    {
        ISymbol IMemberReferenceExpression.Member => FieldSymbolOpt;

        IFieldSymbol IFieldReferenceExpression.Field => FieldSymbolOpt;

        IExpression IMemberReferenceExpression.Instance => Instance;

        enum FieldType
        {
            InstanceField,
            StaticField,
            ClassConstant,
        }

        FieldType _type;

        BoundExpression _parentExpr;    // in case of instance field
        BoundTypeRef _parentType;       // in case of class constant or static field
        BoundVariableName _fieldName;   // field name

        public bool IsInstanceField => _type == FieldType.InstanceField;
        public bool IsStaticField => _type == FieldType.StaticField;
        public bool IsClassConstant => _type == FieldType.ClassConstant;

        /// <summary>
        /// In case of a non static field, gets its instance expression.
        /// </summary>
        public BoundExpression Instance => IsInstanceField ? _parentExpr : null;

        public BoundTypeRef ParentType => _parentType;

        public BoundVariableName FieldName => _fieldName;

        public override OperationKind Kind => OperationKind.FieldReferenceExpression;

        private BoundFieldRef()
        {
        }

        public static BoundFieldRef CreateInstanceField(BoundExpression instance, BoundVariableName name) => new BoundFieldRef() { _parentExpr = instance, _fieldName = name, _type = FieldType.InstanceField };
        public static BoundFieldRef CreateStaticField(BoundTypeRef parent, BoundVariableName name) => new BoundFieldRef() { _parentType = parent, _fieldName = name, _type = FieldType.StaticField };
        public static BoundFieldRef CreateClassConst(BoundTypeRef parent, BoundVariableName name) => new BoundFieldRef() { _parentType = parent, _fieldName = name, _type = FieldType.ClassConstant };


        public override void Accept(OperationVisitor visitor)
            => visitor.VisitFieldReferenceExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitFieldReferenceExpression(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitFieldRef(this);
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

            public override void Accept(PhpOperationVisitor visitor)
            {
                throw new NotImplementedException();
            }
        }

        public override OperationKind Kind => OperationKind.ArrayCreationExpression;

        public override bool RequiresContext => _items.Any(x => (x.Key != null && x.Key.RequiresContext) || x.Value.RequiresContext);

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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitArray(this);
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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitArrayItem(this);
    }

    #endregion

    #region BoundInstanceOfEx

    public partial class BoundInstanceOfEx : BoundExpression, IIsExpression
    {
        #region IIsExpression

        IExpression IIsExpression.Operand => Operand;

        ITypeSymbol IIsExpression.IsType => AsType?.ResolvedType;

        #endregion

        /// <summary>
        /// The value to be checked.
        /// </summary>
        public BoundExpression Operand { get; private set; }

        /// <summary>
        /// The type.
        /// </summary>
        public BoundTypeRef AsType { get; private set; }

        public BoundInstanceOfEx(BoundExpression operand, BoundTypeRef tref)
        {
            Contract.ThrowIfNull(operand);

            this.Operand = operand;
            this.AsType = tref;
        }

        public override OperationKind Kind => OperationKind.IsExpression;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitIsExpression(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitIsExpression(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitInstanceOf(this);
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

        public BoundGlobalConst(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// In case the constant is resolved to a place.
        /// </summary>
        internal IBoundReference _boundExpressionOpt;

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitGlobalConstUse(this);
    }

    #endregion

    #region BoundPseudoConst

    public partial class BoundPseudoConst : BoundExpression
    {
        public override OperationKind Kind => OperationKind.None;

        public PseudoConstUse.Types Type { get; private set; }

        public BoundPseudoConst(PseudoConstUse.Types type)
        {
            this.Type = type;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitPseudoConstUse(this);
    }

    #endregion

    #region BoundPseudoClassConst

    public partial class BoundPseudoClassConst : BoundExpression
    {
        public PseudoClassConstUse.Types Type { get; private set; }

        public override OperationKind Kind => OperationKind.None;

        public BoundTypeRef TargetType { get; private set; }

        public BoundPseudoClassConst(BoundTypeRef targetType, PseudoClassConstUse.Types type)
        {
            this.TargetType = targetType;
            this.Type = type;
        }

        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitPseudoClassConstUse(this);

        public override void Accept(OperationVisitor visitor) => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);
    }

    #endregion

    #region BoundIsSetEx, BoundUnsetEx, BoundIsEmptyEx

    public partial class BoundIsEmptyEx : BoundExpression
    {
        public override OperationKind Kind => OperationKind.None;

        /// <summary>
        /// Reference to be checked if it is set.
        /// </summary>
        public BoundExpression Operand { get; set; }

        public BoundIsEmptyEx(BoundExpression expression)
        {
            this.Operand = expression;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitIsEmpty(this);
    }

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

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor "/> instance. Cannot be <c>null</c>.</param>
        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitIsSet(this);
    }

    #endregion
}
