using System.Net.WebSockets;
using System.Text;
using Agent.Network.Json;

namespace Agent.Network.WebSocket;

public class AgentWebSocketClient : IAgentWebSocketClient
{
    private ClientWebSocket? _clientWebSocket;
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
    private CancellationTokenSource? _receiveCts;

    public bool IsConnected => _clientWebSocket?.State == WebSocketState.Open;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task ConnectAsync(Uri serverUri, CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        _clientWebSocket?.Dispose();
        _clientWebSocket = new ClientWebSocket();

        try
        {
            await _clientWebSocket.ConnectAsync(serverUri, cancellationToken);
            Connected?.Invoke(this, EventArgs.Empty);

            _receiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token), _receiveCts.Token);
        }
        catch
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_receiveCts != null)
        {
            _receiveCts.Cancel();
            _receiveCts.Dispose();
            _receiveCts = null;
        }

        if (_clientWebSocket != null)
        {
            if (_clientWebSocket.State == WebSocketState.Open || _clientWebSocket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Agent shutting down", cancellationToken);
                }
                catch
                {
                    // Ignore errors during closing phase
                }
            }
            _clientWebSocket.Dispose();
            _clientWebSocket = null;
        }

        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public async Task SendMessageAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _clientWebSocket == null)
        {
            throw new InvalidOperationException("WebSocket client is not connected.");
        }

        string jsonPayload = AgentJsonSerializer.Serialize(message);
        byte[] bytes = Encoding.UTF8.GetBytes(jsonPayload);

        await _sendSemaphore.WaitAsync(cancellationToken);
        try
        {
            await _clientWebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken
            );
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync(cancellationToken);
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    string message = await reader.ReadToEndAsync(cancellationToken);
                    MessageReceived?.Invoke(this, message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _receiveCts?.Cancel();
        _clientWebSocket?.Dispose();
        _sendSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
