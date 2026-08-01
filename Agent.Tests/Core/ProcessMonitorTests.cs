using Agent.Core.Processes;
using Xunit;

namespace Agent.Tests.Core;

public class ProcessMonitorTests
{
    [Fact]
    public async Task GetTopProcessesAsync_ReturnsRequestedCount()
    {
        // Arrange
        IProcessMonitor monitor = new ProcessMonitor();
        int requestedCount = 5;

        // Act
        var processes = await monitor.GetTopProcessesAsync(requestedCount);

        // Assert
        Assert.NotNull(processes);
        Assert.InRange(processes.Count, 0, requestedCount);
        Assert.True(processes.SequenceEqual(processes.OrderByDescending(p => p.WorkingSetMemoryBytes)));
    }

    [Fact]
    public async Task GetTotalProcessCountAsync_ReturnsGreaterThanZero()
    {
        // Arrange
        IProcessMonitor monitor = new ProcessMonitor();

        // Act
        int count = await monitor.GetTotalProcessCountAsync();

        // Assert
        Assert.True(count > 0, "Total running process count should be greater than zero.");
    }
}
