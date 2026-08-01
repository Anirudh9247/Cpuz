# ADR 0001: Peer-to-Peer Network Topology and System Tray App Primary Host

* **Status**: Accepted
* **Date**: 2026-08-01

## Context & Problem Statement

The ComputerDoctor mobile diagnostic ecosystem requires a laptop agent to gather system telemetry (CPU/GPU temperature, fan speed, memory usage, SSD SMART status, security flags) and deliver updates to a companion mobile application. 

Two design approaches were evaluated:
1. **P2P LAN Model**: The laptop hosts a WebSocket server (`ws://0.0.0.0:8080/ws`) and broadcasts a UDP discovery beacon on port `8888`. The phone automatically pairs with the laptop over Wi-Fi without cloud dependencies or manual IP entry.
2. **Central Cloud Relay**: The laptop connects outbound to a central cloud WebSocket server (`ws://localhost:5000/ws/agent`), which relays messages to the mobile app.

Additionally, hosting options were evaluated: System Tray Application vs. Windows Background Service.

## Decision Drivers

* **Zero Cloud Cost & Offline Operation**: Peer-to-peer Wi-Fi connection ensures the app functions on isolated local networks without requiring external servers.
* **Ease of User Discovery**: UDP auto-broadcasting eliminates requiring users to find and type local IPv4 addresses.
* **Low-Level Hardware Access**: Ring 0 hardware temperature sensors (LibreHardwareMonitorLib) require user session access; Windows Services running under `LOCAL SYSTEM` encounter session 0 isolation and driver permission hurdles.

## Decision

1. **Adopt P2P LAN Topology**: The laptop agent hosts the WebSocket server and broadcasts a UDP discovery beacon. Central cloud relays are excluded from MVP.
2. **Adopt System Tray App (`Agent.TrayApp`) as Primary Host**: The primary distribution artifact is a taskbar system tray app featuring double-click status popups, balloon tip alerts, and background monitoring.
