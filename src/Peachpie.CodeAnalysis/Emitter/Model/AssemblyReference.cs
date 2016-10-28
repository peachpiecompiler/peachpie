using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Pchp.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cci = Microsoft.Cci;

namespace Pchp.CodeAnalysis.Emitter
{
    internal sealed class AssemblyReference : Cci.IAssemblyReference
    {
        // assembly symbol that represents the target assembly:
        private readonly AssemblySymbol _targetAssembly;

        internal AssemblyReference(AssemblySymbol assemblySymbol)
        {
            Debug.Assert((object)assemblySymbol != null);
            _targetAssembly = assemblySymbol;
        }

        public AssemblyIdentity MetadataIdentity => _targetAssembly.Identity;

        public override string ToString()
        {
            return _targetAssembly.ToString();
        }

        #region Cci.IAssemblyReference

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        string Cci.IAssemblyReference.Culture
        {
            get
            {
                return MetadataIdentity.CultureName;
            }
        }

        bool Cci.IAssemblyReference.IsRetargetable
        {
            get
            {
                return MetadataIdentity.IsRetargetable;
            }
        }

        AssemblyContentType Cci.IAssemblyReference.ContentType
        {
            get
            {
                return MetadataIdentity.ContentType;
            }
        }

        ImmutableArray<byte> Cci.IAssemblyReference.PublicKeyToken
        {
            get { return MetadataIdentity.PublicKeyToken; }
        }

        Version Cci.IAssemblyReference.Version
        {
            get { return MetadataIdentity.Version; }
        }

        string Cci.IAssemblyReference.GetDisplayName()
        {
            return MetadataIdentity.GetDisplayName();
        }

        string Cci.INamedEntity.Name
        {
            get { return MetadataIdentity.Name; }
        }

        Cci.IAssemblyReference Cci.IModuleReference.GetContainingAssembly(EmitContext context)
        {
            return this;
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }

        #endregion
    }
}
