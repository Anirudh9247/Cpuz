# ComputerDoctor.Agent - Telemetry Schema Specification

## `SystemTelemetryReport` Schema

```json
{
  "messageType": "TELEMETRY_REPORT",
  "agentVersion": "1.0.0",
  "report": {
    "agentId": "COMPUTER-DOCTOR-AGENT-01",
    "machineName": "LOCAL-SYSTEM",
    "timestampUtc": "2026-08-01T13:40:00Z",
    "hardware": {
      "cpuTotalUsagePercentage": 24.5,
      "logicalProcessorCount": 8,
      "totalPhysicalMemoryBytes": 17179869184,
      "availablePhysicalMemoryBytes": 8589934592,
      "memoryUsagePercentage": 50.0,
      "systemUptime": "02:15:30",
      "operatingSystem": "Microsoft Windows 11 Pro",
      "cpuArchitecture": "X64"
    },
    "topProcesses": [
      {
        "id": 1420,
        "processName": "devenv",
        "workingSetMemoryBytes": 524288000,
        "privateMemoryMb": 500.0,
        "threadCount": 42
      }
    ],
    "storage": {
      "drives": [
        {
          "name": "C:\\",
          "label": "OSDisk",
          "driveFormat": "NTFS",
          "driveType": "Fixed",
          "totalSizeBytes": 512000000000,
          "freeSizeBytes": 256000000000,
          "usedSizeBytes": 256000000000,
          "usagePercentage": 50.0,
          "isReady": true
        }
      ],
      "totalStorageBytes": 512000000000,
      "totalFreeStorageBytes": 256000000000,
      "overallStorageUsagePercentage": 50.0
    },
    "totalRunningProcessesCount": 180
  }
}
```
