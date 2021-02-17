using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using Ast = Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis.Text;
using Devsense.PHP.Syntax;
using Pchp.CodeAnalysis.Symbols;
using Peachpie.CodeAnalysis.Utilities;
using Devsense.PHP.Syntax.Ast;
using Pchp.CodeAnalysis.Semantics.Graph;

namespace Pchp.CodeAnalysis.Semantics
{
    /// <summary>
    /// Base class representing a statement semantic.
    /// </summary>
    public abstract partial class BoundStatement : BoundOperation, IPhpStatement
    {
        public virtual bool IsInvalid => false;

        public Ast.LangElement PhpSyntax { get; set; }

        public abstract TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor);
    }

    public sealed partial class BoundEmptyStatement : BoundStatement, IEmptyOperation
    {
        public override OperationKind Kind => OperationKind.Empty;

        /// <summary>
        /// Explicit text span used to generate sequence point.
        /// </summary>
        readonly TextSpan _span;

        public BoundEmptyStatement(TextSpan span = default(TextSpan))
        {
            _span = span;
        }

        public BoundEmptyStatement Update(TextSpan span)
        {
            if (span == _span)
            {
                return this;
            }
            else
            {
                return new BoundEmptyStatement(span);
            }
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitEmpty(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitEmpty(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitEmptyStatement(this);
    }

    /// <summary>
    /// Represents an expression statement.
    /// </summary>
    public sealed partial class BoundExpressionStatement : BoundStatement, IExpressionStatementOperation
    {
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        IOperation IExpressionStatementOperation.Operation => Expression;

        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public BoundExpression Expression { get; internal set; }

        public override OperationKind Kind => OperationKind.ExpressionStatement;

        public BoundExpressionStatement(BoundExpression/*!*/expression)
        {
            Debug.Assert(expression != null);
            this.Expression = expression;
        }

        public BoundExpressionStatement Update(BoundExpression expression)
        {
            if (expression == Expression)
            {
                return this;
            }
            else
            {
                return new BoundExpressionStatement(expression);
            }
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitExpressionStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitExpressionStatement(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitExpressionStatement(this);
    }

    /// <summary>
    /// return <c>optional</c>;
    /// </summary>
    public sealed partial class BoundReturnStatement : BoundStatement, IReturnOperation
    {
        public override OperationKind Kind => OperationKind.Return;

        IOperation IReturnOperation.ReturnedValue => Returned;

        public BoundExpression Returned { get; internal set; }

        public BoundReturnStatement(BoundExpression returned)
        {
            this.Returned = returned;
        }

        public BoundReturnStatement Update(BoundExpression returned)
        {
            if (returned == Returned)
            {
                return this;
            }
            else
            {
                return new BoundReturnStatement(returned);
            }
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitReturn(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitReturn(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitReturn(this);
    }

    /// <summary>
    /// Conditionally declared functions.
    /// </summary>
    public sealed partial class BoundFunctionDeclStatement : BoundStatement, IInvalidOperation
    {
        public override OperationKind Kind => OperationKind.LocalFunction;

        internal Ast.FunctionDecl FunctionDecl => (Ast.FunctionDecl)PhpSyntax;

        internal Symbols.SourceFunctionSymbol Function => _function;
        readonly Symbols.SourceFunctionSymbol _function;

        internal BoundFunctionDeclStatement(Symbols.SourceFunctionSymbol function)
        {
            Contract.ThrowIfNull(function);

            _function = function;
            this.PhpSyntax = (Ast.FunctionDecl)function.Syntax;
        }

        internal BoundFunctionDeclStatement Update(SourceFunctionSymbol function)
        {
            if (function == _function)
            {
                return this;
            }
            else
            {
                return new BoundFunctionDeclStatement(function);
            }
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitInvalid(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitInvalid(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitFunctionDeclaration(this);
    }

    /// <summary>
    /// Conditionally declared class.
    /// </summary>
    public sealed partial class BoundTypeDeclStatement : BoundStatement, IInvalidOperation
    {
        public override OperationKind Kind => OperationKind.LocalFunction;

        internal Ast.TypeDecl TypeDecl => (Ast.TypeDecl)PhpSyntax;

        internal Symbols.SourceTypeSymbol DeclaredType => _type;
        readonly Symbols.SourceTypeSymbol _type;

        internal BoundTypeDeclStatement(Symbols.SourceTypeSymbol type)
        {
            Contract.ThrowIfNull(type);


            _type = type;
            this.PhpSyntax = type.Syntax;

            type.PostponedDeclaration();
        }

        internal BoundTypeDeclStatement Update(SourceTypeSymbol type)
        {
            if (type == _type)
            {
                return this;
            }
            else
            {
                return new BoundTypeDeclStatement(type);
            }
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitInvalid(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitInvalid(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitTypeDeclaration(this);
    }

    public sealed partial class BoundGlobalVariableStatement : BoundStatement, IVariableDeclarationOperation
    {
        public override OperationKind Kind => OperationKind.VariableDeclaration;

        ImmutableArray<IVariableDeclaratorOperation> IVariableDeclarationOperation.Declarators => (Variable.Variable != null)
            ? ImmutableArray.Create((IVariableDeclaratorOperation)Variable.Variable)
            : ImmutableArray<IVariableDeclaratorOperation>.Empty;  // unbound yet

        IVariableInitializerOperation IVariableDeclarationOperation.Initializer => null;

        /// <summary>
        /// The variable that will be referenced to a global variable.
        /// </summary>
        public BoundVariableRef Variable { get; internal set; }

        ImmutableArray<IOperation> IVariableDeclarationOperation.IgnoredDimensions => ImmutableArray<IOperation>.Empty;

        public BoundGlobalVariableStatement(BoundVariableRef variable)
        {
            Variable = variable;
        }

        public BoundGlobalVariableStatement Update(BoundVariableRef variable)
        {
            if (variable == Variable)
            {
                return this;
            }
            else
            {
                return new BoundGlobalVariableStatement(variable);
            }
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitVariableDeclaration(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitVariableDeclaration(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitGlobalStatement(this);
    }

    public sealed partial class BoundGlobalConstDeclStatement : BoundStatement
    {
        public override OperationKind Kind => OperationKind.VariableDeclaration;

        public QualifiedName Name { get; private set; }
        public BoundExpression Value { get; internal set; }

        public BoundGlobalConstDeclStatement(QualifiedName name, BoundExpression value)
        {
            Debug.Assert(value.Access.IsRead);

            this.Name = name;
            this.Value = value;
        }

        public BoundGlobalConstDeclStatement Update(QualifiedName name, BoundExpression value)
        {
            if (name == Name && value == Value)
            {
                return this;
            }
            else
            {
                return new BoundGlobalConstDeclStatement(name, value);
            }
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitGlobalConstDecl(this);
    }

    public sealed partial class BoundStaticVariableStatement : BoundStatement, IVariableDeclarationOperation
    {
        internal struct StaticVarDecl : IEquatable<StaticVarDecl>
        {
            public IVariableReference Variable;
            public BoundExpression InitialValue;

            /// <summary>
            /// Variable name.
            /// </summary>
            public string Name => ((LocalVariableReference)Variable).Name;

            public bool Equals(StaticVarDecl other) =>
                Variable == other.Variable &&
                InitialValue == other.InitialValue;

            public override bool Equals(object obj) => obj is StaticVarDecl v && Equals(v);

            public override int GetHashCode() => Variable != null ? Variable.GetHashCode() : -1;

            public static bool operator ==(StaticVarDecl a, StaticVarDecl b) => a.Equals(b);

            public static bool operator !=(StaticVarDecl a, StaticVarDecl b) => !a.Equals(b);
        }

        public override OperationKind Kind => OperationKind.VariableDeclaration;

        ImmutableArray<IVariableDeclaratorOperation> IVariableDeclarationOperation.Declarators =>
            ImmutableArray.Create((IVariableDeclaratorOperation)_variable.Variable);

        IVariableInitializerOperation IVariableDeclarationOperation.Initializer => null;

        ImmutableArray<IOperation> IVariableDeclarationOperation.IgnoredDimensions => ImmutableArray<IOperation>.Empty;

        internal StaticVarDecl Declaration => _variable;
        readonly StaticVarDecl _variable;

        /// <summary>
        /// Synthesized type containing <c>value</c> field with the actual value.
        /// Cannot be <c>null</c>.
        /// </summary>
        internal NamedTypeSymbol HolderClass => _holderClass;
        readonly SynthesizedStaticLocHolder _holderClass;

        internal BoundStaticVariableStatement(StaticVarDecl variable, SynthesizedStaticLocHolder holder)
        {
            _variable = variable;
            _holderClass = holder ?? throw ExceptionUtilities.ArgumentNull(nameof(holder));
        }

        internal BoundStaticVariableStatement Update(StaticVarDecl variable)
        {
            if (variable == _variable)
            {
                return this;
            }
            else
            {
                return new BoundStaticVariableStatement(variable, _holderClass);
            }
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitVariableDeclaration(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitVariableDeclaration(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitStaticStatement(this);
    }

    public partial class BoundUnset : BoundStatement
    {
        public override OperationKind Kind => OperationKind.None;

        /// <summary>
        /// Reference to be unset.
        /// </summary>
        public BoundReferenceExpression Variable { get; set; }

        public BoundUnset(BoundReferenceExpression variable)
        {
            this.Variable = variable;
        }

        public BoundUnset Update(BoundReferenceExpression variable)
        {
            if (variable == Variable)
            {
                return this;
            }
            else
            {
                return new BoundUnset(variable);
            }
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitUnset(this);
    }

    /// <summary>
    /// Represents yield return and continuation.
    /// </summary>
    public partial class BoundYieldStatement : BoundStatement, IReturnOperation
    {
        public override OperationKind Kind => OperationKind.YieldReturn;

        public BoundExpression YieldedValue { get; internal set; }
        public BoundExpression YieldedKey { get; internal set; }

        IOperation IReturnOperation.ReturnedValue => YieldedValue;

        /// <summary>
        /// The yield expression unique ordinal value within the routine.
        /// Indexed from one.
        /// </summary>
        public int YieldIndex { get; }

        /// <summary>
        /// Gets value indicating the `yield` is a part of `yield from` semantics.
        /// In result, keys yielded by this statement do not update Generator auto-incremented keys.
        /// </summary>
        public bool IsYieldFrom { get; set; }

        /// <summary>
        /// "try" scopes in which is this statement included ("catch" and "finally" are handled differently).
        /// Generator state machine may only jump before these scopes (CIL does not allow jumping into).
        /// </summary>
        public LinkedList<TryCatchEdge> ContainingTryScopes { get; private set; } = new LinkedList<TryCatchEdge>();

        public BoundYieldStatement(int index, BoundExpression valueExpression, BoundExpression keyExpression, IEnumerable<TryCatchEdge> tryScopes = null)
        {
            Debug.Assert(index > 0);

            YieldIndex = index;
            YieldedValue = valueExpression;
            YieldedKey = keyExpression;

            tryScopes?.ForEach(ts => ContainingTryScopes.AddLast(ts));
        }

        public BoundYieldStatement Update(int index, BoundExpression valueExpression, BoundExpression keyExpression)
        {
            if (index == YieldIndex && valueExpression == YieldedValue && keyExpression == YieldedKey)
            {
                return this;
            }
            else
            {
                return new BoundYieldStatement(index, valueExpression, keyExpression, ContainingTryScopes);
            }
        }

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor)
            => visitor.VisitYieldStatement(this);

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitReturn(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitReturn(this, argument);
    }

    public sealed partial class BoundDeclareStatement : BoundStatement
    {
        public override OperationKind Kind => OperationKind.None;

        public BoundDeclareStatement()
        {
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);

        /// <summary>Invokes corresponding <c>Visit</c> method on given <paramref name="visitor"/>.</summary>
        /// <param name="visitor">A reference to a <see cref="PhpOperationVisitor{TResult}"/> instance. Cannot be <c>null</c>.</param>
        /// <returns>The value returned by the <paramref name="visitor"/>.</returns>
        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitDeclareStatement(this);
    }
}
