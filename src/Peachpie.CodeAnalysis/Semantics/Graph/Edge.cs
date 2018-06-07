using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis.Text;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Represents an edge to other blocks.
    /// </summary>
    public abstract partial class Edge : AstNode
    {
        /// <summary>
        /// Associated syntax node.
        /// </summary>
        internal LangElement PhpSyntax { get; set; }

        /// <summary>
        /// Target blocks.
        /// </summary>
        public abstract IEnumerable<BoundBlock>/*!!*/Targets { get; }

        /// <summary>
        /// The block after the edge. Can be a <c>null</c> reference.
        /// </summary>
        public abstract BoundBlock NextBlock { get; }

        /// <summary>
        /// Gets value indicating whether the edge represents a conditional edge.
        /// </summary>
        public virtual bool IsConditional => false;

        /// <summary>
        /// Gets value indicating whether the edge represents try/catch.
        /// </summary>
        public virtual bool IsTryCatch => false;

        /// <summary>
        /// Gets value indicating whether the edge represents switch.
        /// </summary>
        public virtual bool IsSwitch => false;

        /// <summary>
        /// Condition expression of conditional edge.
        /// </summary>
        public virtual BoundExpression Condition => null;

        /// <summary>
        /// Catch blocks if try/catch edge.
        /// </summary>
        public virtual CatchBlock[] CatchBlocks => EmptyArray<CatchBlock>.Instance;

        /// <summary>
        /// Finally block of try/catch edge.
        /// </summary>
        public virtual BoundBlock FinallyBlock => null;

        /// <summary>
        /// Enumeration with single case blocks.
        /// </summary>
        public virtual CaseBlock[] CaseBlocks => EmptyArray<CaseBlock>.Instance;

        internal Edge(BoundBlock/*!*/source)
        {
            Contract.ThrowIfNull(source);
        }

        protected void Connect(BoundBlock/*!*/source)
        {
            source.NextEdge = this;
        }

        /// <summary>
        /// Visits the object by given visitor.
        /// </summary>
        public abstract void Visit(GraphVisitor visitor);
    }

    /// <summary>
    /// Represents simple unconditional jump.
    /// </summary>
    [DebuggerDisplay("SimpleEdge")]
    public partial class SimpleEdge : Edge
    {
        /// <summary>
        /// Target block.
        /// </summary>
        public BoundBlock Target => _target;
        private readonly BoundBlock _target;

        /// <summary>
        /// Gets the target block if the simple edge.
        /// </summary>
        public override BoundBlock NextBlock => _target;

        internal SimpleEdge(BoundBlock source, BoundBlock target)
            : base(source)
        {
            Debug.Assert(source != target);
            _target = target;
            Connect(source);
        }

        /// <summary>
        /// Target blocks.
        /// </summary>
        public override IEnumerable<BoundBlock> Targets => new BoundBlock[] { _target };
        
        /// <summary>
        /// Visits the object by given visitor.
        /// </summary>
        public override void Visit(GraphVisitor visitor) => visitor.VisitCFGSimpleEdge(this);
    }

    /// <summary>
    /// Represents an edge leaving try/catch block.
    /// The edge is not emitted.
    /// </summary>
    public partial class LeaveEdge : SimpleEdge
    {
        internal LeaveEdge(BoundBlock source, BoundBlock target)
            :base(source, target)
        {
        }
    }

    /// <summary>
    /// Conditional edge.
    /// </summary>
    [DebuggerDisplay("ConditionalEdge")]
    public sealed partial class ConditionalEdge : Edge
    {
        private readonly BoundBlock _true, _false;
        private readonly BoundExpression _condition;

        public override BoundBlock NextBlock => _false;

        /// <summary>
        /// Gets a value indicating the condition within a loop construct.
        /// </summary>
        public bool IsLoop { get; internal set; }

        /// <summary>
        /// Target true block
        /// </summary>
        public BoundBlock/*!*/TrueTarget => _true;

        /// <summary>
        /// Target false block.
        /// </summary>
        public BoundBlock/*!*/FalseTarget => _false;

        internal ConditionalEdge(BoundBlock source, BoundBlock @true, BoundBlock @false, BoundExpression cond)
            : base(source)
        {
            Debug.Assert(@true != @false);

            _true = @true;
            _false = @false;
            _condition = cond;

            Connect(source);
        }

        /// <summary>
        /// All target blocks.
        /// </summary>
        public override IEnumerable<BoundBlock> Targets => new BoundBlock[] { _true, _false };

        public override bool IsConditional
        {
            get { return true; }
        }

        public override BoundExpression Condition => _condition;

        /// <summary>
        /// Visits the object by given visitor.
        /// </summary>
        public override void Visit(GraphVisitor visitor) => visitor.VisitCFGConditionalEdge(this);
    }

    /// <summary>
    /// Represents try/catch edge.
    /// </summary>
    [DebuggerDisplay("TryCatchEdge")]
    public sealed partial class TryCatchEdge : Edge
    {
        private readonly BoundBlock _body, _end;
        private readonly CatchBlock[] _catchBlocks;
        private readonly BoundBlock _finallyBlock;

        /// <summary>
        /// Where <see cref="BodyBlock"/>, catch blocks and <see cref="FinallyBlock"/> go to.
        /// </summary>
        public override BoundBlock NextBlock => _end;

        /// <summary>
        /// Try block.
        /// </summary>
        public BoundBlock BodyBlock => _body;
        
        internal TryCatchEdge(BoundBlock source, BoundBlock body, CatchBlock[] catchBlocks, BoundBlock finallyBlock, BoundBlock endBlock)
            : base(source)
        {
            Debug.Assert(catchBlocks != null);
            Debug.Assert(catchBlocks.Length != 0 || finallyBlock != null);  // catch or finally or both

            _body = body;
            _catchBlocks = catchBlocks;
            _finallyBlock = finallyBlock;
            _end = endBlock;
            Connect(source);
        }

        /// <summary>
        /// All target blocks.
        /// </summary>
        public override IEnumerable<BoundBlock> Targets
        {
            get
            {
                var size = _catchBlocks.Length + (_finallyBlock != null ? 2 : 1);
                var arr = new BoundBlock[size];

                // [ body, ...catches, finally]

                arr[0] = _body;
                Array.Copy(_catchBlocks, 0, arr, 1, _catchBlocks.Length);
                if (_finallyBlock != null)
                {
                    arr[size - 1] = _finallyBlock;
                }

                //
                return arr;
            }
        }

        public override bool IsTryCatch => true;

        public override CatchBlock[] CatchBlocks => _catchBlocks;

        public override BoundBlock FinallyBlock => _finallyBlock;

        /// <summary>
        /// Ordinal of the block after the <c>try</c> scope.
        /// </summary>
        public int TryBlockScopeEnd => _catchBlocks.Length != 0 ? _catchBlocks[0].Ordinal : _finallyBlock.Ordinal;

        /// <summary>
        /// Visits the object by given visitor.
        /// </summary>
        public override void Visit(GraphVisitor visitor) => visitor.VisitCFGTryCatchEdge(this);
    }

    /// <summary>
    /// Represents foreach edge through the enumeree invocation.
    /// </summary>
    [DebuggerDisplay("ForeachEnumeree")]
    public sealed partial class ForeachEnumereeEdge : SimpleEdge
    {
        /// <summary>
        /// Array to enumerate through.
        /// </summary>
        public BoundExpression Enumeree => _enumeree;
        private readonly BoundExpression _enumeree;
        readonly bool _aliasedValues;

        internal ForeachEnumereeEdge(BoundBlock/*!*/source, BoundBlock/*!*/target, BoundExpression/*!*/enumeree, bool aliasedValues)
            : base(source, target)
        {
            Contract.ThrowIfNull(enumeree);
            _enumeree = enumeree;
            _aliasedValues = aliasedValues;
        }

        /// <summary>
        /// Visits the object by given visitor.
        /// </summary>
        public override void Visit(GraphVisitor visitor) => visitor.VisitCFGForeachEnumereeEdge(this);
    }

    /// <summary>
    /// Represents foreach edge from enumeree invocation through <c>MoveNext</c> to body block or end.
    /// </summary>
    [DebuggerDisplay("ForeachMoveNextEdge")]
    public sealed partial class ForeachMoveNextEdge : Edge
    {
        readonly BoundBlock _body, _end;

        /// <summary>
        /// Span of the move expression to emit sequence point of <c>MoveNext</c> operation.
        /// </summary>
        readonly TextSpan _moveSpan;

        /// <summary>
        /// Content of the foreach.
        /// </summary>
        public BoundBlock BodyBlock => _body;
        
        /// <summary>
        /// Block after the foreach.
        /// </summary>
        public override BoundBlock NextBlock => _end;

        /// <summary>
        /// Reference to the edge defining the enumeree.
        /// </summary>
        public ForeachEnumereeEdge EnumereeEdge => _enumereeEdge;
        readonly ForeachEnumereeEdge _enumereeEdge;

        /// <summary>
        /// Variable to store key in (can be null).
        /// </summary>
        public BoundReferenceExpression KeyVariable { get { return _keyVariable; } }
        readonly BoundReferenceExpression _keyVariable;

        /// <summary>
        /// Variable to store value in
        /// </summary>
        public BoundReferenceExpression ValueVariable { get { return _valueVariable; } }
        readonly BoundReferenceExpression _valueVariable;

        internal ForeachMoveNextEdge(BoundBlock/*!*/source, BoundBlock/*!*/body, BoundBlock/*!*/end, ForeachEnumereeEdge/*!*/enumereeEdge, BoundReferenceExpression keyVar, BoundReferenceExpression/*!*/valueVar, TextSpan moveSpan)
            : base(source)
        {
            Contract.ThrowIfNull(body);
            Contract.ThrowIfNull(end);
            Contract.ThrowIfNull(enumereeEdge);

            _body = body;
            _end = end;
            _enumereeEdge = enumereeEdge;
            _keyVariable = keyVar;
            _valueVariable = valueVar;
            _moveSpan = moveSpan;

            Connect(source);
        }

        public override IEnumerable<BoundBlock> Targets
        {
            get { return new BoundBlock[] { _body, _end }; }
        }

        /// <summary>
        /// Visits the object by given visitor.
        /// </summary>
        public override void Visit(GraphVisitor visitor) => visitor.VisitCFGForeachMoveNextEdge(this);
    }

    /// <summary>
    /// Represents switch edge.
    /// </summary>
    [DebuggerDisplay("SwitchEdge")]
    public sealed partial class SwitchEdge : Edge
    {
        readonly BoundExpression _switchValue;
        readonly CaseBlock[] _caseBlocks;

        readonly BoundBlock _end;

        public override BoundBlock NextBlock => _end;

        /// <summary>
        /// The expression representing the switch value.
        /// </summary>
        public BoundExpression SwitchValue => _switchValue;

        public override IEnumerable<BoundBlock> Targets => _caseBlocks;

        public override bool IsSwitch => true;

        public override CaseBlock[] CaseBlocks => _caseBlocks;

        /// <summary>
        /// Gets the case blocks representing a default section.
        /// </summary>
        public CaseBlock DefaultBlock => _caseBlocks.FirstOrDefault(c => c.IsDefault);

        internal SwitchEdge(BoundBlock source, BoundExpression switchValue, CaseBlock[] caseBlocks, BoundBlock endBlock)
            : base(source)
        {
            Contract.ThrowIfNull(caseBlocks);
            _switchValue = switchValue;
            _caseBlocks = caseBlocks;
            _end = endBlock;

            Connect(source);
        }

        /// <summary>
        /// Visits the object by given visitor.
        /// </summary>
        public override void Visit(GraphVisitor visitor) => visitor.VisitCFGSwitchEdge(this);
    }
}
