using System;
using XDM.Core;
using XDM.Core.Download;

namespace XDM.Core.Download
{
    public class InProgressDownloadEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Date { get; set; }
        public string Folder { get; set; } = string.Empty;
        public string PrimaryUrl { get; set; } = string.Empty;
        public DownloadStatus Status { get; set; }
        public int Progress { get; set; }
        public string DownloadSpeed { get; set; } = string.Empty;
        public string ETA { get; set; } = string.Empty;
        public string Authentication { get; set; } = string.Empty;
        public string ProxyInfo { get; set; } = string.Empty;
        public int MaxSpeedLimitInKiB { get; set; }

        public InProgressDownloadEntry()
        {
            Id = string.Empty;
            Name = string.Empty;
            Type = string.Empty;
            Folder = string.Empty;
            PrimaryUrl = string.Empty;
            Status = DownloadStatus.Queued;
            DownloadSpeed = string.Empty;
            ETA = string.Empty;
            Authentication = string.Empty;
            ProxyInfo = string.Empty;
        }
    }
}
