using Agent.Core.Models;

namespace Agent.Network.Json;

public class TelemetryPayloadWrapper
{
    public string MessageType { get; set; } = "TELEMETRY_REPORT";
    public string AgentVersion { get; set; } = "1.0.0";
    public SystemTelemetryReport Report { get; set; } = new();
}

public class CommandMessage
{
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}
