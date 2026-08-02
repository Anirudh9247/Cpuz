using Agent.Core.Models;

namespace Agent.Core.Health;

public interface IHealthSnapshotBuilder
{
    HealthSnapshot Build(
        AgentConfig config,
        HardwareMetrics? hardware,
        List<ProcessInfo>? topProcesses,
        int totalProcessCount,
        StorageMetrics? storage);
}
