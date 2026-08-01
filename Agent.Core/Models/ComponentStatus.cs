namespace Agent.Core.Models;

public enum OverallHealthStatus
{
    Good,
    Fair,
    Critical
}

public class CpuTelemetry
{
    public double? TempC { get; set; }
    public double? ClockMhz { get; set; }
    public double? LoadPercent { get; set; }
}

public class GpuTelemetry
{
    public double? TempC { get; set; }
    public double? ClockMhz { get; set; }
}

public class FanTelemetry
{
    public int? Rpm { get; set; }
}

public class ProcessTelemetryItem
{
    public string Name { get; set; } = string.Empty;
    public long Mb { get; set; }
}

public class RamTelemetry
{
    public double? UsagePercent { get; set; }
    public List<ProcessTelemetryItem> TopProcesses { get; set; } = new();
}

public class SsdTelemetry
{
    public int? HealthPercent { get; set; }
    public string SmartStatus { get; set; } = "OK";
    public int? WriteWearPercent { get; set; }
}

public class BatteryTelemetry
{
    public int? HealthPercent { get; set; } = 90;
}

public class SecurityTelemetry
{
    public bool DefenderEnabled { get; set; } = true;
    public bool DefinitionsUpToDate { get; set; } = true;
}

public class SuspiciousProcessTelemetry
{
    public string Name { get; set; } = string.Empty;
    public int Pid { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class SystemLogTelemetry
{
    public bool RecentBsod { get; set; }
    public List<string> DriverFailures { get; set; } = new();
}
