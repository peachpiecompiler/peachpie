using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.Syntax;
using Pchp.Syntax.AST;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// PHP class as a CLR type.
    /// </summary>
    internal sealed class SourceNamedTypeSymbol : NamedTypeSymbol, IWithSynthesized
    {
        readonly TypeDecl _syntax;
        readonly SourceFileSymbol _file;

        ImmutableArray<Symbol> _lazyMembers;
        
        NamedTypeSymbol _lazyBaseType;
        MethodSymbol _lazyCtorMethod;   // .ctor
        SynthesizedCctorSymbol _lazyCctorSymbol;   // .cctor
        FieldSymbol _lazyContextField;   // protected Pchp.Core.Context <ctx>;
        FieldSymbol _lazyRuntimeFieldsField; // internal Pchp.Core.PhpArray <runtimeFields>;

        public SourceFileSymbol ContainingFile => _file;
        
        public SourceNamedTypeSymbol(SourceFileSymbol file, TypeDecl syntax)
        {
            Contract.ThrowIfNull(file);

            _syntax = syntax;
            _file = file;
        }

        ImmutableArray<Symbol> Members()
        {
            if (_lazyMembers.IsDefault)
            {
                _lazyMembers = LoadMethods()
                    .Concat<Symbol>(LoadFields())
                    .ToImmutableArray();
            }

            return _lazyMembers;
        }

        IEnumerable<MethodSymbol> LoadMethods()
        {
            // source methods
            foreach (var m in _syntax.Members.OfType<MethodDecl>())
            {
                yield return new SourceMethodSymbol(this, m);
            }

            // default .ctor
            if (InstanceCtorMethodSymbol != null)
                yield return InstanceCtorMethodSymbol;

            if (_lazyCctorSymbol != null)
                yield return _lazyCctorSymbol;
        }

        IEnumerable<FieldSymbol> LoadFields()
        {
            // source fields
            foreach (var flist in _syntax.Members.OfType<FieldDeclList>())
            {
                foreach (var f in flist.Fields)
                {
                    yield return new SourceFieldSymbol(this, f, flist.Modifiers, flist.PHPDoc);
                }
            }

            // special fields
            if (ContextField != null && object.ReferenceEquals(ContextField.ContainingType, this))
                yield return ContextField;

            var runtimefld = EnsureRuntimeFieldsField();
            if (runtimefld != null && object.ReferenceEquals(runtimefld.ContainingType, this))
                yield return runtimefld;
        }

        /// <summary>
        /// <c>.ctor</c> synthesized method.
        /// Only if type is not static.
        /// </summary>
        internal MethodSymbol InstanceCtorMethodSymbol => this.IsStatic ? null : (_lazyCtorMethod ?? (_lazyCtorMethod = new SynthesizedCtorWrapperSymbol(this)));

        public override ImmutableArray<MethodSymbol> StaticConstructors
        {
            get
            {
                if (_lazyCctorSymbol != null)
                    return ImmutableArray.Create<MethodSymbol>(_lazyCctorSymbol);

                return ImmutableArray<MethodSymbol>.Empty;
            }
        }

        /// <summary>
        /// Special field containing reference to current object's context.
        /// May be a field from base type.
        /// </summary>
        internal FieldSymbol ContextField
        {
            get
            {
                if (_lazyContextField == null && !this.IsStatic)
                {
                    // resolve <ctx> field
                    var types = DeclaringCompilation.CoreTypes;
                    NamedTypeSymbol t = this.BaseType;
                    while (t != null && t != types.Object.Symbol)
                    {
                        var candidates = t.GetMembers(SpecialParameterSymbol.ContextName)
                            .OfType<FieldSymbol>()
                            .Where(f => f.DeclaredAccessibility == Accessibility.Protected && !f.IsStatic && f.Type == types.Context.Symbol)
                            .ToList();

                        Debug.Assert(candidates.Count <= 1);
                        if (candidates.Count != 0)
                        {
                            _lazyContextField = candidates[0];
                            break;
                        }

                        t = t.BaseType;
                    }

                    //
                    if (_lazyContextField == null)
                        _lazyContextField = new SynthesizedFieldSymbol(this, types.Context.Symbol, SpecialParameterSymbol.ContextName, Accessibility.Protected, false);
                }

                return _lazyContextField;
            }
        }

        internal FieldSymbol EnsureRuntimeFieldsField()
        {
            if (_lazyRuntimeFieldsField == null && !this.IsStatic)
            {
                const string fldname = "<runtime_fields>";

                // resolve <ctx> field
                var types = DeclaringCompilation.CoreTypes;
                NamedTypeSymbol t = this.BaseType;
                while (t != null && t != types.Object.Symbol)
                {
                    var candidates = t.GetMembers(fldname)
                        .OfType<FieldSymbol>()
                        .Where(f => f.DeclaredAccessibility != Accessibility.Public && !f.IsStatic && f.Type == types.PhpArray.Symbol)
                        .ToList();

                    Debug.Assert(candidates.Count <= 1);
                    if (candidates.Count != 0)
                    {
                        _lazyRuntimeFieldsField = candidates[0];
                        break;
                    }

                    t = t.BaseType;
                }

                //
                if (_lazyRuntimeFieldsField == null)
                    _lazyRuntimeFieldsField = new SynthesizedFieldSymbol(this, types.PhpArray.Symbol, fldname, Accessibility.Internal, false);
            }

            return _lazyRuntimeFieldsField;
        }

        public override NamedTypeSymbol BaseType
        {
            get
            {
                if (_lazyBaseType == null)
                {
                    if (_syntax.BaseClassName.HasValue)
                    {
                        if (_syntax.BaseClassName.Value.IsGeneric)
                            throw new NotImplementedException();

                        _lazyBaseType = (NamedTypeSymbol)DeclaringCompilation.GetTypeByMetadataName(_syntax.BaseClassName.Value.QualifiedName.ClrName())
                            ?? new MissingMetadataTypeSymbol(_syntax.BaseClassName.Value.QualifiedName.ClrName(), 0, false);
                    }
                    else
                    {
                        _lazyBaseType = DeclaringCompilation.CoreTypes.Object.Symbol;
                    }
                }

                return _lazyBaseType;
            }
        }

        /// <summary>
        /// Gets type declaration syntax node.
        /// </summary>
        internal TypeDecl Syntax => _syntax;

        public override int Arity => 0;

        internal override IModuleSymbol ContainingModule => _file.SourceModule;

        public override Symbol ContainingSymbol => _file.SourceModule;

        internal override PhpCompilation DeclaringCompilation => _file.DeclaringCompilation;

        public override string Name => _syntax.Name.Value;

        public override string NamespaceName
            => (_syntax.Namespace != null) ? _syntax.Namespace.QualifiedName.ClrName() : string.Empty;

        public override TypeKind TypeKind
        {
            get
            {
                return IsInterface ? TypeKind.Interface : TypeKind.Class;
            }
        }

        public override Accessibility DeclaredAccessibility => _syntax.MemberAttributes.GetAccessibility();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        internal override bool IsInterface => (_syntax.MemberAttributes & PhpMemberAttributes.Interface) != 0;

        public override bool IsAbstract => _syntax.MemberAttributes.IsAbstract();

        public override bool IsSealed => _syntax.MemberAttributes.IsSealed();

        public override bool IsStatic => _syntax.MemberAttributes.IsStatic();

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override bool ShouldAddWinRTMembers => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override TypeLayout Layout => default(TypeLayout);

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return null;
            }
        }

        internal override bool MangleName => false;

        public override ImmutableArray<NamedTypeSymbol> Interfaces => GetDeclaredInterfaces(null);

        public override ImmutableArray<Symbol> GetMembers() => Members();

        public override ImmutableArray<Symbol> GetMembers(string name)
            => Members().Where(s => s.Name.EqualsOrdinalIgnoreCase(name)).AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => _lazyMembers.OfType<NamedTypeSymbol>().AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => _lazyMembers.OfType<NamedTypeSymbol>().Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            return Members().OfType<IFieldSymbol>();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return this.Interfaces;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            var ifaces = new HashSet<NamedTypeSymbol>();
            foreach (var i in _syntax.ImplementsList)
            {
                if (i.IsGeneric)
                    throw new NotImplementedException();

                var t = (NamedTypeSymbol)DeclaringCompilation.GetTypeByMetadataName(i.QualifiedName.ClrName())
                        ?? new MissingMetadataTypeSymbol(i.QualifiedName.ClrName(), 0, false);

                ifaces.Add(t);
            }

            //
            return ifaces.AsImmutable();
        }

        MethodSymbol IWithSynthesized.GetOrCreateStaticCtorSymbol()
        {
            if (_lazyCctorSymbol == null)
            {
                _lazyCctorSymbol = new SynthesizedCctorSymbol(this);

                if (!_lazyMembers.IsDefault)
                    _lazyMembers = _lazyMembers.Add(_lazyCctorSymbol);
            }

            return _lazyCctorSymbol;
        }

        SynthesizedFieldSymbol IWithSynthesized.GetOrCreateSynthesizedField(TypeSymbol type, string name, Accessibility accessibility, bool isstatic)
        {
            GetMembers();

            var field = _lazyMembers.OfType<SynthesizedFieldSymbol>().FirstOrDefault(f => f.Name == name && f.IsStatic == isstatic && f.Type == type);
            if (field == null)
            {
                field = new SynthesizedFieldSymbol(this, type, name, accessibility, isstatic);
                _lazyMembers = _lazyMembers.Add(field);
            }

            return field;
        }

        void IWithSynthesized.AddTypeMember(NamedTypeSymbol nestedType)
        {
            Contract.ThrowIfNull(nestedType);

            _lazyMembers = _lazyMembers.Add(nestedType);
        }
    }
}
