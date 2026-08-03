using Agent.Core.Commands;
using Agent.Core.Models;
using Agent.Core.Processes;
using Agent.Core.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace Agent.Tests.Core;

public class CommandExecutorTests
{
    private readonly ISessionPairingManager _pairingManager;

    public CommandExecutorTests()
    {
        var config = Options.Create(new AgentConfig { ApiKey = "" });
        _pairingManager = new SessionPairingManager(config);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Unauthenticated_ReturnsUnauthorizedAck()
    {
        // Arrange
        var processMonitor = new ProcessMonitor();
        var executor = new CommandExecutor(processMonitor, _pairingManager);
        var paramsDict = new Dictionary<string, string> { { "processId", "999999" } };

        // Act (No pairing, invalid session token)
        var result = await executor.ExecuteCommandAsync("cmd-001", "KILL_PROCESS", paramsDict, "client-1", "INVALID_TOKEN");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Unauthorized", result.Message);
    }

    [Fact]
    public async Task ExecuteCommandAsync_PairRequest_ReturnsSessionToken()
    {
        // Arrange
        var processMonitor = new ProcessMonitor();
        var executor = new CommandExecutor(processMonitor, _pairingManager);
        var paramsDict = new Dictionary<string, string> { { "deviceName", "iPhone15" } };

        // Act
        var result = await executor.ExecuteCommandAsync("cmd-002", "PAIR_REQUEST", paramsDict, "client-1", "");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(paramsDict.ContainsKey("sessionToken"));
    }

    [Fact]
    public async Task ExecuteCommandAsync_Authenticated_ClearTempFiles_ReturnsSuccess()
    {
        // Arrange
        var processMonitor = new ProcessMonitor();
        var executor = new CommandExecutor(processMonitor, _pairingManager);

        var pairResult = _pairingManager.PairClient("client-1", "TestDevice", "");
        Assert.True(pairResult.Success);

        // Act (Using valid session token)
        var result = await executor.ExecuteCommandAsync("cmd-003", "CLEAR_TEMP_FILES", new Dictionary<string, string>(), "client-1", pairResult.SessionToken);

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
        var executor = new CommandExecutor(processMonitor, _pairingManager);

        var pairResult = _pairingManager.PairClient("client-1", "TestDevice", "");

        // Act
        var result = await executor.ExecuteCommandAsync("cmd-004", "INVALID_COMMAND", new Dictionary<string, string>(), "client-1", pairResult.SessionToken);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Unknown command", result.Message);
    }
}
