using Agent.Core.Models;

namespace Agent.Core.Storage;

public interface IStorageMonitor
{
    Task<StorageMetrics> GetStorageMetricsAsync(CancellationToken cancellationToken = default);
}
