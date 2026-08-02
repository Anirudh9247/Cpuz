using Agent.Core.Models;

namespace Agent.Core.Alerts;

public interface IAlertEngine
{
    List<HealthAlert> Evaluate(HealthSnapshot snapshot);
}
