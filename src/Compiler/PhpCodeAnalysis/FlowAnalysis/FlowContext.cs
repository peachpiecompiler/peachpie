using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax;
using Pchp.Syntax.AST;
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
        int BitsCount => FlowState.CommonState.BitsCount;

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Associated type context.
        /// </summary>
        public TypeRefContext TypeRefContext => _typeRefContext;
        readonly TypeRefContext/*!*/_typeRefContext;

        /// <summary>
        /// Information variables within the routine.
        /// </summary>
        readonly ImmutableArray<BoundVariable>/*!*/_locals;

        /// <summary>
        /// Merged local variables type.
        /// </summary>
        internal TypeRefMask[] LocalsType => _localsType;
        readonly TypeRefMask[]/*!*/_localsType;

        /// <summary>
        /// Index of return variable. It is <c>-1</c> in case there are no return statements.
        /// </summary>
        public int ReturnVarIndex => _returnVarIndex;
        readonly int _returnVarIndex;

        /// <summary>
        /// Bit array indicating what variables may be referenced.
        /// </summary>
        ulong _referencesMask = 0;

        /// <summary>
        /// Gets array of local variables. Names are indexed by their internal index.
        /// </summary>
        internal ImmutableArray<BoundVariable> Locals => _locals;

        /// <summary>
        /// Finds index of variable with given name.
        /// </summary>
        int FindVar(string name)
        {
            var vars = _locals;
            for (int i = 0; i < vars.Length; i++)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(vars[i].Name, name))
                {
                    return i;
                }
            }

            //
            return -1;
        }
        
        #endregion

        #region Construction

        internal FlowContext(TypeRefContext typeCtx, ImmutableArray<BoundVariable> locals, int returnIndex)
        {
            Contract.ThrowIfNull(typeCtx);
            Debug.Assert(!locals.IsDefault);
            Debug.Assert(returnIndex >= -1 && returnIndex < locals.Length);

            _locals = locals;
            _localsType = new TypeRefMask[locals.Length];
            _returnVarIndex = returnIndex;
            _typeRefContext = typeCtx;
        }

        #endregion

        #region Public Methods

        public void SetReference(int varindex)
        {
            if (varindex >= 0 && varindex < BitsCount)
                _referencesMask |= (ulong)1 << varindex;
        }

        public void SetAllReferences()
        {
            _referencesMask = ~(ulong)0;
        }

        /// <summary>
        /// Gets value indicating whether given variable might be a reference.
        /// </summary>
        public bool IsReference(int varindex)
        {
            // anything >= 64 is reported as a possible reference
            return varindex < 0 || varindex >= BitsCount || (_referencesMask & (ulong)1 << varindex) != 0;
        }

        public BoundVariable GetVar(int index)
        {
            return (index >= 0 && index < _locals.Length)
                ? _locals[index]
                : null;
        }

        /// <summary>
        /// Gets bound variable with given name.
        /// </summary>
        public BoundVariable GetVar(string name)
        {
            return GetVar(FindVar(name));
        }

        public void AddVarType(int varindex, TypeRefMask type)
        {
            if (varindex >= 0 && varindex < _localsType.Length)
            {
                _localsType[varindex] |= type;
            }
        }

        public TypeRefMask GetVarType(string name)
        {
            var index = FindVar(name);
            return (index >= 0)
                ? _localsType[index]
                : default(TypeRefMask);
        }

        #endregion
    }
}
