using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using XDM.Core.Network;

namespace XDM.Core.Download
{
    /// <summary>
    /// Smart download scheduler that optimizes download timing and bandwidth usage
    /// </summary>
    public class SmartScheduler
    {
        private readonly Dictionary<string, ScheduledDownload> _scheduledDownloads;
        private readonly SemaphoreSlim _schedulerLock;
        private readonly NetworkMonitor _networkMonitor;
        private readonly BandwidthPredictor _bandwidthPredictor;
        private Timer _schedulerTimer;

        public SmartScheduler()
        {
            _scheduledDownloads = new Dictionary<string, ScheduledDownload>();
            _schedulerLock = new SemaphoreSlim(1);
            _networkMonitor = new NetworkMonitor();
            _bandwidthPredictor = new BandwidthPredictor();
            InitializeScheduler();
        }

        private void InitializeScheduler()
        {
            _schedulerTimer = new Timer(
                async _ => await ProcessSchedule(),
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1)
            );
        }

        public async Task ScheduleDownloadAsync(DownloadTask task, SchedulePreference preference)
        {
            await _schedulerLock.WaitAsync();
            try
            {
                var schedule = new ScheduledDownload
                {
                    Task = task,
                    Preference = preference,
                    Status = ScheduleStatus.Pending,
                    ScheduledTime = CalculateOptimalTime(preference)
                };

                _scheduledDownloads[task.Id] = schedule;
            }
            finally
            {
                _schedulerLock.Release();
            }
        }

        private DateTime CalculateOptimalTime(SchedulePreference preference)
        {
            switch (preference.Priority)
            {
                case SchedulePriority.Immediate:
                    return DateTime.Now;

                case SchedulePriority.OffPeak:
                    return GetNextOffPeakTime();

                case SchedulePriority.BestEffort:
                    return GetOptimalTimeWithinRange(preference);

                default:
                    return DateTime.Now;
            }
        }

        private DateTime GetNextOffPeakTime()
        {
            var now = DateTime.Now;
            var offPeakStart = new TimeSpan(23, 0, 0); // 11 PM
            var offPeakEnd = new TimeSpan(6, 0, 0); // 6 AM
            var currentTime = now.TimeOfDay;

            if (currentTime >= offPeakStart || currentTime < offPeakEnd)
            {
                return now; // Already in off-peak hours
            }

            return now.Date.Add(offPeakStart);
        }

        private DateTime GetOptimalTimeWithinRange(SchedulePreference preference)
        {
            var predictions = _bandwidthPredictor.GetHourlyPredictions(DateTime.Now, 24);
            var bestPrediction = predictions
                .Where(p => p.AvailableBandwidth >= preference.MinBandwidthBytesPerSecond)
                .OrderByDescending(p => p.Confidence)
                .ThenBy(p => p.Hour)
                .FirstOrDefault();

            if (bestPrediction != null)
            {
                return DateTime.Now.Date.AddHours(bestPrediction.Hour);
            }

            return DateTime.Now; // If no suitable time found, start immediately
        }

        private async Task ProcessSchedule()
        {
            await _schedulerLock.WaitAsync();
            try
            {
                var now = DateTime.Now;
                var readyDownloads = _scheduledDownloads
                    .Where(kvp => kvp.Value.Status == ScheduleStatus.Pending && kvp.Value.ScheduledTime <= now)
                    .ToList();

                foreach (var download in readyDownloads)
                {
                    var schedule = download.Value;
                    schedule.Status = ScheduleStatus.Running;

                    // Start the download asynchronously
                    var buffer = new IO.PooledBuffer(8192); // 8KB buffer
                    _ = schedule.Task.StartAsync(buffer, schedule.Preference)
                        .ContinueWith(t =>
                        {
                            if (t.IsCompleted)
                            {
                                schedule.Status = ScheduleStatus.Completed;
                            }
                            else if (t.IsFaulted)
                            {
                                schedule.Status = ScheduleStatus.Failed;
                            }
                        });
                }

                // Clean up completed downloads
                var completedIds = _scheduledDownloads
                    .Where(kvp => kvp.Value.Status == ScheduleStatus.Completed || kvp.Value.Status == ScheduleStatus.Failed)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var id in completedIds)
                {
                    _scheduledDownloads.Remove(id);
                }
            }
            finally
            {
                _schedulerLock.Release();
            }
        }

        public void Dispose()
        {
            _schedulerTimer?.Dispose();
            _networkMonitor.Dispose();
            _schedulerLock.Dispose();
        }
    }

    public class ScheduledDownload
    {
        public DownloadTask Task { get; set; }
        public SchedulePreference Preference { get; set; }
        public ScheduleStatus Status { get; set; }
        public DateTime ScheduledTime { get; set; }
    }

    public class ScheduleInfo
    {
        public string TaskId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public ScheduleStatus Status { get; set; }
        public double Progress { get; set; }
        public string Error { get; set; }
    }

    public enum ScheduleStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }
}
