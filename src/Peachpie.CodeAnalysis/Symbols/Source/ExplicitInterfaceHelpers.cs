using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    internal static class ExplicitInterfaceHelpers
    {
        public static ImmutableArray<T> SubstituteExplicitInterfaceImplementations<T>(ImmutableArray<T> unsubstitutedExplicitInterfaceImplementations, TypeMap map) where T : Symbol
        {
            var builder = ArrayBuilder<T>.GetInstance();
            foreach (var unsubstitutedPropertyImplemented in unsubstitutedExplicitInterfaceImplementations)
            {
                var unsubstitutedInterfaceType = unsubstitutedPropertyImplemented.ContainingType;
                Debug.Assert((object)unsubstitutedInterfaceType != null);
                var explicitInterfaceType = map.SubstituteNamedType(unsubstitutedInterfaceType);
                Debug.Assert((object)explicitInterfaceType != null);
                var name = unsubstitutedPropertyImplemented.Name; //should already be unqualified

                T substitutedMemberImplemented = null;
                foreach (var candidateMember in explicitInterfaceType.GetMembers(name))
                {
                    if (candidateMember.OriginalDefinition == unsubstitutedPropertyImplemented.OriginalDefinition)
                    {
                        substitutedMemberImplemented = (T)candidateMember;
                        break;
                    }
                }
                Debug.Assert((object)substitutedMemberImplemented != null); //if it was an explicit implementation before the substitution, it should still be after
                builder.Add(substitutedMemberImplemented);
            }

            return builder.ToImmutableAndFree();
        }
    }
}
