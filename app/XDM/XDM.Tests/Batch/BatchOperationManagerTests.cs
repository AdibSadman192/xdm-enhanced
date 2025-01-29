using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using XDM.Core.Batch;

namespace XDM.Tests.Batch
{
    public class BatchOperationManagerTests
    {
        private readonly BatchOperationManager _manager;
        private readonly string _testDirectory;

        public BatchOperationManagerTests()
        {
            _manager = new BatchOperationManager();
            _testDirectory = Path.Combine(Path.GetTempPath(), "XDMTests");
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public async Task BatchRename_WithCounter_ShouldRenameCorrectly()
        {
            // Arrange
            var files = CreateTestFiles(3);
            var options = new BatchOperationOptions
            {
                Pattern = "file_{n}",
                StartIndex = 1,
                PaddingDigits = 3,
                Overwrite = false
            };

            // Act
            await _manager.ExecuteBatchOperationAsync("rename", files, options);

            // Assert
            File.Exists(Path.Combine(_testDirectory, "file_001.txt")).Should().BeTrue();
            File.Exists(Path.Combine(_testDirectory, "file_002.txt")).Should().BeTrue();
            File.Exists(Path.Combine(_testDirectory, "file_003.txt")).Should().BeTrue();

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task BatchRename_WithRegexPattern_ShouldRenameCorrectly()
        {
            // Arrange
            var files = CreateTestFiles(2, "test-file-{0}.txt");
            var options = new BatchOperationOptions
            {
                Pattern = "{name}",
                RegexPattern = "test-file-([0-9]+)",
                RegexReplacement = "processed-$1",
                Overwrite = false
            };

            // Act
            await _manager.ExecuteBatchOperationAsync("rename", files, options);

            // Assert
            File.Exists(Path.Combine(_testDirectory, "processed-1.txt")).Should().BeTrue();
            File.Exists(Path.Combine(_testDirectory, "processed-2.txt")).Should().BeTrue();

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task BatchMove_ShouldMoveFilesCorrectly()
        {
            // Arrange
            var files = CreateTestFiles(2);
            var destinationDir = Path.Combine(_testDirectory, "moved");
            Directory.CreateDirectory(destinationDir);

            var options = new BatchOperationOptions
            {
                DestinationDirectory = destinationDir,
                Overwrite = false
            };

            // Act
            await _manager.ExecuteBatchOperationAsync("move", files, options);

            // Assert
            File.Exists(Path.Combine(destinationDir, "test1.txt")).Should().BeTrue();
            File.Exists(Path.Combine(destinationDir, "test2.txt")).Should().BeTrue();

            // Cleanup
            CleanupTestFiles();
        }

        [Fact]
        public async Task BatchCategory_ShouldAssignCategoryCorrectly()
        {
            // Arrange
            var files = CreateTestFiles(2);
            var options = new BatchOperationOptions
            {
                Category = "downloads"
            };

            // Act
            await _manager.ExecuteBatchOperationAsync("category", files, options);

            // Assert
            foreach (var file in files)
            {
                var categoryStream = File.OpenRead(file + ":category");
                using var reader = new StreamReader(categoryStream);
                var category = await reader.ReadToEndAsync();
                category.Should().Be("downloads");
            }

            // Cleanup
            CleanupTestFiles();
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData(null)]
        [InlineData("")]
        public async Task ExecuteBatchOperation_InvalidType_ShouldThrowException(string operationType)
        {
            // Arrange
            var files = new[] { "test.txt" };
            var options = new BatchOperationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(
                () => _manager.ExecuteBatchOperationAsync(operationType, files, options)
            );
        }

        [Fact]
        public async Task BatchRename_WithExistingFiles_ShouldNotOverwrite()
        {
            // Arrange
            var files = CreateTestFiles(2);
            var options = new BatchOperationOptions
            {
                Pattern = "file",
                Overwrite = false
            };

            // Create existing file
            var existingFile = Path.Combine(_testDirectory, "file.txt");
            File.WriteAllText(existingFile, "existing content");

            // Act
            await _manager.ExecuteBatchOperationAsync("rename", files, options);

            // Assert
            File.ReadAllText(existingFile).Should().Be("existing content");

            // Cleanup
            CleanupTestFiles();
        }

        private List<string> CreateTestFiles(int count, string pattern = "test{0}.txt")
        {
            var files = new List<string>();
            for (int i = 1; i <= count; i++)
            {
                var fileName = string.Format(pattern, i);
                var filePath = Path.Combine(_testDirectory, fileName);
                File.WriteAllText(filePath, $"Test content {i}");
                files.Add(filePath);
            }
            return files;
        }

        private void CleanupTestFiles()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
    }
}
