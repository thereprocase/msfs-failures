# JSBSim Engine + Systems Models — Reference

## Source
- Repo: https://github.com/JSBSim-Team/jsbsim
- Cloned: 2026-05-01, `--depth=1 --filter=blob:none --sparse`
- License: LGPL-2.1+ (see src/LICENSE file)
- **Redistribution status:** LGPL-2.1+ permits use as reference. Derivative works must provide ability to re-link against modified LGPL library.

## What was kept (sparse checkout paths)
- `src/models/propulsion/` — Full propulsion subsystem:
  - `FGEngine.h/.cpp` — Base engine class (starter, running, starved, cutoff flags)
  - `FGTurbine.h/.cpp` — Jet turbine with explicit failure phases: tpStall, tpSeize; booleans Stalled, Seized, Overtemp, Fire
  - `FGTurboProp.h/.cpp` — Turboprop engine (directly relevant to PT6A): N1, ITT, OilPressure, OilTemp, Ielu (torque limiter), phases: tpOff, tpRun, tpSpinUp, tpStart, tpTrim
  - `FGPiston.h/.cpp`, `FGPropeller.h/.cpp`, `FGTank.h/.cpp`, `FGRotor.h/.cpp` — other engine types
- `src/models/FGFCS.h/.cpp` — Flight Control System (surface failure, channel disabling)
- `src/models/FGFCSChannel.h`
- `aircraft/737/` — Boeing 737 aircraft definition (XML)
- `aircraft/c172p/` — Cessna 172P (piston, simpler reference)

## Key Architecture

### FGTurbine failure states (FGTurbine.h lines 182, 272-280)
JSBSim models discrete failure phases for a jet/turbofan engine:
```cpp
enum phaseType { tpOff, tpRun, tpSpinUp, tpStart, tpStall, tpSeize, tpTrim };
bool Stalled;   // compressor stall — clears when throttle pulled to idle
bool Seized;    // inner spool seized — N2=0, OilPressure=0, engine dead
bool Overtemp;  // EGT exceeds limits
bool Fire;      // engine fire detected
```

**Stall behavior** (`FGTurbine::Stall()`):
- EGT jumps to TAT + 903°C
- N1/N2 decay toward windmill speed
- Recovery: pull throttle to idle → phase returns to tpRun

**Seize behavior** (`FGTurbine::Seize()`):
- N2 = 0 immediately, N1 windmills slowly
- Oil pressure = 0
- Oil temp decays toward ambient
- Engine stays seized (no automatic recovery)

Both are exposed as bindable SimProperties so XML scripts or external code can trigger them:
```cpp
PropertyManager->Tie(property_name + "/seized", &Seized);
PropertyManager->Tie(property_name + "/stalled", &Stalled);
```

### FGTurboProp wear-relevant parameters
The turboprop model (PT6A-style free-turbine) tracks:
- `N1` — gas generator speed (%)
- `Eng_ITT_degC` — inter-turbine temperature (lag-filtered, uses ITT_N1 table)
- `OilPressure_psi` — computed from N1 and oil temperature
- `OilTemp_degK` — seeks toward operating temp at rate dependent on N1
- `Ielu_max_torque` — IELU (torque limiter) threshold; if exceeded, throttle is backed off automatically
- `Condition` — integer; ≥10 forces engine off (failure injection hook)
- `PSFC` — power-specific fuel consumption; can be degraded by scaling

**Performance degradation** is done by modifying the lookup tables:
- `EnginePowerRPM_N1` — power vs RPM and N1 (scale down → less thrust)
- `EnginePowerVC` — velocity correction function
- `CombustionEfficiency_N1` — fuel efficiency vs N1 (scale → more fuel burn)
- `ITT_N1` — ITT vs N1 (shift up → higher temps at same power)

JSBSim does NOT have a built-in wear accumulator. Wear/degradation must be implemented externally by modifying these table values at runtime via the property system.

### No MEL representation
JSBSim has no concept of MEL (Minimum Equipment List) or deferred defects. Failures are instantaneous boolean state changes.

## PDFs
None — all source code.
