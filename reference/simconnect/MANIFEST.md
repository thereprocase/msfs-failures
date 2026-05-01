# SimConnect Reference — MANIFEST

Reference material collected 2026-05-01 for the `msfs-failures` C# .NET 8 WPF sidecar project.
All items are in `/reference/simconnect/`. Total on-disk: ~3.7 MB across 217 files.

---

## 1. FsConnect — C# SimConnect Wrapper

| Field   | Value |
|---------|-------|
| **Dir** | `FsConnect/` |
| **What** | Open-source MIT-licensed C# wrapper around the MSFS SimConnect managed API. Reduces boilerplate for data definitions, event mapping, aircraft queries, and autopilot/radio managers. |
| **Upstream** | https://github.com/c-true/FsConnect |
| **Commit** | `348053d` (master, 2024-10-14, v1.4.0) |
| **License** | MIT — `FsConnect/src/CTrue.FsConnect/licenses/LICENSE.txt`. Note: the repo bundles `SimConnect.dll` and `Microsoft.FlightSimulator.SimConnect.dll` from MSFS SDK 0.24.3.0 (Microsoft copyright, SDK EULA applies to those two binaries). |
| **Why** | Shows canonical patterns for: connect without cfg file, struct-based AddToDataDefinition, MapClientEventToSimEvent, TransmitClientEvent, simulator-running detection. IFsConnect interface is a clean reference for the API surface. |
| **Caveats** | Build artifacts removed (bin/obj). Two Microsoft DLLs remain in `src/Dependencies/SimConnect/` — these are SDK-EULA-bound and are included because FsConnect itself bundles them in its NuGet package (the repo author accepts responsibility). Do not redistribute those binaries separately. |

---

## 2. CSharpSimConnect — Minimal WinForms Example (.NET 7)

| Field   | Value |
|---------|-------|
| **Dir** | `CSharpSimConnect/` |
| **What** | Single-file WinForms C# example showing SimConnect connect, WndProc message pump, AddToDataDefinition, RegisterDataDefineStruct, RequestDataOnSimObject, and OnRecvSimobjectData. Closest to the pattern needed for WPF sidecar (message pump approach is identical). |
| **Upstream** | https://github.com/rolex20/CSharpSimConnect |
| **Commit** | `9f9ab34` (main, 2024-04-17, tag v1.0-alpha) |
| **License** | No LICENSE file in repo — effectively all-rights-reserved by default. Vendored here for reference only; do not copy code verbatim into production without contacting the author. The code demonstrates widely documented public API patterns. |
| **Why** | The `Form1.cs` file (129 lines) is the clearest end-to-end demonstration of the managed SimConnect loop in C#: connect → define → register struct → request per-second on-change → receive → unbox. |
| **Caveats** | Build artifacts (bin/, obj/) remain in the tree — rm -rf permission was unavailable during collection. The bin/ dir contains Microsoft.FlightSimulator.SimConnect.dll and SimConnect.dll (SDK EULA applies). These should be deleted before committing to any shared repo. |

---

## 3. failure-events.md — Stock TOGGLE_*_FAILURE Event Reference

| Field   | Value |
|---------|-------|
| **File** | `failure-events.md` |
| **What** | Compiled reference of all 12 stock MSFS failure events, with C# code showing MapClientEventToSimEvent + TransmitClientEvent pattern. Includes notes on toggle semantics, shared cockpit propagation, and the absence of gyro/fuel/avionics stock events. |
| **Upstream** | Synthesized from: MSFS SDK Event IDs docs, paruljain/fsx EventIds.txt, Python-SimConnect EventList.py |
| **Sources** | https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Aircraft_Misc_Events.htm · https://github.com/paruljain/fsx/blob/master/EventIds.txt |
| **License** | Authored original (this repo) |
| **Why** | The canonical list of what can be failed via stock SimConnect events; not otherwise in one place. |

---

## 4. aircraft-detection.md — Aircraft Identity Pattern Guide

| Field   | Value |
|---------|-------|
| **File** | `aircraft-detection.md` |
| **What** | Documents how to read Title, ATC MODEL, ATC TYPE, ATC AIRLINE, ATC ID SimVars from a sidecar; C# struct and request code; AircraftLoaded event subscription; MSFS 2024 modular format caveat; family matching pattern. |
| **Upstream** | Synthesized from MSFS SDK SimVar docs, MSFS Universal Announcer source, MSFS DevSupport forum |
| **Sources** | https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Aircraft_SimVars/Aircraft_RadioNavigation_Variables.htm · https://fearlessfrog.github.io/MSFS_Universal_Announcer/statemachine.html · https://devsupport.flightsimulator.com/t/msfs2024-modular-package-simconnect-aircraftloaded-only-serves-the-preset-aircraft-cfg/15386 |
| **License** | Authored original (this repo) |
| **Why** | Answers "how do I know which aircraft is loaded?" — the top question for a failure-injection sidecar that must behave differently per aircraft type. |

---

## 5. simconnect-managed-primer.md — WPF Sidecar Quick-Start

| Field   | Value |
|---------|-------|
| **File** | `simconnect-managed-primer.md` |
| **What** | Step-by-step guide: prerequisites, WPF HWND setup with WindowInteropHelper/HwndSource, connect with retry, AddToDataDefinition + struct, system events (Sim/Pause/Crash/AircraftLoaded), failure transmit, common pitfalls table. |
| **Upstream** | Synthesized from MSFS SDK managed-code guide and community examples |
| **Sources** | https://docs.flightsimulator.com/html/Programming_Tools/SimConnect/Programming_SimConnect_Clients_using_Managed_Code.htm · https://www.theflightsimdev.com/5-reading-simvar-data-with-simconnect/ |
| **License** | Authored original (this repo) |
| **Why** | The official docs assume WinForms; this primer fills the WPF gap and wires together all four goals (connect, SimVars, events, failures) in a single document. |

---

## Items Not Collected

### Microsoft.FlightSimulator.SimConnect NuGet / managed DLL

**Reason**: The managed wrapper DLL (`Microsoft.FlightSimulator.SimConnect.dll`) and the native
`SimConnect.dll` are distributed under the MSFS SDK EULA, which prohibits redistribution of the
SDK separately from the SDK package itself. There is no official Microsoft NuGet package for it.

**How to obtain**: Install MSFS (which installs a Developer Mode SDK) or download the MSFS SDK
installer from the MSFS in-game developer tools. Files land at:
`<SDK root>\SimConnect SDK\lib\managed\Microsoft.FlightSimulator.SimConnect.dll`
`<SDK root>\SimConnect SDK\lib\SimConnect.dll`

The FsConnect and CSharpSimConnect repos both bundle these DLLs; those copies are in this
reference tree but are covered by the SDK EULA and must not be redistributed.

### mhwlng/msfs2020-cockpit-companion, FlyingTroll/MSFS_LiveTraffic

Not cloned — these are large applications (100+ MB with assets), and the relevant patterns
(SimConnect connect/subscribe) are already better illustrated by the smaller FsConnect and
CSharpSimConnect examples. References if needed:
- https://github.com/mhwlng/msfs2020-cockpit-companion (MIT)
- https://github.com/FlyingTroll/MSFS_LiveTraffic

### EvenAR SimConnect.NET

EvenAR's SimConnect work is Node.js-based (`node-simconnect`), not C#. No equivalent C# repo
exists under that name. Skipped.

---

## Quick-Reference: What to Read First

1. `simconnect-managed-primer.md` — end-to-end WPF sidecar setup
2. `CSharpSimConnect/CSharpSimConnect/CSharpSimConnect/Form1.cs` — minimal working example (129 lines)
3. `failure-events.md` — complete failure event list + transmit pattern
4. `aircraft-detection.md` — identify loaded aircraft type
5. `FsConnect/src/CTrue.FsConnect/IFsConnect.cs` — full managed API surface reference
