using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Resonance.Common
{
    public static class HashExtensions
    {
        private static readonly Lazy<MD5> Md5 = new Lazy<MD5>(MD5.Create);
        private static readonly Random Random = new Random();
        private static readonly Lazy<SHA1> Sha1 = new Lazy<SHA1>(SHA1.Create);
        private static readonly Lazy<SHA256> Sha256 = new Lazy<SHA256>(SHA256.Create);
        private static readonly Lazy<SHA384> Sha384 = new Lazy<SHA384>(SHA384.Create);
        private static readonly Lazy<SHA512> Sha512 = new Lazy<SHA512>(SHA512.Create);

        public static string ComputeHash(string plainText, HashType hashType, byte[] saltBytes)
        {
            // If salt is not specified, generate it.
            saltBytes ??= GenerateSalt();

            // Convert plain text into a byte array.
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // Allocate array, which will hold plain text and salt.
            var plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];

            // Copy plain text bytes into resulting array.
            for (var i = 0; i < plainTextBytes.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainTextBytes[i];
            }

            // Append salt bytes to the resulting array.
            for (var i = 0; i < saltBytes.Length; i++)
            {
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
            }

            HashAlgorithm hash = hashType switch
            {
                HashType.SHA1 => Sha1.Value,
                HashType.SHA256 => Sha256.Value,
                HashType.SHA384 => Sha384.Value,
                HashType.SHA512 => Sha512.Value,
                _ => Md5.Value,
            };

            // Compute hash value of our plain text with appended salt.
            var hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

            // Create array which will hold hash and original salt bytes.
            var hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];

            // Copy hash bytes into resulting array.
            for (var i = 0; i < hashBytes.Length; i++)
            {
                hashWithSaltBytes[i] = hashBytes[i];
            }

            // Append salt bytes to the result.
            for (var i = 0; i < saltBytes.Length; i++)
            {
                hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];
            }

            // Convert result into a base64-encoded string.

            // Return the result.
            return Convert.ToBase64String(hashWithSaltBytes);
        }

        public static byte[] GenerateSalt()
        {
            // Define min and max salt sizes.
            const int minSaltSize = 8;
            const int maxSaltSize = 24;

            // Generate a random number for the size of the salt.
            var saltSize = Random.Next(minSaltSize, maxSaltSize);

            // Allocate a byte array, which will hold the salt.
            var saltBytes = new byte[saltSize];

            // Initialize a random number generator.
            var rng = RandomNumberGenerator.Create();

            // Fill the salt with cryptographically strong byte values.
            rng.GetBytes(saltBytes);

            return saltBytes;
        }

        public static string GetFileHash(string path, HashType hashType)
        {
            return GetHash(File.ReadAllBytes(path), hashType);
        }

        public static string GetHash(this byte[] bytes, HashType hashType)
        {
            var sb = new StringBuilder();

            HashAlgorithm hashAlgorithm = hashType switch
            {
                HashType.SHA1 => Sha1.Value,
                HashType.SHA256 => Sha256.Value,
                HashType.SHA384 => Sha384.Value,
                HashType.SHA512 => Sha512.Value,
                _ => Md5.Value,
            };

            foreach (var b in hashAlgorithm.ComputeHash(bytes))
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        public static string GetHash(this FileInfo fileInfo, HashType hashType)
        {
            return GetHash(File.ReadAllBytes(fileInfo.FullName), hashType);
        }

        public static string GetHash(this string value, HashType hashType, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            return GetHash(encoding.GetBytes(value), hashType);
        }

        public static bool VerifyHash(string plainText, HashType hashType, string hashValue)
        {
            // Convert base64-encoded hash value into a byte array.
            var hashWithSaltBytes = Convert.FromBase64String(hashValue);

            var hashSizeInBits = hashType switch
            {
                HashType.SHA1 => 160,
                HashType.SHA256 => 256,
                HashType.SHA384 => 384,
                HashType.SHA512 => 512,
                _ => 128,
            };

            // Convert size of hash from bits to bytes.
            var hashSizeInBytes = hashSizeInBits / 8;

            // Make sure that the specified hash value is long enough.
            if (hashWithSaltBytes.Length < hashSizeInBytes)
                return false;

            // Allocate array to hold original salt bytes retrieved from hash.
            var saltBytes = new byte[hashWithSaltBytes.Length - hashSizeInBytes];

            // Copy salt from the end of the hash to the new array.
            for (var i = 0; i < saltBytes.Length; i++)
            {
                saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];
            }

            // Compute a new hash string.
            var expectedHashString = ComputeHash(plainText, hashType, saltBytes);

            // If the computed hash matches the specified hash,
            // the plain text value must be correct.
            return hashValue == expectedHashString;
        }
    }
}