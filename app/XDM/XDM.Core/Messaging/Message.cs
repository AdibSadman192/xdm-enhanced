using System;

namespace XDM.Messaging
{
    public class Message
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MessageType Type { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public MessagePriority Priority { get; set; }
        public string Source { get; set; }
    }

    public enum MessageType
    {
        Information,
        Warning,
        Error,
        Success,
        Progress,
        Status
    }

    public enum MessagePriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}
