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

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Represents control flow block.
    /// </summary>
    [DebuggerDisplay("Block")]
    public class BoundBlock : AstNode, IBlockStatement
    {
        // TODO: initial local state
        
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
                        block = (edge != null && edge.Target != block) ? edge.Target : null;
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
    public sealed class StartBlock : BoundBlock
    {
    }

    /// <summary>
    /// Represents an exit block.
    /// </summary>
    [DebuggerDisplay("Exit")]
    public sealed class ExitBlock : BoundBlock
    {
        // TODO: list of blocks (may be from another CFG!!!) waiting for return type of this function
    }

    /// <summary>
    /// Represents control flow block of catch item.
    /// </summary>
    [DebuggerDisplay("CatchBlock({ClassName.QualifiedName})")]
    public class CatchBlock : BoundBlock
    {
        /// <summary>
        /// Catch variable type.
        /// </summary>
        public DirectTypeRef TypeRef { get { return _typeRef; } }
        private readonly DirectTypeRef _typeRef;

        /// <summary>
        /// A variable where an exception is assigned in.
        /// </summary>
        public VariableName VariableName { get { return _variableName; } }
        private readonly VariableName _variableName;

        public CatchBlock(CatchItem item)
            : base()
        {
            _typeRef = item.TypeRef;
            _variableName = item.Variable.VarName;
        }

        public override void Accept(GraphVisitor visitor) => visitor.VisitCFGCatchBlock(this);
    }

    /// <summary>
    /// Represents control flow block of case item.
    /// </summary>
    [DebuggerDisplay("CaseBlock")]
    public class CaseBlock : BoundBlock
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

        public CaseBlock(SwitchItem item)
            : base()
        {
            var caseItem = item as CaseItem;
            _caseValue = (caseItem != null)
                ? SemanticsBinder.BindExpression(caseItem.CaseVal)
                : null;  // DefaultItem has no value.
        }

        public override void Accept(GraphVisitor visitor) => visitor.VisitCFGCaseBlock(this);
    }
}
