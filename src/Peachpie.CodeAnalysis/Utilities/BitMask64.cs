using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.CodeAnalysis.Utilities
{
    /// <summary>
    /// A general structure to record bit information about indexed entities in flow analysis.
    /// </summary>
    /// <remarks>
    /// The minimum and initial lattice element is 0, the maximum is ~0. Indices outside the scope of ulong (64)
    /// are regarded as present in the set.
    /// </remarks>
    internal struct BitMask64
    {
        /// <summary>
        /// Size of ulong bit array (<c>64</c>).
        /// </summary>
        public const int BitsCount = sizeof(ulong) * 8;

        public ulong Mask { get; private set; }

        public static BitMask64 FromMask(ulong mask) => new BitMask64() { Mask = mask };

        public static BitMask64 FromSingleValue(int index)
        {
            var result = new BitMask64();
            result.Set(index);
            return result;
        }

        public void Set(int index)
        {
            if (index >= 0 && index < BitsCount)
            {
                Mask |= 1ul << index;
            }
        }

        public void SetAll()
        {
            Mask = ~0ul;
        }

        public bool Get(int index)
        {
            if (index >= 0 && index < BitsCount)
            {
                return (Mask & (1ul << index)) != 0;
            }
            else
            {
                return true;
            }
        }

        public static bool operator ==(BitMask64 a, BitMask64 b) => a.Mask == b.Mask;

        public static bool operator !=(BitMask64 a, BitMask64 b) => a.Mask != b.Mask;

        public static BitMask64 operator |(BitMask64 a, BitMask64 b) => FromMask(a.Mask | b.Mask);

        public static BitMask64 operator &(BitMask64 a, BitMask64 b) => FromMask(a.Mask & b.Mask);

        public static implicit operator ulong(BitMask64 type) => type.Mask;

        public static implicit operator BitMask64(ulong mask) => FromMask(mask);

        public override bool Equals(object obj)
        {
            return obj is BitMask64 mask && Mask == mask.Mask;
        }

        public override int GetHashCode()
        {
            return 1051679217 + Mask.GetHashCode();
        }
    }
}
