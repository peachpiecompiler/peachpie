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
        
        private static Cipher[] Ciphers = { new Cipher("aes-256-cbc", 16) };

        /// <summary>
        /// Information about supported cipher.
        /// </summary>
        private struct Cipher
        {
            public string Name;
            public CipherTypes Type;
            public int IVLength;
            public CipherMode Mode;
            public int KeyLength; // In Bits

            /// <summary>
            /// Use when name is in format [Cipher]-[KeyLength]-[CipherMode].
            /// </summary>
            public Cipher(string name, int iVLength)
            {
                // Initialization
                Name = name;
                IVLength = iVLength;
                Type = CipherTypes.UNKNOWN;
                Mode = CipherMode.CBC;
                KeyLength = 0;

                // Parse data from name
                ParseName(name);
            }

            private void ParseName(string name)
            {
                var tokens = name.Split('-');

                if (tokens.Length > 0)
                    Type = ParseCipherType(tokens[0]);

                if (tokens.Length > 1)
                    KeyLength = int.Parse(tokens[1]);

                if (tokens.Length > 2)
                    Mode = ParseMode(tokens[2]);
            }

            private CipherTypes ParseCipherType(string type)
            {
                switch (type)
                {
                    case "aes":
                        return CipherTypes.AES;

                    case "des":
                        return CipherTypes.DES;

                    default:
                        return CipherTypes.UNKNOWN;
                }
            }
        
            private CipherMode ParseMode(string mode)
            {
                switch (mode)
                {
                    case "cbc":
                        return CipherMode.CBC;

                    case "ecb":
                        return CipherMode.ECB;

                    case "cfb":
                        return CipherMode.CFB;

                    default:
                        return CipherMode.CBC;
                }
            }
        }

        private enum CipherTypes { AES, DES, UNKNOWN};

        #endregion

        #region openssl_encrypt/decrypt

        private static RijndaelManaged PrepareCipher(string data, string key, Cipher cipher, string iv)
        {
            byte[] decodedKey = Encoding.UTF8.GetBytes(key);

            // Pad key out to 32 bytes (256bits) if its too short
            if (decodedKey.Length < cipher.KeyLength / 8)
            {
                var paddedKey = new byte[cipher.KeyLength / 8];
                Buffer.BlockCopy(decodedKey, 0, paddedKey, 0, key.Length);
                decodedKey = paddedKey;
            }


            byte[] iVector = new byte[cipher.IVLength];
            if (!String.IsNullOrEmpty(iv))
            {
                byte[] decodedIV = System.Convert.FromBase64String(iv);

                if (decodedIV.Length < cipher.IVLength) // Pad zeros
                    PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.short_iv, decodedIV.Length.ToString(), cipher.IVLength.ToString());
                else if (decodedIV.Length > cipher.IVLength) // Trancuate
                    PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.long_iv, decodedIV.Length.ToString(), cipher.IVLength.ToString());

                Buffer.BlockCopy(decodedIV, 0, iVector, 0, cipher.IVLength);
            }

            return new RijndaelManaged { Mode = cipher.Mode, Padding = PaddingMode.PKCS7, KeySize = cipher.KeyLength, BlockSize = 128, Key = decodedKey, IV = iVector }; //FeedbackSize = 8
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
        public static string openssl_decrypt(string data, string method, string key, int options = 0, string iv = "", string tag = "", string aad = "")
        {
            // Parameters tag and add are for gcm and ccm cipher mode. (I found implementation in version .Net Core 3.0 and 3.1)

            Cipher cipherMethod = Array.Find(Ciphers, c => c.Name == method);
            if (String.IsNullOrEmpty(cipherMethod.Name)) // Unknown cipher algorithm.
            {
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.unknown_cipher);
                return null;
            }

            if (String.IsNullOrEmpty(iv))
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.empty_iv_vector);

            switch (cipherMethod.Type)
            {
                case CipherTypes.AES:
                    return DecryptWithAES(data, key, cipherMethod, iv);

                case CipherTypes.DES:
                    throw new NotImplementedException();
                
                default:
                    throw new NotImplementedException();
            }
        }

        private static string DecryptWithAES(string data, string key, Cipher cipher, string iv)
        {
            byte[] encryptedBytes = System.Convert.FromBase64String(data);

            RijndaelManaged aesAlg = PrepareCipher(data, key, cipher, iv);
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
        public static string openssl_encrypt(string data, string method, string key, int options = 0, string iv = "", string tag = "", string aad = "", int tag_length = 16)
        {
            // Parameters tag and add are for gcm and ccm cipher mode. (I found implementation in version .Net Core 3.0 and 3.1)

            Cipher cipherMethod = Array.Find(Ciphers, c => c.Name == method);
            if (String.IsNullOrEmpty(cipherMethod.Name)) // Unknown cipher algorithm.
            {
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.unknown_cipher);
                return null;
            }

            if (String.IsNullOrEmpty(iv))
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.empty_iv_vector);

            switch (cipherMethod.Type)
            {
                case CipherTypes.AES:
                    return EncryptWithAES(data, key, cipherMethod, iv);

                case CipherTypes.DES:
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
        }

        private static string EncryptWithAES(string data, string key, Cipher cipher, string iv)
        {
            byte[] encrypted = null;

            RijndaelManaged aesAlg = PrepareCipher(data, key, cipher, iv);
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
