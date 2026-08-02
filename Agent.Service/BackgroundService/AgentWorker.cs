using Agent.Core.Models;
using Agent.Core.Processes;
using Agent.Core.Telemetry;
using Agent.Core.Validation;
using Agent.Network.Json;
using Agent.Network.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.Service.BackgroundService;

public class AgentWorker : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly ITelemetryCollector _telemetryCollector;
    private readonly IProcessMonitor _processMonitor;
    private readonly IAgentWebSocketClient _webSocketClient;
    private readonly IAgentWebSocketServer _webSocketServer;
    private readonly IHealthSnapshotValidator _validator;
    private readonly AgentConfig _config;
    private readonly ILogger<AgentWorker> _logger;

    public AgentWorker(
        ITelemetryCollector telemetryCollector,
        IProcessMonitor processMonitor,
        IAgentWebSocketClient webSocketClient,
        IAgentWebSocketServer webSocketServer,
        IHealthSnapshotValidator validator,
        IOptions<AgentConfig> configOptions,
        ILogger<AgentWorker> logger)
    {
        _telemetryCollector = telemetryCollector;
        _processMonitor = processMonitor;
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

                var wrapper = new TelemetryPayloadWrapper
                {
                    AgentVersion = "1.0.0",
                    MessageType = "HEALTH_SNAPSHOT",
                    Snapshot = snapshot
                };

                // Broadcast to all connected clients on Embedded WebSocket Server
                if (_webSocketServer.ConnectedClientCount > 0)
                {
                    await _webSocketServer.BroadcastAsync(wrapper, stoppingToken);
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

    private async void OnServerClientMessageReceived(object? sender, ClientMessageReceivedEventArgs e)
    {
        _logger.LogInformation("Received inbound message from client [{ClientId}]: {Message}", e.ClientId, e.Message);
        await ProcessIncomingCommandAsync(e.Message);
    }

    private async void OnWebSocketMessageReceived(object? sender, string message)
    {
        _logger.LogInformation("Received message from outbound server: {Message}", message);
        await ProcessIncomingCommandAsync(message);
    }

    private async Task ProcessIncomingCommandAsync(string message)
    {
        try
        {
            var command = AgentJsonSerializer.Deserialize<CommandMessage>(message);
            if (command != null && command.Command.Equals("KILL_PROCESS", StringComparison.OrdinalIgnoreCase))
            {
                if (command.Parameters.TryGetValue("processId", out string? pidStr) && int.TryParse(pidStr, out int pid))
                {
                    _logger.LogWarning("Executing command to kill process ID: {Pid}", pid);
                    bool success = await _processMonitor.KillProcessByIdAsync(pid);
                    _logger.LogInformation("Process kill request for PID {Pid} returned status: {Status}", pid, success);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse or execute incoming command message.");
        }
    }
}
