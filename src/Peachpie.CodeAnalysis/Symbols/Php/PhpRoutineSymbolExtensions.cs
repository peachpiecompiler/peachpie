using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class PhpRoutineSymbolExtensions
    {
        /// <summary>
        /// Constructs most appropriate CLR return type of given routine.
        /// The method handles returning by alias, PHP7 return type, PHPDoc @return tag and result of flow analysis.
        /// In case the routine is an override or can be overriden, the CLR type is a value.
        /// </summary>
        public static TypeSymbol ConstructClrReturnType(SourceRoutineSymbol routine)
        {
            var compilation = routine.DeclaringCompilation;

            // &
            if (routine.SyntaxSignature.AliasReturn)
            {
                return compilation.CoreTypes.PhpAlias;
            }

            // : return type
            if (routine.SyntaxReturnType != null)
            {
                return compilation.GetTypeFromTypeRef(routine.SyntaxReturnType);
            }

            // for non virtual methods:
            if (routine.IsStatic || routine.DeclaredAccessibility == Accessibility.Private || (routine.IsSealed && !routine.IsOverride))
            {
                // /** @return */
                var typeCtx = routine.TypeRefContext;
                if (routine.PHPDocBlock != null && (compilation.Options.PhpDocTypes & PhpDocTypes.ReturnTypes) != 0)
                {
                    var returnTag = routine.PHPDocBlock.Returns;
                    if (returnTag != null && returnTag.TypeNames.Length != 0)
                    {
                        var tmask = PHPDoc.GetTypeMask(typeCtx, returnTag.TypeNamesArray, routine.GetNamingContext());
                        if (!tmask.IsVoid && !tmask.IsAnyType)
                        {
                            return compilation.GetTypeFromTypeRef(typeCtx, tmask);
                        }
                    }
                }

                // determine from code flow
                return compilation.GetTypeFromTypeRef(typeCtx, routine.ResultTypeMask);
            }
            else
            {
                // TODO: an override that respects the base? check routine.ResultTypeMask (flow analysis) and base implementation is it matches
            }

            // any value by default
            return compilation.CoreTypes.PhpValue;
        }

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
                if (r != null && r.IsStatic && r.SyntaxReturnType == null)
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
                var ps = (ParameterSymbol)symbol;
                t = ps.Type;

                if (ps.IsParams)
                {
                    Debug.Assert(t.IsSZArray());
                    return ctx.GetArrayTypeMask(TypeRefFactory.CreateMask(ctx, ((ArrayTypeSymbol)t).ElementType));
                }
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

            int index = 0;

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
                var phpparam = new PhpParam(
                    index++,
                    TypeRefFactory.CreateMask(ctx, p.Type),
                    p.RefKind != RefKind.None,
                    p.IsParams,
                    defaultexpr);

                result.Add(phpparam);
            }

            //
            return result.ToArray();
        }

        /// <summary>
        /// Gets additional flags of the caller routine.
        /// </summary>
        public static RoutineFlags InvocationFlags(this IPhpRoutineSymbol routine)
        {
            RoutineFlags f = RoutineFlags.None;

            var ps = routine.Parameters;
            foreach (var p in ps)
            {
                if (p.IsImplicitlyDeclared)
                {
                    if (SpecialParameterSymbol.IsLocalsParameter(p))
                    {
                        f |= RoutineFlags.UsesLocals;
                    }
                    else if (SpecialParameterSymbol.IsCallerArgsParameter(p))
                    {
                        f |= RoutineFlags.UsesArgs;
                    }
                }
                else
                {
                    break;
                }
            }

            return f;
        }
    }
}
