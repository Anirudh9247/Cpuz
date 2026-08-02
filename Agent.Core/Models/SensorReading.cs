using System.Diagnostics.CodeAnalysis;

namespace Agent.Core.Models;

public class SensorReading<T> where T : struct
{
    public T? Value { get; set; }
    public string Source { get; set; } = "Unavailable";
    public bool IsFallback { get; set; } = false;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue => Value.HasValue;

    public static SensorReading<T> Empty(string source = "Unavailable") => new()
    {
        Value = null,
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
