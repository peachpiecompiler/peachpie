using Pchp.Syntax.AST;
using Pchp.Syntax.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Semantics.Graph
{
    /// <summary>
    /// Represents statements control flow graph.
    /// </summary>
    public sealed partial class ControlFlowGraph : AstNode
    {
        #region LabelBlockFlags, LabelBlockInfo

        /// <summary>
        /// Found label reference (definition or target) information.
        /// </summary>
        [Flags]
        public enum LabelBlockFlags : byte
        {
            /// <summary>
            /// Not used nor defined.
            /// </summary>
            None = 0,

            /// <summary>
            /// Label is defined.
            /// </summary>
            Defined = 1,

            /// <summary>
            /// Label is used as a target.
            /// </summary>
            Used = 2,

            /// <summary>
            /// Label was defined twice or more.
            /// </summary>
            Redefined = 4,
        }

        /// <summary>
        /// Label state.
        /// </summary>
        public sealed class LabelBlockState
        {
            /// <summary>
            /// Label identifier.
            /// </summary>
            public string Label;

            /// <summary>
            /// Positions of label definition and last label use.
            /// </summary>
            public Span LabelSpan;

            /// <summary>
            /// Lable target block.
            /// </summary>
            public BoundBlock TargetBlock;

            /// <summary>
            /// Label information.
            /// </summary>
            public LabelBlockFlags Flags;
        }

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Gets the control flow start block. Cannot be <c>null</c>.
        /// </summary>
        public BoundBlock/*!*/Start { get { return _start; } }
        readonly BoundBlock/*!*/_start;
        
        /// <summary>
        /// Gets the control flow exit block. Cannot be <c>null</c>.
        /// </summary>
        public BoundBlock/*!*/Exit { get { return _exit; } }
        readonly BoundBlock/*!*/_exit;

        ///// <summary>
        ///// Exception block. Can be <c>null</c>.
        ///// If set, code can throw an exception or be terminated by call to <c>exit</c>, before reaching exit block.
        ///// This block is connected with blocks ending with <c>throw</c> statement.
        ///// </summary>
        //public BoundBlock Throws { get { return _exception; } }
        //readonly BoundBlock _exception;

        /// <summary>
        /// Array of labels within routine. Can be <c>null</c>.
        /// </summary>
        public LabelBlockState[] Labels { get { return _labels; } }
        readonly LabelBlockState[] _labels;

        /// <summary>
        /// List of blocks that are unreachable syntactically (statements after JumpStmt etc.).
        /// </summary>
        public List<BoundBlock>/*!*/UnreachableBlocks { get { return _unrecachable; } }
        readonly List<BoundBlock>/*!*/_unrecachable;

        /// <summary>
        /// Last "tag" color used. Used internally for graph algorithms.
        /// </summary>
        int _lastcolor = 0;
        
        #endregion

        #region Construction

        internal ControlFlowGraph(IList<Statement>/*!*/statements, SemanticsBinder/*!*/binder)
            : this(BuilderVisitor.Build(statements, binder))
        {
        }

        private ControlFlowGraph(BuilderVisitor/*!*/builder)
            : this(builder.Start, builder.Exit, /*builder.Exception*/null, builder.Labels, builder.DeadBlocks)
        {
        }

        private ControlFlowGraph(BoundBlock/*!*/start, BoundBlock/*!*/exit, BoundBlock exception, LabelBlockState[] labels, List<BoundBlock> unreachable)
        {
            Contract.ThrowIfNull(start);
            Contract.ThrowIfNull(exit);

            _start = start;
            _exit = exit;
            //_exception = exception;
            _labels = labels;
            _unrecachable = unreachable ?? new List<BoundBlock>();
        }

        #endregion

        /// <summary>
        /// Gets new (unique) color for use by graph algorithms.
        /// </summary>
        /// <returns>New color index.</returns>
        public int NewColor()
        {
            return unchecked(++_lastcolor);
        }

        /// <summary>
        /// Visits control flow blocks and contained statements, in deep.
        /// Unreachable blocks are not visited.
        /// </summary>
        /// <remarks>Visitor does not implement infinite recursion prevention.</remarks>
        public void Visit(GraphVisitor/*!*/visitor) => visitor.VisitCFG(this);
    }
}
