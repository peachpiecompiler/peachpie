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

namespace Pchp.CodeAnalysis.CodeGen
{
    internal class CodeGenerator
    {
        #region Fields

        readonly ILBuilder _il;
        readonly SourceRoutineSymbol _routine;
        readonly PEModuleBuilder _moduleBuilder;
        readonly OptimizationLevel _optimizations;
        readonly bool _emittingPdb;
        readonly HashSet<BoundBlock> _emitted = new HashSet<BoundBlock>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets underlaying <see cref="ILBuilder"/>.
        /// </summary>
        public ILBuilder IL => _il;

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
            _emittingPdb = emittingPdb;
        }

        #endregion

        /// <summary>
        /// Emits routines body.
        /// </summary>
        internal void Generate()
        {
            Generate(_routine.ControlFlowGraph.Start);

            // DEBUG

            _il.EmitNullConstant();
            _il.EmitRet(false);

            // DEBUG

            _il.AssertStackEmpty();
        }

        /// <summary>
        /// Emits given block and recursively its edge if it was not emitted already.
        /// </summary>
        internal void Generate(BoundBlock block)
        {
            Contract.ThrowIfNull(block);

            if (_emitted.Add(block))
            {
                // mark location as a label
                // to allow branching to the block
                _il.MarkLabel(block);

                //
                Emit(block);
                Emit(block.NextEdge);
            }
        }

        /// <summary>
        /// Gets value indicating whether the given block was already emitted.
        /// </summary>
        internal bool IsGenerated(BoundBlock block) => _emitted.Contains(block);

        internal void Emit(IEmittable element)
        {
            if (element != null)
                element.Emit(this);
        }

        /// <summary>
        /// Emit cast from one type to another.
        /// </summary>
        public void EmitCast(INamedTypeSymbol from, INamedTypeSymbol to)
        {
            throw new NotImplementedException();
        }

        public void EmitOpCode(ILOpCode code) => _il.EmitOpCode(code);
    }

    /// <summary>
    /// Represents an semantic element that can be emitted.
    /// </summary>
    internal interface IEmittable
    {
        /// <summary>
        /// Emits IL into the underlaying <see cref="ILBuilder"/>.
        /// </summary>
        void Emit(CodeGenerator il);
    }
}
