using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Descriptor of a well-known type declared in PchpCor library.
    /// </summary>
    [DebuggerDisplay("CoreType {FullName,nq}")]
    sealed class CoreType : IEquatable<CoreType>, IEquatable<TypeSymbol>
    {
        public CoreMethod Method(string name, params SpecialType[] ptypes) => new CoreMethod(this, name, ptypes);

        /// <summary>
        /// Gets full type name.
        /// </summary>
        public readonly string FullName;

        /// <summary>
        /// Gets associated symbol.
        /// </summary>
        /// <remarks>Assuming single singleton instance of pchpcor library.</remarks>
        public NamedTypeSymbol Symbol { get; private set; }

        public CoreType(string fullName)
        {
            Debug.Assert(fullName != null);
            Debug.Assert(fullName.StartsWith("Pchp.Core."));
            this.FullName = fullName;
        }

        internal void Update(NamedTypeSymbol symbol)
        {
            Contract.ThrowIfNull(symbol);
            Debug.Assert(this.Symbol == null);
            this.Symbol = symbol;
        }

        #region IEquatable

        //public override bool Equals(object obj)
        //{
        //    return base.Equals(obj);
        //}

        bool IEquatable<CoreType>.Equals(CoreType other)
        {
            return object.ReferenceEquals(this, other);
        }

        bool IEquatable<TypeSymbol>.Equals(TypeSymbol other)
        {
            return this.Symbol == other;
        }

        //public static bool operator ==(TypeSymbol s, CoreType t)
        //{
        //    return ((IEquatable<TypeSymbol>)t).Equals(s);
        //}

        //public static bool operator !=(TypeSymbol s, CoreType t)
        //{
        //    return !((IEquatable<TypeSymbol>)t).Equals(s);
        //}

        #endregion
    }

    /// <summary>
    /// Set of well-known types declared in PchpCor library.
    /// </summary>
    class CoreTypes
    {
        readonly PhpCompilation _compilation;

        public CoreTypes(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
            _table = new Dictionary<string, CoreType>();

            this.Context = Create("Context");
            this.Operators = Create("Operators");
        }

        public readonly CoreType Context;
        public readonly CoreType Operators;

        #region Table of types

        CoreType Create(string name) => CreateFromFullName("Pchp.Core." + name);

        CoreType CreateFromFullName(string fullName)
        {
            var type = new CoreType(fullName);

            _table.Add(fullName, type);

            return type;
        }

        readonly Dictionary<string, CoreType> _table;

        /// <summary>
        /// Gets well-known core type by its full CLR name.
        /// </summary>
        public CoreType GetTypeFromMetadataName(string fullName)
        {
            CoreType t;
            _table.TryGetValue(fullName, out t);
            return t;
        }

        /// <summary>
        /// Gets well-known core type by its full CLR name.
        /// </summary>
        internal CoreType Update(PENamedTypeSymbol symbol)
        {
            CoreType t = null;

            //
            if (symbol.NamespaceName != null &&
                symbol.ContainingAssembly.IsPchpCorLibrary &&
                symbol.DeclaredAccessibility == Accessibility.Public)
            {
                t = GetTypeFromMetadataName(MetadataHelpers.BuildQualifiedName(symbol.NamespaceName, symbol.Name));
                if (t != null)
                {
                    t.Update(symbol);
                }
            }

            //
            return t;
        }

        internal void Update(AssemblySymbol coreass)
        {
            Contract.ThrowIfNull(coreass);

            foreach (var t in _table.Values)
            {
                var symbol = coreass.GetTypeByMetadataName(t.FullName);
                if (symbol != null)
                    t.Update(symbol);
            }
        }

        #endregion
    }
}
