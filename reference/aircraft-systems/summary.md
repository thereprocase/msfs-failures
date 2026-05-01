# Prior Art Synthesis — WearEngine + FailureEngine Design Patterns

## What we collected

| Item | Path | Size | License |
|------|------|------|---------|
| FlyByWire A32NX/A380X failure system | `flybywiresim/` | 3.8 MB | GPL-3.0 |
| JSBSim propulsion + FCS models | `jsbsim/` | 3.0 MB | LGPL-2.1+ |
| PT6A-140A factsheet (P&WC) | `pt6a/pt6a-140a-factsheet.*` | 1.6 MB | Proprietary — ref only |
| PT6A New Owner Guide | `pt6a/pt6a-new-owner-guide.*` | 3.7 MB | Proprietary — ref only |
| MSFS C208B Improvement Mod | `xplane-plugins/msfs2020-C208-Improvment-Mod/` | 456 KB | MSFS Game Content Rules |
| **Total** | | **~13 MB** | |

---

## Pattern 1: FlyByWire three-layer failure model

A failure is represented at three abstraction layers kept in sync:

**Layer 1 (TS identifier):**
```typescript
// failures-orchestrator.ts
interface Failure { ata: AtaChapterNumber; identifier: number; name: string; }
type FailureDefinition = [AtaChapterNumber, number, string];
// Example: [24, 24020, 'Generator 1']
```

**Layer 2 (Orchestrator singleton):**
- Holds `activeFailures: Set<number>`
- `activate(id)` / `deactivate(id)` marks dirty
- Each frame: if dirty, serializes to JSON → WASM via Coherent COMM_BUS

**Layer 3 (Rust/WASM physics):**
```rust
// mod.rs
pub enum FailureType { Generator(usize), ReservoirLeak(HydraulicColor), ... }
// Each subsystem checks: active_failures.contains(&self.failure_type)
```

**Key insight**: No event queue. Physics polls a hash set each frame. Failures are instantly reversible by removing from the set.

**For us**: `wearLevel[component: float]` lives in a persistent bag. `FailureEngine` reads the bag + random draw and populates an `activeFailures` set. Physics polls the set.

---

## Pattern 2: JSBSim performance degradation via table scaling

FGTurboProp (PT6A-style) has no built-in wear accumulator, but all performance is driven by runtime-mutable lookup tables:

```
EnginePowerRPM_N1       → scale down for compressor wear (less HP)
ITT_N1                  → shift up for hot section wear (higher temps)
CombustionEfficiency_N1 → reduce for injector fouling (more fuel burn)
OilPressure_psi         → inject noise/offset for bearing wear
```

Also has discrete failure states (for FailureEngine):
- `Stalled` → compressor stall; recovers with throttle to idle
- `Seized` → inner spool locked, OilPressure=0, fatal, no auto-recovery
- `Overtemp`, `Fire` → boolean flags triggering ECAM/CAS

---

## Pattern 3: C208B stochastic wear accumulator (steal this)

From the MSFS C208B Improvement Mod README:
```
wearAccumulator += dt  (while torque > max_limit)
if wearAccumulator > 4*60 sec:
    CAS: "METAL CHIPS IN OIL"
    meanTimeToFailure = 100 min
    each overstress second: MTTF -= 8 sec
    failure sampled from Exponential(1/MTTF)
```

This is the canonical pattern:
1. Stress-dependent wear rate → accumulator
2. Accumulator → monotonically shrinking MTTF
3. Failure is a random draw against MTTF, not a hard threshold

---

## Pattern 4: ECAM sensed/non-sensed distinction

From A380X `EcamMessages/AbnormalSensed/ata70.ts`:
```typescript
{
  title: 'ENG 1 SHUT DOWN',
  sensed: true,            // auto-detected by ECAM (vs crew must identify)
  items: [
    { name: 'TCAS MODE', sensed: true, labelNotCompleted: 'TA ONLY' },
    { name: 'NO FUEL LEAK', condition: true, sensed: false },
  ],
  recommendation: 'LAND ASAP',
}
```

For us: each failure carries `sensed: bool` (auto CAS vs crew report), severity, and an ordered checklist. MEL entry is a separate reference not in FlyByWire's open-source code.

---

## PT6A-140A design targets (from P&WC docs)

- TBO: 4,000 hours base, 8,000 hours fleet max
- Monitored limits: Torque (%), ITT (°C), N1 (%), Oil pressure (psi), Oil temp (°C)
- Hot start = discrete damage event → mandatory inspection (not continuous wear)
- IELU torque limiter is a protective subsystem that reduces wear accumulation rate
- Three separate wear clocks: compressor module, hot section module, power turbine module

---

## What was skipped / not found

- No open-source X-Plane failure/wear plugin found — X-Plane uses internal DataRef injection (`sim/operation/failures/rel_*`) without custom wear logic
- FAA/CT-92-29 turbine reliability report: URL timed out. Retry: `curl -L https://www.tc.faa.gov/its/worldpac/techrpt/ct92-29.pdf`
- No public MEL database in machine-readable format
- Proprietary PT6A-140 maintenance manual (Veryon/P&WC subscription required — not vendored)
