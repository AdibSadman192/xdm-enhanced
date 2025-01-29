using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using XDM.Core.Download;
using Moq;
using System.Linq;

namespace XDM.Tests.Download
{
    public class SmartSchedulerTests
    {
        private readonly SmartScheduler _scheduler;
        private readonly Mock<DownloadTask> _mockTask;

        public SmartSchedulerTests()
        {
            _scheduler = new SmartScheduler();
            _mockTask = new Mock<DownloadTask>();
            _mockTask.Setup(t => t.Id).Returns(Guid.NewGuid().ToString());
        }

        [Fact]
        public async Task ScheduleDownload_ImmediatePriority_ShouldStartImmediately()
        {
            // Arrange
            var preference = new SchedulePreference
            {
                Priority = SchedulePriority.Immediate,
                Size = 1024 * 1024 // 1MB
            };

            // Act
            await _scheduler.ScheduleDownloadAsync(_mockTask.Object, preference);
            var status = _scheduler.GetScheduleStatus().First();

            // Assert
            status.Status.Should().NotBe(ScheduleStatus.Pending);
            status.ScheduledTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ScheduleDownload_OffPeak_ShouldScheduleForNightTime()
        {
            // Arrange
            var preference = new SchedulePreference
            {
                Priority = SchedulePriority.OffPeak,
                Size = 1024 * 1024
            };

            // Act
            await _scheduler.ScheduleDownloadAsync(_mockTask.Object, preference);
            var status = _scheduler.GetScheduleStatus().First();

            // Assert
            if (DateTime.Now.Hour >= 23 || DateTime.Now.Hour < 6)
            {
                status.Status.Should().NotBe(ScheduleStatus.Pending);
            }
            else
            {
                status.Status.Should().Be(ScheduleStatus.Pending);
                status.ScheduledTime.Hour.Should().BeOneOf(23, 0, 1, 2, 3, 4, 5);
            }
        }

        [Fact]
        public async Task ScheduleDownload_BestEffort_ShouldConsiderBandwidth()
        {
            // Arrange
            var preference = new SchedulePreference
            {
                Priority = SchedulePriority.BestEffort,
                Size = 100 * 1024 * 1024 // 100MB
            };

            // Act
            await _scheduler.ScheduleDownloadAsync(_mockTask.Object, preference);
            var status = _scheduler.GetScheduleStatus().First();

            // Assert
            status.Should().NotBeNull();
            status.ScheduledTime.Should().BeAfter(DateTime.Now);
        }

        [Fact]
        public async Task CancelSchedule_ShouldRemoveDownload()
        {
            // Arrange
            var taskId = _mockTask.Object.Id;
            var preference = new SchedulePreference
            {
                Priority = SchedulePriority.BestEffort
            };

            // Act
            await _scheduler.ScheduleDownloadAsync(_mockTask.Object, preference);
            await _scheduler.CancelScheduleAsync(taskId);
            var status = _scheduler.GetScheduleStatus();

            // Assert
            status.Should().NotContain(s => s.TaskId == taskId);
        }

        [Fact]
        public async Task GetScheduleStatus_ShouldReturnAllScheduledDownloads()
        {
            // Arrange
            var tasks = Enumerable.Range(0, 3).Select(_ =>
            {
                var mock = new Mock<DownloadTask>();
                mock.Setup(t => t.Id).Returns(Guid.NewGuid().ToString());
                return mock.Object;
            }).ToList();

            // Act
            foreach (var task in tasks)
            {
                await _scheduler.ScheduleDownloadAsync(task, new SchedulePreference
                {
                    Priority = SchedulePriority.BestEffort
                });
            }

            var status = _scheduler.GetScheduleStatus();

            // Assert
            status.Should().HaveCount(tasks.Count);
            status.Select(s => s.TaskId).Should().Contain(tasks.Select(t => t.Id));
        }

        [Fact]
        public async Task ScheduleDownload_WithError_ShouldUpdateStatus()
        {
            // Arrange
            var mockTask = new Mock<DownloadTask>();
            mockTask.Setup(t => t.Id).Returns(Guid.NewGuid().ToString());
            mockTask.Setup(t => t.StartAsync())
                   .ThrowsAsync(new Exception("Test error"));

            // Act
            await _scheduler.ScheduleDownloadAsync(mockTask.Object, new SchedulePreference
            {
                Priority = SchedulePriority.Immediate
            });

            await Task.Delay(100); // Allow time for processing
            var status = _scheduler.GetScheduleStatus().First();

            // Assert
            status.Status.Should().Be(ScheduleStatus.Failed);
            status.Error.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData(SchedulePriority.Immediate)]
        [InlineData(SchedulePriority.OffPeak)]
        [InlineData(SchedulePriority.BestEffort)]
        public async Task ScheduleDownload_DifferentPriorities_ShouldHandleCorrectly(SchedulePriority priority)
        {
            // Arrange
            var preference = new SchedulePreference
            {
                Priority = priority,
                Size = 1024 * 1024
            };

            // Act
            await _scheduler.ScheduleDownloadAsync(_mockTask.Object, preference);
            var status = _scheduler.GetScheduleStatus().First();

            // Assert
            status.Should().NotBeNull();
            status.TaskId.Should().Be(_mockTask.Object.Id);
        }

        [Fact]
        public async Task ScheduleDownload_WithPreferredTime_ShouldRespectPreference()
        {
            // Arrange
            var preferredTime = DateTime.Now.AddHours(2);
            var preference = new SchedulePreference
            {
                Priority = SchedulePriority.BestEffort,
                PreferredTime = preferredTime,
                AllowReschedule = false
            };

            // Act
            await _scheduler.ScheduleDownloadAsync(_mockTask.Object, preference);
            var status = _scheduler.GetScheduleStatus().First();

            // Assert
            status.ScheduledTime.Should().BeCloseTo(preferredTime, TimeSpan.FromMinutes(1));
        }
    }
}
