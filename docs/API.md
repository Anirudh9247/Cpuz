# ComputerDoctor.Agent - Service API Specification

## Core Interfaces

### `IHardwareMonitor`
```csharp
namespace Agent.Core.Hardware;

public interface IHardwareMonitor
{
    Task<HardwareMetrics> GetHardwareMetricsAsync(CancellationToken cancellationToken = default);
}
```

### `IProcessMonitor`
```csharp
namespace Agent.Core.Processes;

public interface IProcessMonitor
{
    Task<List<ProcessInfo>> GetTopProcessesAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<int> GetTotalProcessCountAsync(CancellationToken cancellationToken = default);
    Task<bool> KillProcessByIdAsync(int processId, CancellationToken cancellationToken = default);
}
```

### `IStorageMonitor`
```csharp
namespace Agent.Core.Storage;

public interface IStorageMonitor
{
    Task<StorageMetrics> GetStorageMetricsAsync(CancellationToken cancellationToken = default);
}
```

### `ITelemetryCollector`
```csharp
namespace Agent.Core.Telemetry;

public interface ITelemetryCollector
{
    Task<SystemTelemetryReport> CollectReportAsync(CancellationToken cancellationToken = default);
}
```

### `IAgentWebSocketClient`
```csharp
namespace Agent.Network.WebSocket;

public interface IAgentWebSocketClient : IDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(Uri serverUri, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync<T>(T message, CancellationToken cancellationToken = default);
    event EventHandler<string>? MessageReceived;
    event EventHandler? Connected;
    event EventHandler? Disconnected;
}
```
