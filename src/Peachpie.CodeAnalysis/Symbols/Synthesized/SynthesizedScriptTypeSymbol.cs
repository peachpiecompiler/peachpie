using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    class SynthesizedScriptTypeSymbol : NamedTypeSymbol
    {
        readonly PhpCompilation _compilation;

        /// <summary>
        /// Optional. Real assembly entry point method.
        /// </summary>
        internal MethodSymbol EntryPointSymbol { get; set; }

        /// <summary>
        /// Method that enumerates all referenced global functions.
        /// 
        /// BuiltinFunctions(Action&lt;string, RuntimeMethodHandle&gt; callback)
        /// </summary>
        internal MethodSymbol EnumerateBuiltinFunctionsSymbol => _enumerateBuiltinFunctionsSymbol ?? (_enumerateBuiltinFunctionsSymbol = CreateEnumerateBuiltinFunctionsSymbol());
        MethodSymbol _enumerateBuiltinFunctionsSymbol;

        /// <summary>
        /// Method that enumerates all referenced global functions.
        /// 
        /// BuiltinTypes(Action&lt;string, RuntimeMethodHandle&gt; callback)
        /// </summary>
        internal MethodSymbol EnumerateBuiltinTypesSymbol => _enumerateBuiltinTypesSymbol ?? (_enumerateBuiltinTypesSymbol = CreateBuiltinTypesSymbol());
        MethodSymbol _enumerateBuiltinTypesSymbol;

        /// <summary>
        /// Method that enumerates all script files.
        /// 
        /// EnumerateScripts(Action&lt;string, MainDelegate&gt; callback)
        /// </summary>
        internal MethodSymbol EnumerateScriptsSymbol => _enumerateScripsSymbol ?? (_enumerateScripsSymbol = CreateEnumerateScriptsSymbol());
        MethodSymbol _enumerateScripsSymbol;

        /// <summary>
        /// Method that enumerates all app-wide global constants.
        /// 
        /// BuiltinConstants(Context.IConstantsComposition composer)
        /// </summary>
        internal MethodSymbol EnumerateConstantsSymbol => _enumerateConstantsSymbol ?? (_enumerateConstantsSymbol = CreateEnumerateConstantsSymbol());
        MethodSymbol _enumerateConstantsSymbol;

        /// <summary>
        /// Additional type members.
        /// </summary>
        private List<Symbol> _lazyMembers = new List<Symbol>();

        public SynthesizedScriptTypeSymbol(PhpCompilation compilation)
        {
            _compilation = compilation;
        }

        public override int Arity => 0;

        internal override bool HasTypeArgumentsCustomModifiers => false;

        public override ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal) => GetEmptyTypeArgumentCustomModifiers(ordinal);

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

        public override bool IsSerializable => false;

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
                this.EnumerateBuiltinFunctionsSymbol,
                this.EnumerateBuiltinTypesSymbol,
                this.EnumerateScriptsSymbol,
                this.EnumerateConstantsSymbol,
            };

            //
            if (EntryPointSymbol != null)
            {
                list.Add(EntryPointSymbol);
            }

            //
            list.AddRange(_lazyMembers);

            //
            return list.AsImmutable();
        }

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().Where(m => m.Name == name).AsImmutable();

        public override ImmutableArray<Symbol> GetMembersByPhpName(string name) => ImmutableArray<Symbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => _lazyMembers.OfType<NamedTypeSymbol>().AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => _lazyMembers.OfType<NamedTypeSymbol>().Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit() => _lazyMembers.OfType<FieldSymbol>().AsImmutable();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> StaticConstructors => ImmutableArray<MethodSymbol>.Empty;

        /// <summary>
        /// Method that enumerates all referenced global functions.
        /// BuiltinFunctions(Action&lt;string, RuntimeMethodHandle&gt; callback)
        /// </summary>
        MethodSymbol CreateEnumerateBuiltinFunctionsSymbol()
        {
            var compilation = DeclaringCompilation;
            var action_T2 = compilation.GetWellKnownType(WellKnownType.System_Action_T2);
            var action_string_method = action_T2.Construct(compilation.CoreTypes.String, compilation.CoreTypes.RuntimeMethodHandle);

            var method = new SynthesizedMethodSymbol(this, "BuiltinFunctions", true, false, compilation.CoreTypes.Void, Accessibility.Public);
            method.SetParameters(new SynthesizedParameterSymbol(method, action_string_method, 0, RefKind.None, "callback"));

            //
            return method;
        }

        /// <summary>
        /// Method that enumerates all referenced global types.
        /// EnumerateReferencedTypes(Action&lt;string, RuntimeTypeHandle&gt; callback)
        /// </summary>
        MethodSymbol CreateBuiltinTypesSymbol()
        {
            var compilation = DeclaringCompilation;
            var action_T = compilation.GetWellKnownType(WellKnownType.System_Action_T);
            var action_phptypeinfo = action_T.Construct(compilation.CoreTypes.PhpTypeInfo);

            var method = new SynthesizedMethodSymbol(this, "BuiltinTypes", true, false, compilation.CoreTypes.Void, Accessibility.Public);
            method.SetParameters(new SynthesizedParameterSymbol(method, action_phptypeinfo, 0, RefKind.None, "callback"));

            //
            return method;
        }
        /// <summary>
        /// Method that enumerates all script Main functions.
        /// EnumerateScripts(Action&lt;string, MainDelegate&gt; callback)
        /// </summary>
        MethodSymbol CreateEnumerateScriptsSymbol()
        {
            var compilation = DeclaringCompilation;
            var action_T2 = compilation.GetWellKnownType(WellKnownType.System_Action_T2);
            var action_string_delegate = action_T2.Construct(compilation.CoreTypes.String, compilation.CoreTypes.MainDelegate);

            var method = new SynthesizedMethodSymbol(this, "EnumerateScripts", true, false, compilation.CoreTypes.Void, Accessibility.Public);
            method.SetParameters(new SynthesizedParameterSymbol(method, action_string_delegate, 0, RefKind.None, "callback"));

            //
            return method;
        }

        /// <summary>
        /// Method that enumerates all app-wide global constants.
        /// EnumerateConstants(Context.IConstantsComposition composer)
        /// </summary>
        MethodSymbol CreateEnumerateConstantsSymbol()
        {
            var compilation = DeclaringCompilation;
            var t = (TypeSymbol)compilation.GetTypeByMetadataName(CoreTypes.IConstantsCompositionFullName);
            Debug.Assert(t != null);

            var method = new SynthesizedMethodSymbol(this, "BuiltinConstants", true, false, compilation.CoreTypes.Void, Accessibility.Public);
            method.SetParameters(new SynthesizedParameterSymbol(method, t, 0, RefKind.None, name: "composer"));

            //
            return method;
        }
    }
}
