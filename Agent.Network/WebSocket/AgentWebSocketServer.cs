using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Agent.Network.Json;

namespace Agent.Network.WebSocket;

public class AgentWebSocketServer : IAgentWebSocketServer
{
    private HttpListener? _httpListener;
    private readonly ConcurrentDictionary<string, System.Net.WebSockets.WebSocket> _clients = new();
    private CancellationTokenSource? _serverCts;
    private readonly SemaphoreSlim _broadcastLock = new(1, 1);

    public bool IsRunning => _httpListener != null && _httpListener.IsListening;
    public int ConnectedClientCount => _clients.Count;

    public event EventHandler<ClientMessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<int>? ClientCountChanged;

    public Task StartAsync(string urlPrefix, CancellationToken cancellationToken = default)
    {
        if (IsRunning) return Task.CompletedTask;

        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(urlPrefix.EndsWith("/") ? urlPrefix : urlPrefix + "/");
        _httpListener.Start();

        _serverCts = new CancellationTokenSource();
        _ = Task.Run(() => AcceptLoopAsync(_serverCts.Token), _serverCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_serverCts != null)
        {
            _serverCts.Cancel();
            _serverCts.Dispose();
            _serverCts = null;
        }

        foreach (var kvp in _clients)
        {
            if (kvp.Value.State == WebSocketState.Open)
            {
                try
                {
                    await kvp.Value.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server stopping", cancellationToken);
                }
                catch
                {
                    // Ignore errors during shutdown
                }
            }
            kvp.Value.Dispose();
        }

        _clients.Clear();
        ClientCountChanged?.Invoke(this, 0);

        if (_httpListener != null)
        {
            try
            {
                _httpListener.Stop();
                _httpListener.Close();
            }
            catch
            {
                // Ignore errors during stopping
            }
            _httpListener = null;
        }
    }

    public async Task BroadcastAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        if (_clients.IsEmpty) return;

        string jsonPayload = AgentJsonSerializer.Serialize(message);
        byte[] bytes = Encoding.UTF8.GetBytes(jsonPayload);
        var segment = new ArraySegment<byte>(bytes);

        await _broadcastLock.WaitAsync(cancellationToken);
        try
        {
            var tasks = _clients.Select(async kvp =>
            {
                var clientId = kvp.Key;
                var ws = kvp.Value;

                if (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
                    }
                    catch
                    {
                        RemoveClient(clientId);
                    }
                }
                else
                {
                    RemoveClient(clientId);
                }
            });

            await Task.WhenAll(tasks);
        }
        finally
        {
            _broadcastLock.Release();
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _httpListener != null && _httpListener.IsListening)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    _ = Task.Run(() => ProcessWebSocketRequestAsync(context, cancellationToken), cancellationToken);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (HttpListenerException)
            {
                // Listener stopped
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                // Handle unexpected accept errors gracefully
            }
        }
    }

    private async Task ProcessWebSocketRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        WebSocketContext? wsContext = null;
        try
        {
            wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
        }
        catch
        {
            context.Response.StatusCode = 500;
            context.Response.Close();
            return;
        }

        string clientId = Guid.NewGuid().ToString("N");
        var webSocket = wsContext.WebSocket;
        _clients[clientId] = webSocket;
        ClientCountChanged?.Invoke(this, _clients.Count);

        var buffer = new byte[8192];
        try
        {
            while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    string message = await reader.ReadToEndAsync(cancellationToken);
                    MessageReceived?.Invoke(this, new ClientMessageReceivedEventArgs(clientId, message));
                }
            }
        }
        catch
        {
            // Disconnection handled below
        }
        finally
        {
            RemoveClient(clientId);
        }
    }

    private void RemoveClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out var ws))
        {
            try
            {
                ws.Dispose();
            }
            catch
            {
                // Ignore dispose exceptions
            }
            ClientCountChanged?.Invoke(this, _clients.Count);
        }
    }

    public void Dispose()
    {
        _serverCts?.Cancel();
        StopAsync().GetAwaiter().GetResult();
        _broadcastLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
