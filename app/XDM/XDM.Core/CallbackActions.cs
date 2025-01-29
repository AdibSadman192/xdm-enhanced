using System;
using System.IO;
using TraceLog;
using XDM.Core;
using XDM.Core.UI;

namespace XDM.Core
{
    internal static class CallbackActions
    {
        public static void DownloadStarted(string id)
        {
            var download = ApplicationContext.MainWindow.FindInProgressItem(id);
            if (download == null) return;
            download.Status = DownloadStatus.Downloading;
        }

        public static void DownloadFailed(string id)
        {
            var download = ApplicationContext.MainWindow.FindInProgressItem(id);
            if (download == null) return;
            download.Status = DownloadStatus.Stopped;
        }

        public static void DownloadFinished(string id, long finalFileSize, string filePath, Action callback)
        {
            Log.Debug("Final file name: " + filePath);
            var download = ApplicationContext.MainWindow.FindInProgressItem(id);
            if (download == null) return;
            download.Progress = 100;

            var finishedEntry = new FinishedDownloadItem
            {
                Name = Path.GetFileName(filePath),
                Id = download.Id,
                Date = download.Date,
                Size = download.Size > 0 ? download.Size : finalFileSize,
                Type = download.Type,
                Folder = Path.GetDirectoryName(filePath) ?? string.Empty
            };

            ApplicationContext.MainWindow.AddToTop(finishedEntry);
            ApplicationContext.MainWindow.Delete(download);

            QueueManager.RemoveFinishedDownload(download.Id);

            if (ApplicationContext.CoreService.ActiveDownloadCount == 0 && ApplicationContext.MainWindow.IsInProgressViewSelected)
            {
                Log.Debug("switching to finished listview");
                ApplicationContext.MainWindow.SwitchToFinishedView();
            }

            callback.Invoke();
        }
    }
}
