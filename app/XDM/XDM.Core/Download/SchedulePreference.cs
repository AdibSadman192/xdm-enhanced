using System;

namespace XDM.Core.Download
{
    public class SchedulePreference
    {
        public SchedulePriority Priority { get; set; }
        public long MaxBandwidthBytesPerSecond { get; set; }
        public long MinBandwidthBytesPerSecond { get; set; }
        public DateTime? PreferredTime { get; set; }
        public bool AllowReschedule { get; set; }
    }
}
