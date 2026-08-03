# 📋 ComputerDoctor — Product Roadmap & Honest Progress Tracker

## Status Classification Scheme
- **(a) Fully Complete**: Implemented, compiled, unit-tested, and verified in runtime.
- **(b) Partially Complete**: Implemented/unit-tested; pending environment compilation, physical hardware, or E2E client validation.
- **(c) Blocked**: Pending external environment dependency, hardware, or long-running execution.
- **(d) Not Started**: Planned future phase.

---

## Product Milestone Overview

| Sprint / Milestone | Phase Description | Target / Deliverables | Status |
|---|---|---|---|
| **Phase 0** | **Foundation** | Solution setup (`ComputerDoctor.Agent.sln`), DI container, config schemas | (a) Fully Complete |
| **Phase 1** | **Telemetry Engine** | Sensor collectors (`CPU`, `GPU`, `RAM`, `SSD` SMART, process enumeration) | (a) Fully Complete |
| **Phase 2** | **Networking & P2P** | UDP Auto-Discovery beacon (8888) & WebSocket server (`ws://0.0.0.0:8080/ws`) | (a) Fully Complete |
| **Phase 2.1** | **Stability & Security Core** | Token pairing, state machine, heartbeat, auth gate | (a) Fully Complete |
| **Sprint 3** | **Android Integration** | Auto-Discovery (UDP 8888), WebSocket client, PIN Pairing UI, Telemetry Dashboard | (b) Partially Complete — Code written; pending APK build & Wi-Fi test |
| **Sprint 4** | **Remote Actions Framework** | `KILL_PROCESS`, `RESTART_EXPLORER`, `CLEAR_TEMP_FILES`, `FLUSH_DNS` + `COMMAND_ACK` | (b) Partially Complete — Implemented & unit-tested; pending E2E Android validation |
| **Sprint 5** | **Production Hardening** | Sealed state machines, exponential backoff, versioned `NetworkEnvelope<T>` schema | (a) Fully Complete |
| **Sprint 6** | **System Tray Experience** | WinForms tray host (`TrayApplicationContext.cs`), balloon alerts, status popup | (b) Partially Complete — Code complete; pending usability UI verification |
| **Sprint 7** | **Packaging & Deployment** | Publisher script (`publish.ps1`), Windows Service Installer (`install-service.ps1`) | (b) Partially Complete — Scripts written; requires Admin elevation & WiX Toolset |
| **Sprint 8** | **Documentation Suite** | Architecture C4 diagrams, sequence diagrams, API specification, Handover documentation | (a) Fully Complete |
| **Sprint 9** | **Quality Assurance** | Long-running stability (8–24h), multi-client stress test, recovery verification | (c) Blocked — Requires physical 8–24h hardware run |
| **Sprint 10** | **Final Demonstration** | End-to-end demo flow: Discovery → Pairing → Streaming → Action → ACK | (d) Not Started — Pending physical device deployment |

---

## 🎯 Verification Plan (Stages 1–7 Execution)
- [ ] **Stage 1 — Android Build**: Compile `app-debug.apk` in Android Studio / Gradle.
- [ ] **Stage 2 — Discovery**: Verify UDP beacon auto-discovery over local Wi-Fi.
- [ ] **Stage 3 — Pairing**: Verify 6-digit PIN handshake & session token exchange.
- [ ] **Stage 4 — Telemetry**: Verify continuous 2s `HealthSnapshot` updates.
- [ ] **Stage 5 — Commands**: Verify `KILL_PROCESS` execution & `COMMAND_ACK` roundtrip.
- [ ] **Stage 6 — Long Run**: 8+ hour stability monitoring for memory leaks & stale sockets.
- [ ] **Stage 7 — Final Demo**: Single continuous video recording of end-to-end user flow.
