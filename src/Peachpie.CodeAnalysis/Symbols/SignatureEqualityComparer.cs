using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// TODO: Helper comparer for methods signature and name equality. (overridable)
    /// </summary>
    internal sealed class SignatureEqualityComparer : IEqualityComparer<IMethodSymbol>
    {
        public static readonly SignatureEqualityComparer Instance = new SignatureEqualityComparer();

        private SignatureEqualityComparer() { }

        public bool Equals(IMethodSymbol x, IMethodSymbol y)
        {
            if (x.Name == y.Name)
            {
                var px = x.Parameters;
                var py = y.Parameters;

                if (px.Length == py.Length)
                {
                    for (int i = 0; i < px.Length; i++)
                    {
                        if (!SymbolEqualityComparer.Default.Equals(px[i].Type, py[i].Type))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public int GetHashCode(IMethodSymbol obj)
        {
            var hash = 0; ;
            var ps = obj.Parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                hash = unchecked(hash + ps[i].Type.GetHashCode());
            }

            return hash ^ obj.MetadataName.GetHashCode();
        }
    }
}
