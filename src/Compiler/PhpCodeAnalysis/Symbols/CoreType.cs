using Microsoft.CodeAnalysis;
using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Descriptor of a well-known type.
    /// </summary>
    [DebuggerDisplay("CoreType {FullName,nq}")]
    sealed class CoreType : IEquatable<CoreType>, IEquatable<TypeSymbol>
    {
        internal CoreMethod Method(string name, params CoreType[] ptypes) => new CoreMethod(this, name, ptypes);
        internal CoreConstructor Ctor(params CoreType[] ptypes) => new CoreConstructor(this, ptypes);

        /// <summary>
        /// Gets full type name.
        /// </summary>
        public readonly string FullName;

        /// <summary>
        /// Represented well known type code.
        /// </summary>
        public readonly PhpTypeCode TypeCode;

        /// <summary>
        /// Gets associated symbol.
        /// </summary>
        /// <remarks>Assuming single singleton instance of pchpcor library.</remarks>
        public NamedTypeSymbol Symbol { get; private set; }

        public bool IsNumber => TypeCode == PhpTypeCode.Double || TypeCode == PhpTypeCode.Long || TypeCode == PhpTypeCode.PhpNumber;

        public bool IsString => TypeCode == PhpTypeCode.String || TypeCode == PhpTypeCode.PhpStringBuilder || TypeCode == PhpTypeCode.ByteString;

        public CoreType(string fullName, PhpTypeCode typecode)
        {
            Debug.Assert(fullName != null);
            Debug.Assert(fullName.StartsWith("Pchp.Core.") || fullName.StartsWith("System."));
            this.FullName = fullName;
            this.TypeCode = typecode;
        }

        internal void Update(NamedTypeSymbol symbol)
        {
            Contract.ThrowIfNull(symbol);
            Debug.Assert(this.Symbol == null);
            this.Symbol = symbol;
        }

        /// <summary>
        /// Implicit cast to type symbol.
        /// </summary>
        public static implicit operator NamedTypeSymbol (CoreType t) => t.Symbol;

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
    /// Set of well-known types declared in core libraries.
    /// </summary>
    class CoreTypes
    {
        readonly PhpCompilation _compilation;

        public readonly CoreType
            Context, Operators,
            PhpNumber, PhpValue, PhpAlias,
            Void, Object, Int32, Long, Double, Boolean, String;

        public CoreTypes(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
            _table = new Dictionary<string, CoreType>();

            this.Void = Create(SpecialType.System_Void, PhpTypeCode.Void);
            this.Object = Create(SpecialType.System_Object, PhpTypeCode.Object);
            this.Int32 = Create(SpecialType.System_Int32, PhpTypeCode.Long);
            this.Long = Create(SpecialType.System_Int64, PhpTypeCode.Long);
            this.Double = Create(SpecialType.System_Double, PhpTypeCode.Double);
            this.Boolean = Create(SpecialType.System_Boolean, PhpTypeCode.Boolean);
            this.String = Create(SpecialType.System_String, PhpTypeCode.String);
            this.PhpNumber = Create("PhpNumber", PhpTypeCode.PhpNumber);
            this.PhpAlias = Create("PhpAlias", PhpTypeCode.PhpAlias);
            this.PhpValue = Create("PhpValue", PhpTypeCode.PhpValue);
            this.Context = Create("Context", PhpTypeCode.Object);
            this.Operators = Create("Operators", PhpTypeCode.Object);
        }

        #region Table of types

        readonly Dictionary<string, CoreType> _table;
        readonly Dictionary<TypeSymbol, CoreType> _typetable = new Dictionary<TypeSymbol, CoreType>();
        readonly Dictionary<SpecialType, CoreType> _specialTypes = new Dictionary<SpecialType, CoreType>();
        
        CoreType Create(string name, PhpTypeCode typecode) => CreateFromFullName("Pchp.Core." + name, typecode);

        CoreType Create(SpecialType type, PhpTypeCode typecode) => CreateFromFullName(SpecialTypes.GetMetadataName(type), typecode);
        
        CoreType CreateFromFullName(string fullName, PhpTypeCode typecode)
        {
            var type = new CoreType(fullName, typecode);

            _table.Add(fullName, type);

            return type;
        }

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
        /// Gets well-known core type by associated symbol.
        /// </summary>
        public CoreType GetTypeFromSymbol(TypeSymbol symbol)
        {
            CoreType t;
            _typetable.TryGetValue(symbol, out t);
            return t;
        }

        /// <summary>
        /// Gets special core type.
        /// </summary>
        public CoreType GetSpecialType(SpecialType type)
        {
            CoreType t;
            _specialTypes.TryGetValue(type, out t);
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
                    _typetable[symbol] = t;
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
                if (t.Symbol == null)
                {
                    var symbol = coreass.GetTypeByMetadataName(t.FullName);
                    if (symbol != null)
                    {
                        _typetable[symbol] = t;
                        t.Update(symbol);

                        if (symbol.SpecialType != SpecialType.None)
                            _specialTypes[symbol.SpecialType] = t;
                    }
                }
            }
        }

        #endregion
    }
}
