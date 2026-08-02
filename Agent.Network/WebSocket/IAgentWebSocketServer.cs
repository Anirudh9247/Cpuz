namespace Agent.Network.WebSocket;

public class ClientMessageReceivedEventArgs : EventArgs
{
    public string ClientId { get; }
    public string Message { get; }

    public ClientMessageReceivedEventArgs(string clientId, string message)
    {
        ClientId = clientId;
        Message = message;
    }
}

public interface IAgentWebSocketServer : IDisposable
{
    bool IsRunning { get; }
    int ConnectedClientCount { get; }
    Task StartAsync(string urlPrefix, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task BroadcastAsync<T>(T message, CancellationToken cancellationToken = default);
    event EventHandler<ClientMessageReceivedEventArgs>? MessageReceived;
    event EventHandler<int>? ClientCountChanged;
}
