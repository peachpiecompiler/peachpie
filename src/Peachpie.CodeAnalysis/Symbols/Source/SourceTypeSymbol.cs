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

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// PHP class as a CLR type.
    /// </summary>
    internal sealed partial class SourceTypeSymbol : NamedTypeSymbol, IPhpTypeSymbol
    {
        #region IPhpTypeSymbol

        /// <summary>
        /// Gets fully qualified name of the class.
        /// </summary>
        public QualifiedName FullName => _syntax.QualifiedName;

        /// <summary>
        /// Optional.
        /// A field holding a reference to current runtime context.
        /// Is of type <see cref="Pchp.Core.Context"/>.
        /// </summary>
        public FieldSymbol ContextStore
        {
            get
            {
                if (_lazyContextField == null && !this.IsStatic)
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
        public FieldSymbol RuntimeFieldsStore
        {
            get
            {
                if (_lazyRuntimeFieldsField == null && !this.IsStatic)
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
        /// A method <c>.phpnew</c> that ensures the initialization of the class without calling the base type constructor.
        /// </summary>
        public MethodSymbol InitializeInstanceMethod => this.IsStatic ? null : (_lazyPhpNewMethod ?? (_lazyPhpNewMethod = new SynthesizedPhpNewMethodSymbol(this)));

        /// <summary>
        /// Optional.
        /// A nested class <c>__statics</c> containing class static fields and constants which are bound to runtime context.
        /// </summary>
        public NamedTypeSymbol StaticsContainer => _staticsContainer;

        #endregion

        readonly TypeDecl _syntax;
        readonly SourceFileSymbol _file;

        NamedTypeSymbol _lazyBaseType;
        MethodSymbol _lazyCtorMethod, _lazyPhpNewMethod;   // .ctor, .phpnew 
        FieldSymbol _lazyContextField;   // protected Pchp.Core.Context <ctx>;
        FieldSymbol _lazyRuntimeFieldsField; // internal Pchp.Core.PhpArray <runtimeFields>;
        SynthesizedStaticFieldsHolder/*!*/_staticsContainer; // class __statics { ... }
        SynthesizedMethodSymbol _lazyInvokeSymbol; // IPhpCallable.Invoke(Context, PhpValue[]);

        /// <summary>
        /// Defined type members, methods, fields and constants.
        /// Does not include synthesized members.
        /// </summary>
        List<Symbol> _lazyMembers;

        public SourceFileSymbol ContainingFile => _file;

        public SourceTypeSymbol(SourceFileSymbol file, TypeDecl syntax)
        {
            Contract.ThrowIfNull(file);

            _syntax = syntax;
            _file = file;

            //
            _staticsContainer = new SynthesizedStaticFieldsHolder(this);
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
                    yield return new SourceFieldSymbol(this, f.Name.Value, flist.Modifiers.GetAccessibility(), f.PHPDoc ?? flist.PHPDoc, fkind,
                        (f.Initializer != null) ? binder.BindExpression(f.Initializer, BoundAccess.Read) : null);
                }
            }

            // constants
            foreach (var clist in _syntax.Members.OfType<ConstDeclList>())
            {
                foreach (var c in clist.Constants)
                {
                    yield return new SourceFieldSymbol(this, c.Name.Name.Value, Accessibility.Public, c.PHPDoc ?? clist.PHPDoc,
                        SourceFieldSymbol.KindEnum.ClassConstant,
                        binder.BindExpression(c.Initializer, BoundAccess.Read));
                }
            }
        }

        /// <summary>
        /// <c>.ctor</c> synthesized method. Only if type is not static.
        /// </summary>
        internal MethodSymbol PhpCtorMethodSymbol => this.IsStatic ? null : (_lazyCtorMethod ?? (_lazyCtorMethod = new SynthesizedPhpCtorSymbol(this)));

        public override ImmutableArray<MethodSymbol> StaticConstructors => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> InstanceConstructors => PhpCtorMethodSymbol != null
            ? ImmutableArray.Create<MethodSymbol>(PhpCtorMethodSymbol)
            : ImmutableArray<MethodSymbol>.Empty;

        /// <summary>
        /// Gets value indicating the class can be called (i.e. has <c>__invoke</c> magic method).
        /// </summary>
        public bool IsInvokable
        {
            get
            {
                if (_lazyInvokeSymbol != null)
                {
                    return true;
                }

                return GetMembers(Devsense.PHP.Syntax.Name.SpecialMethodNames.Invoke.Value).Any(s => s is MethodSymbol);
            }
        }

        /// <summary>
        /// In case the class implements <c>__invoke</c> method, we create special Invoke() method that is compatible with IPhpCallable interface.
        /// </summary>
        internal SynthesizedMethodSymbol EnsureInvokeMethod(Emit.PEModuleBuilder module)
        {
            if (_lazyInvokeSymbol == null)
            {
                if (IsInvokable)
                {
                    _lazyInvokeSymbol = new SynthesizedMethodSymbol(this, "IPhpCallable.Invoke", false, true, DeclaringCompilation.CoreTypes.PhpValue)
                    {
                        ExplicitOverride = (MethodSymbol)DeclaringCompilation.CoreTypes.IPhpCallable.Symbol.GetMembers("Invoke").Single(),
                    };
                    _lazyInvokeSymbol.SetParameters(
                        new SpecialParameterSymbol(_lazyInvokeSymbol, DeclaringCompilation.CoreTypes.Context, SpecialParameterSymbol.ContextName, 0),
                        new SynthesizedParameterSymbol(_lazyInvokeSymbol, ArrayTypeSymbol.CreateSZArray(ContainingAssembly, DeclaringCompilation.CoreTypes.PhpValue.Symbol), 1, RefKind.None, "arguments"));

                    //
                    module.SynthesizedManager.AddMethod(this, _lazyInvokeSymbol);
                }
            }
            return _lazyInvokeSymbol;
        }

        public override NamedTypeSymbol BaseType
        {
            get
            {
                if (_lazyBaseType == null)
                {
                    if (_syntax.BaseClass != null)
                    {
                        _lazyBaseType = (NamedTypeSymbol)DeclaringCompilation.GetTypeByMetadataName(_syntax.BaseClass.ClassName.ClrName())
                            ?? new MissingMetadataTypeSymbol(_syntax.BaseClass.ClassName.ClrName(), 0, false);

                        if (_lazyBaseType.Arity != 0)
                        {
                            throw new NotImplementedException();    // generics
                        }
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

        public override string Name => _syntax.Name.Name.Value;

        public override string NamespaceName
            => (_syntax.ContainingNamespace != null) ? _syntax.ContainingNamespace.QualifiedName.QualifiedName.ClrName() : string.Empty;

        public override string MetadataName
        {
            get
            {
                var name = base.MetadataName;

                if (_syntax.IsConditional)
                {
                    var ambiguities = this.DeclaringCompilation.SourceSymbolTables.GetTypes().Where(t => t.Name == this.Name && t.NamespaceName == this.NamespaceName);
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

        public override ImmutableArray<Symbol> GetMembers() => EnsureMembers().AsImmutable();

        public override ImmutableArray<Symbol> GetMembers(string name)
            => EnsureMembers().Where(s => s.Name.EqualsOrdinalIgnoreCase(name)).AsImmutable();

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

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return this.Interfaces;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            var ifaces = new HashSet<NamedTypeSymbol>();
            foreach (var i in _syntax.ImplementsList)
            {
                var t = (NamedTypeSymbol)DeclaringCompilation.GetTypeByMetadataName(i.ClassName.ClrName())
                        ?? new MissingMetadataTypeSymbol(i.ClassName.ClrName(), 0, false);

                if (t.Arity != 0)
                {
                    throw new NotImplementedException();    // generics
                }

                ifaces.Add(t);
            }

            // __invoke => IPhpCallable
            if (IsInvokable)
            {
                ifaces.Add(DeclaringCompilation.CoreTypes.IPhpCallable);
            }

            //
            return ifaces.AsImmutable();
        }

        internal override IEnumerable<IMethodSymbol> GetMethodsToEmit()
        {
            foreach (var m in EnsureMembers().OfType<IMethodSymbol>())
            {
                yield return m;
            }

            // .ctor
            if (PhpCtorMethodSymbol != null)
            {
                yield return PhpCtorMethodSymbol;
            }

            // .phpnew
            if (InitializeInstanceMethod != null)
            {
                yield return InitializeInstanceMethod;
            }
        }

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            foreach (var f in EnsureMembers().OfType<IFieldSymbol>())
            {
                var srcf = f as SourceFieldSymbol;
                if (srcf.RequiresHolder)
                {
                    continue;   // this field has to be emitted within StaticsContainer
                }

                yield return f;
            }

            // special fields
            if (ContextStore?.ContainingType == this)
            {
                yield return ContextStore;
            }

            if (RuntimeFieldsStore?.ContainingType == this)
            {
                yield return RuntimeFieldsStore;
            }
        }
    }
}
