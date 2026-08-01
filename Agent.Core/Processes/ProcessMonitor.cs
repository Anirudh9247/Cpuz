using System.Diagnostics;
using Agent.Core.Models;

namespace Agent.Core.Processes;

public class ProcessMonitor : IProcessMonitor
{
    public Task<List<ProcessInfo>> GetTopProcessesAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var result = new List<ProcessInfo>();
        var processes = Process.GetProcesses();

        foreach (var proc in processes)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var info = new ProcessInfo
                {
                    Id = proc.Id,
                    ProcessName = proc.ProcessName,
                    WorkingSetMemoryBytes = proc.WorkingSet64,
                    PrivateMemoryMb = Math.Round(proc.PrivateMemorySize64 / (1024.0 * 1024.0), 2),
                    ThreadCount = proc.Threads.Count
                };

                try
                {
                    info.StartTime = proc.StartTime;
                    info.TotalProcessorTime = proc.TotalProcessorTime;
                }
                catch
                {
                    // Access denied for privileged system processes
                }

                result.Add(info);
            }
            catch
            {
                // Process exited between query and access
            }
            finally
            {
                proc.Dispose();
            }
        }

        var topProcesses = result
            .OrderByDescending(p => p.WorkingSetMemoryBytes)
            .Take(count)
            .ToList();

        return Task.FromResult(topProcesses);
    }

    public Task<int> GetTotalProcessCountAsync(CancellationToken cancellationToken = default)
    {
        int count = Process.GetProcesses().Length;
        return Task.FromResult(count);
    }

    public Task<bool> KillProcessByIdAsync(int processId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var proc = Process.GetProcessById(processId);
            proc.Kill(entireProcessTree: true);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
