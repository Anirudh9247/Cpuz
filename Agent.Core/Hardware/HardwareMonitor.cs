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
                    if (!metrics.CpuTempC.HasValue || sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) || sensor.Name.Contains("Max", StringComparison.OrdinalIgnoreCase))
                    {
                        metrics.CpuTempC = (float)Math.Round(sensor.Value.Value, 1);
                    }
                }
                else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) && sensor.Value.HasValue)
                {
                    metrics.CpuTotalUsagePercentage = (float)Math.Round(sensor.Value.Value, 1);
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
                    metrics.GpuTempC = (float)Math.Round(sensor.Value.Value, 1);
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
                    if (!metrics.CpuTempC.HasValue && (sensor.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase) || sensor.Name.Contains("System", StringComparison.OrdinalIgnoreCase)))
                    {
                        metrics.CpuTempC = (float)Math.Round(sensor.Value.Value, 1);
                    }
                }
            }
        }
    }

    private static double? ReadCpuTempFromWmi()
    {
        if (!OperatingSystem.IsWindows()) return null;

        // 1. Try MSAcpi_ThermalZoneTemperature in root\WMI
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

        // 2. Try Win32_PerfFormattedData_Counters_ThermalZoneInformation in root\cimv2
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
                if (obj["Temperature"] is uint kelvin && kelvin > 0)
                {
                    double tempCelsius = kelvin - 273.15;
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
