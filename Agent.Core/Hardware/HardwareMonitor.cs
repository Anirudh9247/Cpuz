using Agent.Core.Models;

namespace Agent.Core.Hardware;

public class HardwareMonitor : IHardwareMonitor, IDisposable
{
    private readonly SensorPipeline _pipeline;

    public HardwareMonitor()
    {
        _pipeline = new SensorPipeline();
    }

    public HardwareMonitor(SensorPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<HardwareMetrics> GetHardwareMetricsAsync(CancellationToken cancellationToken = default)
    {
        return _pipeline.HarvestMetricsAsync(cancellationToken);
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }
}
