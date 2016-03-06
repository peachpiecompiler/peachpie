using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Symbols;
using Pchp.CodeAnalysis.Semantics.Graph;
using System.Reflection.Metadata;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.CodeGen
{
    internal partial class CodeGenerator
    {
        #region BoundBlockOrdinalComparer

        sealed class BoundBlockOrdinalComparer : IComparer<BoundBlock>, IEqualityComparer<BoundBlock>
        {
            int IEqualityComparer<BoundBlock>.GetHashCode(BoundBlock obj) => obj.GetHashCode();

            bool IEqualityComparer<BoundBlock>.Equals(BoundBlock x, BoundBlock y) => object.ReferenceEquals(x, y);
            
            int IComparer<BoundBlock>.Compare(BoundBlock x, BoundBlock y) => y.Ordinal - x.Ordinal;
        }

        #endregion

        #region LocalScope

        internal class LocalScope
        {
            #region Fields

            readonly CodeGenerator _codegen;
            readonly LocalScope _parent;
            readonly ScopeType _type;
            readonly int _from, _to;

            HashSet<BoundBlock> _blocks;

            #endregion

            #region Contruction

            public LocalScope(CodeGenerator codegen, LocalScope parent, ScopeType type, int from, int to)
            {
                Contract.ThrowIfNull(codegen);
                Debug.Assert(from < to);
                _codegen = codegen;
                _parent = parent;
                _type = type;
                _from = from;
                _to = to;
            }

            #endregion

            /// <summary>
            /// Gets underlaying <see cref="ILBuilder"/>.
            /// </summary>
            public ILBuilder IL => _codegen.IL;

            /// <summary>
            /// Gets parent scope. Can be <c>null</c> in case of a root scope.
            /// </summary>
            public LocalScope Parent => _parent;

            public bool IsIn(BoundBlock block)
            {
                return block.Ordinal >= _from && block.Ordinal < _to;
            }

            public void Enqueue(BoundBlock block)
            {
                if (IsIn(block))
                {
                    if (_blocks == null)
                        _blocks = new HashSet<BoundBlock>();

                    _blocks.Add(block);
                }
                else
                {
                    if (Parent == null)
                        throw new ArgumentOutOfRangeException();

                    Parent.Enqueue(block);
                }
            }

            public void ContinueWith(BoundBlock block)
            {
                if (_codegen.IsGenerated(block))
                {
                    // backward edge;
                    // or the block was already emitted, branch there:
                    IL.EmitBranch(ILOpCode.Br, block);
                    return;
                }

                if (block.IsDead)
                {
                    return;
                }

                if (block.Ordinal < _from)
                {
                    throw new InvalidOperationException("block miss");
                }

                if (block.Ordinal < _to)    // is in
                {
                    // TODO: avoid branching to a guarded scope // e.g. goto x; try { x: }

                    if (_blocks != null)
                        _blocks.Remove(block);

                    // continue with the block
                    _codegen.GenerateBlock(block);
                }
                else
                {
                    // forward edge:
                    IL.EmitBranch(ILOpCode.Br, block);  // TODO: avoid branch instruction if block will follow immediately
                    Parent.Enqueue(block);
                }
            }

            internal BoundBlock Dequeue()
            {
                BoundBlock block = null;

                if (_blocks != null && _blocks.Count != 0)
                {
                    // TODO: "priority" queue to avoid branching

                    block = _blocks.First();
                    _blocks.Remove(block);
                }

                //
                return block;
            }
        }

        #endregion

        #region Fields

        readonly ILBuilder _il;
        readonly SourceRoutineSymbol _routine;
        readonly PEModuleBuilder _moduleBuilder;
        readonly OptimizationLevel _optimizations;
        readonly bool _emittingPdb;
        readonly DiagnosticBag _diagnostics;

        /// <summary>
        /// BoundBlock.Tag value indicating the block was emitted.
        /// </summary>
        readonly int _emmittedTag;

        LocalScope _scope = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets underlaying <see cref="ILBuilder"/>.
        /// </summary>
        public ILBuilder IL => _il;

        /// <summary>
        /// Gets the routine we are emitting.
        /// </summary>
        public SourceRoutineSymbol Routine => _routine;

        public DiagnosticBag Diagnostics => _diagnostics;

        #endregion

        #region Construction

        public CodeGenerator(SourceRoutineSymbol routine, ILBuilder il, PEModuleBuilder moduleBuilder, DiagnosticBag diagnostics, OptimizationLevel optimizations, bool emittingPdb)
        {
            Contract.ThrowIfNull(routine);
            Contract.ThrowIfNull(il);
            Contract.ThrowIfNull(moduleBuilder);

            if (routine.ControlFlowGraph == null)
                throw new ArgumentException();

            _routine = routine;
            _il = il;
            _moduleBuilder = moduleBuilder;
            _optimizations = optimizations;
            _diagnostics = diagnostics;
            _emittingPdb = emittingPdb;
            _emmittedTag = routine.ControlFlowGraph.NewColor();
        }

        #endregion

        /// <summary>
        /// Emits routines body.
        /// </summary>
        internal void Generate()
        {
            GenerateScope(_routine.ControlFlowGraph.Start, int.MaxValue);
        }

        #region CFG Emitting

        internal void GenerateScope(BoundBlock block, int to)
        {
            GenerateScope(block, ScopeType.Variable, to);
        }

        internal void GenerateScope(BoundBlock block, ScopeType type, int to)
        {
            Contract.ThrowIfNull(block);
            
            // open scope
            _scope = new LocalScope(this, _scope, type, block.Ordinal, to);
            _scope.ContinueWith(block);

            while ((block = _scope.Dequeue()) != null)
            {
                GenerateBlock(block);
            }

            // close scope
            _scope = _scope.Parent;

            //
            _il.AssertStackEmpty();
        }

        /// <summary>
        /// Gets a reference to the current scope.
        /// </summary>
        internal LocalScope Scope => _scope;

        void GenerateBlock(BoundBlock block)
        {
            // mark the block as emitted
            Debug.Assert(block.Tag != _emmittedTag);
            block.Tag = _emmittedTag;
            
            // mark location as a label
            // to allow branching to the block
            _il.MarkLabel(block);

            //
            Generate(block);
        }

        /// <summary>
        /// Invokes <see cref="IGenerator.Generate"/>.
        /// </summary>
        internal void Generate(IGenerator element)
        {
            if (element != null)
                element.Generate(this);
        }

        /// <summary>
        /// Gets value indicating whether the given block was already emitted.
        /// </summary>
        internal bool IsGenerated(BoundBlock block)
        {
            Contract.ThrowIfNull(block);
            return block.Tag == _emmittedTag;
        }

        #endregion
    }

    /// <summary>
    /// Represents an semantic element that can be emitted.
    /// </summary>
    internal interface IGenerator
    {
        /// <summary>
        /// Emits IL into the underlaying <see cref="ILBuilder"/>.
        /// </summary>
        void Generate(CodeGenerator il);
    }
}
