using Agent.Core.Alerts;
using Agent.Core.Models;

namespace Agent.Core.Health;

public class HealthSnapshotBuilder : IHealthSnapshotBuilder
{
    private readonly IAlertEngine _alertEngine;
    private readonly IHealthScoreCalculator _healthScoreCalculator;

    public HealthSnapshotBuilder(IAlertEngine alertEngine, IHealthScoreCalculator healthScoreCalculator)
    {
        _alertEngine = alertEngine;
        _healthScoreCalculator = healthScoreCalculator;
    }

    public HealthSnapshot Build(
        AgentConfig config,
        HardwareMetrics? hardware,
        List<ProcessInfo>? topProcesses,
        int totalProcessCount,
        StorageMetrics? storage)
    {
        var snapshot = new HealthSnapshot
        {
            AgentId = config.AgentId,
            MachineName = config.MachineName,
            TimestampUtc = DateTime.UtcNow,
            Hardware = hardware,
            Storage = storage
        };

        bool anyFallbackUsed = false;
        string mainSource = "LibreHardwareMonitor";

        if (hardware != null)
        {
            // CPU
            if (hardware.CpuTemp.HasValue)
            {
                snapshot.Cpu.TempC = SensorReading<double>.FromValue(hardware.CpuTemp.Value.Value, hardware.CpuTemp.Source, hardware.CpuTemp.IsFallback);
                if (hardware.CpuTemp.IsFallback) anyFallbackUsed = true;
                mainSource = hardware.CpuTemp.Source;
            }
            if (hardware.CpuUsage.HasValue)
            {
                snapshot.Cpu.LoadPercent = SensorReading<double>.FromValue(hardware.CpuUsage.Value.Value, hardware.CpuUsage.Source, hardware.CpuUsage.IsFallback);
                if (hardware.CpuUsage.IsFallback) anyFallbackUsed = true;
            }
            snapshot.Cpu.LogicalProcessorCount = hardware.LogicalProcessorCount;

            // GPU
            if (hardware.GpuTemp.HasValue)
            {
                snapshot.Gpu.TempC = SensorReading<double>.FromValue(hardware.GpuTemp.Value.Value, hardware.GpuTemp.Source, hardware.GpuTemp.IsFallback);
                if (hardware.GpuTemp.IsFallback) anyFallbackUsed = true;
            }

            // Memory
            if (hardware.MemoryUsage.HasValue)
            {
                snapshot.Memory.UsagePercent = SensorReading<double>.FromValue(hardware.MemoryUsage.Value.Value, hardware.MemoryUsage.Source, hardware.MemoryUsage.IsFallback);
            }
            double totalMb = hardware.TotalPhysicalMemoryBytes / (1024.0 * 1024.0);
            double availMb = hardware.AvailablePhysicalMemoryBytes / (1024.0 * 1024.0);
            snapshot.Memory.TotalMb = SensorReading<double>.FromValue(Math.Round(totalMb, 1), "GC.MemoryInfo");
            snapshot.Memory.UsedMb = SensorReading<double>.FromValue(Math.Round(Math.Max(0, totalMb - availMb), 1), "GC.MemoryInfo");
        }

        // Processes
        if (topProcesses != null)
        {
            snapshot.Processes.TopProcesses = topProcesses;
            snapshot.Processes.TotalRunningCount = totalProcessCount;
            snapshot.Memory.TopProcesses = topProcesses;
        }

        // Drives
        if (storage != null)
        {
            snapshot.Drives = storage.Drives.Select(d => new DriveSnapshot
            {
                Name = d.Name,
                Label = d.Label,
                TotalSizeGb = d.TotalSizeBytes / (1024.0 * 1024.0 * 1024.0),
                FreeSpaceGb = d.FreeSizeBytes / (1024.0 * 1024.0 * 1024.0),
                UsagePercent = SensorReading<double>.FromValue(d.UsagePercentage, "DriveInfo"),
                HealthPercent = SensorReading<int>.FromValue(100, "SMART"),
                SmartStatus = "OK"
            }).ToList();
        }

        // Trust Metadata Calculation
        snapshot.Trust = new TrustMetadata
        {
            ConfidenceScore = anyFallbackUsed ? 65 : 100,
            SensorSource = mainSource,
            FallbackUsed = anyFallbackUsed
        };

        // Evaluate Alerts
        snapshot.Alerts = _alertEngine.Evaluate(snapshot);

        // Calculate Overall & Component Health Scores
        var (score, status) = _healthScoreCalculator.Calculate(snapshot, snapshot.Alerts);
        snapshot.OverallHealthScore = score;
        snapshot.OverallStatus = status;

        return snapshot;
    }
}
