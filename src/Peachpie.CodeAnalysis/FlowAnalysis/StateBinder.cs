using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        public static FlowState CreateInitialState(SourceRoutineSymbol/*!*/routine)
        {
            Contract.ThrowIfNull(routine);

            // create typeCtx
            var typeCtx = routine.TypeRefContext;

            // create FlowContext 
            var flowCtx = new FlowContext(typeCtx, routine);

            // create FlowState
            var state = new FlowState(flowCtx);

            // handle input parameters type
            var parameters = routine.SourceParameters;
            foreach (var p in parameters)
            {
                var local = state.GetLocalHandle(p.Name);
                state.SetLocalType(local, p.GetResultType(typeCtx));

                if (p.Syntax.PassedByRef)
                {
                    state.MarkLocalByRef(local);
                }
            }

            // $this
            if (routine.HasThis)
            {
                InitThisVar(flowCtx, state);
            }

            //
            return state;
        }

        /// <summary>
        /// Initializes <c>$this</c> variable, its type and initialized state.
        /// </summary>
        private static void InitThisVar(FlowContext/*!*/ctx, FlowState/*!*/initialState)
        {
            var thisVarType = ctx.TypeRefContext.GetThisTypeMask();
            if (thisVarType.IsUninitialized)
            {
                thisVarType = TypeRefMask.AnyType;
            }

            //
            var thisHandle = ctx.GetVarIndex(VariableName.ThisVariableName);
            initialState.SetLocalType(thisHandle, thisVarType); // set $this type
            initialState.VisitLocal(thisHandle);                // mark as visited (used) to not report as unused
        }
    }
}
