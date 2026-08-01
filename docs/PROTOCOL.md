# ComputerDoctor Agent - Protocol Specification

## 1. UDP Auto-Discovery Beacon Protocol

The laptop agent broadcasts a UDP JSON beacon every 3 seconds on port `8888` to enable mobile applications to automatically discover the laptop on local Wi-Fi without manual IP entry:

```json
{
  "service": "ComputerDoctorAI",
  "agentName": "LAPTOP-AGENT-01",
  "wsUrl": "ws://192.168.1.105:8080/ws",
  "port": 8080
}
```

---

## 2. WebSocket Telemetry Schema (`HealthSnapshot`)

WebSocket Endpoint: `ws://0.0.0.0:8080/ws`

```json
{
  "timestamp": "2026-08-01T13:50:00Z",
  "healthScore": 88,
  "healthStatus": "Good",
  "cpu": {
    "tempC": 52.5,
    "clockMhz": 3200,
    "loadPercent": 18.4
  },
  "gpu": {
    "tempC": 58.0,
    "clockMhz": 1800
  },
  "fan": {
    "rpm": 2400
  },
  "ram": {
    "usagePercent": 45.2,
    "topProcesses": [
      { "name": "devenv.exe", "mb": 1420 },
      { "name": "chrome.exe", "mb": 1150 }
    ]
  },
  "ssd": {
    "healthPercent": 95,
    "smartStatus": "OK",
    "writeWearPercent": 8
  },
  "battery": {
    "healthPercent": 90
  },
  "security": {
    "defenderEnabled": true,
    "definitionsUpToDate": true
  },
  "suspiciousProcesses": [],
  "systemLogs": {
    "recentBsod": false,
    "driverFailures": []
  },
  "alerts": [
    {
      "type": "HighTemp",
      "severity": "Warning",
      "component": "GPU",
      "message": "GPU approaching thermal warning threshold (78°C)"
    }
  ]
}
```
