using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Agent.Core.Models;
using LibreHardwareMonitor.Hardware;

namespace Agent.Core.Hardware;

public class SensorPipeline : ISensorPipeline, IDisposable
{
    private readonly Computer? _computer;
    private readonly LibreHardwareVisitor? _visitor;
    private readonly bool _isLhmInitialized;
    private PerformanceCounter? _ramCounter;

    // Cache timestamps for slow fallback sensors (Short recovery TTL: 5s)
    private DateTime _lastWmiCpuTempCheck = DateTime.MinValue;
    private double? _cachedWmiCpuTemp = null;

    public SensorPipeline()
    {
        _visitor = new LibreHardwareVisitor();
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true
        };

        try
        {
            _computer.Open();
            _isLhmInitialized = true;
        }
        catch (Exception ex)
        {
            _isLhmInitialized = false;
            _computer = null;
            Debug.WriteLine($"[Sensor Pipeline] LHM initialization failed: {ex.Message}. Pipeline will fallback to WMI.");
        }

        if (OperatingSystem.IsWindows())
        {
            try
            {
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch
            {
                _ramCounter = null;
            }
        }
    }

    public Task<SensorReading<double>> ReadCpuTempAsync(CancellationToken cancellationToken = default)
    {
        // 1. Primary: LHM
        if (_isLhmInitialized && _computer != null && _visitor != null)
        {
            try
            {
                _computer.Accept(_visitor);
                var lhmReading = GetLhmCpuTemp();
                if (lhmReading.HasValue) return Task.FromResult(lhmReading);
            }
            catch { }
        }

        // 2. Secondary Fallback: WMI (5s Recovery TTL)
        double? wmiTemp = ReadCpuTempFromWmiWithCache();
        if (wmiTemp.HasValue)
        {
            return Task.FromResult(SensorReading<double>.FromValue(wmiTemp.Value, "WMI.ThermalZone", isFallback: true, confidenceScore: 85));
        }

        return Task.FromResult(SensorReading<double>.Empty("Unavailable"));
    }

    public Task<SensorReading<double>> ReadCpuLoadAsync(CancellationToken cancellationToken = default)
    {
        if (_isLhmInitialized && _computer != null && _visitor != null)
        {
            try
            {
                _computer.Accept(_visitor);
                var lhmReading = GetLhmCpuLoad();
                if (lhmReading.HasValue) return Task.FromResult(lhmReading);
            }
            catch { }
        }

        double? wmiLoad = ReadCpuLoadFromWmi();
        if (wmiLoad.HasValue)
        {
            return Task.FromResult(SensorReading<double>.FromValue(wmiLoad.Value, "WMI.Win32_Processor", isFallback: true, confidenceScore: 85));
        }

        return Task.FromResult(SensorReading<double>.Empty("Unavailable"));
    }

    public Task<SensorReading<double>> ReadGpuTempAsync(CancellationToken cancellationToken = default)
    {
        if (_isLhmInitialized && _computer != null && _visitor != null)
        {
            try
            {
                _computer.Accept(_visitor);
                var lhmReading = GetLhmGpuTemp();
                if (lhmReading.HasValue) return Task.FromResult(lhmReading);
            }
            catch { }
        }

        return Task.FromResult(SensorReading<double>.Empty("Unavailable"));
    }

    public Task<SensorReading<double>> ReadMemoryUsageAsync(CancellationToken cancellationToken = default)
    {
        var gcInfo = GC.GetGCMemoryInfo();
        long totalBytes = gcInfo.TotalAvailableMemoryBytes > 0 ? gcInfo.TotalAvailableMemoryBytes : 8L * 1024 * 1024 * 1024;
        long availBytes;

        if (OperatingSystem.IsWindows() && _ramCounter != null)
        {
            try
            {
                float availableMb = _ramCounter.NextValue();
                availBytes = (long)(availableMb * 1024 * 1024);
                long usedBytes = totalBytes - availBytes;
                double memPercent = Math.Round(((double)usedBytes / totalBytes) * 100.0, 1);
                return Task.FromResult(SensorReading<double>.FromValue(memPercent, "PerformanceCounter.Memory", isFallback: false, confidenceScore: 90));
            }
            catch
            {
                availBytes = totalBytes - gcInfo.HeapSizeBytes;
            }
        }
        else
        {
            availBytes = totalBytes - gcInfo.HeapSizeBytes;
        }

        long usedRam = totalBytes - availBytes;
        double usagePercent = Math.Round(((double)usedRam / totalBytes) * 100.0, 1);
        return Task.FromResult(SensorReading<double>.FromValue(usagePercent, "GC.MemoryInfo", isFallback: true, confidenceScore: 70));
    }

    public async Task<HardwareMetrics> HarvestMetricsAsync(CancellationToken cancellationToken = default)
    {
        var cpuTemp = await ReadCpuTempAsync(cancellationToken);
        var cpuLoad = await ReadCpuLoadAsync(cancellationToken);
        var gpuTemp = await ReadGpuTempAsync(cancellationToken);
        var memUsage = await ReadMemoryUsageAsync(cancellationToken);

        var gcInfo = GC.GetGCMemoryInfo();
        long totalBytes = gcInfo.TotalAvailableMemoryBytes > 0 ? gcInfo.TotalAvailableMemoryBytes : 8L * 1024 * 1024 * 1024;
        long availBytes = (long)((1.0 - (memUsage.Value ?? 0.0) / 100.0) * totalBytes);

        return new HardwareMetrics
        {
            CpuTemp = cpuTemp.HasValue ? SensorReading<float>.FromValue((float)cpuTemp.Value.Value, cpuTemp.Source, cpuTemp.IsFallback, cpuTemp.ConfidenceScore) : SensorReading<float>.Empty(cpuTemp.Source),
            CpuUsage = cpuLoad.HasValue ? SensorReading<float>.FromValue((float)cpuLoad.Value.Value, cpuLoad.Source, cpuLoad.IsFallback, cpuLoad.ConfidenceScore) : SensorReading<float>.Empty(cpuLoad.Source),
            GpuTemp = gpuTemp.HasValue ? SensorReading<float>.FromValue((float)gpuTemp.Value.Value, gpuTemp.Source, gpuTemp.IsFallback, gpuTemp.ConfidenceScore) : SensorReading<float>.Empty(gpuTemp.Source),
            MemoryUsage = memUsage.HasValue ? SensorReading<float>.FromValue((float)memUsage.Value.Value, memUsage.Source, memUsage.IsFallback, memUsage.ConfidenceScore) : SensorReading<float>.Empty(memUsage.Source),
            LogicalProcessorCount = Environment.ProcessorCount,
            TotalPhysicalMemoryBytes = totalBytes,
            AvailablePhysicalMemoryBytes = availBytes,
            SystemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            OperatingSystem = RuntimeInformation.OSDescription,
            CpuArchitecture = RuntimeInformation.ProcessArchitecture.ToString()
        };
    }

    private SensorReading<double> GetLhmCpuTemp()
    {
        if (_computer == null) return SensorReading<double>.Empty();

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && sensor.Value.Value > 0)
                    {
                        return SensorReading<double>.FromValue(Math.Round(sensor.Value.Value, 1), "LibreHardwareMonitor.CPU", isFallback: false, confidenceScore: 100);
                    }
                }
            }
        }
        return SensorReading<double>.Empty();
    }

    private SensorReading<double> GetLhmCpuLoad()
    {
        if (_computer == null) return SensorReading<double>.Empty();

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) && sensor.Value.HasValue)
                    {
                        return SensorReading<double>.FromValue(Math.Round(sensor.Value.Value, 1), "LibreHardwareMonitor.CPU", isFallback: false, confidenceScore: 100);
                    }
                }
            }
        }
        return SensorReading<double>.Empty();
    }

    private SensorReading<double> GetLhmGpuTemp()
    {
        if (_computer == null) return SensorReading<double>.Empty();

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuIntel)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && sensor.Value.Value > 0)
                    {
                        return SensorReading<double>.FromValue(Math.Round(sensor.Value.Value, 1), $"LibreHardwareMonitor.{hardware.HardwareType}", isFallback: false, confidenceScore: 100);
                    }
                }
            }
        }
        return SensorReading<double>.Empty();
    }

    private double? ReadCpuTempFromWmiWithCache()
    {
        if ((DateTime.UtcNow - _lastWmiCpuTempCheck).TotalSeconds < 5.0)
        {
            return _cachedWmiCpuTemp;
        }

        _lastWmiCpuTempCheck = DateTime.UtcNow;
        _cachedWmiCpuTemp = ReadCpuTempFromWmi();
        return _cachedWmiCpuTemp;
    }

    private static double? ReadCpuTempFromWmi()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentTemperature"] is uint kelvinTenths && kelvinTenths > 0)
                {
                    double tempCelsius = (kelvinTenths / 10.0) - 273.15;
                    if (tempCelsius > 0 && tempCelsius < 125)
                        return Math.Round(tempCelsius, 1);
                }
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\cimv2", "SELECT HighPrecisionTemperature, Temperature FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["HighPrecisionTemperature"] is uint kelvinTenths && kelvinTenths > 0)
                {
                    double tempCelsius = (kelvinTenths / 10.0) - 273.15;
                    if (tempCelsius > 0 && tempCelsius < 125)
                        return Math.Round(tempCelsius, 1);
                }
            }
        }
        catch { }

        return null;
    }

    private static double? ReadCpuLoadFromWmi()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["LoadPercentage"] is ushort load)
                    return load;
            }
        }
        catch { }
        return null;
    }

    public void Dispose()
    {
        if (_isLhmInitialized && _computer != null)
        {
            try { _computer.Close(); } catch { }
        }
        _ramCounter?.Dispose();
        GC.SuppressFinalize(this);
    }
}
