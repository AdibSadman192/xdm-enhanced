using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using XDM.Core.Network;

namespace XDM.Tests.Network
{
    public class NetworkMonitorTests
    {
        [Fact]
        public void GetCurrentUsage_ShouldReturnValidData()
        {
            // Arrange
            using var monitor = new NetworkMonitor();

            // Act
            var usage = monitor.GetCurrentUsage();

            // Assert
            usage.Should().NotBeNull();
            usage.NetworkType.Should().NotBe(NetworkType.Unknown);
            usage.AverageBytesPerSecond.Should().BeGreaterOrEqual(0);
        }

        [Fact]
        public async Task GetHistoricalData_ShouldReturnCorrectTimeRange()
        {
            // Arrange
            using var monitor = new NetworkMonitor();
            var duration = TimeSpan.FromSeconds(5);

            // Wait for some samples to be collected
            await Task.Delay(duration);

            // Act
            var data = monitor.GetHistoricalData(duration);

            // Assert
            data.Should().NotBeEmpty();
            data.Should().OnlyContain(s => s.Timestamp >= DateTime.Now - duration);
        }

        [Fact]
        public void CircularBuffer_ShouldMaintainMaxCapacity()
        {
            // Arrange
            var buffer = new CircularBuffer<int>(3);

            // Act
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            // Assert
            var items = buffer.GetLastN(3);
            items.Should().BeEquivalentTo(new[] { 2, 3, 4 });
        }

        [Fact]
        public void CircularBuffer_GetLastN_ShouldReturnCorrectCount()
        {
            // Arrange
            var buffer = new CircularBuffer<int>(5);
            for (int i = 1; i <= 5; i++)
            {
                buffer.Add(i);
            }

            // Act
            var items = buffer.GetLastN(3);

            // Assert
            items.Should().HaveCount(3);
            items.Should().BeEquivalentTo(new[] { 3, 4, 5 });
        }

        [Fact]
        public void NetworkUsage_SignalStrength_ShouldBeInValidRange()
        {
            // Arrange
            using var monitor = new NetworkMonitor();

            // Act
            var usage = monitor.GetCurrentUsage();

            // Assert
            if (usage.SignalStrength.HasValue)
            {
                usage.SignalStrength.Value.Should().BeInRange(0, 100);
            }
        }

        [Fact]
        public async Task Monitor_ShouldHandleMultipleSamplesOverTime()
        {
            // Arrange
            using var monitor = new NetworkMonitor();
            var sampleDuration = TimeSpan.FromSeconds(3);

            // Act
            await Task.Delay(sampleDuration);
            var data = monitor.GetHistoricalData(sampleDuration);

            // Assert
            data.Should().HaveCountGreaterOrEqual(2);
            data.Should().BeInAscendingOrder(s => s.Timestamp);
        }

        [Fact]
        public void Monitor_ShouldIdentifyNetworkType()
        {
            // Arrange
            using var monitor = new NetworkMonitor();

            // Act
            var usage = monitor.GetCurrentUsage();

            // Assert
            usage.NetworkType.Should().NotBe(NetworkType.Unknown);
            Enum.IsDefined(typeof(NetworkType), usage.NetworkType).Should().BeTrue();
        }
    }
}
