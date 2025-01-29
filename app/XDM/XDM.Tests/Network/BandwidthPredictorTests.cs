using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using XDM.Core.Network;

namespace XDM.Tests.Network
{
    public class BandwidthPredictorTests
    {
        private readonly BandwidthPredictor _predictor;

        public BandwidthPredictorTests()
        {
            _predictor = new BandwidthPredictor();
            AddTestData();
        }

        private void AddTestData()
        {
            var now = DateTime.Now;
            var networkTypes = new[] { NetworkType.WiFi, NetworkType.Ethernet };
            var random = new Random();

            // Add samples for the last 7 days
            for (int day = 0; day < 7; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    var time = now.AddDays(-day).Date.AddHours(hour);
                    var bandwidth = GetSimulatedBandwidth(hour);
                    var networkType = networkTypes[random.Next(networkTypes.Length)];

                    _predictor.AddSample(time, bandwidth, networkType);
                }
            }
        }

        private double GetSimulatedBandwidth(int hour)
        {
            // Simulate typical daily bandwidth patterns
            if (hour >= 1 && hour <= 6)
            {
                return 10_000_000; // High bandwidth during night (10 MB/s)
            }
            else if (hour >= 7 && hour <= 9)
            {
                return 5_000_000; // Medium bandwidth during morning (5 MB/s)
            }
            else if (hour >= 10 && hour <= 16)
            {
                return 2_000_000; // Lower bandwidth during work hours (2 MB/s)
            }
            else if (hour >= 17 && hour <= 22)
            {
                return 3_000_000; // Medium-low bandwidth during evening (3 MB/s)
            }
            else
            {
                return 8_000_000; // High bandwidth during late night (8 MB/s)
            }
        }

        [Fact]
        public void PredictBandwidth_ShouldReturnValidPrediction()
        {
            // Arrange
            var targetTime = DateTime.Now.AddHours(1);

            // Act
            var prediction = _predictor.PredictBandwidth(targetTime);

            // Assert
            prediction.Should().NotBeNull();
            prediction.Hour.Should().Be(targetTime.Hour);
            prediction.AvailableBandwidth.Should().BeGreaterThan(0);
            prediction.Confidence.Should().BeInRange(0, 1);
        }

        [Theory]
        [InlineData(3)] // Night
        [InlineData(8)] // Morning
        [InlineData(14)] // Afternoon
        [InlineData(20)] // Evening
        public void PredictBandwidth_ShouldReflectTimeOfDay(int hour)
        {
            // Arrange
            var targetTime = DateTime.Now.Date.AddHours(hour);

            // Act
            var prediction = _predictor.PredictBandwidth(targetTime);

            // Assert
            prediction.Hour.Should().Be(hour);
            prediction.AvailableBandwidth.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetHourlyPredictions_ShouldReturnCorrectNumberOfPredictions()
        {
            // Arrange
            var start = DateTime.Now;
            var hours = 24;

            // Act
            var predictions = _predictor.GetHourlyPredictions(start, hours).ToList();

            // Assert
            predictions.Should().HaveCount(hours);
            predictions.Should().OnlyContain(p => p.AvailableBandwidth >= 0);
            predictions.Should().OnlyContain(p => p.Confidence >= 0 && p.Confidence <= 1);
        }

        [Fact]
        public void Predictions_ShouldShowDailyPattern()
        {
            // Arrange
            var start = DateTime.Now.Date;
            var predictions = _predictor.GetHourlyPredictions(start, 24).ToList();

            // Act
            var nightPredictions = predictions.Where(p => p.Hour >= 1 && p.Hour <= 6);
            var dayPredictions = predictions.Where(p => p.Hour >= 10 && p.Hour <= 16);

            // Assert
            nightPredictions.Average(p => p.AvailableBandwidth)
                .Should().BeGreaterThan(dayPredictions.Average(p => p.AvailableBandwidth));
        }

        [Fact]
        public void UpdateModel_ShouldNotThrowException()
        {
            // Act
            Action action = () => _predictor.UpdateModel();

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void Confidence_ShouldBeHigherForConsistentData()
        {
            // Arrange
            var now = DateTime.Now;
            var consistentHour = 3; // Night time with consistent bandwidth
            var inconsistentHour = 12; // Day time with variable bandwidth

            // Act
            var nightPrediction = _predictor.PredictBandwidth(now.Date.AddHours(consistentHour));
            var dayPrediction = _predictor.PredictBandwidth(now.Date.AddHours(inconsistentHour));

            // Assert
            nightPrediction.Confidence.Should().BeGreaterThan(dayPrediction.Confidence);
        }

        [Fact]
        public void PredictBandwidth_ShouldHandleDifferentNetworkTypes()
        {
            // Arrange
            var time = DateTime.Now;

            // Act
            var prediction = _predictor.PredictBandwidth(time);

            // Assert
            prediction.NetworkType.Should().NotBe(NetworkType.Unknown);
            Enum.IsDefined(typeof(NetworkType), prediction.NetworkType).Should().BeTrue();
        }
    }
}
