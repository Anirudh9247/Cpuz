using System.Diagnostics.CodeAnalysis;

namespace Agent.Core.Models;

/// <summary>
/// Wraps a sensor metric value with sensor provenance metadata (Source, IsFallback, ConfidenceScore).
/// Note: ConfidenceScore is a static source-reliability tier weight (100 = Ring 0 LHM kernel driver, 85–90 = WMI/PerfCounter, 70 = GC/OS fallback, 0 = Unavailable), not a statistical variance estimate.
/// </summary>
public class SensorReading<T> where T : struct
{
    public T? Value { get; set; }
    public string Source { get; set; } = "Unavailable";
    public bool IsFallback { get; set; } = false;

    /// <summary>
    /// Static source-reliability tier weight (0–100).
    /// </summary>
    public int ConfidenceScore { get; set; } = 0;

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue => Value.HasValue;

    public static SensorReading<T> Empty(string source = "Unavailable") => new()
    {
        Value = null,
        Source = source,
        IsFallback = false,
        ConfidenceScore = 0,
        TimestampUtc = DateTime.UtcNow
    };

    public static SensorReading<T> FromValue(T value, string source, bool isFallback = false, int confidenceScore = 100) => new()
    {
        Value = value,
        Source = source,
        IsFallback = isFallback,
        ConfidenceScore = confidenceScore,
        TimestampUtc = DateTime.UtcNow
    };
}
