using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XDM.Core.Download;
using XDM.Core.Interfaces;
using XDM.Core.Network;
using XDM.Core.Util;

namespace XDM.Core
{
    public class AppController : IAppController
    {
        private readonly IMainView _view;
        private readonly IAppService _service;
        private readonly NetworkMonitor _networkMonitor;
        private readonly BandwidthPredictor _bandwidthPredictor;

        public AppController(IMainView view, IAppService service)
        {
            _view = view;
            _service = service;
            _networkMonitor = new NetworkMonitor();
            _bandwidthPredictor = new BandwidthPredictor();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _service.InitializeAsync();
                await _view.ShowAsync();
                await UpdateNetworkStatusAsync();
            }
            catch (Exception ex)
            {
                await _view.ShowErrorAsync("Initialization Error", ex.Message);
            }
        }

        public async Task StartDownloadAsync(DownloadTask task, SchedulePreference preference)
        {
            try
            {
                if (await _service.StartDownloadAsync(task, preference))
                {
                    await _view.RefreshDownloadListAsync();
                }
            }
            catch (Exception ex)
            {
                await _view.ShowErrorAsync("Download Error", ex.Message);
            }
        }

        public async Task PauseDownloadAsync(string taskId)
        {
            try
            {
                if (await _service.PauseDownloadAsync(taskId))
                {
                    await _view.RefreshDownloadListAsync();
                }
            }
            catch (Exception ex)
            {
                await _view.ShowErrorAsync("Pause Error", ex.Message);
            }
        }

        public async Task ResumeDownloadAsync(string taskId)
        {
            try
            {
                if (await _service.ResumeDownloadAsync(taskId))
                {
                    await _view.RefreshDownloadListAsync();
                }
            }
            catch (Exception ex)
            {
                await _view.ShowErrorAsync("Resume Error", ex.Message);
            }
        }

        public async Task CancelDownloadAsync(string taskId)
        {
            try
            {
                if (await _service.CancelDownloadAsync(taskId))
                {
                    await _view.RefreshDownloadListAsync();
                }
            }
            catch (Exception ex)
            {
                await _view.ShowErrorAsync("Cancel Error", ex.Message);
            }
        }

        public async Task<NetworkUsage> GetNetworkUsageAsync()
        {
            return await _networkMonitor.GetCurrentUsageAsync();
        }

        public async Task<BandwidthPrediction> GetBandwidthPredictionAsync(DateTime targetTime)
        {
            return await _bandwidthPredictor.PredictBandwidthAsync(targetTime);
        }

        public async Task UpdateSettingsAsync(AppSettings settings)
        {
            try
            {
                await _service.UpdateSettingsAsync(settings);
                await _view.RefreshSettingsAsync();
            }
            catch (Exception ex)
            {
                await _view.ShowErrorAsync("Settings Error", ex.Message);
            }
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            return await _service.GetSettingsAsync();
        }

        public async Task ShutdownAsync()
        {
            try
            {
                await _service.ShutdownAsync();
            }
            catch (Exception ex)
            {
                await _view.ShowErrorAsync("Shutdown Error", ex.Message);
            }
        }

        private async Task UpdateNetworkStatusAsync()
        {
            try
            {
                var usage = await GetNetworkUsageAsync();
                await _view.UpdateNetworkStatusAsync(usage);
            }
            catch (Exception ex)
            {
                await _view.ShowErrorAsync("Network Status Error", ex.Message);
            }
        }
    }
}
