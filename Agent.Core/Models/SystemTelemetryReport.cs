namespace Agent.Core.Models;

public class SystemTelemetryReport : HealthSnapshot
{
    public List<ProcessInfo>? TopProcesses
    {
        get => Processes.TopProcesses;
        set { if (value != null) Processes.TopProcesses = value; }
    }

    public int TotalRunningProcessesCount
    {
        get => Processes.TotalRunningCount;
        set => Processes.TotalRunningCount = value;
    }
}
