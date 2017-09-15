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
using System.Reflection;

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
        /// Assembly version pattern with wildcards represented by <see cref="ushort.MaxValue"/>,
        /// or null if the version string specified in the <see cref="AssemblyVersionAttribute"/> doesn't contain a wildcard.
        /// 
        /// For example, 
        ///   AssemblyVersion("1.2.*") is represented as 1.2.65535.65535,
        ///   AssemblyVersion("1.2.3.*") is represented as 1.2.3.65535.
        /// </summary>
        public abstract Version AssemblyVersionPattern { get; }

        /// <summary>
        /// Does this symbol represent a missing assembly.
        /// </summary>
        internal virtual bool IsMissing => false;

        /// <summary>
        /// Assembly is /l-ed by compilation that is using it as a reference.
        /// </summary>
        internal abstract bool IsLinked { get; }

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
            Debug.Assert((object)_corLibrary == null || (object)_corLibrary == corLibrary);
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
            if (fullyQualifiedMetadataName == null)
            {
                throw new ArgumentNullException(nameof(fullyQualifiedMetadataName));
            }

            return this.GetTypeByMetadataName(fullyQualifiedMetadataName, includeReferences: false, isWellKnownType: false);
        }

        /// <summary>
        /// Lookup a type within the assembly using its canonical CLR metadata name.
        /// </summary>
        /// <param name="metadataName"></param>
        /// <param name="includeReferences">
        /// If search within assembly fails, lookup in assemblies referenced by the primary module.
        /// For source assembly, this is equivalent to all assembly references given to compilation.
        /// </param>
        /// <param name="isWellKnownType">
        /// Extra restrictions apply when searching for a well-known type.  In particular, the type must be public.
        /// </param>
        /// <param name="useCLSCompliantNameArityEncoding">
        /// While resolving the name, consider only types following CLS-compliant generic type names and arity encoding (ECMA-335, section 10.7.2).
        /// I.e. arity is inferred from the name and matching type must have the same emitted name and arity.
        /// </param>
        /// <param name="warnings">
        /// A diagnostic bag to receive warnings if we should allow multiple definitions and pick one.
        /// </param>
        /// <returns>Null if the type can't be found.</returns>
        internal NamedTypeSymbol GetTypeByMetadataName(
            string metadataName,
            bool includeReferences,
            bool isWellKnownType,
            bool useCLSCompliantNameArityEncoding = false,
            DiagnosticBag warnings = null)
        {
            NamedTypeSymbol type = null;
            MetadataTypeName mdName;

            if (metadataName.IndexOf('+') >= 0)
            {
                var parts = metadataName.Split(s_nestedTypeNameSeparators);
                Debug.Assert(parts.Length > 0);
                mdName = MetadataTypeName.FromFullName(parts[0], useCLSCompliantNameArityEncoding);
                type = GetTypeByMetadataName(mdName.FullName, includeReferences, isWellKnownType);
                //type = GetTopLevelTypeByMetadataName(ref mdName, assemblyOpt: null, includeReferences: includeReferences, isWellKnownType: isWellKnownType, warnings: warnings);
                for (int i = 1; (object)type != null && !type.IsErrorType() && i < parts.Length; i++)
                {
                    mdName = MetadataTypeName.FromTypeName(parts[i]);
                    NamedTypeSymbol temp = type.LookupMetadataType(ref mdName);
                    type = temp;  //(!isWellKnownType || IsValidWellKnownType(temp)) ? temp : null;
                }
                //throw new NotImplementedException();
            }
            else
            {
                mdName = MetadataTypeName.FromFullName(metadataName, useCLSCompliantNameArityEncoding);
                //type = GetTopLevelTypeByMetadataName(ref mdName, assemblyOpt: null, includeReferences: includeReferences, isWellKnownType: isWellKnownType, warnings: warnings);
                type = LookupTopLevelMetadataType(ref mdName, true);
                if (includeReferences && (type == null || type.IsErrorType()))
                {
                    type = null;

                    var assemblies = new HashSet<AssemblySymbol>() { this };
                    assemblies.UnionWith(this.DeclaringCompilation.GetBoundReferenceManager().GetReferencedAssemblies().Select(x => (AssemblySymbol)x.Value));

                    foreach (var ass in assemblies)
                    {
                        var t = ass.LookupTopLevelMetadataType(ref mdName, true);
                        if (t != null && !t.IsErrorType())
                        {
                            if ((object)type != null && type.ContainingAssembly != t.ContainingAssembly)
                            {
                                // TODO: ambiguity
                                Debug.Assert(false, "ambiguity");
                                return null;
                            }

                            type = (NamedTypeSymbol)t;
                        }
                    }
                }
            }

            return ((object)type == null || type.IsErrorType()) ? null : type;
        }

        static readonly char[] s_nestedTypeNameSeparators = new char[] { '+' };

        /// <summary>
        /// Not yet known value is represented by ErrorTypeSymbol.UnknownResultType
        /// </summary>
        private Symbol[] _lazySpecialTypeMembers;

        /// <summary>
        /// Lookup member declaration in predefined CorLib type in this Assembly. Only valid if this 
        /// assembly is the Cor Library
        /// </summary>
        internal virtual Symbol GetDeclaredSpecialTypeMember(SpecialMember member)
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

        /// <summary>
        /// Return an array of assemblies referenced by this assembly, which are linked (/l-ed) by 
        /// each compilation that is using this AssemblySymbol as a reference. 
        /// If this AssemblySymbol is linked too, it will be in this array too.
        /// </summary>
        internal abstract ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies();
        internal abstract void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies);

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
