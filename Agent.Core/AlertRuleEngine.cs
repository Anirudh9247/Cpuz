using Agent.Core.Alerts;
using Agent.Core.Models;

namespace Agent.Core;

[Obsolete("Use IAlertEngine / AlertEngine in Agent.Core.Alerts namespace instead.")]
public class AlertRuleEngine
{
    private readonly AlertEngine _engine;

    public AlertRuleEngine(AgentConfig? config = null)
    {
        _engine = new AlertEngine(config ?? new AgentConfig());
    }

    public void Evaluate(SystemTelemetryReport report)
    {
        report.Alerts = _engine.Evaluate(report);
    }
}
