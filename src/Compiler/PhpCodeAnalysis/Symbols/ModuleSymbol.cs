using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract partial class ModuleSymbol : Symbol, IModuleSymbol
    {
        ModuleReferences<AssemblySymbol> _moduleReferences;

        internal override IModuleSymbol ContainingModule => null;

        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsAbstract => false;

        public override bool IsExtern
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsOverride => false;

        public override bool IsSealed => false;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override SymbolKind Kind => SymbolKind.NetModule;

        public virtual INamespaceSymbol GlobalNamespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ImmutableArray<AssemblyIdentity> ReferencedAssemblies => _moduleReferences.Identities;

        /// <summary>
        /// Returns an array of AssemblySymbol objects corresponding to assemblies referenced 
        /// by this module. Items at the same position from ReferencedAssemblies and 
        /// from ReferencedAssemblySymbols correspond to each other. If reference is 
        /// not resolved by compiler, GetReferencedAssemblySymbols returns MissingAssemblySymbol in the
        /// corresponding item.
        /// 
        /// The array and its content is provided by ReferenceManager and must not be modified.
        /// </summary>
        public virtual ImmutableArray<IAssemblySymbol> ReferencedAssemblySymbols => StaticCast<IAssemblySymbol>.From(_moduleReferences.Symbols);

        /// <summary>
        /// A helper method for ReferenceManager to set assembly identities for assemblies 
        /// referenced by this module and corresponding AssemblySymbols.
        /// </summary>
        internal void SetReferences(ModuleReferences<AssemblySymbol> moduleReferences, SourceAssemblySymbol originatingSourceAssemblyDebugOnly = null)
        {
            Debug.Assert(_moduleReferences == null);
            Debug.Assert(moduleReferences != null);

            _moduleReferences = moduleReferences;
        }

        public virtual ModuleMetadata GetMetadata()
        {
            throw new NotImplementedException();
        }

        public virtual INamespaceSymbol GetModuleNamespace(INamespaceSymbol namespaceSymbol)
        {
            throw new NotImplementedException();
        }
    }
}
