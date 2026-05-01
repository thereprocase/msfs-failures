# Aviation Reliability Reference — Summary for WearEngine + FailureEngine

Generated 2026-05-01 from the documents collected in this directory.

---

## 1. MEL Structure

### Repair Categories (from FAA PL-25 / MMEL BE-200 guidelines)

| Cat | Repair Interval | Meaning |
|-----|----------------|---------|
| A   | Per remarks column | Item-specific; typically 1 flight or 1 flight day |
| B   | 3 consecutive calendar days (72 hrs) | Exclude day of write-up |
| C   | 10 consecutive calendar days (240 hrs) | Exclude day of write-up |
| D   | 120 consecutive calendar days (2,880 hrs) | Exclude day of write-up |

No extension authority on Cat A or D. Operators may extend B/C with FSDO notification within 24 hours.

### MMEL Column Structure (ATA format from BE-200 Rev 15)

```
ATA_ITEM   System_Name        Cat  NumInstalled  NumDispatch  Remarks
61-60-01   Prop Deice (Auto)   C        1            0         May be inop provided Manual Deice operative
```

"NumDispatch = 0" means aircraft can depart with zero operative units (subject to conditions in column 4).
This is the game-mechanic hook: Cat C / dispatch=0 items give the player a MEL deferral window.

### Representative King Air BE-200/BE-300 MMEL Entries

| ATA  | System | Cat | Required | Notes |
|------|--------|-----|----------|-------|
| 21   | Air Conditioning / Pressurization | C | 0 | Manual/alternate available |
| 22   | Autopilot | C | 0 | Ops procedure required |
| 27   | Flight Controls (trim) | C | 0-1 | Depends on axis |
| 28   | Fuel quantity indicators | C | 1 | One per side |
| 30-60| Prop Deice (Auto) | C | 0 | Manual prop deice operative |
| 34   | Navigation/ILS | C or D | varies | GPS/VOR cross-coverage |
| 61   | Propeller systems | C | 0 | Single-engine MEL limits |
| 77   | Engine Indicating | C | 0 | One per engine |

---

## 2. Weibull Shape Parameters by Component Class

### From NASA/CR-2001-210647 (6-year field data, complex GA aircraft)

R(t) = exp(-(t/alpha)^beta). Alpha = characteristic life in flight hours (63.2% cumulative failure).

**Airframe Subsystems**

| Subsystem | Beta | Alpha (hrs) |
|-----------|------|-------------|
| Electrostatic devices | 2.53 | 5,890 |
| Empennage | 1.16 | 5,030 |
| Engine box & cabin fuselage | 1.42 | 6,280 |
| Exterior coatings | 1.45 | 2,990 |
| Seats | 2.66 | 6,770 |
| Wings | 1.79 | 4,250 |

**Aircraft Control Systems**

| Subsystem | Beta | Alpha (hrs) | Mode |
|-----------|------|-------------|------|
| Directional (rudder) | 1.85 | 4,729 | Wear-out |
| Longitudinal (elevator) | 1.57 | 4,718 | Wear-out |
| Lateral (aileron) | 2.25 | 5,844 | Strong wear-out |
| Flaps | 0.95 | 3,956 | Near-random |
| Trim | 0.73 | 2,672 | Infant mortality / random |
| Hydraulic | 1.14 | 3,977 | Near-random |
| Landing gear | 0.92 | 2,896 | Near-random; shortest life |
| Steering | 1.65 | 3,995 | Wear-out |

**Electrical Systems**

| Subsystem | Beta | Alpha (hrs) |
|-----------|------|-------------|
| Lighting | 1.66 | 5,610 |
| Source & distribution | 1.67 | 4,950 |

**Powerplant Subsystems**

| Subsystem | Beta | Alpha (hrs) |
|-----------|------|-------------|
| Engine | 1.58 | 4,830 |
| Fuel system | 1.44 | 5,130 |
| Heating & ventilation | 1.60 | 4,190 |
| Propeller | 1.63 | 3,740 |

### From NASA/TM-2002-211348 (turbine rotating structures)

Weibull slopes for turbine engine structural life: beta = 3, 6, 9 (all strongly wear-out).
At beta=3, blade L0.1 = 9,000 hr gives L5 ~47,391 hr.

### Recommended Defaults for WearEngine/FailureEngine

| Component class | Beta | Alpha (sim hrs) | Rationale |
|----------------|------|-----------------|-----------|
| Engine (piston) | 1.5–1.6 | 2,000–4,000 | NASA; TBO ~2,000 hr |
| Engine (turboprop) | 1.6–2.0 | 4,000–6,000 | TBO 3,500–5,000 hr |
| Propeller | 1.6 | 2,000–3,500 | NASA; prop overhaul ~2,400 hr |
| Landing gear | 0.9–1.0 | 1,500–3,000 | Near-random; actuators |
| Brakes | 1.5–2.0 | 500–2,000 | Cycle-based wear |
| Tires | 1.5–2.5 | 150–400 landings | Condition-based |
| Electrics/avionics | 1.7 | 4,000–6,000 | NASA lighting/dist data |
| Flaps | 1.0 | 3,000–4,000 | Near-random |
| Trim | 0.7–0.9 | 2,500–3,500 | Slight infant mort. |
| Hydraulics | 1.1–1.2 | 3,500–4,500 | Near-random; seal degrad. |
| Control cables | 1.4–1.6 | 4,000–6,000 | Fatigue wear-out |

**Interpretation:** beta<1 = infant mortality (decreasing hazard), beta=1 = random/constant hazard,
1<beta<3 = gradual wear-out, beta>3 = sudden wear-out (tight TBO).

---

## 3. Wear / Consumption Rates

### Oil consumption

**Lycoming formula:**
```
Max consumption (qt/hr) = (0.006 × BHP × 4) / 7.4
```

| Engine HP | Max qt/hr |
|-----------|-----------|
| 160 HP (O-320, C172) | ~0.52 |
| 180 HP (O-360) | ~0.58 |
| 230 HP (O-470) | ~0.75 |
| 310 HP (IO-520) | ~1.00 |

Continental limit (Savvy/EAA 2014): 1 qt/hr for 310-HP TSIO-520-R.
Normal range (new): 0.05–0.15 qt/hr. Concern threshold: >0.5 qt/hr.
Crankcase pressure limits (Continental SB M89-9):
- Small-bore (O-200/O-300): ≤44 mph (AS indicator used as manometer)
- Big-bore (360/470/520/550): ≤90 mph

**WearEngine seed:** oil_rate = 0.08 qt/hr at 0% TBO, 0.35 qt/hr approaching TBO.
Linear 0–80% TBO, exponential 80–100%.

### Tire life (AC 20-97B + Michelin 2021)

- Removal by wear: tread worn to base of any groove at any spot
- Calendar life: 6 years from cure date (molded on sidewall)
- Pressure retention acceptance: ≤5% loss in 24 hours after 12-hr initial stretch
- GA tire life: ~150–400 landings (C172 class)

**WearEngine seed:** base 200 landings. Multipliers: hard landing ×2 wear, crosswind ×1.15,
short/hot runway ×1.3. Replace at ≥90% wear or 6-year calendar.

### Brake wear (AMT Airframe Ch 13)

Minimum lining thickness: manufacturer-specific (~0.100" organic).
Typical GA: 200–600 landings condition-based.
**WearEngine seed:** -0.3%/normal landing; -1.5%/hard braking. Replace at ≤10% remaining.

### Battery SOH (AC 43.13-2B Ch 10)

NiCad: capacity check every 200–400 hrs; replace below 80% rated Ah.
Lead-acid: 3–5 year calendar life for GA; condition-replace for IFR.
No quantitative public SOH curve found. Approximation:
```
capacity_pct = 100 × exp(-0.0005 × total_hrs)
```
Temperature effect: -0.5% per 10°F below 70°F.

---

## 4. Seed Airframe Defaults

### Cessna 172 (piston single, SE MMEL)

| Parameter | Value | Source |
|-----------|-------|--------|
| Engine TBO | 2,000 hr | Lycoming O-320/O-360 |
| Propeller TBO | 2,000 hr | Hartzell constant-speed |
| Engine beta / alpha | 1.58 / 2,000 hr | NASA Table 9 (scaled) |
| Oil max (160 HP) | 0.52 qt/hr | Lycoming formula |
| Oil normal (new) | 0.08 qt/hr | Field average |
| Brake life | 300 landings | Industry average |
| Tire life | 250 landings | Field average |
| Landing gear beta | 0.92 | NASA Table 7 |
| Flap beta | 0.95 | NASA Table 7 |
| MMEL class | SE Rev 2 | FAA single-engine |

### King Air 350 (turboprop twin, BE-300 MMEL Rev 11)

| Parameter | Value | Source |
|-----------|-------|--------|
| Engine TBO (PT6A) | 3,500 hr | P&W estimate |
| Propeller TBO | 2,400 hr | Hartzell 4-blade |
| Engine beta / alpha | 1.58 / 4,000 hr | NASA Table 9 (scaled) |
| Oil normal | 0.05–0.15 qt/hr | Turbine engines less than piston |
| Brake life | 400 landings | Heavier, better technique |
| Tire life | 300–500 landings | Transport-class |
| Prop beta | 1.63 | NASA Table 9 |
| MMEL class | BE-300 Rev 11 | King Air 350 |
| Prop deice (auto) | Cat C, req=0 | ATA 30-60-01 |
| Autopilot | Cat C, req=0 | ATA 22 |

---

## 5. Caveats

1. **OCR two-column interleave:** ac43-13-1b.txt has artifacts in ~5% of lines near figures.
   Use for human reading; do not machine-parse for numerical values without validation.

2. **NASA study vintage (2001):** Field data from mid-1990s GA fleet. Glass cockpit avionics
   will have different (likely better) parameters. Use NASA data as conservative baseline.

3. **SE MMEL operational table not captured:** Only the Final Comment Log was obtainable
   (FSIMS host unreachable). The BE-200/BE-300 MMELs provide the full operational structure.

4. **Battery SOH:** No public quantitative source found. The decay approximation is synthetic.

5. **Michelin manual copyright:** Michelin owns the copyright. The file is vendored here
   because FAA Safety hosts it for pilot training (public access). Remove PDF if redistributing;
   keep text only or link to FAASafety.gov source.
