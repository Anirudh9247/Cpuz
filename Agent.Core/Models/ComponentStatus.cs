namespace Agent.Core.Models;

public enum OverallHealthStatus
{
    Healthy,
    Warning,
    Critical
}

public class TrustMetadata
{
    public int ConfidenceScore { get; set; } = 100;
    public string SensorSource { get; set; } = "LibreHardwareMonitor";
    public bool FallbackUsed { get; set; } = false;
}

public class CpuSnapshot
{
    public double? TempC { get; set; }
    public double? LoadPercent { get; set; }
    public double? ClockMhz { get; set; }
    public int LogicalProcessorCount { get; set; }
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class GpuSnapshot
{
    public double? TempC { get; set; }
    public double? LoadPercent { get; set; }
    public double? ClockMhz { get; set; }
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class MemorySnapshot
{
    public double? UsagePercent { get; set; }
    public double UsedMb { get; set; }
    public double TotalMb { get; set; }
    public List<ProcessInfo> TopProcesses { get; set; } = new();
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class BatterySnapshot
{
    public int? HealthPercent { get; set; } = 100;
    public int? ChargePercent { get; set; } = 100;
    public bool IsPluggedIn { get; set; } = true;
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class DriveSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double TotalSizeGb { get; set; }
    public double FreeSpaceGb { get; set; }
    public double UsagePercent { get; set; }
    public int? HealthPercent { get; set; } = 100;
    public string SmartStatus { get; set; } = "OK";
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class ProcessSnapshot
{
    public int TotalRunningCount { get; set; }
    public List<ProcessInfo> TopProcesses { get; set; } = new();
    public List<SuspiciousProcessTelemetry> SuspiciousProcesses { get; set; } = new();
}

public class DefenderSnapshot
{
    public bool DefenderEnabled { get; set; } = true;
    public bool DefinitionsUpToDate { get; set; } = true;
    public bool RealTimeProtectionEnabled { get; set; } = true;
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class SuspiciousProcessTelemetry
{
    public string Name { get; set; } = string.Empty;
    public int Pid { get; set; }
    public string Reason { get; set; } = string.Empty;
}
