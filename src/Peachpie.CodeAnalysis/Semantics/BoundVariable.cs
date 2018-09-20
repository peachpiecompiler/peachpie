using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.CodeGen;
using Pchp.CodeAnalysis.FlowAnalysis;
using Devsense.PHP.Syntax;
using System.Diagnostics;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Semantics
{
    #region BoundVariable

    /// <summary>
    /// Represents a variable within routine.
    /// </summary>
    public abstract partial class BoundVariable : BoundOperation
    {
        /// <summary>
        /// Variable kind.
        /// </summary>
        public VariableKind VariableKind { get; set; }

        /// <summary>
        /// Associated symbol, local or parameter.
        /// </summary>
        internal abstract Symbol Symbol { get; }

        /// <summary>
        /// Name of the variable.
        /// </summary>
        public virtual string Name => this.Symbol.Name;

        public bool IsInvalid => false;

        public SyntaxNode Syntax => null;

        protected BoundVariable(VariableKind kind)
        {
            this.VariableKind = kind;
        }
    }

    #endregion

    #region BoundLocal

    public partial class BoundLocal : BoundVariable, IVariableDeclaratorOperation
    {
        private SourceLocalSymbol _symbol;

        internal BoundLocal(SourceLocalSymbol symbol, VariableKind kind = VariableKind.LocalVariable)
            : base(kind)
        {
            Debug.Assert(kind == VariableKind.LocalVariable || kind == VariableKind.LocalTemporalVariable);
            _symbol = symbol;
        }

        IVariableInitializerOperation IVariableDeclaratorOperation.Initializer => null;

        ILocalSymbol IVariableDeclaratorOperation.Symbol => _symbol;

        ImmutableArray<IOperation> IVariableDeclaratorOperation.IgnoredArguments => ImmutableArray<IOperation>.Empty;

        internal override Symbol Symbol => _symbol;

        public override OperationKind Kind => OperationKind.VariableDeclaration;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitVariableDeclarator(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitVariableDeclarator(this, argument);
    }

    #endregion

    #region BoundIndirectLocal

    public partial class BoundIndirectLocal : BoundVariable, IVariableDeclaratorOperation
    {
        public override OperationKind Kind => OperationKind.VariableDeclaration;

        internal override Symbol Symbol => null;

        IVariableInitializerOperation IVariableDeclaratorOperation.Initializer => null;

        ILocalSymbol IVariableDeclaratorOperation.Symbol => (ILocalSymbol)Symbol;

        ImmutableArray<IOperation> IVariableDeclaratorOperation.IgnoredArguments => ImmutableArray<IOperation>.Empty;

        public override string Name => null;

        public BoundExpression NameExpression => _nameExpr;
        readonly BoundExpression _nameExpr;

        public BoundIndirectLocal(BoundExpression nameExpr)
            : base(VariableKind.LocalVariable)
        {
            _nameExpr = nameExpr;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitVariableDeclarator(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitVariableDeclarator(this, argument);
    }

    #endregion

    #region BoundParameter

    public partial class BoundParameter : BoundVariable, IParameterInitializerOperation
    {
        private BoundExpression _initializer;
        private ParameterSymbol _symbol;

        internal BoundParameter(ParameterSymbol symbol, BoundExpression initializer)
            : base(VariableKind.Parameter)
        {
            _symbol = symbol;
            _initializer = initializer;
        }

        internal ParameterSymbol Parameter => _symbol;

        IParameterSymbol IParameterInitializerOperation.Parameter => _symbol;

        ImmutableArray<ILocalSymbol> ISymbolInitializerOperation.Locals => ImmutableArray<ILocalSymbol>.Empty;

        public IOperation Value => _initializer;

        internal override Symbol Symbol => _symbol;

        public override OperationKind Kind => OperationKind.ParameterInitializer;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitParameterInitializer(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitParameterInitializer(this, argument);
    }

    #endregion

    #region BoundThisParameter

    /// <summary>
    /// Represents <c>$this</c> variable in PHP code.
    /// </summary>
    public partial class BoundThisParameter : BoundVariable
    {
        readonly SourceRoutineSymbol _routine;

        internal BoundThisParameter(SourceRoutineSymbol routine)
            : base(VariableKind.ThisParameter)
        {
            _routine = routine;
        }

        public override OperationKind Kind => OperationKind.None;

        public override string Name => VariableName.ThisVariableName.Value;

        internal override Symbol Symbol => null;

        public override void Accept(OperationVisitor visitor)
            => visitor.DefaultVisit(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.DefaultVisit(this, argument);
    }

    #endregion

    #region BoundSuperGlobalVariable

    public partial class BoundSuperGlobalVariable : BoundVariable
    {
        private VariableName _name;

        public BoundSuperGlobalVariable(VariableName name)
            : base(VariableKind.GlobalVariable)
        {
            _name = name;
        }

        public override string Name => _name.Value;

        public override OperationKind Kind => OperationKind.None;

        internal override Symbol Symbol
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            throw new NotSupportedException();
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            throw new NotSupportedException();
        }
    }

    #endregion
}
