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
        /// Is of type <see cref="Pchp.Core.Context"/>
        /// </summary>
        public const string ContextName = "<ctx>";

        /// <summary>
        /// Name of special locals parameter.
        /// Is of type <see cref="Pchp.Core.PhpArray"/>
        /// </summary>
        public const string LocalsName = "<locals>";

        /// <summary>
        /// Name of special late-bound parameter.
        /// Is of type <see cref="System.RuntimeTypeHandle"/>
        /// </summary>
        public const string StaticTypeName = "<static>";

        /// <summary>
        /// Name of special <c>this</c> parameter.
        /// </summary>
        public static string ThisName => VariableName.ThisVariableName.Value;

        readonly MethodSymbol _symbol;
        readonly int _index;
        readonly string _name;
        readonly object _type;

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
            => p != null && p.Ordinal == 0 && p.Type != null && p.Type.MetadataName == "Context"; // TODO: && namespace == Pchp.Core.

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
                if (_type is TypeSymbol)
                    return (TypeSymbol)_type;

                if (_type is CoreType)
                    return ((CoreType)_type).Symbol;

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
