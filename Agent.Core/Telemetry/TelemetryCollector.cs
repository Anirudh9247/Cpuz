using Agent.Core.Alerts;
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
    private readonly IAlertEngine _alertEngine;
    private readonly IHealthScoreCalculator _healthScoreCalculator;
    private readonly AgentConfig _config;

    public TelemetryCollector(
        IHardwareMonitor hardwareMonitor,
        IProcessMonitor processMonitor,
        IStorageMonitor storageMonitor,
        IAlertEngine alertEngine,
        IHealthScoreCalculator healthScoreCalculator,
        IOptions<AgentConfig> configOptions)
    {
        _hardwareMonitor = hardwareMonitor;
        _processMonitor = processMonitor;
        _storageMonitor = storageMonitor;
        _alertEngine = alertEngine;
        _healthScoreCalculator = healthScoreCalculator;
        _config = configOptions.Value;
    }

    public async Task<HealthSnapshot> CollectSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = new HealthSnapshot
        {
            AgentId = _config.AgentId,
            MachineName = _config.MachineName,
            TimestampUtc = DateTime.UtcNow
        };

        if (_config.EnableHardwareMonitoring)
        {
            var hw = await _hardwareMonitor.GetHardwareMetricsAsync(cancellationToken);
            snapshot.Hardware = hw;
            if (hw != null)
            {
                snapshot.Cpu.TempC = hw.CpuTempC;
                snapshot.Cpu.LoadPercent = hw.CpuTotalUsagePercentage;
                snapshot.Cpu.LogicalProcessorCount = hw.LogicalProcessorCount;

                snapshot.Gpu.TempC = hw.GpuTempC;

                snapshot.Memory.UsagePercent = hw.MemoryUsagePercentage;
                double totalMb = hw.TotalPhysicalMemoryBytes / (1024.0 * 1024.0);
                double availMb = hw.AvailablePhysicalMemoryBytes / (1024.0 * 1024.0);
                snapshot.Memory.TotalMb = totalMb;
                snapshot.Memory.UsedMb = Math.Max(0, totalMb - availMb);
            }
        }

        if (_config.EnableProcessMonitoring)
        {
            var topProcs = await _processMonitor.GetTopProcessesAsync(_config.TopProcessCount, cancellationToken);
            int totalCount = await _processMonitor.GetTotalProcessCountAsync(cancellationToken);

            snapshot.Processes.TopProcesses = topProcs;
            snapshot.Processes.TotalRunningCount = totalCount;
            snapshot.Memory.TopProcesses = topProcs;
        }

        if (_config.EnableStorageMonitoring)
        {
            var storage = await _storageMonitor.GetStorageMetricsAsync(cancellationToken);
            snapshot.Storage = storage;
            if (storage != null)
            {
                snapshot.Drives = storage.Drives.Select(d => new DriveSnapshot
                {
                    Name = d.Name,
                    Label = d.Label,
                    TotalSizeGb = d.TotalSizeBytes / (1024.0 * 1024.0 * 1024.0),
                    FreeSpaceGb = d.FreeSizeBytes / (1024.0 * 1024.0 * 1024.0),
                    UsagePercent = d.UsagePercentage,
                    HealthPercent = 100,
                    SmartStatus = "OK"
                }).ToList();
            }
        }

        // Trust Metadata (Sprint 4 Foundation)
        snapshot.Trust = new TrustMetadata
        {
            ConfidenceScore = 100,
            SensorSource = "LibreHardwareMonitor",
            FallbackUsed = false
        };

        // Evaluate Alerts
        snapshot.Alerts = _alertEngine.Evaluate(snapshot);

        // Calculate Overall & Component Health Scores
        var (score, status) = _healthScoreCalculator.Calculate(snapshot, snapshot.Alerts);
        snapshot.OverallHealthScore = score;
        snapshot.OverallStatus = status;

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
