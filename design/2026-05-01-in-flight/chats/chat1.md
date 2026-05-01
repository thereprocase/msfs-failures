# MSFS Failures UI

_Started 2026-05-01 14:34 UTC_

---

## User

https://github.com/thereprocase/msfs-failures make a UI home screen for this app

## Assistant

I'll explore the repo first to understand what this app does, then ask a few questions before designing.

_[tool: github_get_tree]_

_[tool: github_read_file]_

_[tool: github_read_file]_

I have a strong picture now. This is a maintenance/failures sidecar for MSFS — a desktop WPF app showing fleet, airframes, squawks, wear, hours, etc. Before I design the home screen, let me ask a few focused questions.

_[tool: questions_v2]_

## User

Questions answered:
- home_purpose: Explore a few options
- aesthetic: Maintenance shop terminal — slightly industrial, technical, monospace accents
- platform: Windows desktop app (window chrome, full layout)
- density: Balanced — clear hierarchy, moderate density
- key_modules: Sim connection status (SimConnect + MobiFlight WASM), Last session recap (max G, overtemps, hard landings), Quick actions (open squawk, defer, service, ground), Currently-loaded aircraft / live state
- fleet_size: 5
- fleet_mix: Piper PA-28 (light single), King Air 350, Cessna 172 (light single), anything BlackSquare makes
- live_state: Yes but only when one's actually loaded up in the sim. 
- interactions: Toggle between Fleet / Squawks / Maintenance views, Sim-connect status toggle (mock connect/disconnect), Click an airframe to open its detail panel
- variant_axes: Visual style (muted vs glass-cockpit vs maintenance-shop), Information hierarchy (fleet-first vs alerts-first vs live-first), Layout (sidebar vs top-nav vs split-pane)
- variants: 4
- tone: Mix — technical labels, plain explanations

## Assistant

I have what I need. Building 4 home-screen variants on a design canvas, all in the maintenance-shop-terminal aesthetic but exploring different layouts and information hierarchies.

**System:** Mono accents (JetBrains Mono), Inter for UI, dark warm-neutral backgrounds, amber/green/red status colors. Windows 11 chrome. 5 airframes: 2× C172, 1× PA-28, 1× King Air 350, 1× Black Square Bonanza/Duke (BlackSquare-style GA twins). Live state only when sim-loaded.

Variants:
1. **Fleet-first sidebar** — left nav, fleet grid, calm
2. **Alerts-first split-pane** — squawks dominate, fleet secondary
3. **Live-first dispatch** — large currently-flying panel + dense fleet ribbon
4. **Top-nav terminal** — dense data terminal, top tabs, monospace-heavy

_[tool: update_todos]_

_[tool: copy_starter_component]_

_[tool: copy_starter_component]_

I'll skip macos-window since the user wants Windows chrome. Let me build a custom Windows 11 frame and the variants.

_[tool: delete_file]_

_[tool: read_file]_

Good, I have what I need. Now let me build the data and shared components.

_[tool: write_file]_

_[tool: write_file]_

_[tool: snip]_

Now let me build the four variants. I'll start with shared atoms then each variant.

_[tool: write_file]_

_[tool: snip]_

_[tool: write_file]_

_[tool: snip]_

_[tool: write_file]_

_[tool: snip]_

_[tool: write_file]_

_[tool: snip]_

_[tool: write_file]_

_[tool: snip]_

_[tool: done]_

_[tool: snip]_

_[tool: fork_verifier_agent]_

Four home-screen variants for **msfs-failures** are on the design canvas:

- **V1 · Fleet-first sidebar** — calm, balanced; left nav with Hangar/Squawks/Maintenance/Sessions, fleet grid + detail rail
- **V2 · Alerts-first split** — top-tab dispatch view; prioritized squawk feed dominates, compact fleet column on the right
- **V3 · Live-first dispatch** — glass-cockpit hero with full PFD/engine gauges for the aircraft currently in sim, fleet ribbon below
- **V4 · Terminal** — densest, mono-heavy logbook software; tabular fleet manifest, terminal-style status header/footer

All share: warm-neutral dark, JetBrains Mono for tail numbers / hours / cycles / IDs, Inter for narrative copy, color reserved for state (green airworthy, amber MX-due, orange squawk, red grounding). Live aircraft panel only renders when sim is connected.

**Working interactions:** tab switching, fleet selection, squawk row expansion (V2), and the sim-connect toggle on each variant (try toggling — V3's hero swaps to an offline state, V4's terminal flips green/red).

Fleet: N172AB (C172), N812RP (PA-28), N350KA (King Air 350, currently in sim), N58BS (Black Square Baron 58), N210BS (Black Square C210, grounded).

## User

I like the v4 terminal layout. make a single screen mockup of an "In Flight" tab for that showing me in a 208 in-flight, with some little nice to haves in the window.

## Assistant

I'll build an "In Flight" tab for the V4 terminal showing a Cessna 208 Caravan with rich live telemetry and thoughtful extras.

_[tool: snip]_

_[tool: read_file]_

I have what I need. Let me build a focused "In Flight" tab — single-screen mock of the V4 terminal showing a 208 in flight with thoughtful extras (engine trends, wear-rate live ticker, accelerator status, route strip, sim binding info, a recorded-this-flight events log).

_[tool: write_file]_

_[tool: snip]_

_[tool: done]_

_[tool: snip]_

_[tool: fork_verifier_agent]_

The "In Flight" tab is built — **N208RC** (Cessna 208B Grand Caravan EX) in cruise from KAPA → KSEZ at 9,500 ft.

**Layout** (V4 terminal, with `IN FLIGHT` tab now active and a green pulse next to it):

- **Top:** terminal header strip + tab bar + scrolling live readout (ALT/VSI/IAS/TAS/GS/HDG/OAT/ITT/TQ/FF/FUEL)
- **Hero (left):** big 6-up flight tape · ALT, IAS, GS, TAS, HDG, OAT
- **Route (right):** KAPA → DBL → JNC → LKV → KSEZ progress rail with current position pulsing on amber, plus DTK / dist remaining / ETE / ETA
- **Engine (PT6A-140):** ITT, Torque, Ng, Np gauges with caution/redline tick marks, oil temp + pressure below, plus 5-minute trend sparklines for ITT and Torque (NOW / PEAK / AVG)
- **Fuel:** on-board, fuel flow, endurance, L/R tank bars, and reserve-above-45-min readout at destination
- **Session:** duration, hobbs Δ, cycles +1, max G, max IAS, overtemp/overspeed/hard-landing counters
- **Right column nice-to-haves:**
  - **Airframe inspect** — tail, model, hobbs, cycles, engine TTSN, prop SOH, hours since MX, next 100-hr · with quick `+ SQUAWK / MARK NOTE / SNAPSHOT` buttons (drop a marker mid-flight you can review on landing)
  - **Wear engine live tick** — per-component wear rate per hour with category accelerator multipliers (showing tires/brakes idle in cruise, vacuum pump at ×1.05 from earlier climb load)
  - **Accelerators** — the 6 rules being evaluated each tick (OVERTEMP_ITT, OVERSPEED_PROP, HARD_LANDING…) with their formulas visible; "NONE FIRING" right now
  - **Events · this flight** — timestamped ticker (LIFTOFF, cycle increment, ITT peak, TOC, oil consumed) with a blinking `tail -f` cursor
- **Status bar:** RECORDING · TICK 4 Hz · WEAR-ENGINE OK · FAILURE-ENGINE OK · session ID + sample count + delta written to fleet.db

## User

Verifier agent check completed

## Assistant

_[tool: snip]_

_[tool: str_replace_edit]_

I also need to make Block stop forcing inner overflow:auto so the panels stack naturally in flex flow, otherwise they'll still fight for height.

_[tool: snip]_

_[tool: str_replace_edit]_

_[tool: snip]_

_[tool: copy_files]_

_[tool: snip]_

Now I'll redesign v2 to lean into the "decision surfaces" model — health %, thermal margin, abuse history, maintenance debt, dispatch posture. Let me rewrite it from scratch since the changes are substantial.

_[tool: write_file]_

_[tool: snip]_

_[tool: done]_

_[tool: snip]_

_[tool: fork_verifier_agent]_

**In Flight v2** is up — same terminal chassis, but reorganized around the "decision surfaces" model from the captain's-perspective notes.

**What changed from v1:**

- **Top hero is now Dispatch Posture** — three big surfaces side-by-side: Engine Condition Index donut (74%), Thermal Margin (live ITT vs limit + budget left + overtemp debt), Fuel + Endurance (with contamination status). These are the "would I dispatch this airframe right now?" reads.
- **Subsystem health grid (8 indices)** — engine, hot section, compressor, oil, fuel, prop, gear/brakes, electrical — each as a derived 0–100% with one-line context. Compressor flags amber at 61% with "wash due."
- **Engine deep-dive** keeps the PT6A gauges (ITT/TQ/Ng/Np) + 5-min trends, but is now part of a system, not the lead.
- **Pilot Habits · 30-day rolling** — the "killing it slowly" surface: hot-start streak, torque exceedances, ITT overtemp debt, mean climb torque vs book, hard landings, beta compliance, hours-since-wash, fuel sample interval. 2 soft flags right now (climb torque +8%, wash overdue).
- **Performance Drift** — same TQ → climb rate, TAS, fuel burn, compressor efficiency, all vs new-engine baseline. Surfaces the "fuel burn creep" insight.
- **Latent Precursors** — silent accumulators: compressor fouling, fuel nozzle coking, bearing wear (oil debris ppm), prop erosion, brake thermal load, strut packing.
- **Maintenance Debt** — MEL-style: 1 overdue (compressor wash), 1 deferred (vacuum filter), plus 100-hr / oil / HSI countdowns.
- **Oil + Fuel** — 15-flight oil quantity trend with rate vs baseline (0.12 vs 0.08 qt/h, amber), fuel contamination risk panel with sample age + filter ΔP + heater + departure humidity.
- **Post-flight projection** — what *this leg* will cost the airframe at touchdown: hot-section hours, compressor efficiency Δ, oil consumed, tire/brake wear, cycles +1.
- Route + Airframe + Events still anchor the bottom row, but smaller and after the decision content.

Tabs and chrome are unchanged so the two versions compare cleanly. Verifier is running in the background.

## User

can you do a TBM 950 version that looks EXACTLY like the EFB a real TBM 950 operator would be reviewing prior to takeoff for MX items?

## Assistant

_[tool: snip]_

_[tool: list_files]_

I'll think about what a real TBM 950 EFB pre-flight MX review screen actually looks like. The TBM 950 uses the Garmin G3000 cockpit and Daher's CMP (Centralized Maintenance Page) / EFB ecosystem — operators typically use **ForeFlight** or **Garmin Pilot** plus the **Daher Me & My TBM** app, with airframe data pulled from the Garmin GDU. The EFB pre-flight MX view is utilitarian, light-themed (sunlight-readable on iPad), uses Daher's brand red, and shows airframe time, life-limited components, AD/SB compliance, deferred items, and oil/fuel.

Let me build this with proper Daher branding aesthetics — light EFB UI, app bar, tabbed sections, real TBM 950 system limits.

_[tool: write_file]_

_[tool: snip]_

_[tool: done]_

_[tool: snip]_

_[tool: fork_verifier_agent]_

**TBM 950 pre-flight EFB** is up — rendered as Daher's *Me & My TBM* iPad app, light theme, sunlight-readable, with the Daher red as the only brand accent.

**Layout (1194×834, iPad Pro landscape):**

- **iOS status bar** + **app bar** with Daher mark, "Pre-Flight Review" page title, search (⌘K), refresh, menu, and PIC profile (M. Becker · ATP/SE-Turboprop)
- **Aircraft strip** — N950DT · TBM 950 · MSN 1421 · S/N PT6A-66D / RB-0814, AIRWORTHY pill, "2 ITEMS DUE ≤10 H" amber pill, KPIs (Airframe Time / Cycles / Engine Time / Hobbs), big red **RELEASE FOR FLIGHT** action
- **Tab nav** — Pre-Flight (active, red underline) · Logbook · Limitations · AD/SB · Squawks · Inspections · Fuel & Oil · Engine Trend · CAS History · Documents
- **Left rail (260px):** Dispatch Posture donut (86% READINESS, "Cleared for IFR · day/night") · Quick Actions grid · Crew & Currency (medical/FR/IPC/type-specific with day countdowns) · Insurance summary
- **Main column:**
  - **Amber release banner** — "Aircraft is airworthy. Two items require attention…" with Acknowledge / Defer with MEL buttons
  - **Inspections & Time Limits** — 50-hr, 100-hr, Annual (CFR 91.409), Pitot/Static & XPDR (91.411/.413), ELT, HSI 1800-h, **Compressor Wash overdue −47 h**, **O₂ hydro due 6.4 h (SB 70-001)**, Gear OH @3000 cyc — each with progress bar, last compliance, A&P/IA basis
  - **Open Squawks** — 5 entries with OPEN/MEL/MON tags, IDs (SQ-2026-014…), MEL expiry dates, work order refs
  - **Fluids & Consumables** — Jet A-1, AeroShell 560 oil, **Oxygen 1420/1800 psi (red, below 1500 dispatch min)**, hydraulic, washer
  - **AD / SB** — 2024-13-08, 2023-21-04 (PT6A fuel pump/hose), SB 70-001 (chip detector, due), SB 53-018 (aft pressure bulkhead), SB 25-014 R2
  - **PT6A-66D last-flight snapshot** — peak ITT 742°C, peak TQ 95.2%, peak Ng 101.4%, hottest start 682°C, oil consumed, fuel burn drift, compressor η 87.3% (was 92.0)
  - **Life-Limited Parts** — CT disk, PT disk, CT blades, RGB sun gear, Hartzell 5-blade hub with P/N + S/N, cycles used / life limit
  - **Logbook · last 10** — FLT/SQK/MX entries, e-Logbook signed
- **Footer** — "Daher Me & My TBM · v4.8.2 · iPad EFB · Data synced 14 s ago via TBM Connect"

Numbers are realistic for a TBM 950 / PT6A-66D operator — 100-hr / 50-hr phase intervals, 1800-h HSI, 3000-cyc gear OH, MEL deferral with expiry dates, CFR 91.409/.411/.413 references, AD/SB compliance ledger. The whole thing reads as the screen a TBM owner-pilot or chief pilot would actually scroll through at the FBO before signing the release.

