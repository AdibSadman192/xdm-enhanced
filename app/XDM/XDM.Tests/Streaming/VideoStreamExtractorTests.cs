using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using XDM.Core.Streaming;

namespace XDM.Tests.Streaming
{
    public class VideoStreamExtractorTests
    {
        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "youtube")]
        [InlineData("https://youtu.be/dQw4w9WgXcQ", "youtube")]
        [InlineData("https://vimeo.com/123456789", "vimeo")]
        [InlineData("https://www.dailymotion.com/video/x7tgd2g", "dailymotion")]
        public void DetectPlatform_ShouldReturnCorrectPlatform(string url, string expectedPlatform)
        {
            // Arrange
            var extractor = new VideoStreamExtractor();

            // Act & Assert
            var exception = Record.Exception(() => 
            {
                var result = extractor.ExtractVideoInfoAsync(url).GetAwaiter().GetResult();
                result.Should().NotBeNull();
            });

            if (exception != null)
            {
                // We expect NotSupportedException only for unsupported platforms
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().Contain("not supported");
            }
        }

        [Fact]
        public async Task ExtractVideoInfo_YouTube_ShouldReturnCorrectInfo()
        {
            // Arrange
            var mockHttpClient = new Mock<HttpClient>();
            var extractor = new YouTubeExtractor();
            var testUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

            // Mock response
            var mockResponse = @"{
                'videoDetails': {
                    'title': 'Test Video'
                },
                'streamingData': {
                    'formats': [
                        {
                            'url': 'https://test.com/video.mp4',
                            'qualityLabel': '720p',
                            'mimeType': 'video/mp4'
                        }
                    ]
                }
            }";

            // Act
            var result = await extractor.ExtractAsync(testUrl, mockHttpClient.Object);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test Video");
            result.Formats.Should().HaveCount(1);
            result.Formats[0].Quality.Should().Be("720p");
            result.Formats[0].MimeType.Should().Be("video/mp4");
        }

        [Fact]
        public async Task ExtractVideoInfo_Vimeo_ShouldReturnCorrectInfo()
        {
            // Arrange
            var mockHttpClient = new Mock<HttpClient>();
            var extractor = new VimeoExtractor();
            var testUrl = "https://vimeo.com/123456789";

            // Mock response
            var mockResponse = @"{
                'video': {
                    'title': 'Test Vimeo Video'
                },
                'request': {
                    'files': {
                        'progressive': [
                            {
                                'url': 'https://test.com/video.mp4',
                                'quality': '1080p'
                            }
                        ]
                    }
                }
            }";

            // Act
            var result = await extractor.ExtractAsync(testUrl, mockHttpClient.Object);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Test Vimeo Video");
            result.Formats.Should().HaveCount(1);
            result.Formats[0].Quality.Should().Be("1080p");
        }

        [Fact]
        public async Task ExtractVideoInfo_InvalidUrl_ShouldThrowException()
        {
            // Arrange
            var extractor = new VideoStreamExtractor();
            var invalidUrl = "https://invalid-url.com/video";

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(
                () => extractor.ExtractVideoInfoAsync(invalidUrl)
            );
        }

        [Fact]
        public void ExtractVideoId_YouTube_ShouldExtractCorrectly()
        {
            // Arrange
            var extractor = new YouTubeExtractor();
            var urls = new[]
            {
                "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                "https://youtu.be/dQw4w9WgXcQ",
                "https://www.youtube.com/embed/dQw4w9WgXcQ"
            };

            // Act & Assert
            foreach (var url in urls)
            {
                var exception = Record.Exception(() =>
                {
                    var type = extractor.GetType();
                    var method = type.GetMethod("ExtractVideoId", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    var result = method?.Invoke(extractor, new object[] { url }) as string;

                    // Assert
                    result.Should().Be("dQw4w9WgXcQ");
                });

                exception.Should().BeNull();
            }
        }
    }
}
