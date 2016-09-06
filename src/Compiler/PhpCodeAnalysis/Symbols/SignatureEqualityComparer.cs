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
            return obj.Parameters.Sum(p => p.Type.GetHashCode()) ^ obj.MetadataName.GetHashCode();
        }
    }
}
