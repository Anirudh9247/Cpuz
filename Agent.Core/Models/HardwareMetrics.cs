namespace Agent.Core.Models;

public class HardwareMetrics
{
    public SensorReading<float> CpuUsage { get; set; } = SensorReading<float>.Empty();
    public SensorReading<float> CpuTemp { get; set; } = SensorReading<float>.Empty();
    public SensorReading<float> GpuTemp { get; set; } = SensorReading<float>.Empty();
    public SensorReading<float> MemoryUsage { get; set; } = SensorReading<float>.Empty();

    // Backward compatibility helper properties
    public float? CpuTotalUsagePercentage
    {
        get => CpuUsage.Value;
        set => CpuUsage = value.HasValue ? SensorReading<float>.FromValue(value.Value, CpuUsage.Source, CpuUsage.IsFallback) : SensorReading<float>.Empty();
    }

    public float? CpuTempC
    {
        get => CpuTemp.Value;
        set => CpuTemp = value.HasValue ? SensorReading<float>.FromValue(value.Value, CpuTemp.Source, CpuTemp.IsFallback) : SensorReading<float>.Empty();
    }

    public float? GpuTempC
    {
        get => GpuTemp.Value;
        set => GpuTemp = value.HasValue ? SensorReading<float>.FromValue(value.Value, GpuTemp.Source, GpuTemp.IsFallback) : SensorReading<float>.Empty();
    }

    public float? MemoryUsagePercentage
    {
        get => MemoryUsage.Value;
        set => MemoryUsage = value.HasValue ? SensorReading<float>.FromValue(value.Value, MemoryUsage.Source, MemoryUsage.IsFallback) : SensorReading<float>.Empty();
    }

    public int? FanRpm { get; set; }
    public int LogicalProcessorCount { get; set; }
    public long TotalPhysicalMemoryBytes { get; set; }
    public long AvailablePhysicalMemoryBytes { get; set; }
    public TimeSpan SystemUptime { get; set; }
    public string OperatingSystem { get; set; } = Environment.OSVersion.ToString();
    public string CpuArchitecture { get; set; } = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
}
