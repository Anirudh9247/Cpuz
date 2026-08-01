using Agent.Core.Models;

namespace Agent.Core;

public class AlertRuleEngine
{
    private readonly AgentConfig _config;

    public AlertRuleEngine(AgentConfig? config = null)
    {
        _config = config ?? new AgentConfig();
    }

    public void Evaluate(SystemTelemetryReport report)
    {
        if (report.Hardware == null) return;

        int score = 100;

        // CPU Total Usage Evaluation
        if (report.Hardware.CpuTotalUsagePercentage.HasValue && report.Hardware.CpuTotalUsagePercentage.Value > 95)
        {
            score -= 15;
        }

        // Memory Load Evaluation
        if (report.Hardware.MemoryUsagePercentage.HasValue)
        {
            float memoryPercent = report.Hardware.MemoryUsagePercentage.Value;
            if (memoryPercent >= _config.RamCriticalPercent)
            {
                score -= 20;
            }
            else if (memoryPercent >= _config.RamWarningPercent)
            {
                score -= 10;
            }
        }

        // Storage Health Evaluation
        if (report.Storage != null && report.Storage.OverallStorageUsagePercentage > 90)
        {
            score -= 15;
        }
    }
}
