using Agent.Core.Models;
using Agent.Core.Processes;
using Agent.Core.Telemetry;
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
    private readonly AgentConfig _config;
    private readonly ILogger<AgentWorker> _logger;

    public AgentWorker(
        ITelemetryCollector telemetryCollector,
        IProcessMonitor processMonitor,
        IAgentWebSocketClient webSocketClient,
        IOptions<AgentConfig> configOptions,
        ILogger<AgentWorker> logger)
    {
        _telemetryCollector = telemetryCollector;
        _processMonitor = processMonitor;
        _webSocketClient = webSocketClient;
        _config = configOptions.Value;
        _logger = logger;

        _webSocketClient.MessageReceived += OnWebSocketMessageReceived;
        _webSocketClient.Connected += (s, e) => _logger.LogInformation("Successfully connected to WebSocket server at {Url}", _config.ServerWebSocketUrl);
        _webSocketClient.Disconnected += (s, e) => _logger.LogWarning("WebSocket client disconnected from server.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ComputerDoctor Agent Service starting up for AgentId: {AgentId}...", _config.AgentId);

        var serverUri = new Uri(_config.ServerWebSocketUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_webSocketClient.IsConnected)
                {
                    _logger.LogInformation("Attempting to connect to WebSocket server: {Uri}", serverUri);
                    try
                    {
                        await _webSocketClient.ConnectAsync(serverUri, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to connect to WebSocket server. Retrying in 5 seconds...");
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }
                }

                var report = await _telemetryCollector.CollectReportAsync(stoppingToken);
                var wrapper = new TelemetryPayloadWrapper
                {
                    AgentVersion = "1.0.0",
                    MessageType = "TELEMETRY_REPORT",
                    Report = report
                };

                await _webSocketClient.SendMessageAsync(wrapper, stoppingToken);
                _logger.LogDebug("Telemetry report successfully transmitted. CPU: {Cpu:F1}%, Memory: {Ram:F1}%", 
                    report.Hardware?.CpuTotalUsagePercentage ?? 0, 
                    report.Hardware?.MemoryUsagePercentage ?? 0);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during telemetry collection/transmission iteration.");
            }

            await Task.Delay(_config.MetricCollectionIntervalMs, stoppingToken);
        }

        if (_webSocketClient.IsConnected)
        {
            await _webSocketClient.DisconnectAsync(CancellationToken.None);
        }

        _logger.LogInformation("ComputerDoctor Agent Service has stopped.");
    }

    private async void OnWebSocketMessageReceived(object? sender, string message)
    {
        _logger.LogInformation("Received message from WebSocket server: {Message}", message);

        try
        {
            var command = AgentJsonSerializer.Deserialize<CommandMessage>(message);
            if (command != null && command.Command.Equals("KILL_PROCESS", StringComparison.OrdinalIgnoreCase))
            {
                if (command.Parameters.TryGetValue("processId", out string? pidStr) && int.TryParse(pidStr, out int pid))
                {
                    _logger.LogWarning("Executing server command to kill process ID: {Pid}", pid);
                    bool success = await _processMonitor.KillProcessByIdAsync(pid);
                    _logger.LogInformation("Process kill request for PID {Pid} returned status: {Status}", pid, success);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse or execute incoming WebSocket command message.");
        }
    }
}
