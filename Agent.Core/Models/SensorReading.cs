namespace Agent.Core.Models;

public class SensorReading<T>
{
    public T? Value { get; set; }
    public string Source { get; set; } = "Unavailable";
    public bool IsFallback { get; set; } = false;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public bool HasValue => Value != null;

    public static SensorReading<T> Empty(string source = "Unavailable") => new()
    {
        Value = default,
        Source = source,
        IsFallback = false,
        TimestampUtc = DateTime.UtcNow
    };

    public static SensorReading<T> FromValue(T value, string source, bool isFallback = false) => new()
    {
        Value = value,
        Source = source,
        IsFallback = isFallback,
        TimestampUtc = DateTime.UtcNow
    };
}
