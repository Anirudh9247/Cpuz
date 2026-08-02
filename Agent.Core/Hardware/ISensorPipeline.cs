using Agent.Core.Models;

namespace Agent.Core.Hardware;

public interface ISensorPipeline
{
    Task<HardwareMetrics> HarvestMetricsAsync(CancellationToken cancellationToken = default);
}
