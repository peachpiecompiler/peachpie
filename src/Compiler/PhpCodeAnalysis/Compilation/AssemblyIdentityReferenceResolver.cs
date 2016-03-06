using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Pchp.CodeAnalysis
{
    class AssemblyIdentityReferenceResolver : MetadataReferenceResolver
    {
        readonly MetadataReferenceResolver _resolver;

        public AssemblyIdentityReferenceResolver(MetadataReferenceResolver next)
        {
            Contract.ThrowIfNull(next);
            _resolver = next;
        }

        string PublicKeyToken(ImmutableArray<byte> key)
        {
            StringBuilder sb = new StringBuilder(16);
            foreach (byte b in key)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        #region Object

        public override bool Equals(object other) => object.ReferenceEquals(this, other);

        public override int GetHashCode() => unchecked((int)0xdeadbeef);

        #endregion

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            AssemblyIdentity identity;
            AssemblyIdentityParts parts;
            var result = ImmutableArray<PortableExecutableReference>.Empty;
            if (AssemblyIdentity.TryParseDisplayName(reference, out identity, out parts))
            {
                if (identity.Name == "mscorlib")
                {
                    result = _resolver.ResolveReference(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll", baseFilePath, properties);
                }

                if (result.IsDefaultOrEmpty)
                {
                    var gacreq = AssemblyIdentityParts.Version | AssemblyIdentityParts.Name | AssemblyIdentityParts.PublicKeyToken;
                    if ((parts & gacreq) == gacreq)
                    {
                        var fname = $"C:\\Windows\\Microsoft.Net\\assembly\\GAC_MSIL\\{identity.Name}\\v4.0_{identity.Version.ToString(4)}__{PublicKeyToken(identity.PublicKeyToken)}\\{identity.Name}.dll";
                        result = _resolver.ResolveReference(fname, baseFilePath, properties);
                    }
                }

                if (result.IsDefaultOrEmpty)
                {
                    result = _resolver.ResolveReference(identity.Name + ".dll", baseFilePath, properties);
                }
            }

            if (result.IsDefaultOrEmpty)
            {
                result = _resolver.ResolveReference(reference, baseFilePath, properties);
            }

            //
            return result;
        }
    }
}
