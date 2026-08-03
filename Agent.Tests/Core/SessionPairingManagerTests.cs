using Agent.Core.Models;
using Agent.Core.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace Agent.Tests.Core;

public class SessionPairingManagerTests
{
    [Fact]
    public void PairClient_ValidPin_ReturnsSuccessAndToken()
    {
        // Arrange
        var config = Options.Create(new AgentConfig { ApiKey = "123456" });
        var pairingManager = new SessionPairingManager(config);

        // Act
        var result = pairingManager.PairClient("client-1", "Pixel7", "123456");

        // Assert
        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.SessionToken));
        Assert.Equal(ClientConnectionState.Paired, pairingManager.GetClientState("client-1"));
    }

    [Fact]
    public void PairClient_InvalidPin_ReturnsFailure()
    {
        // Arrange
        var config = Options.Create(new AgentConfig { ApiKey = "123456" });
        var pairingManager = new SessionPairingManager(config);

        // Act
        var result = pairingManager.PairClient("client-1", "Pixel7", "WRONG_PIN");

        // Assert
        Assert.False(result.Success);
        Assert.True(string.IsNullOrEmpty(result.SessionToken));
        Assert.Equal(ClientConnectionState.Disconnected, pairingManager.GetClientState("client-1"));
    }

    [Fact]
    public void ValidateSessionToken_ValidToken_ReturnsTrueAndActivatesState()
    {
        // Arrange
        var config = Options.Create(new AgentConfig { ApiKey = "" }); // Open pairing
        var pairingManager = new SessionPairingManager(config);

        var pairResult = pairingManager.PairClient("client-2", "GalaxyS23", "");
        Assert.True(pairResult.Success);

        // Act
        bool isValid = pairingManager.ValidateSessionToken("client-2", pairResult.SessionToken);

        // Assert
        Assert.True(isValid);
        Assert.Equal(ClientConnectionState.Active, pairingManager.GetClientState("client-2"));
    }
}
