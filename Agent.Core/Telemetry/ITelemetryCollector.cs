using Agent.Core.Models;

namespace Agent.Core.Telemetry;

public interface ITelemetryCollector
{
    Task<SystemTelemetryReport> CollectReportAsync(CancellationToken cancellationToken = default);
}
