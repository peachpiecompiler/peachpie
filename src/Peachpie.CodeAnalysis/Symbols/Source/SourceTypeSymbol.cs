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
        /// Is of type <c>Context</c>.
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
                        _lazyContextField = new SynthesizedFieldSymbol(this, DeclaringCompilation.CoreTypes.Context.Symbol, SpecialParameterSymbol.ContextName,
                            accessibility: this.IsSealed ? Accessibility.Private : Accessibility.Protected,
                            isStatic: false,
                            isReadOnly: true);
                    }
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
                        _lazyTraitInstanceField = new SynthesizedFieldSymbol(ContainingType, Symbol, "<>" + "trait_" + Symbol.Name,
                            accessibility: Accessibility.Private,
                            isStatic: false,
                            isReadOnly: true);
                    }

                    return _lazyTraitInstanceField;
                }
            }
            IFieldSymbol _lazyTraitInstanceField;

            /// <summary>
            /// Specifies implementation of a trait method in containing class.
            /// </summary>
            [DebuggerDisplay("{Visibility} {Name,nq} -> {SourceMethod}")]
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
                        IEnumerable<MethodSymbol> traitmethods = Symbol.GetMembers().OfType<MethodSymbol>();

                        // methods from used traits:
                        if (Symbol.OriginalDefinition is SourceTypeSymbol srct && !srct.TraitUses.IsEmpty)
                        {
                            // containing trait uses first (members will be overriden by this trait use)
                            traitmethods = srct.TraitMembers.Select(m => m.AsMember(m.ContainingType.Construct(ContainingType))).Concat(traitmethods);
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
                                Accessibility = Accessibility.Public,    // TODO: declared visibility
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
                                    var qname = a.TraitMemberName.Item1.HasValue ? a.TraitMemberName.Item1.QualifiedName : phpt.FullName;

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
                                            Accessibility = Accessibility.Public,    // TODO: declared visibility
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
                        _lazyMembersMap = map;
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
                    _lazyMembers = CreateMembers().AsImmutable();
                }

                return _lazyMembers;
            }
            ImmutableArray<Symbol> _lazyMembers;

            IEnumerable<Symbol> CreateMembers()
            {
                // TODO: constants
                // TODO: properties

                // methods
                foreach (var m in MembersMap.Values)
                {
                    if (m.SourceMethod.IsAbstract)
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
        /// Gets value indicating the type declaration is ambiguous.
        /// </summary>
        public bool HasVersions => (_version != 0);

        /// <summary>
        /// Enumerates all versions of this declaration.
        /// </summary>
        public ImmutableArray<SourceTypeSymbol> AllVersions()
        {
            ResolveBaseTypes();

            if (_nextVersion == null)
            {
                return ImmutableArray.Create(this);
            }
            else
            {
                Debug.Assert(_version != 0);
                var result = new SourceTypeSymbol[_version];
                for (var x = this; x != null; x = x.NextVersion)
                {
                    Debug.Assert(x._version > 0 && x._version <= result.Length);
                    result[x._version - 1] = x;
                }
                return ImmutableArray.Create(result);
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

        List<SourceLambdaSymbol> _lambdas;

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

        private SourceTypeSymbol NewSelf(NamedTypeSymbol baseType, ImmutableArray<NamedTypeSymbol> ifacesType, ImmutableArray<TraitUse> traitUses, int version, SourceTypeSymbol nextVersion)
        {
            var self = NewSelf();

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
        private void ResolveBaseTypes()
        {
            if (_lazyInterfacesType.IsDefault == false) // or _nextVersion != null or _lazyBaseType != null
            {
                return; // resolved
            }

            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            ResolveBaseTypes(diagnostics);
            AddDeclarationDiagnostics(diagnostics);
            diagnostics.Free();
        }

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

        ImmutableArray<TraitUse> SelectTraitUses(TypeRefSymbol[] tsignature, ImmutableArray<NamedTypeSymbol> boundtypes)
        {
            Debug.Assert(tsignature.Length == boundtypes.Length);
            List<TraitUse> list = null;

            for (int i = 0; i < tsignature.Length; i++)
            {
                if (tsignature[i].Attributes.IsTrait())
                {
                    if (list == null) list = new List<TraitUse>(1);
                    list.Add(new TraitUse(this, boundtypes[i], tsignature[i].TraitAdaptations));
                }
            }

            //
            return (list != null) ? list.AsImmutableOrEmpty() : ImmutableArray<TraitUse>.Empty;
        }

        void ResolveBaseTypes(DiagnosticBag diagnostics)
        {
            Debug.Assert(_lazyInterfacesType.IsDefault);    // not resolved yet
            Debug.Assert(_lazyTraitUses.IsDefault);         // not resolved yet

            // get possible type signature [ BaseType?, Interface1, ..., InterfaceN ]
            // Single slots may refer to a MissingTypeSymbol or an ambiguous type symbol
            var tsignature = ResolveTypeSignature(this, this.DeclaringCompilation).ToArray();
            Debug.Assert(tsignature.Length >= 1);   // [0] is base class

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
                    Debug.Assert(v.Length == tsignature.Length);

                    // Since we create various versions,
                    // some of them may be invalid (wrong base type etc.).
                    // Only create valid version. If there is no valid version, report the errors.

                    if (CheckForErrors(tmperrors, tsignature, v))
                    {
                        Debug.Assert(tmperrors.Count != 0);
                        continue;
                    }

                    var v_base = v[0];
                    var v_interfaces = SelectInterfaces(tsignature, v);
                    var v_traituses = SelectTraitUses(tsignature, v);

                    // create the variation:

                    if (self)
                    {
                        _lazyBaseType = v_base;
                        _lazyInterfacesType = v_interfaces;
                        _lazyTraitUses = v_traituses;

                        self = false;
                    }
                    else
                    {
                        // create next version of this type with already resolved type signature
                        _nextVersion = NewSelf(v_base, v_interfaces, v_traituses, ++lastVersion, _nextVersion);

                        // clone lambdas that use $this
                        if (_lambdas != null)
                        {
                            foreach (var l in _lambdas.Where(l => l.UseThis))
                            {
                                ((ILambdaContainerSymbol)_nextVersion).AddLambda(new SourceLambdaSymbol((LambdaFunctionExpr)l.Syntax, _nextVersion, l.UseThis));
                            }
                        }
                    }
                }

                if (self)   // no valid variation found, report errors
                {
                    Debug.Assert(tmperrors != null && tmperrors.Count != 0);
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
                _lazyInterfacesType = tsignature.Where(x => x.Attributes.IsInterface()).Select(x => x.Symbol).AsImmutable();
                _lazyTraitUses = ImmutableArray<TraitUse>.Empty;
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
            var best = ambiguity.FirstOrDefault(x => ReferenceEquals((x as SourceTypeSymbol)?.ContainingFile, containingFile) && !(x as SourceTypeSymbol)._syntax.IsConditional);
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
        private static IEnumerable<TypeRefSymbol> ResolveTypeSignature(SourceTypeSymbol type, PhpCompilation compilation)
        {
            var syntax = type.Syntax;

            // base type or NULL
            if (syntax.BaseClass != null)   // a class with base
            {
                var baseTypeName = syntax.BaseClass.ClassName;

                yield return new TypeRefSymbol()
                {
                    TypeRef = syntax.BaseClass,
                    Symbol = (baseTypeName == type.FullName) ? type : (NamedTypeSymbol)compilation.GlobalSemantics.GetType(baseTypeName),
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
            var visited = new HashSet<QualifiedName>(); // set of visited interfaces
            foreach (var i in syntax.ImplementsList)
            {
                var qname = i.ClassName;
                if (visited.Add(qname))
                {
                    yield return new TypeRefSymbol()
                    {
                        TypeRef = i,
                        Symbol = (qname == type.FullName) ? type : (NamedTypeSymbol)compilation.GlobalSemantics.GetType(qname),
                        Attributes = PhpMemberAttributes.Interface,
                    };
                }
            }

            // used traits // consider: move to semantic binder?
            foreach (var m in syntax.Members.OfType<TraitsUse>())
            {
                foreach (var t in m.TraitsList)
                {
                    var namedt = (INamedTypeRef)t;

                    yield return new TypeRefSymbol()
                    {
                        TypeRef = namedt,
                        Symbol = (NamedTypeSymbol)compilation.GlobalSemantics.GetType(namedt.ClassName),
                        Attributes = PhpMemberAttributes.Trait,
                        TraitAdaptations = m.TraitAdaptationBlock?.Adaptations,
                    };
                }
            }
        }

        #endregion

        #region ILambdaContainerSymbol

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

        #endregion

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
            var binder = new SemanticsBinder(locals: null, diagnostics: DeclaringCompilation.DeclarationDiagnostics);

            // fields
            foreach (var flist in _syntax.Members.OfType<FieldDeclList>())
            {
                var fkind = (flist.Modifiers & PhpMemberAttributes.Static) == 0
                    ? SourceFieldSymbol.KindEnum.InstanceField
                    : flist.IsAppStatic()
                        ? SourceFieldSymbol.KindEnum.AppStaticField
                        : SourceFieldSymbol.KindEnum.StaticField;

                foreach (var f in flist.Fields)
                {
                    yield return new SourceFieldSymbol(this, f.Name.Value,
                        CreateLocation(f.NameSpan),
                        flist.Modifiers.GetAccessibility(), f.PHPDoc ?? flist.PHPDoc,
                        fkind,
                        (f.Initializer != null) ? binder.BindWholeExpression(f.Initializer, BoundAccess.Read).GetOnlyBoundElement() : null);
                }
            }

            // constants
            foreach (var clist in _syntax.Members.OfType<ConstDeclList>())
            {
                foreach (var c in clist.Constants)
                {
                    yield return new SourceFieldSymbol(this, c.Name.Name.Value,
                        CreateLocation(c.Name.Span),
                        Accessibility.Public, c.PHPDoc ?? clist.PHPDoc,
                        SourceFieldSymbol.KindEnum.ClassConstant,
                        binder.BindWholeExpression(c.Initializer, BoundAccess.Read).GetOnlyBoundElement());
                }
            }
        }

        public override ImmutableArray<MethodSymbol> StaticConstructors => ImmutableArray<MethodSymbol>.Empty;

        public sealed override ImmutableArray<MethodSymbol> InstanceConstructors
        {
            get
            {
                var result = _lazyCtors;
                if (result.IsDefault)
                {
                    _lazyCtors = result = CreateInstanceConstructors().ToImmutableArray();
                }

                return result;
            }
        }

        protected virtual IEnumerable<MethodSymbol> CreateInstanceConstructors() => SynthesizedPhpCtorSymbol.CreateCtors(this);

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
                ResolveBaseTypes();
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

                // name<>`num#version

                // <>
                if (_syntax.IsConditional)
                {
                    name += "<>";
                }

                // `order
                if (decls.Count != 1)
                {
                    name += "`" + decls.TakeWhile(f => f.Syntax != this.Syntax).Count().ToString();   // index within types with the same name
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

        public override Accessibility DeclaredAccessibility => _syntax.MemberAttributes.GetAccessibility();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        internal override bool IsInterface => (_syntax.MemberAttributes & PhpMemberAttributes.Interface) != 0;

        public virtual bool IsTrait => false;

        public override bool IsAbstract => _syntax.MemberAttributes.IsAbstract() || IsInterface;

        public override bool IsSealed => _syntax.MemberAttributes.IsSealed();

        public override bool IsStatic => _syntax.MemberAttributes.IsStatic();

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
                ResolveBaseTypes();
                Debug.Assert(!_lazyTraitUses.IsDefault);

                return _lazyTraitUses;
            }
        }

        public IEnumerable<SynthesizedMethodSymbol> TraitMembers
        {
            get
            {
                if (TraitUses.IsEmpty)
                {
                    return Enumerable.Empty<SynthesizedMethodSymbol>();
                }
                else
                {
                    return TraitUses.SelectMany(t => t.GetMembers()).Cast<SynthesizedMethodSymbol>();
                }
            }
        }

        public override ImmutableArray<Symbol> GetMembers() => EnsureMembers().AsImmutable();

        public override ImmutableArray<Symbol> GetMembers(string name, bool ignoreCase = false)
        {
            IEnumerable<Symbol> members = EnsureMembers();

            // lookup trait members
            if (!TraitUses.IsEmpty)
            {
                members = members.Concat(TraitMembers);
            }

            //
            return members
                .Where(s => s.Name.StringsEqual(name, ignoreCase))
                .AsImmutable();
        }

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

            // [PhpTypeAttribute(FullName, FileName)]
            attrs = attrs.Add(new SynthesizedAttributeData(
                    DeclaringCompilation.CoreMethods.Ctors.PhpTypeAttribute_string_string,
                    ImmutableArray.Create(
                        new TypedConstant(DeclaringCompilation.CoreTypes.String.Symbol, TypedConstantKind.Primitive, FullName.ToString()),
                        new TypedConstant(DeclaringCompilation.CoreTypes.String.Symbol, TypedConstantKind.Primitive, ContainingFile.RelativeFilePath.ToString())),
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty));

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

            return ifaces;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            ResolveBaseTypes();

            //
            return _lazyInterfacesType;
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

            // trait instances
            foreach (var t in this.TraitUses)
            {
                yield return t.TraitInstanceField;
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

        protected override SourceTypeSymbol NewSelf() => new SourceAnonymousTypeSymbol(ContainingFile, Syntax);
    }
}
