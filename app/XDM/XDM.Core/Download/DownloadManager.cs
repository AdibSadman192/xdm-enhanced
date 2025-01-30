using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using XDM.Core.IO;

namespace XDM.Core.Download
{
    /// <summary>
    /// Manages download operations with improved performance and resource management
    /// </summary>
    public class DownloadManager
    {
        private readonly ConcurrentDictionary<string, DownloadTask> _activeTasks = new();
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly int _maxConcurrentDownloads;
        private const int DefaultBufferSize = 81920; // 80KB

        public DownloadManager(int maxConcurrentDownloads = 5)
        {
            _maxConcurrentDownloads = maxConcurrentDownloads;
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrentDownloads);
        }

        /// <summary>
        /// Starts a new download with optimized resource usage
        /// </summary>
        public async Task<string> StartDownloadAsync(string url, string destinationPath)
        {
            await _concurrencyLimiter.WaitAsync();
            
            try
            {
                var downloadId = Guid.NewGuid().ToString();
                var task = new DownloadTask(url, destinationPath);
                
                if (_activeTasks.TryAdd(downloadId, task))
                {
                    _ = ProcessDownloadAsync(downloadId, task);
                    return downloadId;
                }
                
                throw new InvalidOperationException("Failed to start download");
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }

        private async Task ProcessDownloadAsync(string downloadId, DownloadTask task)
        {
            try
            {
                using var buffer = new IO.PooledBuffer(DefaultBufferSize);
                await task.StartAsync(buffer);
            }
            catch (Exception ex)
            {
                task.SetError(ex);
            }
            finally
            {
                _activeTasks.TryRemove(downloadId, out _);
            }
        }

        /// <summary>
        /// Cancels an active download
        /// </summary>
        public void CancelDownload(string downloadId)
        {
            if (_activeTasks.TryGetValue(downloadId, out var task))
            {
                task.Cancel();
                _activeTasks.TryRemove(downloadId, out _);
            }
        }

        /// <summary>
        /// Pauses an active download
        /// </summary>
        public void PauseDownload(string downloadId)
        {
            if (_activeTasks.TryGetValue(downloadId, out var task))
            {
                task.Pause();
            }
        }

        /// <summary>
        /// Resumes a paused download
        /// </summary>
        public void ResumeDownload(string downloadId)
        {
            if (_activeTasks.TryGetValue(downloadId, out var task))
            {
                task.Resume();
            }
        }
    }
}
