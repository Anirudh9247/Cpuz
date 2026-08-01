namespace Agent.Core.Models;

public class ProcessInfo
{
    public int Id { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public long WorkingSetMemoryBytes { get; set; }
    public double PrivateMemoryMb { get; set; }
    public int ThreadCount { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan TotalProcessorTime { get; set; }
    public string Status { get; set; } = "Running";
}
