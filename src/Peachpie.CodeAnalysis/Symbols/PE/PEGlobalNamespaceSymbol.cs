using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    internal sealed class PEGlobalNamespaceSymbol : PENamespaceSymbol
    {
        /// <summary>
        /// The module containing the namespace.
        /// </summary>
        /// <remarks></remarks>
        readonly PEModuleSymbol _moduleSymbol;

        internal PEGlobalNamespaceSymbol(PEModuleSymbol moduleSymbol)
        {
            Debug.Assert((object)moduleSymbol != null);
            _moduleSymbol = moduleSymbol;
        }

        public override Symbol ContainingSymbol => _moduleSymbol;

        internal override PEModuleSymbol ContainingPEModule => _moduleSymbol;

        public override string Name => string.Empty;

        public override bool IsGlobalNamespace => true;

        public override AssemblySymbol ContainingAssembly => _moduleSymbol.ContainingAssembly;

        internal override ModuleSymbol ContainingModule => _moduleSymbol;

        protected override void EnsureAllMembersLoaded()
        {
            if (_types == null)
            {
                var groups = _moduleSymbol.Module.GroupTypesByNamespaceOrThrow(StringComparer.OrdinalIgnoreCase);
                LazyInitializeTypes(groups);
            }
        }

        internal override PhpCompilation DeclaringCompilation => null;
    }
}
