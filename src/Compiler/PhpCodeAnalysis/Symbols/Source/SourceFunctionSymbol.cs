using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Pchp.Syntax.AST;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Represents a global PHP function.
    /// </summary>
    internal sealed class SourceFunctionSymbol : MethodSymbol
    {
        readonly PhpCompilation/*!*/_compilation;
        readonly FunctionDecl/*!*/_syntax;

        public SourceFunctionSymbol(PhpCompilation/*!*/compilation, FunctionDecl/*!*/syntax)
        {
            Contract.ThrowIfNull(compilation);
            Contract.ThrowIfNull(syntax);

            _compilation = compilation;
            _syntax = syntax;
        }

        public override string Name => NameUtils.MakeQualifiedName(_syntax.Name, _syntax.Namespace).ClrName();

        public override Symbol ContainingSymbol => _compilation.SourceModule;

        internal override IModuleSymbol ContainingModule => _compilation.SourceModule;

        public override IAssemblySymbol ContainingAssembly => _compilation.SourceAssembly;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsExtern => false;

        public override bool IsOverride => false;

        public override bool IsSealed => true;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override SymbolKind Kind => SymbolKind.Method;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override ImmutableArray<IParameterSymbol> Parameters
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ITypeSymbol ReturnType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;   // TODO: from PHPDoc

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;
    }
}
