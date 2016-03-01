using Pchp.CodeAnalysis.Symbols;
using Pchp.Syntax;
using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.FlowAnalysis.Visitors;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Binds flow state to a routine.
    /// </summary>
    internal static class StateBinder
    {
        /// <summary>
        /// Creates new type context, flow context and flow state for the routine.
        /// </summary>
        public static FlowState CreateInitialState(SourceRoutineSymbol routine)
        {
            Contract.ThrowIfNull(routine);
            Debug.Assert(routine.ContainingType == null || routine.ContainingType is SourceNamedTypeSymbol);

            var containingType = routine.ContainingType as SourceNamedTypeSymbol;

            // collect locals
            var locals = LocalsBinder.BindLocals(routine);
            var returnIdx = locals.IndexOf(x => x.VariableKind == VariableKind.ReturnVariable);

            // create typeCtx
            var typeCtx = routine.TypeRefContext;

            // create FlowContext
            var flowCtx = new FlowContext(typeCtx, locals, returnIdx);

            // create FlowState
            var state = new FlowState(flowCtx);

            // handle parameters passed by reference
            var parameters = routine.Parameters;
            foreach (SourceParameterSymbol p in parameters)
                if (p.Syntax.PassedByRef)
                    state.SetVarRef(p.Name);

            //// get PHPDoc block for routine
            //var phpdoc = routine.Element.GetProperty<PHPDocBlock>();

            // mark $this as initialized
            // mark global variables as ByRef, used
            // mark function parameters as used, initialized, typed
            // construct initial state for variables

            int paramIdx = 0;
            for (int i = 0; i < locals.Length; i++)
            {
                switch (locals[i].VariableKind)
                {
                    case VariableKind.GlobalVariable:
                        state.SetVarRef(i); // => used, byref, initialized
                        break;
                    case VariableKind.Parameter:
                        //state.SetVarUsed(i);
                        //var paramtag = TypeRef.Helpers.PHPDoc.GetParamTag(phpdoc, paramIdx, locals[i].Name);
                        state.SetVar(i, GetParamType(typeCtx, null, ((SourceParameterSymbol)parameters[paramIdx]).Syntax, default(CallInfo), paramIdx));
                        paramIdx++;
                        break;
                    //case VariableKind.UseParameter:
                    //    state.SetVar(i, TypeRefMask.AnyType);
                    //    break;
                    case VariableKind.ThisParameter:
                        InitThisVar(flowCtx, state, i);
                        break;
                    case VariableKind.StaticVariable:
                        state.SetVarInitialized(i);
                        break;
                }
            }
            //
            return state;
        }

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
        private static TypeRefMask GetParamType(TypeRefContext/*!*/typeCtx, PHPDocBlock.ParamTag paramTag, FormalParam syntax, CallInfo call, int paramIndex)
        {
            Contract.ThrowIfNull(typeCtx);
            Debug.Assert(paramIndex >= 0);

            TypeRefMask result = 0;
            bool isvariadic = false;

            // lookup actual type hint
            if (syntax != null)
            {
                isvariadic = syntax.IsVariadic;

                var hint = syntax.TypeHint;
                if (hint != null)
                {
                    result = typeCtx.GetTypeMaskFromTypeHint(syntax.TypeHint);

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
    }
}
