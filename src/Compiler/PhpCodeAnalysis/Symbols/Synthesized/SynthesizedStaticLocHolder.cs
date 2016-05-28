using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Nested class representing a static local variable.
    /// 
    /// Template:
    /// class static'foo'x : IStaticInit {
    ///     public T value;
    ///     public void Init(Context ctx){ ... }
    /// }
    /// </summary>
    internal class SynthesizedStaticLocHolder : NamedTypeSymbol
    {
        /// <summary>
        /// Containing source routine.
        /// </summary>
        readonly SourceRoutineSymbol _routine;

        /// <summary>
        /// Name of the local variable.
        /// </summary>
        readonly string _locName;

        /// <summary>
        /// Type of local variable.
        /// </summary>
        readonly TypeSymbol _locType;

        public override bool IsImplicitlyDeclared => true;

        public override ImmutableArray<NamedTypeSymbol> Interfaces
        {
            get
            {
                return base.Interfaces;
            }
        }

        /// <summary>
        /// The containing value represented as a class field.
        /// </summary>
        public SynthesizedFieldSymbol ValueField
        {
            get
            {
                if (_valueField == null)
                {
                    _valueField = new SynthesizedFieldSymbol(this, _locType, "value", Accessibility.Public, false);
                }

                return _valueField;
            }
        }
        SynthesizedFieldSymbol _valueField;

        public SynthesizedStaticLocHolder(SourceRoutineSymbol routine, string locName, TypeSymbol locType)
        {
            Contract.ThrowIfNull(routine);
            Contract.ThrowIfNull(locType);

            _routine = routine;
            _locName = locName ?? "?";
            _locType = locType;
        }

        public override int Arity => 0;

        public override Symbol ContainingSymbol => _routine.ContainingType;

        internal override PhpCompilation DeclaringCompilation => _routine.DeclaringCompilation;

        public override NamedTypeSymbol BaseType => DeclaringCompilation.CoreTypes.Object;

        public override Accessibility DeclaredAccessibility => Accessibility.Private;

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

        public override string Name => _locName + "<>`" + _routine.Name;

        public override TypeKind TypeKind => TypeKind.Class;

        internal override bool IsInterface => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override TypeLayout Layout => default(TypeLayout);

        internal override bool MangleName => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override bool ShouldAddWinRTMembers => false;

        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray.Create<Symbol>(ValueField);
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return GetMembers().Where(s => s.Name == name).AsImmutable();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved) => GetInterfacesToEmit();

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            return new IFieldSymbol[] { ValueField };
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty; // TODO: IStaticInit if there is Initializer
        }

        public override ImmutableArray<NamedTypeSymbol> AllInterfaces => GetInterfacesToEmit();
    }
}
