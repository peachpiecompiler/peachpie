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

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a file within the mudule as a CLR type.
    /// </summary>
    /// <remarks>
    /// namespace [DIR]{
    ///     class [FNAME] {
    ///         object [Main](){ ... }
    ///         int Index{get;};
    ///     }
    /// }</remarks>
    sealed class SourceFileSymbol : NamedTypeSymbol
    {
        readonly PhpCompilation _compilation;
        readonly GlobalCode _syntax;
        readonly SourceGlobalMethodSymbol _mainMethod;

        SynthesizedFieldSymbol _lazyIndexField;

        /// <summary>
        /// Unique ordinal of the source file.
        /// Used in runtime in bit arrays to check whether the file was included.
        /// </summary>
        public int Ordinal => _index;
        readonly int _index;

        public GlobalCode Syntax => _syntax;

        public SourceModuleSymbol SourceModule => _compilation.SourceModule;

        public SourceFileSymbol(PhpCompilation compilation, GlobalCode syntax, int index)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(syntax);
            Debug.Assert(index >= 0);

            _compilation = compilation;
            _syntax = syntax;
            _index = index;
            _mainMethod = new SourceGlobalMethodSymbol(this);
        }

        private FieldSymbol GetIndexField()
        {
            if (_lazyIndexField == null)
            {
                _lazyIndexField = new SynthesizedFieldSymbol(this, (NamedTypeSymbol)_compilation.GetSpecialType(SpecialType.System_Int32), "<Ordinal>", Accessibility.Internal, ConstantValue.Create(_index));
            }

            return _lazyIndexField;
        }

        public override string Name => PathUtilities.GetFileName(_syntax.SourceUnit.FilePath).Replace('.', '_');

        public override string NamespaceName
        {
            get
            {
                var path = _syntax.SourceUnit.FilePath;
                return "<Files>" + PathUtilities.GetDirectoryName(path);   // TODO: something nice, relative directory
            }
        }

        public override NamedTypeSymbol BaseType
        {
            get
            {
                return DeclaringCompilation.CoreTypes.Object.Symbol;
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

        public override ImmutableArray<Symbol> GetMembers() => ImmutableArray.Create((Symbol)_mainMethod, GetIndexField());

        public override ImmutableArray<Symbol> GetMembers(string name) => ImmutableArray<Symbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<IFieldSymbol> GetFieldsToEmit()
        {
            yield return GetIndexField();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;
    }
}
