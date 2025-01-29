using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

namespace XDM.Core.Streaming
{
    /// <summary>
    /// Extracts video streams from various streaming platforms
    /// </summary>
    public class VideoStreamExtractor
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, IPlatformExtractor> _extractors;

        public VideoStreamExtractor()
        {
            _httpClient = new HttpClient();
            _extractors = new Dictionary<string, IPlatformExtractor>
            {
                { "youtube", new YouTubeExtractor() },
                { "vimeo", new VimeoExtractor() },
                { "dailymotion", new DailyMotionExtractor() }
            };
        }

        public async Task<VideoInfo> ExtractVideoInfoAsync(string url)
        {
            var platform = DetectPlatform(url);
            if (_extractors.TryGetValue(platform, out var extractor))
            {
                return await extractor.ExtractAsync(url, _httpClient);
            }

            throw new NotSupportedException($"Platform {platform} is not supported");
        }

        private string DetectPlatform(string url)
        {
            if (url.Contains("youtube.com") || url.Contains("youtu.be")) return "youtube";
            if (url.Contains("vimeo.com")) return "vimeo";
            if (url.Contains("dailymotion.com")) return "dailymotion";
            throw new NotSupportedException("Unsupported platform");
        }
    }

    public interface IPlatformExtractor
    {
        Task<VideoInfo> ExtractAsync(string url, HttpClient client);
    }

    public class YouTubeExtractor : IPlatformExtractor
    {
        public async Task<VideoInfo> ExtractAsync(string url, HttpClient client)
        {
            // Extract video ID
            var videoId = ExtractVideoId(url);
            var playerResponse = await GetPlayerResponse(videoId, client);

            var formats = playerResponse
                .GetProperty("streamingData")
                .GetProperty("formats")
                .EnumerateArray()
                .Select(format => new VideoFormat
                {
                    Url = format.GetProperty("url").GetString(),
                    Quality = format.GetProperty("qualityLabel").GetString(),
                    MimeType = format.GetProperty("mimeType").GetString()
                })
                .ToList();

            return new VideoInfo
            {
                Title = playerResponse.GetProperty("videoDetails").GetProperty("title").GetString(),
                Formats = formats
            };
        }

        private string ExtractVideoId(string url)
        {
            var regex = new Regex(@"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})");
            var match = regex.Match(url);
            return match.Success ? match.Groups[1].Value : throw new ArgumentException("Invalid YouTube URL");
        }

        private async Task<JsonElement> GetPlayerResponse(string videoId, HttpClient client)
        {
            var response = await client.GetStringAsync($"https://www.youtube.com/youtubei/v1/player?key=YOUR_API_KEY&videoId={videoId}");
            return JsonDocument.Parse(response).RootElement;
        }
    }

    public class VimeoExtractor : IPlatformExtractor
    {
        public async Task<VideoInfo> ExtractAsync(string url, HttpClient client)
        {
            var videoId = ExtractVideoId(url);
            var config = await GetVideoConfig(videoId, client);

            var formats = config
                .GetProperty("request")
                .GetProperty("files")
                .GetProperty("progressive")
                .EnumerateArray()
                .Select(format => new VideoFormat
                {
                    Url = format.GetProperty("url").GetString(),
                    Quality = format.GetProperty("quality").GetString(),
                    MimeType = "video/mp4"
                })
                .ToList();

            return new VideoInfo
            {
                Title = config.GetProperty("video").GetProperty("title").GetString(),
                Formats = formats
            };
        }

        private string ExtractVideoId(string url)
        {
            var regex = new Regex(@"vimeo\.com/(?:.*#|.*/videos/)?([0-9]+)");
            var match = regex.Match(url);
            return match.Success ? match.Groups[1].Value : throw new ArgumentException("Invalid Vimeo URL");
        }

        private async Task<JsonElement> GetVideoConfig(string videoId, HttpClient client)
        {
            var response = await client.GetStringAsync($"https://player.vimeo.com/video/{videoId}/config");
            return JsonDocument.Parse(response).RootElement;
        }
    }

    public class DailyMotionExtractor : IPlatformExtractor
    {
        public async Task<VideoInfo> ExtractAsync(string url, HttpClient client)
        {
            var videoId = ExtractVideoId(url);
            var metadata = await GetVideoMetadata(videoId, client);

            var formats = metadata
                .GetProperty("qualities")
                .EnumerateObject()
                .SelectMany(quality => quality.Value.EnumerateArray()
                    .Select(format => new VideoFormat
                    {
                        Url = format.GetProperty("url").GetString(),
                        Quality = quality.Name,
                        MimeType = format.GetProperty("type").GetString()
                    }))
                .ToList();

            return new VideoInfo
            {
                Title = metadata.GetProperty("title").GetString(),
                Formats = formats
            };
        }

        private string ExtractVideoId(string url)
        {
            var regex = new Regex(@"dailymotion\.com/(?:video|embed/video)/([a-zA-Z0-9]+)");
            var match = regex.Match(url);
            return match.Success ? match.Groups[1].Value : throw new ArgumentException("Invalid Dailymotion URL");
        }

        private async Task<JsonElement> GetVideoMetadata(string videoId, HttpClient client)
        {
            var response = await client.GetStringAsync($"https://api.dailymotion.com/video/{videoId}?fields=title,qualities");
            return JsonDocument.Parse(response).RootElement;
        }
    }

    public class VideoInfo
    {
        public string Title { get; set; }
        public List<VideoFormat> Formats { get; set; }
    }

    public class VideoFormat
    {
        public string Url { get; set; }
        public string Quality { get; set; }
        public string MimeType { get; set; }
    }
}
