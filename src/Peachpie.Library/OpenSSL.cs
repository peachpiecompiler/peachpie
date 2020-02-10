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
        #region Constants
        public const int OPENSSL_RAW_DATA = (int)Option.OPENSSL_RAW_DATA;
        public const int OPENSSL_ZERO_PADDING = (int)Option.OPENSSL_ZERO_PADDING;

        private static Dictionary<string, Cipher> Ciphers = new Dictionary<string, Cipher> { 
        {"aes-256-cbc", new Cipher(CipherTypes.AES, Cipher.IVLengthAES, CipherMode.CBC,256)}, {"aes-192-cbc", new Cipher(CipherTypes.AES, Cipher.IVLengthAES, CipherMode.CBC, 192)},
        {"aes-128-cbc", new Cipher(CipherTypes.AES, Cipher.IVLengthAES, CipherMode.CBC,128)}, {"aes-256-ecb", new Cipher(CipherTypes.AES, Cipher.IVLengthAES, CipherMode.ECB, 256)},
        {"aes-192-ecb", new Cipher(CipherTypes.AES, Cipher.IVLengthAES, CipherMode.ECB,192)}, {"aes-128-ecb", new Cipher(CipherTypes.AES, Cipher.IVLengthAES, CipherMode.ECB, 128)},
        {"des-ecb", new Cipher(CipherTypes.DES, Cipher.IVLengthDES, CipherMode.ECB, Cipher.KeyLengthDES)},
        {"des-cbc", new Cipher(CipherTypes.DES, Cipher.IVLengthDES,CipherMode.CBC, Cipher.KeyLengthDES)},
        {"des-ede3", new Cipher(CipherTypes.TripleDES, Cipher.IVLengthDES, CipherMode.ECB, Cipher.KeyLengthTripleDES)},
        {"des-ede3-cbc", new Cipher(CipherTypes.TripleDES, Cipher.IVLengthDES, CipherMode.CBC, Cipher.KeyLengthTripleDES)}
        // CFB mode is not supported in .NET Core yet https://github.com/dotnet/runtime/issues/15771
        // RC2 is ok when there is right length of password, but when it is longer, PHP transforms password in some way, but i can not figure out how.
        // Parameters tag and add are for gcm and ccm cipher mode. (I found implementation in version .Net Core 3.0 and 3.1)
        };

        /// <summary>
        /// Information about supported cipher.
        /// </summary>
        private struct Cipher
        {
            public const int KeyLengthDES = 64;
            public const int KeyLengthTripleDES = 192;
            public const int IVLengthAES = 16;
            public const int IVLengthDES = 8;

            public readonly CipherTypes Type;
            public readonly int IVLength;
            public readonly CipherMode Mode;
            public readonly int KeyLength; // In Bits

            public Cipher(CipherTypes type, int iVLength, CipherMode mode, int keyLength)
            {
                // Initialization
                Type = type;
                IVLength = iVLength;
                Mode = mode;
                KeyLength = keyLength;
            }
        }

        [Flags]
        public enum Option { OPENSSL_RAW_DATA = 1, OPENSSL_ZERO_PADDING = 2 };

        private enum CipherTypes { AES, DES, TripleDES};

        #endregion

        #region openssl_encrypt/decrypt

        private static SymmetricAlgorithm PrepareCipher(Context ctx, byte[] data, PhpString key, Cipher cipher, PhpString iv, Option options)
        {
            byte[] decodedKey = key.ToBytes(ctx);

            // Pad key out to KeyLength in bytes if its too short or trancuate if it is too long
            if (decodedKey.Length < cipher.KeyLength / 8 || decodedKey.Length > cipher.KeyLength / 8)
            {
                var resizedKey = new byte[cipher.KeyLength / 8];
                Buffer.BlockCopy(decodedKey, 0, resizedKey, 0, Math.Min(key.Length, resizedKey.Length));
                decodedKey = resizedKey;
            }


            byte[] iVector = new byte[cipher.IVLength];
            if (!iv.IsEmpty)
            {
                byte[] decodedIV = iv.ToBytes(ctx);

                if (decodedIV.Length < cipher.IVLength) // Pad zeros
                    PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.short_iv, decodedIV.Length.ToString(), cipher.IVLength.ToString());
                else if (decodedIV.Length > cipher.IVLength) // Trancuate
                    PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.long_iv, decodedIV.Length.ToString(), cipher.IVLength.ToString());

                Buffer.BlockCopy(decodedIV, 0, iVector, 0, Math.Min(cipher.IVLength, decodedIV.Length));
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
        /// <param name="ctx">Context of the script.</param>
        /// <param name="data">The encrypted message to be decrypted.</param>
        /// <param name="method">The cipher method. For a list of available cipher methods, use openssl_get_cipher_methods().</param>
        /// <param name="key">The key.</param>
        /// <param name="options">options can be one of OPENSSL_RAW_DATA, OPENSSL_ZERO_PADDING.</param>
        /// <param name="iv">A non-NULL Initialization Vector.</param>
        /// <param name="tag">The authentication tag in AEAD cipher mode. If it is incorrect, the authentication fails and the function returns FALSE.</param>
        /// <param name="aad">Additional authentication data.</param>
        /// <returns>The decrypted string on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public static string openssl_decrypt(Context ctx, PhpString data, string method, PhpString key, Option options, PhpString iv, string tag = "", string aad = "")
        {
            Cipher cipherMethod;
            if (!Ciphers.TryGetValue(method, out cipherMethod))
            {
                // Unknown cipher algorithm.
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.unknown_cipher);
                return null;
            }

            if (iv.IsEmpty)
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.empty_iv_vector);

            try 
            {
                return Decrypt(ctx, data, key, cipherMethod, iv, options);
            }
            catch (CryptographicException ex)
            {
                PhpException.Throw(PhpError.E_WARNING, ex.Message);
                return ""; 
            }
        }

        private static string Decrypt(Context ctx, PhpString data, PhpString key, Cipher cipher, PhpString iv, Option options)
        {
            byte[] encryptedBytes;
            if ((options & Option.OPENSSL_RAW_DATA) == Option.OPENSSL_RAW_DATA)
                encryptedBytes = data.ToBytes(ctx);
            else
                encryptedBytes = System.Convert.FromBase64String(data.ToString(ctx));

            var aesAlg = PrepareCipher(ctx, encryptedBytes, key, cipher, iv, options);
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msDecrypt = new MemoryStream(encryptedBytes);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }

        /// <summary>
        /// Encrypts given data with given method and key, returns a raw or base64 encoded string
        /// </summary>
        /// <param name="ctx">Context of the script.</param>
        /// <param name="data">The plaintext message data to be encrypted.</param>
        /// <param name="method">The cipher method. For a list of available cipher methods, use openssl_get_cipher_methods().</param>
        /// <param name="key">The key.</param>
        /// <param name="options">options is a bitwise disjunction of the flags OPENSSL_RAW_DATA and OPENSSL_ZERO_PADDING.</param>
        /// <param name="iv">A non-NULL Initialization Vector.</param>
        /// <param name="tag">The authentication tag passed by reference when using AEAD cipher mode (GCM or CCM).</param>
        /// <param name="aad">Additional authentication data.</param>
        /// <param name="tag_length">The length of the authentication tag. Its value can be between 4 and 16 for GCM mode.</param>
        /// <returns>Returns the encrypted string on success or FALSE on failure.</returns>
        public static PhpString openssl_encrypt(Context ctx, string data, string method, PhpString key, Option options, PhpString iv, string tag = "", string aad = "", int tag_length = 16)
        {
            Cipher cipherMethod;
            if (!Ciphers.TryGetValue(method, out cipherMethod))
            {
                // Unknown cipher algorithm.
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.unknown_cipher);
                return null;
            }

            if (iv.IsEmpty)
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.empty_iv_vector);

            try
            {
                return Encrypt(ctx, data, key, cipherMethod, iv, options);
            }
            catch (CryptographicException ex)
            {
                PhpException.Throw(PhpError.E_WARNING, ex.Message);
                return "";
            }
        }

        private static PhpString Encrypt(Context ctx, string data, PhpString key, Cipher cipher, PhpString iv, Option options)
        {
            byte[] encrypted = null;
            byte[] dataBytes = ctx.StringEncoding.GetBytes(data);

            var aesAlg = PrepareCipher(ctx, dataBytes, key, cipher, iv, options);
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msEncrypt = new MemoryStream();
            using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            
            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                swEncrypt.Write(data);

            encrypted = msEncrypt.ToArray();

            if ((options & Option.OPENSSL_RAW_DATA) == Option.OPENSSL_RAW_DATA)
                return new PhpString(encrypted);
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
            Cipher cipherMethod;
            if (!Ciphers.TryGetValue(method, out cipherMethod))
            {
                // Unknown cipher algorithm.
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.unknown_cipher);
                return -1;
            }

            // In .NET there must be non zero length IV, but in PHP cipher mode ECB has zero length. 
            // Due to compatibility in openssl_cipher_iv_length, there is this wierd thing.         
            return (cipherMethod.Mode == CipherMode.ECB) ? 0 : cipherMethod.IVLength;
        }

        /// <summary>
        /// Gets a list of available cipher methods.
        /// </summary>
        /// <param name="aliases">Set to TRUE if cipher aliases should be included within the returned array.</param>
        /// <returns>An array of available cipher methods.</returns>
        public static PhpArray openssl_get_cipher_methods(bool aliases = false)
        {
            PhpArray result = new PhpArray(Ciphers.Keys);

            if (aliases)
                throw new NotImplementedException();

            return result;
        }

        #region openssl_digest/get_md_methods

        private static string[] HashMethods = new string[]{ "md5" };

        /// <summary>
        /// Computes a digest hash value for the given data using a given method, and returns a raw or binhex encoded string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="method">The digest method to use, e.g. "sha256", see openssl_get_md_methods() for a list of available digest methods.</param>
        /// <param name="raw_output">Setting to TRUE will return as raw output data, otherwise the return value is binhex encoded.</param>
        /// <returns>Returns the digested hash value on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public static string openssl_digest(string data , string method, bool raw_output = false)
        {
            string result = Array.Find(HashMethods, hashName => hashName == method.ToLower());

            if (String.IsNullOrEmpty(result)) // Unknown cipher algorithm.
            {
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.unknown_hash);
                return null;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a list of available digest methods.
        /// </summary>
        /// <param name="aliases">Set to TRUE if digest aliases should be included within the returned array.</param>
        /// <returns>An array of available digest methods.</returns>
        public static PhpArray openssl_get_md_methods(bool aliases = false)
        {
                throw new NotImplementedException();
        }
   
        #endregion
    }
}
