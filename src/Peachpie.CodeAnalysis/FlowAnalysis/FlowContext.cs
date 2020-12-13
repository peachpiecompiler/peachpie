using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Manages context of local variables, their merged type and return value type.
    /// </summary>
    public class FlowContext
    {
        #region Constants

        /// <summary>
        /// Size of ulong bit array (<c>64</c>).
        /// </summary>
        internal const int BitsCount = sizeof(ulong) * 8;

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Associated type context.
        /// </summary>
        public TypeRefContext TypeRefContext => _typeCtx;
        readonly TypeRefContext/*!*/_typeCtx;

        /// <summary>
        /// Reference to corresponding routine symbol. Can be a <c>null</c> reference.
        /// </summary>
        internal SourceRoutineSymbol Routine => _routine;
        readonly SourceRoutineSymbol _routine;

        /// <summary>
        /// Gets reference to containing file symbol.
        /// Cannot be <c>null</c>.
        /// </summary>
        internal SourceFileSymbol ContainingFile
        {
            get
            {
                if (Routine != null)
                {
                    return Routine.ContainingFile;
                }

                if (_typeCtx.SelfType != null)
                {
                    return ((SourceTypeSymbol)_typeCtx.SelfType).ContainingFile;
                }

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Map of variables name and their index.
        /// </summary>
        readonly Dictionary<VariableName, int>/*!*/_varsIndex;

        /// <summary>
        /// Bit mask of variables where bit with value <c>1</c> signalizes that variables with index corresponding to the bit number has been used.
        /// </summary>
        ulong _usedMask;

        /// <summary>
        /// Merged local variables type.
        /// </summary>
        internal TypeRefMask[] VarsType => _varsType;
        TypeRefMask[]/*!*/_varsType = EmptyArray<TypeRefMask>.Instance;

        /// <summary>
        /// Merged return expressions type.
        /// </summary>
        internal TypeRefMask ReturnType { get; set; }

        /// <summary>
        /// Version of the analysis, incremented whenever a set of semantic tree transformations happen.
        /// </summary>
        internal int Version => _version;
        int _version;

        #endregion

        #region Construction

        internal FlowContext(TypeRefContext/*!*/typeCtx, SourceRoutineSymbol routine)
        {
            Contract.ThrowIfNull(typeCtx);

            _typeCtx = typeCtx;
            _routine = routine;

            //
            _varsIndex = new Dictionary<VariableName, int>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets index of variable within the context.
        /// </summary>
        public VariableHandle GetVarIndex(VariableName name)
        {
            Debug.Assert(name.IsValid());

            // TODO: RW lock

            int index;
            if (!_varsIndex.TryGetValue(name, out index))
            {
                lock (_varsIndex)
                {
                    index = _varsType.Length;
                    Array.Resize(ref _varsType, index + 1);

                    //
                    _varsIndex[name] = index;
                }
            }

            //
            return new VariableHandle() { Slot = index, Name = name };
        }

        /// <summary>
        /// Enumerates all known variables as pairs of their index and name.
        /// </summary>
        public IEnumerable<VariableHandle> EnumerateVariables()
        {
            return _varsIndex.Select(pair => new VariableHandle()
            {
                Slot = pair.Value,
                Name = pair.Key,
            });
        }

        public void SetReference(int varindex)
        {
            if (varindex >= 0 && varindex < _varsType.Length)
            {
                _varsType[varindex] |= TypeRefMask.IsRefMask;
            }
        }

        /// <summary>
        /// Gets value indicating whether given variable might be a reference.
        /// </summary>
        public bool IsReference(int varindex)
        {
            // anything >= 64 is reported as a possible reference
            return varindex < 0 || varindex >= _varsType.Length || _varsType[varindex].IsRef;
        }

        public void AddVarType(int varindex, TypeRefMask type)
        {
            if (varindex >= 0 && varindex < _varsType.Length)
            {
                _varsType[varindex] |= type;
            }
        }

        public TypeRefMask GetVarType(VariableName name)
        {
            var idx = GetVarIndex(name);
            return _varsType[idx];
        }

        /// <summary>
        /// Sets specified variable as being used.
        /// </summary>
        public void SetUsed(int varindex)
        {
            if (varindex >= 0 && varindex < BitsCount)
            {
                _usedMask |= (ulong)1 << varindex;
            }
        }

        /// <summary>
        /// Marks all local variables as used.
        /// </summary>
        public void SetAllUsed()
        {
            _usedMask = ~(ulong)0;
        }

        public bool IsUsed(int varindex)
        {
            // anything >= 64 is used
            return varindex < 0 || varindex >= BitsCount || (_usedMask & (ulong)1 << varindex) != 0;
        }

        /// <summary>
        /// Discard the current flow analysis information, should be called whenever the routine is transformed.
        /// </summary>
        /// <remarks>
        /// It is expected to be called either on a context without a routine (parameter initializers etc.) or
        /// on a routine with a CFG, hence no abstract methods etc.
        /// </remarks>
        public void InvalidateAnalysis()
        {
            Debug.Assert(Routine?.ControlFlowGraph != null);

            // By incrementing the version, the current flow states won't be valid any longer
            _version++;

            // Reset internal structures to prevent possible bugs in re-analysis
            _usedMask = 0;
            _varsIndex.Clear();
            _varsType = EmptyArray<TypeRefMask>.Instance;

            // Revert the information regarding the return type to the default state
            ReturnType = default;

            // TODO: Recreate the state also in the case of a standalone expression (such as a parameter initializer)
            if (_routine != null)
            {
                // Reset routine properties related to the analysis
                _routine.IsReturnAnalysed = false;

                // Recreate the entry state to enable another analysis
                _routine.ControlFlowGraph.Start.FlowState = StateBinder.CreateInitialState(_routine, this);
            }
        }

        #endregion
    }
}
