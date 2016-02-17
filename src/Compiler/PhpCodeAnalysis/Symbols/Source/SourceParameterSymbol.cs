using Pchp.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a PHP function parameter.
    /// </summary>
    internal sealed class SourceParameterSymbol : ParameterSymbol   
    {
        readonly SourceBaseMethodSymbol _method;
        readonly FormalParam _syntax;
        readonly int _index;

        public SourceParameterSymbol(SourceBaseMethodSymbol method, FormalParam syntax, int index)
        {
            _method = method;
            _syntax = syntax;
            _index = index;
        }

        public override Symbol ContainingSymbol => _method;

        internal override IModuleSymbol ContainingModule => _method.ContainingModule;

        public override INamedTypeSymbol ContainingType => _method.ContainingType;

        public override string Name => _syntax.Name.Value;

        public override bool IsThis => _syntax.Name.IsThisVariableName;

        public override ITypeSymbol Type
        {
            get
            {
                return _method.DeclaringCompilation.GetSpecialType(SpecialType.System_Object);
            }
        }

        public override RefKind RefKind
        {
            get
            {
                if (_syntax.IsOut)
                    return RefKind.Out;

                return RefKind.None;
            }
        }

        public override bool IsParams => _syntax.IsVariadic;

        public override int Ordinal => _index;

        public override SymbolKind Kind => SymbolKind.Parameter;

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

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return null;
            }
        }
    }
}
