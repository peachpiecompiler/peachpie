using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using System.Threading;
using Devsense.PHP.Syntax;
using Devsense.PHP.Syntax.Ast;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Container for class static and const fields.
    /// Such fields have to be put in a separate container since they are instantiated in context of current request, not the app domain.
    /// </summary>
    internal partial class SynthesizedStaticFieldsHolder : NamedTypeSymbol
    {
        readonly SourceTypeSymbol _class;
        
        public SynthesizedStaticFieldsHolder(SourceTypeSymbol @class)
        {
            Contract.ThrowIfNull(@class);
            _class = @class;
        }

        /// <summary>
        /// Gets enumeration of fields that will be emitted within this holder.
        /// </summary>
        internal IEnumerable<SourceFieldSymbol> Fields => _class.GetMembers().OfType<SourceFieldSymbol>().Where(f => f.RequiresHolder);

        /// <summary>
        /// Gets value indicating whether there are fields or constants.
        /// </summary>
        public bool IsEmpty => this.Fields.IsEmpty();

        #region NamedTypeSymbol

        public override string Name => WellKnownPchpNames.StaticsHolderClassName;

        public override int Arity => 0;

        public override NamedTypeSymbol BaseType => DeclaringCompilation.CoreTypes.Object;

        public override Symbol ContainingSymbol => _class;

        public override NamedTypeSymbol ContainingType => _class;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        public override bool IsStatic => false;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override TypeKind TypeKind => TypeKind.Class;

        internal override bool IsInterface => false;

        public override bool IsImplicitlyDeclared => true;

        internal override bool IsWindowsRuntimeImport => false;

        internal override TypeLayout Layout => default(TypeLayout);

        internal override bool MangleName => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override bool ShouldAddWinRTMembers => false;

        public override ImmutableArray<Symbol> GetMembers() => Fields.AsImmutable<Symbol>();

        public override ImmutableArray<Symbol> GetMembers(string name) => Fields.Where(f => f.Name.EqualsOrdinalIgnoreCase(name)).AsImmutable<Symbol>();
        
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => GetTypeMembers();

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved) => GetInterfacesToEmit();

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit() => Fields;

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            if (Fields.Any(f => f.RequiresContext))
            {
                // we need Init(Context) method
                return ImmutableArray.Create(DeclaringCompilation.CoreTypes.IStaticInit.Symbol);
            }
            else
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }
        }

        #endregion
    }
}
