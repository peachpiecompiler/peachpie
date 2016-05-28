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
    /// internal static class &lt;Script&gt; { ... }
    /// </summary>
    class SynthesizedScriptTypeSymbol : NamedTypeSymbol, IWithSynthesized
    {
        readonly PhpCompilation _compilation;

        /// <summary>
        /// Optional. Real assembly entry point method.
        /// </summary>
        internal MethodSymbol EntryPointSymbol { get; set; }

        /// <summary>
        /// Method that enumerates all referenced global functions.
        /// 
        /// EnumerateReferencedFunctions(Action&lt;string, RuntimeMethodHandle&gt; callback)
        /// </summary>
        internal MethodSymbol EnumerateReferencedFunctionsSymbol => _enumerateReferencedFunctionsSymbol ?? (_enumerateReferencedFunctionsSymbol = CreateEnumerateReferencedFunctionsSymbol());
        MethodSymbol _enumerateReferencedFunctionsSymbol;

        /// <summary>
        /// Method that enumerates all script files.
        /// 
        /// EnumerateScripts(Action&lt;string, RuntimeMethodHandle&gt; callback)
        /// </summary>
        internal MethodSymbol EnumerateScriptsSymbol => _enumerateScripsSymbol ?? (_enumerateScripsSymbol = CreateEnumerateScriptsSymbol());
        MethodSymbol _enumerateScripsSymbol;

        /// <summary>
        /// Method that enumerates all app-wide global constants.
        /// 
        /// EnumerateScripts(Action&lt;string name, PhpValue value, bool ignorecase&gt; callback)
        /// </summary>
        internal MethodSymbol EnumerateConstantsSymbol => _enumerateConstantsSymbol ?? (_enumerateConstantsSymbol = CreateEnumerateConstantsSymbol());
        MethodSymbol _enumerateConstantsSymbol;

        /// <summary>
        /// Optional static ctor if needed.
        /// </summary>
        SynthesizedCctorSymbol _lazyCctorSymbol;

        /// <summary>
        /// Additional type members.
        /// </summary>
        private List<Symbol> _lazyMembers = new List<Symbol>();

        public SynthesizedScriptTypeSymbol(PhpCompilation compilation)
        {
            _compilation = compilation;
        }

        public override int Arity => 0;

        public override Symbol ContainingSymbol => _compilation.SourceModule;

        internal override IModuleSymbol ContainingModule => _compilation.SourceModule;

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

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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
            var list = new List<Symbol>()
            {
                this.EnumerateReferencedFunctionsSymbol,
                this.EnumerateScriptsSymbol,
                this.EnumerateConstantsSymbol,
            };

            //
            if (EntryPointSymbol != null)
                list.Add(EntryPointSymbol);

            if (_lazyCctorSymbol != null)
                list.Add(_lazyCctorSymbol);

            //
            list.AddRange(_lazyMembers);

            //
            return list.AsImmutable();
        }

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().Where(m => m.Name == name).AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => _lazyMembers.OfType<NamedTypeSymbol>().AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => _lazyMembers.OfType<NamedTypeSymbol>().Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit() => _lazyMembers.OfType<FieldSymbol>().AsImmutable();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

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
        /// Method that enumerates all referenced global functions.
        /// EnumerateReferencedFunctions(Action&lt;string, RuntimeMethodHandle&gt; callback)
        /// </summary>
        MethodSymbol CreateEnumerateReferencedFunctionsSymbol()
        {
            var compilation = DeclaringCompilation;
            var action_T2 = compilation.GetWellKnownType(WellKnownType.System_Action_T2);
            var action_string_method = action_T2.Construct(compilation.CoreTypes.String, compilation.CoreTypes.RuntimeMethodHandle);

            var method = new SynthesizedMethodSymbol(this, "EnumerateReferencedFunctions", true, false, compilation.CoreTypes.Void, Accessibility.Public);
            method.SetParameters(new SynthesizedParameterSymbol(method, action_string_method, 0, RefKind.None, "callback"));

            //
            return method;
        }

        /// <summary>
        /// Method that enumerates all script Main functions.
        /// EnumerateScripts(Action&lt;string, RuntimeMethodHandle&gt; callback)
        /// </summary>
        MethodSymbol CreateEnumerateScriptsSymbol()
        {
            var compilation = DeclaringCompilation;
            var action_T2 = compilation.GetWellKnownType(WellKnownType.System_Action_T2);
            var action_string_method = action_T2.Construct(compilation.CoreTypes.String, compilation.CoreTypes.RuntimeMethodHandle);

            var method = new SynthesizedMethodSymbol(this, "EnumerateScripts", true, false, compilation.CoreTypes.Void, Accessibility.Public);
            method.SetParameters(new SynthesizedParameterSymbol(method, action_string_method, 0, RefKind.None, "callback"));

            //
            return method;
        }

        /// <summary>
        /// Method that enumerates all app-wide global constants.
        /// EnumerateConstants(Action&lt;string, PhpValue, bool&gt; callback)
        /// </summary>
        MethodSymbol CreateEnumerateConstantsSymbol()
        {
            var compilation = DeclaringCompilation;
            var action_T3 = compilation.GetWellKnownType(WellKnownType.System_Action_T3);
            var action_string_value_bool = action_T3.Construct(compilation.CoreTypes.String, compilation.CoreTypes.PhpValue, compilation.CoreTypes.Boolean);

            var method = new SynthesizedMethodSymbol(this, "EnumerateConstants", true, false, compilation.CoreTypes.Void, Accessibility.Public);
            method.SetParameters(new SynthesizedParameterSymbol(method, action_string_value_bool, 0, RefKind.None, "callback"));

            //
            return method;
        }

        #region IWithSynthesized

        MethodSymbol IWithSynthesized.GetOrCreateStaticCtorSymbol()
        {
            if (_lazyCctorSymbol == null)
                _lazyCctorSymbol = new SynthesizedCctorSymbol(this);

            return _lazyCctorSymbol;
        }

        SynthesizedFieldSymbol IWithSynthesized.GetOrCreateSynthesizedField(TypeSymbol type, string name, Accessibility accessibility, bool isstatic)
        {
            var field = _lazyMembers.OfType<SynthesizedFieldSymbol>().FirstOrDefault(f => f.Name == name && f.IsStatic == isstatic && f.Type == type);
            if (field == null)
            {
                field = new SynthesizedFieldSymbol(this, type, name, accessibility, isstatic);
                _lazyMembers.Add(field);
            }

            return field;
        }

        void IWithSynthesized.AddTypeMember(NamedTypeSymbol nestedType)
        {
            Contract.ThrowIfNull(nestedType);

            _lazyMembers.Add(nestedType);
        }

        #endregion
    }
}
