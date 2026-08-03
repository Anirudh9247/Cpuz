using System.Threading.Channels;
using Agent.Core.Commands;
using Agent.Core.Models;
using Agent.Core.Processes;
using Agent.Core.Telemetry;
using Agent.Core.Validation;
using Agent.Network.Discovery;
using Agent.Network.Json;
using Agent.Network.Security;
using Agent.Network.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.Service.BackgroundService;

public class InboundCommandItem
{
    public string ClientId { get; set; } = string.Empty;
    public string RawMessage { get; set; } = string.Empty;
}

public class AgentWorker : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly ITelemetryCollector _telemetryCollector;
    private readonly IProcessMonitor _processMonitor;
    private readonly ICommandExecutor _commandExecutor;
    private readonly IDiscoveryBroadcaster _discoveryBroadcaster;
    private readonly ISessionPairingManager _sessionPairingManager;
    private readonly IHeartbeatMonitor _heartbeatMonitor;
    private readonly IAgentWebSocketClient _webSocketClient;
    private readonly IAgentWebSocketServer _webSocketServer;
    private readonly IHealthSnapshotValidator _validator;
    private readonly AgentConfig _config;
    private readonly ILogger<AgentWorker> _logger;
    private readonly Channel<InboundCommandItem> _commandChannel = Channel.CreateUnbounded<InboundCommandItem>();

    public AgentWorker(
        ITelemetryCollector telemetryCollector,
        IProcessMonitor processMonitor,
        ICommandExecutor commandExecutor,
        IDiscoveryBroadcaster discoveryBroadcaster,
        ISessionPairingManager sessionPairingManager,
        IHeartbeatMonitor heartbeatMonitor,
        IAgentWebSocketClient webSocketClient,
        IAgentWebSocketServer webSocketServer,
        IHealthSnapshotValidator validator,
        IOptions<AgentConfig> configOptions,
        ILogger<AgentWorker> logger)
    {
        _telemetryCollector = telemetryCollector;
        _processMonitor = processMonitor;
        _commandExecutor = commandExecutor;
        _discoveryBroadcaster = discoveryBroadcaster;
        _sessionPairingManager = sessionPairingManager;
        _heartbeatMonitor = heartbeatMonitor;
        _webSocketClient = webSocketClient;
        _webSocketServer = webSocketServer;
        _validator = validator;
        _config = configOptions.Value;
        _logger = logger;

        _webSocketClient.MessageReceived += OnWebSocketMessageReceived;
        _webSocketClient.Connected += (s, e) => _logger.LogInformation("Successfully connected to outbound WebSocket server at {Url}", _config.ServerWebSocketUrl);
        _webSocketClient.Disconnected += (s, e) => _logger.LogWarning("Outbound WebSocket client disconnected.");

        _webSocketServer.MessageReceived += OnServerClientMessageReceived;
        _webSocketServer.ClientCountChanged += (s, count) => _logger.LogInformation("🌐 Embedded WebSocket Server client count updated: {Count} client(s) connected", count);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ComputerDoctor Agent Service starting up for AgentId: {AgentId}...", _config.AgentId);

        // Start background Command Processor worker thread
        _ = Task.Run(() => ProcessCommandQueueAsync(stoppingToken), stoppingToken);

        // Start Heartbeat PING/PONG Monitor
        try
        {
            _heartbeatMonitor.Start(pingIntervalMs: 5000, pongTimeoutMs: 15000);
            _logger.LogInformation("💓 Heartbeat PING/PONG Monitor started (ping: 5s, timeout: 15s)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start Heartbeat Monitor.");
        }

        // Start UDP Auto-Discovery Broadcaster on Port 8888
        try
        {
            _discoveryBroadcaster.Start(port: 8888, broadcastIntervalMs: 3000);
            _logger.LogInformation("📡 UDP Auto-Discovery Broadcaster started on port 8888 (interval: 3s)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start UDP Auto-Discovery Broadcaster on port 8888.");
        }

        // Start Embedded WebSocket Server for mobile / dashboard streaming
        try
        {
            await _webSocketServer.StartAsync("http://localhost:8080/ws/", stoppingToken);
            _logger.LogInformation("🚀 Embedded WebSocket Server started at http://localhost:8080/ws/");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start embedded WebSocket server at http://localhost:8080/ws/");
        }

        var serverUri = new Uri(_config.ServerWebSocketUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Harvest canonical HealthSnapshot
                var snapshot = await _telemetryCollector.CollectSnapshotAsync(stoppingToken);

                // Snapshot Validation
                var validation = _validator.Validate(snapshot);
                if (!validation.IsValid)
                {
                    _logger.LogWarning("⚠️ SNAPSHOT VALIDATION FAILED — Corrupted telemetry packet suppressed! Errors: {Errors}", string.Join("; ", validation.Errors));
                    await Task.Delay(_config.MetricCollectionIntervalMs, stoppingToken);
                    continue;
                }

                // Format status badge for logging
                string statusBadge = snapshot.OverallStatus switch
                {
                    OverallHealthStatus.Healthy => "🟢 HEALTHY",
                    OverallHealthStatus.Warning => "🟡 WARNING",
                    OverallHealthStatus.Critical => "🔴 CRITICAL",
                    _ => "⚪ UNKNOWN"
                };

                double cpuLoad = snapshot.Cpu.LoadPercent.HasValue && snapshot.Cpu.LoadPercent.Value.HasValue ? snapshot.Cpu.LoadPercent.Value.Value : 0.0;
                string cpuTempStr = snapshot.Cpu.TempC.HasValue && snapshot.Cpu.TempC.Value.HasValue ? $"{snapshot.Cpu.TempC.Value.Value:F1}°C" : "N/A";
                double ramUsage = snapshot.Memory.UsagePercent.HasValue && snapshot.Memory.UsagePercent.Value.HasValue ? snapshot.Memory.UsagePercent.Value.Value : 0.0;

                _logger.LogInformation("📊 HEALTH SNAPSHOT [{StatusBadge}] Score: {Score}/100 | Confidence: {Confidence}% | Latency: {Latency}ms | Seq: #{Seq} | Source: {Source} | Alerts: {AlertCount} | CPU: {CpuLoad:F1}% ({CpuTemp}) | RAM: {RamLoad:F1}%",
                    statusBadge,
                    snapshot.OverallHealthScore,
                    snapshot.Trust.ConfidenceScore,
                    snapshot.ProcessingLatencyMs,
                    snapshot.Sequence,
                    snapshot.Trust.SensorSource,
                    snapshot.Alerts.Count,
                    cpuLoad,
                    cpuTempStr,
                    ramUsage);

                if (snapshot.Alerts.Count > 0)
                {
                    foreach (var alert in snapshot.Alerts)
                    {
                        _logger.LogWarning("   🚨 ALERT [{Severity}] [{Category}] {Message}", alert.Severity, alert.Category, alert.Message);
                    }
                }

                // Standardized Envelope Broadcasting
                var envelope = new NetworkEnvelope<HealthSnapshot>
                {
                    Type = "TELEMETRY",
                    SchemaVersion = 1,
                    Payload = snapshot
                };

                var wrapper = new TelemetryPayloadWrapper
                {
                    AgentVersion = "1.0.0",
                    MessageType = "HEALTH_SNAPSHOT",
                    Snapshot = snapshot
                };

                // Broadcast to all connected clients on Embedded WebSocket Server
                if (_webSocketServer.ConnectedClientCount > 0)
                {
                    await _webSocketServer.BroadcastAsync(envelope, stoppingToken);
                }

                // Transmission over outbound WebSocket Client (if configured)
                if (!_webSocketClient.IsConnected)
                {
                    try
                    {
                        await _webSocketClient.ConnectAsync(serverUri, stoppingToken);
                    }
                    catch
                    {
                        // Background reconnect attempt
                    }
                }

                if (_webSocketClient.IsConnected)
                {
                    await _webSocketClient.SendMessageAsync(wrapper, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during telemetry collection iteration.");
            }

            await Task.Delay(_config.MetricCollectionIntervalMs, stoppingToken);
        }

        if (_webSocketServer.IsRunning)
        {
            await _webSocketServer.StopAsync(CancellationToken.None);
        }

        if (_webSocketClient.IsConnected)
        {
            await _webSocketClient.DisconnectAsync(CancellationToken.None);
        }

        _logger.LogInformation("ComputerDoctor Agent Service has stopped.");
    }

    private void OnServerClientMessageReceived(object? sender, ClientMessageReceivedEventArgs e)
    {
        _logger.LogInformation("Enqueuing inbound command from client [{ClientId}]", e.ClientId);
        _commandChannel.Writer.TryWrite(new InboundCommandItem { ClientId = e.ClientId, RawMessage = e.Message });
    }

    private void OnWebSocketMessageReceived(object? sender, string message)
    {
        _logger.LogInformation("Enqueuing message from outbound WebSocket server");
        _commandChannel.Writer.TryWrite(new InboundCommandItem { ClientId = "OutboundServer", RawMessage = message });
    }

    private async Task ProcessCommandQueueAsync(CancellationToken cancellationToken)
    {
        while (await _commandChannel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_commandChannel.Reader.TryRead(out var item))
            {
                try
                {
                    await ExecuteCommandItemAsync(item, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing queued command from client [{ClientId}]", item.ClientId);
                }
            }
        }
    }

    private async Task ExecuteCommandItemAsync(InboundCommandItem item, CancellationToken cancellationToken)
    {
        var command = AgentJsonSerializer.Deserialize<CommandMessage>(item.RawMessage);
        if (command == null || string.IsNullOrEmpty(command.Command)) return;

        // Security Authentication Check
        bool isAuthenticated = string.IsNullOrEmpty(_config.ApiKey) ||
            (command.Parameters.TryGetValue("apiKey", out string? providedKey) && providedKey == _config.ApiKey);

        if (!isAuthenticated)
        {
            _logger.LogWarning("🚨 UNAUTHORIZED COMMAND REJECTED from client [{ClientId}] for command '{Command}'", item.ClientId, command.Command);

            var authFailAck = new NetworkEnvelope<CommandAckPayload>
            {
                Type = "COMMAND_ACK",
                SchemaVersion = 1,
                Payload = new CommandAckPayload
                {
                    Command = command.Command,
                    Success = false,
                    Message = "Unauthorized: Missing or invalid API key."
                }
            };

            if (_webSocketServer.ConnectedClientCount > 0)
            {
                await _webSocketServer.BroadcastAsync(authFailAck, cancellationToken);
            }
            return;
        }

        string commandId = Guid.NewGuid().ToString("N");
        command.Parameters.TryGetValue("sessionToken", out string? token);
        _logger.LogInformation("⚡ Executing remote command '{Command}' from client [{ClientId}]", command.Command, item.ClientId);

        var ackPayload = await _commandExecutor.ExecuteCommandAsync(commandId, command.Command, command.Parameters, item.ClientId, token ?? string.Empty, cancellationToken);

        _logger.LogInformation("⚡ Remote command '{Command}' returned Result: {Success} in {Ms}ms — Message: {Msg}",
            command.Command, ackPayload.Success, ackPayload.ExecutionTimeMs, ackPayload.Message);

        var ackEnvelope = new NetworkEnvelope<CommandAckPayload>
        {
            Type = "COMMAND_ACK",
            SchemaVersion = 1,
            Payload = ackPayload
        };

        if (_webSocketServer.ConnectedClientCount > 0)
        {
            await _webSocketServer.BroadcastAsync(ackEnvelope, cancellationToken);
        }
    }
}
