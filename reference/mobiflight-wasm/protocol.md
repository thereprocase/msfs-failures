# MobiFlight L:Var Bridge Protocol

## Client-Data Areas

The WASM module creates four shared memory regions per client (SimConnectCache.cs:78–98):

- **`{ClientName}.LVars`** — Float SimVar values (4096 bytes; Module.cpp:467)
- **`{ClientName}.StringVars`** — String SimVar values (8 KB max; Module.cpp:488)
- **`{ClientName}.Command`** — Command input from client (1024 bytes; Module.cpp:481)
- **`{ClientName}.Response`** — Response output from WASM (1024 bytes; Module.cpp:474)

Initial "MobiFlight" client is created at module init (Module.cpp:612); additional clients request registration via `MF.Clients.Add.{Name}` command.

## Request Format

Commands are **null-terminated ASCII strings** sent via `SetClientData()` to the Command area (WasmModuleClient.cs:47–53). Max payload: **1024 bytes** (Module.cpp:29). String payload is marshaled as `byte[1024]` with ASCII encoding (SimConnectDefinitions.cs:22–34).

Core commands (Module.cpp:651–719):
- `MF.Ping` — Heartbeat
- `MF.LVars.List` — Enumerate available L:Vars
- `MF.SimVars.Add.{GaugeCode}` — Register float SimVar; code is executed via `execute_calculator_code()` (Module.cpp:690–693)
- `MF.SimVars.AddString.{GaugeCode}` — Register string SimVar (Module.cpp:697–702)
- `MF.SimVars.Set.{GaugeCode}` — Execute gauge code (K:Event or variable write; Module.cpp:677–684)
- `MF.SimVars.Clear` — Unsubscribe from all variables (Module.cpp:656–658)
- `MF.Clients.Add.{ClientName}` — Register new client (Module.cpp:705–710)
- `MF.Config.MAX_VARS_PER_FRAME.Set.{N}` — Tune frame-update budget (Module.cpp:713–718)

## Response Format

Responses are **null-terminated ASCII strings** (max 1024 bytes) written to the Response area via `SendResponse()` (Module.cpp:202–211). Client subscribes via `RequestClientData(..., SIMCONNECT_PERIOD_ON_SET)` (SimConnectCache.cs:305–314). Response struct is `ResponseString` with 1024-byte `Data` field (SimConnectDefinitions.cs:37–41).

Variable values (float or string) are written to their respective areas (LVars, StringVars) as they are read/registered:
- **Float:** single 4-byte `ClientDataValue.data` at computed offset (Module.cpp:268–275)
- **String:** up to 128-byte null-terminated ASCII at offset (Module.cpp:245–263)

## Lifecycle

1. **Connect:** Client calls `MapClientDataNameToID()` and `CreateClientData()` for the four default areas (SimConnectCache.cs:286–299).
2. **Register:** Client sends `MF.Ping` on Command area; WASM responds `MF.Pong` (Module.cpp:651–652). If first client, WASM registers default "MobiFlight" client. Second client sends `MF.Clients.Add.{Name}` and waits for `MF.Clients.Add.{Name}.Finished` response (SimConnectCache.cs:336–342).
3. **Subscribe:** Client calls `AddToClientDataDefinition()` and `RequestClientData()` for each variable it wants (SimConnectCache.cs:559–577 for float, 589–607 for string). Sends `MF.SimVars.Add.{GaugeCode}` or `MF.SimVars.AddString.{GaugeCode}` to Command area.
4. **Read:** Each frame (EVENT_FRAME), WASM reads up to `MAX_VARS_PER_FRAME` variables in round-robin (Module.cpp:435–454), executes gauge code, and writes values to LVars/StringVars areas.
5. **Write:** Client sends K:Event commands via `MF.SimVars.Set.{GaugeCode}` (e.g., `MF.SimVars.Set.5 (>L:MyVar)`; Module.cpp:677–684).
6. **Unsubscribe:** Client sends `MF.SimVars.Clear` (Module.cpp:656) or disconnects; WASM zeroes and clears tracked variables (Module.cpp:378–398).

## Gotchas

**Size limits:**
- Command/Response payload: 1024 bytes (Module.cpp:29); clients must send short ASCII commands.
- Float SimVar: 4 bytes per variable (Module.cpp:296); offset = index × 4.
- String SimVar: 128 bytes per variable (Module.cpp:45); offset = index × 128; max 64 strings (8 KB total, Module.cpp:49–51). No overflow; truncated to 128 bytes (Module.cpp:369).
- L:Var list enumeration: enumerated by calling `get_name_of_named_variable(i)` for i ∈ [0, 1000) (Module.cpp:228); capped at 1000 L:Vars.

**Encoding:** All strings are ASCII (SimConnectDefinitions.cs:30, Module.cpp:643). Non-ASCII will corrupt.

**Threading:** Single frame callback (EVENT_FRAME, Module.cpp:723–727); variable reads are throttled at `MOBIFLIGHT_MAX_VARS_PER_FRAME` per frame (default 30, Module.cpp:41, 440–453). Rolling round-robin read index prevents starvation but means high-count subscriptions have multi-frame latency.

**Sequencing:** First SetClientData after client startup may be silently dropped (wasm-module/README.md:57); client should send dummy command first (WasmModuleClient.cs:18).

**Definition ID allocation:** Client 0 reserves definition IDs 1000–19999 for float SimVars, 20000–29999 for string SimVars. Client N reserves offset by `N × 20000` (Module.cpp:549–550). Max ~10 clients before collision (Module.cpp:36–37).

**Client reconnect:** Existing clients can re-register by name without losing subscriptions (Module.cpp:524–529).
