# msfs-failures

**The maintenance hangar your sim never had.**

A standalone Windows sidecar for **Microsoft Flight Simulator** that turns every aircraft in your hangar into a *living airframe* — one that ages, wears, breaks, and demands the same respect a real one does. Built for simmers who already fly the real thing, or wish they did, and who are tired of pretending the airplane is brand new every time they hit "Fly."

> Status: design phase. No code yet. See [`PLAN.md`](./PLAN.md) for the v1 architecture and scope.

## The pitch

You preflight the same Skyhawk you flew yesterday. The oil's a quart low because you ran it lean over the Sierras last weekend. The right brake feels soft — you logged a squawk after that long taxi at KSFO and deferred it under MEL. Number two mag was rough on the runup three flights ago; you've been meaning to call the shop. Today the weather's marginal, you're heavy, and the cylinders haven't been happy about your climb profile.

That airplane has a *history*. Right now, in your sim, it doesn't.

**msfs-failures** gives it one.

It rides outside MSFS, talks to the sim through SimConnect and the **MobiFlight WASM module** so it can reach into virtually any aircraft — default, FBW, PMDG, Fenix, Working Title, Inibuilds, HJet, you name it — and keeps a persistent SQLite ledger of your fleet. Every tail. Every component. Every hour, every cycle, every overtemp, every hard landing. It models how those things compound, the way they actually do on the ramp.

## What "realistic" actually means here

- **Hours and cycles tracked per component, per tail.** Hobbs and tach, separately. Engine starts, pressurization cycles, gear cycles. Forever, across sessions.
- **Consumables that actually deplete.** Oil burns down as a function of power, RPM, oil temp, and engine health. Tires wear from groundspeed-at-touchdown. Brake pads from energy dissipated. Battery state-of-health degrades with deep discharges. Hydraulic fluid, O2 — all on the books.
- **MTBF that *responds to how you fly*.** Every component has a base mean time between failures. Operate it well and it lasts. Run cylinders hot, redline the prop, slam it on at 600 fpm vertical, lean too aggressive — and the dice get loaded against you. Category accelerators are configurable, transparent, and brutally fair.
- **Squawks and MEL-style deferrals.** Open a squawk, defer it (where rules allow), or ground the airplane. Maintenance happens *between* flights, in an actual interface, not in a menu toggle.
- **Per-airframe failure injection.** Stock SimConnect events for default aircraft; L:Var writes through MobiFlight for the third-party planes that have their own failure systems (e.g. `A32NX_FAILURE_*`, PMDG `_FAILURES_*`). One engine. One hangar. One source of truth.
- **An airframe editor.** Bring your own aircraft. Wire up the L:Vars. Define components, MTBFs, wear curves, and failure modes. Ship binding packs to friends.

## Who this is for

- Real pilots who want their sim time to *carry weight* — where neglecting an inspection has consequences three flights from now, and good airmanship pays dividends.
- Virtual airline ops, study groups, and home-cockpit owners who want a fleet that feels owned, not summoned.
- Tinkerers who want a transparent, data-driven failure model they can author and audit, not a black box.

## What it isn't

- A career manager (yet). The economy/parts/mechanic-time layer is v2 — the goal there is **Neofly + OnAir + FSE had a baby, and the baby actually models the *aircraft*.**
- An in-sim mod. It runs as a sidecar so it works across aircraft, updates without breaking the sim, and survives MSFS patches.
- A toy. The defaults will be conservative and physically grounded; the editor lets you tune everything to your standard of realism.

## v1 scope

- [x] Project plan locked
- [ ] Persistent wear / hours / cycles per component, per tail
- [ ] Realistic consumables — oil, tires, brakes, battery SOH, hydraulics
- [ ] MTBF-driven random failures with category accelerators
- [ ] Maintenance actions & MEL-style squawk deferral
- [ ] Airframe editor with L:Var binding authoring
- [ ] Seed packs: C172, PA28, BE58, plus one third-party reference profile

## v2+

Career and economy — costs, parts inventory, mechanic time, downtime, dispatch reliability targets, fleet planning. Built on the same component ledger so the airplane you've been flying for 200 hours stays the airplane you fly into the career layer.

## Stack

- **C# / .NET 8 WPF** (Windows-only, x64)
- **SimConnect** + **MobiFlight WASM module** for sim I/O
- **SQLite** for persistence
- Optional **FSUIPC7** detection if installed

## Why MobiFlight WASM is the depth layer

It's free, actively maintained, widely installed, and exposes **arbitrary L:Vars / A:Vars / H:Events** on any aircraft over a SimConnect client-data area. That's the only sane way to support per-airframe failure injection on third-party planes without writing one bespoke integration per aircraft. We pay that integration cost once, in the binding format, and the community gets to fill in the rest.

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

Not yet — design phase. See [`PLAN.md`](./PLAN.md) for the full architecture, data model, and implementation order.

## Contributing

Once v1 lands, the highest-leverage contributions will be **binding packs** for third-party aircraft — the small JSON/SQL payloads that map a model's L:Vars into the engine. If you fly a study-level airplane and know its failure surface, you can teach the app to wear it down.

## License

TBD before first release.
