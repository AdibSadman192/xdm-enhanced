using FFMpegCore.Enums;

namespace XDM.Core.Media
{
    internal class OptimalSettings
    {
        public Codec VideoCodec { get; set; }
        public Codec AudioCodec { get; set; }
        public int VideoBitrate { get; set; }
        public int AudioBitrate { get; set; }
        public HardwareAccelerator? Accelerator { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
