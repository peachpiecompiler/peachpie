using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Devsense.PHP.Syntax;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Helper class resolving overriden method.
    /// </summary>
    internal static class OverrideHelper
    {
        /// <summary>
        /// Conversion value used for overload resolution.
        /// </summary>
        [Flags]
        public enum ConversionCost : ushort
        {
            /// <summary>
            /// No conversion is needed. Best case.
            /// </summary>
            Pass = 0,

            /// <summary>
            /// The operation is costly but the value is kept without loosing precision.
            /// </summary>
            PassCostly = 1,

            /// <summary>
            /// Conversion using implicit cast without loosing precision.
            /// </summary>
            ImplicitCast = 2,

            /// <summary>
            /// Conversion using explicit cast that may loose precision.
            /// </summary>
            LoosingPrecision = 4,

            /// <summary>
            /// Conversion is possible but the value is lost and warning should be generated.
            /// </summary>
            Warning = 8,

            /// <summary>
            /// Implicit value will be used, argument is missing and parameter is optional.
            /// </summary>
            DefaultValue = 16,

            /// <summary>
            /// Too many arguments provided. Arguments will be omitted.
            /// </summary>
            TooManyArgs = 32,

            /// <summary>
            /// Missing mandatory arguments, default values will be used instead.
            /// </summary>
            MissingArgs = 64,

            /// <summary>
            /// Conversion does not exist.
            /// </summary>
            NoConversion = 128,

            /// <summary>
            /// Unspecified error.
            /// </summary>
            Error = 256,
        }

        /// <summary>
        /// Resolves best method to be overriden.
        /// </summary>
        /// <param name="method">The override.</param>
        /// <returns>Candidate to be overriden by given <paramref name="method"/>.</returns>
        public static MethodSymbol ResolveOverride(this MethodSymbol method)
        {
            Contract.ThrowIfNull(method);

            if (method.IsStatic || method.DeclaredAccessibility == Accessibility.Private)
            {
                return null;    // static or private methods can't be overrides
            }

            if (method.IsAbstract && method.ContainingType.IsInterface)
            {
                return null;    // interface member does not override
            }

            //
            var bestCost = ConversionCost.Error;
            MethodSymbol bestCandidate = null;

            // enumerate types in descending order and
            // find best candidate to be overriden

            // once a type defines method with same name, we have to ignore all its overriden methods (they are overriden already)

            var overriden = new HashSet<MethodSymbol>();    // set of methods we will ignore, they are already overriden

            foreach (var t in EnumerateOverridableTypes(method.ContainingType))
            {
                Debug.Assert(t != method.ContainingType); // cannot override a member on the same type

                foreach (var m in t.GetMembersByPhpName(method.Name).OfType<MethodSymbol>())
                {
                    if (overriden.Contains(m))
                    {
                        continue;
                    }

                    if (IsOverrideable(m))
                    {
                        if (t.IsInterface && overriden.Count != 0) // method on interface that might be already implemented
                        {
                            if (overriden.Any(o => o.SignaturesMatch(m)))
                            {
                                continue; // already implemented in a class
                            }
                        }

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

                    // already overriden methods cannot be overriden again:
                    for (var mbase = m; mbase != null; mbase = (MethodSymbol)mbase.OverriddenMethod)
                    {
                        if (!overriden.Add(mbase))
                        {
                            // already added,
                            // the rest of methods as well
                            break;
                        }
                    }
                }
            }

            //
            return bestCandidate;
        }

        internal static bool IsExplicitInterfaceImplementation(this MethodSymbol method, MethodSymbol basemethod)
        {
            if (method.DeclaredAccessibility == Accessibility.Private && method.ExplicitInterfaceImplementations.Contains(basemethod))
            {
                Debug.Assert(SignaturesMatch(method, basemethod, ignorename: true));
                return true;
            }
            else
            {
                return false;
            }
        }

        public static MethodSymbol ResolveMethodImplementation(this MethodSymbol method, ImmutableArray<Symbol> overridecandidates)
        {
            if (overridecandidates.IsDefaultOrEmpty)
            {
                return null;
            }

            var bestCost = ConversionCost.Error;
            MethodSymbol bestCandidate = null;

            foreach (var s in overridecandidates)
            {
                var c = s as MethodSymbol;
                if (c == null)
                {
                    continue; // not a method
                }

                if (c.IsExplicitInterfaceImplementation(method))
                {
                    // we've found explicit override
                    return c;
                }

                if (c.CanOverride())
                {
                    var cost = OverrideCost(c, method);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestCandidate = c;

                        if (cost == ConversionCost.Pass)
                        {
                            break;
                        }
                    }
                }
            }

            //
            return bestCandidate;
        }

        public static MethodSymbol ResolveMethodImplementation(MethodSymbol method, NamedTypeSymbol type)
        {
            // ignoring System.Object (we don't override its methods from PHP)
            bool ignoreSystemObject = method.ContainingType.IsPhpType();

            for (; type != null; type = type.BaseType)
            {
                if (ignoreSystemObject && type.SpecialType == SpecialType.System_Object)
                {
                    break;
                }

                var resolved = ResolveMethodImplementation(method, type.GetMembers());
                if (resolved != null)
                {
                    return resolved;
                }
            }

            //
            return null;
        }

        /// <summary>
        /// Enumerates base types and interfaces of given type (i.e. types that can contain methods that can be overriden).
        /// </summary>
        static IEnumerable<NamedTypeSymbol> EnumerateOverridableTypes(NamedTypeSymbol type)
        {
            Debug.Assert(type != null);

            // ignoring System.Object (we don't override its methods from PHP)

            for (var t = type.BaseType; t != null && t.SpecialType != SpecialType.System_Object; t = t.BaseType)
            {
                yield return t;
            }

            //yield return from type.AllInterfaces; // NOTE: this returns too much interfaces, we only needs the once that can introduce an abstract member:

            //
            // check interfaces which can introduce an abstract member only:
            //

            Queue<NamedTypeSymbol> typesWithInterfaces = null; // remember abstract types which interfaces have to be returned as well

            for (var t = type; t != null && t.SpecialType != SpecialType.System_Object; t = t.BaseType)
            {
                if (t.IsAbstract || t == type)
                {
                    if (t.Interfaces.IsDefaultOrEmpty)
                    {
                        continue;
                    }

                    if (typesWithInterfaces == null)
                    {
                        typesWithInterfaces = new Queue<NamedTypeSymbol>();
                    }

                    typesWithInterfaces.Enqueue(t);
                }
                else
                {
                    // we don't have to check interfaces,
                    // all the virtual members from interface were already implemented
                    break;
                }
            }

            if (typesWithInterfaces != null)
            {
                var set = new HashSet<NamedTypeSymbol>();

                // recursively get all interfaces from abstract types

                while (typesWithInterfaces.Count != 0)
                {
                    var t = typesWithInterfaces.Dequeue();
                    if (set.Add(t))
                    {
                        t.Interfaces.ForEach(typesWithInterfaces.Enqueue);
                    }
                }

                foreach (var t in set)
                {
                    if (t.IsInterface && t != type) // take only interfaces from the set of all base types
                    {
                        yield return t;
                    }
                }
            }
        }

        public static bool IsOverrideable(this MethodSymbol method)
        {
            return !method.IsStatic && !method.IsSealed && method.DeclaredAccessibility != Accessibility.Private && (method.IsVirtual || method.IsAbstract || method.IsOverride);
        }

        /// <summary>
        /// Gets value indicating the given method can override another method.
        /// </summary>
        public static bool CanOverride(this MethodSymbol method)
        {
            return !method.IsStatic && method.DeclaredAccessibility != Accessibility.Private;
        }

        static bool IsAllowedCost(ConversionCost cost) => cost < ConversionCost.NoConversion;

        /// <summary>
        /// Calculates override cost, i.e. whether the override is possible and its value.
        /// In case of more possible overrides, the one with better cost is selected.
        /// </summary>
        public static ConversionCost OverrideCost(ImmutableArray<ParameterSymbol> givenps, ImmutableArray<ParameterSymbol> baseps)
        {
            //
            var result = ConversionCost.Pass;

            // NOTE: there shouldn't be any implicit parameters (Context and LateBoundType are known from this instance)

            for (int i = 0; i < givenps.Length; i++)
            {
                if (i < baseps.Length)
                {
                    var p = givenps[i];
                    var pbase = baseps[i];

                    if (p.Type == pbase.Type)
                    {
                        result |= ConversionCost.Pass; // 0
                    }
                    else if (p.Type.Is_PhpValue() || p.Type.Is_PhpAlias())
                    {
                        result |= ConversionCost.ImplicitCast;
                    }
                    else if (p.Type.IsOfType(pbase.Type))
                    {
                        result |= ConversionCost.PassCostly;
                    }
                    else
                    {
                        // TODO: conversions between int, long, double
                        result |= ConversionCost.NoConversion;
                    }
                }
                else
                {
                    result |= ConversionCost.TooManyArgs;
                    break;
                }
            }

            for (int i = givenps.Length; i < baseps.Length; i++)
            {
                if (baseps[i].IsOptional || baseps[i].Initializer != null)
                {
                    result |= ConversionCost.DefaultValue;
                }
                else
                {
                    result |= ConversionCost.MissingArgs;
                }
            }

            //
            return result;
        }

        /// <summary>
        /// Calculates override cost, i.e. whether the override is possible and its value.
        /// In case of more possible overrides, the one with better cost is selected.
        /// </summary>
        /// <param name="method">Source method.</param>
        /// <param name="basemethod">A hypothetical base method.</param>
        /// <returns></returns>
        static ConversionCost OverrideCost(MethodSymbol method, MethodSymbol basemethod)
        {
            Contract.ThrowIfNull(method);
            Contract.ThrowIfNull(basemethod);

            //
            if (!string.Equals(method.RoutineName, basemethod.RoutineName, StringComparison.InvariantCultureIgnoreCase))
            {
                return ConversionCost.Error;
            }

            if (!basemethod.IsOverrideable() ||
                !method.CanOverride())
            {
                return ConversionCost.Error;
            }

            //if (method.ReturnType != basemethod.ReturnType)   // the return type is not important for the override cost
            //{
            //    return ConversionCost.ImplicitCast;
            //}

            return OverrideCost(method.Parameters, basemethod.Parameters);
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
        public static bool SignaturesMatch(this MethodSymbol a, MethodSymbol b, bool ignorename = false)
        {
            Contract.ThrowIfNull(a);
            Contract.ThrowIfNull(b);

            if (a.ReturnType != b.ReturnType || a.RefKind != b.RefKind)
            {
                return false;
            }

            if (a.Name != b.Name && !ignorename)
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

        public static PropertySymbol ResolveOverridenMember(this PropertySymbol p)
        {
            // TODO: use implementation of OverriddenOrHiddenMembersHelpers

            if (p.IsOverride)
            {
                for (var t = p.ContainingType.BaseType; t != null; t = t.BaseType)
                {
                    var candidates = t.GetMembers(p.Name).OfType<PropertySymbol>();
                    foreach (var c in candidates)
                    {
                        if (!c.IsSealed && !c.IsStatic)
                        {
                            if (c.IsVirtual || c.IsAbstract || c.IsOverride)
                            {
                                return c;
                            }
                        }
                    }
                }
            }
            //
            return null;
        }
    }
}
