using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.FlowAnalysis;
using Pchp.CodeAnalysis.Semantics;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// List of overloads for a function call.
    /// </summary>
    internal class OverloadsList
    {
        /// <summary>
        /// Defines the scope of members visibility.
        /// Used to resolve visibility of called methods and accessed properties.
        /// </summary>
        public struct VisibilityScope
        {
            /// <summary>
            /// The type scope if resolved.
            /// Can be <c>null</c> when outside of class or when scope is unknown in compile-time.
            /// </summary>
            public NamedTypeSymbol Scope;

            /// <summary>
            /// Whether the scope can change.
            /// In result visibility of private and protected members may change in runtime. 
            /// </summary>
            public bool ScopeIsDynamic;

            /// <summary>
            /// Builds the visibility scope.
            /// </summary>
            public static VisibilityScope Create(NamedTypeSymbol self, SourceRoutineSymbol routine)
            {
                return new VisibilityScope()
                {
                    Scope = self,
                    ScopeIsDynamic = self.IsTraitType() || routine is SourceLambdaSymbol || (routine?.IsGlobalScope == true),
                };
            }
        }

        readonly List<MethodSymbol> _methods;

        public OverloadsList(params MethodSymbol[] methods)
        {
            _methods = new List<MethodSymbol>(methods);
        }

        /// <summary>
        /// Tries to resolve method in design time.
        /// </summary>
        /// <returns>
        /// Might return one of following:
        /// - resolved single <see cref="MethodSymbol"/>
        /// - <see cref="MissingMethodSymbol"/>
        /// - <see cref="AmbiguousMethodSymbol"/>
        /// - <see cref="InaccessibleMethodSymbol"/>
        /// </returns>
        public MethodSymbol/*!*/Resolve(TypeRefContext typeCtx, ImmutableArray<BoundArgument> args, VisibilityScope scope)
        {
            if (_methods.Count == 0)
            {
                return new MissingMethodSymbol();
            }

            if (_methods.Count == 1 && _methods[0].IsErrorMethodOrNull())
            {
                return _methods[0] ?? new MissingMethodSymbol();
            }

            // see Pchp.Core.Dynamic.OverloadBinder

            // collect valid methods:
            var result = new List<MethodSymbol>(_methods.Where(MethodSymbolExtensions.IsValidMethod));

            // only visible methods:
            RemoveInaccessible(result, scope);

            if (result.Count == 0)
            {
                return new InaccessibleMethodSymbol(_methods.AsImmutable());
            }

            if (scope.ScopeIsDynamic && result.Any(IsNonPublic))
            {
                // we have to postpone the resolution to runtime:
                return new AmbiguousMethodSymbol(result.AsImmutable(), false);
            }

            if (result.Count == 1)
            {
                return result[0];
            }

            // TODO: cost of args convert operation

            // by params count

            var result2 = new List<MethodSymbol>();

            foreach (var m in result)
            {
                var nmandatory = 0;
                var hasoptional = false;
                var hasparams = false;
                var match = true;
                var hasunpacking = false;

                var expectedparams = m.GetExpectedArguments(typeCtx);

                foreach (var p in expectedparams)
                {
                    hasoptional |= p.DefaultValue != null;
                    hasparams |= p.IsVariadic;
                    if (!hasoptional && !hasparams) nmandatory++;

                    if (p.Index < args.Length)
                    {
                        hasunpacking |= args[p.Index].IsUnpacking;

                        // TODO: check args[i] is convertible to p.Type
                        var p_type = typeCtx.WithoutNull(p.Type);
                        var a_type = typeCtx.WithoutNull(args[p.Index].Value.TypeRefMask);

                        match &= a_type == p_type && !hasunpacking; // check types match (ignoring NULL flag)
                    }
                }

                //
                if ((args.Length >= nmandatory || hasunpacking) && (hasparams || args.Length <= expectedparams.Length))
                {
                    // TODO: this is naive implementation of overload resolution,
                    // make it properly using Conversion Cost
                    if (match && !hasparams)
                    {
                        return m;   // perfect match
                    }

                    //
                    result2.Add(m);
                }
            }

            //
            return (result2.Count == 1) ? result2[0] : new AmbiguousMethodSymbol(result.AsImmutable(), true);
        }

        static bool IsNonPublic(MethodSymbol m) => m.DeclaredAccessibility != Accessibility.Public;

        /// <summary>
        /// Removes methods that are inaccessible for sure.
        /// </summary>
        static void RemoveInaccessible(List<MethodSymbol> methods, VisibilityScope scope)
        {
            for (int i = methods.Count - 1; i >= 0; i--)
            {
                var m = methods[i];

                if ((!scope.ScopeIsDynamic && !m.IsAccessible(scope.Scope)) ||  // method is not accessible for sure
                    m.IsFieldsOnlyConstructor())    // method is special .ctor which is not accessible from user's code
                {
                    methods.RemoveAt(i);
                }
            }
        }
    }
}
