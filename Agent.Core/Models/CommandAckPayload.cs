namespace Agent.Core.Models;

public class CommandAckPayload
{
    public string CommandId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool Success { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string Message { get; set; } = string.Empty;
}
