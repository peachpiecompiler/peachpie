using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Emit
{
    /// <summary>
    /// Manages synthesized symbols in the module builder.
    /// </summary>
    internal class SynthesizedManager
    {
        readonly PEModuleBuilder _module;

        public PhpCompilation DeclaringCompilation => _module.Compilation;

        readonly Dictionary<TypeSymbol, List<Symbol>> _membersByType = new Dictionary<TypeSymbol, List<Symbol>>();

        public SynthesizedManager(PEModuleBuilder module)
        {
            Contract.ThrowIfNull(module);

            _module = module;
        }

        #region Synthesized Members

        List<Symbol> EnsureList(TypeSymbol type)
        {
            List<Symbol> list;
            if (!_membersByType.TryGetValue(type, out list))
            {
                _membersByType[type] = list = new List<Symbol>();
            }

            //
            return list;
        }

        /// <summary>
        /// Gets or initializes static constructor symbol.
        /// </summary>
        public MethodSymbol/*!*/EnsureStaticCtor(TypeSymbol container)
        {
            Contract.ThrowIfNull(container);

            //if (container is NamedTypeSymbol)
            //{
            //    var cctors = ((NamedTypeSymbol)container).StaticConstructors;
            //    if (!cctors.IsDefaultOrEmpty)
            //    {
            //        return cctors[0];
            //    }
            //}

            //
            var members = EnsureList(container);

            //
            var cctor = members.OfType<SynthesizedCctorSymbol>().FirstOrDefault();
            if (cctor == null)
            {
                cctor = new SynthesizedCctorSymbol(container);
                members.Add(cctor);
            }

            //
            return cctor;
        }

        /// <summary>
        /// Creates synthesized field.
        /// </summary>
        public SynthesizedFieldSymbol/*!*/GetOrCreateSynthesizedField(NamedTypeSymbol container, TypeSymbol type, string name, Accessibility accessibility, bool isstatic, bool @readonly)
        {
            var members = EnsureList(container);

            var field = members
                .OfType<SynthesizedFieldSymbol>()
                .FirstOrDefault(f => f.Name == name && f.IsStatic == isstatic && f.Type == type && f.IsReadOnly == @readonly);

            if (field == null)
            {
                field = new SynthesizedFieldSymbol(container, type, name, accessibility, isstatic, @readonly);
                members.Add(field);
            }

            Debug.Assert(field.IsImplicitlyDeclared);

            return field;
        }

        /// <summary>
        /// Adds a type member to the class.
        /// </summary>
        /// <param name="container">Containing type.</param>
        /// <param name="nestedType">Type to be added as nested type.</param>
        public void AddNestedType(TypeSymbol container, NamedTypeSymbol nestedType)
        {
            Contract.ThrowIfNull(nestedType);
            Debug.Assert(nestedType.IsImplicitlyDeclared);
            Debug.Assert(container.ContainingType == null); // can't nest in nested type

            EnsureList(container).Add(nestedType);
        }

        /// <summary>
        /// Adds a synthedized method to the class.
        /// </summary>
        public void AddMethod(TypeSymbol container, MethodSymbol method)
        {
            Contract.ThrowIfNull(method);
            Debug.Assert(method.IsImplicitlyDeclared);

            EnsureList(container).Add(method);
        }

        /// <summary>
        /// Gets synthezised members contained in <paramref name="container"/>.
        /// </summary>
        /// <typeparam name="T">Type of members to enumerate.</typeparam>
        /// <param name="container">Containing type.</param>
        /// <returns>Enumeration of synthesized type members.</returns>
        public IEnumerable<T> GetMembers<T>(TypeSymbol container) where T : ISymbol
        {
            List<Symbol> list;
            if (_membersByType.TryGetValue(container, out list) && list.Count != 0)
            {
                return list.OfType<T>();
            }
            else
            {
                return ImmutableArray<T>.Empty;
            }
        }

        #endregion
    }
}
