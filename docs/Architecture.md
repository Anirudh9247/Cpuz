# ComputerDoctor.Agent - Architecture Specification

## Overview

`ComputerDoctor.Agent` is a high-performance system telemetry agent built on .NET 8. It collects hardware, process, and storage metrics and streams telemetry payloads in real-time over WebSocket to central monitoring services.

## Component Diagram

```text
ComputerDoctor.Agent
│
├── Agent.Core
│     ├── Hardware          Hardware metrics collectors (CPU, RAM, Uptime)
│     ├── Storage           Storage volume & drive telemetry (SMART, free space)
│     ├── Processes         Process enumeration & resource ranking
│     ├── Telemetry         TelemetryCollector orchestration service
│     └── Models            Telemetry DTOs & configuration schemas
│
├── Agent.Network
│     ├── Json              System.Text.Json payload wrappers
│     └── WebSocket         Managed ClientWebSocket with auto-reconnect
│
├── Agent.Service
│     ├── BackgroundService AgentWorker orchestration loop
│     ├── Configuration     appsettings.json settings binder
│     └── Logging           Console & Windows Event Log providers
│
├── Agent.Tests             xUnit automated test suite
│
├── docs                    Architecture & API documentation
│
└── README.md
```

## Data Flow Pipeline

```text
HardwareMonitor \
ProcessMonitor  --> TelemetryCollector --> SystemTelemetryReport --> AgentJsonSerializer --> AgentWebSocketClient --> WebSocket Server
StorageMonitor  /
```
