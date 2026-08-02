namespace Agent.Core.Models;

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public class HealthAlert
{
    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
