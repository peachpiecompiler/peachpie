using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;
using System.Security.Cryptography;
using System.IO;

namespace Pchp.Library
{
    /// <summary>
    /// PHP openssl support.
    /// </summary>
    [PhpExtension("openssl")]
    public static class OpenSSL
    {
        #region Variables
        private const int KeyLengthDES = 64;
        private const int KeyLengthTripleDES = 192;
        private const int MaxKeyLengthRC2 = 128;
        private const int IVLengthAES = 16;
        private const int IVLengthDES = 8;

        private static Cipher[] Ciphers = { Cipher.ParseAES("aes-256-cbc", IVLengthAES), Cipher.ParseAES("aes-192-cbc", IVLengthAES), Cipher.ParseAES("aes-128-cbc", IVLengthAES),
                                            Cipher.ParseAES("aes-256-ecb", 0), Cipher.ParseAES("aes-192-ecb", 0), Cipher.ParseAES("aes-128-ecb", 0),
                                            // CFB mode is not supported in .NET Core yet https://github.com/dotnet/runtime/issues/15771
                                            Cipher.ParseDES("des-ecb", 0), Cipher.ParseDES("des-cbc", IVLengthDES),
                                            Cipher.ParseTripleDES("des-ede3", 0), Cipher.ParseTripleDES("des-ede3-cbc", IVLengthDES),
                                            //RC2 is ok when there is right length of password, but when it is longer, PHP transforms password in some way, but i can not figure out how. 
                                            Cipher.ParseRC2("rc2-40-cbc",IVLengthDES), Cipher.ParseRC2("rc2-64-cbc",IVLengthDES), Cipher.ParseRC2("rc2-ecb",0), Cipher.ParseRC2("rc2-cbc",IVLengthDES)
                                          };

        /// <summary>
        /// Information about supported cipher.
        /// </summary>
        private struct Cipher
        {
            public readonly string Name;
            public readonly CipherTypes Type;
            public readonly int IVLength;
            public readonly CipherMode Mode;
            public readonly int KeyLength; // In Bits

            private Cipher(string name, CipherTypes type, int iVLength, CipherMode mode, int keyLength)
            {
                // Initialization
                Name = name;
                Type = type;
                IVLength = iVLength;
                Mode = mode;
                KeyLength = keyLength;
            }

            /// <summary>
            /// Parse AES algorithm
            /// </summary>
            /// <param name="name">Format aes/AES-[KeyLength]-[CipherMode]</param>
            public static Cipher ParseAES(string name, int iVLength)
            {
                var tokens = name.Split('-');
                if (tokens.Length != 3)
                    throw new ArgumentException("Wrong format of name", nameof(name));

                return new Cipher(name, CipherTypes.AES, iVLength, ParseMode(tokens[2]), int.Parse(tokens[1]));
            }

            /// <summary>
            /// Parse DES algorithm
            /// </summary>
            /// <param name="name">Format des-[CipherMode]</param>
            public static Cipher ParseDES(string name, int iVLength)
            {
                var tokens = name.Split('-');
                if (tokens.Length != 2)
                    throw new ArgumentException("Wrong format of name", nameof(name));

                return new Cipher(name, CipherTypes.DES, iVLength, ParseMode(tokens[1]), KeyLengthDES);
            }

            /// <summary>
            /// Parse TripleDES algorithm
            /// </summary>
            /// <param name="name">Format des-ede3(-[CipherMode])</param>
            public static Cipher ParseTripleDES(string name, int iVLength)
            {
                var tokens = name.Split('-');
                CipherMode mode;

                if (tokens.Length == 2)
                    mode = CipherMode.ECB;
                else if (tokens.Length == 3)
                    mode = ParseMode(tokens[2]);
                else
                    throw new ArgumentException("Wrong format of name", nameof(name));

                return new Cipher(name, CipherTypes.TripleDES, iVLength, mode, KeyLengthTripleDES);
            }

            /// <summary>
            /// Parse RC2 algorithm
            /// </summary>
            /// <param name="name">Format rc2(-[Keylength])-[CipherMode]</param>
            public static Cipher ParseRC2(string name, int iVLength)
            {
                var tokens = name.Split('-');
                CipherMode mode;
                int KeyLength = MaxKeyLengthRC2;

                if (tokens.Length == 2)
                {
                    mode = ParseMode(tokens[1]);
                }
                else if (tokens.Length == 3)
                {
                    KeyLength = int.Parse(tokens[1]);
                    mode = ParseMode(tokens[2]);
                }
                else
                    throw new ArgumentException("Wrong format of name", nameof(name));

                return new Cipher(name, CipherTypes.RC2, iVLength, mode, KeyLength);
            }

            private static CipherMode ParseMode(string mode)
            {
                switch (mode)
                {
                    case "cbc":
                        return CipherMode.CBC;

                    case "ecb":
                        return CipherMode.ECB;

                    default:
                        return CipherMode.CBC;
                }
            }
        }

        [Flags]
        public enum Option { OPENSSL_RAW_DATA = 1, OPENSSL_ZERO_PADDING = 2};

        private enum CipherTypes { AES, DES, TripleDES, RC2, UNKNOWN};

        #endregion

        #region openssl_encrypt/decrypt

        private static SymmetricAlgorithm PrepareCipher(byte[] data, PhpString key, Cipher cipher, PhpString iv, Option options)
        {
            byte[] decodedKey = key.ToBytes(Encoding.Default);

            // Pad key out to KeyLength in bytes if its too short or trancuate if it is too long
            if (decodedKey.Length < cipher.KeyLength / 8 || decodedKey.Length > cipher.KeyLength / 8)
            {
                var resizedKey = new byte[cipher.KeyLength / 8];
                Buffer.BlockCopy(decodedKey, 0, resizedKey, 0, Math.Min(key.Length,resizedKey.Length));
                decodedKey = resizedKey;
            }

            // In .NET there must be non zero length IV, but in PHP cipher mode ECB has zero length. Due to compatibility in openssl_cipher_iv_length
            // There is this wierd thing.
            int lengthForECB = 0;
            switch (cipher.Type)
            {
                case CipherTypes.AES:
                    lengthForECB = IVLengthAES;
                    break;
                case CipherTypes.DES:
                    lengthForECB = IVLengthDES;
                    break;
                case CipherTypes.TripleDES:
                    lengthForECB = IVLengthDES;
                    break;
                case CipherTypes.RC2:
                    lengthForECB = IVLengthDES;
                    break;
            }
            byte[] iVector = new byte[cipher.Mode == CipherMode.ECB ? lengthForECB : cipher.IVLength];
            if (!iv.IsEmpty)
            {
                byte[] decodedIV = iv.ToBytes(Encoding.Default);

                if (decodedIV.Length < cipher.IVLength) // Pad zeros
                    PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.short_iv, decodedIV.Length.ToString(), cipher.IVLength.ToString());
                else if (decodedIV.Length > cipher.IVLength) // Trancuate
                    PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.long_iv, decodedIV.Length.ToString(), cipher.IVLength.ToString());

                Buffer.BlockCopy(decodedIV, 0, iVector, 0, Math.Min(cipher.IVLength,decodedIV.Length));
            }

            SymmetricAlgorithm alg = null;
            switch (cipher.Type)
            {
                case CipherTypes.AES:
                    alg = new RijndaelManaged { Padding = PaddingMode.PKCS7, KeySize = cipher.KeyLength };
                    break;
                case CipherTypes.DES:
                    alg = DES.Create();
                    break;
                case CipherTypes.TripleDES:
                    alg = TripleDES.Create();
                    break;
                case CipherTypes.RC2:
                    alg = RC2.Create();
                    alg.KeySize = cipher.KeyLength;
                    break;
                case CipherTypes.UNKNOWN:
                    throw new NotImplementedException();
            }

            alg.Mode = cipher.Mode;
            alg.Key = decodedKey;
            alg.IV = iVector;

            if ((options & Option.OPENSSL_ZERO_PADDING) == Option.OPENSSL_ZERO_PADDING)
                alg.Padding = PaddingMode.None;

            return alg;
        }

        /// <summary>
        /// Takes a raw or base64 encoded string and decrypts it using a given method and key.
        /// </summary>
        /// <param name="data">The encrypted message to be decrypted.</param>
        /// <param name="method">The cipher method. For a list of available cipher methods, use openssl_get_cipher_methods().</param>
        /// <param name="key">The key.</param>
        /// <param name="options">options can be one of OPENSSL_RAW_DATA, OPENSSL_ZERO_PADDING.</param>
        /// <param name="iv">A non-NULL Initialization Vector.</param>
        /// <param name="tag">The authentication tag in AEAD cipher mode. If it is incorrect, the authentication fails and the function returns FALSE.</param>
        /// <param name="aad">Additional authentication data.</param>
        /// <returns>The decrypted string on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public static string openssl_decrypt(string data, string method, PhpString key, Option options, PhpString iv, string tag = "", string aad = "")
        {
            // Parameters tag and add are for gcm and ccm cipher mode. (I found implementation in version .Net Core 3.0 and 3.1)

            Cipher cipherMethod = Array.Find(Ciphers, c => c.Name == method);
            if (String.IsNullOrEmpty(cipherMethod.Name)) // Unknown cipher algorithm.
            {
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.unknown_cipher);
                return null;
            }

            if (iv.IsEmpty)
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.empty_iv_vector);

            try
            {
                 return Decrypt(data, key, cipherMethod, iv, options);
            }
            catch (CryptographicException)
            {
                return "";
            }
        }

        private static string Decrypt(string data, PhpString key, Cipher cipher, PhpString iv, Option options)
        {
            byte[] encryptedBytes;
            if ((options & Option.OPENSSL_RAW_DATA) == Option.OPENSSL_RAW_DATA)
            {
                encryptedBytes = new byte[data.Length];
                for (int i = 0; i < data.Length; i++)
                    encryptedBytes[i] = (byte)data[i];
            }
            else
                encryptedBytes = System.Convert.FromBase64String(data);

            var aesAlg = PrepareCipher(encryptedBytes, key, cipher, iv, options);
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            string plaintext;
            using (MemoryStream msDecrypt = new MemoryStream(encryptedBytes))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        plaintext = srDecrypt.ReadToEnd();
                        srDecrypt.Close();
                    }
                }
            }

            return plaintext;
        }

        /// <summary>
        /// Encrypts given data with given method and key, returns a raw or base64 encoded string
        /// </summary>
        /// <param name="data">The plaintext message data to be encrypted.</param>
        /// <param name="method">The cipher method. For a list of available cipher methods, use openssl_get_cipher_methods().</param>
        /// <param name="key">The key.</param>
        /// <param name="options">options is a bitwise disjunction of the flags OPENSSL_RAW_DATA and OPENSSL_ZERO_PADDING.</param>
        /// <param name="iv">A non-NULL Initialization Vector.</param>
        /// <param name="tag">The authentication tag passed by reference when using AEAD cipher mode (GCM or CCM).</param>
        /// <param name="aad">Additional authentication data.</param>
        /// <param name="tag_length">The length of the authentication tag. Its value can be between 4 and 16 for GCM mode.</param>
        /// <returns>Returns the encrypted string on success or FALSE on failure.</returns>
        public static string openssl_encrypt(string data, string method, PhpString key, Option options, PhpString iv, string tag = "", string aad = "", int tag_length = 16)
        {
            // Parameters tag and add are for gcm and ccm cipher mode. (I found implementation in version .Net Core 3.0 and 3.1)

            Cipher cipherMethod = Array.Find(Ciphers, c => c.Name == method);
            if (String.IsNullOrEmpty(cipherMethod.Name)) // Unknown cipher algorithm.
            {
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.unknown_cipher);
                return null;
            }

            if (iv.IsEmpty)
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.empty_iv_vector);

            try
            {
                return Encrypt(data, key, cipherMethod, iv, options);
            }
            catch (CryptographicException)
            {
                return "";
            }  
        }

        private static string Encrypt(string data, PhpString key, Cipher cipher, PhpString iv, Option options)
        {
            byte[] encrypted = null;
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            var aesAlg = PrepareCipher(dataBytes, key, cipher, iv, options);
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                            swEncrypt.Write(data);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }

            if ((options & Option.OPENSSL_RAW_DATA) == Option.OPENSSL_RAW_DATA)
                return Encoding.UTF8.GetString(encrypted);
            else
                return System.Convert.ToBase64String(encrypted);
        }

        #endregion

        /// <summary>
        /// Gets the cipher initialization vector (iv) length.
        /// </summary>
        /// <param name="method">The cipher method, see openssl_get_cipher_methods() for a list of potential values.</param>
        /// <returns>Returns the cipher length on success, or FALSE on failure.</returns>
        [return: CastToFalse]
        public static int openssl_cipher_iv_length(string method) 
        {
            Cipher result = Array.Find(Ciphers, cipher => cipher.Name == method);

            if (String.IsNullOrEmpty(result.Name)) // Unknown cipher algorithm.
            {
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.unknown_cipher);
                return -1;
            }
            else
                return result.IVLength;
        }
        
    }
}
