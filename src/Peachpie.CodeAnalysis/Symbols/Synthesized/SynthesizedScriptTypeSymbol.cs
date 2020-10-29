using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// internal static class &lt;Script&gt; { ... }
    /// </summary>
    class SynthesizedScriptTypeSymbol : NamedTypeSymbol
    {
        readonly PhpCompilation _compilation;

        /// <summary>
        /// Optional. Real assembly entry point method.
        /// </summary>
        internal MethodSymbol EntryPointSymbol { get; set; }

        /// <summary>
        /// Additional type members.
        /// </summary>
        private List<Symbol> _lazyMembers = new List<Symbol>();

        public SynthesizedScriptTypeSymbol(PhpCompilation compilation)
        {
            _compilation = compilation;
        }

        public override int Arity => 0;

        internal override bool HasTypeArgumentsCustomModifiers => false;

        public override ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal) => GetEmptyTypeArgumentCustomModifiers(ordinal);

        public override Symbol ContainingSymbol => _compilation.SourceModule;

        internal override ModuleSymbol ContainingModule => _compilation.SourceModule;

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsStatic => true;

        public override bool IsSerializable => false;

        public override string Name => WellKnownPchpNames.DefaultScriptClassName;

        public override string NamespaceName => string.Empty;

        public override NamedTypeSymbol BaseType => _compilation.CoreTypes.Object;

        public override TypeKind TypeKind => TypeKind.Class;

        internal override bool IsInterface => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override TypeLayout Layout => default(TypeLayout);

        internal override bool MangleName => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override bool ShouldAddWinRTMembers => false;

        public override ImmutableArray<Symbol> GetMembers()
        {
            var list = new List<Symbol>();

            //
            if (EntryPointSymbol != null)
            {
                list.Add(EntryPointSymbol);
            }

            //
            list.AddRange(_lazyMembers);

            //
            return list.AsImmutable();
        }

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().Where(m => m.Name == name).AsImmutable();

        public override ImmutableArray<Symbol> GetMembersByPhpName(string name) => ImmutableArray<Symbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => _lazyMembers.OfType<NamedTypeSymbol>().AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => _lazyMembers.OfType<NamedTypeSymbol>().Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit() => _lazyMembers.OfType<FieldSymbol>().AsImmutable();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> StaticConstructors => ImmutableArray<MethodSymbol>.Empty;
    }
}
