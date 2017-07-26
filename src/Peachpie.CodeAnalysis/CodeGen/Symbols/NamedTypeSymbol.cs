using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.CodeGen;

namespace Pchp.CodeAnalysis.Symbols
{
    partial class NamedTypeSymbol
    {
        /// <summary>
        /// Gets special <c>_statics</c> nested class holding static fields bound to context.
        /// </summary>
        /// <returns></returns>
        internal TypeSymbol TryGetStatics() => (TypeSymbol)(this as IPhpTypeSymbol)?.StaticsContainer;

        /// <summary>
        /// Emits load of statics holder.
        /// </summary>
        internal TypeSymbol EmitLoadStatics(CodeGenerator cg)
        {
            var statics = TryGetStatics();

            if (statics != null && statics.GetMembers().OfType<IFieldSymbol>().Any())
            {
                // Template: <ctx>.GetStatics<_statics>()
                cg.EmitLoadContext();
                return cg.EmitCall(ILOpCode.Callvirt, cg.CoreMethods.Context.GetStatic_T.Symbol.Construct(statics))
                    .Expect(statics);
            }

            return null;
        }

        /// <summary>
        /// Provides information about a method and its override.
        /// </summary>
        [DebuggerDisplay("{Method.ContainingType,nq} {Method.RoutineName,nq}")]
        internal struct OverrideInfo
        {
            /// <summary>
            /// Method to be overriden.
            /// </summary>
            public MethodSymbol Method { get; set; }

            /// <summary>
            /// Gets the routine name.
            /// </summary>
            public string RoutineName => Method.RoutineName;

            /// <summary>
            /// The method is abstract with no possible override.
            /// </summary>
            public bool IsUnresolvedAbstract => Method.IsAbstract && !HasOverride;

            /// <summary>
            /// Whether there is a possible override of <see cref="Method"/>.
            /// </summary>
            public bool HasOverride => Override != null || OverrideCandidate != null;

            /// <summary>
            /// Whether the override resolves implementation of a newly introduced interface method.
            /// </summary>
            public bool ImplementsInterface { get; set; }

            /// <summary>
            /// Metched override.
            /// </summary>
            public MethodSymbol Override { get; set; }

            /// <summary>
            /// A candidate override which signature does not match exactly the method.
            /// </summary>
            public MethodSymbol OverrideCandidate { get; set; }

            public OverrideInfo(MethodSymbol method, MethodSymbol methodoverride = null)
            {
                Debug.Assert(method != null);

                this.Method = method;

                //
                // store the override,
                // either as an override or just a candidate if the signatures are not matching.
                // In case of the candidate, a ghost stub will be generated later.
                //

                MethodSymbol overridecandidate = null;

                if (methodoverride != null)
                {
                    if (!method.SignaturesMatch(methodoverride))
                    {
                        overridecandidate = methodoverride;
                        methodoverride = null;
                    }
                }

                this.Override = methodoverride;
                this.OverrideCandidate = overridecandidate;
                this.ImplementsInterface = false;
            }
        }

        OverrideInfo[] _lazyOverrides;

        /// <summary>
        /// Matches all methods that can be overriden (non-static, public or protected, abstract or virtual)
        /// within this type sub-tree (this type, its base and interfaces)
        /// with its override.
        /// Methods without an override are either abstract or a ghost stup has to be synthesized.
        /// </summary>
        /// <param name="diagnostics"></param>
        internal OverrideInfo[] ResolveOverrides(DiagnosticBag diagnostics)
        {
            if (_lazyOverrides != null)
            {
                // already resolved
                return _lazyOverrides;
            }

            // TODO: ignore System.Object ?

            // inherit abstracts from base type
            var overrides = new List<OverrideInfo>();
            if (BaseType != null)
            {
                overrides.AddRange(BaseType.ResolveOverrides(diagnostics));
            }

            // collect this type declared methods including synthesized methods
            var methods = this.GetMembers().OfType<MethodSymbol>();
            var methodslookup = methods.Where(OverrideHelper.CanOverride).ToLookup(m => m.RoutineName);

            // resolve overrides of inherited members
            for (int i = 0; i < overrides.Count; i++)
            {
                var m = overrides[i];
                if (m.HasOverride == false)
                {
                    // update override info of the inherited member
                    overrides[i] = new OverrideInfo(m.Method, OverrideHelper.ResolveMethodImplementation(m.Method, methodslookup[m.RoutineName]));
                }
                else
                {
                    // clear the interface flag of inherited override info
                    m.ImplementsInterface = false;
                    overrides[i] = m;
                }
            }

            // resolve overrides of interface methods
            foreach (var iface in Interfaces)
            {
                if (BaseType != null && BaseType.ImplementsInterface(iface))
                {
                    // iface is already handled within overrides => skip
                    // note: iface can be ignored in metadata at all actually
                    continue;
                }

                var iface_abstracts = iface.ResolveOverrides(diagnostics);
                foreach (var m in iface_abstracts)
                {
                    if (BaseType != null && m.Method.ContainingType != iface && BaseType.ImplementsInterface(m.Method.ContainingType))
                    {
                        // iface {m.Method.ContainingType} already handled within overrides => skip
                        continue;
                    }

                    // add interface member,
                    // resolve its override
                    overrides.Add(new OverrideInfo(m.Method, OverrideHelper.ResolveMethodImplementation(m.Method, this)) { ImplementsInterface = true });
                }
            }

            // add overrideable routines from this type
            foreach (var m in methods)
            {
                if (m.IsOverrideable())
                {
                    overrides.Add(new OverrideInfo(m));
                }
            }

            // report unresolved abstracts
            if (!this.IsInterface && !this.IsAbstract)
            {
                foreach (var m in overrides)
                {
                    if (m.IsUnresolvedAbstract)
                    {
                        // TODO: diagnostics.Add()
                    }
                }
            }

            // cache & return
            return (_lazyOverrides = overrides.ToArray());
        }
    }
}
