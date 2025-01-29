using System;
using System.Threading.Tasks;
using XDM.Core.Download;

namespace XDM.Core.Interfaces
{
    public interface IMainView
    {
        Task ShowAsync();
        Task HideAsync();
        Task UpdateDownloadProgressAsync(string taskId, double progress, double speed, TimeSpan remaining);
        Task ShowNotificationAsync(string title, string message);
        Task ShowErrorAsync(string title, string message);
        Task<bool> ConfirmAsync(string title, string message);
        Task RefreshDownloadListAsync();
        Task UpdateNetworkStatusAsync(NetworkStatus status);
        Task UpdateSettingsAsync(AppSettings settings);
    }

    public class NetworkStatus
    {
        public double CurrentSpeed { get; set; }
        public double AverageSpeed { get; set; }
        public string NetworkType { get; set; }
        public int? SignalStrength { get; set; }
        public int ActiveDownloads { get; set; }
        public int QueuedDownloads { get; set; }
    }
}
