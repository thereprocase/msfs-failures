# msfs-failures

A standalone Windows sidecar app for **Microsoft Flight Simulator** that simulates **realistic per-airframe maintenance, wear, and failures** — the missing layer between MSFS's stock failure toggles and a real maintenance program.

> Status: design phase. No code yet. See [`PLAN.md`](./PLAN.md) for the v1 architecture and scope.

## What it is

MSFS ships with crude on/off failure toggles and no notion of accumulated wear. Real airframes age: oil burns down, tires wear, batteries lose capacity, magnetos drift, hard landings stress gear, overtemps cook bearings. Components have MTBF that *changes* based on how the airframe is operated. Squawks get deferred under MEL rules. Maintenance happens between flights, not during them.

This app rides outside the sim, talks to it over SimConnect + the **MobiFlight WASM module** (so it can reach L:Vars on third-party aircraft like FBW, PMDG, Fenix, Working Title, etc.), and maintains a persistent SQLite database of your fleet — every tail, every component, every hour, every cycle.

## v1 scope

- **Persistent wear / hours / cycles** per component, per tail
- **Realistic consumables** — oil burn, tire wear, brake pad life, battery SOH, hydraulic fluid
- **MTBF-driven random failures** with category accelerators (overtemp, overspeed, over-G, hard landing, neglected service)
- **Maintenance actions & squawks** with MEL-style deferral
- **Airframe editor UI** so any aircraft (default or third-party) can be wired up via L:Var bindings

## v2+

Career / economy layer — costs, parts inventory, mechanic time, downtime. The goal is "Neofly + OnAir + FSE had a baby, but the baby actually models the *aircraft*."

## Stack

- **C# / .NET 8 WPF** (Windows-only, x64)
- **SimConnect** + **MobiFlight WASM module** for sim I/O
- **SQLite** for persistence
- Optional **FSUIPC7** detection if installed

## Why MobiFlight WASM as the depth layer

It's free, actively maintained, widely installed, and exposes **arbitrary L:Vars / A:Vars / H:Events** on any aircraft over a SimConnect client-data area. That's what makes per-airframe failure injection on third-party planes feasible without writing one integration per aircraft.

## Repo layout (planned)

```
src/
  MsfsFailures.App/    WPF UI
  MsfsFailures.Core/   domain, wear engine, failure engine
  MsfsFailures.Sim/    SimConnect + MobiFlight WASM client
  MsfsFailures.Data/   SQLite + migrations
tests/
data/seed/             starter airframes (C172, PA28, BE58)
docs/
```

## Building

Not yet — design phase. See [`PLAN.md`](./PLAN.md).

## License

TBD.
