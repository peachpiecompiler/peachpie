using System;
using System.Collections.Generic;
using System.Text;

namespace Pchp.Library
{
    public static partial class PhpHash 
    {
        // This code was copied and modified from The PHP Interpreter (https://github.com/php/php-src/blob/master/ext/standard/crypt_freesec.c)
        static class DES
        {
            
            #region Variables    
            private const char passwordEFMT1 = '_';

            private const string ascii64 = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            private static byte[,] m_sbox = new byte[4, 4096];

            private static uint[] bits32 = {
            0x80000000, 0x40000000, 0x20000000, 0x10000000,
            0x08000000, 0x04000000, 0x02000000, 0x01000000,
            0x00800000, 0x00400000, 0x00200000, 0x00100000,
            0x00080000, 0x00040000, 0x00020000, 0x00010000,
            0x00008000, 0x00004000, 0x00002000, 0x00001000,
            0x00000800, 0x00000400, 0x00000200, 0x00000100,
            0x00000080, 0x00000040, 0x00000020, 0x00000010,
            0x00000008, 0x00000004, 0x00000002, 0x00000001
            };

            private static byte[,] sbox = {
            {
                 14,  4, 13,  1,  2, 15, 11,  8,  3, 10,  6, 12,  5,  9,  0,  7,
                  0, 15,  7,  4, 14,  2, 13,  1, 10,  6, 12, 11,  9,  5,  3,  8,
                  4,  1, 14,  8, 13,  6,  2, 11, 15, 12,  9,  7,  3, 10,  5,  0,
                 15, 12,  8,  2,  4,  9,  1,  7,  5, 11,  3, 14, 10,  0,  6, 13
            },
            {
                15,  1,  8, 14,  6, 11,  3,  4,  9,  7,  2, 13, 12,  0,  5, 10,
                 3, 13,  4,  7, 15,  2,  8, 14, 12,  0,  1, 10,  6,  9, 11,  5,
                 0, 14,  7, 11, 10,  4, 13,  1,  5,  8, 12,  6,  9,  3,  2, 15,
                13,  8, 10,  1,  3, 15,  4,  2, 11,  6,  7, 12,  0,  5, 14,  9
            },
            {
                10,  0,  9, 14,  6,  3, 15,  5,  1, 13, 12,  7, 11,  4,  2,  8,
                13,  7,  0,  9,  3,  4,  6, 10,  2,  8,  5, 14, 12, 11, 15,  1,
                13,  6,  4,  9,  8, 15,  3,  0, 11,  1,  2, 12,  5, 10, 14,  7,
                 1, 10, 13,  0,  6,  9,  8,  7,  4, 15, 14,  3, 11,  5,  2, 12
            },
            {
                 7, 13, 14,  3,  0,  6,  9, 10,  1,  2,  8,  5, 11, 12,  4, 15,
                13,  8, 11,  5,  6, 15,  0,  3,  4,  7,  2, 12,  1, 10, 14,  9,
                10,  6,  9,  0, 12, 11,  7, 13, 15,  1,  3, 14,  5,  2,  8,  4,
                 3, 15,  0,  6, 10,  1, 13,  8,  9,  4,  5, 11, 12,  7,  2, 14
            },
            {
                 2, 12,  4,  1,  7, 10, 11,  6,  8,  5,  3, 15, 13,  0, 14,  9,
                14, 11,  2, 12,  4,  7, 13,  1,  5,  0, 15, 10,  3,  9,  8,  6,
                 4,  2,  1, 11, 10, 13,  7,  8, 15,  9, 12,  5,  6,  3,  0, 14,
                11,  8, 12,  7,  1, 14,  2, 13,  6, 15,  0,  9, 10,  4,  5,  3
            },
            {
                12,  1, 10, 15,  9,  2,  6,  8,  0, 13,  3,  4, 14,  7,  5, 11,
                10, 15,  4,  2,  7, 12,  9,  5,  6,  1, 13, 14,  0, 11,  3,  8,
                 9, 14, 15,  5,  2,  8, 12,  3,  7,  0,  4, 10,  1, 13, 11,  6,
                 4,  3,  2, 12,  9,  5, 15, 10, 11, 14,  1,  7,  6,  0,  8, 13
            },
            {
                 4, 11,  2, 14, 15,  0,  8, 13,  3, 12,  9,  7,  5, 10,  6,  1,
                13,  0, 11,  7,  4,  9,  1, 10, 14,  3,  5, 12,  2, 15,  8,  6,
                 1,  4, 11, 13, 12,  3,  7, 14, 10, 15,  6,  8,  0,  5,  9,  2,
                 6, 11, 13,  8,  1,  4, 10,  7,  9,  5,  0, 15, 14,  2,  3, 12
            },
            {
                13,  2,  8,  4,  6, 15, 11,  1, 10,  9,  3, 14,  5,  0, 12,  7,
                 1, 15, 13,  8, 10,  3,  7,  4, 12,  5,  6, 11,  0, 14,  9,  2,
                 7, 11,  4,  1,  9, 12, 14,  2,  0,  6, 10, 13, 15,  3,  5,  8,
                 2,  1, 14,  7,  4, 10,  8, 13, 15, 12,  9,  0,  3,  5,  6, 11
            }
            };

            private static byte[] IP = {
                58, 50, 42, 34, 26, 18, 10,  2, 60, 52, 44, 36, 28, 20, 12,  4,
                62, 54, 46, 38, 30, 22, 14,  6, 64, 56, 48, 40, 32, 24, 16,  8,
                57, 49, 41, 33, 25, 17,  9,  1, 59, 51, 43, 35, 27, 19, 11,  3,
                61, 53, 45, 37, 29, 21, 13,  5, 63, 55, 47, 39, 31, 23, 15,  7
            };

            private static byte[] key_perm = {
                57, 49, 41, 33, 25, 17,  9,  1, 58, 50, 42, 34, 26, 18,
                10,  2, 59, 51, 43, 35, 27, 19, 11,  3, 60, 52, 44, 36,
                63, 55, 47, 39, 31, 23, 15,  7, 62, 54, 46, 38, 30, 22,
                14,  6, 61, 53, 45, 37, 29, 21, 13,  5, 28, 20, 12,  4
            };

            private static byte[] comp_perm = {
                14, 17, 11, 24,  1,  5,  3, 28, 15,  6, 21, 10,
                23, 19, 12,  4, 26,  8, 16,  7, 27, 20, 13,  2,
                41, 52, 31, 37, 47, 55, 30, 40, 51, 45, 33, 48,
                44, 49, 39, 56, 34, 53, 46, 42, 50, 36, 29, 32
            };

            private static byte[] pbox = {
                16,  7, 20, 21, 29, 12, 28, 17,  1, 15, 23, 26,  5, 18, 31, 10,
                 2,  8, 24, 14, 32, 27,  3,  9, 19, 13, 30,  6, 22, 11,  4, 25
            };

            private static byte[] bits8 = { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };

            private static byte[] key_shifts = {
                  1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1
            };

            private static uint[,] ip_maskl = new uint[8, 256];
            private static uint[,] ip_maskr = new uint[8, 256];

            private static uint[,] fp_maskl = new uint[8, 256];
            private static uint[,] fp_maskr = new uint[8, 256];

            private static uint[,] key_perm_maskr = new uint[8, 128];
            private static uint[,] key_perm_maskl = new uint[8, 128];

            private static uint[,] comp_maskl = new uint[8, 128];
            private static uint[,] comp_maskr = new uint[8, 128];

            private static uint[,] psbox = new uint[4, 256];

            private static bool initialized = false;

            private static readonly object init_lock = new object();

        #endregion

        private static void Init()
            {
                byte bits28Index = 4;
                byte bits24Index = 8;
                byte[,] u_sbox = new byte[8, 64];
                byte[] init_perm = new byte[64];
                byte[] final_perm = new byte[64];
                byte[] inv_key_perm = new byte[64];
                byte[] inv_comp_perm = new byte[56];
                byte[] un_pbox = new byte[32];

                // Invert the S-boxes, reordering the input bits.
                int b = 0;
                for (int i = 0; i < 8; i++)
                    for (int j = 0; j < 64; j++)
                    {
                        b = (j & 0x20) | ((j & 1) << 4) | ((j >> 1) & 0xf);
                        u_sbox[i, j] = sbox[i, b];
                    }

                /* Convert the inverted S-boxes into 4 arrays of 8 bits.
	            * Each will handle 12 bits of the S-box input. */
                for (b = 0; b < 4; b++)
                    for (byte i = 0; i < 64; i++)
                        for (byte j = 0; j < 64; j++)
                            m_sbox[b, (i << 6) | j] = (byte)((u_sbox[(b << 1), i] << 4) | u_sbox[(b << 1) + 1, j]);

                /* Set up the initial & final permutations into a useful form, and
                 * initialise the inverted key permutation. */
                for (byte i = 0; i < 64; i++)
                {
                    init_perm[final_perm[i] = (byte)(IP[i] - 1)] = i;
                    inv_key_perm[i] = 255;
                }

                /* Invert the key permutation and initialise the inverted key
	            * compression permutation. */
                for (byte i = 0; i < 56; i++)
                {
                    inv_key_perm[key_perm[i] - 1] = i;
                    inv_comp_perm[i] = 255;
                }

                // Invert the key compression permutation.
                for (byte i = 0; i < 48; i++)
                    inv_comp_perm[comp_perm[i] - 1] = i;

                /* Set up the OR-mask arrays for the initial and final permutations,
                * and for the key initial and compression permutations. */
                int inbit, obit;
                for (byte k = 0; k < 8; k++)
                {
                    for (int i = 0; i < 256; i++)
                    {
                        ip_maskl[k, i] = 0;
                        ip_maskr[k, i] = 0;
                        fp_maskl[k, i] = 0;
                        fp_maskr[k, i] = 0;
                        for (byte j = 0; j < 8; j++)
                        {
                            inbit = 8 * k + j;
                            if ((i & bits8[j]) != 0)
                            {
                                if ((obit = init_perm[inbit]) < 32)
                                    ip_maskl[k, i] |= bits32[obit];
                                else
                                    ip_maskr[k, i] |= bits32[obit - 32];
                                if ((obit = final_perm[inbit]) < 32)
                                    fp_maskl[k, i] |= bits32[obit];
                                else
                                    fp_maskr[k, i] |= bits32[obit - 32];
                            }
                        }
                    }
                    for (byte i = 0; i < 128; i++)
                    {
                        key_perm_maskl[k, i] = 0;
                        key_perm_maskr[k, i] = 0;
                        for (byte j = 0; j < 7; j++)
                        {
                            inbit = 8 * k + j;
                            if ((i & bits8[j + 1]) != 0)
                            {
                                if ((obit = inv_key_perm[inbit]) == 255)
                                    continue;
                                if (obit < 28)
                                    key_perm_maskl[k, i] |= bits32[bits28Index + obit];
                                else
                                    key_perm_maskr[k, i] |= bits32[bits28Index + obit - 28];
                            }
                        }
                        comp_maskl[k, i] = 0;
                        comp_maskr[k, i] = 0;
                        for (byte j = 0; j < 7; j++)
                        {
                            inbit = 7 * k + j;
                            if ((i & bits8[j + 1]) != 0)
                            {
                                if ((obit = inv_comp_perm[inbit]) == 255)
                                    continue;
                                if (obit < 24)
                                    comp_maskl[k, i] |= bits32[bits24Index + obit];
                                else
                                    comp_maskr[k, i] |= bits32[bits24Index + obit - 24];
                            }
                        }
                    }
                }

                /* Invert the P-box permutation, and convert into OR-masks for
	            * handling the output of the S-box arrays setup above. */
                for (byte i = 0; i < 32; i++)
                    un_pbox[pbox[i] - 1] = i;

                for (b = 0; b < 4; b++)
                    for (int i = 0; i < 256; i++)
                    {
                        psbox[b, i] = 0;
                        for (byte j = 0; j < 8; j++)
                        {
                            if ((i & bits8[j]) != 0)
                                psbox[b, i] |= bits32[un_pbox[8 * b + j]];
                        }
                    }
            }

            private static int SetKey(byte[] password, ref CryptExtendedData data)
            {
                uint rawKey0, rawKey1;

                rawKey0 =
                    (uint)(byte)password[3] |
                    ((uint)(byte)password[2] << 8) |
                    ((uint)(byte)password[1] << 16) |
                    ((uint)(byte)password[0] << 24);
                rawKey1 =
                    (uint)(byte)password[7] |
                    ((uint)(byte)password[6] << 8) |
                    ((uint)(byte)password[5] << 16) |
                    ((uint)(byte)password[4] << 24);

                if ((rawKey0 | rawKey1) != 0 && rawKey0 == data.oldRawKey0 && rawKey1 == data.oldRawKey1)
                {
                    /* Already setup for this key.
                     * This optimisation fails on a zero key (which is weak and
                     * has bad parity anyway) in order to simplify the starting
                     * conditions. */
                    return 0;
                }
                data.oldRawKey0 = rawKey0;
                data.oldRawKey1 = rawKey1;

                // Do key permutation and split into two 28-bit subkeys.
                uint key0 = key_perm_maskl[0, rawKey0 >> 25]
                   | key_perm_maskl[1, (rawKey0 >> 17) & 0x7f]
                   | key_perm_maskl[2, (rawKey0 >> 9) & 0x7f]
                   | key_perm_maskl[3, (rawKey0 >> 1) & 0x7f]
                   | key_perm_maskl[4, rawKey1 >> 25]
                   | key_perm_maskl[5, (rawKey1 >> 17) & 0x7f]
                   | key_perm_maskl[6, (rawKey1 >> 9) & 0x7f]
                   | key_perm_maskl[7, (rawKey1 >> 1) & 0x7f];
                uint key1 = key_perm_maskr[0, rawKey0 >> 25]
                   | key_perm_maskr[1, (rawKey0 >> 17) & 0x7f]
                   | key_perm_maskr[2, (rawKey0 >> 9) & 0x7f]
                   | key_perm_maskr[3, (rawKey0 >> 1) & 0x7f]
                   | key_perm_maskr[4, rawKey1 >> 25]
                   | key_perm_maskr[5, (rawKey1 >> 17) & 0x7f]
                   | key_perm_maskr[6, (rawKey1 >> 9) & 0x7f]
                   | key_perm_maskr[7, (rawKey1 >> 1) & 0x7f];

                // Rotate subkeys and do compression permutation.
                int shifts = 0;
                for (int round = 0; round < 16; round++)
                {
                    shifts += key_shifts[round];

                    uint t0 = (key0 << shifts) | (key0 >> (28 - shifts));
                    uint t1 = (key1 << shifts) | (key1 >> (28 - shifts));

                    data.decryptKeysL[15 - round] =
                    data.encryptKeysL[round] = comp_maskl[0, (t0 >> 21) & 0x7f]
                            | comp_maskl[1, (t0 >> 14) & 0x7f]
                            | comp_maskl[2, (t0 >> 7) & 0x7f]
                            | comp_maskl[3, t0 & 0x7f]
                            | comp_maskl[4, (t1 >> 21) & 0x7f]
                            | comp_maskl[5, (t1 >> 14) & 0x7f]
                            | comp_maskl[6, (t1 >> 7) & 0x7f]
                            | comp_maskl[7, t1 & 0x7f];

                    data.decryptKeysR[15 - round] =
                    data.encryptKeysR[round] = comp_maskr[0, (t0 >> 21) & 0x7f]
                            | comp_maskr[1, (t0 >> 14) & 0x7f]
                            | comp_maskr[2, (t0 >> 7) & 0x7f]
                            | comp_maskr[3, t0 & 0x7f]
                            | comp_maskr[4, (t1 >> 21) & 0x7f]
                            | comp_maskr[5, (t1 >> 14) & 0x7f]
                            | comp_maskr[6, (t1 >> 7) & 0x7f]
                            | comp_maskr[7, t1 & 0x7f];
                }
                return 0;
            }

            private static int ConvertASCIIToBin(char ch)
            {
                sbyte sch = (sbyte)ch;
                int retval;

                retval = sch - '.';
                if (sch >= 'A')
                {
                    retval = sch - ('A' - 12);
                    if (sch >= 'a')
                        retval = sch - ('a' - 38);
                }
                retval &= 0x3f;

                return (retval);
            }

            private static void SetupSalt(uint salt, ref CryptExtendedData data)
            {
                if (salt == data.oldSalt)
                    return;
                data.oldSalt = salt;

                uint saltbits = 0;
                uint saltbit = 1;
                uint obit = 0x800000;

                for (byte i = 0; i < 24; i++)
                {
                    if ((salt & saltbit) != 0)
                        saltbits |= obit;
                    saltbit <<= 1;
                    obit >>= 1;
                }
                data.saltbits = saltbits;
            }

            private static int DoDes(uint l_in, uint r_in, ref uint l_out, ref uint r_out, int count, ref CryptExtendedData data)
            {
                // l_in, r_in, l_out, and r_out are in pseudo - "big-endian" format.
                uint[] kl1, kr1;

                if (count == 0)
                {
                    return 1;
                }
                else if (count > 0)
                {
                    // Encrypting
                    kl1 = data.encryptKeysL;
                    kr1 = data.encryptKeysR;
                }
                else
                {
                    // Decrypting
                    count = -count;
                    kl1 = data.decryptKeysL;
                    kr1 = data.decryptKeysR;
                }

                // Do initial permutation (IP).
                uint l = ip_maskl[0, l_in >> 24]
                    | ip_maskl[1, (l_in >> 16) & 0xff]
                    | ip_maskl[2, (l_in >> 8) & 0xff]
                    | ip_maskl[3, l_in & 0xff]
                    | ip_maskl[4, r_in >> 24]
                    | ip_maskl[5, (r_in >> 16) & 0xff]
                    | ip_maskl[6, (r_in >> 8) & 0xff]
                    | ip_maskl[7, r_in & 0xff];
                uint r = ip_maskr[0, l_in >> 24]
                    | ip_maskr[1, (l_in >> 16) & 0xff]
                    | ip_maskr[2, (l_in >> 8) & 0xff]
                    | ip_maskr[3, l_in & 0xff]
                    | ip_maskr[4, r_in >> 24]
                    | ip_maskr[5, (r_in >> 16) & 0xff]
                    | ip_maskr[6, (r_in >> 8) & 0xff]
                    | ip_maskr[7, r_in & 0xff];

                uint saltbits = data.saltbits;

                while (count-- != 0)
                {
                    uint f = 0;
                    // Do each round.
                    int klIndex = 0;
                    int krIndex = 0;
                    int round = 16;
                    while (round-- != 0)
                    {
                        // Expand R to 48 bits (simulate the E-box).
                        uint r48l = ((r & 0x00000001) << 23)
                            | ((r & 0xf8000000) >> 9)
                            | ((r & 0x1f800000) >> 11)
                            | ((r & 0x01f80000) >> 13)
                            | ((r & 0x001f8000) >> 15);

                        uint r48r = ((r & 0x0001f800) << 7)
                            | ((r & 0x00001f80) << 5)
                            | ((r & 0x000001f8) << 3)
                            | ((r & 0x0000001f) << 1)
                            | ((r & 0x80000000) >> 31);

                        /* Do salting for crypt() and friends, and
                         * XOR with the permuted key. */

                        f = (r48l ^ r48r) & saltbits;
                        r48l ^= f ^ kl1[klIndex++];
                        r48r ^= f ^ kr1[krIndex++];

                        /* Do sbox lookups (which shrink it back to 32 bits)
                         * and do the pbox permutation at the same time. */

                        f = psbox[0, m_sbox[0, r48l >> 12]]
                          | psbox[1, m_sbox[1, r48l & 0xfff]]
                          | psbox[2, m_sbox[2, r48r >> 12]]
                          | psbox[3, m_sbox[3, r48r & 0xfff]];

                        // Now that we've permuted things, complete f().
                        f ^= l;
                        l = r;
                        r = f;
                    }
                    r = l;
                    l = f;
                }
                // Do final permutation (inverse of IP).

                l_out = fp_maskl[0, l >> 24]
                      | fp_maskl[1, (l >> 16) & 0xff]
                      | fp_maskl[2, (l >> 8) & 0xff]
                      | fp_maskl[3, l & 0xff]
                      | fp_maskl[4, r >> 24]
                      | fp_maskl[5, (r >> 16) & 0xff]
                      | fp_maskl[6, (r >> 8) & 0xff]
                      | fp_maskl[7, r & 0xff];
                r_out = fp_maskr[0, l >> 24]
                      | fp_maskr[1, (l >> 16) & 0xff]
                      | fp_maskr[2, (l >> 8) & 0xff]
                      | fp_maskr[3, l & 0xff]
                      | fp_maskr[4, r >> 24]
                      | fp_maskr[5, (r >> 16) & 0xff]
                      | fp_maskr[6, (r >> 8) & 0xff]
                      | fp_maskr[7, r & 0xff];

                return 0;
            }

            private static int Cipher(byte[] inBuffer, byte[] outBuffer, uint salt, int count, ref CryptExtendedData data)
            {
                uint leftOut = 0;
                uint rightOut = 0;

                SetupSalt(salt, ref data);

                uint rawLeft = (uint)(byte)inBuffer[3] |
                    ((uint)(byte)inBuffer[2] << 8) |
                    ((uint)(byte)inBuffer[1] << 16) |
                    ((uint)(byte)inBuffer[0] << 24);

                uint rawRight = (uint)(byte)inBuffer[7] |
                    ((uint)(byte)inBuffer[6] << 8) |
                    ((uint)(byte)inBuffer[5] << 16) |
                    ((uint)(byte)inBuffer[4] << 24);

                int result = DoDes(rawLeft, rawRight, ref leftOut, ref rightOut, count, ref data);

                outBuffer[0] = (byte)(leftOut >> 24);
                outBuffer[1] = (byte)(leftOut >> 16);
                outBuffer[2] = (byte)(leftOut >> 8);
                outBuffer[3] = (byte)leftOut;
                outBuffer[4] = (byte)(rightOut >> 24);
                outBuffer[5] = (byte)(rightOut >> 16);
                outBuffer[6] = (byte)(rightOut >> 8);
                outBuffer[7] = (byte)rightOut;

                return result;
            }

            private static bool IsASCIIUnsafe(char ch)
            {
                return ch == 0 || ch == '\n' || ch == ':';
            }

            public static string Crypt(string password, string setting)
            {

                lock (init_lock)
                { 
                    if (!initialized)
                    {
                        Init();
                        initialized = true;             
                    }
                }
                CryptExtendedData data = new CryptExtendedData();
                data.Init();

                int keyIndex = 0;
                byte[] keyBuffer = new byte[8];
                
                for (int i = 0; i < 8; i++)
                {
                    keyBuffer[i] |= (byte)(password[keyIndex] << 1);
                    if (password[keyIndex] != 0)
                        keyIndex++;
                }

                if (SetKey(keyBuffer, ref data) != 0)
                    return null;

                uint salt = 0;
                uint count = 0;
                int outputIndex = 0;
                if (setting[0] == passwordEFMT1)
                {
                    /* "new"-style:
                    *	setting - underscore, 4 chars of count, 4 chars of salt
                    *	key - unlimited characters */
                    for (int i = 1; i < 5; i++)
                    {
                        int value = ConvertASCIIToBin(setting[i]);
                        if (ascii64[value] != setting[i])
                            return null;
                        count |= (uint)value << (i - 1) * 6;
                    }

                    if (count == 0)
                        return null;

                    for (byte i = 5; i < 9; i++)
                    {
                        int value = ConvertASCIIToBin(setting[i]);
                        if (ascii64[value] != setting[i])
                            return null;
                        salt |= (uint)(value << (i - 5) * 6);
                    }

                    while (password.Length > keyIndex)
                    {

                        // Encrypt the key with itself.
                        if (Cipher(keyBuffer, keyBuffer, 0, 1, ref data) != 0)
                            return null;

                        // And XOR with the next 8 characters of the key.
                        for (int i = 0; i < 8 && password.Length > keyIndex; i++)
                            keyBuffer[i] ^= (byte)(password[keyIndex++] << 1);

                        if (SetKey(keyBuffer, ref data) != 0)
                            return null;
                    }

                    for (int i = 0; i < 9; i++)
                        data.output[i] = (byte)setting[i];

                    data.output[9] = 0;
                    outputIndex = 9;
                }
                else
                {
                    /* "old"-style:
                     *	setting - 2 chars of salt
                     *	key - up to 8 characters */
                    count = 25;

                    if (IsASCIIUnsafe(setting[0]) || IsASCIIUnsafe(setting[1]))
                        return null;

                    salt = (uint)((ConvertASCIIToBin(setting[1]) << 6) | ConvertASCIIToBin(setting[0]));

                    data.output[0] = (byte)setting[0];
                    data.output[1] = (byte)setting[1];
                    outputIndex = 2;
                }

                SetupSalt(salt, ref data);

                // Do it.
                uint l, r0 = 0, r1 = 0;

                if (DoDes(0, 0, ref r0, ref r1, (int)count, ref data) != 0)
                    return null;

                // Now encode the result...

                l = (r0 >> 8);
                data.output[outputIndex++] = (byte)ascii64[(int)(l >> 18) & 0x3f];
                data.output[outputIndex++] = (byte)ascii64[(int)(l >> 12) & 0x3f];
                data.output[outputIndex++] = (byte)ascii64[(int)(l >> 6) & 0x3f];
                data.output[outputIndex++] = (byte)ascii64[(int)l & 0x3f];

                l = (r0 << 16) | ((r1 >> 16) & 0xffff);
                data.output[outputIndex++] = (byte)ascii64[(int)(l >> 18) & 0x3f];
                data.output[outputIndex++] = (byte)ascii64[(int)(l >> 12) & 0x3f];
                data.output[outputIndex++] = (byte)ascii64[(int)(l >> 6) & 0x3f];
                data.output[outputIndex++] = (byte)ascii64[(int)(l & 0x3f)];

                l = r1 << 2;
                data.output[outputIndex++] = (byte)ascii64[(int)(l >> 12) & 0x3f];
                data.output[outputIndex++] = (byte)ascii64[(int)(l >> 6) & 0x3f];
                data.output[outputIndex++] = (byte)ascii64[(int)l & 0x3f];
                data.output[outputIndex] = 0;

                return Encoding.ASCII.GetString(data.output).Trim('\0');
            }
        }

        struct CryptExtendedData
        {
            public bool initialized;
            public uint saltbits;
            public uint oldSalt;
            public uint[] encryptKeysL;
            public uint[] encryptKeysR;
            public uint[] decryptKeysL;
            public uint[] decryptKeysR;
            public uint oldRawKey0, oldRawKey1;
            public byte[] output;

            public  void Init()
            {
                oldRawKey0 = oldRawKey1 = 0;
                saltbits = 0;
                oldSalt = 0;
                encryptKeysL = new uint[16];
                encryptKeysR = new uint[16];
                decryptKeysL = new uint[16];
                decryptKeysR = new uint[16];
                output = new byte[21];
                initialized = true;
            }
        }
    }
}
