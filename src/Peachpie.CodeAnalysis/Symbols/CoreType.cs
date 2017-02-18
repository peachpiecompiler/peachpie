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
        internal CoreProperty Property(string name) => new CoreProperty(this, name);
        internal CoreField Field(string name) => new CoreField(this, name);
        internal CoreOperator Operator(string name, params CoreType[] ptypes) => new CoreOperator(this, name, ptypes);
        internal CoreConstructor Ctor(params CoreType[] ptypes) => new CoreConstructor(this, ptypes);

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
            Debug.Assert(!string.IsNullOrEmpty(fullName));
            this.FullName = fullName;
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
        public static implicit operator NamedTypeSymbol(CoreType t) => t.Symbol;

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

        /// <summary>
        /// Name of attribute class representing an extension library.
        /// </summary>
        public const string PhpExtensionAttributeName = "Pchp.Core.PhpExtensionAttribute";

        /// <summary>
        /// Name of attribute class annotating trait declaration.
        /// </summary>
        public const string PhpTraitAttributeName = "Pchp.Core.PhpTraitAttribute";

        /// <summary>
        /// Full name of <c>PhpFieldsOnlyCtorAttribute</c> class.
        /// </summary>
        public const string PhpFieldsOnlyCtorAttributeName = "Pchp.Core.PhpFieldsOnlyCtorAttribute";

        public readonly CoreType
            Context, Operators, Convert, Comparison, StrictComparison,
            ScriptAttribute, PhpTraitAttribute, PhpHiddenAttribute, PhpFieldsOnlyCtorAttribute, ScriptDiedException,
            IStaticInit, RoutineInfo,
            CallBinderFactory, GetClassConstBinder, GetFieldBinder, SetFieldBinder, AccessFlags,
            PhpTypeInfoExtension, PhpTypeInfo,
            PhpNumber, PhpValue, PhpAlias, PhpString, PhpArray, PhpResource, IPhpArray, IPhpEnumerable, IPhpCallable, IPhpConvertible,
            IntStringKey, PhpHashtable,
            Void, Object, Int32, Long, Double, Boolean, String, Exception,
            RuntimeTypeHandle, RuntimeMethodHandle,
            stdClass, ArrayAccess;

        public CoreTypes(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
            _table = new Dictionary<string, CoreType>();

            Void = Create(SpecialType.System_Void);
            Object = Create(SpecialType.System_Object);
            Int32 = Create(SpecialType.System_Int32);
            Long = Create(SpecialType.System_Int64);
            Double = Create(SpecialType.System_Double);
            Boolean = Create(SpecialType.System_Boolean);
            String = Create(SpecialType.System_String);
            Exception = CreateFromFullName(WellKnownTypes.GetMetadataName(WellKnownType.System_Exception));
            RuntimeTypeHandle = Create(SpecialType.System_RuntimeTypeHandle);
            RuntimeMethodHandle = Create(SpecialType.System_RuntimeMethodHandle);

            PhpNumber = Create("PhpNumber");
            PhpAlias = Create("PhpAlias");
            PhpValue = Create("PhpValue");
            PhpString = Create("PhpString");
            PhpArray = Create("PhpArray");
            PhpResource = Create("PhpResource");
            IPhpArray = Create("IPhpArray");
            IPhpEnumerable = Create("IPhpEnumerable");
            IPhpCallable = Create("IPhpCallable");
            IPhpConvertible = Create("IPhpConvertible");
            IntStringKey = Create("IntStringKey");
            PhpHashtable = Create("PhpHashtable");
            ScriptDiedException = Create("ScriptDiedException");
            Context = Create("Context");
            Operators = Create("Operators");
            Comparison = Create("Comparison");
            StrictComparison = Create("StrictComparison");
            Convert = Create("Convert");
            ScriptAttribute = Create("ScriptAttribute");
            PhpTraitAttribute = Create("PhpTraitAttribute");
            PhpHiddenAttribute = Create("PhpHiddenAttribute");
            PhpFieldsOnlyCtorAttribute = CreateFromFullName(PhpFieldsOnlyCtorAttributeName);
            IStaticInit = Create("IStaticInit");
            RoutineInfo = Create("Reflection.RoutineInfo");
            stdClass = CreateFromFullName("stdClass");
            ArrayAccess = CreateFromFullName("ArrayAccess");

            CallBinderFactory = Create("Dynamic.CallBinderFactory");
            GetClassConstBinder = Create("Dynamic.GetClassConstBinder");
            GetFieldBinder = Create("Dynamic.GetFieldBinder");
            SetFieldBinder = Create("Dynamic.SetFieldBinder");
            AccessFlags = Create("Dynamic.AccessFlags");

            PhpTypeInfoExtension = Create("Reflection.PhpTypeInfoExtension");
            PhpTypeInfo = Create("Reflection.PhpTypeInfo");
        }

        #region Table of types

        readonly Dictionary<string, CoreType> _table;
        readonly Dictionary<TypeSymbol, CoreType> _typetable = new Dictionary<TypeSymbol, CoreType>();
        readonly Dictionary<SpecialType, CoreType> _specialTypes = new Dictionary<SpecialType, CoreType>();

        CoreType Create(string name) => CreateFromFullName("Pchp.Core." + name);

        CoreType Create(SpecialType type) => CreateFromFullName(SpecialTypes.GetMetadataName(type));

        CoreType CreateFromFullName(string fullName)
        {
            var type = new CoreType(fullName);

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

        internal void Update(AssemblySymbol coreass)
        {
            Contract.ThrowIfNull(coreass);

            foreach (var t in _table.Values)
            {
                if (t.Symbol == null)
                {
                    var mdname = MetadataTypeName.FromFullName(t.FullName, false); ;
                    var symbol = coreass.LookupTopLevelMetadataType(ref mdname, true);
                    if (symbol != null && !symbol.IsErrorType())
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
