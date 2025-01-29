using System;
using XDM.Core.Downloader.Adaptive.Dash;
using XDM.Core.Downloader.Adaptive.Hls;
using XDM.Core.Downloader.Progressive.DualHttp;
using XDM.Core.Downloader.Progressive.SingleHttp;

namespace XDM.Core.BrowserMonitoring
{
    public interface IVideoTracker
    {
        event EventHandler<MediaInfoEventArgs> MediaAdded;
        event EventHandler<MediaInfoEventArgs> MediaUpdated;

        void ClearVideoList();
        void UpdateMediaTitle(string tabUrl, string tabTitle);
        void AddOrUpdateYtVideo(string url, DualSourceHTTPDownloadInfo info, StreamingVideoDisplayInfo displayInfo);
        void AddOrUpdateVideo(string url, SingleSourceHTTPDownloadInfo info, StreamingVideoDisplayInfo displayInfo);
        void AddOrUpdateHlsVideo(string url, MultiSourceHLSDownloadInfo info, StreamingVideoDisplayInfo displayInfo);
        void AddOrUpdateDashVideo(string url, MultiSourceDASHDownloadInfo info, StreamingVideoDisplayInfo displayInfo);
        void RemoveVideo(string url);
        void RemoveVideoByTabUrl(string tabUrl);
        void AddVideoDownload(string videoId);
    }
}
