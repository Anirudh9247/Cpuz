using Agent.Core.Models;

namespace Agent.Network.Json;

public class TelemetryPayloadWrapper
{
    public string MessageType { get; set; } = "HEALTH_SNAPSHOT";
    public string AgentVersion { get; set; } = "1.0.0";
    public HealthSnapshot Snapshot { get; set; } = new();

    // Legacy compatibility property
    public SystemTelemetryReport Report
    {
        get => Snapshot is SystemTelemetryReport sysReport ? sysReport : new SystemTelemetryReport { AgentId = Snapshot.AgentId, MachineName = Snapshot.MachineName, Hardware = Snapshot.Hardware, Storage = Snapshot.Storage };
        set => Snapshot = value;
    }
}

public class CommandMessage
{
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}
