using Agent.Core.Models;

namespace Agent.Network.Json;

public class NetworkEnvelope<T>
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "TELEMETRY"; // TELEMETRY, COMMAND, COMMAND_ACK, HEARTBEAT
    public int SchemaVersion { get; set; } = 1;
    public string AgentVersion { get; set; } = "1.0.0";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public T? Payload { get; set; }
}

public class CommandPayload
{
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class CommandAckPayload
{
    public string CommandId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string ResultMessage { get; set; } = string.Empty;
}

public class HeartbeatPayload
{
    public string Status { get; set; } = "PING";
}

public class TelemetryPayloadWrapper
{
    public string MessageType { get; set; } = "HEALTH_SNAPSHOT";
    public string AgentVersion { get; set; } = "1.0.0";
    public HealthSnapshot Snapshot { get; set; } = new();

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
