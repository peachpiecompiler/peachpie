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
    internal sealed class SourceNamedTypeSymbol : NamedTypeSymbol
    {
        readonly TypeDecl _syntax;
        readonly SourceFileSymbol _file;

        ImmutableArray<Symbol> _lazyMembers;
        
        NamedTypeSymbol _lazyBaseType;
        MethodSymbol _lazyCtorMethod;   // .ctor
        FieldSymbol _lazyContextField;   // protected Pchp.Core.Context <ctx>;

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
            yield return InstanceCtorMethodSymbol;
        }

        IEnumerable<FieldSymbol> LoadFields()
        {
            // source fields
            foreach (var flist in _syntax.Members.OfType<FieldDeclList>())
            {
                foreach (var f in flist.Fields)
                {
                    yield return new SourceFieldSymbol(this, f, flist.Modifiers);
                }
            }

            // special fields
            if (object.ReferenceEquals(ContextField.ContainingType, this))
                yield return ContextField;
        }

        /// <summary>
        /// <c>.ctor</c> synthesized method.
        /// Only if type is not static.
        /// </summary>
        internal MethodSymbol InstanceCtorMethodSymbol => this.IsStatic ? null : (_lazyCtorMethod ?? (_lazyCtorMethod = new SynthesizedCtorSymbol(this)));

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

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

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
    }
}
