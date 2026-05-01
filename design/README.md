# Design artifacts

Permanent record of design sessions for `msfs-failures`. Each subdirectory is a verbatim export from Claude Design (claude.ai/design) — HTML/JSX prototypes, the original chat transcript, and the bundle README.

These files are **reference only**. The implemented app lives in `src/`. When the prototypes and the implementation drift, the implementation is the source of truth.

## Sessions

### `2026-05-01-home-screen/`

Four home-screen variants in a maintenance-shop terminal aesthetic. We picked **V4 · Terminal** (densest, mono-heavy, top-nav data terminal) and that variant is what `MsfsFailures.App`'s home screen implements.

- Entry: `project/Home Screen Variants.html`
- Chosen variant: `project/v4-terminal.jsx`
- Shared data: `project/data.jsx`
- Chat: `chats/chat1.md`

### `2026-05-01-in-flight/`

In-flight tab — a dense single-page "health surface" screen that stays open while flying. Centered on derived health indices (engine condition, hot section, compressor, oil, fuel, prop, gear/brakes, electrical), thermal margin, fuel endurance, pilot habits, performance drift vs. new-engine baseline, latent precursors, maintenance debt, route progress, post-flight wear projection, and an event log.

- Entry: `project/In Flight Tab v2.html`
- Chat: `chats/chat1.md`
