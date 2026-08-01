namespace Agent.Core.Models;

public class SystemTelemetryReport
{
    public string AgentId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public HardwareMetrics? Hardware { get; set; }
    public List<ProcessInfo>? TopProcesses { get; set; }
    public StorageMetrics? Storage { get; set; }
    public int TotalRunningProcessesCount { get; set; }
}
