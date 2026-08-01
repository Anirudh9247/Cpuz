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
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ComputerDoctor Agent Service starting up for AgentId: {AgentId}...", _config.AgentId);

        var serverUri = new Uri(_config.ServerWebSocketUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Harvest telemetry metrics
                var report = await _telemetryCollector.CollectReportAsync(stoppingToken);

                _logger.LogInformation("📊 TELEMETRY HARVESTED | CPU Load: {Cpu:F1}% (Temp: {CpuTemp}°C) | Memory: {Ram:F1}% | Processes: {ProcCount} | Drives: {DriveCount}", 
                    report.Hardware?.CpuTotalUsagePercentage ?? 0, 
                    report.Hardware?.CpuTempC.HasValue == true ? $"{report.Hardware.CpuTempC.Value:F1}" : "N/A",
                    report.Hardware?.MemoryUsagePercentage ?? 0,
                    report.TotalRunningProcessesCount,
                    report.Storage?.Drives.Count ?? 0);

                // Optional network transmission
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
                    var wrapper = new TelemetryPayloadWrapper
                    {
                        AgentVersion = "1.0.0",
                        MessageType = "TELEMETRY_REPORT",
                        Report = report
                    };
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
