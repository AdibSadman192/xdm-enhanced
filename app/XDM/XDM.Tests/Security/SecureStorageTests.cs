using System;
using Xunit;
using FluentAssertions;
using XDM.Core.Security;

namespace XDM.Tests.Security
{
    public class SecureStorageTests
    {
        [Fact]
        public void ProtectData_WithValidInput_ShouldEncryptAndDecrypt()
        {
            // Arrange
            var sensitiveData = "MySecretPassword123!@#";

            // Act
            var encrypted = SecureStorage.ProtectData(sensitiveData);
            var decrypted = SecureStorage.UnprotectData(encrypted);

            // Assert
            encrypted.Should().NotBe(sensitiveData);
            decrypted.Should().Be(sensitiveData);
        }

        [Fact]
        public void ProtectData_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            var emptyData = string.Empty;

            // Act
            var result = SecureStorage.ProtectData(emptyData);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void UnprotectData_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            var emptyData = string.Empty;

            // Act
            var result = SecureStorage.UnprotectData(emptyData);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ProtectData_WithNullInput_ShouldReturnEmptyString()
        {
            // Arrange
            string nullData = null;

            // Act
            var result = SecureStorage.ProtectData(nullData);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void UnprotectData_WithNullInput_ShouldReturnEmptyString()
        {
            // Arrange
            string nullData = null;

            // Act
            var result = SecureStorage.UnprotectData(nullData);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void UnprotectData_WithInvalidInput_ShouldThrowException()
        {
            // Arrange
            var invalidData = "NotValidBase64Data";

            // Act & Assert
            Action act = () => SecureStorage.UnprotectData(invalidData);
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Failed to unprotect data");
        }

        [Fact]
        public void ProtectData_MultipleCalls_ShouldGenerateDifferentEncryption()
        {
            // Arrange
            var sensitiveData = "MySecretData";

            // Act
            var encrypted1 = SecureStorage.ProtectData(sensitiveData);
            var encrypted2 = SecureStorage.ProtectData(sensitiveData);

            // Assert
            encrypted1.Should().NotBe(encrypted2); // Each encryption should be unique
            SecureStorage.UnprotectData(encrypted1).Should().Be(sensitiveData);
            SecureStorage.UnprotectData(encrypted2).Should().Be(sensitiveData);
        }

        [Theory]
        [InlineData("Short")]
        [InlineData("MediumSizedPassword123")]
        [InlineData("VeryLongPasswordWithSpecialCharacters!@#$%^&*()_+")]
        public void ProtectData_WithDifferentLengths_ShouldWorkCorrectly(string input)
        {
            // Act
            var encrypted = SecureStorage.ProtectData(input);
            var decrypted = SecureStorage.UnprotectData(encrypted);

            // Assert
            decrypted.Should().Be(input);
        }

        [Fact]
        public void ProtectData_WithUnicodeCharacters_ShouldWorkCorrectly()
        {
            // Arrange
            var unicodeData = "パスワード123アБВ";

            // Act
            var encrypted = SecureStorage.ProtectData(unicodeData);
            var decrypted = SecureStorage.UnprotectData(encrypted);

            // Assert
            decrypted.Should().Be(unicodeData);
        }
    }
}
