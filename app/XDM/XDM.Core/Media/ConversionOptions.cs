namespace XDM.Core.Media
{
    public class ConversionOptions
    {
        public VideoSize TargetSize { get; set; } = VideoSize.HD720p;
        public string HardwareAccelerator { get; set; } = "auto";
        public int Quality { get; set; } = 23; // Lower is better, range 0-51
        public int AudioBitrate { get; set; } = 128; // kbps
        public bool PreserveMetadata { get; set; } = true;
        public bool FastStart { get; set; } = true;
    }
}
