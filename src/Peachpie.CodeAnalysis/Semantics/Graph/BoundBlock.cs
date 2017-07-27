using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Represents control flow block.
    /// </summary>
    [DebuggerDisplay("{DebugDisplay}")]
    public partial class BoundBlock : BoundStatement, IBlockStatement
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
        /// Current naming context.
        /// Can be a <c>null</c> reference.
        /// </summary>
        internal NamingContext Naming { get; set; }

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

        public virtual void Accept(GraphVisitor visitor) => visitor.VisitCFGBlock(this);

        #region IBlockStatement

        ImmutableArray<IStatement> IBlockStatement.Statements => _statements.Cast<IStatement>().AsImmutable();

        ImmutableArray<ILocalSymbol> IBlockStatement.Locals => Locals;
        protected virtual ImmutableArray<ILocalSymbol> Locals => ImmutableArray<ILocalSymbol>.Empty;

        public override OperationKind Kind => OperationKind.BlockStatement;

        bool IOperation.IsInvalid => false;

        SyntaxNode IOperation.Syntax => null;

        public override void Accept(PhpOperationVisitor visitor) => visitor.VisitBlockStatement(this);

        public override void Accept(OperationVisitor visitor) => visitor.VisitBlockStatement(this);

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitBlockStatement(this, argument);

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

        public override void Accept(GraphVisitor visitor) => visitor.VisitCFGExitBlock(this);
    }

    /// <summary>
    /// Represents control flow block of catch item.
    /// </summary>
    [DebuggerDisplay("CatchBlock({ClassName.QualifiedName})")]
    public partial class CatchBlock : BoundBlock //, ICatch
    {
        /// <summary>
        /// Internal name of the block.
        /// </summary>
        protected override string DebugName => "Catch";

        /// <summary>
        /// Catch variable type.
        /// </summary>
        public BoundTypeRef TypeRef { get { return _typeRef; } }
        private readonly BoundTypeRef _typeRef;

        /// <summary>
        /// A variable where an exception is assigned in.
        /// </summary>
        public BoundVariableRef Variable => _variable;

        readonly BoundVariableRef _variable;

        public CatchBlock(BoundTypeRef typeRef, BoundVariableRef variable)
        {
            _typeRef = typeRef;
            _variable = variable;
        }

        public override void Accept(GraphVisitor visitor) => visitor.VisitCFGCatchBlock(this);

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
        /// Gets case value expression bag. In case of default item, returns <c>BoundItemsBag/<BoundExpression/>.Empty</c>.
        /// </summary>
        public BoundItemsBag<BoundExpression> CaseValue { get { return _caseValue; } }
        private readonly BoundItemsBag<BoundExpression> _caseValue;

        /// <summary>
        /// Gets value indicating whether the case represents a default.
        /// </summary>
        public bool IsDefault => _caseValue.IsEmpty;

        public CaseBlock(BoundItemsBag<BoundExpression> caseValue)
        {
            _caseValue = caseValue;
        }

        public override void Accept(GraphVisitor visitor) => visitor.VisitCFGCaseBlock(this);
    }
}
