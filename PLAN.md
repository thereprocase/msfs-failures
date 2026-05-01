# MSFS Failures & Maintenance Sidecar — v1 Plan

## Context

The user wants a standalone Windows app that runs alongside MSFS and simulates **realistic maintenance, wear, and failures** on a per-airframe basis. The project directory `/mnt/f/link/msfs-failures/` is currently empty — this is greenfield.

The design goal is to be the "Neofly + OnAir + FSE baby" of failures/maintenance: deeper, more realistic per-component modeling than stock MSFS, with persistent hours/cycles/wear, realistic consumables (oil, brake pads, tire wear, battery health, mag timing, etc.), and MTBF-driven random failures whose probability is accelerated by category-specific variables (overtemp, overspeed, hard landing, neglected oil change, etc.).

Career/economy is explicitly **out of scope for v1** but the data model must not paint us into a corner.

## Decisions locked in

| Area | Choice |
|---|---|
| Sim link | **MobiFlight WASM module** (primary) + **SimConnect** (always on). Optional FSUIPC7 detection for users who already have it. |
| Stack | **C# / .NET 8 WPF**, x64, Windows-only |
| Storage | **SQLite** (Microsoft.Data.Sqlite) with an in-app editor UI |
| v1 scope | Persistent wear/hours, realistic consumables, MTBF random failures with category accelerators, maintenance actions & squawks (MEL-style deferrals) |
| v2+ | Career/economy layer (parts, costs, mechanic time, downtime) |

### Why MobiFlight WASM as the primary depth layer

- Free, open, actively maintained, and already installed by a huge slice of the home-cockpit crowd.
- Exposes **arbitrary L:Vars / A:Vars / H:Events** for *any* aircraft (Asobo, FBW, PMDG, Fenix, HJet, Inibuilds, Working Title, etc.) — this is the only way to read/write deep third-party systems state.
- Talks over SimConnect using the standard client-data area protocol, so we add a thin WasmClient on top of our SimConnect client without a second transport.
- FSUIPC7 covers a similar surface but is paid; we'll auto-detect it and use it as a secondary source if present, but won't require it.

## Architecture

```
┌────────────────────────────────────────────────────────────────┐
│  MsfsFailures.App  (WPF, MVVM, CommunityToolkit.Mvvm)          │
│   ├─ Dashboard / Squawks / Maintenance / Airframe Editor       │
│   └─ Live systems view (oil temp, hours, wear bars, …)         │
└──────────────▲──────────────────────────────────▲──────────────┘
               │ DI                                │ DI
┌──────────────┴────────┐          ┌───────────────┴─────────────┐
│ MsfsFailures.Core      │          │ MsfsFailures.Sim            │
│  - Domain model        │          │  - SimConnectClient         │
│  - WearEngine          │          │  - MobiFlightWasmClient     │
│  - FailureEngine       │          │  - Fsuipc7Client (optional) │
│  - MaintenanceService  │          │  - AircraftDetector         │
│  - Persistence (EF/Dapper + SQLite)│  - VarBinding registry     │
└────────────────────────┘          └─────────────────────────────┘
```

- **Tick loop**: Sim layer pushes a 4 Hz state snapshot (RPM, MAP, OAT, oil T/P, EGT, CHT, G-load, IAS, gear/flap, weight, fuel flow, on-ground, brakes, etc.) into Core. WearEngine integrates wear; FailureEngine rolls dice; MaintenanceService applies user actions.
- **Failure injection**: writes back via SimConnect events (stock failures: `TOGGLE_*_FAILURE`) **and** MobiFlight WASM `(>K:…)` / `(>L:…)` writes for aircraft-specific systems (e.g. PMDG `_FAILURES_*` L:Vars, FBW `A32NX_FAILURE_*`, etc.) sourced from a per-airframe binding profile.
- **Persistence**: SQLite file in `%LOCALAPPDATA%\MsfsFailures\fleet.db`. Migrations via EF Core or hand-rolled. Per-airframe configs are *rows*, not files — editor UI is first-class.

## Data model (SQLite)

Tables (sketch):

- `airframes` (id, icao_type, tail_number, model_ref, total_hours, total_cycles, created_at)
- `model_refs` (id, name, manufacturer, sim_match_rules_json) — one per aircraft *type* so multiple tails share component templates
- `component_templates` (id, model_ref_id, category, name, mtbf_hours, wear_curve_json, consumable_kind, replace_interval_hours, replace_interval_cycles)
- `components` (id, airframe_id, template_id, hours, cycles, wear_0_1, condition, last_serviced_at, installed_at)
- `consumables` (id, airframe_id, kind /* oil, hyd, brake_pad, tire, battery_soh */, level, capacity, last_topped_up_at)
- `accelerators` (id, category, variable, formula_json) — e.g. `oil_temp > 115 → wear_rate ×= 1 + (T-115)/20`
- `failure_modes` (id, template_id, name, sim_binding_kind /* simconnect_event, lvar_write, internal_only */, sim_binding_payload, severity, repair_hours, mel_deferrable)
- `squawks` (id, airframe_id, failure_mode_id, opened_at, deferred_until, status, notes)
- `maintenance_log` (id, airframe_id, action, components_touched_json, hours_at_action, cost_placeholder, performed_at)
- `var_bindings` (id, model_ref_id, logical_name, source /* A:, L:, K:, simconnect_var */, expression) — the heart of third-party support
- `sessions` (id, airframe_id, started_at, ended_at, hobbs_start, hobbs_end, max_g, hard_landings, overtemps_json)

The `var_bindings` table is what lets the same WearEngine drive a default C172, a PMDG 737, and a Fenix A320 — only the bindings differ.

## Realism details for v1

- **Hours**: Hobbs-style (engine running) and Tach-style (RPM-weighted) tracked separately per engine.
- **Cycles**: takeoff/landing pairs, engine starts, pressurization cycles (when applicable).
- **Oil**: consumed at a rate `f(power, RPM, oil_temp, engine_wear)`; low oil → temp rise → accelerated cam/bearing wear → eventual oil-pressure failure.
- **Tires/brakes**: wear from groundspeed-at-touchdown, brake energy, taxi distance.
- **Battery**: SOH degrades with deep discharges and overcharge events.
- **Hard landings**: vertical speed at touchdown × weight → gear/strut wear, possible immediate failure above threshold.
- **Overtemps/overspeeds/over-G**: every excursion logs to `sessions.overtemps_json` and feeds accelerators.
- **Random MTBF**: per-tick Poisson roll using effective MTBF = base_MTBF / Π(accelerators).

## Project layout to create

```
/mnt/f/link/msfs-failures/
├─ MsfsFailures.sln
├─ src/
│  ├─ MsfsFailures.App/          (WPF, net8.0-windows)
│  ├─ MsfsFailures.Core/         (netstandard2.1 or net8.0)
│  ├─ MsfsFailures.Sim/          (net8.0-windows, references SimConnect)
│  └─ MsfsFailures.Data/         (EF Core + SQLite)
├─ tests/
│  ├─ MsfsFailures.Core.Tests/
│  └─ MsfsFailures.Sim.Tests/
├─ data/
│  └─ seed/                      (starter airframes: C172, PA28, BE58)
├─ docs/
│  ├─ architecture.md
│  └─ binding-authoring.md
├─ .gitignore
├─ .editorconfig
└─ README.md
```

## Critical external pieces

- **Microsoft.FlightSimulator.SimConnect** NuGet (or DLL from MSFS SDK)
- **MobiFlight WASM module** — runtime dependency installed by user; we detect via SimConnect client-data area `MobiFlight.LVars`
- **CommunityToolkit.Mvvm**, **Microsoft.Extensions.Hosting**, **Serilog**, **Microsoft.Data.Sqlite** / **Dapper** (or **EF Core 8**)

No code to reuse — this is a new repo.

## Implementation order

1. Solution + project skeleton, DI host, Serilog, settings file.
2. `MsfsFailures.Sim`: SimConnectClient connect/disconnect, basic A-var subscription, aircraft-detection event.
3. MobiFlight WASM client (LVar read/write over client-data area), behind an `ISimVarBus` abstraction that fans SimConnect + WASM into one logical bus.
4. SQLite schema + migrations + seed for one airframe (C172).
5. Core: WearEngine tick, consumable model, basic failure roll.
6. WPF dashboard: live state, hours, wear bars, squawk list.
7. Failure injection: SimConnect events first, then L:Var writes via bindings.
8. Maintenance actions UI + squawk deferral (MEL-style).
9. Airframe editor UI (CRUD on `model_refs`, `component_templates`, `var_bindings`).
10. Seed packs for PA28, BE58; one third-party reference (FBW A32NX bindings).

## Verification

- **Unit**: WearEngine math (overtemp acceleration, MTBF reduction), Poisson roll determinism with fixed seed, persistence round-trip.
- **Integration (no sim)**: fake `ISimVarBus` feeding scripted flight profiles; assert oil consumption, hours, wear, expected failure distribution over N simulated hours.
- **Live (sim required)**: with MSFS + default C172 — confirm Hobbs accumulates, oil temp tracked, an injected magneto failure shows up on mag check; with FBW A32NX — confirm an `A32NX_FAILURE_*` write triggers ECAM message in cockpit.
- **Manual smoke**: open editor UI, create a new airframe from C172 template, fly 1 h, close sim, reopen app — hours and consumables persisted.

## Open items (tracked, not blocking v1 start)

- Exact NuGet vs SDK-DLL strategy for SimConnect (CI-friendliness).
- EF Core vs Dapper — defer to project skeleton step.
- Whether to ship a small companion in-sim WASM gauge for things MobiFlight can't reach (deferred unless needed).
