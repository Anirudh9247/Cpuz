using Agent.Core.Commands;
using Agent.Core.Processes;
using Xunit;

namespace Agent.Tests.Core;

public class CommandExecutorTests
{
    [Fact]
    public async Task ExecuteCommandAsync_KillProcess_ReturnsExpectedAck()
    {
        // Arrange
        var processMonitor = new ProcessMonitor();
        var executor = new CommandExecutor(processMonitor);
        var paramsDict = new Dictionary<string, string> { { "processId", "999999" } };

        // Act
        var result = await executor.ExecuteCommandAsync("cmd-001", "KILL_PROCESS", paramsDict);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cmd-001", result.CommandId);
        Assert.Equal("KILL_PROCESS", result.Command);
        Assert.False(result.Success); // Non-existent process returns false cleanly without throwing
        Assert.True(result.ExecutionTimeMs >= 0);
    }

    [Fact]
    public async Task ExecuteCommandAsync_ClearTempFiles_ReturnsSuccess()
    {
        // Arrange
        var processMonitor = new ProcessMonitor();
        var executor = new CommandExecutor(processMonitor);

        // Act
        var result = await executor.ExecuteCommandAsync("cmd-002", "CLEAR_TEMP_FILES", new Dictionary<string, string>());

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("Cleaned", result.Message);
    }

    [Fact]
    public async Task ExecuteCommandAsync_UnknownCommand_ReturnsFailure()
    {
        // Arrange
        var processMonitor = new ProcessMonitor();
        var executor = new CommandExecutor(processMonitor);

        // Act
        var result = await executor.ExecuteCommandAsync("cmd-003", "INVALID_COMMAND", new Dictionary<string, string>());

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Unknown command", result.Message);
    }
}
