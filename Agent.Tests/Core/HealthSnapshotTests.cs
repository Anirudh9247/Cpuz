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
            Cpu = new CpuSnapshot { TempC = 92.0, LoadPercent = 95.0 }, // 2 CPU alerts
            Memory = new MemorySnapshot { UsagePercent = 91.0 },       // 1 RAM alert
            Drives = new List<DriveSnapshot>
            {
                new DriveSnapshot { Name = "C:", UsagePercent = 96.0 } // 1 Disk alert
            },
            Defender = new DefenderSnapshot { DefenderEnabled = false }  // 1 Defender alert
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
            Cpu = new CpuSnapshot { TempC = 92.0 },
            Memory = new MemorySnapshot { UsagePercent = 82.0 }
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
    public void HealthSnapshotValidator_DetectsInvalidSnapshots()
    {
        // Arrange
        var validator = new HealthSnapshotValidator();
        var invalidSnapshot = new HealthSnapshot
        {
            AgentId = "", // Missing AgentId
            MachineName = "TEST",
            Cpu = new CpuSnapshot { LoadPercent = 150.0 } // Invalid load % > 100
        };

        // Act
        var result = validator.Validate(invalidSnapshot);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
    }

    [Fact]
    public void HealthSnapshotValidator_AcceptsValidSnapshot()
    {
        // Arrange
        var validator = new HealthSnapshotValidator();
        var validSnapshot = new HealthSnapshot
        {
            AgentId = "AGENT-01",
            MachineName = "PC-MAIN",
            TimestampUtc = DateTime.UtcNow,
            Cpu = new CpuSnapshot { TempC = 55.0, LoadPercent = 30.0 },
            Memory = new MemorySnapshot { UsagePercent = 45.0 }
        };

        // Act
        var result = validator.Validate(validSnapshot);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
