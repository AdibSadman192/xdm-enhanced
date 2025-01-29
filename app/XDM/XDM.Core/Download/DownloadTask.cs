using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using XDM.Core.IO;
using XDM.Core.Network;

namespace XDM.Core.Download
{
    /// <summary>
    /// Represents a single download task with support for pause, resume, chunked downloads, and smart scheduling
    /// </summary>
    public class DownloadTask
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;
        private readonly string _destinationPath;
        private CancellationTokenSource _cancellationTokenSource;
        private DownloadStatus _status;
        private double _progress;
        private long _downloadedBytes;
        private long _totalBytes;
        private DateTime? _scheduledTime;
        private Exception _lastError;

        public event EventHandler<ProgressEventArgs> ProgressChanged;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        public event EventHandler<ScheduleEventArgs> ScheduleChanged;

        public string Id { get; }
        public string Url => _url;
        public string DestinationPath => _destinationPath;
        public DownloadStatus Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    OnScheduleChanged();
                }
            }
        }
        public double Progress => _progress;
        public long DownloadedBytes => _downloadedBytes;
        public long TotalBytes => _totalBytes;
        public DateTime? ScheduledTime => _scheduledTime;
        public Exception LastError => _lastError;

        public DownloadTask(string id, string url, string destinationPath, HttpClient httpClient)
        {
            Id = id;
            _url = url;
            _destinationPath = destinationPath;
            _httpClient = httpClient;
            _status = DownloadStatus.Queued;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync(PooledBuffer buffer)
        {
            try
            {
                Status = DownloadStatus.Downloading;

                using var response = await _httpClient.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                _totalBytes = response.Content.Headers.ContentLength ?? -1;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(_destinationPath);

                var bytesRead = 0;
                _downloadedBytes = 0;

                while ((bytesRead = await stream.ReadAsync(buffer.Buffer, 0, buffer.Buffer.Length)) > 0)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Status = DownloadStatus.Cancelled;
                        return;
                    }

                    await fileStream.WriteAsync(buffer.Buffer, 0, bytesRead);
                    _downloadedBytes += bytesRead;

                    if (_totalBytes > 0)
                    {
                        _progress = (double)_downloadedBytes / _totalBytes * 100;
                    }

                    OnProgressChanged();
                }

                Status = DownloadStatus.Completed;
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private async Task ScheduleDownloadAsync(SchedulePreference preference)
        {
            try
            {
                var scheduledTime = GetOptimalDownloadTime(preference);
                if (scheduledTime > DateTime.Now)
                {
                    _scheduledTime = scheduledTime;
                    Status = DownloadStatus.Scheduled;

                    var delay = scheduledTime - DateTime.Now;
                    await Task.Delay(delay);

                    if (Status == DownloadStatus.Scheduled) // Only start if still scheduled
                    {
                        _scheduledTime = null;
                        await StartAsync(new PooledBuffer(8192));
                    }
                }
                else
                {
                    await StartAsync(new PooledBuffer(8192));
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
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

            return now.Date + offPeakStart; // Next off-peak start time
        }

        private DateTime GetOptimalDownloadTime(SchedulePreference preference)
        {
            return preference.Priority switch
            {
                SchedulePriority.OffPeak => GetNextOffPeakTime(),
                _ => DateTime.Now
            };
        }

        public void Pause()
        {
            if (Status == DownloadStatus.Downloading)
            {
                Status = DownloadStatus.Paused;
                _cancellationTokenSource.Cancel();
            }
        }

        public async Task Resume()
        {
            if (Status == DownloadStatus.Paused)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                await StartAsync(new PooledBuffer(8192));
            }
        }

        public void Cancel()
        {
            Status = DownloadStatus.Cancelled;
            _cancellationTokenSource.Cancel();
        }

        private void SetError(Exception error)
        {
            _lastError = error;
            Status = DownloadStatus.Failed;
            OnErrorOccurred();
        }

        private void OnProgressChanged()
        {
            ProgressChanged?.Invoke(this, new ProgressEventArgs(_progress, _downloadedBytes, _totalBytes));
        }

        private void OnErrorOccurred()
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(_lastError));
        }

        private void OnScheduleChanged()
        {
            ScheduleChanged?.Invoke(this, new ScheduleEventArgs(_status, _scheduledTime));
        }
    }

    public class ProgressEventArgs : EventArgs
    {
        public double Progress { get; }
        public long DownloadedBytes { get; }
        public long TotalBytes { get; }

        public ProgressEventArgs(double progress, long downloadedBytes, long totalBytes)
        {
            Progress = progress;
            DownloadedBytes = downloadedBytes;
            TotalBytes = totalBytes;
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        public Exception Error { get; }

        public ErrorEventArgs(Exception error)
        {
            Error = error;
        }
    }

    public class ScheduleEventArgs : EventArgs
    {
        public DownloadStatus Status { get; }
        public DateTime? ScheduledTime { get; }

        public ScheduleEventArgs(DownloadStatus status, DateTime? scheduledTime)
        {
            Status = status;
            ScheduledTime = scheduledTime;
        }
    }

    public enum DownloadStatus
    {
        Queued,
        Downloading,
        Paused,
        Cancelled,
        Completed,
        Scheduled,
        Failed
    }

    public enum SchedulePriority
    {
        Immediate,
        OffPeak,
        BestEffort
    }

    public class SchedulePreference
    {
        public SchedulePriority Priority { get; set; }
    }
}
