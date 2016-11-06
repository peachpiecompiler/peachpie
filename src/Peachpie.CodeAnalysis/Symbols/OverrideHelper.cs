using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;
using Pchp.Core.Dynamic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Helper class resolving overriden method.
    /// </summary>
    internal static class OverrideHelper
    {
        /// <summary>
        /// Resolves best method to be overriden.
        /// </summary>
        /// <param name="method">The override.</param>
        /// <returns>Candidate to be overriden by given <paramref name="method"/>.</returns>
        public static MethodSymbol ResolveOverride(this SourceMethodSymbol method)
        {
            Contract.ThrowIfNull(method);

            if (method.IsStatic || method.DeclaredAccessibility == Accessibility.Private)
            {
                return null;    // static or private methods can't be overrides
            }

            //
            var bestCost = ConversionCost.Error;
            MethodSymbol bestCandidate = null;

            // enumerate types in descending order and
            // find best candidate to be overriden

            foreach (var t in EnumerateOverridableTypes(method.ContainingType))
            {
                foreach (var m in t.GetMembers(method.Name).OfType<MethodSymbol>())
                {
                    var cost = OverrideCost(method, m);
                    if (cost < bestCost && IsAllowedCost(cost))
                    {
                        bestCost = cost;
                        bestCandidate = m;

                        if (cost == ConversionCost.Pass)
                        {
                            return bestCandidate;
                        }
                    }
                }
            }


            //
            return bestCandidate;
        }

        /// <summary>
        /// Enumerates base types and interfaces of given type (i.e. types that can contain methods that can be overriden).
        /// </summary>
        static IEnumerable<NamedTypeSymbol> EnumerateOverridableTypes(NamedTypeSymbol type)
        {
            Debug.Assert(type != null);
            
            for (var t = type.BaseType; t != null; t = t.BaseType)
            {
                yield return t;
            }

            //
            foreach (var t in type.AllInterfaces)
            {
                yield return t;
            }
        }

        static bool IsAllowedCost(ConversionCost cost) => cost < ConversionCost.MissingArgs;

        /// <summary>
        /// Calculates override cost, i.e. whether the override is possible and its value.
        /// In case of more possible overrides, the one with better cost is selected.
        /// </summary>
        /// <param name="method">Source method.</param>
        /// <param name="basemethod">A hypothetical base method.</param>
        /// <returns></returns>
        public static ConversionCost OverrideCost(SourceMethodSymbol method, MethodSymbol basemethod)
        {
            Contract.ThrowIfNull(method);
            Contract.ThrowIfNull(basemethod);

            //
            if (method.IsStatic || basemethod.IsStatic || basemethod.IsSealed ||
                (!basemethod.IsVirtual && !basemethod.IsAbstract) ||    // not abstract or virtual
                method.Name.EqualsOrdinalIgnoreCase(basemethod.Name) == false ||
                method.DeclaredAccessibility == Accessibility.Private || basemethod.DeclaredAccessibility == Accessibility.Private)
            {
                return ConversionCost.NoConversion;
            }

            if (method.ReturnType != basemethod.ReturnType)
            {
                return ConversionCost.ImplicitCast;
            }

            //
            var ps = method.Parameters;
            var psbase = basemethod.Parameters;

            //
            var result = ConversionCost.Pass;

            // NOTE: there shouldn't be any implicit parameters (Context and LateBoundType are known from this instance)

            for (int i = 0; i < ps.Length; i++)
            {
                if (i < psbase.Length)
                {
                    var p = ps[i];
                    var pbase = psbase[i];

                    if (p.Type != pbase.Type)
                    {
                        if (p.Type.IsOfType(pbase.Type))
                        {
                            result |= ConversionCost.ImplicitCast;
                        }
                        else
                        {
                            result |= ConversionCost.NoConversion;
                        }
                    }
                }
                else
                {
                    result |= ConversionCost.TooManyArgs;
                }
            }

            //
            if (ps.Length < psbase.Length)
            {
                result |= ConversionCost.MissingArgs;
            }

            //
            return result;
        }

        /// <summary>
        /// Determines whether <paramref name="method"/> can override <paramref name="basemethod"/>.
        /// </summary>
        /// <param name="method">Source method.</param>
        /// <param name="basemethod">Overriden method.</param>
        public static bool CanBeOverride(SourceMethodSymbol method, MethodSymbol basemethod)
        {
            return IsAllowedCost(OverrideCost(method, basemethod));
        }

        /// <summary>
        /// Checks whether signatures of two methods match exactly so one can override the second.
        /// </summary>
        public static bool SignaturesMatch(this MethodSymbol a, MethodSymbol b)
        {
            Contract.ThrowIfNull(a);
            Contract.ThrowIfNull(b);

            if (a.ReturnType != b.ReturnType)
            {
                return false;
            }

            var ps1 = a.Parameters;
            var ps2 = b.Parameters;

            if (ps1.Length != ps2.Length)
            {
                return false;
            }

            for (int i = 0; i < ps1.Length; i++)
            {
                var p1 = ps1[i];
                var p2 = ps2[i];

                if (p1.Type != p2.Type || p1.RefKind != p2.RefKind)
                {
                    return false;
                }
            }

            //
            return true;
        }
    }
}
