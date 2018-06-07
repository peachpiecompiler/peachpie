using Devsense.PHP.Syntax;
using Pchp.CodeAnalysis.Symbols;

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
            foreach (var p in routine.SourceParameters)
            {
                var local = state.GetLocalHandle(new VariableName(p.Name));
                var ptype = p.GetResultType(typeCtx);
                if (p.IsNotNull)
                {
                    // remove 'null' type from the mask,
                    // it cannot be null
                    ptype = typeCtx.WithoutNull(ptype);
                }

                state.SetLocalType(local, ptype);

                if (p.Syntax.PassedByRef && !p.Syntax.IsVariadic)
                {
                    state.MarkLocalByRef(local);
                }
            }

            // $this
            if (routine.GetPhpThisVariablePlace() != null)
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
