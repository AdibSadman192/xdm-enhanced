using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace XDM.Core.Download
{
    /// <summary>
    /// Optimizes download queue processing and bandwidth allocation
    /// </summary>
    public class QueueOptimizer
    {
        private readonly SemaphoreSlim _queueSemaphore;
        private readonly Dictionary<string, DownloadPriority> _priorityMap;
        private readonly List<DownloadTask> _activeDownloads;
        private readonly object _lock = new object();
        private readonly BandwidthManager _bandwidthManager;

        public QueueOptimizer(int maxConcurrentDownloads = 5)
        {
            _queueSemaphore = new SemaphoreSlim(maxConcurrentDownloads);
            _priorityMap = new Dictionary<string, DownloadPriority>();
            _activeDownloads = new List<DownloadTask>();
            _bandwidthManager = new BandwidthManager();
        }

        public async Task<bool> AddToQueueAsync(DownloadTask task, DownloadPriority priority)
        {
            try
            {
                lock (_lock)
                {
                    _priorityMap[task.Id] = priority;
                }

                await _queueSemaphore.WaitAsync();

                lock (_lock)
                {
                    _activeDownloads.Add(task);
                }

                OptimizeBandwidth();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void RemoveFromQueue(string taskId)
        {
            lock (_lock)
            {
                var task = _activeDownloads.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    _activeDownloads.Remove(task);
                    _priorityMap.Remove(taskId);
                    _queueSemaphore.Release();
                }

                OptimizeBandwidth();
            }
        }

        private void OptimizeBandwidth()
        {
            lock (_lock)
            {
                var totalBandwidth = _bandwidthManager.GetAvailableBandwidth();
                var priorityGroups = _activeDownloads
                    .GroupBy(t => _priorityMap[t.Id])
                    .OrderByDescending(g => g.Key);

                foreach (var group in priorityGroups)
                {
                    var groupBandwidth = CalculateGroupBandwidth(totalBandwidth, group.Key);
                    var perTaskBandwidth = groupBandwidth / group.Count();

                    foreach (var task in group)
                    {
                        task.SetBandwidthLimit(perTaskBandwidth);
                    }
                }
            }
        }

        private long CalculateGroupBandwidth(long totalBandwidth, DownloadPriority priority)
        {
            return priority switch
            {
                DownloadPriority.High => (long)(totalBandwidth * 0.5),
                DownloadPriority.Medium => (long)(totalBandwidth * 0.3),
                DownloadPriority.Low => (long)(totalBandwidth * 0.2),
                _ => (long)(totalBandwidth * 0.1)
            };
        }

        public void UpdatePriority(string taskId, DownloadPriority newPriority)
        {
            lock (_lock)
            {
                if (_priorityMap.ContainsKey(taskId))
                {
                    _priorityMap[taskId] = newPriority;
                    OptimizeBandwidth();
                }
            }
        }

        public List<QueueStatus> GetQueueStatus()
        {
            lock (_lock)
            {
                return _activeDownloads.Select(task => new QueueStatus
                {
                    TaskId = task.Id,
                    Priority = _priorityMap[task.Id],
                    Progress = task.Progress,
                    Speed = task.CurrentSpeed,
                    EstimatedTimeRemaining = task.EstimatedTimeRemaining
                }).ToList();
            }
        }
    }

    public class BandwidthManager
    {
        private const long DEFAULT_BANDWIDTH = 10 * 1024 * 1024; // 10 MB/s

        public long GetAvailableBandwidth()
        {
            // In a real implementation, this would measure actual network capacity
            return DEFAULT_BANDWIDTH;
        }
    }

    public enum DownloadPriority
    {
        Low,
        Medium,
        High
    }

    public class QueueStatus
    {
        public string TaskId { get; set; }
        public DownloadPriority Priority { get; set; }
        public double Progress { get; set; }
        public long Speed { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }

    public static class DownloadTaskExtensions
    {
        public static void SetBandwidthLimit(this DownloadTask task, long bytesPerSecond)
        {
            // Implementation would depend on the actual DownloadTask class
            // This is just a placeholder for the example
            task.BandwidthLimit = bytesPerSecond;
        }
    }
}
