using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
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
                    // In case of a static function, we can return expected return type mask exactly.
                    // Such function cannot be overriden and we know exactly what the return type will be even the CLR type covers more possibilities.
                    return ctx.AddToContext(r.TypeRefContext, r.ResultTypeMask);
                }

                t = m.ReturnType;
            }
            else if (symbol is PropertySymbol)
            {
                t = ((PropertySymbol)symbol).Type;
            }
            else if (symbol is ParameterSymbol)
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

        /// <summary>
        /// Resolves list of input arguments.
        /// Implicit parameters passed by compiler are ignored.
        /// </summary>
        /// <param name="routine">Routine.</param>
        /// <param name="ctx">TYpe context to transmer type masks into.</param>
        /// <returns>List of input PHP arguments.</returns>
        public static PhpParam[] GetExpectedArguments(this IPhpRoutineSymbol routine, TypeRefContext ctx)
        {
            Contract.ThrowIfNull(routine);

            var ps = routine.Parameters;
            var table = (routine as SourceRoutineSymbol)?.LocalsTable;
            var result = new List<PhpParam>(ps.Length);

            foreach (ParameterSymbol p in ps)
            {
                if (result.Count == 0 && p.IsImplicitlyDeclared)
                {
                    continue;
                }

                // default value (bound expression)
                ConstantValue cvalue;
                var psrc = p as SourceParameterSymbol;
                var defaultexpr = psrc != null
                    ? psrc.Initializer
                    : ((cvalue = p.ExplicitDefaultConstantValue) != null ? new BoundLiteral(cvalue.Value) : null);

                //
                result.Add(new PhpParam(TypeRefFactory.CreateMask(ctx, p.Type), p.IsParams, defaultexpr));
            }

            //
            return result.ToArray();
        }
    }
}
