using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Pchp.CodeAnalysis.Symbols
{
    internal abstract class AssemblySymbol : Symbol, IAssemblySymbol
    {
        AssemblySymbol _corLibrary;

        public override Symbol ContainingSymbol => null;

        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual INamespaceSymbol GlobalNamespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public abstract AssemblyIdentity Identity { get; }

        /// <summary>
        /// The system assembly, which provides primitive types like Object, String, etc., e.g. mscorlib.dll. 
        /// The value is MissingAssemblySymbol if none of the referenced assemblies can be used as a source for the 
        /// primitive types and the owning assembly cannot be used as the source too. Otherwise, it is one of 
        /// the referenced assemblies returned by GetReferencedAssemblySymbols() method or the owning assembly.
        /// </summary>
        internal AssemblySymbol CorLibrary
        {
            get
            {
                return _corLibrary;
            }
        }

        /// <summary>
        /// A helper method for ReferenceManager to set the system assembly, which provides primitive 
        /// types like Object, String, etc., e.g. mscorlib.dll. 
        /// </summary>
        internal void SetCorLibrary(AssemblySymbol corLibrary)
        {
            Debug.Assert((object)_corLibrary == null);
            _corLibrary = corLibrary;
        }

        public virtual bool IsCorLibrary => false;

        public virtual bool IsPchpCorLibrary => false;

        public override bool IsAbstract => false;

        public override bool IsExtern => false;

        public virtual bool IsInteractive => false;

        public override bool IsOverride => false;

        public override bool IsSealed => false;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override SymbolKind Kind => SymbolKind.Assembly;

        public override ImmutableArray<Location> Locations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Lookup declaration for predefined CorLib type in this Assembly.
        /// </summary>
        /// <returns>The symbol for the pre-defined type or an error type if the type is not defined in the core library.</returns>
        internal virtual NamedTypeSymbol GetDeclaredSpecialType(SpecialType type)
        {
            // TODO: cache SpecialType
            return CorLibrary.GetTypeByMetadataName(type.GetMetadataName());
        }

        /// <summary>
        /// Gets the symbol for the pre-defined type from core library associated with this assembly.
        /// </summary>
        /// <returns>The symbol for the pre-defined type or an error type if the type is not defined in the core library.</returns>
        internal NamedTypeSymbol GetSpecialType(SpecialType type)
        {
            return GetDeclaredSpecialType(type);
        }

        public virtual bool MightContainExtensionMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IEnumerable<IModuleSymbol> IAssemblySymbol.Modules => Modules;

        public abstract ImmutableArray<ModuleSymbol> Modules { get; }

        public virtual ICollection<string> NamespaceNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ICollection<string> TypeNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal virtual ImmutableArray<byte> PublicKey { get { throw new NotSupportedException(); } }

        public virtual AssemblyMetadata GetMetadata()
        {
            throw new NotImplementedException();
        }

        INamedTypeSymbol IAssemblySymbol.GetTypeByMetadataName(string fullyQualifiedMetadataName)
            => (NamedTypeSymbol)GetTypeByMetadataName(fullyQualifiedMetadataName);

        public virtual NamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            return (NamedTypeSymbol)this.GlobalNamespace.GetTypeMembers(fullyQualifiedMetadataName).FirstOrDefault();
        }

        /// <summary>
        /// Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        /// </summary>
        private Symbol[] _lazySpecialTypeMembers;

        /// <summary>
        /// Lookup member declaration in predefined CorLib type in this Assembly. Only valid if this 
        /// assembly is the Cor Library
        /// </summary>
        internal Symbol GetDeclaredSpecialTypeMember(SpecialMember member)
        {
            if (_lazySpecialTypeMembers == null || ReferenceEquals(_lazySpecialTypeMembers[(int)member], ErrorTypeSymbol.UnknownResultType))
            {
                if (_lazySpecialTypeMembers == null)
                {
                    var specialTypeMembers = new Symbol[(int)SpecialMember.Count];

                    for (int i = 0; i < specialTypeMembers.Length; i++)
                    {
                        specialTypeMembers[i] = ErrorTypeSymbol.UnknownResultType;
                    }

                    Interlocked.CompareExchange(ref _lazySpecialTypeMembers, specialTypeMembers, null);
                }

                var descriptor = SpecialMembers.GetDescriptor(member);
                NamedTypeSymbol type = GetDeclaredSpecialType((SpecialType)descriptor.DeclaringTypeId);
                Symbol result = null;

                if (!type.IsErrorType())
                {
                    result = PhpCompilation.GetRuntimeMember(type, ref descriptor, PhpCompilation.SpecialMembersSignatureComparer.Instance, null);
                }

                Interlocked.CompareExchange(ref _lazySpecialTypeMembers[(int)member], result, ErrorTypeSymbol.UnknownResultType);
            }

            return _lazySpecialTypeMembers[(int)member];
        }

        public virtual bool GivesAccessTo(IAssemblySymbol toAssembly)
        {
            throw new NotImplementedException();
        }

        public INamedTypeSymbol ResolveForwardedType(string fullyQualifiedMetadataName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Lookup a top level type referenced from metadata, names should be
        /// compared case-sensitively.
        /// </summary>
        /// <param name="emittedName">
        /// Full type name with generic name mangling.
        /// </param>
        /// <param name="digThroughForwardedTypes">
        /// Take forwarded types into account.
        /// </param>
        /// <remarks></remarks>
        internal NamedTypeSymbol LookupTopLevelMetadataType(ref MetadataTypeName emittedName, bool digThroughForwardedTypes)
        {
            return LookupTopLevelMetadataTypeWithCycleDetection(ref emittedName, visitedAssemblies: null, digThroughForwardedTypes: digThroughForwardedTypes);
        }

        /// <summary>
        /// Lookup a top level type referenced from metadata, names should be
        /// compared case-sensitively.  Detect cycles during lookup.
        /// </summary>
        /// <param name="emittedName">
        /// Full type name, possibly with generic name mangling.
        /// </param>
        /// <param name="visitedAssemblies">
        /// List of assemblies lookup has already visited (since type forwarding can introduce cycles).
        /// </param>
        /// <param name="digThroughForwardedTypes">
        /// Take forwarded types into account.
        /// </param>
        internal abstract NamedTypeSymbol LookupTopLevelMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies, bool digThroughForwardedTypes);

        internal ErrorTypeSymbol CreateCycleInTypeForwarderErrorTypeSymbol(ref MetadataTypeName emittedName)
        {
            //DiagnosticInfo diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_CycleInTypeForwarder, emittedName.FullName, this.Name);
            //return new MissingMetadataTypeSymbol.TopLevelWithCustomErrorInfo(this.Modules[0], ref emittedName, diagnosticInfo);
            return new MissingMetadataTypeSymbol(emittedName.FullName, emittedName.ForcedArity, emittedName.IsMangled);
        }

        /// <summary>
        /// Look up the given metadata type, if it is forwarded.
        /// </summary>
        internal virtual NamedTypeSymbol TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies)
        {
            return null;
        }
    }
}
