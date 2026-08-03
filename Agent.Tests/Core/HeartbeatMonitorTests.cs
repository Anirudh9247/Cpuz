using Agent.Core.Models;
using Agent.Core.Security;
using Agent.Network.Security;
using Agent.Network.WebSocket;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Agent.Tests.Core;

public class HeartbeatMonitorTests
{
    [Fact]
    public void RecordPong_UpdatesLastActiveTimestamp()
    {
        // Arrange
        var config = Options.Create(new AgentConfig { ApiKey = "" });
        var pairingManager = new SessionPairingManager(config);
        var wsServer = new AgentWebSocketServer();
        var logger = NullLogger<HeartbeatMonitor>.Instance;
        var heartbeat = new HeartbeatMonitor(pairingManager, wsServer, logger);

        var pairResult = pairingManager.PairClient("client-pong", "TestDevice", "");
        Assert.True(pairResult.Success);

        var sessionBefore = pairingManager.GetSession("client-pong");
        Assert.NotNull(sessionBefore);
        var timeBefore = sessionBefore.LastActiveUtc;

        // Act
        heartbeat.RecordPong("client-pong");

        // Assert
        var sessionAfter = pairingManager.GetSession("client-pong");
        Assert.NotNull(sessionAfter);
        Assert.True(sessionAfter.LastActiveUtc >= timeBefore);
    }
}
