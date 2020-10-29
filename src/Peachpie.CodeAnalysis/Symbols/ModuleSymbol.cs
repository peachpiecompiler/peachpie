using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Symbols;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract partial class ModuleSymbol : Symbol, IModuleSymbol, IModuleSymbolInternal
    {
        ModuleReferences<AssemblySymbol> _moduleReferences;

        internal override ModuleSymbol ContainingModule => null;

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

        INamespaceSymbol IModuleSymbol.GlobalNamespace => GlobalNamespace;

        public abstract NamespaceSymbol GlobalNamespace { get; }

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
        public virtual ImmutableArray<AssemblySymbol> ReferencedAssemblySymbols => _moduleReferences.Symbols;

        ImmutableArray<IAssemblySymbol> IModuleSymbol.ReferencedAssemblySymbols => StaticCast<IAssemblySymbol>.From(ReferencedAssemblySymbols);

        /// <summary>
        /// A helper method for ReferenceManager to set assembly identities for assemblies 
        /// referenced by this module and corresponding AssemblySymbols.
        /// </summary>
        internal void SetReferences(ModuleReferences<AssemblySymbol> moduleReferences, SourceAssemblySymbol originatingSourceAssemblyDebugOnly = null)
        {
            Debug.Assert(HasReferencesSet == false);
            Debug.Assert(moduleReferences != null);

            _moduleReferences = moduleReferences;
        }

        internal bool HasReferencesSet => _moduleReferences != null;

        public virtual ModuleMetadata GetMetadata()
        {
            throw new NotImplementedException();
        }

        public virtual INamespaceSymbol GetModuleNamespace(INamespaceSymbol namespaceSymbol)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        /// <summary>
        /// Lookup a top level type referenced from metadata, names should be
        /// compared case-sensitively.
        /// </summary>
        /// <param name="emittedName">
        /// Full type name, possibly with generic name mangling.
        /// </param>
        /// <returns>
        /// Symbol for the type, or MissingMetadataSymbol if the type isn't found.
        /// </returns>
        /// <remarks></remarks>
        internal abstract NamedTypeSymbol LookupTopLevelMetadataType(ref MetadataTypeName emittedName);
    }
}
