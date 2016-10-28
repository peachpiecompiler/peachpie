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
    internal partial class SynthesizedStaticFieldsHolder : NamedTypeSymbol, IWithSynthesized
    {
        readonly SourceNamedTypeSymbol _class;
        ImmutableArray<Symbol> _lazyMembers;
        SynthesizedCctorSymbol _lazyCctorSymbol;

        public SynthesizedStaticFieldsHolder(SourceNamedTypeSymbol @class)
        {
            Contract.ThrowIfNull(@class);
            _class = @class;
        }

        /// <summary>
        /// Builds symbols from the class declaration.
        /// </summary>
        void EnsureMembers()
        {
            if (!_lazyMembers.IsDefault)
                return;

            Semantics.SemanticsBinder binder = new Semantics.SemanticsBinder(null, null);

            var members = new List<Symbol>();

            foreach (var srcmember in _class.Syntax.Members)
            {
                var cdecl = srcmember as ConstDeclList;
                if (cdecl != null)
                {
                    foreach (var c in cdecl.Constants)
                    {
                        var cvalue = Semantics.SemanticsBinder.TryGetConstantValue(this.DeclaringCompilation, c.Initializer);
                        if (cvalue == null) // constant has to be resolved in runtime
                        {
                            members.Add(new SourceRuntimeConstantSymbol(_class, c.Name.Name.Value, cdecl.PHPDoc,
                                binder.BindExpression(c.Initializer, Semantics.BoundAccess.Read)));
                        }
                    }
                }

                var fdecl = srcmember as FieldDeclList;
                if (fdecl != null && fdecl.Modifiers.IsStatic() && !fdecl.IsAppStatic())    // context-static fields has to be contained in the holder
                {
                    foreach (var f in fdecl.Fields)
                    {
                        members.Add(new SourceFieldSymbol(_class, f.Name.Value, fdecl.Modifiers & (~PhpMemberAttributes.Static), fdecl.PHPDoc,
                            f.HasInitVal ? binder.BindExpression(f.Initializer, Semantics.BoundAccess.Read) : null));
                    }
                }
            }

            //
            _lazyMembers = members.AsImmutable();
        }

        /// <summary>
        /// Gets value indicating whether there are fields or constants.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                EnsureMembers();
                return _lazyMembers.OfType<IFieldSymbol>().IsEmpty();
            }
        }

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

        internal override bool IsWindowsRuntimeImport => false;

        internal override TypeLayout Layout => default(TypeLayout);

        internal override bool MangleName => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override bool ShouldAddWinRTMembers => false;

        public override ImmutableArray<Symbol> GetMembers()
        {
            EnsureMembers();
            return _lazyMembers;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            EnsureMembers();
            return _lazyMembers.Where(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => GetTypeMembers();

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved) => GetInterfacesToEmit();

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            EnsureMembers();
            return _lazyMembers.OfType<IFieldSymbol>();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            if (GetMembers().OfType<SourceFieldSymbol>().Any(f => f.InitializerRequiresContext))
                return ImmutableArray.Create(DeclaringCompilation.CoreTypes.IStaticInit.Symbol);    // we need Init() method
            else
                return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        #endregion

        #region IWithSynthesized

        MethodSymbol IWithSynthesized.GetOrCreateStaticCtorSymbol()
        {
            EnsureMembers();

            if (_lazyCctorSymbol == null)
            {
                _lazyCctorSymbol = new SynthesizedCctorSymbol(this);
                _lazyMembers = _lazyMembers.Add(_lazyCctorSymbol);
            }

            return _lazyCctorSymbol;
        }

        SynthesizedFieldSymbol IWithSynthesized.GetOrCreateSynthesizedField(TypeSymbol type, string name, Accessibility accessibility, bool isstatic, bool @readonly)
        {
            EnsureMembers();

            var field = _lazyMembers.OfType<SynthesizedFieldSymbol>().FirstOrDefault(f => f.Name == name && f.IsStatic == isstatic && f.Type == type && f.IsReadOnly == @readonly);
            if (field == null)
            {
                field = new SynthesizedFieldSymbol(this, type, name, accessibility, isstatic, @readonly);
                _lazyMembers = _lazyMembers.Add(field);
            }

            return field;
        }

        void IWithSynthesized.AddTypeMember(NamedTypeSymbol nestedType)
        {
            Contract.ThrowIfNull(nestedType);

            throw new NotSupportedException();
        }

        #endregion
    }
}
