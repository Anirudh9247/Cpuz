using Agent.Core.Models;

namespace Agent.Core.Health;

public interface IHealthScoreCalculator
{
    (int HealthScore, OverallHealthStatus Status) Calculate(HealthSnapshot snapshot, List<HealthAlert> alerts);
}
