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

            var containingType = routine.ContainingType as SourceNamedTypeSymbol;

            // collect locals
            var locals = LocalsBinder.BindLocals(routine);
            var returnIdx = locals.IndexOf(x => x.VariableKind == VariableKind.ReturnVariable);

            // create typeCtx
            var typeCtx = routine.TypeRefContext;

            // create FlowContext
            var flowCtx = new FlowContext(typeCtx, locals, returnIdx);

            // create FlowState
            var state = new FlowState(flowCtx, routine);

            // handle parameters passed by reference
            var parameters = routine.Parameters.OfType<SourceParameterSymbol>().ToImmutableArray();
            foreach (var p in parameters)
                if (p.Syntax.PassedByRef)
                    state.SetVarRef(p.Name);

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
                        var paramtag = PHPDoc.GetParamTag(routine.PHPDocBlock, paramIdx, locals[i].Name);
                        state.SetVar(i, GetParamType(typeCtx, paramtag, parameters[paramIdx].Syntax, default(CallInfo), paramIdx));
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
                    case VariableKind.ReturnVariable:
                        InitReturn(flowCtx, state, routine.PHPDocBlock);
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
        /// Sets the initial routine return type.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="initialState"></param>
        /// <param name="phpdoc"></param>
        static void InitReturn(FlowContext/*!*/ctx, FlowState/*!*/initialState, PHPDocBlock phpdoc)
        {
            Debug.Assert(ctx.ReturnVarIndex >= 0);
            if (phpdoc != null)
            {
                var returnTag = phpdoc.Returns;
                if (returnTag != null && returnTag.TypeNamesArray.Length != 0)
                {
                    initialState.SetVar(ctx.ReturnVarIndex, PHPDoc.GetTypeMask(ctx.TypeRefContext, returnTag.TypeNamesArray));
                }
            }
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
            bool isalias = false;

            // lookup actual type hint
            if (syntax != null)
            {
                isvariadic = syntax.IsVariadic;
                isalias = syntax.PassedByRef || syntax.IsOut;

                var hint = syntax.TypeHint;
                if (hint != null)
                {
                    result = typeCtx.GetTypeMaskFromTypeHint(syntax.TypeHint);

                    if (isvariadic) // PHP 5.6 variadic parameter (...) // TypeHint -> TypeHint[]
                        result = typeCtx.GetArrayTypeMask(result);
                }
            }

            if (result.IsUninitialized)
            {
                // lookup callInfo
                result = call.GetParamType(typeCtx, paramIndex);
                if (result.IsUninitialized)
                {
                    // lookup PHPDoc
                    if (paramTag != null && paramTag.TypeNamesArray.Length != 0)
                        result = PHPDoc.GetTypeMask(typeCtx, paramTag.TypeNamesArray);

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
            result.IsRef = isalias;

            //
            Debug.Assert(!result.IsUninitialized);
            return result;
        }
    }
}
