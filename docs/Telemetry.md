# ComputerDoctor.Agent - Telemetry & HealthSnapshot Schema Specification

## `HealthSnapshot` Schema (Sprint 1 Production Canonical Model)

```json
{
  "messageType": "HEALTH_SNAPSHOT",
  "agentVersion": "1.0.0",
  "snapshot": {
    "agentId": "COMPUTER-DOCTOR-AGENT-01",
    "machineName": "LOCAL-SYSTEM",
    "timestampUtc": "2026-08-02T07:45:00Z",
    "overallHealthScore": 92,
    "overallStatus": "Healthy",
    "cpu": {
      "tempC": 52.4,
      "loadPercent": 24.5,
      "clockMhz": 3400.0,
      "logicalProcessorCount": 8,
      "status": "Healthy"
    },
    "gpu": {
      "tempC": 48.0,
      "loadPercent": 15.0,
      "clockMhz": 1200.0,
      "status": "Healthy"
    },
    "memory": {
      "usagePercent": 48.5,
      "usedMb": 8331.0,
      "totalMb": 16384.0,
      "topProcesses": [
        {
          "id": 1420,
          "processName": "devenv",
          "workingSetMemoryBytes": 524288000,
          "privateMemoryMb": 500.0,
          "threadCount": 42,
          "status": "Running"
        }
      ],
      "status": "Healthy"
    },
    "battery": {
      "healthPercent": 100,
      "chargePercent": 95,
      "isPluggedIn": true,
      "status": "Healthy"
    },
    "drives": [
      {
        "name": "C:",
        "label": "OSDisk",
        "totalSizeGb": 512.0,
        "freeSpaceGb": 256.0,
        "usagePercent": 50.0,
        "healthPercent": 100,
        "smartStatus": "OK",
        "status": "Healthy"
      }
    ],
    "processes": {
      "totalRunningCount": 180,
      "topProcesses": [],
      "suspiciousProcesses": []
    },
    "defender": {
      "defenderEnabled": true,
      "definitionsUpToDate": true,
      "realTimeProtectionEnabled": true,
      "status": "Healthy"
    },
    "alerts": [
      {
        "severity": "Warning",
        "category": "CPU",
        "message": "CPU temperature (82.0°C) exceeded warning threshold (80.0°C)",
        "timestampUtc": "2026-08-02T07:45:00Z"
      }
    ],
    "trust": {
      "confidenceScore": 100,
      "sensorSource": "LibreHardwareMonitor",
      "fallbackUsed": false
    }
  }
}
```

## Overall & Component Status Enums

- **Status Values**:
  - `Healthy` (🟢) — Score 85–100
  - `Warning` (🟡) — Score 60–84
  - `Critical` (🔴) — Score 0–59

## Configurable Thresholds (`appsettings.json`)

```json
{
  "AgentConfig": {
    "CpuWarningTempC": 80.0,
    "CpuCriticalTempC": 90.0,
    "CpuWarningLoadPercent": 80.0,
    "CpuCriticalLoadPercent": 90.0,
    "GpuWarningTempC": 78.0,
    "GpuCriticalTempC": 85.0,
    "RamWarningPercent": 80.0,
    "RamCriticalPercent": 90.0,
    "StorageWarningPercent": 85.0,
    "StorageCriticalPercent": 95.0,
    "DefenderAlertEnabled": true
  }
}
```
