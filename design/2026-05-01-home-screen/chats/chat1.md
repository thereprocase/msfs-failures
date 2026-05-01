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

