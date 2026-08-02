using System.Diagnostics;
using Agent.Core.Hardware;
using Agent.Core.Health;
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
    private readonly IHealthSnapshotBuilder _snapshotBuilder;
    private readonly AgentConfig _config;

    public TelemetryCollector(
        IHardwareMonitor hardwareMonitor,
        IProcessMonitor processMonitor,
        IStorageMonitor storageMonitor,
        IHealthSnapshotBuilder snapshotBuilder,
        IOptions<AgentConfig> configOptions)
    {
        _hardwareMonitor = hardwareMonitor;
        _processMonitor = processMonitor;
        _storageMonitor = storageMonitor;
        _snapshotBuilder = snapshotBuilder;
        _config = configOptions.Value;
    }

    public async Task<HealthSnapshot> CollectSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        HardwareMetrics? hardware = null;
        List<ProcessInfo>? topProcesses = null;
        int totalProcessCount = 0;
        StorageMetrics? storage = null;

        if (_config.EnableHardwareMonitoring)
        {
            hardware = await _hardwareMonitor.GetHardwareMetricsAsync(cancellationToken);
        }

        if (_config.EnableProcessMonitoring)
        {
            topProcesses = await _processMonitor.GetTopProcessesAsync(_config.TopProcessCount, cancellationToken);
            totalProcessCount = await _processMonitor.GetTotalProcessCountAsync(cancellationToken);
        }

        if (_config.EnableStorageMonitoring)
        {
            storage = await _storageMonitor.GetStorageMetricsAsync(cancellationToken);
        }

        var snapshot = _snapshotBuilder.Build(_config, hardware, topProcesses, totalProcessCount, storage);
        sw.Stop();
        snapshot.ProcessingLatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);

        return snapshot;
    }

    public async Task<SystemTelemetryReport> CollectReportAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await CollectSnapshotAsync(cancellationToken);
        return new SystemTelemetryReport
        {
            AgentId = snapshot.AgentId,
            MachineName = snapshot.MachineName,
            TimestampUtc = snapshot.TimestampUtc,
            OverallHealthScore = snapshot.OverallHealthScore,
            OverallStatus = snapshot.OverallStatus,
            Cpu = snapshot.Cpu,
            Gpu = snapshot.Gpu,
            Memory = snapshot.Memory,
            Battery = snapshot.Battery,
            Drives = snapshot.Drives,
            Processes = snapshot.Processes,
            Defender = snapshot.Defender,
            Alerts = snapshot.Alerts,
            Trust = snapshot.Trust,
            Hardware = snapshot.Hardware,
            Storage = snapshot.Storage
        };
    }
}
