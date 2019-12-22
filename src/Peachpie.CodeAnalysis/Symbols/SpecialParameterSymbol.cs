using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using Devsense.PHP.Syntax;

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
        /// Value to be imported.
        /// From `Pchp.Core.ImportValueAttribute+ValueSpec`.
        /// </summary>
        public enum ValueSpec
        {
            /// <summary>
            /// Not used.
            /// </summary>
            Error = 0,

            /// <summary>
            /// Current class context.
            /// The parameter must be of type <see cref="RuntimeTypeHandle"/>, <c>PhpTypeInfo</c> or <see cref="string"/>.
            /// </summary>
            CallerClass,

            /// <summary>
            /// Current late static bound class (<c>static</c>).
            /// The parameter must be of type <c>PhpTypeInfo</c>.
            /// </summary>
            CallerStaticClass,

            /// <summary>
            /// Calue of <c>$this</c> variable or <c>null</c> if variable is not defined.
            /// The parameter must be of type <see cref="object"/>.
            /// </summary>
            This,

            /// <summary>
            /// Provides a reference to the array of local PHP variables.
            /// The parameter must be of type <c>PhpArray</c>.
            /// </summary>
            Locals,

            /// <summary>
            /// Provides callers parameters.
            /// The parameter must be of type array of <c>PhpTypeInfo</c>.
            /// </summary>
            CallerArgs,

            /// <summary>
            /// Provides reference to the current script container.
            /// The parameter must be of type <see cref="RuntimeTypeHandle"/>.
            /// </summary>
            CallerScript,
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
        public static bool IsImportValueParameter(IParameterSymbol p) => IsImportValueParameter(p, out _);

        /// <summary>
        /// Determines whether given parameter is treated as a special implicitly provided, by the compiler or the runtime.
        /// </summary>
        public static bool IsImportValueParameter(IParameterSymbol p, out ValueSpec valueEnum)
        {
            if (p != null)
            {
                var attr = ((ParameterSymbol)p).GetAttribute("Pchp.Core.ImportValueAttribute");
                if (attr != null)
                {
                    var args = attr.ConstructorArguments;
                    if (args.Length == 1 && args[0].Value is int enumvalue)
                    {
                        valueEnum = (ValueSpec)enumvalue;
                        return true;
                    }
                }
            }

            //
            valueEnum = default;
            return false;
        }

        public static bool IsDummyFieldsOnlyCtorParameter(IParameterSymbol p) => p.Type.Name == "DummyFieldsOnlyCtor";

        public static bool IsCallerClassParameter(IParameterSymbol p) => IsImportValueParameter(p, out var spec) && spec == ValueSpec.CallerClass;

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
