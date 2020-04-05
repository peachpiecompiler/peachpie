using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using Devsense.PHP.Syntax;
using Peachpie.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Synthetized routine parameter.
    /// </summary>
    class SpecialParameterSymbol : ParameterSymbol
    {
        /// <summary>
        /// Name of special context parameter.
        /// Is of type <c>Context</c>.
        /// </summary>
        public const string ContextName = "<ctx>";

        /// <summary>
        /// Name of special locals parameter.
        /// Is of type <c>PhpArray</c>.
        /// </summary>
        public const string LocalsName = "<locals>";

        /// <summary>
        /// Name of special locals parameter used for temporary variables used by compiler.
        /// Is of type <c>PhpArray</c>.
        /// </summary>
        public const string TemporaryLocalsName = "<tmpLocals>";

        /// <summary>
        /// Synthesized params parameter.
        /// </summary>
        public const string ParamsName = "<arguments>";

        /// <summary>
        /// Name of special late-static-bound parameter.
        /// Is of type <c>PhpTypeInfo</c>
        /// </summary>
        public const string StaticTypeName = "<static>";

        /// <summary>
        /// Name of special <c>this</c> parameter.
        /// </summary>
        public const string ThisName = "this";

        /// <summary>
        /// Name of special <c>self</c> parameter.
        /// </summary>
        public const string SelfName = "<self>";

        readonly MethodSymbol _symbol;
        readonly int _index;
        readonly string _name;
        object _type;

        public SpecialParameterSymbol(MethodSymbol symbol, object type, string name, int index)
        {
            Debug.Assert(type is TypeSymbol || type is CoreType);
            Contract.ThrowIfNull(symbol);
            Contract.ThrowIfNull(type);

            _symbol = symbol;
            _type = type;
            _name = name;
            _index = index;
        }

        /// <summary>
        /// Determines whether given parameter is treated as a special Context parameter
        /// which is always first and of type <c>Pchp.Core.Context</c>.
        /// </summary>
        public static bool IsContextParameter(ParameterSymbol p)
            => p != null &&
                p.DeclaringCompilation != null
                ? p.Type == p.DeclaringCompilation.CoreTypes.Context.Symbol
                : p.Type != null
                    ? (p.Type.Name == "Context" && p.Type.ContainingAssembly.IsPeachpieCorLibrary)
                    : false;

        /// <summary>
        /// Determines whether given parameter is treated as a special implicitly provided, by the compiler or the runtime.
        /// </summary>
        public static bool IsImportValueParameter(ParameterSymbol p) => p.ImportValueAttributeData.IsValid;

        /// <summary>
        /// Determines whether given parameter is treated as a special implicitly provided, by the compiler or the runtime.
        /// </summary>
        public static bool IsImportValueParameter(ParameterSymbol p, out ImportValueAttributeData.ValueSpec valueEnum)
        {
            var data = p.ImportValueAttributeData;
            valueEnum = data.Value;
            return data.IsValid;
        }

        public static bool IsDummyFieldsOnlyCtorParameter(IParameterSymbol p) => p.Type.Name == "DummyFieldsOnlyCtor";

        public static bool IsCallerClassParameter(ParameterSymbol p) => p.ImportValueAttributeData.Value == ImportValueAttributeData.ValueSpec.CallerClass;

        /// <summary>
        /// Determines whether given parameter is a special lately static bound parameter.
        /// This parameter provides late static bound type, of type <c>PhpTypeInfo</c>.
        /// </summary>
        public static bool IsLateStaticParameter(ParameterSymbol p)
            => p != null && p.Type != null && p.Type.MetadataName == "PhpTypeInfo" && !(p is SourceParameterSymbol) && p.Name == StaticTypeName; // TODO: && namespace == Pchp.Core.

        /// <summary>
        /// Determines whether given parameter is a special self parameter.
        /// This parameter provides self type, of type <c>RuntimeTypeHandle</c>.
        /// </summary>
        public static bool IsSelfParameter(ParameterSymbol p)
            => p != null && p.Type != null && p.Type.MetadataName == "RuntimeTypeHandle" && !(p is SourceParameterSymbol) && p.Name == SelfName;

        public override bool IsImplicitlyDeclared => true;

        public override Symbol ContainingSymbol => _symbol;

        internal override IModuleSymbol ContainingModule => _symbol.ContainingModule;

        public override NamedTypeSymbol ContainingType => _symbol.ContainingType;

        public override string Name => _name;

        public override bool IsThis => _index == -1;

        internal override TypeSymbol Type
        {
            get
            {
                if (_type is CoreType ctype)
                {
                    _type = ctype.Symbol;
                    Debug.Assert(_type != null, "ReferenceManager was not bound (probably)");
                }

                if (_type is TypeSymbol t)
                {
                    return t;
                }

                throw new ArgumentException();
            }
        }

        public override RefKind RefKind => RefKind.None;

        public override int Ordinal => _index;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ConstantValue ExplicitDefaultConstantValue => null;   // TODO
    }
}
