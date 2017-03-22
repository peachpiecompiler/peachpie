using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using System.Diagnostics;
using Pchp.CodeAnalysis.Semantics;
using Devsense.PHP.Syntax.Ast;
using Devsense.PHP.Syntax;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// PHP class as a CLR type.
    /// </summary>
    internal partial class SourceTypeSymbol : NamedTypeSymbol, IPhpTypeSymbol, ILambdaContainerSymbol
    {
        #region IPhpTypeSymbol

        /// <summary>
        /// Gets fully qualified name of the class.
        /// </summary>
        public virtual QualifiedName FullName => _syntax.QualifiedName;

        /// <summary>
        /// Optional.
        /// A field holding a reference to current runtime context.
        /// Is of type <see cref="Pchp.Core.Context"/>.
        /// </summary>
        public IFieldSymbol ContextStore
        {
            get
            {
                if (_lazyContextField == null && !this.IsStatic && !this.IsInterface)
                {
                    // resolve <ctx> field
                    _lazyContextField = (this.BaseType as IPhpTypeSymbol)?.ContextStore;

                    //
                    if (_lazyContextField == null)
                    {
                        _lazyContextField = new SynthesizedFieldSymbol(this, DeclaringCompilation.CoreTypes.Context.Symbol, SpecialParameterSymbol.ContextName, Accessibility.Protected, false, true);
                    }
                }

                return _lazyContextField;
            }
        }

        /// <summary>
        /// Optional.
        /// A field holding array of the class runtime fields.
        /// Is of type <see cref="Pchp.Core.PhpArray"/>.
        /// </summary>
        public IFieldSymbol RuntimeFieldsStore
        {
            get
            {
                if (_lazyRuntimeFieldsField == null && !this.IsStatic && !this.IsInterface)
                {
                    const string fldname = "<runtime_fields>";

                    _lazyRuntimeFieldsField = (this.BaseType as IPhpTypeSymbol)?.RuntimeFieldsStore;

                    //
                    if (_lazyRuntimeFieldsField == null)
                    {
                        _lazyRuntimeFieldsField = new SynthesizedFieldSymbol(this, DeclaringCompilation.CoreTypes.PhpArray.Symbol, fldname, Accessibility.Internal, false);
                    }
                }

                return _lazyRuntimeFieldsField;
            }
        }

        /// <summary>
        /// Optional.
        /// A nested class <c>__statics</c> containing class static fields and constants which are bound to runtime context.
        /// </summary>
        public INamedTypeSymbol StaticsContainer => _staticsContainer;

        /// <summary>
        /// Optional. A <c>.ctor</c> that ensures the initialization of the class without calling the PHP constructor.
        /// </summary>
        public IMethodSymbol InstanceConstructorFieldsOnly => InstanceConstructors.Where(MethodSymbolExtensions.IsFieldsOnlyConstructor).SingleOrDefault();

        #endregion

        readonly protected TypeDecl _syntax;
        readonly SourceFileSymbol _file;

        NamedTypeSymbol _lazyBaseType;
        ImmutableArray<MethodSymbol> _lazyCtors;   // .ctor
        IFieldSymbol _lazyContextField;   // protected Pchp.Core.Context <ctx>;
        IFieldSymbol _lazyRuntimeFieldsField; // internal Pchp.Core.PhpArray <runtimeFields>;
        SynthesizedStaticFieldsHolder/*!*/_staticsContainer; // class __statics { ... }

        /// <summary>
        /// Defined type members, methods, fields and constants.
        /// Does not include synthesized members.
        /// </summary>
        List<Symbol> _lazyMembers;

        List<SourceLambdaSymbol> _lambdas;

        /// <summary>[PhpTrait] attribute if this class is a trait. Initialized lazily.</summary>
        BaseAttributeData _lazyPhpTraitAttribute;

        public SourceFileSymbol ContainingFile => _file;

        Location CreateLocation(TextSpan span) => Location.Create(ContainingFile.SyntaxTree, span);

        public SourceTypeSymbol(SourceFileSymbol file, TypeDecl syntax)
        {
            Contract.ThrowIfNull(file);

            _syntax = syntax;
            _file = file;

            //
            _staticsContainer = new SynthesizedStaticFieldsHolder(this);
        }

        void ILambdaContainerSymbol.AddLambda(SourceLambdaSymbol routine)
        {
            Contract.ThrowIfNull(routine);
            if (_lambdas == null) _lambdas = new List<SourceLambdaSymbol>();
            _lambdas.Add(routine);
        }

        IEnumerable<SourceLambdaSymbol> ILambdaContainerSymbol.Lambdas
        {
            get
            {
                return (IEnumerable<SourceLambdaSymbol>)_lambdas ?? Array.Empty<SourceLambdaSymbol>();
            }
        }

        SourceLambdaSymbol ILambdaContainerSymbol.ResolveLambdaSymbol(LambdaFunctionExpr expr)
        {
            if (expr == null) throw new ArgumentNullException(nameof(expr));
            return _lambdas.First(s => s.Syntax == expr);
        }

        List<Symbol> EnsureMembers()
        {
            if (_lazyMembers == null)
            {
                var members = new List<Symbol>();

                //
                members.AddRange(LoadMethods());
                members.AddRange(LoadFields());

                //
                _lazyMembers = members;
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
        }

        IEnumerable<FieldSymbol> LoadFields()
        {
            var binder = new SemanticsBinder(null);

            // fields
            foreach (var flist in _syntax.Members.OfType<FieldDeclList>())
            {
                var fkind = (flist.Modifiers & PhpMemberAttributes.Static) == 0
                    ? SourceFieldSymbol.KindEnum.InstanceField
                    : SourceFieldSymbol.KindEnum.StaticField;

                foreach (var f in flist.Fields)
                {
                    yield return new SourceFieldSymbol(this, f.Name.Value,
                        CreateLocation(f.NameSpan.ToTextSpan()),
                        flist.Modifiers.GetAccessibility(), f.PHPDoc ?? flist.PHPDoc,
                        fkind,
                        (f.Initializer != null) ? binder.BindExpression(f.Initializer, BoundAccess.Read) : null);
                }
            }

            // constants
            foreach (var clist in _syntax.Members.OfType<ConstDeclList>())
            {
                foreach (var c in clist.Constants)
                {
                    yield return new SourceFieldSymbol(this, c.Name.Name.Value,
                        CreateLocation(c.Name.Span.ToTextSpan()),
                        Accessibility.Public, c.PHPDoc ?? clist.PHPDoc,
                        SourceFieldSymbol.KindEnum.ClassConstant,
                        binder.BindExpression(c.Initializer, BoundAccess.Read));
                }
            }
        }

        public override ImmutableArray<MethodSymbol> StaticConstructors => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> InstanceConstructors
        {
            get
            {
                var result = _lazyCtors;
                if (result.IsDefault)
                {
                    _lazyCtors = result = SynthesizedPhpCtorSymbol.CreateCtors(this).ToImmutableArray();
                }

                return result;
            }
        }

        /// <summary>
        /// Gets magic <c>__invoke</c> method or <c>null</c>.
        /// </summary>
        MethodSymbol TryGetMagicInvoke()
        {
            return GetMembers(Devsense.PHP.Syntax.Name.SpecialMethodNames.Invoke.Value, true)
                .OfType<MethodSymbol>()
                .Where(m => !m.IsStatic)
                .SingleOrDefault();
        }

        internal override bool HasTypeArgumentsCustomModifiers => false;

        public override ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal) => GetEmptyTypeArgumentCustomModifiers(ordinal);

        public override NamedTypeSymbol BaseType
        {
            get
            {
                if (ReferenceEquals(_lazyBaseType, null))
                {
                    DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
                    if (Interlocked.CompareExchange(ref _lazyBaseType, ResolveBaseType(diagnostics), null) == null)
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }

                    diagnostics.Free();
                }

                return _lazyBaseType;
            }
        }

        NamedTypeSymbol ResolveBaseType(DiagnosticBag diagnostics)
        {
            NamedTypeSymbol btype;

            if (_syntax.BaseClass != null)
            {
                var baseTypeName = _syntax.BaseClass.ClassName;
                if (baseTypeName == this.MakeQualifiedName())
                {
                    // TODO: check full circular dependency after the resolution
                    // Circular base class dependency involving '{0}' and '{1}'
                    diagnostics.Add(
                        CreateLocation(_syntax.HeadingSpan.ToTextSpan()),
                        Errors.ErrorCode.ERR_CircularBase, baseTypeName, this);
                }

                btype = (NamedTypeSymbol)DeclaringCompilation.GlobalSemantics.GetType(baseTypeName)
                    ?? new MissingMetadataTypeSymbol(baseTypeName.ClrName(), 0, false);

                if (btype.Arity != 0)
                {
                    // generics not supported yet
                    // TODO: Err diagnostics
                    throw new NotImplementedException($"Class {this.MakeQualifiedName()} extends a generic type {baseTypeName}.");
                }
                else if (btype.IsErrorType())
                {
                    // error: Type name '{1}' could not be resolved.
                    diagnostics.Add(CreateLocation(_syntax.BaseClass.Span.ToTextSpan()), Errors.ErrorCode.ERR_TypeNameCannotBeResolved, baseTypeName);
                }
            }
            else if (!IsStatic && !IsInterface)
            {
                btype = DeclaringCompilation.CoreTypes.Object.Symbol;
            }
            else
            {
                btype = null;
            }

            //
            return btype;
        }

        /// <summary>
        /// Gets type declaration syntax node.
        /// </summary>
        internal TypeDecl Syntax => _syntax;

        public override int Arity => 0;

        internal override IModuleSymbol ContainingModule => _file.SourceModule;

        public override Symbol ContainingSymbol => _file.SourceModule;

        internal override PhpCompilation DeclaringCompilation => _file.DeclaringCompilation;

        public override string Name => FullName.Name.Value;

        public override string NamespaceName => string.Join(".", FullName.Namespaces);

        public override string MetadataName
        {
            get
            {
                var name = base.MetadataName;

                if (_syntax.IsConditional)
                {
                    var ambiguities = this.DeclaringCompilation.SourceSymbolCollection.GetTypes().Where(t => t.Name == this.Name && t.NamespaceName == this.NamespaceName);
                    name += "@" + ambiguities.TakeWhile(f => f != this).Count().ToString(); // index within types with the same name
                }

                return name;
            }
        }

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

        public bool IsTrait => (_syntax.MemberAttributes & PhpMemberAttributes.Trait) != 0;

        public override bool IsAbstract => _syntax.MemberAttributes.IsAbstract() || IsInterface;

        public override bool IsSealed => _syntax.MemberAttributes.IsSealed();

        public override bool IsStatic => _syntax.MemberAttributes.IsStatic();

        public override ImmutableArray<Location> Locations => ImmutableArray.Create(CreateLocation(_syntax.Span.ToTextSpan()));

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

        public override ImmutableArray<Symbol> GetMembers() => EnsureMembers().AsImmutable();

        public override ImmutableArray<Symbol> GetMembers(string name, bool ignoreCase = false)
            => EnsureMembers().Where(s => s.Name.StringsEqual(name, ignoreCase)).AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            var result = new List<NamedTypeSymbol>();

            if (!_staticsContainer.IsEmpty)
            {
                result.Add(_staticsContainer);
            }

            //
            return result.AsImmutable();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
            => GetTypeMembers().Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            var attrs = base.GetAttributes();

            if (this.IsTrait)
            {
                // [PhpTraitAttribute()]

                if (_lazyPhpTraitAttribute == null)
                {
                    _lazyPhpTraitAttribute = new SynthesizedAttributeData(
                        DeclaringCompilation.CoreMethods.Ctors.PhpTraitAttribute,
                        ImmutableArray<TypedConstant>.Empty,
                        ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
                }

                attrs = attrs.Add(_lazyPhpTraitAttribute);
            }

            return attrs;
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
                var t = (NamedTypeSymbol)DeclaringCompilation.GlobalSemantics.GetType(i.ClassName)
                        ?? new MissingMetadataTypeSymbol(i.ClassName.ClrName(), 0, false);

                if (t.Arity != 0)
                {
                    throw new NotImplementedException();    // generics
                }

                ifaces.Add(t);
            }

            // __invoke => IPhpCallable
            if (TryGetMagicInvoke() != null)
            {
                ifaces.Add(DeclaringCompilation.CoreTypes.IPhpCallable);
            }

            //
            return ifaces.AsImmutable();
        }

        internal override IEnumerable<IMethodSymbol> GetMethodsToEmit()
        {
            return EnsureMembers().OfType<IMethodSymbol>()
                .Concat(InstanceConstructors)
                .Concat(((ILambdaContainerSymbol)this).Lambdas);
        }

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            foreach (var f in EnsureMembers().OfType<IFieldSymbol>())
            {
                if (f.OriginalDefinition != f)
                {
                    // field redeclares its parent member, discard
                    continue;
                }

                var srcf = f as SourceFieldSymbol;
                if (srcf.RequiresHolder)
                {
                    continue;   // this field has to be emitted within StaticsContainer
                }

                yield return f;
            }

            // special fields
            if (ReferenceEquals(ContextStore?.ContainingType, this))
            {
                yield return ContextStore;
            }

            if (ReferenceEquals(RuntimeFieldsStore?.ContainingType, this))
            {
                yield return RuntimeFieldsStore;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Syntax.PHPDoc?.Summary ?? string.Empty;
        }
    }

    /// <summary>
    /// Symbol representing a PHP anonymous class.
    /// Builds a type similar to <b>internal sealed class [anonymous@class filename position]</b>.
    /// </summary>
    internal class SourceAnonymousTypeSymbol : SourceTypeSymbol
    {
        public new AnonymousTypeDecl Syntax => (AnonymousTypeDecl)_syntax;

        public override QualifiedName FullName => Syntax.GetAnonymousTypeQualifiedName();

        public override string MetadataName => Name;

        public override bool IsSealed => true;

        public override bool IsAnonymousType => true;

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public SourceAnonymousTypeSymbol(SourceFileSymbol file, AnonymousTypeDecl syntax)
            : base(file, syntax)
        {
        }
    }
}
