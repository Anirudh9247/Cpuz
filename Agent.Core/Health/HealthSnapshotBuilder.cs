using Agent.Core.Alerts;
using Agent.Core.Models;

namespace Agent.Core.Health;

public class HealthSnapshotBuilder : IHealthSnapshotBuilder
{
    private static long _sequenceCounter = 0;
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
            SchemaVersion = 1,
            Sequence = Interlocked.Increment(ref _sequenceCounter),
            AgentId = config.AgentId,
            MachineName = config.MachineName,
            TimestampUtc = DateTime.UtcNow,
            Hardware = hardware,
            Storage = storage
        };

        bool anyFallbackUsed = false;
        string mainSource = "LibreHardwareMonitor";
        int totalConfidence = 0;
        int sensorCount = 0;

        if (hardware != null)
        {
            // CPU
            if (hardware.CpuTemp.HasValue)
            {
                snapshot.Cpu.TempC = SensorReading<double>.FromValue(hardware.CpuTemp.Value.Value, hardware.CpuTemp.Source, hardware.CpuTemp.IsFallback, hardware.CpuTemp.ConfidenceScore);
                if (hardware.CpuTemp.IsFallback) anyFallbackUsed = true;
                mainSource = hardware.CpuTemp.Source;
                totalConfidence += hardware.CpuTemp.ConfidenceScore;
                sensorCount++;
            }
            if (hardware.CpuUsage.HasValue)
            {
                snapshot.Cpu.LoadPercent = SensorReading<double>.FromValue(hardware.CpuUsage.Value.Value, hardware.CpuUsage.Source, hardware.CpuUsage.IsFallback, hardware.CpuUsage.ConfidenceScore);
                if (hardware.CpuUsage.IsFallback) anyFallbackUsed = true;
                totalConfidence += hardware.CpuUsage.ConfidenceScore;
                sensorCount++;
            }
            snapshot.Cpu.LogicalProcessorCount = hardware.LogicalProcessorCount;

            // GPU
            if (hardware.GpuTemp.HasValue)
            {
                snapshot.Gpu.TempC = SensorReading<double>.FromValue(hardware.GpuTemp.Value.Value, hardware.GpuTemp.Source, hardware.GpuTemp.IsFallback, hardware.GpuTemp.ConfidenceScore);
                if (hardware.GpuTemp.IsFallback) anyFallbackUsed = true;
                totalConfidence += hardware.GpuTemp.ConfidenceScore;
                sensorCount++;
            }

            // Memory
            if (hardware.MemoryUsage.HasValue)
            {
                snapshot.Memory.UsagePercent = SensorReading<double>.FromValue(hardware.MemoryUsage.Value.Value, hardware.MemoryUsage.Source, hardware.MemoryUsage.IsFallback, hardware.MemoryUsage.ConfidenceScore);
                totalConfidence += hardware.MemoryUsage.ConfidenceScore;
                sensorCount++;
            }
            double totalMb = hardware.TotalPhysicalMemoryBytes / (1024.0 * 1024.0);
            double availMb = hardware.AvailablePhysicalMemoryBytes / (1024.0 * 1024.0);
            snapshot.Memory.TotalMb = SensorReading<double>.FromValue(Math.Round(totalMb, 1), "GC.MemoryInfo", isFallback: false, confidenceScore: 100);
            snapshot.Memory.UsedMb = SensorReading<double>.FromValue(Math.Round(Math.Max(0, totalMb - availMb), 1), "GC.MemoryInfo", isFallback: false, confidenceScore: 100);
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
                UsagePercent = SensorReading<double>.FromValue(d.UsagePercentage, "DriveInfo", isFallback: false, confidenceScore: 100),
                HealthPercent = SensorReading<int>.FromValue(100, "SMART", isFallback: false, confidenceScore: 100),
                SmartStatus = "OK"
            }).ToList();
        }

        // Compute overall snapshot confidence average
        int overallConfidence = sensorCount > 0 ? (int)Math.Round((double)totalConfidence / sensorCount) : 100;

        snapshot.Trust = new TrustMetadata
        {
            ConfidenceScore = overallConfidence,
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
