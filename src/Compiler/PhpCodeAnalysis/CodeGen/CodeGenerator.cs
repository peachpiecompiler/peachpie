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
        /// Emit cast from one type to another.
        /// </summary>
        internal void EmitCast(INamedTypeSymbol from, INamedTypeSymbol to)
        {
            throw new NotImplementedException();
        }
    }
}
