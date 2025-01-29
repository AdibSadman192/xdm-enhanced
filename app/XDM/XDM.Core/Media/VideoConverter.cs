using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using System.IO;
using TraceLog;
using XDM.Core.Media;

namespace XDM.Core.Media
{
    /// <summary>
    /// Handles video conversion with hardware acceleration and optimal settings
    /// </summary>
    public class VideoConverter
    {
        private readonly string _ffmpegPath;

        public VideoConverter(string ffmpegPath)
        {
            _ffmpegPath = ffmpegPath ?? throw new ArgumentNullException(nameof(ffmpegPath));
        }

        public async Task ConvertVideoAsync(string inputFile, string outputFile, ConversionOptions options)
        {
            try
            {
                var settings = GetOptimalSettings(options);
                var ffmpeg = FFMpegArguments.FromFileInput(inputFile);

                ConfigureOutput(ffmpeg, settings, options);

                await ffmpeg.OutputToFile(outputFile).ProcessAsynchronously();
            }
            catch (Exception ex)
            {
                throw new VideoConversionException($"Failed to convert video: {ex.Message}", ex);
            }
        }

        private OptimalSettings GetOptimalSettings(ConversionOptions options)
        {
            var settings = new OptimalSettings
            {
                VideoCodec = Codec.LibX264,
                AudioCodec = Codec.Aac,
                VideoBitrate = 2000,
                AudioBitrate = options.AudioBitrate ?? 128,
                Width = 1280,
                Height = 720
            };

            switch (options.HardwareAccelerator?.ToLower())
            {
                case "nvidia":
                    settings.VideoCodec = Codec.H264Nvenc;
                    settings.Accelerator = HardwareAccelerator.NVENC;
                    break;

                case "amd":
                    settings.VideoCodec = Codec.H264Amf;
                    settings.Accelerator = HardwareAccelerator.AMF;
                    break;

                case "intel":
                    settings.VideoCodec = Codec.H264Qsv;
                    settings.Accelerator = HardwareAccelerator.QSV;
                    break;

                default:
                    Log.Debug($"Using software encoding");
                    break;
            }

            switch (options.TargetSize)
            {
                case VideoSize.SD480p:
                    settings.Width = 854;
                    settings.Height = 480;
                    settings.VideoBitrate = 1000;
                    break;

                case VideoSize.HD720p:
                    settings.Width = 1280;
                    settings.Height = 720;
                    settings.VideoBitrate = 2500;
                    break;

                case VideoSize.HD1080p:
                    settings.Width = 1920;
                    settings.Height = 1080;
                    settings.VideoBitrate = 4000;
                    break;

                case VideoSize.QHD1440p:
                    settings.Width = 2560;
                    settings.Height = 1440;
                    settings.VideoBitrate = 6000;
                    break;

                case VideoSize.UHD4K:
                    settings.Width = 3840;
                    settings.Height = 2160;
                    settings.VideoBitrate = 10000;
                    break;
            }

            return settings;
        }

        private void ConfigureOutput(FFMpegArgumentProcessor ffmpeg, OptimalSettings settings, ConversionOptions options)
        {
            var outputOptions = ffmpeg.OutputToFile(options.FastStart ? "-movflags +faststart" : "");

            outputOptions.WithVideoCodec(settings.VideoCodec.ToString())
                        .WithAudioCodec(settings.AudioCodec.ToString())
                        .WithConstantRateFactor(options.Quality)
                        .WithVideoBitrate(settings.VideoBitrate * 1000)
                        .WithAudioBitrate(settings.AudioBitrate * 1000);

            if (settings.Width > 0 && settings.Height > 0)
            {
                outputOptions.WithVideoFilters(filterOptions => filterOptions.Scale(settings.Width, settings.Height));
            }

            if (settings.Accelerator.HasValue)
            {
                outputOptions.WithHardwareAcceleration(settings.Accelerator.Value);
            }

            if (options.PreserveMetadata)
            {
                outputOptions.WithCustomArgument("-map_metadata 0");
            }
        }
    }
}
