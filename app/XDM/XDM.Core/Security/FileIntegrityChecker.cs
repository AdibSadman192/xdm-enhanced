using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace XDM.Core.Security
{
    /// <summary>
    /// Provides file integrity checking functionality
    /// </summary>
    public static class FileIntegrityChecker
    {
        /// <summary>
        /// Calculates the SHA-256 hash of a file
        /// </summary>
        public static async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await CalculateHashAsync(stream, sha256);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Verifies a file's hash against an expected value
        /// </summary>
        public static async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash)
        {
            var actualHash = await CalculateFileHashAsync(filePath);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<byte[]> CalculateHashAsync(Stream stream, HashAlgorithm hashAlgorithm)
        {
            const int bufferSize = 81920;
            var buffer = new byte[bufferSize];
            int read;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                hashAlgorithm.TransformBlock(buffer, 0, read, null, 0);
            }

            hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return hashAlgorithm.Hash ?? Array.Empty<byte>();
        }

        /// <summary>
        /// Verifies file signature if available
        /// </summary>
        public static async Task<bool> VerifyFileSignatureAsync(string filePath)
        {
            try
            {
                var sigCheck = new X509Certificate2();
                // Implementation would depend on the signature format used
                // This is a placeholder for the actual implementation
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
