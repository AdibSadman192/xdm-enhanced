using System;

namespace XDM.Core.Download
{
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
}
