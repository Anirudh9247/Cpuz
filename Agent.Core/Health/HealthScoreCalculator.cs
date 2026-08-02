using Agent.Core.Models;

namespace Agent.Core.Health;

public class HealthScoreCalculator : IHealthScoreCalculator
{
    public (int HealthScore, OverallHealthStatus Status) Calculate(HealthSnapshot snapshot, List<HealthAlert> alerts)
    {
        int score = 100;

        // Group alerts by category to prevent compounding penalties on a single component
        var alertsByCategory = alerts.GroupBy(a => a.Category, StringComparer.OrdinalIgnoreCase);

        foreach (var categoryGroup in alertsByCategory)
        {
            bool hasCritical = categoryGroup.Any(a => a.Severity == AlertSeverity.Critical);
            bool hasWarning = categoryGroup.Any(a => a.Severity == AlertSeverity.Warning);

            if (hasCritical)
            {
                score -= 25;
            }
            else if (hasWarning)
            {
                score -= 10;
            }
        }

        // Clamp score between 0 and 100
        score = Math.Clamp(score, 0, 100);

        // Assign individual component statuses
        AssignComponentStatuses(snapshot, alerts);

        // Determine overall health status
        OverallHealthStatus status;
        if (score >= 85)
        {
            status = OverallHealthStatus.Healthy;
        }
        else if (score >= 60)
        {
            status = OverallHealthStatus.Warning;
        }
        else
        {
            status = OverallHealthStatus.Critical;
        }

        return (score, status);
    }

    private static void AssignComponentStatuses(HealthSnapshot snapshot, List<HealthAlert> alerts)
    {
        snapshot.Cpu.Status = GetComponentStatus(alerts, "CPU");
        snapshot.Gpu.Status = GetComponentStatus(alerts, "GPU");
        snapshot.Memory.Status = GetComponentStatus(alerts, "Memory");
        snapshot.Defender.Status = GetComponentStatus(alerts, "Defender");

        if (snapshot.Drives != null && snapshot.Drives.Count > 0)
        {
            foreach (var drive in snapshot.Drives)
            {
                drive.Status = alerts.Any(a => a.Category.Equals("Disk", StringComparison.OrdinalIgnoreCase) &&
                                               a.Message.Contains(drive.Name, StringComparison.OrdinalIgnoreCase) &&
                                               a.Severity == AlertSeverity.Critical)
                    ? OverallHealthStatus.Critical
                    : alerts.Any(a => a.Category.Equals("Disk", StringComparison.OrdinalIgnoreCase) &&
                                     a.Message.Contains(drive.Name, StringComparison.OrdinalIgnoreCase) &&
                                     a.Severity == AlertSeverity.Warning)
                        ? OverallHealthStatus.Warning
                        : OverallHealthStatus.Healthy;
            }
        }
    }

    private static OverallHealthStatus GetComponentStatus(List<HealthAlert> alerts, string category)
    {
        var categoryAlerts = alerts.Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        if (categoryAlerts.Any(a => a.Severity == AlertSeverity.Critical))
        {
            return OverallHealthStatus.Critical;
        }
        if (categoryAlerts.Any(a => a.Severity == AlertSeverity.Warning))
        {
            return OverallHealthStatus.Warning;
        }
        return OverallHealthStatus.Healthy;
    }
}
