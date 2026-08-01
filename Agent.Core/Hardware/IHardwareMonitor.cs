using Agent.Core.Models;

namespace Agent.Core.Hardware;

public interface IHardwareMonitor
{
    Task<HardwareMetrics> GetHardwareMetricsAsync(CancellationToken cancellationToken = default);
}
