using Agent.Core.Models;

namespace Agent.Core.Hardware;

public interface ISensorPipeline
{
    Task<SensorReading<double>> ReadCpuTempAsync(CancellationToken cancellationToken = default);
    Task<SensorReading<double>> ReadCpuLoadAsync(CancellationToken cancellationToken = default);
    Task<SensorReading<double>> ReadGpuTempAsync(CancellationToken cancellationToken = default);
    Task<SensorReading<double>> ReadGpuLoadAsync(CancellationToken cancellationToken = default);
    Task<SensorReading<double>> ReadMemoryUsageAsync(CancellationToken cancellationToken = default);
    Task<HardwareMetrics> HarvestMetricsAsync(CancellationToken cancellationToken = default);
}
