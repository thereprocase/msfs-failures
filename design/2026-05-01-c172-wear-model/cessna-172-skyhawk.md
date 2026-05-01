# Cessna 172 Skyhawk — wear model companion

Human-readable walkthrough of `cessna-172-skyhawk.msfail.json`. The JSON is
the source of truth; this doc is for quick web review.

> **Status:** v1 draft, 2026-05-01. Targets default Asobo C172 (G1000 NXi) in
> MSFS 2024. A&P-grade submodel dictionaries (powerplant, airframe, electrical,
> coupling/recipe glue) are being generated in parallel and will land in a
> follow-up commit as `submodels-*.json` in this same folder.

---

## What this pack is

A single portable file, `cessna-172-skyhawk.msfail.json`, that tells the
MsfsFailures wear engine **everything** it needs to know about a C172:

- which SimVars to read and how often,
- how to cook those into derived stress signals,
- which physical components to track,
- what stresses each one and how fast,
- which failure modes each component can throw,
- how to inject those failures into the sim,
- and — crucially — **what this aircraft *can't* tell us**, so the dashboard
  can be honest about gaps instead of faking data.

Format spec: see [`README.md`](./README.md). Schema:
[`aircraft-pack.schema.json`](./aircraft-pack.schema.json).

---

## Pack identity

| field         | value                                                       |
|---------------|-------------------------------------------------------------|
| id            | `com.msfsfailures.c172-default`                             |
| version       | 1.0.0                                                       |
| match titles  | "Cessna Skyhawk G1000", "Cessna 172 Skyhawk"                |
| ATC / ICAO    | `C172`                                                      |
| tags          | piston, ga, fixed-gear, default-asobo, lycoming-o320        |

---

## Telemetry the pack reads

37 logical signals, grouped by sample tier:

### Fast tier (20–50 Hz)
For things that can spike between slow ticks.

`on_ground`, `g_force`, `vertical_speed_fpm`, `ground_speed_kts`,
`airspeed_indicated`, `engine_rpm`, `brake_left`, `brake_right`.

### Slow tier (1–2 Hz)
Temperatures, pressures, fuel, electrical.

`engine_elapsed_s`, `total_aircraft_hours`, `airspeed_true`,
`manifold_pressure`, `throttle_pct`, `mixture_pct`, `fuel_flow_gph`,
`oil_temp_c`, `oil_pressure_psi`, `cht_c`, `egt_c`, `fuel_total_gal`,
`fuel_left_gal`, `fuel_right_gal`, `battery_v`, `battery_load_a`,
`alternator_active`, `bus_v`, `ambient_temp_c`, `ambient_pressure`,
`pressure_alt`.

### Event tier (edge-triggered)
Only sampled when something actually changes.

`touchdown_v_fpm`, `combustion`, `starter`, `master_battery`,
`fuel_selector`, `parking_brake`, `flaps_handle_pct`.

---

## Derived stress signals

The pack never embeds raw math — it picks from a fixed library of named
recipes baked into Core. v1 ships with seven, all used by this pack:

| derived signal              | recipe               | what it represents                              |
|-----------------------------|----------------------|-------------------------------------------------|
| `power_pct`                 | `power_pct`          | engine output as fraction of redline RPM × MAP  |
| `touchdown_energy_j`        | `touchdown_energy`   | landing impact energy in joules (mass-weighted) |
| `shock_cooling_rate`        | `shock_cooling_rate` | CHT drop per minute on descent                  |
| `density_alt_ft`            | `density_altitude`   | density altitude from pressure alt + OAT        |
| `braking_power`             | `braking_energy`     | instantaneous braking power dissipation         |
| `start_kind`                | `start_kind`         | classifies cold / warm / hot start              |
| `battery_discharge_edge`    | `discharge_cycle`    | edge-detects each meaningful discharge cycle    |

---

## Components tracked

13 components in v1. Each one references logical signals; if a `requires`
signal is missing on the running aircraft, the component is loaded **disabled**
with a visible reason on the dashboard — no silent dead-reckoning.

| id                | category    | MTBF (h) | key stressors                                                      |
|-------------------|-------------|----------|--------------------------------------------------------------------|
| engine_1          | Engine      | 2000     | high power, hot CHT, shock cooling, hot oil, cold oil, high DA, hot starts |
| starter_1         | Engine      | 1500     | long crank, low cranking voltage                                   |
| battery_main      | Battery     | 1200     | deep discharge, alt-off operation, cumulative discharge cycles     |
| alternator_1      | Battery     | 1800     | base wear curve                                                    |
| gear_main_left    | GearBrakes  | 5000     | hard-landing energy on this leg                                    |
| gear_main_right   | GearBrakes  | 5000     | hard-landing energy on this leg                                    |
| gear_nose         | GearBrakes  | 3500     | hard landing, wheelbarrowing on brakes                             |
| tire_left         | Tires       | (cycles) | hard braking, high-energy touchdown                                |
| tire_right        | Tires       | (cycles) | hard braking, high-energy touchdown                                |
| brake_left        | GearBrakes  | (hours)  | high-energy brake events                                           |
| brake_right       | GearBrakes  | (hours)  | high-energy brake events                                           |
| fuel_system       | FuelSystem  | 4000     | low-tank operation (imbalance proxy)                               |
| pitot_static      | Avionics    | 6000     | base                                                               |

### How the engine wears (worked example)

`engine_1` baseline life is a Weibull (shape 2.4, scale 2200 h). Multipliers
apply when telemetry crosses thresholds:

| stressor       | trigger                          | factor                |
|----------------|----------------------------------|-----------------------|
| highPower      | `power_pct` > 0.75               | ×1.6 while above      |
| chtHot         | `cht_c` > 232 °C                 | ×3.0 while above      |
| shockCooling   | `shock_cooling_rate` > 28 °C/min | ×2.5 while above      |
| oilHot         | `oil_temp_c` > 118 °C            | ×2.0 while above      |
| oilCold        | `oil_temp_c` < 24 °C             | ×1.5 while above      |
| highDA         | `density_alt_ft` > 8000 ft       | ×1.2 while above      |
| hotStart       | `start_kind` event = "hot"       | +1 cycle per event    |

So a pilot who shoves the throttle in cold, climbs at 100% power, then chops
to idle for descent eats wear at multiple times the baseline rate. A pilot who
runs ~65% cruise and does proper cool-down approaches the book TBO.

---

## Failure modes

24 failure modes declared, with severity and dispatch-blocking flags:

| severity    | examples                                                                    |
|-------------|-----------------------------------------------------------------------------|
| Annunciator | starter slow crank, fuel selector stiff                                     |
| Caution     | mag drop L/R, carb ice, oleo leak, shimmy damper, tire flat spot, pad worn  |
| Warning     | rough running, battery low capacity, alternator fail, strut bottomed, pitot |
| Grounding   | low oil pressure, starter no-crank, dead cell, tire blowout, master cyl leak, fuel starvation, gascolator leak |

A `Grounding` failure with `blocksDispatch: true` prevents a flight from being
filed until repaired (or MEL-deferred where eligible). `Caution`/`Annunciator`
items accumulate in the squawk list and pressure the next maintenance event.

---

## Failure injection

10 of the 24 failure modes have a sim-side effect on the default C172 (the
rest are tracked internally — they show up in the squawk list and cost money,
but the sim itself isn't told):

| failure              | injection                                      |
|----------------------|------------------------------------------------|
| mag_drop_left        | `TOGGLE_MAGNETO_1_FAILURE` (SimConnect)        |
| mag_drop_right       | `TOGGLE_MAGNETO_2_FAILURE` (SimConnect)        |
| alternator_fail      | `TOGGLE_ALTERNATOR1` (SimConnect)              |
| pitot_blocked        | `PITOT_HEAT_OFF` (SimConnect — proxy)          |
| low_oil_pressure     | InternalOnly                                   |
| rough_running        | InternalOnly                                   |
| carb_ice             | InternalOnly                                   |
| starter_no_crank     | InternalOnly                                   |
| tire_flat_spot       | InternalOnly                                   |
| tire_blowout         | InternalOnly                                   |

Most "InternalOnly" entries become real once a study-level mod is loaded that
exposes the right L:Vars — at which point a different `.msfail` for that mod
takes over and replaces them with `LVarWrite` / `KEventWrite` injections.

---

## What this pack honestly tracks vs. doesn't

### ✅ Covered
- Engine wear from RPM/MAP/oil
- Oil consumption + leak modeling
- Starter abuse
- Battery state-of-health and discharge cycles
- Alternator failure
- Hard-landing detection (per gear leg)
- Brake/tire wear and flat-spot risk
- Fuel mismanagement (starvation, imbalance)
- Pitot/static blockage

### ⚠️ Partial
- **CHT exceedance / shock cooling** — default Asobo only gives bulk CHT, not
  per-cylinder. Mitigation: install A2A or BlackBox C172 + the matching pack.
- **EGT-based leaning quality** — `GENERAL ENG EGT:1` may read zero. When
  absent, engine wear silently falls back to RPM/MAP/oil-only stressors.

### ❌ Unsupported (and why)
- **Vacuum / gyro failure** — `SUCTION PRESSURE` not exposed by the default
  C172 in MSFS 2024. Failure modes are hidden from the MEL list rather than
  faked.
- **Alternator field current / load split** — no SimVar exposed; the battery
  model uses voltage sag as a proxy.
- **Carb heat position read-back** — not reliably exposed; carb ice fires as
  a scheduled internal event instead of a closed-loop model.

This list is rendered as a card on the airframe detail screen in the
dashboard. Honesty is a feature.

---

## Submodel dictionaries (A&P granularity)

The v1 component table above is the **rolled-up** view used to drive in-sim
failure injection. The submodel files are what an A&P would actually log
against — real units, jargon names, Fermi-accurate service limits. Total:
**625 submodels** across 9 shards.

### Powerplant — 210 submodels

| file                                                      | rows | scope                                                                 |
|-----------------------------------------------------------|------|-----------------------------------------------------------------------|
| [`submodels-powerplant-bottom-top.json`](./submodels-powerplant-bottom-top.json)                       | 89   | mains/rod bearings, journals, cam lobes, lifters, rings, pistons, valves, guides, springs |
| [`submodels-powerplant-ignition-induction.json`](./submodels-powerplant-ignition-induction.json)       | 60   | both magnetos, 8 spark plugs, harness, carburetor internals, intake, primer |
| [`submodels-powerplant-oil-cool-exh-acc-prop.json`](./submodels-powerplant-oil-cool-exh-acc-prop.json) | 61   | oil pump/cooler/seals, baffles, exhaust risers, alt/starter brushes, prop, mount, oil SOAP |

### Airframe — 241 submodels

| file                                                          | rows | scope                                                              |
|---------------------------------------------------------------|------|--------------------------------------------------------------------|
| [`submodels-airframe-gear-brakes-tires.json`](./submodels-airframe-gear-brakes-tires.json)   | 94   | main spring/nose oleo, wheel bearings, tire tread/cord/pressure, Cleveland disc + caliper + master cyl |
| [`submodels-airframe-controls-flaps.json`](./submodels-airframe-controls-flaps.json)         | 79   | aileron/elevator/rudder hinges, cable tensions, pulleys, flap track rollers, trim drum |
| [`submodels-airframe-structure.json`](./submodels-airframe-structure.json)                   | 68   | wing struts, tank sealant, firewall, mount isolators, doors/seals, exterior corrosion |

### Electrical / Avionics / Environmental — 174 submodels

| file                                                            | rows | scope                                                            |
|-----------------------------------------------------------------|------|------------------------------------------------------------------|
| [`submodels-electrical-fuel-power.json`](./submodels-electrical-fuel-power.json)             | 71   | wet-wing tanks + sumps, fuel selector/lines, alternator internals, Concorde RG-35 SoH/IR/cycles, bus bars, contactors, ground bonds |
| [`submodels-electrical-avionics.json`](./submodels-electrical-avionics.json)                 | 49   | G1000 PFD/MFD/GIA/GRS/GMU/GDC/GEA, COM/NAV, GTX33, GFC700 servos, ELT, analog standby instruments |
| [`submodels-electrical-environmental.json`](./submodels-electrical-environmental.json)       | 54   | landing/taxi/position/strobe/beacon, pitot heat + static leak, vacuum (legacy/N/A), cabin heat + CO, antennas, annunciators |

### Glue layer — coupling, recipes, events, inspections

[`submodels-coupling-recipes.json`](./submodels-coupling-recipes.json):

- **64 couplings** — directed edges between submodels with `amplifies` /
  `triggers` / `consumes` / `masks` / `induces_failure` semantics. Example
  chains: ring end gap → blow-by → oil consumption → bearing starvation;
  voltage regulator drift → battery overcharge → SoH loss; oleo seal leak →
  spring landings → spring back-set.
- **20 recipes** — the math kernels: `power_pct`, `bsfc_estimate`,
  `valve_thermal_load`, `ring_blowby_proxy`, `mag_timing_drift_proxy`,
  `brake_thermal_pulse`, `oleo_compression_event`, `crosswind_side_load`,
  `corrosion_index`, `alternator_load_factor`, `bus_voltage_dip`, etc.
- **32 events** — edge detectors with debounce/cooldown: `hot_start`,
  `shock_cooling`, `lean_of_peak`, `hard_landing`, `wheel_landing`,
  `bounced_landing`, `flat_spot_brake`, `mag_check_drop_excess`,
  `prop_strike_proxy`, `parked_outdoors_24h`, etc.
- **12 inspections** — the A&P calendar: 25h oil-and-filter, 50h alternative,
  100-hour, annual, 500h magneto IRAN, 1000h prop, ELT battery, pitot-static
  cert (FAR 91.411), transponder cert (FAR 91.413), brake fluid 5yr, battery
  capacity test (annual), engine TBO 2000h.

Each submodel row carries `drivers` (which telemetry signals/events
accelerate its wear), `couples` (ids of neighbours it physically affects),
`manifestsAs` (which cockpit-visible failures it induces past `failureValue`),
and `inspectionMethod` (how an A&P would actually measure it — diff
compression, SOAP, megger, leak test, borescope, hydrometer).
