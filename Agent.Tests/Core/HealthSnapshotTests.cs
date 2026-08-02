using System.Collections.Concurrent;
using Agent.Core.Alerts;
using Agent.Core.Health;
using Agent.Core.Models;
using Agent.Core.Validation;
using Xunit;

namespace Agent.Tests.Core;

public class HealthSnapshotTests
{
    private readonly AgentConfig _config = new AgentConfig
    {
        CpuWarningTempC = 80.0,
        CpuCriticalTempC = 90.0,
        CpuWarningLoadPercent = 80.0,
        CpuCriticalLoadPercent = 90.0,
        RamWarningPercent = 80.0,
        RamCriticalPercent = 90.0,
        StorageWarningPercent = 85.0,
        StorageCriticalPercent = 95.0,
        GpuWarningTempC = 78.0,
        GpuCriticalTempC = 85.0,
        DefenderAlertEnabled = true
    };

    [Fact]
    public void AlertEngine_GeneratesMultipleAlerts_WhenThresholdsExceeded()
    {
        // Arrange
        var engine = new AlertEngine(_config);
        var snapshot = new HealthSnapshot
        {
            Cpu = new CpuSnapshot
            {
                TempC = SensorReading<double>.FromValue(92.0, "LibreHardwareMonitor"),
                LoadPercent = SensorReading<double>.FromValue(95.0, "LibreHardwareMonitor")
            },
            Memory = new MemorySnapshot
            {
                UsagePercent = SensorReading<double>.FromValue(91.0, "PerformanceCounter")
            },
            Drives = new List<DriveSnapshot>
            {
                new DriveSnapshot
                {
                    Name = "C:",
                    UsagePercent = SensorReading<double>.FromValue(96.0, "DriveInfo")
                }
            },
            Defender = new DefenderSnapshot
            {
                DefenderEnabled = SensorReading<bool>.FromValue(false, "WMI")
            }
        };

        // Act
        var alerts = engine.Evaluate(snapshot);

        // Assert
        Assert.NotNull(alerts);
        Assert.True(alerts.Count >= 4, $"Expected at least 4 alerts, got {alerts.Count}");
        Assert.Contains(alerts, a => a.Category == "CPU" && a.Severity == AlertSeverity.Critical);
        Assert.Contains(alerts, a => a.Category == "Memory" && a.Severity == AlertSeverity.Critical);
        Assert.Contains(alerts, a => a.Category == "Disk" && a.Severity == AlertSeverity.Critical);
        Assert.Contains(alerts, a => a.Category == "Defender" && a.Severity == AlertSeverity.Critical);
    }

    [Fact]
    public void HealthScoreCalculator_ComputesScoreAndStatuses_Correctly()
    {
        // Arrange
        var scoreCalculator = new HealthScoreCalculator();
        var alerts = new List<HealthAlert>
        {
            new HealthAlert { Category = "CPU", Severity = AlertSeverity.Critical, Message = "CPU Temp Critical" },
            new HealthAlert { Category = "Memory", Severity = AlertSeverity.Warning, Message = "RAM High" }
        };
        var snapshot = new HealthSnapshot
        {
            Cpu = new CpuSnapshot { TempC = SensorReading<double>.FromValue(92.0, "LHM") },
            Memory = new MemorySnapshot { UsagePercent = SensorReading<double>.FromValue(82.0, "PC") }
        };

        // Act
        var (score, status) = scoreCalculator.Calculate(snapshot, alerts);

        // Assert
        Assert.Equal(65, score); // 100 - 25 (CPU Critical) - 10 (RAM Warning) = 65
        Assert.Equal(OverallHealthStatus.Warning, status);
        Assert.Equal(OverallHealthStatus.Critical, snapshot.Cpu.Status);
        Assert.Equal(OverallHealthStatus.Warning, snapshot.Memory.Status);
    }

    [Fact]
    public void HealthSnapshotBuilder_ConstructsSnapshotWithSensorMetadata()
    {
        // Arrange
        var alertEngine = new AlertEngine(_config);
        var scoreCalculator = new HealthScoreCalculator();
        var builder = new HealthSnapshotBuilder(alertEngine, scoreCalculator);

        var hw = new HardwareMetrics
        {
            CpuTemp = SensorReading<float>.FromValue(85.0f, "LibreHardwareMonitor.CPU"),
            CpuUsage = SensorReading<float>.FromValue(82.0f, "LibreHardwareMonitor.CPU")
        };

        // Act
        var snapshot = builder.Build(_config, hw, null, 150, null);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(1, snapshot.SchemaVersion);
        Assert.True(snapshot.Sequence > 0);
        Assert.Equal("LibreHardwareMonitor.CPU", snapshot.Cpu.TempC.Source);
        Assert.Equal(85.0, snapshot.Cpu.TempC.Value);
        Assert.Equal(100, snapshot.Trust.ConfidenceScore);
        Assert.False(snapshot.Trust.FallbackUsed);
        Assert.NotEmpty(snapshot.Alerts);
    }

    [Fact]
    public void HealthSnapshotBuilder_IncrementsSequenceNumber_OnEveryBuild()
    {
        // Arrange
        var builder = new HealthSnapshotBuilder(new AlertEngine(_config), new HealthScoreCalculator());

        // Act
        var snapshot1 = builder.Build(_config, null, null, 0, null);
        var snapshot2 = builder.Build(_config, null, null, 0, null);

        // Assert
        Assert.True(snapshot2.Sequence > snapshot1.Sequence);
    }

    [Fact]
    public void HealthSnapshotBuilder_SequenceIsMonotonic_UnderConcurrentBuilds()
    {
        // Arrange
        var builder = new HealthSnapshotBuilder(new AlertEngine(_config), new HealthScoreCalculator());
        var sequenceNumbers = new ConcurrentBag<long>();
        int count = 100;

        // Act
        Parallel.For(0, count, _ =>
        {
            var snapshot = builder.Build(_config, null, null, 0, null);
            sequenceNumbers.Add(snapshot.Sequence);
        });

        // Assert
        Assert.Equal(count, sequenceNumbers.Distinct().Count());
    }

    [Fact]
    public void HealthSnapshotBuilder_CalculatesWeightedConfidence_WhenFallbackIsUsed()
    {
        // Arrange
        var builder = new HealthSnapshotBuilder(new AlertEngine(_config), new HealthScoreCalculator());
        var hw = new HardwareMetrics
        {
            CpuTemp = SensorReading<float>.FromValue(65.0f, "WMI.ThermalZone", isFallback: true, confidenceScore: 85),
            CpuUsage = SensorReading<float>.FromValue(25.0f, "LibreHardwareMonitor.CPU", isFallback: false, confidenceScore: 100)
        };

        // Act
        var snapshot = builder.Build(_config, hw, null, 100, null);

        // Assert
        Assert.True(snapshot.Trust.FallbackUsed);
        Assert.InRange(snapshot.Trust.ConfidenceScore, 80, 99);
    }

    [Fact]
    public void HealthSnapshotValidator_DetectsInvalidSnapshots()
    {
        // Arrange
        var validator = new HealthSnapshotValidator();
        var invalidSnapshot = new HealthSnapshot
        {
            AgentId = "", // Missing AgentId
            MachineName = "TEST",
            Cpu = new CpuSnapshot { LoadPercent = SensorReading<double>.FromValue(150.0, "Test") } // Invalid load % > 100
        };

        // Act
        var result = validator.Validate(invalidSnapshot);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
    }
}
