using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Agent.Core.Models;
using LibreHardwareMonitor.Hardware;

namespace Agent.Core.Hardware;

public class HardwareMonitor : IHardwareMonitor, IDisposable
{
    private readonly Computer? _computer;
    private readonly LibreHardwareVisitor? _visitor;
    private readonly bool _isLhmInitialized;
    private PerformanceCounter? _ramCounter;

    public HardwareMonitor()
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
            Debug.WriteLine($"[Sensor Warning] LHM initialization failed: {ex.Message}. Falling back to WMI.");
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

    public Task<HardwareMetrics> GetHardwareMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new HardwareMetrics
        {
            LogicalProcessorCount = Environment.ProcessorCount,
            SystemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            OperatingSystem = RuntimeInformation.OSDescription,
            CpuArchitecture = RuntimeInformation.ProcessArchitecture.ToString()
        };

        if (_isLhmInitialized && _computer != null && _visitor != null)
        {
            try
            {
                _computer.Accept(_visitor);
                ReadFromLhm(metrics);
            }
            catch
            {
                // Fallback to WMI if runtime traversal fails
            }
        }

        // Fill metric gaps via WMI if LHM didn't populate critical values
        if (!metrics.CpuTempC.HasValue)
            metrics.CpuTempC = (float?)ReadCpuTempFromWmi();

        if (!metrics.CpuTotalUsagePercentage.HasValue)
            metrics.CpuTotalUsagePercentage = (float?)ReadCpuLoadFromWmi();

        // Memory info
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
            }
            catch
            {
                metrics.AvailablePhysicalMemoryBytes = metrics.TotalPhysicalMemoryBytes - gcInfo.HeapSizeBytes;
            }
        }
        else
        {
            metrics.AvailablePhysicalMemoryBytes = metrics.TotalPhysicalMemoryBytes - gcInfo.HeapSizeBytes;
        }

        long usedMemory = metrics.TotalPhysicalMemoryBytes - metrics.AvailablePhysicalMemoryBytes;
        metrics.MemoryUsagePercentage = metrics.TotalPhysicalMemoryBytes > 0
            ? (float)(((double)usedMemory / metrics.TotalPhysicalMemoryBytes) * 100.0)
            : null;

        return Task.FromResult(metrics);
    }

    private void ReadFromLhm(HardwareMetrics metrics)
    {
        if (_computer == null) return;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                var tempSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Value.HasValue);
                if (tempSensor?.Value != null)
                    metrics.CpuTempC = (float)Math.Round(tempSensor.Value.Value, 1);

                var loadSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total") && s.Value.HasValue);
                if (loadSensor?.Value != null)
                    metrics.CpuTotalUsagePercentage = (float)Math.Round(loadSensor.Value.Value, 1);
            }

            if (hardware.HardwareType == HardwareType.GpuNvidia || 
                hardware.HardwareType == HardwareType.GpuAmd || 
                hardware.HardwareType == HardwareType.GpuIntel)
            {
                var gpuTemp = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Value.HasValue);
                if (gpuTemp?.Value != null)
                    metrics.GpuTempC = (float)Math.Round(gpuTemp.Value.Value, 1);

                var fanSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Fan && s.Value.HasValue);
                if (fanSensor?.Value != null)
                    metrics.FanRpm = (int)fanSensor.Value.Value;
            }
        }
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
