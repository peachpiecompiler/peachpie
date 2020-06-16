using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Pchp.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Represents control flow block.
    /// </summary>
    [DebuggerDisplay("{DebugDisplay}")]
    public partial class BoundBlock : BoundStatement, IBlockOperation
    {
        /// <summary>
        /// Internal name of the block.
        /// </summary>
        protected virtual string DebugName => "Block";

        /// <summary>
        /// Debugger display.
        /// </summary>
        internal string DebugDisplay => $"{FlowState?.Routine?.RoutineName}: {DebugName} #{Ordinal}";

        readonly List<BoundStatement>/*!*/_statements;
        Edge _next;

        /// <summary>
        /// Tag used for graph algorithms.
        /// </summary>
        public int Tag { get { return _tag; } set { _tag = value; } }
        private int _tag;

        /// <summary>
        /// Gets statements contained in this block.
        /// </summary>
        public List<BoundStatement>/*!!*/Statements => _statements;

        /// <summary>
        /// Gets edge pointing out of this block.
        /// </summary>
        public Edge NextEdge
        {
            get { return _next; }
            internal set { _next = value; }
        }

        /// <summary>
        /// Gets block topological index.
        /// Index is unique within the graph.
        /// </summary>
        public int Ordinal { get { return _ordinal; } internal set { _ordinal = value; } }
        private int _ordinal;

        /// <summary>
        /// Gets value indicating the block is unreachable.
        /// </summary>
        public bool IsDead => _ordinal < 0;

        internal BoundBlock()
            :this(new List<BoundStatement>())
        {
            
        }

        internal BoundBlock(List<BoundStatement> statements)
        {
            Debug.Assert(statements != null);
            _statements = statements;

            //CompilerLogSource.Log.Count("TotalBoundBlocks");
        }

        internal BoundBlock Update(List<BoundStatement> statements, Edge nextEdge)
        {
            if (statements == _statements && nextEdge == _next)
            {
                return this;
            }
            else
            {
                return new BoundBlock(statements) { NextEdge = nextEdge, }
                    .WithLocalPropertiesFrom(this);
            }
        }

        internal virtual BoundBlock Clone()
        {
            // We duplicate _statements because of the List's mutability
            return new BoundBlock(new List<BoundStatement>(_statements)) { NextEdge = this.NextEdge }
                .WithLocalPropertiesFrom(this);
        }

        /// <summary>
        /// Adds statement to the block.
        /// </summary>
        internal void Add(BoundStatement stmt)
        {
            Contract.ThrowIfNull(stmt);
            _statements.Add(stmt);
        }

        /// <summary>
        /// Traverses empty blocks to their non-empty successor. Skips duplicities.
        /// </summary>
        internal static List<BoundBlock>/*!*/SkipEmpty(IEnumerable<BoundBlock>/*!*/blocks)
        {
            Contract.ThrowIfNull(blocks);

            var result = new HashSet<BoundBlock>();

            foreach (var x in blocks)
            {
                var block = x;
                while (block != null && block.GetType() == typeof(BoundBlock) && block.Statements.Count == 0)
                {
                    var edge = block.NextEdge as SimpleEdge;
                    if (edge != null || block.NextEdge == null)
                    {
                        block = (edge != null && edge.NextBlock != block) ? edge.NextBlock : null;
                    }
                    else
                    {
                        break;
                    }
                }

                if (block != null)
                {
                    result.Add(block);
                }
            }

            //
            return result.ToList();
        }

        public virtual TResult Accept<TResult>(GraphVisitor<TResult> visitor) => visitor.VisitCFGBlock(this);

        #region IBlockStatement

        ImmutableArray<IOperation> IBlockOperation.Operations => _statements.Cast<IOperation>().AsImmutable();

        ImmutableArray<ILocalSymbol> IBlockOperation.Locals => Locals;
        protected virtual ImmutableArray<ILocalSymbol> Locals => ImmutableArray<ILocalSymbol>.Empty;

        public override OperationKind Kind => OperationKind.Block;

        SyntaxNode IOperation.Syntax => null;

        public override TResult Accept<TResult>(PhpOperationVisitor<TResult> visitor) => visitor.VisitBlockStatement(this);

        public override void Accept(OperationVisitor visitor) => visitor.VisitBlock(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitBlock(this, argument);

        #endregion
    }

    /// <summary>
    /// Represents a start block.
    /// </summary>
    [DebuggerDisplay("Start")]
    public sealed partial class StartBlock : BoundBlock
    {
        /// <summary>
        /// Internal name of the block.
        /// </summary>
        protected override string DebugName => "Start";

        internal StartBlock()
            : base()
        { }

        private StartBlock(List<BoundStatement> statements)
            : base(statements)
        { }

        internal new StartBlock Update(List<BoundStatement> statements, Edge nextEdge)
        {
            if (statements == Statements && nextEdge == NextEdge)
            {
                return this;
            }
            else
            {
                return new StartBlock(statements) { NextEdge = nextEdge }
                    .WithLocalPropertiesFrom(this);
            }
        }

        internal override BoundBlock Clone()
        {
            return new StartBlock(new List<BoundStatement>(Statements)) { NextEdge = this.NextEdge }
                .WithLocalPropertiesFrom(this);
        }

        public override TResult Accept<TResult>(GraphVisitor<TResult> visitor) => visitor.VisitCFGStartBlock(this);
    }

    /// <summary>
    /// Represents an exit block.
    /// </summary>
    [DebuggerDisplay("Exit")]
    public sealed partial class ExitBlock : BoundBlock
    {
        /// <summary>
        /// Internal name of the block.
        /// </summary>
        protected override string DebugName => "Exit";

        internal ExitBlock()
            : base()
        { }

        private ExitBlock(List<BoundStatement> statements)
            : base(statements)
        { }

        internal ExitBlock Update(List<BoundStatement> statements)
        {
            Debug.Assert(NextEdge == null);

            if (statements == Statements)
            {
                return this;
            }
            else
            {
                return new ExitBlock(statements)
                    .WithLocalPropertiesFrom(this);
            }
        }

        internal override BoundBlock Clone()
        {
            return new ExitBlock(new List<BoundStatement>(Statements))
                .WithLocalPropertiesFrom(this);
        }

        public override TResult Accept<TResult>(GraphVisitor<TResult> visitor) => visitor.VisitCFGExitBlock(this);
    }

    /// <summary>
    /// Represents control flow block of catch item.
    /// </summary>
    [DebuggerDisplay("CatchBlock({TypeRef})")]
    public partial class CatchBlock : BoundBlock //, ICatch
    {
        /// <summary>
        /// Internal name of the block.
        /// </summary>
        protected override string DebugName => "Catch";

        /// <summary>
        /// Catch variable type.
        /// </summary>
        public IBoundTypeRef TypeRef { get { return _typeRef; } }
        private readonly IBoundTypeRef _typeRef;

        /// <summary>
        /// A variable where an exception is assigned in.
        /// Can be <c>null</c> if catch is non-capturing.
        /// </summary>
        public BoundVariableRef Variable { get; }

        public CatchBlock(IBoundTypeRef typeRef, BoundVariableRef variable)
            : this(typeRef, variable, new List<BoundStatement>())
        { }

        private CatchBlock(IBoundTypeRef typeRef, BoundVariableRef variable, List<BoundStatement> statements)
            : base(statements)
        {
            _typeRef = typeRef;
            this.Variable = variable;
        }

        internal CatchBlock Update(BoundTypeRef typeRef, BoundVariableRef variable, List<BoundStatement> statements, Edge nextEdge)
        {
            if (typeRef == _typeRef && variable == Variable && statements == Statements && nextEdge == NextEdge)
            {
                return this;
            }
            else
            {
                return new CatchBlock(typeRef, variable, statements) { NextEdge = nextEdge }
                    .WithLocalPropertiesFrom(this);
            }
        }

        internal override BoundBlock Clone()
        {
            return new CatchBlock(_typeRef, Variable, new List<BoundStatement>(Statements)) { NextEdge = NextEdge }
                .WithLocalPropertiesFrom(this);
        }

        public override TResult Accept<TResult>(GraphVisitor<TResult> visitor) => visitor.VisitCFGCatchBlock(this);

        //#region ICatch

        //IBlockStatement ICatch.Handler => this.NextEdge.Targets.Single();

        //ITypeSymbol ICatch.CaughtType => this.ResolvedType;

        //IExpression ICatch.Filter => null;

        //ILocalSymbol ICatch.ExceptionLocal => _variable.Variable?.Symbol as ILocalSymbol;

        //#endregion
    }

    /// <summary>
    /// Represents control flow block of case item.
    /// </summary>
    [DebuggerDisplay("CaseBlock")]
    public partial class CaseBlock : BoundBlock
    {
        /// <summary>
        /// Internal name of the block.
        /// </summary>
        protected override string DebugName => IsDefault ? "default:" : "case:";

        /// <summary>
        /// Gets case value expression bag.
        /// In case of default case, it is set <see cref="BoundItemsBag{BoundExpression}.Empty"/>.
        /// </summary>
        public BoundItemsBag<BoundExpression> CaseValue => _caseValue;
        private readonly BoundItemsBag<BoundExpression> _caseValue;

        /// <summary>
        /// Gets value indicating whether the case represents a default.
        /// </summary>
        public bool IsDefault => _caseValue.IsEmpty;

        public CaseBlock(BoundItemsBag<BoundExpression> caseValue)
            : this(caseValue, new List<BoundStatement>())
        { }

        private CaseBlock(BoundItemsBag<BoundExpression> caseValue, List<BoundStatement> statements)
            : base(statements)
        {
            _caseValue = caseValue;
        }

        internal CaseBlock Update(BoundItemsBag<BoundExpression> caseValue, List<BoundStatement> statements, Edge nextEdge)
        {
            if (caseValue == _caseValue && statements == Statements && nextEdge == NextEdge)
            {
                return this;
            }
            else
            {
                return new CaseBlock(caseValue, statements) { NextEdge = nextEdge }
                    .WithLocalPropertiesFrom(this);
            }
        }

        internal override BoundBlock Clone()
        {
            return new CaseBlock(_caseValue, new List<BoundStatement>(Statements)) { NextEdge = NextEdge }
                .WithLocalPropertiesFrom(this);
        }

        public override TResult Accept<TResult>(GraphVisitor<TResult> visitor) => visitor.VisitCFGCaseBlock(this);
    }
}
