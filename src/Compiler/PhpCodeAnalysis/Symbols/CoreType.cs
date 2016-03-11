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
    class CoreType : IEquatable<CoreType>
    {
        internal static CoreType FromFullName(string fullName) => new CoreType(fullName);
        internal static CoreType FromName(string name) => FromFullName("Pchp.Core." + name);

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

            //
            CoreTypes.RegisterCoreType(this);
        }

        internal void Update(NamedTypeSymbol symbol)
        {
            Contract.ThrowIfNull(symbol);
            Debug.Assert(this.Symbol == null);
            this.Symbol = symbol;
        }

        #region IEquatable<CoreType>

        bool IEquatable<CoreType>.Equals(CoreType other)
        {
            return object.ReferenceEquals(this, other);
        }

        #endregion
    }

    /// <summary>
    /// Set of well-known types declared in PchpCor library.
    /// </summary>
    static class CoreTypes
    {
        // TODO: bind to core library instead of static

        public static readonly CoreType Context = CoreType.FromName("Context");
        public static readonly CoreType Operators = CoreType.FromName("Operators");

        #region Table of types

        static Dictionary<string, CoreType> _table;
        internal static void RegisterCoreType(CoreType t)
        {
            Contract.ThrowIfNull(t);

            if (_table == null)
                _table = new Dictionary<string, CoreType>();

            _table.Add(t.FullName, t);
        }

        /// <summary>
        /// Gets well-known core type by its full CLR name.
        /// </summary>
        public static CoreType GetTypeFromMetadataName(string fullName)
        {
            CoreType t;
            _table.TryGetValue(fullName, out t);
            return t;
        }

        /// <summary>
        /// Gets well-known core type by its full CLR name.
        /// </summary>
        internal static CoreType Update(PENamedTypeSymbol symbol)
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

        #endregion
    }
}
