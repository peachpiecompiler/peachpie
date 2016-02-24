using Microsoft.CodeAnalysis;
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
        const int BitsCount = sizeof(ulong) * 8;

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Associated type context.
        /// </summary>
        public TypeRefContext TypeRefContext => _typeRefContext;
        readonly TypeRefContext/*!*/_typeRefContext;

        /// <summary>
        /// Merged type information of variables.
        /// </summary>
        readonly TypeRefMask[] _types;

        /// <summary>
        /// Information variables within the routine.
        /// </summary>
        readonly ImmutableArray<ILocalSymbol>/*!*/_vars;

        /// <summary>
        /// Bit array indicating what variables may be referenced.
        /// </summary>
        ulong _referencesMask = 0;

        /// <summary>
        /// Gets or sets type of expressions in return statement.
        /// </summary>
        public TypeRefMask ReturnType => _returnType;
        TypeRefMask _returnType = default(TypeRefMask);

        /// <summary>
        /// Gets array of local variables. Names are indexed by their internal index.
        /// </summary>
        public ImmutableArray<ILocalSymbol> LocalVariables => _vars;
        
        #endregion

        #region Construction

        private FlowContext(TypeRefContext typeCtx, ImmutableArray<ILocalSymbol> locals)
        {
            Contract.ThrowIfNull(typeCtx);
            Debug.Assert(!locals.IsDefaultOrEmpty);

            _types = new TypeRefMask[locals.Length];
            _vars = locals;
            _typeRefContext = typeCtx;
        }

        ///// <summary>
        ///// Creates locals table and gets initial locals state for given routine.
        ///// </summary>
        ///// <remarks>Collects local variables, initializes their ByRef state, initializes them with their type (from type hint, actual CallInfo or PHPDoc).</remarks>
        //internal static void Create(IRoutine/*!*/routine, LangElement[] additional, CallInfo call, out LocalsTable/*!*/table, out LocalsState/*!*/initialState)
        //{
        //    Contract.RequiresNotNull(routine);

        //    // collect local variables within the routine
        //    var locals = Visitors.LocalsVisitor.GetVariables(routine.Element, additional).ToArray();

        //    // create clean type context for the routine
        //    var typeCtx = RoutineHelpers.CreateTypeRefContext(routine);
        //    typeCtx.SetLateStaticBindType(call.GetLateStaticBindType(typeCtx));

        //    table = new FlowContext(typeCtx, locals);
        //    initialState = new LocalsState(table);

        //    // handle parameters passed by reference
        //    var signature = routine.Signature;
        //    if (signature.FormalParams != null)
        //        foreach (var p in signature.FormalParams)
        //            if (p.PassedByRef)
        //                table.SetReference(initialState.Common.GetVarIndex(p.Name));

        //    // get PHPDoc block for routine
        //    var phpdoc = routine.Element.GetProperty<PHPDocBlock>();

        //    // mark $this as initialized
        //    // mark global variables as ByRef, used
        //    // mark function parameters as used, initialized, typed
        //    // construct initial state for variables

        //    int paramIndex = 0;
        //    for (int i = 0; i < locals.Length; i++)
        //    {
        //        switch (locals[i].Type)
        //        {
        //            case VarType.GlobalVariable:
        //                initialState.Common.SetUsed(i);
        //                table.SetReference(i);
        //                initialState.SetVarInitialized(i, true);
        //                break;
        //            case VarType.Parameter:
        //                var paramtag = TypeRef.Helpers.PHPDoc.GetParamTag(phpdoc, paramIndex, locals[i].Name);
        //                initialState.Common.SetUsed(i);
        //                initialState.SetVarInternal(i, GetParamType(typeCtx, paramtag, signature, call, paramIndex));
        //                if (paramtag != null)
        //                    initialState.Common.SetVarSummary(i, paramtag.Description);
        //                //initialState.SetVarValueInternal(i, call.GetParamValue(paramIndex));
        //                paramIndex++;
        //                break;
        //            case VarType.UseParameter:
        //                initialState.SetVarInternal(i, TypeRefMask.AnyType);
        //                initialState.SetVarInitialized(i, true);
        //                break;
        //            case VarType.ThisVariable:
        //                InitThisVar(table, initialState, i);
        //                break;
        //            case VarType.StaticVariable:
        //                initialState.SetVarInitialized(i, true);
        //                break;
        //        }
        //    }
        //}

        /// <summary>
        /// Initializes <c>$this</c> variable, its type and initialized state.
        /// </summary>
        private static void InitThisVar(FlowContext/*!*/ctx, FlowState/*!*/initialState, int varIndex)
        {
            var thisVarType = ctx.TypeRefContext.GetThisTypeMask();
            if (thisVarType.IsUninitialized)
                thisVarType = TypeRefMask.AnyType;

            //
            initialState.SetVarUsed(varIndex);
            initialState.SetVar(varIndex, thisVarType);
        }

        /// <summary>
        /// Helper method getting parameter type.
        /// </summary>
        /// <param name="typeCtx">Routine type context.</param>
        /// <param name="paramTag">PHPDoc param tag if available.</param>
        /// <param name="signature">Call signature.</param>
        /// <param name="call">Call information if called specifically with given context.</param>
        /// <param name="paramIndex">Parameter index.</param>
        /// <returns>Expected type of parameter. Cannot be uninitialized.</returns>
        private static TypeRefMask GetParamType(TypeRefContext/*!*/typeCtx, PHPDocBlock.ParamTag paramTag,
            Signature signature, CallInfo call, int paramIndex)
        {
            Contract.ThrowIfNull(typeCtx);
            Debug.Assert(paramIndex >= 0);

            TypeRefMask result = 0;
            bool isvariadic = false;

            // lookup actual type hint
            if (signature.FormalParams != null && paramIndex < signature.FormalParams.Length)
            {
                isvariadic = signature.FormalParams[paramIndex].IsVariadic;

                var hint = signature.FormalParams[paramIndex].TypeHint;
                if (hint != null)
                {
                    result = typeCtx.GetTypeMaskFromTypeHint(signature.FormalParams[paramIndex].TypeHint);

                    if (isvariadic) // PHP 5.6 variadic parameter (...) // TypeHint -> TypeHint[]
                        result = typeCtx.GetArrayTypeMask(result);
                }
            }

            if (result.IsUninitialized || result.IsAnyType)
            {
                // lookup callInfo
                result = call.GetParamType(typeCtx, paramIndex);
                if (result.IsUninitialized)
                {
                    //// lookup PHPDoc
                    //if (paramTag != null && paramTag.TypeNamesArray.Length != 0)
                    //    result = TypeRef.Helpers.PHPDoc.GetTypeMask(typeCtx, paramTag.TypeNamesArray);

                    if (result.IsUninitialized)
                    {
                        // NOTE: if still unknown, we can use type of the FormalParam.InitValue as Hint
                        result = TypeRefMask.AnyType;
                    }
                }

                // PHP 5.6, variadic parameter (...) is always of type array,
                // if specified else, user meant type of its elements
                if (isvariadic && !typeCtx.IsArray(result))
                    result = typeCtx.GetArrayTypeMask(result);  // hint -> hint[]
            }

            //
            Debug.Assert(!result.IsUninitialized);
            return result;
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

        public void AddVarType(int varindex, TypeRefMask type)
        {
            if (varindex >= 0)
            {
                _types[varindex] |= type;
            }
        }

        /// <summary>
        /// Gets merged type information for given variable by its index.
        /// </summary>
        internal TypeRefMask GetVarType(int varindex)
        {
            return _types[varindex];
        }

        #endregion
    }
}
