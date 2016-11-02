using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using System.Diagnostics;
using Pchp.CodeAnalysis.Utilities;
using Devsense.PHP.Syntax.Ast;

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
    sealed partial class SourceFileSymbol : NamedTypeSymbol
    {
        readonly PhpCompilation _compilation;
        readonly GlobalCode _syntax;

        readonly SourceGlobalMethodSymbol _mainMethod;

        BaseAttributeData _lazyScriptAttribute;

        /// <summary>
        /// List of functions declared within the file.
        /// </summary>
        public ImmutableArray<SourceFunctionSymbol> Functions => _lazyMembers.OfType<SourceFunctionSymbol>().ToImmutableArray();
        
        readonly List<Symbol> _lazyMembers = new List<Symbol>();
        
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
        /// Lazily adds a function into the list of global functions declared within this file.
        /// </summary>
        internal void AddFunction(SourceFunctionSymbol routine)
        {
            Contract.ThrowIfNull(routine);
            _lazyMembers.Add(routine);
        }

        internal string RelativeFilePath => PhpFileUtilities.GetRelativePath(_syntax.ContainingSourceUnit.FilePath, _compilation.Options.BaseDirectory);

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

        public override string Name => PathUtilities.GetFileName(_syntax.ContainingSourceUnit.FilePath, true).Replace('.', '_');

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
            var builder = ImmutableArray.CreateBuilder<Symbol>(1 + _lazyMembers.Count);

            builder.Add(_mainMethod);
            builder.AddRange(_lazyMembers);

            return builder.ToImmutable();
        }

        public override ImmutableArray<Symbol> GetMembers(string name) => GetMembers().Where(x => x.Name == name).ToImmutableArray();

        public override ImmutableArray<MethodSymbol> StaticConstructors => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => _lazyMembers.OfType<NamedTypeSymbol>().AsImmutable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => _lazyMembers.OfType<NamedTypeSymbol>().Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).AsImmutable();

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit() => GetMembers().OfType<FieldSymbol>();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;
    }
}
