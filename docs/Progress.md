# ComputerDoctor.Agent - Roadmap & Progress Tracker

## Milestone Progress

| Sprint / Phase | Description | Status |
|---|---|---|
| **Phase 1: Foundation** | Solution structure (`ComputerDoctor.Agent.sln`), DI, `appsettings.json`, `.vscode/settings.json` | ✅ Completed |
| **Phase 2: Hardware & Core Sensors** | `IHardwareMonitor`, `IProcessMonitor`, `IStorageMonitor`, `ITelemetryCollector` | ✅ Completed |
| **Phase 3: Alert Engine & Thresholds** | Configurable alert thresholds in `AgentConfig` & `AlertRuleEngine` | ✅ Completed |
| **Phase 4: Networking & P2P** | P2P WebSocket server (`ws://0.0.0.0:8080/ws`) & UDP Discovery (`8888`) | ✅ Completed |
| **Phase 5: System Tray UI** | WinForms `TrayIconContext`, balloon notifications, status popups | ✅ Completed |
| **Sprint 1: Telemetry Stabilization** | Canonical `HealthSnapshot`, 0–100 Health Score (🟢/🟡/🔴), Alert Engine, Configurable Thresholds, Snapshot Validation | ✅ Completed |
| **Sprint 2: Android Integration** | WebSocket live payload delivery to Android Client | ⏳ Next Up |
| **Sprint 3: Actions Execution** | Remote actions (Clear Temp, Kill Process, Power Plan, Flush DNS) | ⏳ Upcoming |
| **Sprint 4: Trust Layer** | Sensor confidence metadata (`LibreHardwareMonitor` vs `WMI` fallback verification) | ⏳ Upcoming |
