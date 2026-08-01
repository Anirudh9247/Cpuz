namespace Agent.Network.WebSocket;

public interface IAgentWebSocketClient : IDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(Uri serverUri, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync<T>(T message, CancellationToken cancellationToken = default);
    event EventHandler<string>? MessageReceived;
    event EventHandler? Connected;
    event EventHandler? Disconnected;
}
