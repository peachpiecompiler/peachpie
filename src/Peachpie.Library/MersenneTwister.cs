using System;

/* 
  
  Mersanne Twister random generator C# implementation. Adapted by Tomas Matousek from original 
  C version by Takuji Nishimura and Makoto Matsumoto (http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html).
  
  Experimental version.
  
*/

/*  
  License:

  Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura,
  All rights reserved.                          

  Redistribution and use in source and binary forms, with or without
  modification, are permitted provided that the following conditions
  are met:

    1. Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.

    2. Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

    3. The names of its contributors may not be used to endorse or promote 
      products derived from this software without specific prior written 
      permission.

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
  A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/


namespace Pchp.Library
{
    /// <summary>
    /// Mersanne Twister random generator.
    /// </summary>
    internal sealed class MersenneTwister : Random
    {
        // period parameters:
        private const int N = 624;
        private const int M = 397;

        // mag01[x] = x * MATRIX_A  for x=0,1
        private static readonly uint[] mag01 = { 0, 0x9908b0dfU };

        // mti==N+1 means mt[N] is not initialized:
        private int mti = N + 1;

        // the array for the state vector:
        private readonly uint[] mt = new uint[N];

        ///// <summary>
        ///// Create a new instance of <see cref="MersenneTwister"/> using a default seed.
        ///// </summary>
        //public MersenneTwister()
        //    : this(5489U)
        //{
        //}

        /// <summary>
        /// Create a new instance of <see cref="MersenneTwister"/> using a specified seed.
        /// </summary>
        /// <param name="seed">The seed.</param>
        public MersenneTwister(uint seed)
            : base(0/*avoid calling base GenerateSeed() method*/)
        {
            Seed(seed);
        }

        /// <summary>
        /// Seeds the generator.
        /// </summary>
        /// <param name="seed">The seed.</param>
        public void Seed(uint seed)
        {
            mt[0] = seed;
            for (int i = 1; i < N; i++)
            {
                // See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier.
                // In the previous versions, MSBs of the seed affect
                // only MSBs of the array mt[].
                // 2002/01/09 modified by Makoto Matsumoto
                mt[i] = unchecked((uint)((1812433253UL * (mt[i - 1] ^ (mt[i - 1] >> 30)) + (ulong)i)));
            }
            mti = N;
        }

        /// <summary>
        /// Generates a random unsigned integer.
        /// </summary>
        /// <returns>The generated number.</returns>
        public uint NextUnsigned()
        {
            // most significant w-r bits:
            const uint upper_mask = 0x80000000U;

            // least significant r bits:
            const uint lower_mask = 0x7fffffffU;

            uint result;

            unchecked
            {
                // generate N words at one time:
                if (mti >= N)
                {
                    int k;

                    for (k = 0; k < N - M; k++)
                    {
                        result = (mt[k] & upper_mask) | (mt[k + 1] & lower_mask);
                        mt[k] = mt[k + M] ^ (result >> 1) ^ mag01[result & 1];
                    }

                    for (; k < N - 1; k++)
                    {
                        result = (mt[k] & upper_mask) | (mt[k + 1] & lower_mask);
                        mt[k] = mt[k + (M - N)] ^ (result >> 1) ^ mag01[result & 1];
                    }

                    result = (mt[N - 1] & upper_mask) | (mt[0] & lower_mask);
                    mt[N - 1] = mt[M - 1] ^ (result >> 1) ^ mag01[result & 1];

                    mti = 0;
                }

                result = mt[mti++];

                // tempering:
                result ^= (result >> 11);
                result ^= (result << 7) & 0x9d2c5680U;
                result ^= (result << 15) & 0xefc60000U;
                result ^= (result >> 18);
            }

            return result;
        }

        /// <summary>
        /// Generates a random signed integer value.
        /// </summary>
        /// <returns>The generated number.</returns>
        public override int Next()
        {
            return unchecked((int)(NextUnsigned() >> 1));
        }

        /// <summary>
        /// Generates a random number from interval [min,max).
        /// </summary>
        /// <returns>The generated number.</returns>
        public override int Next(int min, int max)
        {
            if (min > max)
                throw new ArgumentOutOfRangeException(nameof(min));

            unchecked
            {
                int range = max - min;

                if (range < 0)
                {
                    long long_range = (long)max - min;
                    return (int)((long)(NextDouble() * long_range) + min);
                }
                else
                {
                    return ((int)(NextDouble() * range)) + min;
                }
            }
        }

        /// <summary>
        /// Generates a random double value from interval [0,1).
        /// </summary>
        /// <returns>The generated number.</returns>
        public override double NextDouble()
        {
            return Sample();
        }

        public override int Next(int maxValue) => Next(0, maxValue);

        public override void NextBytes(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(NextUnsigned() & 0xff);
            }
        }

        protected override double Sample()
        {
            return (double)NextUnsigned() * (1.0 / (double)UInt32.MaxValue);
        }
    }
}