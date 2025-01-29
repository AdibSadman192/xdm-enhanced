using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace XDM.Core.Security
{
    /// <summary>
    /// Provides enhanced certificate validation for HTTPS connections
    /// </summary>
    public static class CertificateValidator
    {
        private static bool _enforceValidation = true;

        /// <summary>
        /// Gets or sets whether to enforce strict certificate validation
        /// </summary>
        public static bool EnforceValidation
        {
            get => _enforceValidation;
            set => _enforceValidation = value;
        }

        /// <summary>
        /// Validates the remote certificate
        /// </summary>
        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (!_enforceValidation)
                return true;

            // If there are no errors, the certificate is valid
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            // Check specific error cases
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
                return false;

            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                return false;

            if (chain == null || certificate == null)
                return false;

            // Verify certificate chain
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                foreach (X509ChainStatus status in chain.ChainStatus)
                {
                    if (status.Status == X509ChainStatusFlags.RevocationStatusUnknown)
                        continue;

                    if (status.Status == X509ChainStatusFlags.OfflineRevocation)
                        continue;

                    // If we get here, the certificate is not valid
                    return false;
                }
            }

            return true;
        }
    }
}
