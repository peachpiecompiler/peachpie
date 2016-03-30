using Microsoft.CodeAnalysis;
using Pchp.CodeAnalysis.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis
{
    partial class OverloadResolution
    {
        /// <summary>
        /// Checks all candidates expect or do not expect <c>this</c> parameter on evaluation stack.
        /// </summary>
        /// <returns></returns>
        internal bool IsStaticIsConsistent()
        {
            if (_candidates.Length != 0)
            {
                return _candidates.All(c => c.IsStatic == _candidates[0].IsStatic);
            }

            return true;
        }

        internal bool IsImplicitlyDeclared(int index)
        {
            return _candidates.Length != 0 && _candidates.All(c => c.ParameterCount > index && c.Parameters[index].IsImplicitlyDeclared);
        }

        internal TypeSymbol ConsistentParameterType(int index)
        {
            if (_candidates.Length != 0)
            {
                var first = _candidates[0];
                var t = (first.ParameterCount > index) ? first.Parameters[index].Type : null;
                if (_candidates.Length == 1 || _candidates.All(c => c.ParameterCount > index && c.Parameters[index].Type == t))
                    return t;
            }

            return null;
        }

        /// <summary>
        /// Gets candidates having i-th parameter of given type.
        /// </summary>
        internal ImmutableArray<MethodSymbol> CandidatesWithParameterType(TypeSymbol pt, int pi)
        {
            var selected = _candidates.Where(c => c.ParameterCount > pi && c.Parameters[pi].Type == pt);
            return selected.ToImmutableArray();
        }

        internal MethodKind IsMethodKindConsistent()
        {
            if (_candidates.Length != 0)
            {
                var kind = _candidates[0].MethodKind;
                if (_candidates.Length == 1 || _candidates.All(c => c.MethodKind == kind))
                    return kind;
            }

            return (MethodKind)(-1);
        }
    }
}
