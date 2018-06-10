using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// TODO: Helper comparer for methods signature and name equality. (overridable)
    /// </summary>
    internal sealed class SignatureEqualityComparer : IEqualityComparer<IMethodSymbol>
    {
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
                        if (px[i].Type != py[i].Type)
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
