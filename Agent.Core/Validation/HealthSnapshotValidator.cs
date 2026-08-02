using Agent.Core.Models;

namespace Agent.Core.Validation;

public class HealthSnapshotValidator : IHealthSnapshotValidator
{
    public SnapshotValidationResult Validate(HealthSnapshot snapshot)
    {
        var result = new SnapshotValidationResult();

        if (snapshot == null)
        {
            result.Errors.Add("HealthSnapshot instance is null.");
            return result;
        }

        // 1. Validate Timestamp
        if (snapshot.TimestampUtc == default || snapshot.TimestampUtc == DateTime.MinValue)
        {
            result.Errors.Add("HealthSnapshot timestamp is invalid or default.");
        }
        else if (snapshot.TimestampUtc > DateTime.UtcNow.AddMinutes(5))
        {
            result.Errors.Add("HealthSnapshot timestamp is in the future.");
        }

        // 2. Validate Identity Metadata
        if (string.IsNullOrWhiteSpace(snapshot.AgentId))
        {
            result.Errors.Add("AgentId missing in HealthSnapshot.");
        }

        if (string.IsNullOrWhiteSpace(snapshot.MachineName))
        {
            result.Errors.Add("MachineName missing in HealthSnapshot.");
        }

        // 3. Validate CPU Metrics
        if (snapshot.Cpu.LoadPercent.HasValue)
        {
            double cpuLoad = snapshot.Cpu.LoadPercent.Value!;
            if (double.IsNaN(cpuLoad) || double.IsInfinity(cpuLoad) || cpuLoad < 0.0 || cpuLoad > 100.0)
            {
                result.Errors.Add($"CPU load percentage ({cpuLoad}) is out of valid range [0, 100].");
            }
        }

        if (snapshot.Cpu.TempC.HasValue)
        {
            double cpuTemp = snapshot.Cpu.TempC.Value!;
            if (double.IsNaN(cpuTemp) || double.IsInfinity(cpuTemp) || cpuTemp < -50.0 || cpuTemp > 200.0)
            {
                result.Errors.Add($"CPU temperature ({cpuTemp}°C) is physically invalid.");
            }
        }

        // 4. Validate Memory Metrics
        if (snapshot.Memory.UsagePercent.HasValue)
        {
            double ramPercent = snapshot.Memory.UsagePercent.Value!;
            if (double.IsNaN(ramPercent) || double.IsInfinity(ramPercent) || ramPercent < 0.0 || ramPercent > 100.0)
            {
                result.Errors.Add($"Memory usage percentage ({ramPercent}) is out of valid range [0, 100].");
            }
        }

        // 5. Validate Drives Metrics
        if (snapshot.Drives != null)
        {
            foreach (var drive in snapshot.Drives)
            {
                if (drive.UsagePercent.HasValue)
                {
                    double driveUsage = drive.UsagePercent.Value!;
                    if (driveUsage < 0.0 || driveUsage > 100.0)
                    {
                        result.Errors.Add($"Drive {drive.Name} usage percentage ({driveUsage}) is out of valid range [0, 100].");
                    }
                }
            }
        }

        return result;
    }
}
