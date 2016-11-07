using Microsoft.CodeAnalysis.Semantics;
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

namespace Pchp.CodeAnalysis.Semantics
{
    #region BoundVariable

    /// <summary>
    /// Represents a variable within routine.
    /// </summary>
    public abstract partial class BoundVariable : IOperation
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

        public abstract OperationKind Kind { get; }

        public bool IsInvalid => false;

        public SyntaxNode Syntax => null;

        protected BoundVariable(VariableKind kind)
        {
            this.VariableKind = kind;
        }

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    #endregion

    #region BoundLocal

    public partial class BoundLocal : BoundVariable, IVariable
    {
        private SourceLocalSymbol _symbol;

        internal BoundLocal(SourceLocalSymbol symbol)
            : base(symbol.LocalKind)
        {
            _symbol = symbol;
        }

        public virtual IExpression InitialValue => null;

        public ILocalSymbol Variable => _symbol;

        internal override Symbol Symbol => _symbol;

        public override OperationKind Kind => OperationKind.VariableDeclaration;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitVariable(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitVariable(this, argument);
    }

    #endregion

    #region BoundIndirectLocal

    public partial class BoundIndirectLocal : BoundVariable, IVariable
    {
        public override OperationKind Kind => OperationKind.VariableDeclaration;

        internal override Symbol Symbol => null;

        IExpression IVariable.InitialValue => null;

        ILocalSymbol IVariable.Variable => (ILocalSymbol)Symbol;

        public override string Name => null;

        public BoundExpression NameExpression => _nameExpr;
        readonly BoundExpression _nameExpr;

        public BoundIndirectLocal(BoundExpression nameExpr)
            : base(VariableKind.LocalVariable)
        {
            _nameExpr = nameExpr;
        }

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitVariable(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitVariable(this, argument);
    }

    #endregion

    #region BoundStaticLocal

    public partial class BoundStaticLocal : BoundLocal
    {
        protected BoundExpression _initialier;

        internal BoundStaticLocal(SourceLocalSymbol symbol, BoundExpression initializer)
            : base(symbol)
        {
            _initialier = initializer;
        }

        public override IExpression InitialValue => _initialier;

        public void Update(BoundExpression initializer)
        {
            _initialier = initializer;
        }
    }

    #endregion

    #region BoundParameter

    public partial class BoundParameter : BoundVariable, IParameterInitializer
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

        IParameterSymbol IParameterInitializer.Parameter => _symbol;

        public IExpression Value => _initializer;

        internal override Symbol Symbol => _symbol;

        public override OperationKind Kind => OperationKind.ParameterInitializerAtDeclaration;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitParameterInitializer(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitParameterInitializer(this, argument);
    }

    #endregion

    #region BoundGlobalVariable

    public partial class BoundGlobalVariable : BoundVariable
    {
        private VariableName _name;

        public BoundGlobalVariable(VariableName name)
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
