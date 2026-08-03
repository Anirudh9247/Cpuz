using System.Diagnostics;
using Agent.Core.Processes;
using Agent.Core.Models;

namespace Agent.Core.Commands;

public class CommandExecutor : ICommandExecutor
{
    private readonly IProcessMonitor _processMonitor;

    public CommandExecutor(IProcessMonitor processMonitor)
    {
        _processMonitor = processMonitor;
    }

    public async Task<CommandAckPayload> ExecuteCommandAsync(string commandId, string commandName, Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var response = new CommandAckPayload
        {
            CommandId = commandId,
            Command = commandName
        };

        try
        {
            switch (commandName.ToUpperInvariant())
            {
                case "KILL_PROCESS":
                    if (parameters.TryGetValue("processId", out string? pidStr) && int.TryParse(pidStr, out int pid))
                    {
                        bool success = await _processMonitor.KillProcessByIdAsync(pid, cancellationToken);
                        response.Success = success;
                        response.Message = success 
                            ? $"Process PID {pid} was successfully terminated." 
                            : $"Failed to terminate process PID {pid}. Access denied or process exited.";
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = "Missing or invalid 'processId' parameter.";
                    }
                    break;

                case "RESTART_EXPLORER":
                    response.Success = RestartExplorerShell();
                    response.Message = response.Success 
                        ? "Windows Explorer shell (explorer.exe) restarted successfully." 
                        : "Failed to restart Windows Explorer shell.";
                    break;

                case "CLEAR_TEMP_FILES":
                    var (cleanedCount, freedMb) = ClearTemporaryFiles();
                    response.Success = true;
                    response.Message = $"Cleaned {cleanedCount} temporary files, freeing {freedMb:F1} MB of disk space.";
                    break;

                case "FLUSH_DNS":
                    response.Success = FlushDnsCache();
                    response.Message = response.Success 
                        ? "Windows DNS Resolver Cache flushed successfully." 
                        : "Failed to flush DNS Resolver Cache.";
                    break;

                default:
                    response.Success = false;
                    response.Message = $"Unknown command '{commandName}'. Supported: KILL_PROCESS, RESTART_EXPLORER, CLEAR_TEMP_FILES, FLUSH_DNS.";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Execution error: {ex.Message}";
        }
        finally
        {
            sw.Stop();
            response.ExecutionTimeMs = sw.ElapsedMilliseconds;
        }

        return response;
    }

    private static bool RestartExplorerShell()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            foreach (var proc in Process.GetProcessesByName("explorer"))
            {
                try { proc.Kill(); } catch { }
            }

            Process.Start("explorer.exe");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static (int fileCount, double freedMb) ClearTemporaryFiles()
    {
        int count = 0;
        long bytesFreed = 0;
        string tempPath = Path.GetTempPath();

        try
        {
            var dir = new DirectoryInfo(tempPath);
            foreach (var file in dir.GetFiles())
            {
                try
                {
                    long length = file.Length;
                    file.Delete();
                    count++;
                    bytesFreed += length;
                }
                catch
                {
                    // File in use by active system process
                }
            }
        }
        catch { }

        return (count, bytesFreed / (1024.0 * 1024.0));
    }

    private static bool FlushDnsCache()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/flushdns",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
