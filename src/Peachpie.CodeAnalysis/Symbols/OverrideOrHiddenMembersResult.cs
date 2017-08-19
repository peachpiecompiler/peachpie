using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Groups the information computed by MakeOverriddenOrHiddenMembers.
    /// </summary>
    internal sealed class OverriddenOrHiddenMembersResult
    {
        public static readonly OverriddenOrHiddenMembersResult Empty =
            new OverriddenOrHiddenMembersResult(
                ImmutableArray<Symbol>.Empty,
                ImmutableArray<Symbol>.Empty,
                ImmutableArray<Symbol>.Empty);

        private readonly ImmutableArray<Symbol> _overriddenMembers;
        public ImmutableArray<Symbol> OverriddenMembers { get { return _overriddenMembers; } }

        private readonly ImmutableArray<Symbol> _hiddenMembers;
        public ImmutableArray<Symbol> HiddenMembers { get { return _hiddenMembers; } }

        private readonly ImmutableArray<Symbol> _runtimeOverriddenMembers;
        public ImmutableArray<Symbol> RuntimeOverriddenMembers { get { return _runtimeOverriddenMembers; } }

        private OverriddenOrHiddenMembersResult(
            ImmutableArray<Symbol> overriddenMembers,
            ImmutableArray<Symbol> hiddenMembers,
            ImmutableArray<Symbol> runtimeOverriddenMembers)
        {
            _overriddenMembers = overriddenMembers;
            _hiddenMembers = hiddenMembers;
            _runtimeOverriddenMembers = runtimeOverriddenMembers;
        }

        public static OverriddenOrHiddenMembersResult Create(
            ImmutableArray<Symbol> overriddenMembers,
            ImmutableArray<Symbol> hiddenMembers,
            ImmutableArray<Symbol> runtimeOverriddenMembers)
        {
            if (overriddenMembers.IsEmpty && hiddenMembers.IsEmpty && runtimeOverriddenMembers.IsEmpty)
            {
                return Empty;
            }
            else
            {
                return new OverriddenOrHiddenMembersResult(overriddenMembers, hiddenMembers, runtimeOverriddenMembers);
            }
        }

        internal static Symbol GetOverriddenMember(Symbol substitutedOverridingMember, Symbol overriddenByDefinitionMember)
        {
            Debug.Assert(!substitutedOverridingMember.IsDefinition);

            if ((object)overriddenByDefinitionMember != null)
            {
                NamedTypeSymbol overriddenByDefinitionContaining = overriddenByDefinitionMember.ContainingType;
                NamedTypeSymbol overriddenByDefinitionContainingTypeDefinition = overriddenByDefinitionContaining.OriginalDefinition;
                for (NamedTypeSymbol baseType = substitutedOverridingMember.ContainingType.BaseType;
                    (object)baseType != null;
                    baseType = baseType.BaseType)
                {
                    if (baseType.OriginalDefinition == overriddenByDefinitionContainingTypeDefinition)
                    {
                        if (baseType == overriddenByDefinitionContaining)
                        {
                            return overriddenByDefinitionMember;
                        }

                        return overriddenByDefinitionMember.OriginalDefinition.SymbolAsMember(baseType);
                    }
                }

                throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
            }

            return null;
        }

        /// <summary>
        /// It is not suitable to call this method on a <see cref="OverriddenOrHiddenMembersResult"/> object
        /// associated with a member within substituted type, <see cref="GetOverriddenMember(Symbol, Symbol)"/>
        /// should be used instead.
        /// </summary>
        internal Symbol GetOverriddenMember()
        {
            foreach (var overriddenMember in _overriddenMembers)
            {
                if (overriddenMember.IsAbstract || overriddenMember.IsVirtual || overriddenMember.IsOverride)
                {
                    return overriddenMember;
                }
            }

            return null;
        }
    }
}
