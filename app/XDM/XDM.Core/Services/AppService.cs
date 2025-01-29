using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XDM.Core.Download;
using XDM.Core.Interfaces;
using XDM.Core.Network;
using XDM.Core.Media;
using XDM.Core.BrowserMonitoring;
using XDM.Core.Security;
using Newtonsoft.Json;

namespace XDM.Core.Services
{
    public class AppService : IAppService
    {
        private readonly NetworkMonitor _networkMonitor;
        private readonly BandwidthPredictor _bandwidthPredictor;
        private readonly VideoConverter _videoConverter;
        private readonly IVideoTracker _videoTracker;
        private readonly string _settingsPath;
        private readonly string _downloadsPath;
        private readonly Dictionary<string, DownloadTask> _downloads;
        private AppSettings _settings;

        public AppService(string dataDirectory)
        {
            _networkMonitor = new NetworkMonitor();
            _bandwidthPredictor = new BandwidthPredictor();
            _videoConverter = new VideoConverter();
            _videoTracker = new VideoTracker();
            _downloads = new Dictionary<string, DownloadTask>();
            _settingsPath = Path.Combine(dataDirectory, "settings.json");
            _downloadsPath = Path.Combine(dataDirectory, "downloads.json");
            _settings = AppSettings.Default;
        }

        public async Task InitializeAsync()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath));
            Directory.CreateDirectory(Path.GetDirectoryName(_downloadsPath));

            await LoadSettingsAsync();
            await LoadDownloadsAsync();
        }

        public async Task<List<DownloadTask>> GetDownloadsAsync()
        {
            return _downloads.Values.ToList();
        }

        public async Task<DownloadTask> GetDownloadAsync(string taskId)
        {
            return _downloads.TryGetValue(taskId, out var task) ? task : null;
        }

        public async Task<bool> StartDownloadAsync(DownloadTask task, SchedulePreference preference)
        {
            if (_downloads.ContainsKey(task.Id))
            {
                return false;
            }

            _downloads[task.Id] = task;
            await SaveDownloadsAsync();

            var buffer = new PooledBuffer(8192); // 8KB buffer
            _ = task.StartAsync(buffer, preference); // Start asynchronously

            return true;
        }

        public async Task<bool> PauseDownloadAsync(string taskId)
        {
            if (_downloads.TryGetValue(taskId, out var task))
            {
                task.Pause();
                await SaveDownloadsAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> ResumeDownloadAsync(string taskId)
        {
            if (_downloads.TryGetValue(taskId, out var task))
            {
                task.Resume();
                await SaveDownloadsAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> CancelDownloadAsync(string taskId)
        {
            if (_downloads.TryGetValue(taskId, out var task))
            {
                task.Cancel();
                await SaveDownloadsAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteDownloadAsync(string taskId, bool deleteFile)
        {
            if (_downloads.TryGetValue(taskId, out var task))
            {
                task.Cancel();
                _downloads.Remove(taskId);

                if (deleteFile && File.Exists(task.DestinationPath))
                {
                    try
                    {
                        File.Delete(task.DestinationPath);
                    }
                    catch
                    {
                        // Log but continue
                    }
                }

                await SaveDownloadsAsync();
                return true;
            }
            return false;
        }

        public async Task<NetworkUsage> GetNetworkUsageAsync()
        {
            return _networkMonitor.GetCurrentUsage();
        }

        public async Task<BandwidthPrediction> GetBandwidthPredictionAsync(DateTime targetTime)
        {
            return _bandwidthPredictor.PredictBandwidth(targetTime);
        }

        public async Task<List<BandwidthPrediction>> GetHourlyPredictionsAsync(DateTime start, int hours)
        {
            var predictions = new List<BandwidthPrediction>();
            for (int i = 0; i < hours; i++)
            {
                var time = start.AddHours(i);
                predictions.Add(_bandwidthPredictor.PredictBandwidth(time));
            }
            return predictions;
        }

        public async Task<bool> UpdateSettingsAsync(AppSettings settings)
        {
            _settings = settings;
            await SaveSettingsAsync();
            return true;
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            return _settings;
        }

        public async Task<bool> ImportDownloadsAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var downloads = JsonConvert.DeserializeObject<List<DownloadTask>>(json);
                foreach (var download in downloads)
                {
                    if (!_downloads.ContainsKey(download.Id))
                    {
                        _downloads[download.Id] = download;
                    }
                }
                await SaveDownloadsAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExportDownloadsAsync(string filePath)
        {
            try
            {
                var downloads = _downloads.Values.ToList();
                var json = JsonConvert.SerializeObject(downloads, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            // TODO: Implement update checking
            return false;
        }

        public async Task<bool> InstallUpdateAsync()
        {
            // TODO: Implement update installation
            return false;
        }

        public async Task ShutdownAsync()
        {
            await SaveSettingsAsync();
            await SaveDownloadsAsync();
            _networkMonitor.Dispose();
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = await File.ReadAllTextAsync(_settingsPath);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? AppSettings.Default;
                }
            }
            catch
            {
                _settings = AppSettings.Default;
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                await File.WriteAllTextAsync(_settingsPath, json);
            }
            catch
            {
                // Log but continue
            }
        }

        private async Task LoadDownloadsAsync()
        {
            try
            {
                if (File.Exists(_downloadsPath))
                {
                    var json = await File.ReadAllTextAsync(_downloadsPath);
                    var downloads = JsonConvert.DeserializeObject<List<DownloadTask>>(json);
                    foreach (var download in downloads)
                    {
                        _downloads[download.Id] = download;
                    }
                }
            }
            catch
            {
                // Log but continue
            }
        }

        private async Task SaveDownloadsAsync()
        {
            try
            {
                var downloads = _downloads.Values.ToList();
                var json = JsonConvert.SerializeObject(downloads, Formatting.Indented);
                await File.WriteAllTextAsync(_downloadsPath, json);
            }
            catch
            {
                // Log but continue
            }
        }
    }
}
