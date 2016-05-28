using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.Syntax.AST;
using Roslyn.Utilities;
using System.Diagnostics;
using Pchp.CodeAnalysis.Utilities;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a file within the mudule as a CLR type.
    /// </summary>
    /// <remarks>
    /// namespace [DIR]{
    ///     class [FNAME] {
    ///         object [Main](){ ... }
    ///     }
    /// }</remarks>
    sealed class SourceFileSymbol : NamedTypeSymbol, IWithSynthesized
    {
        readonly PhpCompilation _compilation;
        readonly GlobalCode _syntax;

        readonly SourceGlobalMethodSymbol _mainMethod;

        /// <summary>
        /// Wrapper of main method getting <c>PhpValue</c> as a result.
        /// Used by runtime to create generic delegate to scripts main.
        /// </summary>
        SynthesizedMethodSymbol _mainMethodRegularOpt;

        BaseAttributeData _lazyScriptAttribute;

        /// <summary>
        /// List of functions declared within the file.
        /// </summary>
        public ImmutableArray<SourceFunctionSymbol> Functions => _lazyMembers.OfType<SourceFunctionSymbol>().ToImmutableArray();
        
        readonly List<Symbol> _lazyMembers = new List<Symbol>();
        
        SynthesizedCctorSymbol _lazyCctorSymbol;

        public GlobalCode Syntax => _syntax;

        public SourceModuleSymbol SourceModule => _compilation.SourceModule;

        public SourceFileSymbol(PhpCompilation compilation, GlobalCode syntax)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(syntax);

            _compilation = compilation;
            _syntax = syntax;
            _mainMethod = new SourceGlobalMethodSymbol(this);
        }

        /// <summary>
        /// Special main method representing the script global code.
        /// </summary>
        internal SourceGlobalMethodSymbol MainMethod => _mainMethod;

        /// <summary>
        /// Main method wrapper in case it does not return PhpValue.
        /// </summary>
        /// <returns></returns>
        internal SynthesizedMethodSymbol EnsureMainMethodRegular()
        {
            if (_mainMethodRegularOpt == null &&
                _mainMethod.ControlFlowGraph != null && // => main routine initialized
                _mainMethod.ReturnType != DeclaringCompilation.CoreTypes.PhpValue)
            {
                // PhpValue <Main>`0(parameters)
                _mainMethodRegularOpt = new SynthesizedMethodSymbol(
                    this, WellKnownPchpNames.GlobalRoutineName + "`0", true, false,
                    DeclaringCompilation.CoreTypes.PhpValue, Accessibility.Public);

                _mainMethodRegularOpt.SetParameters(_mainMethod.Parameters.Select(p =>
                    new SpecialParameterSymbol(_mainMethodRegularOpt, p.Type, p.Name, p.Ordinal)).ToArray());
            }

            return _mainMethodRegularOpt;
        }

        /// <summary>
        /// Lazily adds a function into the list of global functions declared within this file.
        /// </summary>
        internal void AddFunction(SourceFunctionSymbol routine)
        {
            Contract.ThrowIfNull(routine);
            _lazyMembers.Add(routine);
        }

        internal string RelativeFilePath => PhpFileUtilities.GetRelativePath(_syntax.SourceUnit.FilePath, _compilation.Options.BaseDirectory);

        /// <summary>
        /// Gets relative path excluding the file name and trailing slashes.
        /// </summary>
        internal string DirectoryRelativePath
        {
            get
            {
                return (PathUtilities.GetDirectoryName(this.RelativeFilePath) ?? string.Empty)
                    .TrimEnd(PathUtilities.AltDirectorySeparatorChar, PathUtilities.DirectorySeparatorChar)     // NormalizeRelativeDirectoryPath
                    .Replace(PathUtilities.AltDirectorySeparatorChar, PathUtilities.DirectorySeparatorChar);
            }
        }

        public override string Name => PathUtilities.GetFileName(_syntax.SourceUnit.FilePath, true);//.Replace('.', '_');

        public override string NamespaceName
        {
            get
            {
                return WellKnownPchpNames.ScriptsRootNamespace + DirectoryRelativePath;
            }
        }

        public override ImmutableArray<AttributeData> GetAttributes()
        {
            // [ScriptAttribute(RelativeFilePath)]  // TODO: LastWriteTime
            if (_lazyScriptAttribute == null)
            {
                _lazyScriptAttribute = new SynthesizedAttributeData(
                    DeclaringCompilation.CoreMethods.Ctors.ScriptAttribute_string,
                    ImmutableArray.Create(new TypedConstant(DeclaringCompilation.CoreTypes.String.Symbol, TypedConstantKind.Primitive, this.RelativeFilePath)),
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            //
            return ImmutableArray.Create<AttributeData>(_lazyScriptAttribute);
        }

        public override NamedTypeSymbol BaseType
        {
            get
            {
                return _compilation.CoreTypes.Object;
            }
        }

        public override int Arity => 0;

        public override Symbol ContainingSymbol => _compilation.SourceModule;

        internal override IModuleSymbol ContainingModule => _compilation.SourceModule;

        internal override PhpCompilation DeclaringCompilation => _compilation;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override bool IsInterface => false;

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

        internal override bool ShouldAddWinRTMembers => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override TypeLayout Layout => default(TypeLayout);

        internal override bool MangleName => false;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            var builder = ImmutableArray.CreateBuilder<Symbol>();

            builder.Add(_mainMethod);

            EnsureMainMethodRegular();
            if (_mainMethodRegularOpt != null)
                builder.Add(_mainMethodRegularOpt);

            if (_lazyCctorSymbol != null)
                builder.Add(_lazyCctorSymbol);

            builder.AddRange(_lazyMembers);

            return builder.ToImmutable();
        }

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().Where(x => x.Name == name).ToImmutableArray();

        public override ImmutableArray<MethodSymbol> StaticConstructors
        {
            get
            {
                if (_lazyCctorSymbol != null)
                    return ImmutableArray.Create<MethodSymbol>(_lazyCctorSymbol);

                return ImmutableArray<MethodSymbol>.Empty;
            }
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => _lazyMembers.OfType<NamedTypeSymbol>().AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => _lazyMembers.OfType<NamedTypeSymbol>().Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit() => GetMembers().OfType<FieldSymbol>();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

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
    }
}
