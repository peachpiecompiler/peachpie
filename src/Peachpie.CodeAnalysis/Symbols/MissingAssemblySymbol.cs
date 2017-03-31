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
    /// A <see cref="MissingAssemblySymbol"/> is a special kind of <see cref="AssemblySymbol"/> that represents
    /// an assembly that couldn't be found.
    /// </summary>
    internal sealed class MissingAssemblySymbol : AssemblySymbol
    {
        readonly AssemblyIdentity identity;

        public MissingAssemblySymbol(AssemblyIdentity identity)
        {
            Debug.Assert(identity != null);
            this.identity = identity;
        }

        internal sealed override bool IsMissing
        {
            get
            {
                return true;
            }
        }

        internal override bool IsLinked
        {
            get
            {
                return false;
            }
        }

        internal override Symbol GetDeclaredSpecialTypeMember(SpecialMember member)
        {
            return null;
        }

        public override AssemblyIdentity Identity
        {
            get
            {
                return identity;
            }
        }

        public override Version AssemblyVersionPattern => null;

        internal override ImmutableArray<byte> PublicKey
        {
            get { return Identity.PublicKey; }
        }

        public override ImmutableArray<ModuleSymbol> Modules => ImmutableArray<ModuleSymbol>.Empty;

        public override int GetHashCode()
        {
            return identity.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MissingAssemblySymbol);
        }

        public bool Equals(MissingAssemblySymbol other)
        {
            if ((object)other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return identity.Equals(other.Identity);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        internal override void SetLinkedReferencedAssemblies(ImmutableArray<AssemblySymbol> assemblies)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<AssemblySymbol> GetLinkedReferencedAssemblies()
        {
            return ImmutableArray<AssemblySymbol>.Empty;
        }

        public override INamespaceSymbol GlobalNamespace => null;

        public override ICollection<string> TypeNames
        {
            get
            {
                return SpecializedCollections.EmptyCollection<string>();
            }
        }

        public override ICollection<string> NamespaceNames
        {
            get
            {
                return SpecializedCollections.EmptyCollection<string>();
            }
        }

        internal override NamedTypeSymbol LookupTopLevelMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies, bool digThroughForwardedTypes)
        {
            return new MissingMetadataTypeSymbol(emittedName.FullName, emittedName.ForcedArity, emittedName.IsMangled);
        }

        internal override NamedTypeSymbol GetDeclaredSpecialType(SpecialType type)
        {
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                return false;
            }
        }

        public override AssemblyMetadata GetMetadata() => null;
    }
}
