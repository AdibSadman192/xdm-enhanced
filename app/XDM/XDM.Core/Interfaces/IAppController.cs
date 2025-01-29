using System;
using System.Threading.Tasks;
using XDM.Core.Download;
using XDM.Core.Network;

namespace XDM.Core.Interfaces
{
    public interface IAppController
    {
        Task InitializeAsync();
        Task StartDownloadAsync(DownloadTask task, SchedulePreference preference);
        Task PauseDownloadAsync(string taskId);
        Task ResumeDownloadAsync(string taskId);
        Task CancelDownloadAsync(string taskId);
        Task<NetworkUsage> GetNetworkUsageAsync();
        Task<BandwidthPrediction> GetBandwidthPredictionAsync(DateTime targetTime);
        Task UpdateSettingsAsync(AppSettings settings);
        Task<AppSettings> GetSettingsAsync();
        Task ShutdownAsync();
    }
}
