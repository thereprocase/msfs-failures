# C208B Improvement Mod — MSFS Wear & Failure Reference

## Source
- Repo: https://github.com/SheepCreativeSoftware/msfs2020-C208-Improvment-Mod
- Cloned: 2026-05-01, `--depth=1`
- License: Microsoft "Game Content Usage Rules" — community mod using MSFS assets. **Do not redistribute** as a standalone product. Kept for code pattern reference only.
- Binary assets (DDS textures, GLTF models, WAV sounds, FX effects) have been deleted to save space.

## Note on naming
Despite the "xplane-plugins" directory name, this is an MSFS (Microsoft Flight Simulator 2020) mod for the Cessna 208B Grand Caravan. No suitable open-source X-Plane failure/wear plugin was found. This MSFS mod is the closest prior art found in the open-source flight sim ecosystem.

## Why This Is Valuable
This mod implements the only open-source **stochastic wear-to-failure** model for the PT6A-powered C208B found during research. It uses MSFS XML simulation variables to implement:

### Engine Overstress → Wear → Failure Chain (from README.md)
```
4 minutes above max torque limit
  → CAS: "METAL CHIPS IN OIL" warning
  → failure probability accumulates
  → average time to engine failure: 100 minutes (stochastic)
  → each additional second above torque limit: -8 seconds from expected failure time
  → MSFS Assistance Options can enable/disable this
```

This is the key pattern: **accumulated stress time** reduces a **mean-time-to-failure** parameter, and a **random draw** against that distribution determines when failure actually occurs.

### Other failure/degradation implementations
- **Stall sensor**: after 5 minutes of ice accumulation without heat, stall sensor either silently stops or permanently triggers (random each occurrence). Recovery requires heater ON + 60-120 seconds melt time.
- **Starter-Generator**: operates in starter mode below 46% Ng, generator above 46% — models the real Cessna combined unit behavior.
- **Anti-ice fluid depletion**: 20.8 gallon tank, multiple consumption modes, monitored on PFD; refills only on new flight (persistence model).

## Key Files (after pruning)
- `README.md` — Full feature description including failure mechanics
- `C208B-mod/SimObjects/Airplanes/Asobo_208B_GRAND_CARAVAN_EX/engines.cfg` — Engine type 5 (turboprop), PT6A parameters including N1/N2 tables, TSFC, rated N2 RPM (33,000)
- `C208B-mod/SimObjects/Airplanes/Asobo_208B_GRAND_CARAVAN_EX/systems.cfg` — Full electrical bus topology, hydraulic, pneumatic, pitot-static, vacuum, stall warning, autopilot
- `C208B-mod/SimObjects/Airplanes/Asobo_208B_GRAND_CARAVAN_EX/aircraft.cfg` — Aircraft-level config
- `C208B-mod/SimObjects/Airplanes/Asobo_208B_GRAND_CARAVAN_EX/flight_model.cfg` — Aerodynamics

## PDFs
None.
