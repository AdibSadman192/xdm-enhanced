namespace XDM.Core.BrowserMonitoring
{
    public class StreamingVideoDisplayInfo
    {
        public string Title { get; set; }
        public string TabUrl { get; set; }
        public string TabTitle { get; set; }
        public string VideoUrl { get; set; }
        public string Resolution { get; set; }
        public string Format { get; set; }
        public long Size { get; set; }
        public string Duration { get; set; }
        public bool IsLive { get; set; }
    }
}
