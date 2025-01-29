using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using XDM.Core.Security;

namespace XDM.Tests
{
    [TestClass]
    public class SecurityTests
    {
        [TestMethod]
        public void TestSecureStorage()
        {
            const string sensitiveData = "MySecretPassword123";
            
            // Protect the data
            string protectedData = SecureStorage.ProtectData(sensitiveData);
            Assert.IsFalse(string.IsNullOrEmpty(protectedData), "Protected data should not be empty");
            Assert.AreNotEqual(sensitiveData, protectedData, "Protected data should be different from original");

            // Unprotect the data
            string unprotectedData = SecureStorage.UnprotectData(protectedData);
            Assert.AreEqual(sensitiveData, unprotectedData, "Unprotected data should match original");
        }

        [TestMethod]
        public async Task TestFileIntegrityChecker()
        {
            // Create a test file
            string testFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(testFile, "Test content for integrity checking");

            try
            {
                // Calculate hash
                string hash = await FileIntegrityChecker.CalculateFileHashAsync(testFile);
                Assert.IsFalse(string.IsNullOrEmpty(hash), "Hash should not be empty");

                // Verify hash
                bool verified = await FileIntegrityChecker.VerifyFileHashAsync(testFile, hash);
                Assert.IsTrue(verified, "Hash verification should succeed");

                // Modify file and verify hash changes
                await File.AppendAllTextAsync(testFile, "Modified content");
                string newHash = await FileIntegrityChecker.CalculateFileHashAsync(testFile);
                Assert.AreNotEqual(hash, newHash, "Hash should change when file is modified");
            }
            finally
            {
                File.Delete(testFile);
            }
        }

        [TestMethod]
        public void TestCertificateValidation()
        {
            // Test with null certificate (should fail)
            bool result = CertificateValidator.ValidateServerCertificate(
                null, null, null, System.Net.Security.SslPolicyErrors.None);
            Assert.IsFalse(result, "Validation should fail with null certificate");

            // Enable/disable validation
            CertificateValidator.EnforceValidation = false;
            result = CertificateValidator.ValidateServerCertificate(
                null, null, null, System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable);
            Assert.IsTrue(result, "Validation should pass when enforcement is disabled");

            CertificateValidator.EnforceValidation = true;
            result = CertificateValidator.ValidateServerCertificate(
                null, null, null, System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable);
            Assert.IsFalse(result, "Validation should fail when enforcement is enabled");
        }
    }
}
