namespace Agent.Core.Models;

public class HardwareMetrics
{
    public float? CpuTotalUsagePercentage { get; set; }
    public float? CpuTempC { get; set; }
    public float? GpuTempC { get; set; }
    public int? FanRpm { get; set; }
    public int LogicalProcessorCount { get; set; }
    public long TotalPhysicalMemoryBytes { get; set; }
    public long AvailablePhysicalMemoryBytes { get; set; }
    public float? MemoryUsagePercentage { get; set; }
    public TimeSpan SystemUptime { get; set; }
    public string OperatingSystem { get; set; } = Environment.OSVersion.ToString();
    public string CpuArchitecture { get; set; } = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
}
