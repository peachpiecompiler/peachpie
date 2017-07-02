using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pchp.CodeAnalysis.Symbols
{
    /// <summary>
    /// Mask of single bits representing a true-false state.
    /// </summary>
    internal struct PhpSignatureMask
    {
        ulong _mask;

        static ulong BitAt(int index) => (1ul << index);

        /// <summary>
        /// Implicitly converts the mask to <see cref="ulong"/> mask.
        /// </summary>
        public static implicit operator ulong(PhpSignatureMask mask) => mask._mask;

        /// <summary>
        /// Combines two masks.
        /// </summary>
        public static PhpSignatureMask operator |(PhpSignatureMask a, PhpSignatureMask b) => new PhpSignatureMask() { _mask = a._mask | b._mask };

        public bool this[int index]
        {
            get
            {
                return (_mask & BitAt(index)) != 0;
            }
            set
            {
                if (value)
                {
                    _mask |= BitAt(index);
                }
                else
                {
                    _mask &= ~BitAt(index);
                }
            }
        }

        /// <summary>
        /// Sets all bits from given position to <c>1</c>.
        /// </summary>
        public void SetFrom(int start)
        {
            _mask |= ~(BitAt(start) - 1);
        }
    }
}
