using System;

namespace XDM.Core.UI
{
    public interface IClipboardMonitor
    {
        event EventHandler<string> UrlDetected;
        void Start();
        void Stop();
    }
}
