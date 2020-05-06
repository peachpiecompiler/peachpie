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
using static Pchp.CodeAnalysis.AstUtils;
using Pchp.CodeAnalysis.Utilities;
using Pchp.CodeAnalysis.Errors;
using System.IO;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// PHP class as a CLR type.
    /// </summary>
    internal partial class SourceTypeSymbol : NamedTypeSymbol, IPhpTypeSymbol
    {
        #region IPhpTypeSymbol

        /// <summary>
        /// Gets fully qualified name of the class.
        /// </summary>
        public virtual QualifiedName FullName => _syntax.QualifiedName;

        /// <summary><see cref="FullName"/> as string.</summary>
        internal string FullNameString => _FullNameString ??= FullName.ToString();
        string _FullNameString;

        /// <summary>
        /// Optional.
        /// A field holding a reference to current runtime context.
        /// Is of type <c>Context</c>.
        /// </summary>
        public IFieldSymbol ContextStore
        {
            get
            {
                if (_lazyContextField == null && !this.IsStatic && !this.IsInterface)
                {
                    // resolve <ctx> field
                    var lazyContextField = (this.BaseType as IPhpTypeSymbol)?.ContextStore;

                    //
                    if (lazyContextField == null)
                    {
                        lazyContextField = new SynthesizedFieldSymbol(this, DeclaringCompilation.CoreTypes.Context.Symbol, SpecialParameterSymbol.ContextName,
                            accessibility: this.IsSealed ? Accessibility.Private : Accessibility.Protected,
                            isStatic: false,
                            isReadOnly: true);
                    }

                    Interlocked.CompareExchange(ref _lazyContextField, lazyContextField, null);
                }

                return _lazyContextField;
            }
        }

        /// <summary>
        /// Optional.
        /// A field holding array of the class runtime fields.
        /// Is of type <c>PhpArray</c>.
        /// </summary>
        public IFieldSymbol RuntimeFieldsStore
        {
            get
            {
                if (_lazyRuntimeFieldsField == null && !this.IsStatic && !this.IsInterface && !this.IsTrait)
                {
                    const string fldname = "<runtime_fields>";

                    var lazyRuntimeFieldsField = (this.BaseType as IPhpTypeSymbol)?.RuntimeFieldsStore;

                    //
                    if (lazyRuntimeFieldsField == null)
                    {
                        lazyRuntimeFieldsField = new SynthesizedFieldSymbol(this, DeclaringCompilation.CoreTypes.PhpArray.Symbol, fldname, Accessibility.Internal, false);
                    }

                    Interlocked.CompareExchange(ref _lazyRuntimeFieldsField, lazyRuntimeFieldsField, null);
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
        public IMethodSymbol InstanceConstructorFieldsOnly => InstanceConstructors.SingleOrDefault(ctor => ctor.IsInitFieldsOnly);

        public virtual byte AutoloadFlag
        {
            get
            {
                if (_autoloadFlag == 0xff)
                {
                    _autoloadFlag = ResolvePhpTypeAutoloadFlag();
                }

                return _autoloadFlag;
            }
        }

        byte _autoloadFlag = 0xff;

        #endregion

        #region TraitUse

        protected sealed class TraitUse
        {
            /// <summary>
            /// Type using the trait.
            /// </summary>
            readonly SourceTypeSymbol ContainingType;

            /// <summary>
            /// Constructed trait type symbol.
            /// </summary>
            public readonly NamedTypeSymbol Symbol;
            public readonly TraitsUse.TraitAdaptation[] Adaptations;

            /// <summary>
            /// private readonly T &lt;&gt;_t;
            /// </summary>
            public IFieldSymbol TraitInstanceField
            {
                get
                {
                    if (_lazyTraitInstanceField == null)
                    {
                        var lazyTraitInstanceField = new SynthesizedFieldSymbol(ContainingType, Symbol, "<>" + "trait_" + Symbol.Name,
                            accessibility: Accessibility.Private,
                            isStatic: false,
                            isReadOnly: true);
                        Interlocked.CompareExchange(ref _lazyTraitInstanceField, lazyTraitInstanceField, null);
                    }

                    return _lazyTraitInstanceField;
                }
            }
            IFieldSymbol _lazyTraitInstanceField;

            /// <summary>
            /// Specifies implementation of a trait method in containing class.
            /// </summary>
            [DebuggerDisplay("{Accessibility} {Name,nq} -> {SourceMethod}")]
            struct DeclaredAs
            {
                /// <summary>
                /// Trait method.
                /// </summary>
                public MethodSymbol SourceMethod;

                /// <summary>
                /// Method visibility in containing class.
                /// </summary>
                public Accessibility Accessibility;

                /// <summary>
                /// Method name in containing class.
                /// </summary>
                public string Name;
            }

            /// <summary>
            /// Gets real accessibility (visibility) of trait member.
            /// </summary>
            Accessibility DeclaredAccessibility(MethodSymbol m)
            {
                if (m is SynthesizedMethodSymbol sm && sm.ForwardedCall != null)
                {
                    return DeclaredAccessibility(sm);
                }
                else
                {
                    return m.DeclaredAccessibility;
                }
            }

            /// <summary>
            /// Map of visible trait members how to be declared in containing class.
            /// </summary>
            Dictionary<Name, DeclaredAs> MembersMap
            {
                get
                {
                    if (_lazyMembersMap == null)
                    {
                        // collect source methods to be mapped to containing class:
                        var sourcemembers = new Dictionary<MemberQualifiedName, MethodSymbol>();
                        var map = new Dictionary<Name, DeclaredAs>();

                        // mehods that will be mapped to containing class:
                        IEnumerable<MethodSymbol> traitmethods = Symbol.GetMembers()
                            .OfType<MethodSymbol>()
                            .Where(m => m.MethodKind == MethodKind.Ordinary); // only regular methods, not constructors

                        // methods from used traits:
                        if (Symbol.OriginalDefinition is SourceTypeSymbol srct && !srct.TraitUses.IsEmpty)
                        {
                            // containing trait uses first (members will be overriden by this trait use)
                            traitmethods = srct
                                .TraitMembers.OfType<MethodSymbol>()
                                .Select(m => m.AsMember(m.ContainingType.Construct(ContainingType)))
                                .Concat(traitmethods);
                        }

                        // map of methods from trait:
                        foreach (var m in traitmethods)
                        {
                            // {T::method_name} => method
                            var phpt = (IPhpTypeSymbol)m.ContainingType.OriginalDefinition;
                            sourcemembers[new MemberQualifiedName(phpt.FullName, new Name(m.RoutineName))] = m;

                            // {method_name} => method
                            map[new Name(m.RoutineName)] = new DeclaredAs()
                            {
                                Name = m.RoutineName,
                                SourceMethod = m,
                                Accessibility = DeclaredAccessibility(m),
                            };
                        }

                        // adaptations
                        if (Adaptations != null && Adaptations.Length != 0)
                        {
                            var phpt = (IPhpTypeSymbol)Symbol.OriginalDefinition;
                            foreach (var a in Adaptations)
                            {
                                var membername = a.TraitMemberName.Item2.Name;

                                if (a is TraitsUse.TraitAdaptationPrecedence precedence)
                                {
                                    if (map.TryGetValue(membername, out DeclaredAs declared) &&
                                        precedence.IgnoredTypes.Select(t => t.QualifiedName.Value).Contains(((IPhpTypeSymbol)declared.SourceMethod.ContainingType.OriginalDefinition).FullName))
                                    {
                                        // member was hidden
                                        map.Remove(membername);
                                    }
                                }
                                else if (a is TraitsUse.TraitAdaptationAlias alias)
                                {
                                    // add an alias to the map:
                                    var qname = a.TraitMemberName.Item1 != null ? a.TraitMemberName.Item1.QualifiedName.Value : phpt.FullName;

                                    // add an alias to the map:
                                    if (sourcemembers.TryGetValue(new MemberQualifiedName(qname, membername), out MethodSymbol s))
                                    {
                                        if (map.TryGetValue(membername, out DeclaredAs declaredas) && declaredas.SourceMethod == s)
                                        {
                                            // aliasing the symbol, remove the old declaration
                                            map.Remove(membername);
                                        }

                                        declaredas = new DeclaredAs()
                                        {
                                            SourceMethod = s,
                                            Name = membername.Value,
                                            Accessibility = DeclaredAccessibility(s),
                                        };

                                        if (alias.NewModifier.HasValue) declaredas.Accessibility = alias.NewModifier.Value.GetAccessibility();
                                        if (alias.NewName.HasValue) declaredas.Name = alias.NewName.Name.Value;

                                        // update map
                                        map[new Name(declaredas.Name)] = declaredas;
                                    }
                                }
                                else
                                {
                                    throw ExceptionUtilities.UnexpectedValue(a);
                                }
                            }
                        }

                        //
                        Interlocked.CompareExchange(ref _lazyMembersMap, map, null);
                    }

                    return _lazyMembersMap;
                }
            }
            Dictionary<Name, DeclaredAs> _lazyMembersMap;

            /// <summary>
            /// Gets synthesized members (methods, properties, constants).
            /// </summary>
            public ImmutableArray<Symbol> GetMembers()
            {
                if (_lazyMembers.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyMembers, CreateMembers().AsImmutable());
                }

                return _lazyMembers;
            }
            ImmutableArray<Symbol> _lazyMembers;

            IEnumerable<Symbol> CreateMembers()
            {
                // properties
                foreach (var p in Symbol.EnumerateProperties())
                {
                    if (((Symbol)p).IsPhpHidden(ContainingType.DeclaringCompilation))
                    {
                        continue;
                    }

                    yield return new SynthesizedTraitFieldSymbol(ContainingType, (FieldSymbol)TraitInstanceField, p);
                }

                // methods
                foreach (var m in MembersMap.Values)
                {
                    // abstract trait member must be overriden in the containing class (even by its base)
                    // -> ignore abstract trait members
                    if (m.SourceMethod.IsAbstract || (m.SourceMethod.OriginalDefinition is SourceMethodSymbol srcm && ((MethodDecl)srcm.Syntax).Modifiers.IsAbstract())) // TODO: determine abstract method better
                    {
                        // abstract methods must be implemented by containing class
                        continue;
                    }

                    if (ContainingType.Syntax.Members.OfType<MethodDecl>().Any(x => x.Name.Name.Equals(m.Name)))
                    {
                        // "overriden" in containing class
                        continue;
                    }

                    yield return CreateTraitMethodImplementation(m);
                }

                //
                yield break;
            }

            SynthesizedMethodSymbol CreateTraitMethodImplementation(DeclaredAs declared)
            {
                return new SynthesizedTraitMethodSymbol(
                    ContainingType, declared.Name, declared.SourceMethod, declared.Accessibility,
                    isfinal: false);
            }

            public TraitUse(SourceTypeSymbol type, NamedTypeSymbol symbol, TraitsUse.TraitAdaptation[] adaptations)
            {
                Debug.Assert(symbol != null);
                Debug.Assert(symbol.IsTraitType());

                this.ContainingType = type;
                this.Symbol = symbol.Construct(type is SourceTraitTypeSymbol tt ? tt.TSelfParameter : type);    // if contained in trait, pass its TSelf
                this.Adaptations = adaptations;
            }
        }

        #endregion

        readonly protected TypeDecl _syntax;
        readonly SourceFileSymbol _file;

        /// <summary>
        /// Resolved base type.
        /// </summary>
        protected NamedTypeSymbol _lazyBaseType;

        /// <summary>
        /// Resolved base interfaces.
        /// </summary>
        ImmutableArray<NamedTypeSymbol> _lazyInterfacesType;

        ImmutableArray<TraitUse> _lazyTraitUses;

        /// <summary>
        /// Whether is this particular declaration unreachable according to the analysis.
        /// </summary>
        public bool IsMarkedUnreachable { get; set; }

        /// <summary>
        /// Whether this declarations or any of the ancestors is unreachable.
        /// </summary>
        public override bool IsUnreachable
        {
            get
            {
                HashSet<QualifiedName> visited = null;
                // handle circular dependency, stack overflow
                return IsUnreachableChecked(ref visited);
            }
        }

        bool IsUnreachableChecked(ref HashSet<QualifiedName> visited)
        {
            if (IsMarkedUnreachable)
            {
                return true;
            }

            visited?.Add(FullName);

            if (_syntax.BaseClass != null || _syntax.ImplementsList.Length != 0 || HasTraitUses)
            {
                if (visited == null) visited = new HashSet<QualifiedName>() { FullName };

                if (_syntax.BaseClass != null)
                {
                    if (visited.Add(_syntax.BaseClass.ClassName) && this.BaseType is SourceTypeSymbol s && s.IsUnreachableChecked(ref visited))
                    {
                        return true;
                    }
                }

                foreach (var i in Interfaces)
                {
                    if (i is SourceTypeSymbol s && visited.Add(s.FullName) && s.IsUnreachableChecked(ref visited))
                    {
                        return true;
                    }
                }

                foreach (var t in TraitUses)
                {
                    if (t.Symbol is SourceTypeSymbol s && visited.Add(s.FullName) && s.IsUnreachableChecked(ref visited))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// In case the declaration is ambiguous, this references symbol with alternative declaration.
        /// </summary>
        public SourceTypeSymbol NextVersion
        {
            get
            {
                ResolveBaseTypes();
                return _nextVersion;
            }
        }
        SourceTypeSymbol _nextVersion;
        int _version;

        /// <summary>
        /// Gets value indicating the class uses traits.
        /// </summary>
        public bool HasTraitUses => _syntax.Members.OfType<TraitsUse>().Any();

        /// <summary>
        /// Gets value indicating the type declaration is ambiguous.
        /// </summary>
        public bool HasVersions => (_version != 0);

        /// <summary>
        /// Enumerates all reachable versions of this declaration.
        /// </summary>
        public ImmutableArray<SourceTypeSymbol> AllReachableVersions(Dictionary<QualifiedName, INamedTypeSymbol> resolved = null)
        {
            ResolveBaseTypes(resolved);

            if (_nextVersion == null)
            {
                return IsUnreachable ? ImmutableArray<SourceTypeSymbol>.Empty : ImmutableArray.Create(this);
            }
            else
            {
                Debug.Assert(_version != 0);
                var builder = ImmutableArray.CreateBuilder<SourceTypeSymbol>(_version);
                for (var x = this; x != null; x = x.NextVersion)
                {
                    Debug.Assert(x._version > 0 && x._version <= builder.Capacity);
                    if (!x.IsUnreachable)
                    {
                        builder.Add(x);
                    }
                }
                return (builder.Capacity == builder.Count) ? builder.MoveToImmutable() : builder.ToImmutable();
            }
        }

        ImmutableArray<MethodSymbol> _lazyCtors;   // .ctor
        IFieldSymbol _lazyContextField;   // protected Pchp.Core.Context <ctx>;
        IFieldSymbol _lazyRuntimeFieldsField; // internal Pchp.Core.PhpArray <runtimeFields>;
        SynthesizedStaticFieldsHolder/*!*/_staticsContainer; // class __statics { ... }

        /// <summary>
        /// Defined type members, methods, fields and constants.
        /// Does not include synthesized members.
        /// </summary>
        List<Symbol> _lazyMembers;

        public SourceFileSymbol ContainingFile => _file;

        Location CreateLocation(TextSpan span) => Location.Create(ContainingFile.SyntaxTree, span);

        Location CreateLocation(Devsense.PHP.Text.Span span) => CreateLocation(span.ToTextSpan());

        #region Construction

        internal static SourceTypeSymbol Create(SourceFileSymbol file, TypeDecl syntax)
        {
            if (syntax is AnonymousTypeDecl at)
            {
                return new SourceAnonymousTypeSymbol(file, at);
            }

            if (syntax.MemberAttributes.IsTrait())
            {
                return new SourceTraitTypeSymbol(file, syntax);
            }

            return new SourceTypeSymbol(file, syntax);
        }

        protected SourceTypeSymbol(SourceFileSymbol file, TypeDecl syntax)
        {
            Contract.ThrowIfNull(file);

            _syntax = syntax;
            _file = file;

            //
            _staticsContainer = new SynthesizedStaticFieldsHolder(this);
        }

        /// <summary>
        /// Creates instance of self to be used for creating new versions of the same type.
        /// </summary>
        protected virtual SourceTypeSymbol NewSelf() => new SourceTypeSymbol(_file, _syntax);

        private SourceTypeSymbol InitNewSelf(SourceTypeSymbol self, NamedTypeSymbol baseType, ImmutableArray<NamedTypeSymbol> ifacesType, ImmutableArray<TraitUse> traitUses, int version, SourceTypeSymbol nextVersion)
        {
            self._lazyBaseType = baseType;
            self._lazyInterfacesType = ifacesType;
            self._lazyTraitUses = traitUses;
            self._version = version;
            self._nextVersion = nextVersion;

            //
            return self;
        }

        /// <summary>
        /// Writes up <see cref="_lazyBaseType"/> and <see cref="_lazyInterfacesType"/>.
        /// </summary>
        private void ResolveBaseTypes(Dictionary<QualifiedName, INamedTypeSymbol> resolved = null)
        {
            if (_lazyInterfacesType.IsDefault) // not resolved yet
            {
                if (_syntax.BaseClass == null && _syntax.ImplementsList.Length == 0 && !HasTraitUses)
                {
                    // simple class - no interfaces, no base class, no traits:
                    _lazyBaseType = ((_syntax.MemberAttributes & (PhpMemberAttributes.Static | PhpMemberAttributes.Interface)) == 0) // not static class nor interface
                        ? DeclaringCompilation.GetSpecialType(SpecialType.System_Object)
                        : null;

                    _lazyTraitUses = ImmutableArray<TraitUse>.Empty;
                    _lazyInterfacesType = ImmutableArray<NamedTypeSymbol>.Empty;
                }
                else
                {
                    // resolve slowly:

                    // get possible type signature [ BaseType?, Interface1, ..., InterfaceN ]
                    // Single slots may refer to a MissingTypeSymbol or an ambiguous type symbol
                    var tsignature = ResolveTypeSignature(
                        this,
                        resolved ?? new Dictionary<QualifiedName, INamedTypeSymbol>(),
                        this.DeclaringCompilation)
                    .ToArray();

                    Debug.Assert(tsignature.Length >= 1 && tsignature.Skip(1).All(x => x.Symbol != null));  // all the types (except for the base) have bound symbol (it might be ErrorTypeSymbol but never null)

                    lock (_basetypes_sync) // critical section:
                    {
                        if (_lazyInterfacesType.IsDefault) // double checked lock :(
                        {
                            var diagnostics = DiagnosticBag.GetInstance();

                            ResolveBaseTypesNoLock(tsignature, diagnostics);
                            AddDeclarationDiagnostics(diagnostics);
                            diagnostics.Free();

                            //
                            Debug.Assert(_lazyInterfacesType.IsDefault == false);
                            Debug.Assert(_lazyTraitUses.IsDefault == false);
                        }
                    }
                }
            }
        }

        readonly object _basetypes_sync = new object();

        ImmutableArray<NamedTypeSymbol> SelectInterfaces(TypeRefSymbol[] tsignature, ImmutableArray<NamedTypeSymbol> boundtypes)
        {
            Debug.Assert(tsignature.Length == boundtypes.Length);
            List<NamedTypeSymbol> list = null;

            for (int i = 0; i < tsignature.Length; i++)
            {
                if (tsignature[i].Attributes.IsInterface())
                {
                    if (list == null) list = new List<NamedTypeSymbol>(1);
                    list.Add(boundtypes[i]);
                }
            }

            //
            return (list != null) ? list.AsImmutableOrEmpty() : ImmutableArray<NamedTypeSymbol>.Empty;
        }

        static ImmutableArray<TraitUse> SelectTraitUses(SourceTypeSymbol owner, TypeRefSymbol[] tsignature, ImmutableArray<NamedTypeSymbol> boundtypes)
        {
            Debug.Assert(tsignature.Length == boundtypes.Length);
            List<TraitUse> list = null;

            for (int i = 0; i < tsignature.Length; i++)
            {
                if (tsignature[i].Attributes.IsTrait())
                {
                    if (list == null) list = new List<TraitUse>(1);
                    list.Add(new TraitUse(owner, boundtypes[i], tsignature[i].TraitAdaptations));
                }
            }

            //
            return (list != null) ? list.AsImmutableOrEmpty() : ImmutableArray<TraitUse>.Empty;
        }

        void ResolveBaseTypesNoLock(TypeRefSymbol[] tsignature, DiagnosticBag diagnostics)
        {
            Debug.Assert(tsignature.Length >= 1, "at least base type should be in tsignature");   // [0] is base class

            // check all types are supported
            foreach (var t in tsignature)
            {
                if (t.Symbol != null)
                {
                    if (t.Symbol.Arity != 0 && !t.Attributes.IsTrait())
                    {
                        diagnostics.Add(CreateLocation(t.TypeRef.Span), ErrorCode.ERR_NotYetImplemented, "Using generic types");
                    }
                    if (t.Symbol is ErrorTypeSymbol err && err.CandidateReason != CandidateReason.Ambiguous)
                    {
                        diagnostics.Add(CreateLocation(t.TypeRef.Span), ErrorCode.ERR_TypeNameCannotBeResolved, t.TypeRef.ClassName.ToString());
                    }
                }
            }

            if (!diagnostics.HasAnyErrors())
            {
                var tmperrors = new List<Diagnostic>();

                // collect variations of possible base types
                var variations = Variations(tsignature.Select(t => t.Symbol).AsImmutable(), this.ContainingFile);

                // instantiate versions
                bool self = true;       // first we update bound types in current instance, then we create versions
                int lastVersion = 0;    // the SourceTypeSymbol version, 0 ~ a single version, >0 ~ multiple version
                foreach (var v in variations)
                {
                    Debug.Assert(v.Length == tsignature.Length, "variation should be the same length as tsignature");

                    // Since we create various versions,
                    // some of them may be invalid (wrong base type etc.).
                    // Only create valid version. If there is no valid version, report the errors.

                    if (CheckForErrors(tmperrors, tsignature, v))
                    {
                        Debug.Assert(tmperrors.Count != 0, "expecting errors");
                        continue;
                    }

                    var v_base = v[0];
                    var v_interfaces = SelectInterfaces(tsignature, v);

                    // create the variation:

                    if (self)
                    {
                        _lazyBaseType = v_base;
                        _lazyTraitUses = SelectTraitUses(this, tsignature, v);
                        _lazyInterfacesType = v_interfaces;

                        self = false;
                    }
                    else
                    {
                        // create next version of this type with already resolved type signature
                        var vinst = NewSelf();
                        _nextVersion = InitNewSelf(vinst, v_base, v_interfaces, SelectTraitUses(vinst, tsignature, v), ++lastVersion, _nextVersion);
                    }
                }

                if (self)   // no valid variation found, report errors
                {
                    Debug.Assert(tmperrors != null && tmperrors.Count != 0, "expecting some errors if we did not resolve anything");
                    var spansreported = new HashSet<TextSpan>();
                    foreach (var err in tmperrors)
                    {
                        if (spansreported.Add(err.Location.SourceSpan))
                        {
                            diagnostics.Add(err);
                        }
                    }
                }

                if (lastVersion != 0)
                {
                    _version = ++lastVersion;

                    diagnostics.Add(CreateLocation(_syntax.HeadingSpan), ErrorCode.WRN_AmbiguousDeclaration, this.FullName);
                }
            }

            if (diagnostics.HasAnyErrors())
            {
                // default with unbound error types:
                _lazyBaseType = tsignature[0].Symbol;
                _lazyTraitUses = ImmutableArray<TraitUse>.Empty;
                _lazyInterfacesType = tsignature.Where(x => x.Attributes.IsInterface()).Select(x => x.Symbol).AsImmutable();
            }
            else
            {
                // check for circular bases in all versions
                for (var t = this; t != null; t = t.NextVersion)
                {
                    CheckForCircularBase(t, diagnostics);
                }
            }
        }

        bool CheckForErrors(List<Diagnostic> errors, TypeRefSymbol[] tsignature, ImmutableArray<NamedTypeSymbol> boundtypes)
        {
            int count = errors.Count;

            // check the base:
            var v_base = boundtypes[0];
            if (v_base != null)
            {
                if (v_base.IsInterface || v_base.IsTraitType() || v_base.IsStructType())
                {
                    errors.Add(MessageProvider.Instance.CreateDiagnostic(
                        ErrorCode.ERR_CannotExtendFrom, CreateLocation(tsignature[0].TypeRef.Span),
                        this.FullName, v_base.IsInterface ? "interface" : v_base.IsStructType() ? "struct" : "trait", v_base.MakeQualifiedName()));
                }
            }

            // check implements and use
            for (int i = 0; i < tsignature.Length; i++)
            {
                var target = tsignature[i].Attributes;
                var bound = boundtypes[i];

                if ((target.IsInterface() && !bound.IsInterface) || // implements non-interface
                    (target.IsTrait() && !bound.IsTraitType()))     // use non-trait
                {
                    errors.Add(MessageProvider.Instance.CreateDiagnostic(
                        target.IsInterface() ? ErrorCode.ERR_CannotImplementNonInterface : ErrorCode.ERR_CannotUseNonTrait,
                        CreateLocation(tsignature[i].TypeRef.Span),
                        this.FullName, bound.MakeQualifiedName()));
                }
            }

            //
            return count != errors.Count;
        }

        void CheckForCircularBase(SourceTypeSymbol t, DiagnosticBag diagnostics)
        {
            if (t.BaseType != null && t.BaseType.SpecialType != SpecialType.System_Object)
            {
                var set = new HashSet<SourceTypeSymbol>();  // only care about source symbols
                for (var b = t; b != null; b = b.BaseType as SourceTypeSymbol)
                {
                    if (set.Add(b) == false)
                    {
                        diagnostics.Add(CreateLocation(_syntax.HeadingSpan), ErrorCode.ERR_CircularBase, t.BaseType, t);
                        break;
                    }
                }
            }
        }

        static ErrorTypeSymbol CreateInCycleErrorType(QualifiedName name)
        {
            return new MissingMetadataTypeSymbol(name.ClrName(), 0, false);
        }

        static IEnumerable<ImmutableArray<T>> Variations<T>(ImmutableArray<T> types, SourceFileSymbol containingFile) where T : NamedTypeSymbol
        {
            if (types.Length == 0)
            {
                return new ImmutableArray<T>[] { ImmutableArray<T>.Empty };
            }

            // skip non ambiguous types
            int i = 0;
            while (i < types.Length && (types[i] == null || types[i].IsErrorType() == false))
            {
                i++;
            }

            if (i == types.Length)
            {
                return new ImmutableArray<T>[] { types };
            }

            // [prefix, {types[i]}, Variations(types[i+1..])

            var prefix = (i == 0)
                ? ImmutableArray<T>.Empty
                : ImmutableArray.CreateRange(types.Take(i));

            var suffixvariations = Variations(types.RemoveRange(0, i + 1), containingFile);

            var ambiguity = (types[i] as ErrorTypeSymbol).CandidateSymbols.Cast<T>().ToList();

            // in case there is an ambiguity that is declared in current scope unconditionally, pick this one and ignore the others
            var best = ambiguity.FirstOrDefault(x => x is SourceTypeSymbol srct && ReferenceEquals(srct.ContainingFile, containingFile) && !srct.Syntax.IsConditional);
            if (best != null)
            {
                ambiguity = new List<T>(1) { best };
            }

            return ambiguity.SelectMany(t =>
            {
                var list = new List<ImmutableArray<T>>();

                // prefix + t + |suffixvariations|
                foreach (var v in suffixvariations)
                {
                    var bldr = ImmutableArray.CreateBuilder<T>(types.Length);
                    bldr.AddRange(prefix);
                    bldr.Add(t);
                    bldr.AddRange(v);
                    // prefix + t + v
                    list.Add(bldr.ToImmutable());
                }

                //
                return list;
            });
        }

        struct TypeRefSymbol
        {
            public INamedTypeRef TypeRef;
            public NamedTypeSymbol Symbol;

            public PhpMemberAttributes Attributes;

            /// <summary>Optional. Trait adaptations.</summary>
            public TraitsUse.TraitAdaptation[] TraitAdaptations;
        }

        /// <summary>
        /// Gets type signature of the type [BaseType or NULL, Interface1, ..., InterfaceN, Trait1, ..., TraitN]
        /// </summary>
        private static IEnumerable<TypeRefSymbol> ResolveTypeSignature(SourceTypeSymbol type, Dictionary<QualifiedName, INamedTypeSymbol>/*!*/resolved, PhpCompilation compilation)
        {
            Debug.Assert(!resolved.ContainsKey(type.FullName));
            resolved[type.FullName] = null; // recursion check

            var syntax = type.Syntax;

            // base type or NULL
            if (syntax.BaseClass != null)   // a class with base
            {
                var baseTypeName = syntax.BaseClass.ClassName;

                yield return new TypeRefSymbol()
                {
                    TypeRef = syntax.BaseClass,
                    Symbol = (NamedTypeSymbol)compilation.GlobalSemantics.ResolveType(baseTypeName, resolved) ?? CreateInCycleErrorType(baseTypeName),
                };
            }
            else if ((syntax.MemberAttributes & (PhpMemberAttributes.Static | PhpMemberAttributes.Interface)) != 0) // a static class or an interface
            {
                yield return default(TypeRefSymbol);  // nothing
            }
            else // a class without base
            {
                yield return new TypeRefSymbol() { Symbol = compilation.CoreTypes.Object.Symbol };
            }

            // base interfaces
            if (syntax.ImplementsList.Length != 0)
            {
                var visited = new HashSet<QualifiedName>(); // set of visited interfaces

                if (type.IsInterface)
                {
                    visited.Add(type.FullName);
                }

                foreach (var i in syntax.ImplementsList)
                {
                    var qname = i.ClassName;
                    if (visited.Add(qname))
                    {
                        yield return new TypeRefSymbol()
                        {
                            TypeRef = i,
                            Symbol = (NamedTypeSymbol)compilation.GlobalSemantics.ResolveType(qname, resolved) ?? CreateInCycleErrorType(qname),
                            Attributes = PhpMemberAttributes.Interface,
                        };
                    }
                }
            }

            // used traits // consider: move to semantic binder?
            foreach (var m in syntax.Members.OfType<TraitsUse>())
            {
                foreach (var namedt in m.TraitsList.OfType<INamedTypeRef>())
                {
                    yield return new TypeRefSymbol()
                    {
                        TypeRef = namedt,
                        Symbol = (NamedTypeSymbol)compilation.GlobalSemantics.ResolveType(namedt.ClassName, resolved) ?? CreateInCycleErrorType(namedt.ClassName),
                        Attributes = PhpMemberAttributes.Trait,
                        TraitAdaptations = m.TraitAdaptationBlock?.Adaptations,
                    };
                }
            }

            // end recursion check:
            resolved.Remove(type.FullName);
        }

        #endregion

        /// <summary>
        /// Collects declaration diagnostics.
        /// </summary>
        internal virtual void GetDiagnostics(DiagnosticBag diagnostic)
        {
            if (IsAbstract && IsSealed)
            {
                // Cannot use the final modifier on an abstract class
                diagnostic.Add(CreateLocation(_syntax.HeadingSpan), ErrorCode.ERR_FinalAbstractClassDeclared);
            }

            if (BaseType != null && BaseType.IsSealed)
            {
                // Cannot inherit from final class '{0}'
                diagnostic.Add(DiagnosticBagExtensions.ParserDiagnostic(
                    _file.SyntaxTree, _syntax.BaseClass.Span,
                    Devsense.PHP.Errors.Errors.FinalClassExtended, ((IPhpTypeSymbol)BaseType).FullName.ToString()));
            }

            // fields re-definition:
            foreach (var f in GetMembers().OfType<SourceFieldSymbol>())
            {
                var basedef = f.OverridenDefinition;
                if (basedef != null && f.DeclaredAccessibility < basedef.DeclaredAccessibility)
                {
                    // ERR: Access level to {0}::${1} must be {2} (as in class {3}) or weaker
                    diagnostic.Add(f.Locations[0], ErrorCode.ERR_PropertyAccessibilityError, FullName, f.Name, basedef.DeclaredAccessibility.ToString().ToLowerInvariant(), ((IPhpTypeSymbol)basedef.ContainingType).FullName);
                }
            }

            // trait use check
            foreach (var m in Syntax.Members.OfType<TraitsUse>())
            {
                foreach (var t in m.TraitsList)
                {
                    if (t is Devsense.PHP.Syntax.Ast.PrimitiveTypeRef)
                    {
                        diagnostic.Add(CreateLocation(t.Span), ErrorCode.ERR_PrimitiveTypeNameMisused, t);
                    }
                }
            }

            // bind & diagnose attributes
            GetAttributes();
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
                Interlocked.CompareExchange(ref _lazyMembers, members, null);
            }

            return _lazyMembers;
        }

        protected virtual MethodSymbol CreateSourceMethod(MethodDecl m) => new SourceMethodSymbol(this, m);

        IEnumerable<MethodSymbol> LoadMethods()
        {
            return _syntax.Members.OfType<MethodDecl>().Select(CreateSourceMethod);
        }

        IEnumerable<FieldSymbol> LoadFields()
        {
            var binder = new SemanticsBinder(DeclaringCompilation, ContainingFile.SyntaxTree, locals: null, routine: null, self: this);

            // fields
            foreach (var flist in _syntax.Members.OfType<FieldDeclList>())
            {
                var fkind = (flist.Modifiers & PhpMemberAttributes.Static) == 0
                    ? PhpPropertyKind.InstanceField
                    : flist.IsAppStatic()
                        ? PhpPropertyKind.AppStaticField
                        : PhpPropertyKind.StaticField;

                flist.TryGetCustomAttributes(out var attrs);

                foreach (var f in flist.Fields)
                {
                    yield return new SourceFieldSymbol(this, f.Name.Value,
                        CreateLocation(f.NameSpan),
                        flist.Modifiers.GetAccessibility(), flist.PHPDoc,
                        fkind,
                        initializer: (f.Initializer != null) ? binder.BindWholeExpression(f.Initializer, BoundAccess.Read).SingleBoundElement() : null,
                        customAttributes: attrs);
                }
            }

            // constants
            foreach (var clist in _syntax.Members.OfType<ConstDeclList>())
            {
                foreach (var c in clist.Constants)
                {
                    yield return new SourceFieldSymbol(this, c.Name.Name.Value,
                        CreateLocation(c.Name.Span),
                        clist.Modifiers.GetAccessibility(), clist.PHPDoc,
                        PhpPropertyKind.ClassConstant,
                        binder.BindWholeExpression(c.Initializer, BoundAccess.Read).SingleBoundElement());
                }
            }
        }

        public override ImmutableArray<MethodSymbol> StaticConstructors => ImmutableArray<MethodSymbol>.Empty;

        public sealed override ImmutableArray<MethodSymbol> InstanceConstructors
        {
            get
            {
                if (_lazyCtors.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyCtors, CreateInstanceConstructors());
                }

                return _lazyCtors;
            }
        }

        protected virtual ImmutableArray<MethodSymbol> CreateInstanceConstructors() => SynthesizedPhpCtorSymbol.CreateCtors(this);

        /// <summary>
        /// Gets magic <c>__invoke</c> method of class or <c>null</c>.
        /// Gets <c>null</c> if the type is trait or interface or <c>__invoke</c> is not defined.
        /// </summary>
        MethodSymbol TryGetMagicInvoke()
        {
            if (this.IsInterface || this.IsTrait)
            {
                return null;
            }

            return GetMembersByPhpName(Devsense.PHP.Syntax.Name.SpecialMethodNames.Invoke.Value)
                .OfType<MethodSymbol>()
                .Where(m => !m.IsStatic)
                .SingleOrDefault();
        }

        /// <summary>
        /// Gets <c>__destruct</c> method of class or <c>null</c>.
        /// Gets <c>null</c> if the type is trait or interface or <c>__destruct</c> is not defined.
        /// </summary>
        MethodSymbol TryGetDestruct()
        {
            if (this.IsInterface || this.IsTrait)
            {
                return null;
            }

            return GetMembersByPhpName(Devsense.PHP.Syntax.Name.SpecialMethodNames.Destruct.Value)
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
                if (_lazyBaseType != null)
                {
                    return _lazyBaseType;
                }
                else if (_syntax.BaseClass == null)
                {
                    // no resolution needed
                    _lazyBaseType = ((_syntax.MemberAttributes & (PhpMemberAttributes.Static | PhpMemberAttributes.Interface)) == 0) // not static class nor interface
                        ? DeclaringCompilation.GetSpecialType(SpecialType.System_Object)
                        : null;

                    return _lazyBaseType;
                }
                else
                {
                    ResolveBaseTypes();
                    return _lazyBaseType;
                }
            }
        }

        /// <summary>
        /// Gets type declaration syntax node.
        /// </summary>
        internal TypeDecl Syntax => _syntax;

        public override int Arity => 0;

        internal override IModuleSymbol ContainingModule => _file.SourceModule;

        public override Symbol ContainingSymbol => _file.SourceModule;

        /// <summary>
        /// Gets value indicating the type is declared conditionally.
        /// </summary>
        internal override bool IsConditional => _syntax.IsConditional || _postponedDeclaration;

        /// <summary>
        /// Marks the symbol as its declaration should not be performed at the beginning of the file, since it mat depend on statements evaluated before the declaration.
        /// </summary>
        internal void PostponedDeclaration()
        {
            _postponedDeclaration = true;
        }
        bool _postponedDeclaration = false;

        internal override PhpCompilation DeclaringCompilation => _file.DeclaringCompilation;

        public override string Name => FullName.Name.Value;

        public override string NamespaceName => string.Join(".", FullName.Namespaces);

        public override string MetadataName
        {
            get
            {
                var name = base.MetadataName;

                // count declarations with the same name
                // to avoid duplicities in PE metadata
                var decls = this.DeclaringCompilation.SourceSymbolCollection.GetDeclaredTypes(this.FullName).ToList();
                Debug.Assert(decls.Count != 0);

                // name?num#version

                if (decls.Count != 1)
                {
                    // ?order
                    name += "?" + decls.TakeWhile(f => f.Syntax != this.Syntax).Count().ToString();   // index within types with the same name
                }
                else if (Syntax.IsConditional)
                {
                    // ?
                    name += "?";
                }

                // #version
                if (_version != 0)
                {
                    name += "#" + _version;
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

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                if (FullName == NameUtils.SpecialNames.System)
                {
                    // class "System" would be in conflict with everything in .NET,
                    // let's workaround it by making it internal
                    return Accessibility.Internal;
                }

                // all classes in PHP are public:
                return Accessibility.Public;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        internal override bool IsInterface => _syntax.MemberAttributes.IsInterface();

        public virtual bool IsTrait => false;

        public override bool IsAbstract => _syntax.MemberAttributes.IsAbstract() || IsInterface;

        public override bool IsSealed => _syntax.MemberAttributes.IsSealed();

        public override bool IsStatic => _syntax.MemberAttributes.IsStatic();

        public override bool IsSerializable => false;

        public override ImmutableArray<Location> Locations => ImmutableArray.Create(CreateLocation(_syntax.Span));

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

        internal override bool MangleName => Arity != 0;

        public override ImmutableArray<NamedTypeSymbol> Interfaces => GetDeclaredInterfaces(null);

        /// <summary>
        /// Bound trait uses.
        /// </summary>
        protected ImmutableArray<TraitUse> TraitUses
        {
            get
            {
                if (!HasTraitUses)
                {
                    _lazyTraitUses = ImmutableArray<TraitUse>.Empty;
                }
                else
                {
                    // resolve slowly:
                    ResolveBaseTypes();
                }

                Debug.Assert(!_lazyTraitUses.IsDefault);
                return _lazyTraitUses;
            }
        }

        public IEnumerable<Symbol> TraitMembers
        {
            get
            {
                if (TraitUses.IsEmpty)
                {
                    return Enumerable.Empty<SynthesizedMethodSymbol>();
                }
                else
                {
                    HashSet<string> fieldsset = null;

                    return TraitUses
                        .SelectMany(t => t.GetMembers())
                        // duplicity checks for fields
                        .Where(s =>
                        {
                            if (s is SynthesizedFieldSymbol fld)
                            {
                                if (fieldsset == null) fieldsset = new HashSet<string>();

                                if (fieldsset.Add(fld.Name) == false)
                                {
                                    // field already declared
                                    return false;
                                }

                                if (this.Syntax.Members.OfType<FieldDeclList>().SelectMany(list => list.Fields).Select(fdecl => fdecl.Name.Value).Contains(fld.Name))
                                {
                                    // field already declared by containing class
                                    return false;
                                }
                            }

                            return true;
                        });
                }
            }
        }

        /// <summary>
        /// Enumerates all members including fields that will be contained in <c>_statics</c> holder.
        /// </summary>
        internal IEnumerable<Symbol> GetDeclaredMembers()
        {
            IEnumerable<Symbol> members = EnsureMembers();

            // lookup trait members
            if (!TraitUses.IsEmpty)
            {
                members = members.Concat(TraitMembers);
            }

            return members;
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return GetDeclaredMembers()
                .Where(m => m.ContainingType == this)   // skips members contained in _statics holder
                .AsImmutable();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return GetDeclaredMembers()
                .Where(m => m.ContainingType == this)   // skips members contained in _statics holder
                .Where(s => s.Name == name) // s.Name.StringsEqual(name, ignoreCase))
                .AsImmutable();
        }

        public override ImmutableArray<Symbol> GetMembersByPhpName(string name)
        {
            return GetDeclaredMembers()
                .Where(s => s.ContainingType == this)   // skips members contained in _statics holder
                .Where(s => s.PhpName().StringsEqual(name, ignoreCase: true))
                .AsImmutable();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            var result = ImmutableArray<NamedTypeSymbol>.Empty;

            if (!_staticsContainer.IsEmpty)
            {
                result = result.Add(_staticsContainer);
            }

            //
            return result;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
            => GetTypeMembers().Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();

        /// <summary>
        /// See <c>PhpTypeAttribute</c>
        /// - 0: type is not selected to be autloaded.<br/>
        /// - 1: type is marked to be autoloaded.<br/>
        /// - 2: type is marked to be autoloaded and it is the only unconditional declaration in its source file.<br/>
        /// </summary>
        byte ResolvePhpTypeAutoloadFlag()
        {
            var options = DeclaringCompilation.Options;

            bool isautoload = false;

            // match the class in classmap:
            if (options.Autoload_ClassMapFiles != null &&
                options.Autoload_ClassMapFiles.Count != 0)
            {
                var relativeFilePath = this.ContainingFile.RelativeFilePath;
                isautoload = options.Autoload_ClassMapFiles.Contains(relativeFilePath);
            }

            // match the class in psr map:
            if (!isautoload && // autoload not resolved yet
                options.Autoload_PSR4 != null &&
                options.Autoload_PSR4.Count != 0 &&
                (FullName.Name.Value + ".php").Equals(ContainingFile.FileName, StringComparison.InvariantCultureIgnoreCase) // "file name" must match "class name .php"
                )
            {
                var fullname = FullNameString;
                var relativeFilePath = this.ContainingFile.RelativeFilePath;

                foreach (var prefix_path in options.Autoload_PSR4)
                {
                    // prefix must match (it may or may not be suffixed with slash)
                    if (fullname.StartsWith(prefix_path.prefix, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // cut off name component of prefix, keep trailing slash
                        // "UniqueGlobalClass" -> ""
                        // "Monolog\" -> "Monolog\"
                        // "A\B\" -> "A\B\"
                        var nsprefix = prefix_path.prefix;
                        nsprefix = nsprefix.Substring(0, nsprefix.LastIndexOf(QualifiedName.Separator) + 1);

                        // path+{fullname without prefix namespace} == {relativeFilePath}
                        var expectedpath = PhpFileUtilities.NormalizeSlashes(Path.Combine(prefix_path.path, fullname.Substring(nsprefix.Length) + ".php"));
                        if (expectedpath.Equals(relativeFilePath, StringComparison.InvariantCultureIgnoreCase))
                        {
                            isautoload = true;
                            break;
                        }
                    }
                }
            }

            // determine autoload flag:
            if (isautoload)
            {
                // source file does not have any side effects?
                // 1: autoload but with side effects
                // 2: autoload without side effect

                // function declaration, other types, global code, ... ?

                foreach (var f in ContainingFile.SyntaxTree.Functions)
                {
                    if (f.ContainingType == null) // function declared in global code (not in a method)
                        return 1;
                }

                foreach (var t in ContainingFile.SyntaxTree.Types)
                {
                    if (t is AnonymousTypeDecl)
                        continue;

                    if (t.IsConditional || t != this.Syntax)
                        return 1;
                }

                // ContainingFile.SyntaxTree.Root contains a global code?
                var statements = new List<Statement>(ContainingFile.SyntaxTree.Root.Statements);
                for (int i = 0; i < statements.Count; i++)
                {
                    var stmt = statements[i];

                    if (stmt is NamespaceDecl ns)
                    {
                        statements.AddRange(ns.Body.Statements);
                    }
                    else if (stmt is EmptyStmt || stmt is TypeDecl || stmt is DeclareStmt)
                    {
                        continue;
                    }
                    else
                    {
                        return 1;
                    }
                }

                // naive recursion prevention...
                _autoloadFlag = 2;

                // check base type, interfaces, and used traits
                foreach (var d in this.GetDependentSourceTypeSymbols().OfType<IPhpTypeSymbol>())
                {
                    var a = d.AutoloadFlag;
                    if (a == 0 || a == 1) return 1;
                }

                // no side effects found
                return 2;
            }

            //
            return 0;
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            var attrs = base.GetAttributes();

            // [PhpTypeAttribute(string FullName, string FileName, byte Autoload)]
            attrs = attrs.Add(new SynthesizedAttributeData(
                    DeclaringCompilation.CoreMethods.Ctors.PhpTypeAttribute_string_string_byte,
                    ImmutableArray.Create(
                        DeclaringCompilation.CreateTypedConstant(FullNameString),
                        DeclaringCompilation.CreateTypedConstant(ContainingFile.RelativeFilePath.ToString()),
                        DeclaringCompilation.CreateTypedConstant(AutoloadFlag)
                    ),
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty));

            // attributes from syntax node
            if (this.Syntax.TryGetCustomAttributes(out var customattrs))
            {
                // initialize attribute data if necessary:
                customattrs
                    .OfType<SourceCustomAttribute>()
                    .ForEach(x => x.Bind(this, this.ContainingFile));

                //
                attrs = attrs.AddRange(customattrs);
            }

            return attrs;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            var ifaces = GetDeclaredInterfaces(null);

            //
            if (TryGetMagicInvoke() != null)
            {
                // __invoke => IPhpCallable
                ifaces = ifaces.Add(DeclaringCompilation.CoreTypes.IPhpCallable);
            }

            if (TryGetDestruct() != null)
            {
                // __destruct => IDisposable
                ifaces = ifaces.Add(DeclaringCompilation.GetSpecialType(SpecialType.System_IDisposable));
            }

            return ifaces;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            if (_syntax.ImplementsList.Length == 0)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }
            else
            {
                ResolveBaseTypes();
                return _lazyInterfacesType;
            }
        }

        internal override IEnumerable<IMethodSymbol> GetMethodsToEmit()
        {
            return InstanceConstructors.Concat(EnsureMembers().OfType<IMethodSymbol>());
        }

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            // special fields
            if (ReferenceEquals(ContextStore?.ContainingType, this))
            {
                yield return ContextStore;
            }

            if (ReferenceEquals(RuntimeFieldsStore?.ContainingType, this))
            {
                yield return RuntimeFieldsStore;
            }

            // trait instances
            foreach (var t in this.TraitUses)
            {
                yield return t.TraitInstanceField;
            }

            foreach (var m in GetMembers())
            {
                if (m is FieldSymbol f)
                {
                    // declared fields
                    if (m is SourceFieldSymbol srcf && srcf.IsRedefinition)
                    {
                        // field redeclares its parent member, discard
                        continue;
                    }

                    yield return f;
                }
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

        public override byte AutoloadFlag => 0; // anonymous classes are never autoloaded (nor even declared in Context)

        public override string MetadataName => Name;

        public override bool IsSealed => true;

        public override bool IsAnonymousType => true;

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public SourceAnonymousTypeSymbol(SourceFileSymbol file, AnonymousTypeDecl syntax)
            : base(file, syntax)
        {
        }

        protected override SourceTypeSymbol NewSelf() => new SourceAnonymousTypeSymbol(ContainingFile, Syntax);
    }
}
