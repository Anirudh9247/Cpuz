# ComputerDoctor Agent

**ComputerDoctor Agent** is a modular system telemetry, diagnostic, and monitoring agent built using C# and .NET 8. It periodically harvests CPU, RAM, disk storage, and process metrics, streaming telemetry updates over WebSockets to a centralized management server.

---

## 📁 Architecture & Solution Structure

```text
ComputerDoctor.Agent.sln
│
├── Agent.Core             Domain layer for system metric collectors and telemetry models
│   ├── Hardware           CPU usage, memory consumption, uptime, and OS architecture
│   ├── Storage            Storage drive enumeration, total/free capacity, volume labels
│   ├── Processes          Process enumeration, top RAM/CPU consumer ranking, process kill
│   ├── Telemetry          TelemetryCollector orchestration service
│   └── Models             Telemetry DTOs, hardware metrics, process models, configuration
│
├── Agent.Network          Real-time network and serialization library
│   ├── WebSocket          ClientWebSocket manager with auto-reconnect, ping/pong, send loop
│   └── Json               Optimized System.Text.Json serializers and contract wrappers
│
├── Agent.Service          Executable Host & Windows Background Worker Service
│   └── BackgroundService  AgentWorker orchestration engine and Windows Service host
│
├── Agent.Tests            XUnit unit testing suite
│   ├── Core               Tests for storage calculation and process sorting
│   └── Network            Tests for JSON serialization and payload wrappers
│
├── docs                   Technical documentation
│   ├── Architecture.md    System architecture breakdown
│   ├── API.md             Service API & interface specifications
│   ├── Telemetry.md       Telemetry payload JSON schema
│   └── Progress.md        Project progress & roadmap
│
└── README.md              Technical documentation and operational guide
```

---

## ⚙️ Configuration (`appsettings.json`)

Configure your agent parameters in `Agent.Service/appsettings.json`:

```json
{
  "AgentConfig": {
    "AgentId": "COMPUTER-DOCTOR-AGENT-01",
    "MachineName": "LOCAL-SYSTEM",
    "ServerWebSocketUrl": "ws://localhost:5000/ws/agent",
    "MetricCollectionIntervalMs": 2000,
    "TopProcessCount": 10,
    "EnableHardwareMonitoring": true,
    "EnableProcessMonitoring": true,
    "EnableStorageMonitoring": true
  }
}
```

---

## 🚀 Building & Running

### Requirements
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or Visual Studio 2022 (v17.8+)

### Command Line Instructions

1. **Build Solution:**
   ```bash
   dotnet build ComputerDoctor.Agent.sln -c Release
   ```

2. **Run Tests:**
   ```bash
   dotnet test Agent.Tests/Agent.Tests.csproj
   ```

3. **Run Service Interactively:**
   ```bash
   dotnet run --project Agent.Service/Agent.Service.csproj
   ```

---

## 🔧 Installing as a Windows Service

To install ComputerDoctor Agent as a Windows Background Service running under `LOCAL SYSTEM`:

```powershell
# Publish self-contained executable
dotnet publish Agent.Service/Agent.Service.csproj -c Release -r win-x64 --self-contained true -o C:\Services\ComputerDoctorAgent

# Create Windows Service (Run PowerShell as Administrator)
New-Service -Name "ComputerDoctorAgent" `
            -BinaryPathName "C:\Services\ComputerDoctorAgent\Agent.Service.exe" `
            -DisplayName "ComputerDoctor Telemetry Agent" `
            -StartupType Automatic

# Start Service
Start-Service -Name "ComputerDoctorAgent"
```
