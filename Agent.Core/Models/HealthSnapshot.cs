namespace Agent.Core.Models;

public class HealthSnapshot
{
    public string AgentId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public int OverallHealthScore { get; set; } = 100;
    public OverallHealthStatus OverallStatus { get; set; } = OverallHealthStatus.Healthy;

    public CpuSnapshot Cpu { get; set; } = new();
    public GpuSnapshot Gpu { get; set; } = new();
    public MemorySnapshot Memory { get; set; } = new();
    public BatterySnapshot Battery { get; set; } = new();
    public List<DriveSnapshot> Drives { get; set; } = new();
    public ProcessSnapshot Processes { get; set; } = new();
    public DefenderSnapshot Defender { get; set; } = new();
    public List<HealthAlert> Alerts { get; set; } = new();
    public TrustMetadata Trust { get; set; } = new();

    // Raw underlying metrics for backward compatibility or deep inspection
    public HardwareMetrics? Hardware { get; set; }
    public StorageMetrics? Storage { get; set; }
}
