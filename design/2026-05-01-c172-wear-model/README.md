# C172 wear model + `.msfail` aircraft pack format

Status: draft, 2026-05-01.
Scope: defines the **portable per-aircraft config file** (`*.msfail`) that users
hand-author or share, plus the v1 wear model for the default Cessna 172.

A pack is a single JSON file. One file per airplane variant. No external
dependencies, no relative paths, no embedded code — the engine interprets it.

---

## 1. Why one file

- Drag-and-drop sharing on forums, Discord, Flightsim.to.
- Reproducible bug reports: "attach your `.msfail`."
- Versioned with `schemaVersion` + `packVersion`; the app upgrades old packs
  on import, never mutates the file in place.
- The pack is the **only** aircraft-specific knowledge in the system. Core
  ships with zero hardcoded SimVar names.

---

## 2. File layout (top-level)

```jsonc
{
  "$schema": "https://msfsfailures.dev/schema/aircraft-pack.v1.json",
  "schemaVersion": 1,
  "pack": { ... },          // identity + match rules
  "signals": [ ... ],       // logical → SimVar/L:Var bindings (inputs)
  "derived": [ ... ],       // computed signals (named recipes only)
  "components": [ ... ],    // wear-tracked physical systems
  "consumables": [ ... ],   // oil, tires, brake pads, battery SoH
  "failureModes": [ ... ],  // shared, referenced by components
  "injection": [ ... ],     // how to push a failure into the sim
  "support": { ... }        // declared coverage / known gaps
}
```

The engine validates against `aircraft-pack.schema.json` (sibling file). Any
unknown top-level key is a hard error — keeps packs honest.

---

## 3. `pack` block — identity & match

```jsonc
"pack": {
  "id": "com.msfsfailures.c172-default",   // reverse-DNS, globally unique
  "name": "Cessna 172 Skyhawk (default)",
  "author": "msfs-failures",
  "packVersion": "1.0.0",
  "match": {
    // any one rule matching wins; multiple rules = OR
    "title": ["Cessna Skyhawk G1000", "Cessna 172 Skyhawk"],
    "atcModel": ["C172"],
    "icaoType": ["C172"]
  },
  "tags": ["piston", "ga", "fixed-gear", "default-asobo"]
}
```

Match precedence at runtime: `title` (exact) > `atcModel` > `icaoType`. First
loaded pack with a winning match wins; user can pin a pack per airframe to
override.

---

## 4. `signals` — what we read from the sim

Each entry is a **logical name** that the rest of the pack refers to. The
engine never sees raw SimVar strings outside this block.

```jsonc
{
  "name": "engine_rpm",
  "source": "AVar",                     // AVar | LVar | KEvent | SimConnectVar
  "expression": "GENERAL ENG RPM:1",
  "unit": "rpm",
  "tier": "fast",                       // fast (20–50 Hz) | slow (1–2 Hz) | event
  "fallback": null                      // logical name to fall back to, or null
}
```

Tier rules (enforced by `TickHost`):

| tier  | rate     | use for                                       |
|-------|----------|-----------------------------------------------|
| fast  | 20–50 Hz | g-load, vertical speed, on-ground, RPM spikes |
| slow  | 1–2 Hz   | temps, fuel qty, voltages, ambient            |
| event | edge     | starter, master switch, gear handle           |

Fallback chain example: a study-level mod exposes `A32NX_CHT_1` as L:Var; if
absent, fall back to A:Var `RECIP ENG CYLINDER HEAD TEMPERATURE:1`; if that's
also absent, the signal is **declared unsupported** (see §9) and any wear model
that requires it is auto-disabled.

---

## 5. `derived` — computed signals

To keep packs portable and sandboxed, derived signals reference **named
recipes** baked into Core. No arbitrary expressions.

```jsonc
{
  "name": "touchdown_energy_j",
  "recipe": "touchdown_energy",
  "inputs": {
    "verticalSpeed": "vertical_speed_fpm",
    "groundSpeed": "ground_speed_kts",
    "mass": { "const": 1157 }            // kg, C172 MTOW; or signal name
  },
  "params": { }
}
```

Recipes shipped in v1:

| recipe                | inputs                                  | output unit |
|-----------------------|-----------------------------------------|-------------|
| `touchdown_energy`    | verticalSpeed, groundSpeed, mass        | joules      |
| `shock_cooling_rate`  | cht (timeseries)                        | °C/min      |
| `density_altitude`    | pressureAlt, oat                        | feet        |
| `power_pct`           | rpm, manifoldPressure, redlineRpm       | 0–1         |
| `braking_energy`      | brakeL, brakeR, groundSpeed             | J/s         |
| `start_kind`          | oilTemp, oat (edge on starter)          | enum        |
| `discharge_cycle`     | batteryV, alternatorActive              | bool edge   |

Adding a recipe = Core code change; packs cannot define new ones. This is the
deliberate tradeoff for not running pack-supplied code.

---

## 6. `components` — the wear units

Mirrors `ComponentTemplate` in Core, but expressed in pack-native form. Import
hydrates the DB.

```jsonc
{
  "id": "engine_1",
  "category": "Engine",
  "name": "Lycoming O-320-H2AD",
  "mtbfHours": 2000,                    // TBO baseline
  "wearCurve": {
    "type": "weibull",
    "shape": 2.4,
    "scale": 2200
  },
  "stress": {
    // each stressor multiplies the wear rate
    "highPower":    { "signal": "power_pct",  "above": 0.75, "factor": 1.6 },
    "chtExceedance":{ "signal": "cht_max_c",  "above": 232,  "factor": 3.0 },
    "shockCooling": { "signal": "shock_cooling_rate", "above": 28, "factor": 2.5 },
    "hotStart":     { "event":  "start_kind", "equals": "hot", "addCycles": 1 }
  },
  "failureModes": ["mag_drop_left", "low_oil_pressure", "rough_running"],
  "consumables":  ["oil_engine_1"],
  "requires":     ["engine_rpm", "manifold_pressure", "oil_temp_c", "oil_pressure_psi"],
  "optional":     ["cht_max_c", "egt_c"]
}
```

`requires` lists logical signal names; if any are missing **and** have no
fallback, the component is loaded **disabled** with a visible reason. This is
how the dashboard shows "Engine wear tracking degraded — no CHT signal" rather
than silently lying.

---

## 7. `failureModes` and `injection`

Failure modes are declared once and referenced from components. Injection is
separate so the same failure can be pushed via SimConnect on default aircraft
and via L:Var on FBW/A2A aircraft without forking the failure list.

```jsonc
"failureModes": [
  {
    "id": "mag_drop_left",
    "displayName": "Left magneto rough",
    "severity": "Caution",
    "symptoms": ["RPM drop > 175 on L mag check", "rough running"],
    "mtbfHours": 1500,
    "blocksDispatch": false
  }
],
"injection": [
  {
    "failureMode": "mag_drop_left",
    "kind": "SimConnectEvent",
    "expression": "TOGGLE_MAGNETO_1_FAILURE"
  }
]
```

If no `injection` row exists for a failure, it's `InternalOnly` — tracked in
the squawk list, no sim-side effect. That's fine; users still see the wear
trend and the maintenance bill.

---

## 8. `consumables`

```jsonc
{
  "id": "oil_engine_1",
  "kind": "Oil",
  "capacity": 8,                  // quarts
  "burnRateRecipe": "oil_burn_basic",
  "burnRateParams": { "qtPerHour": 0.05, "highPowerMultiplier": 2.0 },
  "minOperating": 5,              // squawk grounds the aircraft below this
  "topUpTo": 7
}
```

---

## 9. `support` — declared coverage

The pack tells the dashboard exactly what it can and can't model. Honesty is
the feature.

```jsonc
"support": {
  "covered": [
    "engine wear (RPM/MAP/oil)",
    "oil consumption + leaks",
    "hard-landing detection",
    "brake/tire wear",
    "battery SoH",
    "fuel mismanagement"
  ],
  "partial": [
    {
      "feature": "CHT exceedance",
      "reason": "Default C172 does not expose per-cylinder CHT; uses single-source bulk CHT only.",
      "mitigation": "Install Working Title CJ4-style enhancement pack or A2A C172 for per-cyl."
    },
    {
      "feature": "shock cooling",
      "reason": "Derived from bulk CHT; less accurate than per-cyl."
    }
  ],
  "unsupported": [
    {
      "feature": "vacuum / gyro failure",
      "reason": "SUCTION PRESSURE not exposed by default C172 in MSFS 2024.",
      "workaround": "Disabled; failure mode hidden from MEL list."
    },
    {
      "feature": "alternator field current",
      "reason": "No SimVar; battery model uses voltage sag heuristic instead."
    }
  ]
}
```

The dashboard renders this as a "what this pack tracks" card on the airframe
detail screen — same place the user picks the pack.

---

## 10. C172 v1 component table

| component              | category    | mtbf (h) | key signals                              | failure modes                              |
|------------------------|-------------|----------|------------------------------------------|--------------------------------------------|
| Engine (O-320)         | Engine      | 2000     | rpm, map, oilT, oilP, cht?, egt?         | mag drop L/R, low oil pressure, rough run  |
| Oil system             | OilSystem   | 4000     | oilT, oilP, oil qty (modeled)            | leak slow, leak fast, filter clog          |
| Starter                | Engine      | 1500     | starter edge, batteryV sag               | slow crank, no crank                       |
| Battery                | Battery     | 1200     | bus V, batteryV, alternator active       | low capacity, dead cell, won't hold charge |
| Alternator             | Battery     | 1800     | alt active, bus V                        | failed, low output                         |
| Main gear (L/R)        | GearBrakes  | 5000     | touchdown energy, gnd speed              | strut bottomed, oleo leak                  |
| Nose gear              | GearBrakes  | 3500     | touchdown energy, brake spike            | shimmy damper worn                         |
| Tires (L/R/N)          | Tires       | n/a      | brake input, gnd speed, touchdown        | flat-spot, tread, blowout                  |
| Brakes (L/R)           | GearBrakes  | n/a      | brake input ∫, gnd speed²                | pad worn, master cyl leak, fade            |
| Pitot/static           | Avionics    | 6000     | iat, ambient, on ground                  | blocked pitot, blocked static              |
| Vacuum / gyros         | Avionics    | n/a      | **unsupported on default C172**          | (declared in `support.unsupported`)        |

Per-component wear equations and stressor tables: see `cessna-172-skyhawk.msfail.json`.

---

## 11. Editor + validation flow

- Users edit packs in any text editor; the schema gives autocomplete in
  VS Code via `$schema`.
- The app has an **Import** button that runs schema validation, signal
  resolution (does the sim actually expose these?), and a dry-run wear tick on
  a synthetic 1-hour profile. Failures are reported per-line, not as a wall of
  text.
- Packs are stored under `%APPDATA%/MsfsFailures/packs/{id}/{version}.msfail`
  and indexed in the DB; the pack file is the source of truth, the DB is a cache.

---

## 12. Out of scope for v1

- Per-cylinder CHT/EGT (kept as `optional` signals, no v1 wear math).
- Turbocharger / waste-gate (no C172 turbo variants in v1).
- Constant-speed prop governor wear (only fixed-pitch C172 in v1).
- Pack inheritance / overlays — every pack is standalone. Inheritance is a
  v2 problem once we have ≥ 5 packs and see real duplication.

---

## 13. Files in this folder

- `README.md` — this document.
- `aircraft-pack.schema.json` — JSON Schema, draft 2020-12.
- `cessna-172-skyhawk.msfail.json` — reference C172 pack, target of v1.
