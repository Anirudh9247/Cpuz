using System.Net;
using System.Net.Sockets;
using Agent.Network.Json;
using Agent.Network.WebSocket;
using Xunit;

namespace Agent.Tests.Network;

[Collection("WebSocketTests")]
public class WebSocketServerTests
{
    private static int GetFreeTcpPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    [Fact]
    public async Task AgentWebSocketServer_Starts_AcceptsConnection_And_BroadcastsMessage()
    {
        // Arrange
        int port = GetFreeTcpPort();
        string serverUrl = $"http://localhost:{port}/ws/";
        Uri clientUri = new Uri($"ws://localhost:{port}/ws/");

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

    [Fact]
    public async Task AgentWebSocketServer_MultipleClients_DisconnectOne_SurvivingClientReceivesBroadcast()
    {
        // Arrange
        int port = GetFreeTcpPort();
        string serverUrl = $"http://localhost:{port}/ws/";
        Uri clientUri = new Uri($"ws://localhost:{port}/ws/");

        using var server = new AgentWebSocketServer();
        using var client1 = new AgentWebSocketClient();
        using var client2 = new AgentWebSocketClient();

        string? client2Received = null;
        var tcs = new TaskCompletionSource<string>();

        client2.MessageReceived += (s, msg) =>
        {
            client2Received = msg;
            tcs.TrySetResult(msg);
        };

        // Act
        await server.StartAsync(serverUrl);
        await client1.ConnectAsync(clientUri);
        await client2.ConnectAsync(clientUri);

        Assert.Equal(2, server.ConnectedClientCount);

        // Disconnect Client 1
        await client1.DisconnectAsync();
        await Task.Delay(200);

        Assert.Equal(1, server.ConnectedClientCount);

        // Broadcast to remaining Client 2
        var envelope = new NetworkEnvelope<string>
        {
            Type = "TEST_PING",
            Payload = "Hello Client 2"
        };
        await server.BroadcastAsync(envelope);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        Assert.Equal(tcs.Task, completed);

        // Assert
        Assert.NotNull(client2Received);
        Assert.Contains("Hello Client 2", client2Received);

        await client2.DisconnectAsync();
        await server.StopAsync();
    }
}
