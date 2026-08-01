using Agent.Core.Hardware;
using Agent.Core.Models;
using Agent.Core.Processes;
using Agent.Core.Storage;
using Microsoft.Extensions.Options;

namespace Agent.Core.Telemetry;

public class TelemetryCollector : ITelemetryCollector
{
    private readonly IHardwareMonitor _hardwareMonitor;
    private readonly IProcessMonitor _processMonitor;
    private readonly IStorageMonitor _storageMonitor;
    private readonly AgentConfig _config;

    public TelemetryCollector(
        IHardwareMonitor hardwareMonitor,
        IProcessMonitor processMonitor,
        IStorageMonitor storageMonitor,
        IOptions<AgentConfig> configOptions)
    {
        _hardwareMonitor = hardwareMonitor;
        _processMonitor = processMonitor;
        _storageMonitor = storageMonitor;
        _config = configOptions.Value;
    }

    public async Task<SystemTelemetryReport> CollectReportAsync(CancellationToken cancellationToken = default)
    {
        var report = new SystemTelemetryReport
        {
            AgentId = _config.AgentId,
            MachineName = _config.MachineName,
            TimestampUtc = DateTime.UtcNow
        };

        if (_config.EnableHardwareMonitoring)
        {
            report.Hardware = await _hardwareMonitor.GetHardwareMetricsAsync(cancellationToken);
        }

        if (_config.EnableProcessMonitoring)
        {
            report.TopProcesses = await _processMonitor.GetTopProcessesAsync(_config.TopProcessCount, cancellationToken);
            report.TotalRunningProcessesCount = await _processMonitor.GetTotalProcessCountAsync(cancellationToken);
        }

        if (_config.EnableStorageMonitoring)
        {
            report.Storage = await _storageMonitor.GetStorageMetricsAsync(cancellationToken);
        }

        return report;
    }
}
