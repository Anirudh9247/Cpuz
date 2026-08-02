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

    // Cache timestamps for slow sensors
    private DateTime _lastWmiCpuTempCheck = DateTime.MinValue;
    private float? _cachedWmiCpuTemp = null;

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

    public Task<HardwareMetrics> HarvestMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new HardwareMetrics
        {
            LogicalProcessorCount = Environment.ProcessorCount,
            SystemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            OperatingSystem = RuntimeInformation.OSDescription,
            CpuArchitecture = RuntimeInformation.ProcessArchitecture.ToString()
        };

        // 1. Primary Source: LibreHardwareMonitor Traversal
        if (_isLhmInitialized && _computer != null && _visitor != null)
        {
            try
            {
                _computer.Accept(_visitor);
                ReadFromLhm(metrics);
            }
            catch
            {
                // Primary LHM traversal exception handled safely
            }
        }

        // 2. Field-by-Field Fallback Pipeline

        // CPU Temperature Fallback (LHM -> WMI)
        if (!metrics.CpuTemp.HasValue)
        {
            float? wmiTemp = ReadCpuTempFromWmiWithCache();
            if (wmiTemp.HasValue)
            {
                metrics.CpuTemp = SensorReading<float>.FromValue(wmiTemp.Value, "WMI.ThermalZone", isFallback: true);
            }
        }

        // CPU Usage Fallback (LHM -> WMI)
        if (!metrics.CpuUsage.HasValue)
        {
            float? wmiLoad = ReadCpuLoadFromWmi();
            if (wmiLoad.HasValue)
            {
                metrics.CpuUsage = SensorReading<float>.FromValue(wmiLoad.Value, "WMI.Win32_Processor", isFallback: true);
            }
        }

        // Memory Usage Calculation
        var gcInfo = GC.GetGCMemoryInfo();
        metrics.TotalPhysicalMemoryBytes = gcInfo.TotalAvailableMemoryBytes > 0
            ? gcInfo.TotalAvailableMemoryBytes
            : 8L * 1024 * 1024 * 1024;

        if (OperatingSystem.IsWindows() && _ramCounter != null)
        {
            try
            {
                float availableMb = _ramCounter.NextValue();
                metrics.AvailablePhysicalMemoryBytes = (long)(availableMb * 1024 * 1024);
                long usedMemoryBytes = metrics.TotalPhysicalMemoryBytes - metrics.AvailablePhysicalMemoryBytes;
                float memUsagePercent = (float)(((double)usedMemoryBytes / metrics.TotalPhysicalMemoryBytes) * 100.0);

                metrics.MemoryUsage = SensorReading<float>.FromValue(
                    (float)Math.Round(memUsagePercent, 1),
                    "PerformanceCounter.Memory",
                    isFallback: false);
            }
            catch
            {
                metrics.AvailablePhysicalMemoryBytes = metrics.TotalPhysicalMemoryBytes - gcInfo.HeapSizeBytes;
            }
        }
        else
        {
            metrics.AvailablePhysicalMemoryBytes = metrics.TotalPhysicalMemoryBytes - gcInfo.HeapSizeBytes;
            long usedMemoryBytes = metrics.TotalPhysicalMemoryBytes - metrics.AvailablePhysicalMemoryBytes;
            float memUsagePercent = (float)(((double)usedMemoryBytes / metrics.TotalPhysicalMemoryBytes) * 100.0);

            metrics.MemoryUsage = SensorReading<float>.FromValue((float)Math.Round(memUsagePercent, 1), "GC.MemoryInfo", isFallback: true);
        }

        return Task.FromResult(metrics);
    }

    private void ReadFromLhm(HardwareMetrics metrics)
    {
        if (_computer == null) return;

        foreach (var hardware in _computer.Hardware)
        {
            InspectHardwareForSensors(hardware, metrics);
            foreach (var sub in hardware.SubHardware)
            {
                InspectHardwareForSensors(sub, metrics);
            }
        }
    }

    private static void InspectHardwareForSensors(IHardware hardware, HardwareMetrics metrics)
    {
        if (hardware.HardwareType == HardwareType.Cpu)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && sensor.Value.Value > 0)
                {
                    if (!metrics.CpuTemp.HasValue || sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) || sensor.Name.Contains("Max", StringComparison.OrdinalIgnoreCase))
                    {
                        float val = (float)Math.Round(sensor.Value.Value, 1);
                        metrics.CpuTemp = SensorReading<float>.FromValue(val, "LibreHardwareMonitor.CPU", isFallback: false);
                    }
                }
                else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) && sensor.Value.HasValue)
                {
                    float val = (float)Math.Round(sensor.Value.Value, 1);
                    metrics.CpuUsage = SensorReading<float>.FromValue(val, "LibreHardwareMonitor.CPU", isFallback: false);
                }
            }
        }
        else if (hardware.HardwareType == HardwareType.GpuNvidia ||
                 hardware.HardwareType == HardwareType.GpuAmd ||
                 hardware.HardwareType == HardwareType.GpuIntel)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && sensor.Value.Value > 0)
                {
                    float val = (float)Math.Round(sensor.Value.Value, 1);
                    metrics.GpuTemp = SensorReading<float>.FromValue(val, $"LibreHardwareMonitor.{hardware.HardwareType}", isFallback: false);
                }
                else if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                {
                    metrics.FanRpm = (int)sensor.Value.Value;
                }
            }
        }
        else if (hardware.HardwareType == HardwareType.Motherboard)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue && sensor.Value.Value > 0)
                {
                    if (!metrics.CpuTemp.HasValue && (sensor.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase) || sensor.Name.Contains("System", StringComparison.OrdinalIgnoreCase)))
                    {
                        float val = (float)Math.Round(sensor.Value.Value, 1);
                        metrics.CpuTemp = SensorReading<float>.FromValue(val, "LibreHardwareMonitor.Motherboard", isFallback: false);
                    }
                }
            }
        }
    }

    private float? ReadCpuTempFromWmiWithCache()
    {
        // TTL cache 2.0 seconds for WMI thermal query
        if ((DateTime.UtcNow - _lastWmiCpuTempCheck).TotalSeconds < 2.0)
        {
            return _cachedWmiCpuTemp;
        }

        _lastWmiCpuTempCheck = DateTime.UtcNow;
        _cachedWmiCpuTemp = ReadCpuTempFromWmi();
        return _cachedWmiCpuTemp;
    }

    private static float? ReadCpuTempFromWmi()
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
                        return (float)Math.Round(tempCelsius, 1);
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
                        return (float)Math.Round(tempCelsius, 1);
                }
            }
        }
        catch { }

        return null;
    }

    private static float? ReadCpuLoadFromWmi()
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
