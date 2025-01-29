using System;

namespace XDM.Core.Media
{
    public class VideoConversionException : Exception
    {
        public VideoConversionException(string message) : base(message)
        {
        }

        public VideoConversionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
