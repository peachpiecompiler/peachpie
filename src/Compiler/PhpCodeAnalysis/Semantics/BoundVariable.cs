using Microsoft.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;

namespace Pchp.CodeAnalysis.Semantics
{
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
        public string Name => this.Symbol.Name;

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

    public partial class BoundLocal : BoundVariable, IVariable
    {
        private SourceLocalSymbol _symbol;
        private BoundExpression _initializer;

        internal BoundLocal(SourceLocalSymbol symbol, BoundExpression initializer)
            :base(symbol.LocalKind)
        {
            _symbol = symbol;
            _initializer = initializer;
        }

        public IExpression InitialValue => _initializer;

        public ILocalSymbol Variable => _symbol;

        internal override Symbol Symbol => _symbol;

        public override OperationKind Kind => OperationKind.VariableDeclaration;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitVariable(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitVariable(this, argument);
    }

    public partial class BoundParameter : BoundVariable, IParameterInitializer
    {
        private ParameterSymbol _symbol;
        // TODO: temporary BoundLocal for copy-on-pass parameters

        internal BoundParameter(ParameterSymbol symbol)
            :base(VariableKind.Parameter)
        {
            _symbol = symbol;
        }

        public IParameterSymbol Parameter => _symbol;

        public IExpression Value => null;

        internal override Symbol Symbol => _symbol;

        public override OperationKind Kind => OperationKind.ParameterInitializerAtDeclaration;

        public override void Accept(OperationVisitor visitor)
            => visitor.VisitParameterInitializer(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitParameterInitializer(this, argument);
    }
}
