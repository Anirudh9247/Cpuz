using Agent.Core.Models;

namespace Agent.Core.Validation;

public class SnapshotValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();
}

public interface IHealthSnapshotValidator
{
    SnapshotValidationResult Validate(HealthSnapshot snapshot);
}
