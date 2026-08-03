using Agent.Core.Models;
using Agent.Core.Security;
using Agent.Network.Json;
using Agent.Network.WebSocket;
using Microsoft.Extensions.Logging;

namespace Agent.Network.Security;

public class HeartbeatMonitor : IHeartbeatMonitor
{
    private readonly ISessionPairingManager _sessionPairingManager;
    private readonly IAgentWebSocketServer _webSocketServer;
    private readonly ILogger<HeartbeatMonitor> _logger;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public HeartbeatMonitor(
        ISessionPairingManager sessionPairingManager,
        IAgentWebSocketServer webSocketServer,
        ILogger<HeartbeatMonitor> logger)
    {
        _sessionPairingManager = sessionPairingManager;
        _webSocketServer = webSocketServer;
        _logger = logger;
    }

    public void Start(int pingIntervalMs = 5000, int pongTimeoutMs = 15000)
    {
        if (_isRunning) return;

        _cts = new CancellationTokenSource();
        _isRunning = true;

        _ = Task.Run(async () =>
        {
            while (_cts != null && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await SendPingAndCheckStaleConnectionsAsync(pongTimeoutMs, _cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error occurred during heartbeat monitoring iteration.");
                }

                try
                {
                    await Task.Delay(pingIntervalMs, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        });
    }

    public void RecordPong(string clientId)
    {
        _sessionPairingManager.UpdateClientActivity(clientId);
        _logger.LogDebug("Heartbeat PONG received from client [{ClientId}]", clientId);
    }

    private async Task SendPingAndCheckStaleConnectionsAsync(int pongTimeoutMs, CancellationToken cancellationToken)
    {
        var activeSessions = _sessionPairingManager.GetActiveSessions();
        if (activeSessions.Count == 0) return;

        var now = DateTime.UtcNow;
        var pingEnvelope = new NetworkEnvelope<HeartbeatPayload>
        {
            Type = "PING",
            SchemaVersion = 1,
            Payload = new HeartbeatPayload { Status = "PING" }
        };

        foreach (var session in activeSessions)
        {
            double elapsedMs = (now - session.LastActiveUtc).TotalMilliseconds;

            if (elapsedMs > pongTimeoutMs)
            {
                _logger.LogWarning("⚠️ Stale connection detected for client [{ClientId}] ({DeviceName}). Last active {Ms:F0}ms ago. Purging session.",
                    session.ClientId, session.DeviceName, elapsedMs);

                _sessionPairingManager.SetClientState(session.ClientId, ClientConnectionState.Disconnected);
                _sessionPairingManager.RemoveClient(session.ClientId);
            }
            else if (_webSocketServer.ConnectedClientCount > 0)
            {
                await _webSocketServer.BroadcastAsync(pingEnvelope, cancellationToken);
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
