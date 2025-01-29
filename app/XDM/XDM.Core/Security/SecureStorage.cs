using System;
using System.Security.Cryptography;
using System.Text;

namespace XDM.Core.Security
{
    /// <summary>
    /// Provides secure storage functionality using Windows Data Protection API (DPAPI)
    /// </summary>
    public static class SecureStorage
    {
        /// <summary>
        /// Encrypts sensitive data using Windows DPAPI
        /// </summary>
        public static string ProtectData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] protectedData = ProtectedData.Protect(
                    dataBytes,
                    null, // Optional entropy
                    DataProtectionScope.CurrentUser);

                return Convert.ToBase64String(protectedData);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failed to protect data", ex);
            }
        }

        /// <summary>
        /// Decrypts sensitive data using Windows DPAPI
        /// </summary>
        public static string UnprotectData(string protectedData)
        {
            if (string.IsNullOrEmpty(protectedData))
                return string.Empty;

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(protectedData);
                byte[] originalData = ProtectedData.Unprotect(
                    protectedBytes,
                    null, // Optional entropy
                    DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(originalData);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failed to unprotect data", ex);
            }
        }
    }
}
