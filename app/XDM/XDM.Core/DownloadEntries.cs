using Newtonsoft.Json;
using System;
using XDM.Core.Downloader;
using XDM.Core.Download;

namespace XDM.Core
{
    public class DownloadItemBase : IComparable<DownloadItemBase>
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Folder { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;

        public int CompareTo(DownloadItemBase? other)
        {
            if (other == null) return 1;
            return other.Date.CompareTo(this.Date);
        }

        public override string ToString()
        {
            return $"{Name} [{Size}] - {Date}";
        }
    }

    public class InProgressDownloadItem : DownloadItemBase
    {
        public string TargetFileName { get; set; } = string.Empty;
        public string PrimaryUrl { get; set; } = string.Empty;
        public DownloadStatus Status { get; set; }
        public string FileNameFetchMode { get; set; } = string.Empty;
        public string Authentication { get; set; } = string.Empty;
        public string ProxyInfo { get; set; } = string.Empty;
    }

    public class FinishedDownloadItem : DownloadItemBase
    {
    }

    public enum DownloadStatus
    {
        Downloading, Stopped, Finished, Waiting
    }
}