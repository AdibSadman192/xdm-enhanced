using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TraceLog;
using XDM.Core.Collections;
using XDM.Core.Downloader;
using XDM.Core.Downloader.Adaptive.Dash;
using XDM.Core.Downloader.Adaptive.Hls;
using XDM.Core.Downloader.Progressive.DualHttp;
using XDM.Core.Downloader.Progressive.SingleHttp;
using XDM.Core.MediaProcessor;
using XDM.Core.Util;

namespace XDM.Core.BrowserMonitoring
{
    public class VideoTracker : IVideoTracker
    {
        private GenericOrderedDictionary<string, KeyValuePair<DualSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>> ytVideoList = new();
        private GenericOrderedDictionary<string, KeyValuePair<SingleSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>> videoList = new();
        private GenericOrderedDictionary<string, KeyValuePair<MultiSourceHLSDownloadInfo, StreamingVideoDisplayInfo>> hlsVideoList = new();
        private GenericOrderedDictionary<string, KeyValuePair<MultiSourceDASHDownloadInfo, StreamingVideoDisplayInfo>> dashVideoList = new();

        public event EventHandler<MediaInfoEventArgs> MediaAdded;
        public event EventHandler<MediaInfoEventArgs> MediaUpdated;

        public void ClearVideoList()
        {
            ytVideoList.Clear();
            hlsVideoList.Clear();
            dashVideoList.Clear();
            videoList.Clear();
            ApplicationContext.BroadcastConfigChange();
        }

        public void AddOrUpdateYtVideo(string url, DualSourceHTTPDownloadInfo info, StreamingVideoDisplayInfo displayInfo)
        {
            if (ytVideoList.ContainsKey(url))
            {
                ytVideoList[url] = new KeyValuePair<DualSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo);
                OnMediaUpdated(new MediaInfoEventArgs(url, info, displayInfo));
            }
            else
            {
                ytVideoList.Add(url, new KeyValuePair<DualSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo));
                OnMediaAdded(new MediaInfoEventArgs(url, info, displayInfo));
            }
            ApplicationContext.BroadcastConfigChange();
        }

        public void AddOrUpdateVideo(string url, SingleSourceHTTPDownloadInfo info, StreamingVideoDisplayInfo displayInfo)
        {
            if (videoList.ContainsKey(url))
            {
                videoList[url] = new KeyValuePair<SingleSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo);
                OnMediaUpdated(new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(url, info.File, displayInfo.Title, DateTime.Now, displayInfo.TabId)
                });
            }
            else
            {
                videoList.Add(url, new KeyValuePair<SingleSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo));
                OnMediaAdded(new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(url, info.File, displayInfo.Title, DateTime.Now, displayInfo.TabId)
                });
            }
            ApplicationContext.BroadcastConfigChange();
        }

        public void AddOrUpdateHlsVideo(string url, MultiSourceHLSDownloadInfo info, StreamingVideoDisplayInfo displayInfo)
        {
            if (hlsVideoList.ContainsKey(url))
            {
                hlsVideoList[url] = new KeyValuePair<MultiSourceHLSDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo);
                OnMediaUpdated(new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(url, info.File, displayInfo.Title, DateTime.Now, displayInfo.TabId)
                });
            }
            else
            {
                hlsVideoList.Add(url, new KeyValuePair<MultiSourceHLSDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo));
                OnMediaAdded(new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(url, info.File, displayInfo.Title, DateTime.Now, displayInfo.TabId)
                });
            }
            ApplicationContext.BroadcastConfigChange();
        }

        public void AddOrUpdateDashVideo(string url, MultiSourceDASHDownloadInfo info, StreamingVideoDisplayInfo displayInfo)
        {
            if (dashVideoList.ContainsKey(url))
            {
                dashVideoList[url] = new KeyValuePair<MultiSourceDASHDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo);
                OnMediaUpdated(new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(url, info.File, displayInfo.Title, DateTime.Now, displayInfo.TabId)
                });
            }
            else
            {
                dashVideoList.Add(url, new KeyValuePair<MultiSourceDASHDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo));
                OnMediaAdded(new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(url, info.File, displayInfo.Title, DateTime.Now, displayInfo.TabId)
                });
            }
            ApplicationContext.BroadcastConfigChange();
        }

        private string GenerateUpdatedFileName(string oldFile, string newName)
        {
            var ext = Path.GetExtension(oldFile);
            var file = FileHelper.SanitizeFileName(newName);
            if (!string.IsNullOrEmpty(ext))
            {
                file += ext;
            }
            return file!;
        }

        public void UpdateMediaTitle(string tabUrl, string tabTitle)
        {
            foreach (var e in ytVideoList)
            {
                var u = e.Value.Value.TabUrl;
                if (string.IsNullOrEmpty(u))
                {
                    continue;
                }
                if (u == tabUrl)
                {
                    e.Value.Key.File = GenerateUpdatedFileName(e.Value.Key.File, tabTitle);
                    this.MediaUpdated?.Invoke(
                        this,
                        new MediaInfoEventArgs
                        {
                            MediaInfo = new MediaInfo(e.Key, e.Value.Key.File,
                            e.Value.Value.DescriptionText, e.Value.Value.CreationTime,
                            e.Value.Value.TabId)
                        });
                }
            }

            foreach (var e in videoList)
            {
                var u = e.Value.Value.TabUrl;
                if (string.IsNullOrEmpty(u))
                {
                    continue;
                }
                if (u == tabUrl)
                {
                    e.Value.Key.File = GenerateUpdatedFileName(e.Value.Key.File, tabTitle);
                    this.MediaUpdated?.Invoke(
                        this,
                        new MediaInfoEventArgs
                        {
                            MediaInfo = new MediaInfo(e.Key, e.Value.Key.File, e.Value.Value.DescriptionText,
                            e.Value.Value.CreationTime, e.Value.Value.TabId)
                        });
                }
            }
        }

        public bool IsFFmpegRequiredForDownload(string id)
        {
            return ytVideoList.ContainsKey(id) || dashVideoList.ContainsKey(id) || hlsVideoList.ContainsKey(id);
        }

        public void StartVideoDownload(string videoId,
            string name,
            string? folder,
            bool startImmediately,
            AuthenticationInfo? authentication,
            ProxyInfo? proxyInfo,
            int maxSpeedLimit,
            string? queueId,
            bool convertToMp3 = false //only applicable for dual source http downloads
            )
        {
            //IBaseDownloader downloader = null;
            if (ytVideoList.ContainsKey(videoId))
            {
                ApplicationContext.CoreService.StartDownload(ytVideoList[videoId].Key, name, FileNameFetchMode.ExtensionOnly,
                        folder, startImmediately, authentication, proxyInfo, queueId, false);
            }
            else if (videoList.ContainsKey(videoId))
            {
                ApplicationContext.CoreService.StartDownload(videoList[videoId].Key, name, convertToMp3 ? FileNameFetchMode.None : FileNameFetchMode.ExtensionOnly,
                    folder, startImmediately, authentication, proxyInfo, queueId, convertToMp3);
            }
            else if (hlsVideoList.ContainsKey(videoId))
            {
                ApplicationContext.CoreService.StartDownload(hlsVideoList[videoId].Key, name, FileNameFetchMode.ExtensionOnly,
                    folder, startImmediately, authentication, proxyInfo, queueId, false);
            }
            else if (dashVideoList.ContainsKey(videoId))
            {
                ApplicationContext.CoreService.StartDownload(dashVideoList[videoId].Key, name, FileNameFetchMode.ExtensionOnly,
                    folder, startImmediately, authentication, proxyInfo, queueId, false);
            }
        }

        public List<MediaInfo> GetVideoList()
        {
            lock (this)
            {
                var list = new List<MediaInfo>();
                foreach (var e in ytVideoList)
                {
                    list.Add(new MediaInfo(e.Key, e.Value.Key.File, e.Value.Value.DescriptionText,
                        e.Value.Value.CreationTime, e.Value.Value.TabId));
                }
                foreach (var e in videoList)
                {
                    list.Add(new MediaInfo(e.Key, e.Value.Key.File, e.Value.Value.DescriptionText,
                        e.Value.Value.CreationTime, e.Value.Value.TabId));
                }
                foreach (var e in hlsVideoList)
                {
                    list.Add(new MediaInfo(e.Key, e.Value.Key.File, e.Value.Value.DescriptionText,
                        e.Value.Value.CreationTime, e.Value.Value.TabId));
                }
                foreach (var e in dashVideoList)
                {
                    list.Add(new MediaInfo(e.Key, e.Value.Key.File, e.Value.Value.DescriptionText,
                        e.Value.Value.CreationTime, e.Value.Value.TabId));
                }
                list.Sort((a, b) => a.DateAdded.CompareTo(b.DateAdded));
                return list;
            }
        }

        public void AddVideoNotifications(IEnumerable<KeyValuePair<DualSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>> notifications)
        {
            lock (this)
            {
                foreach (var info in notifications)
                {
                    var id = Guid.NewGuid().ToString();
                    ytVideoList.Add(id, info);
                    Log.Debug("Video url1: " + info.Key.Uri1);
                    Log.Debug("Video url2: " + info.Key.Uri2);
                    this.MediaAdded?.Invoke(this, new MediaInfoEventArgs
                    {
                        MediaInfo = new MediaInfo(id, info.Key.File, info.Value.DescriptionText,
                        DateTime.Now, info.Value.TabId)
                    });
                }
                ApplicationContext.PlatformUIService.ShowMediaNotification();
                ApplicationContext.BroadcastConfigChange();
            }
        }

        public void AddVideoNotifications(IEnumerable<KeyValuePair<SingleSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>> notifications)
        {
            lock (this)
            {
                foreach (var info in notifications)
                {
                    var id = Guid.NewGuid().ToString();
                    videoList.Add(id, info);
                    Log.Debug("Video url1: " + info.Key.Uri);
                    this.MediaAdded?.Invoke(this, new MediaInfoEventArgs
                    {
                        MediaInfo = new MediaInfo(id, info.Key.File, info.Value.DescriptionText,
                        DateTime.Now, info.Value.TabId)
                    });
                }
                ApplicationContext.PlatformUIService.ShowMediaNotification();
                ApplicationContext.BroadcastConfigChange();
            }
        }

        public void AddVideoNotifications(IEnumerable<KeyValuePair<MultiSourceHLSDownloadInfo, StreamingVideoDisplayInfo>> notifications)
        {
            lock (this)
            {
                foreach (var info in notifications)
                {
                    var id = Guid.NewGuid().ToString();
                    hlsVideoList.Add(id, info);
                    Log.Debug("Video url1: " + info.Key.VideoUri);
                    Log.Debug("Video url2: " + info.Key.AudioUri);
                    this.MediaAdded?.Invoke(this, new MediaInfoEventArgs
                    {
                        MediaInfo = new MediaInfo(id, info.Key.File, info.Value.DescriptionText,
                        DateTime.Now, info.Value.TabId)
                    });
                }
                ApplicationContext.PlatformUIService.ShowMediaNotification();
                ApplicationContext.BroadcastConfigChange();
            }
        }

        public void AddVideoNotifications(IEnumerable<KeyValuePair<MultiSourceDASHDownloadInfo, StreamingVideoDisplayInfo>> notifications)
        {
            lock (this)
            {
                foreach (var info in notifications)
                {
                    var id = Guid.NewGuid().ToString();
                    dashVideoList.Add(id, info);
                    Log.Debug("Video url1: " + info.Key.Url);
                    this.MediaAdded?.Invoke(this, new MediaInfoEventArgs
                    {
                        MediaInfo = new MediaInfo(id, info.Key.File, info.Value.DescriptionText,
                        DateTime.Now, info.Value.TabId)
                    });
                }
                ApplicationContext.PlatformUIService.ShowMediaNotification();
                ApplicationContext.BroadcastConfigChange();
            }
        }

        public void AddVideoNotification(StreamingVideoDisplayInfo displayInfo, DualSourceHTTPDownloadInfo info)
        {
            lock (this)
            {
                var id = Guid.NewGuid().ToString();
                ytVideoList.Add(id, new KeyValuePair<DualSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo));
                Log.Debug("Video url1: " + info.Uri1);
                Log.Debug("Video url2: " + info.Uri2);
                this.MediaAdded?.Invoke(this, new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(id, info.File, displayInfo.DescriptionText,
                    DateTime.Now, displayInfo.TabId)
                });
                ApplicationContext.PlatformUIService.ShowMediaNotification();
                ApplicationContext.BroadcastConfigChange();
            }
        }

        public void AddVideoNotification(StreamingVideoDisplayInfo displayInfo, SingleSourceHTTPDownloadInfo info)
        {
            lock (this)
            {
                var id = Guid.NewGuid().ToString();
                videoList.Add(id, new KeyValuePair<SingleSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo));
                Log.Debug("Video url1: " + info.Uri);
                this.MediaAdded?.Invoke(this, new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(id, info.File, displayInfo.DescriptionText,
                    DateTime.Now, displayInfo.TabId)
                });
                ApplicationContext.PlatformUIService.ShowMediaNotification();
                ApplicationContext.BroadcastConfigChange();
            }
        }

        public void AddVideoNotification(StreamingVideoDisplayInfo displayInfo, MultiSourceHLSDownloadInfo info)
        {
            lock (this)
            {
                var id = Guid.NewGuid().ToString();
                hlsVideoList.Add(id, new KeyValuePair<MultiSourceHLSDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo));
                Log.Debug("Video url1: " + info.VideoUri);
                Log.Debug("Video url2: " + info.AudioUri);
                this.MediaAdded?.Invoke(this, new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(id, info.File, displayInfo.DescriptionText,
                    DateTime.Now, displayInfo.TabId)
                });
                ApplicationContext.PlatformUIService.ShowMediaNotification();
                ApplicationContext.BroadcastConfigChange();
            }
        }

        public void AddVideoNotification(StreamingVideoDisplayInfo displayInfo, MultiSourceDASHDownloadInfo info)
        {
            lock (this)
            {
                var id = Guid.NewGuid().ToString();
                Log.Debug("DASH video added with id: " + id);
                dashVideoList.Add(id, new KeyValuePair<MultiSourceDASHDownloadInfo, StreamingVideoDisplayInfo>(info, displayInfo));
                Log.Debug("Video url1: " + info.Url);
                this.MediaAdded?.Invoke(this, new MediaInfoEventArgs
                {
                    MediaInfo = new MediaInfo(id, info.File, displayInfo.DescriptionText,
                    DateTime.Now, displayInfo.TabId)
                });
                ApplicationContext.PlatformUIService.ShowMediaNotification();
                ApplicationContext.BroadcastConfigChange();
            }
        }

        public void AddVideoDownload(string videoId)
        {
            var name = string.Empty;
            var size = 0L;
            var contentType = string.Empty;
            var valid = false;
            if (ytVideoList.ContainsKey(videoId))
            {
                if (ApplicationContext.LinkRefresher.LinkAccepted(ytVideoList[videoId].Key)) return;
                name = ytVideoList[videoId].Key.File;
                size = ytVideoList[videoId].Value.Size;
                contentType = ytVideoList[videoId].Key.ContentType1;
                valid = true;
            }
            else if (videoList.ContainsKey(videoId))
            {
                if (ApplicationContext.LinkRefresher.LinkAccepted(videoList[videoId].Key)) return;
                name = videoList[videoId].Key.File;
                size = videoList[videoId].Value.Size;
                contentType = videoList[videoId].Key.ContentType;
                valid = true;
            }
            else if (hlsVideoList.ContainsKey(videoId))
            {
                Log.Debug("Download HLS video added with id: " + videoId);
                name = hlsVideoList[videoId].Key.File;
                valid = true;
                try
                {
                    contentType = hlsVideoList[videoId].Key.ContentType;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, ex.Message);
                }
            }
            else if (dashVideoList.ContainsKey(videoId))
            {
                Log.Debug("Download DASH video added with id: " + videoId);
                name = dashVideoList[videoId].Key.File;
                contentType = dashVideoList[videoId].Key.ContentType;
                valid = true;
            }
            if (valid)
            {
                if (Config.Instance.StartDownloadAutomatically && IsFFmpegOK(videoId))
                {
                    StartVideoDownload(
                        videoId, FileHelper.SanitizeFileName(name),
                        null, true, null, Config.Instance.Proxy,
                    Helpers.GetSpeedLimit(), null);
                }
                else
                {
                    ApplicationContext.Application.ShowVideoDownloadDialog(videoId, name, size, contentType);
                }
            }
        }

        public void RemoveVideo(string url)
        {
            if (ytVideoList.ContainsKey(url))
            {
                ytVideoList.Remove(url);
            }
            else if (videoList.ContainsKey(url))
            {
                videoList.Remove(url);
            }
            else if (hlsVideoList.ContainsKey(url))
            {
                hlsVideoList.Remove(url);
            }
            else if (dashVideoList.ContainsKey(url))
            {
                dashVideoList.Remove(url);
            }
            ApplicationContext.BroadcastConfigChange();
        }

        public void RemoveVideoByTabUrl(string tabUrl)
        {
            var keysToRemove = new List<string>();

            foreach (var entry in ytVideoList)
            {
                if (entry.Value.Value.TabUrl == tabUrl)
                {
                    keysToRemove.Add(entry.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                ytVideoList.Remove(key);
            }

            keysToRemove.Clear();
            foreach (var entry in videoList)
            {
                if (entry.Value.Value.TabUrl == tabUrl)
                {
                    keysToRemove.Add(entry.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                videoList.Remove(key);
            }

            keysToRemove.Clear();
            foreach (var entry in hlsVideoList)
            {
                if (entry.Value.Value.TabUrl == tabUrl)
                {
                    keysToRemove.Add(entry.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                hlsVideoList.Remove(key);
            }

            keysToRemove.Clear();
            foreach (var entry in dashVideoList)
            {
                if (entry.Value.Value.TabUrl == tabUrl)
                {
                    keysToRemove.Add(entry.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                dashVideoList.Remove(key);
            }

            ApplicationContext.BroadcastConfigChange();
        }

        public static bool IsFFmpegOK(string id)
        {
            if (!ApplicationContext.VideoTracker.IsFFmpegRequiredForDownload(id)) return true;
            return FFmpegMediaProcessor.IsFFmpegInstalled();
        }

        private void OnMediaAdded(MediaInfoEventArgs e)
        {
            MediaAdded?.Invoke(this, e);
        }

        private void OnMediaUpdated(MediaInfoEventArgs e)
        {
            MediaUpdated?.Invoke(this, e);
        }
    }

    public class MediaInfo
    {
        public MediaInfo(string id, string name, string description,
            DateTime date, string tabId)
        {
            this.ID = id;
            this.Name = name;
            this.Description = description;
            this.DateAdded = date;
            this.TabId = tabId;
        }

        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public string TabId { get; set; }
    }

    public class MediaInfoEventArgs : EventArgs
    {
        public MediaInfoEventArgs()
        {
        }

        public MediaInfoEventArgs(string id, DualSourceHTTPDownloadInfo info, StreamingVideoDisplayInfo displayInfo)
        {
            MediaInfo = new MediaInfo(id, info.File, displayInfo.Title, DateTime.Now, displayInfo.TabId);
        }

        public MediaInfo MediaInfo { get; set; }
    }
}
