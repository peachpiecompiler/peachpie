using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Descriptor of a well-known type.
    /// </summary>
    [DebuggerDisplay("CoreType {FullName}")]
    sealed class CoreType : IEquatable<CoreType>, IEquatable<TypeSymbol>
    {
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

    static class CoreTypeExtensions
    {
        public static CoreMethod Method(this CoreType type, string name, params CoreType[] ptypes) => new CoreMethod(type, name, ptypes);
        public static CoreProperty Property(this CoreType type, string name) => new CoreProperty(type, name);
        public static CoreField Field(this CoreType type, string name) => new CoreField(type, name);
        public static CoreOperator Operator(this CoreType type, string name, params CoreType[] ptypes) => new CoreOperator(type, name, ptypes);
        public static CoreConstructor Ctor(this CoreType type, params CoreType[] ptypes) => new CoreConstructor(type, ptypes);
        public static CoreCast CastImplicit(this CoreType type, CoreType target) => new CoreCast(type, target, false);
    }

    /// <summary>
    /// Set of well-known types declared in core libraries.
    /// </summary>
    class CoreTypes
    {
        readonly PhpCompilation _compilation;

        /// <summary>
        /// Root namespace for Peachpie Runtime types.
        /// </summary>
        public const string PeachpieRuntimeNamespace = "Pchp.Core";

        /// <summary>
        /// Name of attribute class representing an extension library.
        /// </summary>
        public const string PhpExtensionAttributeFullName = PeachpieRuntimeNamespace + ".PhpExtensionAttribute";

        /// <summary>
        /// Name of attribute class representing a PHP type descriptor.
        /// </summary>
        public const string PhpTypeAttributeFullName = PeachpieRuntimeNamespace + ".PhpTypeAttribute";

        /// <summary>
        /// Name of attribute class representing a script type.
        /// </summary>
        public const string PhpScriptAttributeFullName = PeachpieRuntimeNamespace + ".ScriptAttribute";

        /// <summary>
        /// Name of attribute class representing a PHAR archive script type.
        /// </summary>
        public const string PharAttributeFullName = PeachpieRuntimeNamespace + ".PharAttribute";

        /// <summary>
        /// Name of attribute class representing target PHP language specification.
        /// </summary>
        public const string TargetPhpLanguageAttributeFullName = PeachpieRuntimeNamespace + ".TargetPhpLanguageAttribute";

        /// <summary>
        /// Full name of Context+DllLoader&lt;&gt;.
        /// </summary>
        public const string Context_DllLoader_T = PeachpieRuntimeNamespace + ".Context+DllLoader`1";

        /// <summary>
        /// Name of attribute class annotating trait declaration.
        /// </summary>
        public const string PhpTraitAttributeName = "PhpTraitAttribute";

        /// <summary>
        /// Name of <c>PhpFieldsOnlyCtorAttribute</c> class.
        /// </summary>
        public const string PhpFieldsOnlyCtorAttributeName = "PhpFieldsOnlyCtorAttribute";

        /// <summary>
        /// Name of <c>PhpTraitMemberVisibilityAttribute</c> class.
        /// </summary>
        public const string PhpMemberVisibilityAttributeName = "PhpMemberVisibilityAttribute";

        public readonly CoreType
            Context, Operators, Convert, StrictConvert, Comparison, StrictComparison, PhpException,
            ScriptAttribute, PhpTraitAttribute, PharAttribute, PhpTypeAttribute, PhpHiddenAttribute, PhpFieldsOnlyCtorAttribute, NotNullAttribute, DefaultValueAttribute, PhpMemberVisibilityAttribute, PhpStaticLocalAttribute,
            ScriptDiedException,
            IStaticInit, RoutineInfo, IndirectLocal,
            BinderFactory, GetClassConstBinder, GetFieldBinder, SetFieldBinder, AccessMask,
            Dynamic_NameParam_T, Dynamic_TargetTypeParam, Dynamic_LateStaticTypeParam, Dynamic_CallerTypeParam, Dynamic_UnpackingParam_T,
            RuntimeChain_ChainEnd, RuntimeChain_Value_T, RuntimeChain_Property_T, RuntimeChain_ArrayItem_T, RuntimeChain_ArrayNewItem_T,
            PhpTypeInfoExtension, PhpTypeInfo, CommonPhpArrayKeys,
            PhpNumber, PhpValue, PhpAlias, PhpString, PhpArray, PhpResource, IPhpArray, IPhpEnumerable, IPhpCallable, IPhpConvertible, PhpString_Blob,
            IntStringKey, PhpHashtable, ImportValueAttribute, DummyFieldsOnlyCtor,
            Void, Object, Byte, Int32, Long, Double, Boolean, String, Exception,
            RuntimeTypeHandle, RuntimeMethodHandle,
            stdClass, ArrayAccess, Closure, Generator, Iterator, Traversable, GeneratorStateMachineDelegate, MainDelegate, IntPtr;

        public CoreTypes(PhpCompilation compilation)
        {
            Contract.ThrowIfNull(compilation);
            _compilation = compilation;
            _table = new Dictionary<string, CoreType>();

            Void = Create(SpecialType.System_Void);
            Object = Create(SpecialType.System_Object);
            Byte = Create(SpecialType.System_Byte);
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
            PhpString_Blob = Create("PhpString+Blob");
            IntStringKey = Create("IntStringKey");
            PhpHashtable = Create("PhpHashtable");
            ScriptDiedException = Create("ScriptDiedException");
            Context = Create("Context");
            Operators = Create("Operators");
            Comparison = Create("Comparison");
            StrictComparison = Create("StrictComparison");
            Convert = Create("Convert");
            StrictConvert = Create("StrictConvert");
            PhpException = Create("PhpException");
            ScriptAttribute = Create("ScriptAttribute");
            PhpTraitAttribute = Create(PhpTraitAttributeName);
            PharAttribute = Create("PharAttribute");
            PhpTypeAttribute = Create("PhpTypeAttribute");
            PhpHiddenAttribute = Create("PhpHiddenAttribute");
            ImportValueAttribute = Create("ImportValueAttribute");
            DummyFieldsOnlyCtor = Create("DummyFieldsOnlyCtor");
            PhpFieldsOnlyCtorAttribute = Create(PhpFieldsOnlyCtorAttributeName);
            NotNullAttribute = Create("NotNullAttribute");
            DefaultValueAttribute = Create("DefaultValueAttribute");
            PhpMemberVisibilityAttribute = Create(PhpMemberVisibilityAttributeName);
            IStaticInit = Create("IStaticInit");
            RoutineInfo = Create("Reflection.RoutineInfo");
            IndirectLocal = Create("IndirectLocal");
            stdClass = CreateFromFullName("stdClass");
            ArrayAccess = CreateFromFullName("ArrayAccess");
            Closure = CreateFromFullName("Closure");

            BinderFactory = Create("Dynamic.BinderFactory");
            GetClassConstBinder = Create("Dynamic.GetClassConstBinder");
            GetFieldBinder = Create("Dynamic.GetFieldBinder");
            SetFieldBinder = Create("Dynamic.SetFieldBinder");
            AccessMask = CreateFromFullName("Pchp.CodeAnalysis.Semantics.AccessMask");

            Dynamic_NameParam_T = Create("Dynamic.NameParam`1");
            Dynamic_TargetTypeParam = Create("Dynamic.TargetTypeParam");
            Dynamic_LateStaticTypeParam = Create("Dynamic.LateStaticTypeParam");
            Dynamic_CallerTypeParam = Create("Dynamic.CallerTypeParam");
            Dynamic_UnpackingParam_T = Create("Dynamic.UnpackingParam`1");

            RuntimeChain_ChainEnd = Create("Dynamic.RuntimeChain.ChainEnd");
            RuntimeChain_Value_T = Create("Dynamic.RuntimeChain.Value`1");
            RuntimeChain_Property_T = Create("Dynamic.RuntimeChain.Property`1");
            RuntimeChain_ArrayItem_T = Create("Dynamic.RuntimeChain.ArrayItem`1");
            RuntimeChain_ArrayNewItem_T = Create("Dynamic.RuntimeChain.ArrayNewItem`1");

            PhpTypeInfoExtension = Create("Reflection.PhpTypeInfoExtension");
            PhpTypeInfo = Create("Reflection.PhpTypeInfo");
            CommonPhpArrayKeys = Create("CommonPhpArrayKeys");

            Iterator = CreateFromFullName("Iterator");
            Traversable = CreateFromFullName("Traversable");
            Generator = CreateFromFullName("Generator");
            GeneratorStateMachineDelegate = CreateFromFullName("GeneratorStateMachineDelegate");

            MainDelegate = Create("Context+MainDelegate");
            IntPtr = CreateFromFullName("System.IntPtr");
        }

        #region Table of types

        readonly Dictionary<string, CoreType> _table;
        readonly Dictionary<TypeSymbol, CoreType> _typetable = new Dictionary<TypeSymbol, CoreType>();
        //readonly Dictionary<SpecialType, CoreType> _specialTypes = new Dictionary<SpecialType, CoreType>();

        CoreType Create(string name) => CreateFromFullName(PeachpieRuntimeNamespace + "." + name);

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

        ///// <summary>
        ///// Gets special core type.
        ///// </summary>
        //public CoreType GetSpecialType(SpecialType type)
        //{
        //    CoreType t;
        //    _specialTypes.TryGetValue(type, out t);
        //    return t;
        //}

        internal void Update(AssemblySymbol coreass)
        {
            Contract.ThrowIfNull(coreass);

            foreach (var t in _table.Values)
            {
                if (t.Symbol == null)
                {
                    var fullname = t.FullName;

                    // nested types: todo: in Lookup
                    string nested = null;
                    int plus = fullname.IndexOf('+');
                    if (plus > 0)
                    {
                        nested = fullname.Substring(plus + 1);
                        fullname = fullname.Remove(plus);
                    }

                    var mdname = MetadataTypeName.FromFullName(fullname, false);
                    var symbol = coreass.LookupTopLevelMetadataType(ref mdname, true);
                    if (symbol.IsValidType())
                    {
                        if (nested != null)
                        {
                            symbol = symbol
                                .GetTypeMembers(nested)
                                .SingleOrDefault();

                            if (symbol == null)
                            {
                                continue;
                            }
                        }

                        _typetable[symbol] = t;
                        t.Update(symbol);

                        //if (symbol.SpecialType != SpecialType.None)
                        //    _specialTypes[symbol.SpecialType] = t;
                    }
                }
            }
        }

        #endregion
    }
}
