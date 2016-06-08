using Microsoft.CodeAnalysis.Semantics;
using Pchp.Syntax;
using Pchp.Syntax.AST;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Represents control flow block.
    /// </summary>
    [DebuggerDisplay("Block")]
    public partial class BoundBlock : AstNode, IBlockStatement
    {
        readonly List<BoundStatement>/*!*/_statements;
        Edge _next;

        /// <summary>
        /// Tag used for graph algorithms.
        /// </summary>
        public int Tag { get { return _tag; } set { _tag = value; } }
        private int _tag;

        /// <summary>
        /// Associated syntax node.
        /// </summary>
        internal LangElement PhpSyntax { get; set; }

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
        {
            _statements = new List<BoundStatement>();
        }

        /// <summary>
        /// Adds statement to the block.
        /// </summary>
        internal void AddStatement(BoundStatement stmt)
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

        ImmutableArray<IStatement> IBlockStatement.Statements => _statements.AsImmutable<IStatement>();

        ImmutableArray<ILocalSymbol> IBlockStatement.Locals => Locals;
        protected virtual ImmutableArray<ILocalSymbol> Locals => ImmutableArray<ILocalSymbol>.Empty;

        OperationKind IOperation.Kind => OperationKind.BlockStatement;

        bool IOperation.IsInvalid => false;

        SyntaxNode IOperation.Syntax => null;

        void IOperation.Accept(OperationVisitor visitor)
            => visitor.VisitBlockStatement(this);

        TResult IOperation.Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitBlockStatement(this, argument);

        #endregion
    }

    /// <summary>
    /// Represents a start block.
    /// </summary>
    [DebuggerDisplay("Start")]
    public sealed partial class StartBlock : BoundBlock
    {
    }

    /// <summary>
    /// Represents an exit block.
    /// </summary>
    [DebuggerDisplay("Exit")]
    public sealed partial class ExitBlock : BoundBlock
    {
        public override void Accept(GraphVisitor visitor) => visitor.VisitCFGExitBlock(this);
    }

    /// <summary>
    /// Represents control flow block of catch item.
    /// </summary>
    [DebuggerDisplay("CatchBlock({ClassName.QualifiedName})")]
    public partial class CatchBlock : BoundBlock //, ICatch
    {
        /// <summary>
        /// Catch variable type.
        /// </summary>
        public DirectTypeRef TypeRef { get { return _typeRef; } }
        private readonly DirectTypeRef _typeRef;

        /// <summary>
        /// Resolved <see cref="TypeRef"/>.
        /// </summary>
        internal TypeSymbol ResolvedType { get; set; }

        /// <summary>
        /// A variable where an exception is assigned in.
        /// </summary>
        public BoundVariableRef Variable => _variable;

        readonly BoundVariableRef _variable;

        public CatchBlock(DirectTypeRef typeRef, BoundVariableRef variable)
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
        /// Gets case value expression. In case of default item, returns <c>null</c>.
        /// </summary>
        public BoundExpression CaseValue { get { return _caseValue; } }
        private readonly BoundExpression _caseValue;

        /// <summary>
        /// Gets value indicating whether the case represents a default.
        /// </summary>
        public bool IsDefault => _caseValue == null;

        public CaseBlock(BoundExpression caseValue)
        {
            _caseValue = caseValue;
        }

        public override void Accept(GraphVisitor visitor) => visitor.VisitCFGCaseBlock(this);
    }
}
