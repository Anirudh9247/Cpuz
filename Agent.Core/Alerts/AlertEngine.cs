using Agent.Core.Models;
using Microsoft.Extensions.Options;

namespace Agent.Core.Alerts;

public class AlertEngine : IAlertEngine
{
    private readonly AgentConfig _config;

    public AlertEngine(IOptions<AgentConfig> configOptions)
    {
        _config = configOptions.Value;
    }

    public AlertEngine(AgentConfig config)
    {
        _config = config;
    }

    public List<HealthAlert> Evaluate(HealthSnapshot snapshot)
    {
        var alerts = new List<HealthAlert>();
        var now = DateTime.UtcNow;

        // 1. CPU Temperature Evaluation
        if (snapshot.Cpu.TempC.HasValue)
        {
            double temp = snapshot.Cpu.TempC.Value.Value;
            if (temp >= _config.CpuCriticalTempC)
            {
                alerts.Add(new HealthAlert
                {
                    Severity = AlertSeverity.Critical,
                    Category = "CPU",
                    Message = $"CPU temperature ({temp:F1}°C) exceeded critical threshold ({_config.CpuCriticalTempC}°C) [Source: {snapshot.Cpu.TempC.Source}]",
                    TimestampUtc = now
                });
            }
            else if (temp >= _config.CpuWarningTempC)
            {
                alerts.Add(new HealthAlert
                {
                    Severity = AlertSeverity.Warning,
                    Category = "CPU",
                    Message = $"CPU temperature ({temp:F1}°C) exceeded warning threshold ({_config.CpuWarningTempC}°C) [Source: {snapshot.Cpu.TempC.Source}]",
                    TimestampUtc = now
                });
            }
        }

        // 2. CPU Load Evaluation
        if (snapshot.Cpu.LoadPercent.HasValue)
        {
            double load = snapshot.Cpu.LoadPercent.Value.Value;
            if (load >= _config.CpuCriticalLoadPercent)
            {
                alerts.Add(new HealthAlert
                {
                    Severity = AlertSeverity.Critical,
                    Category = "CPU",
                    Message = $"CPU utilization ({load:F1}%) exceeded critical threshold ({_config.CpuCriticalLoadPercent}%) [Source: {snapshot.Cpu.LoadPercent.Source}]",
                    TimestampUtc = now
                });
            }
            else if (load >= _config.CpuWarningLoadPercent)
            {
                alerts.Add(new HealthAlert
                {
                    Severity = AlertSeverity.Warning,
                    Category = "CPU",
                    Message = $"CPU utilization ({load:F1}%) exceeded warning threshold ({_config.CpuWarningLoadPercent}%) [Source: {snapshot.Cpu.LoadPercent.Source}]",
                    TimestampUtc = now
                });
            }
        }

        // 3. Memory Usage Evaluation
        if (snapshot.Memory.UsagePercent.HasValue)
        {
            double ramPercent = snapshot.Memory.UsagePercent.Value.Value;
            if (ramPercent >= _config.RamCriticalPercent)
            {
                alerts.Add(new HealthAlert
                {
                    Severity = AlertSeverity.Critical,
                    Category = "Memory",
                    Message = $"RAM usage ({ramPercent:F1}%) exceeded critical threshold ({_config.RamCriticalPercent}%) [Source: {snapshot.Memory.UsagePercent.Source}]",
                    TimestampUtc = now
                });
            }
            else if (ramPercent >= _config.RamWarningPercent)
            {
                alerts.Add(new HealthAlert
                {
                    Severity = AlertSeverity.Warning,
                    Category = "Memory",
                    Message = $"RAM usage ({ramPercent:F1}%) exceeded warning threshold ({_config.RamWarningPercent}%) [Source: {snapshot.Memory.UsagePercent.Source}]",
                    TimestampUtc = now
                });
            }
        }

        // 4. GPU Temperature Evaluation
        if (snapshot.Gpu.TempC.HasValue)
        {
            double gpuTemp = snapshot.Gpu.TempC.Value.Value;
            if (gpuTemp >= _config.GpuCriticalTempC)
            {
                alerts.Add(new HealthAlert
                {
                    Severity = AlertSeverity.Critical,
                    Category = "GPU",
                    Message = $"GPU temperature ({gpuTemp:F1}°C) exceeded critical threshold ({_config.GpuCriticalTempC}°C) [Source: {snapshot.Gpu.TempC.Source}]",
                    TimestampUtc = now
                });
            }
            else if (gpuTemp >= _config.GpuWarningTempC)
            {
                alerts.Add(new HealthAlert
                {
                    Severity = AlertSeverity.Warning,
                    Category = "GPU",
                    Message = $"GPU temperature ({gpuTemp:F1}°C) exceeded warning threshold ({_config.GpuWarningTempC}°C) [Source: {snapshot.Gpu.TempC.Source}]",
                    TimestampUtc = now
                });
            }
        }

        // 5. Storage Drives Evaluation
        if (snapshot.Drives != null && snapshot.Drives.Count > 0)
        {
            foreach (var drive in snapshot.Drives)
            {
                if (drive.UsagePercent.HasValue)
                {
                    double driveUsage = drive.UsagePercent.Value.Value;
                    if (driveUsage >= _config.StorageCriticalPercent)
                    {
                        alerts.Add(new HealthAlert
                        {
                            Severity = AlertSeverity.Critical,
                            Category = "Disk",
                            Message = $"Drive {drive.Name} disk usage ({driveUsage:F1}%) exceeded critical threshold ({_config.StorageCriticalPercent}%)",
                            TimestampUtc = now
                        });
                    }
                    else if (driveUsage >= _config.StorageWarningPercent)
                    {
                        alerts.Add(new HealthAlert
                        {
                            Severity = AlertSeverity.Warning,
                            Category = "Disk",
                            Message = $"Drive {drive.Name} disk usage ({driveUsage:F1}%) exceeded warning threshold ({_config.StorageWarningPercent}%)",
                            TimestampUtc = now
                        });
                    }
                }

                if (drive.HealthPercent.HasValue && drive.HealthPercent.Value.Value <= _config.SsdCriticalHealthPercent)
                {
                    alerts.Add(new HealthAlert
                    {
                        Severity = AlertSeverity.Critical,
                        Category = "Disk",
                        Message = $"Drive {drive.Name} SMART health status ({drive.HealthPercent.Value.Value}%) is critical",
                        TimestampUtc = now
                    });
                }
            }
        }
        else if (snapshot.Storage != null && snapshot.Storage.OverallStorageUsagePercentage >= _config.StorageCriticalPercent)
        {
            alerts.Add(new HealthAlert
            {
                Severity = AlertSeverity.Critical,
                Category = "Disk",
                Message = $"Overall storage usage ({snapshot.Storage.OverallStorageUsagePercentage:F1}%) exceeded critical threshold ({_config.StorageCriticalPercent}%)",
                TimestampUtc = now
            });
        }

        // 6. Security / Defender Evaluation
        if (_config.DefenderAlertEnabled && snapshot.Defender != null && snapshot.Defender.DefenderEnabled.HasValue && !snapshot.Defender.DefenderEnabled.Value.Value)
        {
            alerts.Add(new HealthAlert
            {
                Severity = AlertSeverity.Critical,
                Category = "Defender",
                Message = "Windows Defender real-time protection is disabled",
                TimestampUtc = now
            });
        }

        return alerts;
    }
}
