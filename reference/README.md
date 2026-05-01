# Reference

Third-party source, docs, and prior art collected so we can consult it offline as we build out the app. **Nothing here is production code — it is reference material.** When we copy-paste anything, we vendor it into `src/` with attribution.

Each subdirectory has its own `MANIFEST.md` describing what was collected, why, license, and the upstream URL/commit.

## Layout

| Dir | Topic | Why we care |
|---|---|---|
| `simconnect/`     | SimConnect SDK + managed bindings + MS samples | Stock failure events, A:Var subscription, aircraft state |
| `mobiflight-wasm/`| MobiFlight WASM module + Connector LVar bridge code | Read/write any L:Var on any aircraft (third-party support) |
| `fsuipc/`         | FSUIPC7 docs + offsets + .NET wrapper | Optional secondary I/O surface where users have it |
| `aircraft-systems/`| FBW A32NX failure system, JSBSim engine, others | How real sim aircraft model failures and wear |
| `reliability/`    | MTBF math, FAA AC 43.13, MEL structure, aviation wear refs | Grounds the WearEngine and FailureEngine |
| `career-prior-art/`| Public docs from Neofly / OnAir / FSEconomy | What "career & economy" means in the sim community |
| `wpf-ui/`         | Dense-data WPF dashboards, monospace UIs, sparkline impls | Prior art for the rest of the screens |

## What's NOT here

- Anything paid or under a non-redistributable license. If a thing is paywalled, the manifest links to it and notes how to obtain it.
- Anything that would bloat the repo past ~50 MB. Large samples get a fetch script instead of vendored bytes.
