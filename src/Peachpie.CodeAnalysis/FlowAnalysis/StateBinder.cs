using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pchp.CodeAnalysis.FlowAnalysis.Visitors;
using System.Collections.Immutable;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

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

            var containingType = routine.ContainingType as SourceTypeSymbol;

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
            {
                state.SetVar(p.Name, p.GetResultType(typeCtx));

                if (p.Syntax.PassedByRef)
                {
                    state.SetVarRef(p.Name);
                }
            }

            // mark $this as initialized
            // mark global variables as ByRef, used
            // mark function parameters as used, initialized, typed
            // construct initial state for variables

            for (int i = 0; i < locals.Length; i++)
            {
                switch (locals[i].VariableKind)
                {
                    case VariableKind.GlobalVariable:
                        state.SetVarRef(i); // => used, byref, initialized
                        break;
                    case VariableKind.Parameter:
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
    }
}
