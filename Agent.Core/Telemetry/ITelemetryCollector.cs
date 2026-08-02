using Agent.Core.Models;

namespace Agent.Core.Telemetry;

public interface ITelemetryCollector
{
    Task<HealthSnapshot> CollectSnapshotAsync(CancellationToken cancellationToken = default);
    Task<SystemTelemetryReport> CollectReportAsync(CancellationToken cancellationToken = default);
}
