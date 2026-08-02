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
    public SensorReading<double> TempC { get; set; } = SensorReading<double>.Empty();
    public SensorReading<double> LoadPercent { get; set; } = SensorReading<double>.Empty();
    public SensorReading<double> ClockMhz { get; set; } = SensorReading<double>.Empty();
    public int LogicalProcessorCount { get; set; }
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class GpuSnapshot
{
    public SensorReading<double> TempC { get; set; } = SensorReading<double>.Empty();
    public SensorReading<double> LoadPercent { get; set; } = SensorReading<double>.Empty();
    public SensorReading<double> ClockMhz { get; set; } = SensorReading<double>.Empty();
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class MemorySnapshot
{
    public SensorReading<double> UsagePercent { get; set; } = SensorReading<double>.Empty();
    public SensorReading<double> UsedMb { get; set; } = SensorReading<double>.Empty();
    public SensorReading<double> TotalMb { get; set; } = SensorReading<double>.Empty();
    public List<ProcessInfo> TopProcesses { get; set; } = new();
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class BatterySnapshot
{
    public SensorReading<int> HealthPercent { get; set; } = SensorReading<int>.FromValue(100, "Windows.Power");
    public SensorReading<int> ChargePercent { get; set; } = SensorReading<int>.FromValue(100, "Windows.Power");
    public SensorReading<bool> IsPluggedIn { get; set; } = SensorReading<bool>.FromValue(true, "Windows.Power");
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class DriveSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double TotalSizeGb { get; set; }
    public double FreeSpaceGb { get; set; }
    public SensorReading<double> UsagePercent { get; set; } = SensorReading<double>.Empty();
    public SensorReading<int> HealthPercent { get; set; } = SensorReading<int>.FromValue(100, "SMART");
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
    public SensorReading<bool> DefenderEnabled { get; set; } = SensorReading<bool>.FromValue(true, "WMI.SecurityCenter");
    public SensorReading<bool> DefinitionsUpToDate { get; set; } = SensorReading<bool>.FromValue(true, "WMI.SecurityCenter");
    public SensorReading<bool> RealTimeProtectionEnabled { get; set; } = SensorReading<bool>.FromValue(true, "WMI.SecurityCenter");
    public OverallHealthStatus Status { get; set; } = OverallHealthStatus.Healthy;
}

public class SuspiciousProcessTelemetry
{
    public string Name { get; set; } = string.Empty;
    public int Pid { get; set; }
    public string Reason { get; set; } = string.Empty;
}
