using Agent.Core.Models;
using Agent.Core.Storage;
using Xunit;

namespace Agent.Tests.Core;

public class StorageMonitorTests
{
    [Fact]
    public async Task GetStorageMetricsAsync_ReturnsValidMetrics()
    {
        // Arrange
        IStorageMonitor monitor = new StorageMonitor();

        // Act
        var metrics = await monitor.GetStorageMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.NotNull(metrics.Drives);
        Assert.True(metrics.TotalStorageBytes >= 0);
        Assert.True(metrics.OverallStorageUsagePercentage >= 0.0 && metrics.OverallStorageUsagePercentage <= 100.0);
    }

    [Fact]
    public void DriveMetrics_UsagePercentage_CalculatesCorrectly()
    {
        // Arrange
        var drive = new DriveMetrics
        {
            TotalSizeBytes = 1000,
            FreeSizeBytes = 250
        };

        // Act & Assert
        Assert.Equal(750, drive.UsedSizeBytes);
        Assert.Equal(75.0, drive.UsagePercentage);
    }
}
