/*
 * What is almost implemented, but something missing.
 * - Hash and crypto algorithms. There are supported the most useful algos.
 * - There are new useful methods and options avaible in .NET standart 2.1.
 * - Export method in X509 region is almost finished. Parameter notext adds human-readable information to output. 
 *   There are missing two pieces of info.
 * What is not implemented and can be almost done with base .NET
 * - CSR and Key resource. I think that base functionality can be done with .NET
 * What is not implemented and can not be done with base .NET
 * - openssl_pkey_export .NET standart 2.0 does not support export PEM format of private key (2.1 does)
 * - Loading Key resource from PEM format because of same reason as above.
 * - Advanced functionality in methods, like some special properties. 
 */
using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;
using System.Security.Cryptography;
using System.IO;
using static Pchp.Library.PhpHash;
using System.Security.Cryptography.X509Certificates;
using Pchp.Core.Utilities;

namespace Pchp.Library
{
    /// <summary>
    /// PHP openssl support.
    /// </summary>
    [PhpExtension("openssl")]
    public static class OpenSSL
    {
        #region Constants
        public const int OPENSSL_RAW_DATA = (int)Options.OPENSSL_RAW_DATA;
        public const int OPENSSL_ZERO_PADDING = (int)Options.OPENSSL_ZERO_PADDING;

        private static Dictionary<string, Cipher> Ciphers = new Dictionary<string, Cipher>(StringComparer.OrdinalIgnoreCase)
        {
            {"aes-256-cbc", new Cipher(CipherType.AES, Cipher.IVLengthAES, CipherMode.CBC,256)},
            {"aes-192-cbc", new Cipher(CipherType.AES, Cipher.IVLengthAES, CipherMode.CBC, 192)},
            {"aes-128-cbc", new Cipher(CipherType.AES, Cipher.IVLengthAES, CipherMode.CBC,128)},
            {"aes-256-ecb", new Cipher(CipherType.AES, Cipher.IVLengthAES, CipherMode.ECB, 256)},
            {"aes-192-ecb", new Cipher(CipherType.AES, Cipher.IVLengthAES, CipherMode.ECB,192)},
            {"aes-128-ecb", new Cipher(CipherType.AES, Cipher.IVLengthAES, CipherMode.ECB, 128)},
            {"des-ecb", new Cipher(CipherType.DES, Cipher.IVLengthDES, CipherMode.ECB, Cipher.KeyLengthDES)},
            {"des-cbc", new Cipher(CipherType.DES, Cipher.IVLengthDES,CipherMode.CBC, Cipher.KeyLengthDES)},
            {"des-ede3", new Cipher(CipherType.TripleDES, Cipher.IVLengthDES, CipherMode.ECB, Cipher.KeyLengthTripleDES)},
            {"des-ede3-cbc", new Cipher(CipherType.TripleDES, Cipher.IVLengthDES, CipherMode.CBC, Cipher.KeyLengthTripleDES)}
        // CFB mode is not supported in .NET Core yet https://github.com/dotnet/runtime/issues/15771
        // RC2 is ok when there is right length of password, but when it is longer, PHP transforms password in some way, but i can not figure out how.
        // Parameters tag and add are for gcm and ccm cipher mode. (I found implementation in version .Net Core 3.0 and 3.1 (standart 2.1))
        };

        private static Dictionary<string, string> CiphersAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"aes128", "aes-128-cbc"},
            {"aes192", "aes-192-cbc"},
            {"aes256", "aes-256-cbc"},
            {"des", "des-cbc"},
            {"des-ede3-ecb", "des-ede3"},
            {"des3", "des-ede3-cbc"}
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

            public readonly CipherType Type;
            public readonly int IVLength;
            public readonly CipherMode Mode;
            public readonly int KeyLength; // In Bits

            public Cipher(CipherType type, int iVLength, CipherMode mode, int keyLength)
            {
                // Initialization
                Type = type;
                IVLength = iVLength;
                Mode = mode;
                KeyLength = keyLength;
            }
        }

        [Flags]
        public enum Options { OPENSSL_RAW_DATA = 1, OPENSSL_ZERO_PADDING = 2 };

        private enum CipherType { AES, DES, TripleDES };

        #endregion

        #region openssl_encrypt/decrypt

        private static SymmetricAlgorithm PrepareCipher(Context ctx, byte[] data, PhpString key, Cipher cipher, PhpString iv, Options options)
        {
            byte[] decodedKey = key.ToBytes(ctx);

            // Pad key out to KeyLength in bytes if its too short or trancuate if it is too long
            int KeyLengthInBytes = cipher.KeyLength / 8;
            if (decodedKey.Length < KeyLengthInBytes || decodedKey.Length > KeyLengthInBytes)
            {
                var resizedKey = new byte[KeyLengthInBytes];
                Buffer.BlockCopy(decodedKey, 0, resizedKey, 0, Math.Min(key.Length, resizedKey.Length));
                decodedKey = resizedKey;
            }

            byte[] iVector = new byte[cipher.IVLength];
            if (!iv.IsEmpty)
            {

                byte[] decodedIV = ((options & Options.OPENSSL_RAW_DATA) != Options.OPENSSL_RAW_DATA) ?
                iv.ToBytes(ctx) : System.Convert.FromBase64String(iv.ToString(ctx));

                if (decodedIV.Length < cipher.IVLength) // Pad zeros
                    PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.openssl_long_iv, decodedIV.Length.ToString(), cipher.IVLength.ToString());
                else if (decodedIV.Length > cipher.IVLength) // Trancuate
                    PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.openssl_long_iv, decodedIV.Length.ToString(), cipher.IVLength.ToString());

                Buffer.BlockCopy(decodedIV, 0, iVector, 0, Math.Min(cipher.IVLength, decodedIV.Length));
            }

            SymmetricAlgorithm alg = null;
            switch (cipher.Type)
            {
                case CipherType.AES:
                    alg = new RijndaelManaged { Padding = PaddingMode.PKCS7, KeySize = cipher.KeyLength };
                    break;
                case CipherType.DES:
                    alg = DES.Create();
                    break;
                case CipherType.TripleDES:
                    alg = TripleDES.Create();
                    break;
            }

            alg.Mode = cipher.Mode;
            alg.Key = decodedKey;
            alg.IV = iVector;

            if ((options & Options.OPENSSL_ZERO_PADDING) == Options.OPENSSL_ZERO_PADDING)
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
        public static string openssl_decrypt(Context ctx, PhpString data, string method, PhpString key, Options options, PhpString iv, string tag = "", string aad = "")
        {
            if (CiphersAliases.TryGetValue(method, out var aliasName))
            {
                method = aliasName;
            }

            if (!Ciphers.TryGetValue(method, out var cipherMethod))
            {
                // Unknown cipher algorithm.
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.openssl_unknown_cipher);
                return null;
            }

            if (iv.IsEmpty)
            {
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.openssl_empty_iv);
            }

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

        private static string Decrypt(Context ctx, PhpString data, PhpString key, Cipher cipher, PhpString iv, Options options)
        {
            byte[] encryptedBytes;
            if ((options & Options.OPENSSL_RAW_DATA) == Options.OPENSSL_RAW_DATA)
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
        public static PhpString openssl_encrypt(Context ctx, string data, string method, PhpString key, Options options, PhpString iv, string tag = "", string aad = "", int tag_length = 16)
        {
            if (CiphersAliases.TryGetValue(method, out var aliasName))
            {
                method = aliasName;
            }

            if (!Ciphers.TryGetValue(method, out var cipherMethod))
            {
                // Unknown cipher algorithm.
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.openssl_unknown_cipher);
                return null;
            }

            if (iv.IsEmpty)
            {
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.openssl_empty_iv);
            }

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

        private static PhpString Encrypt(Context ctx, string data, PhpString key, Cipher cipher, PhpString iv, Options options)
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

            if ((options & Options.OPENSSL_RAW_DATA) == Options.OPENSSL_RAW_DATA)
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
            if (CiphersAliases.TryGetValue(method, out var aliasName))
            {
                method = aliasName;
            }

            if (!Ciphers.TryGetValue(method, out var cipherMethod))
            {
                // Unknown cipher algorithm.
                PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.openssl_unknown_cipher);
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
            var result = new PhpArray(Ciphers.Keys);

            if (aliases)
            {
                result.AddRange(CiphersAliases.Keys);
            }

            return result;
        }

        // This type is thread safe.
        private static RNGCryptoServiceProvider randomNumbers = new RNGCryptoServiceProvider();

        /// <summary>
        /// Generate a pseudo-random string of bytes.
        /// </summary>
        /// <param name="length">The length of the desired string of bytes. Must be a positive integer.</param>
        /// <param name="crypto_strong">If passed into the function, this will hold a boolean value that determines if the algorithm used was "cryptographically strong"</param>
        /// <returns>Returns the generated string of bytes on success, or FALSE on failure.</returns>
        public static PhpString openssl_random_pseudo_bytes(int length, ref bool? crypto_strong)
        {
            if (length < 1)
            {
                crypto_strong = null;
                return PhpString.Empty;
            }

            crypto_strong = true;
            byte[] random = new byte[length];
            randomNumbers.GetBytes(random);

            return new PhpString(random);
        }

        #region openssl_digest/get_md_methods

        // There are algos, which are implemented in .NET but there are not implemented in Hash.cs
        private static Dictionary<string, Func<byte[], byte[]>> AditionaHashMethods = new Dictionary<string, Func<byte[], byte[]>>(StringComparer.OrdinalIgnoreCase)
        {
            {"sha384", (byte[] data) => SHA384Managed.Create().ComputeHash(data)}
        };

        private static Dictionary<string, string> HashAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"rsa-md4", "md4"},
            {"rsa-md5", "md5"},
            {"rsa-sha1", "sha1"},
            {"rsa-sha256", "sha256"},
            {"rsa-sha384", "sha384"},
            {"rsa-sha512", "sha512"}
        };

        /// <summary>
        /// Computes a digest hash value for the given data using a given method, and returns a raw or binhex encoded string.
        /// </summary>
        /// <param name="ctx">Context of the script.</param>
        /// <param name="data">The data.</param>
        /// <param name="method">The digest method to use, e.g. "sha256", see openssl_get_md_methods() for a list of available digest methods.</param>
        /// <param name="raw_output">Setting to TRUE will return as raw output data, otherwise the return value is binhex encoded.</param>
        /// <returns>Returns the digested hash value on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public static PhpString openssl_digest(Context ctx, PhpString data, string method, bool raw_output = false)
        {
            if (HashAliases.TryGetValue(method, out var aliasedname))
            {
                method = aliasedname;
            }

            if (HashPhpResource.HashAlgorithms.ContainsKey(method)) // Supported in Hash.cs
            {
                return PhpHash.hash(method, data.ToBytes(ctx), raw_output);
            }
            else
            {
                if (AditionaHashMethods.TryGetValue(method, out var alg))
                {
                    var hashedBytes = alg(data.ToBytes(ctx));
                    return raw_output ? new PhpString(hashedBytes) : StringUtils.BinToHex(hashedBytes, string.Empty);
                }
                else
                {
                    // Unknown cipher algorithm.
                    PhpException.Throw(PhpError.E_WARNING, Resources.LibResources.openssl_unknown_hash);
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a list of available digest methods.
        /// </summary>
        /// <param name="aliases">Set to TRUE if digest aliases should be included within the returned array.</param>
        /// <returns>An array of available digest methods.</returns>
        [return: NotNull]
        public static PhpArray openssl_get_md_methods(bool aliases = false)
        {
            var algos = hash_algos();

            algos.AddRange(AditionaHashMethods.Keys);

            if (aliases)
            {
                algos.AddRange(HashAliases.Keys);
            }

            return algos;
        }

        #endregion

        #region X.509

        private const string FileSchemePrefix = "file://";

        /// <summary>
        /// Context of X509 certificate
        /// </summary>
        public class X509Resource : PhpResource
        {
            public X509Certificate2 Certificate { get; private set; }

            public X509Resource(X509Certificate2 certificate) : base("OpenSSL X.509")
            {
                Certificate = certificate;
            }

            /// <summary>
            /// Disposes Certificate and sets it to null.
            /// </summary>
            protected override void FreeManaged()
            {
                base.FreeManaged();
                if (Certificate != null)
                    Certificate.Dispose();
                Certificate = null;
            }
        }

        /// <summary>
        /// Gets instance of <see cref="X509Resource"/> or <c>null</c>.
        /// If given argument is not an instance of <see cref="X509Resource"/>, PHP warning is reported.
        /// </summary>
        static X509Resource ParseX509Certificate(Context ctx, PhpValue mixed)
        {
            string cert;

            if (mixed.AsResource() is X509Resource h && h.IsValid)
            {
                return h;
            }
            else if ((cert = mixed.AsString(ctx)) != null)
            {
                try
                {
                    if (cert.StartsWith(FileSchemePrefix)) // Load from file 
                    {
                        return new X509Resource(new X509Certificate2(FileSystemUtils.AbsolutePath(ctx, cert)));
                    }
                    else // Load from string
                        return new X509Resource(new X509Certificate2(ctx.StringEncoding.GetBytes(cert)));
                }
                catch (CryptographicException)
                {
                    PhpException.Throw(PhpError.Warning, Resources.Resources.openssl_X509_cannot_be_coerced);
                    return null;
                }
            }

            //
            PhpException.Throw(PhpError.Warning, Resources.Resources.openssl_X509_cannot_be_coerced);
            return null;
        }

        /// <summary>
        /// Parses the certificate supplied by x509certdata and returns a resource identifier for it.
        /// </summary>
        /// <param name="ctx">Context of the script.</param>
        /// <param name="x509certdata">Path to file with PEM encoded certificate or a string containing the content of a certificate or X509Resource</param>
        /// <returns>Returns a resource identifier on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public static X509Resource openssl_x509_read(Context ctx, PhpValue x509certdata)
        {
            return ParseX509Certificate(ctx, x509certdata);
        }

        /// <summary>
        /// Stores x509 into a file named by outfilename in a PEM encoded format.
        /// </summary>
        /// <param name="ctx">Context of the script.</param>
        /// <param name="x509">Path to file with PEM encoded certificate or a string containing the content of a certificate or X509Resource</param>
        /// <param name="outfilename">Path to the output file.</param>
        /// <param name="notext">The optional parameter notext affects the verbosity of the output; if it is FALSE, then additional human-readable information is included in the output. The default value of notext is TRUE.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool openssl_x509_export_to_file(Context ctx, PhpValue x509, string outfilename, bool notext = true)
        {
            var resource = ParseX509Certificate(ctx, x509);
            if (resource == null)
                return false;

            string certificate = Export(resource, notext);

            try
            {
                using (StreamWriter wr = new StreamWriter(outfilename))
                    wr.Write(certificate);
            }
            catch (IOException) { return false; }

            return true;
        }

        private static string Export(X509Resource x509, bool notext)
        {
            StringBuilder builder = new StringBuilder();

            if (!notext)
            {
                builder.Append("Certificate:\n");
                builder.Append("\tData:\n");
                builder.AppendFormat("\t\tVersion: {0} 0x{0:X}\n", x509.Certificate.Version);
                builder.Append("\t\tSerial Number:\n");
                builder.AppendFormat("\t\t\t{0}\n", x509.Certificate.SerialNumber);
                builder.AppendFormat("\t\tSigniture Algorithm: {0}\n", x509.Certificate.SignatureAlgorithm.FriendlyName);
                builder.AppendFormat("\t\tIssuer: {0}\n", x509.Certificate.Issuer);
                builder.Append("\t\tValidity:\n");
                builder.AppendFormat("\t\t\tNot Before: {0:R}\n", x509.Certificate.NotBefore);
                builder.AppendFormat("\t\t\tNot After : {0:R}\n", x509.Certificate.NotAfter);
                builder.AppendFormat("\t\tSubject: {0}\n", x509.Certificate.Subject);
                builder.Append("\t\tSubject Public Key Info:\n");
                builder.AppendFormat("\t\t\tPublic Key Algorithm: {0}\n", x509.Certificate.PublicKey.Key.KeyExchangeAlgorithm);
                builder.AppendFormat("\t\t\t\tRSA Public-Key: ({0} bit)\n", x509.Certificate.PublicKey.Key.KeySize);
                // Key Parameters       
                RSAParameters parameters = x509.Certificate.GetRSAPublicKey().ExportParameters(false);
                builder.Append("\t\t\t\tModulus:\n");
                builder.AppendFormat("\t\t\t\t\t{0}\n", BitConverter.ToString(parameters.Modulus).Replace("-", ":"));
                string exponent = BitConverter.ToString(parameters.Exponent).Replace("-", "");
                builder.AppendFormat("\t\t\t\tExponent: {0} (0x{1})\n", System.Convert.ToInt32(exponent, 16), exponent);
                builder.AppendFormat("\t\tX509v{0} extensions:\n", x509.Certificate.Version);
                // TODO: Extensions
                builder.AppendFormat("\t\tSigniture Algorithm: {0}\n", x509.Certificate.SignatureAlgorithm.FriendlyName);
                // TODO: Last field of bytes ??
                builder.AppendFormat("\t\t\t{0}\n", BitConverter.ToString(x509.Certificate.RawData).Replace("-", ":"));
            }
            builder.Append("-----BEGIN CERTIFICATE-----\n");

            int alignment = 64;
            string encoded = System.Convert.ToBase64String(x509.Certificate.Export(X509ContentType.Cert));

            int reminder = 0;
            while (reminder < encoded.Length - alignment)
            {
                builder.Append(encoded.Substring(reminder, alignment));
                builder.Append("\n");
                reminder += alignment;
            }

            if (reminder != encoded.Length - 1)
                builder.Append(encoded.Substring(reminder, encoded.Length - reminder));
            builder.Append("\n");

            builder.Append("-----END CERTIFICATE-----\n");

            return builder.ToString();
        }

        /// <summary>
        /// Calculates the fingerprint, or digest, of a given X.509 certificate
        /// </summary>
        /// <param name="ctx">Context of the script.</param>
        /// <param name="x509">Path to file with PEM encoded certificate or a string containing the content of a certificate or X509Resource</param>
        /// <param name="hash_algorithm">The digest method or hash algorithm to use, e.g. "sha256", one of openssl_get_md_methods().</param>
        /// <param name="raw_output">When set to TRUE, outputs raw binary data. FALSE outputs lowercase hexits.</param>
        /// <returns>Returns a string containing the calculated certificate fingerprint as lowercase hexits unless raw_output is set to TRUE in which case the raw binary representation of the message digest is returned.</returns>
        [return: CastToFalse]
        public static PhpString openssl_x509_fingerprint(Context ctx, PhpValue x509, string hash_algorithm = "sha1", bool raw_output = false)
        {
            var resource = ParseX509Certificate(ctx, x509);
            if (resource == null)
                return null;

            return openssl_digest(ctx, new PhpString(resource.Certificate.Export(X509ContentType.Cert)), hash_algorithm, raw_output);
        }

        /// <summary>
        /// Stores x509 into a string named by output in a PEM encoded form
        /// </summary>
        /// <param name="ctx">Context of the script.</param>
        /// <param name="x509">Path to file with PEM encoded certificate or a string containing the content of a certificate or X509Resource</param>
        /// <param name="output">On success, this will hold the PEM.</param>
        /// <param name="notext">The optional parameter notext affects the verbosity of the output; if it is FALSE, then additional human-readable information is included in the output. The default value of notext is TRUE.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool openssl_x509_export(Context ctx, PhpValue x509, ref string output, bool notext = true)
        {
            var resource = ParseX509Certificate(ctx, x509);
            if (resource == null)
                return false;

            output = Export(resource, notext);

            return true;
        }

        /// <summary>
        /// Frees the certificate associated with the specified x509cert resource from memory.
        /// </summary>
        public static void openssl_x509_free(PhpResource x509cert)
        {
            if (x509cert is X509Resource h)
            {
                h.Dispose();
            }
        }

        #endregion

        #region OpenSSL key

        public const int OPENSSL_KEYTYPE_RSA = (int)KeyType.RSA; // 0
        public const int OPENSSL_KEYTYPE_DSA = (int)KeyType.DSA; // 1
        public const int OPENSSL_KEYTYPE_DH = (int)KeyType.DH; // 2
        public const int OPENSSL_KEYTYPE_EC = (int)KeyType.EC; // 3

        public const int OPENSSL_ALGO_SHA1 = 1;
        public const int OPENSSL_ALGO_MD5 = 2;
        public const int OPENSSL_ALGO_MD4 = 3;
        public const int OPENSSL_ALGO_SHA256 = 7;
        public const int OPENSSL_ALGO_SHA384 = 8;
        public const int OPENSSL_ALGO_SHA512 = 9;
        // Others methods are not supported

        public enum KeyType { RSA = 0, DSA = 1, DH = 2, EC = 3 };

        /// <summary>
        /// Context of OpenSSL Key
        /// </summary>
        public class OpenSSLKeyResource : PhpResource
        {
            public AsymmetricAlgorithm Algorithm { get; } = null;
            KeyType Type;

            public OpenSSLKeyResource(AsymmetricAlgorithm algorithm, KeyType type) : base("OpenSSL key")
            {
                Algorithm = algorithm;
                Type = type;
            }

            ///// <summary>
            ///// Exports key in PEM format.
            ///// </summary>
            ///// <param name="ctx">Context of the script.</param>
            ///// <param name="publicKey"> Exports public key if TRUE else private key.</param>
            ///// <returns>PEM formatted key.</returns>
            //public string Export(Context ctx, bool publicKey)
            //{
            //    RSAParameters parameters = new RSAParameters();
            //    switch (Type)
            //    {
            //        case KeyType.RSA:
            //            parameters = ((RSA)Algorithm).ExportParameters(false);
            //            break;
            //        case KeyType.DSA:
            //        case KeyType.DH:
            //        case KeyType.EC:
            //            throw new NotImplementedException();
            //    }

            //    using MemoryStream stream = new MemoryStream();
            //    using (var writer = new PemWriter(stream))
            //    {
            //        if (publicKey)
            //            writer.WritePublicKey(parameters);
            //        else
            //            writer.WritePrivateKey(parameters);

            //    }

            //    return ctx.StringEncoding.GetString(stream.ToArray());
            //}

            //public byte[] Sign(byte[] data)
            //{
            //    switch (Type)
            //    {
            //        case KeyType.RSA:
            //            return ((RSA)Algorithm).SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            //        case KeyType.DSA:
            //        case KeyType.DH:
            //        case KeyType.EC:
            //            throw new NotImplementedException();
            //    }
            //    throw new NotImplementedException();
            //}
        }

        static OpenSSLKeyResource ParseOpenSSLKey(PhpValue mixed)
        {
            if (mixed.AsResource() is OpenSSLKeyResource h && h.IsValid)
            {
                return h;
            }
            else
            {
                //var path = "a";
                //using (var stream = File.OpenRead(path))
                //using (var reader = new PemReader(stream))
                //{
                //    var rsaParameters = reader.ReadRsaKey();
                //    // ...
                //}
                // TODO: Other posibilities
                throw new NotImplementedException();
            }

            //
            //PhpException.Throw(PhpError.Warning, Resources.Resources.X509_cannot_be_coerced);
            //return null;
        }

        public static OpenSSLKeyResource openssl_pkey_new(PhpArray configargs = null)
        {
            KeyType type = KeyType.RSA; // By default

            if (configargs != null && configargs.Count != 0)
            {
                // Important atributes: config, curve_name, encrypt_key_cipher, encrypt_key, private_key_type, private_key_bits

                // // private_key_type
                if (configargs.TryGetValue("private_key_type", out var fieldtypeValue) &&
                    fieldtypeValue.IsLong(out var fieldtype))
                {
                    type = (KeyType)fieldtype;
                }

                // TODO: Other Atributes
            }

            AsymmetricAlgorithm alg;
            switch (type)
            {
                case KeyType.RSA:
                    alg = RSA.Create();
                    break;
                case KeyType.DSA:
                    alg = DSA.Create();
                    break;
                case KeyType.DH:
                case KeyType.EC:
                    throw new NotImplementedException();

                default:
                    throw new ArgumentException($"private_key_type '{type}' unsupported.");
            }

            return new OpenSSLKeyResource(alg, type);
        }

        //public static int openssl_verify(Context ctx, string data , string signature , PhpValue pub_key_id, PhpValue signature_alg)
        //{
        //    int defaultSignitureAlg = OPENSSL_ALGO_SHA1;

        //    if (signature_alg.IsInteger())
        //    {
        //        throw new NotImplementedException();
        //    } else if (signature_alg.IsString()) 
        //    {
        //        throw new NotImplementedException();
        //    }

        //    var resource = ParseOpenSSLKey(pub_key_id);
        //    if (resource == null)
        //        return -1;

        //    // TODO: Compare array byte -> pozor na porovnani referenci
        //    return (resource.Sign(Core.Convert.ToBytes(data, ctx)) == Core.Convert.ToBytes(signature, ctx)) ? 1 : 0;
        //}

        //public static bool openssl_pkey_export(PhpValue key, ref string pkey, string passphrase = "", PhpArray configargs = null)
        //{
        //    var resource = ParseOpenSSLKey(key);
        //    if (resource == null)
        //        return false;



        //    // TODO: Implementation
        //    throw new NotImplementedException();
        //}


        #endregion
    }
}
