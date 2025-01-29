using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XDM.Core.Batch
{
    /// <summary>
    /// Manages batch operations for downloads
    /// </summary>
    public class BatchOperationManager
    {
        private readonly List<IBatchOperation> _operations;

        public BatchOperationManager()
        {
            _operations = new List<IBatchOperation>
            {
                new BatchRenameOperation(),
                new BatchMoveOperation(),
                new BatchCategoryOperation()
            };
        }

        public async Task ExecuteBatchOperationAsync(string operationType, IEnumerable<string> filePaths, BatchOperationOptions options)
        {
            var operation = _operations.FirstOrDefault(op => op.OperationType.Equals(operationType, StringComparison.OrdinalIgnoreCase));
            if (operation == null)
            {
                throw new NotSupportedException($"Operation type {operationType} is not supported");
            }

            await operation.ExecuteAsync(filePaths, options);
        }
    }

    public interface IBatchOperation
    {
        string OperationType { get; }
        Task ExecuteAsync(IEnumerable<string> filePaths, BatchOperationOptions options);
    }

    public class BatchRenameOperation : IBatchOperation
    {
        public string OperationType => "rename";

        public async Task ExecuteAsync(IEnumerable<string> filePaths, BatchOperationOptions options)
        {
            var pattern = options.Pattern ?? throw new ArgumentNullException(nameof(options.Pattern));
            var counter = options.StartIndex ?? 1;

            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath)) continue;

                var directory = Path.GetDirectoryName(filePath);
                var extension = Path.GetExtension(filePath);
                var originalName = Path.GetFileNameWithoutExtension(filePath);

                var newName = pattern
                    .Replace("{n}", counter.ToString("D" + options.PaddingDigits))
                    .Replace("{name}", originalName)
                    .Replace("{ext}", extension);

                if (options.RegexPattern != null && options.RegexReplacement != null)
                {
                    newName = Regex.Replace(newName, options.RegexPattern, options.RegexReplacement);
                }

                var newPath = Path.Combine(directory ?? "", newName + extension);
                
                if (File.Exists(newPath) && !options.Overwrite)
                {
                    continue;
                }

                File.Move(filePath, newPath);
                counter++;
            }

            await Task.CompletedTask;
        }
    }

    public class BatchMoveOperation : IBatchOperation
    {
        public string OperationType => "move";

        public async Task ExecuteAsync(IEnumerable<string> filePaths, BatchOperationOptions options)
        {
            var destinationDir = options.DestinationDirectory ?? throw new ArgumentNullException(nameof(options.DestinationDirectory));
            
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath)) continue;

                var fileName = Path.GetFileName(filePath);
                var destinationPath = Path.Combine(destinationDir, fileName);

                if (File.Exists(destinationPath) && !options.Overwrite)
                {
                    continue;
                }

                File.Move(filePath, destinationPath);
            }

            await Task.CompletedTask;
        }
    }

    public class BatchCategoryOperation : IBatchOperation
    {
        public string OperationType => "category";

        public async Task ExecuteAsync(IEnumerable<string> filePaths, BatchOperationOptions options)
        {
            var category = options.Category ?? throw new ArgumentNullException(nameof(options.Category));
            
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath)) continue;

                // Get file attributes
                var attributes = File.GetAttributes(filePath);
                
                // Add category to file metadata (using NTFS alternate data streams)
                using (var stream = File.OpenWrite(filePath + ":category"))
                {
                    using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(category);
                }
            }
        }
    }

    public class BatchOperationOptions
    {
        // Rename options
        public string Pattern { get; set; }
        public int? StartIndex { get; set; }
        public int PaddingDigits { get; set; } = 3;
        public string RegexPattern { get; set; }
        public string RegexReplacement { get; set; }

        // Move options
        public string DestinationDirectory { get; set; }

        // Category options
        public string Category { get; set; }

        // Common options
        public bool Overwrite { get; set; }
    }

    public class BatchOperationResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public string Error { get; set; }
    }
}
