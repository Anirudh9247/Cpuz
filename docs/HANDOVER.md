# ComputerDoctor.Agent — Project Handover Framework

**Prepared by**: Anirudh  
**Handover to**: Editor (primary), Intern 2 (shadow / co-owner in training)  
**Repository**: `ComputerDoctor.Agent.sln` — `main` branch, commit `76111ac`  

This document is the single source of truth for the handover of `ComputerDoctor.Agent` from Anirudh to the Editor, with Intern 2 shadowing throughout. It defines what's done, what's left, who decides what, how we communicate, and how we confirm the work is complete.

---

## 1. Work Inventory — Completed vs Remaining

### 1.1 Completed Work
Everything below has been built, tested, and pushed to `main`. The Editor should treat this as stable ground — do not refactor unless a remaining task explicitly requires touching it.

| Component | Task | Status | Notes |
|---|---|---|---|
| **Agent.Core / Hardware** | Hybrid sensor read (`LibreHardwareMonitorLib` + WMI fallback) for CPU/GPU temps, fan RPM | Done | Field-by-field fallback verified |
| **Agent.Core / Processes** | Process enumeration, top memory-hog ranking, kill-by-PID | Done | Covered by unit tests |
| **Agent.Core / Storage** | SMART health %, SMART status, drive capacity read | Done | Verified against physical drives |
| **Agent.Core / Alerts** | Threshold-based `AlertEngine` (Thermal/RAM/SMART) reading `appsettings.json` | Done | Configurable warning/critical thresholds |
| **Agent.Core / Health** | `HealthScoreCalculator` producing 0–100 composite score | Done | Evaluates hardware & alert penalties |
| **Agent.Network / Discovery** | UDP broadcast beacon on port 8888, 3s interval, JSON payload | Done | Verified against mobile auto-discovery |
| **Agent.Network / WebSocket** | `AgentWebSocketServer` streaming `HEALTH_SNAPSHOT` every 2000ms on `ws://0.0.0.0:8080/ws` | Done | No auth/session gate yet — see Risks |
| **Agent.Network / Json** | `NetworkEnvelope<T>` versioned schema (message ID, session, UTC timestamp) | Done | Standardized JSON contract |
| **Agent.Core / Commands** | `CommandExecutor`: `KILL_PROCESS`, `RESTART_EXPLORER`, `CLEAR_TEMP_FILES`, `FLUSH_DNS` + `COMMAND_ACK` | Done | Destructive actions — currently unauthenticated |
| **Agent.Tests** | 17/17 xUnit tests passing across Core and Network | Done | 442ms run time |
| **Build & Repo** | .NET 10 build, 0 errors / 1 warning, pushed to `main` (`76111ac`) | Done | Warning not yet triaged |

---

### 1.2 Remaining Work
Ordered by dependency — items 1–4 (security core) block items 6 and 10, so they come first regardless of team availability.

| # | Task | Owner | Priority | Status |
|---|---|---|---|---|
| **1** | Session token pairing workflow (mobile ↔ agent handshake) | Editor | Critical | Not Started |
| **2** | Client connection state machine (connecting/paired/active/disconnected) | Editor | Critical | Not Started |
| **3** | PING/PONG heartbeat + stale-connection timeout handling | Editor | High | Not Started |
| **4** | Auth gate on `CommandExecutor` so only paired sessions can send destructive commands | Editor | Critical | Not Started |
| **5** | Triage the 1 existing build warning (`CA1416` platform compatibility) | Editor | Low | Not Started |
| **6** | WinForms system tray host with live status popup | Editor + Intern 2 | Medium | Not Started |
| **7** | Windows Service installer package | Editor | Medium | Not Started |
| **8** | Architecture / sequence / component diagrams | Intern 2 (drafts) + Editor (review) | Medium | Not Started |
| **9** | Update `appsettings.json` docs for new security config | Editor | Low | Not Started |
| **10** | End-to-end test: Android app pairing → command → ack round trip | Editor + Intern 2 | High | Not Started |

---

## 2. Roles & Decision Authority

### 2.1 Editor — Role Summary
The Editor owns implementation of the remaining tasks end-to-end: writing code, tests, and documentation, and driving tasks to the QA checkpoints defined in Section 7. The Editor is expected to work independently within the boundaries below and to flag anything outside them before proceeding.

### 2.2 Decision Matrix

| Decision Type | Editor Can Decide Independently | Requires Anirudh's Approval First |
|---|---|---|
| **Code structure** | Internal method/class organization within existing project folders | New top-level folders or renaming existing modules |
| **Security design** | Implementation details of the pairing handshake, so long as it uses token-based pairing as agreed | Choice of auth mechanism itself if it deviates from token pairing (e.g. switching to OAuth, certs) |
| **Dependencies** | Patch/minor version bumps of existing NuGet/npm packages | Adding any new third-party package or library |
| **Scope** | Small refactors that don't change public API surface | Any change to WebSocket message schema or `NetworkEnvelope<T>` contract |
| **Testing** | Writing additional unit tests beyond the 17 existing | Removing or skipping any existing test |
| **Timeline** | Reordering the remaining task list for efficiency | Slipping any milestone date in the timeline below |
| **Intern 2 involvement** | Assigning Intern 2 read-only/shadowing tasks | Assigning Intern 2 any task that touches `CommandExecutor` or security code |

> **Rule of thumb**: If a decision changes something Intern 2 or Anirudh will need to relearn, or touches the security/command-execution path, it needs approval first. Everything else, use judgment and move.

---

## 3. Communication & Escalation Protocols

- **Daily**: Async written status update (completed, in-progress, blockers).
- **Weekly**: 30-minute live check-in (Mondays).
- **Milestone Reviews**: Dedicated review session at each milestone (M1–M7).
- **Escalation**: If blocked for >4 working hours, post an update immediately. State: what you're blocked on, what you've tried, and what you need from Anirudh to proceed.

---

## 4. Timeline & Milestones

| Milestone | Deliverable | Target Date | Buffer |
|---|---|---|---|
| **M1 — Onboarding complete** | Editor has read all docs, built solution locally, run test suite green | Day 2 | — |
| **M2 — Security core** | Tasks 1–4 (pairing, state machine, heartbeat, auth gate) complete + tested | Day 9 | +2 days review |
| **M3 — QA checkpoint 1** | Anirudh review of security core before anything is built on top of it | Day 11 | — |
| **M4 — Tray shell + installer** | Tasks 6–7 complete, Intern 2 shadowing on tray UI | Day 18 | +2 days review |
| **M5 — Diagrams + docs** | Tasks 8–9 complete, Intern 2 leads diagram drafts | Day 22 | +1 day review |
| **M6 — Integration test** | Task 10: full pairing → command → ack test on real Android device | Day 25 | +2 days for fixes |
| **M7 — Final sign-off** | All items closed, sign-off checklist completed | Day 28 | — |

---

## 5. Quality Assurance Checkpoints

- **Code Review**: Diff review against existing style, no regressions in existing 17 tests (Anirudh).
- **Security Review**: Mandatory gate after M2 security core before proceeding to M4 (Anirudh).
- **Functional Test**: Feature verified against live Android client (Editor self-check, spot-checked by Anirudh).
- **Final QA Pass**: Full regression run, clean clone build, clean VM installer test (Anirudh + Editor jointly).
