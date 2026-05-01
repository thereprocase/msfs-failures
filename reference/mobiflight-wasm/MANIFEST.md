# MobiFlight WASM Bridge — Reference Materials

## wasm-module/

**Upstream:** https://github.com/MobiFlight/MobiFlight-WASM-Module

**License:** MIT (Sebastian Moebius, MobiFlight; see LICENSE.md)

**Purpose:** WASM module for Microsoft Flight Simulator 2020 that enables external SimConnect clients to send events to the sim, read L:Vars (local variables), A-Variables, and execute gauge calculator code. The module creates shared memory areas (SimConnect client-data regions) for bidirectional communication with third-party applications.

**Key files for integrators:**
- `src/Sources/Code/Module.cpp` — Main WASM module implementing the protocol dispatcher. Handles all client commands (MF.Ping, MF.SimVars.Add, MF.LVars.List, etc.), manages client registration, and updates L:Var values at frame time.
- Protocol constants: client-data area names (lines 23–27); message size 1024 bytes (line 29); string var size 128 bytes (line 45).
- Client registration and data-area setup: RegisterNewClient() (line 519), RegisterClientDataArea() (line 461).
- Command dispatcher: MyDispatchProc() (line 632) parses all incoming commands.

## connector-wasm-bridge/

**Upstream:** https://github.com/MobiFlight/MobiFlight-Connector

**License:** GPL (see WasmModuleClient.cs header or MobiFlight-Connector repository)

**Files:**
- WasmModuleClient.cs — Static helper methods to send commands to the WASM module
- WasmModuleClientData.cs — Configuration struct for a client (area IDs, definition IDs, offsets)
- SimConnectDefinitions.cs — Data structures (ClientDataValue, ClientDataString, etc.) and enum definitions
- SimConnectCache.cs — Full SimConnect lifecycle: connection, client-data initialization, receive callbacks, SimVar registration and retrieval

## Why We Care

The MobiFlight WASM bridge is the canonical L:Var access protocol over SimConnect's client-data area. It allows third-party MSFS aircraft and tools to expose arbitrary L:Vars and A-Variables to external applications (home cockpits, overlays, telemetry) without direct gauge access. By registering custom clients and subscribing to variables, any external app can read live sim state and write K:Events, enabling hardware integration impossible through standard SimConnect alone.
