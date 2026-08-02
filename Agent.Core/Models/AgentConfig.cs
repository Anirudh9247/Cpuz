namespace Agent.Core.Models;

public class AgentConfig
{
    private string _apiKey = "ComputerDoctorSecretKey123";

    public string AgentId { get; set; } = Guid.NewGuid().ToString();
    public string MachineName { get; set; } = Environment.MachineName;

    // Networking & Security
    public string ServerWebSocketUrl { get; set; } = "ws://localhost:8080/ws";
    public int WebSocketPort { get; set; } = 8080;
    public int UdpDiscoveryPort { get; set; } = 8888;
    public bool EnableUdpDiscovery { get; set; } = true;

    /// <summary>
    /// Preshared API key for WebSocket command authentication.
    /// Prefers the environment variable COMPUTERDOCTOR_API_KEY if present.
    /// Note: Production deployment will replace static pre-shared keys with dynamic per-device pairing tokens in Sprint 3.
    /// </summary>
    public string ApiKey
    {
        get => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPUTERDOCTOR_API_KEY"))
            ? Environment.GetEnvironmentVariable("COMPUTERDOCTOR_API_KEY")!
            : _apiKey;
        set => _apiKey = value;
    }

    // Staggered Polling Intervals (ms)
    public int FastCollectionIntervalMs { get; set; } = 2000;   // CPU, RAM, GPU
    public int SlowCollectionIntervalMs { get; set; } = 30000;  // Battery, Defender, Event Logs, SMART
    public int MetricCollectionIntervalMs { get; set; } = 2000;
    public int TopProcessCount { get; set; } = 10;

    // Feature Flags
    public bool EnableHardwareMonitoring { get; set; } = true;
    public bool EnableProcessMonitoring { get; set; } = true;
    public bool EnableStorageMonitoring { get; set; } = true;

    // Configurable Alert & Health Thresholds
    public double CpuWarningTempC { get; set; } = 80.0;
    public double CpuCriticalTempC { get; set; } = 90.0;
    public double CpuWarningLoadPercent { get; set; } = 80.0;
    public double CpuCriticalLoadPercent { get; set; } = 90.0;

    public double GpuWarningTempC { get; set; } = 78.0;
    public double GpuCriticalTempC { get; set; } = 85.0;

    public double RamWarningPercent { get; set; } = 80.0;
    public double RamCriticalPercent { get; set; } = 90.0;

    public double StorageWarningPercent { get; set; } = 85.0;
    public double StorageCriticalPercent { get; set; } = 95.0;
    public int SsdWarningHealthPercent { get; set; } = 85;
    public int SsdCriticalHealthPercent { get; set; } = 70;

    public bool DefenderAlertEnabled { get; set; } = true;
}
