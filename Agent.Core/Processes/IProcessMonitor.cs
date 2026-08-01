using Agent.Core.Models;

namespace Agent.Core.Processes;

public interface IProcessMonitor
{
    Task<List<ProcessInfo>> GetTopProcessesAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<int> GetTotalProcessCountAsync(CancellationToken cancellationToken = default);
    Task<bool> KillProcessByIdAsync(int processId, CancellationToken cancellationToken = default);
}
