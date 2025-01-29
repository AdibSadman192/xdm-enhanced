using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XDM.Core.Download;
using XDM.Core.Network;

namespace XDM.Core.Interfaces
{
    public interface IAppService
    {
        Task InitializeAsync();
        Task<List<DownloadTask>> GetDownloadsAsync();
        Task<DownloadTask> GetDownloadAsync(string taskId);
        Task<bool> StartDownloadAsync(DownloadTask task, SchedulePreference preference);
        Task<bool> PauseDownloadAsync(string taskId);
        Task<bool> ResumeDownloadAsync(string taskId);
        Task<bool> CancelDownloadAsync(string taskId);
        Task<bool> DeleteDownloadAsync(string taskId, bool deleteFile);
        Task<NetworkUsage> GetNetworkUsageAsync();
        Task<BandwidthPrediction> GetBandwidthPredictionAsync(DateTime targetTime);
        Task<List<BandwidthPrediction>> GetHourlyPredictionsAsync(DateTime start, int hours);
        Task<bool> UpdateSettingsAsync(AppSettings settings);
        Task<AppSettings> GetSettingsAsync();
        Task<bool> ImportDownloadsAsync(string filePath);
        Task<bool> ExportDownloadsAsync(string filePath);
        Task<bool> CheckForUpdatesAsync();
        Task<bool> InstallUpdateAsync();
        Task ShutdownAsync();
    }
}
