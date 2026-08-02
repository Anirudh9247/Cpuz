using Agent.Network.Json;
using Agent.Network.WebSocket;
using Xunit;

namespace Agent.Tests.Network;

public class WebSocketServerTests
{
    [Fact]
    public async Task AgentWebSocketServer_Starts_AcceptsConnection_And_BroadcastsMessage()
    {
        // Arrange
        string serverUrl = "http://localhost:8089/ws/";
        Uri clientUri = new Uri("ws://localhost:8089/ws/");

        using var server = new AgentWebSocketServer();
        using var client = new AgentWebSocketClient();

        string? receivedMessage = null;
        var tcs = new TaskCompletionSource<string>();

        server.MessageReceived += (s, e) =>
        {
            receivedMessage = e.Message;
            tcs.TrySetResult(e.Message);
        };

        // Act
        await server.StartAsync(serverUrl);
        Assert.True(server.IsRunning);

        await client.ConnectAsync(clientUri);
        Assert.True(client.IsConnected);

        var payload = new CommandMessage
        {
            Command = "KILL_PROCESS",
            Parameters = new Dictionary<string, string> { { "processId", "1234" } }
        };

        await client.SendMessageAsync(payload);

        var result = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        Assert.Equal(tcs.Task, result);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Contains("KILL_PROCESS", receivedMessage);

        await client.DisconnectAsync();
        await server.StopAsync();
    }
}
