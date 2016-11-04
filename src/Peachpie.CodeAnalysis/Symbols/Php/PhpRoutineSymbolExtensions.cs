using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class PhpRoutineSymbolExtensions
    {
        /// <summary>
        /// Gets expected return type mask of given symbol (field, function, method or property).
        /// </summary>
        /// <remarks>Returned type mask corresponds to types that can be returned by invoking given symbol.</remarks>
        public static TypeRefMask GetResultType(this IPhpValue symbol, TypeRefContext ctx)
        {
            Contract.ThrowIfNull(symbol);
            Contract.ThrowIfNull(ctx);

            TypeSymbol t;

            if (symbol is FieldSymbol)
            {
                t = ((FieldSymbol)symbol).Type;
            }
            else if (symbol is MethodSymbol)
            {
                var m = (MethodSymbol)symbol;
                var r = symbol as SourceRoutineSymbol;
                if (r != null && r.IsStatic)
                {
                    var cfg = r.ControlFlowGraph;
                    if (cfg != null && cfg.HasFlowState)
                    {
                        // In case of a static function, we can return expected return type mask exactly.
                        // Such function cannot be overriden and we know exactly what the return type will be even the CLR type covers more possibilities.
                        return ctx.AddToContext(r.FlowContext.TypeRefContext, r.TargetState.GetReturnType());
                    }
                }

                t = m.ReturnType;
            }
            else if (symbol is PropertySymbol)
            {
                t = ((PropertySymbol)symbol).Type;
            }
            else if(symbol is ParameterSymbol)
            {
                t = ((ParameterSymbol)symbol).Type;
            }
            else
            {
                throw Roslyn.Utilities.ExceptionUtilities.UnexpectedValue(symbol);
            }

            // create the type mask from the CLR type symbol
            var mask = TypeRefFactory.CreateMask(ctx, t);

            // [CastToFalse]
            if (symbol is IPhpRoutineSymbol && ((IPhpRoutineSymbol)symbol).CastToFalse)
            {
                mask |= ctx.GetBooleanTypeMask();    // the function may return FALSE
            }

            //
            return mask;
        }
    }
}
