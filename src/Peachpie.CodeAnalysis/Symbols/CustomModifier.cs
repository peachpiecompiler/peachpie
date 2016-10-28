using Microsoft.CodeAnalysis;
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
    /// Represents a custom modifier (modopt/modreq).
    /// </summary>
    internal abstract partial class CSharpCustomModifier : CustomModifier
    {
        protected readonly NamedTypeSymbol modifier;

        private CSharpCustomModifier(NamedTypeSymbol modifier)
        {
            Debug.Assert((object)modifier != null);
            this.modifier = modifier;
        }

        /// <summary>
        /// A type used as a tag that indicates which type of modification applies.
        /// </summary>
        public override INamedTypeSymbol Modifier
        {
            get
            {
                return modifier;
            }
        }

        public abstract override int GetHashCode();

        public abstract override bool Equals(object obj);

        internal static CustomModifier CreateOptional(NamedTypeSymbol modifier)
        {
            return new OptionalCustomModifier(modifier);
        }

        internal static CustomModifier CreateRequired(NamedTypeSymbol modifier)
        {
            return new RequiredCustomModifier(modifier);
        }

        internal static ImmutableArray<CustomModifier> Convert(ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers)
        {
            if (customModifiers.IsDefault)
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
            return customModifiers.SelectAsArray(Convert);
        }

        private static CustomModifier Convert(ModifierInfo<TypeSymbol> customModifier)
        {
            var modifier = (NamedTypeSymbol)customModifier.Modifier;
            return customModifier.IsOptional ? CreateOptional(modifier) : CreateRequired(modifier);
        }

        private class OptionalCustomModifier : CSharpCustomModifier
        {
            public OptionalCustomModifier(NamedTypeSymbol modifier)
                : base(modifier)
            { }

            public override bool IsOptional
            {
                get
                {
                    return true;
                }
            }

            public override int GetHashCode()
            {
                return modifier.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                OptionalCustomModifier other = obj as OptionalCustomModifier;

                return other != null && other.modifier.Equals(this.modifier);
            }
        }

        private class RequiredCustomModifier : CSharpCustomModifier
        {
            public RequiredCustomModifier(NamedTypeSymbol modifier)
                : base(modifier)
            { }

            public override bool IsOptional
            {
                get
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return modifier.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                RequiredCustomModifier other = obj as RequiredCustomModifier;

                return other != null && other.modifier.Equals(this.modifier);
            }
        }
    }
}
