# SimConnect Failure Event IDs

Stock MSFS failures are triggered by sending **client events** via `SimConnect.TransmitClientEvent()`.
All events below are in the `SIMCONNECT_GROUP_PRIORITY_HIGHEST` group and are toggling — send once
to enable a failure, send again to clear it.

## How to Send a Failure Event (C#)

```csharp
// 1. Map the event name to a local ID (do this once, e.g. in OnRecvOpen)
simconnect.MapClientEventToSimEvent(MyEvents.ToggleEngine1Failure, "TOGGLE_ENGINE1_FAILURE");

// 2. Add to a notification group (optional but common practice)
simconnect.AddClientEventToNotificationGroup(MyGroups.Failures, MyEvents.ToggleEngine1Failure, false);

// 3. Transmit the event
simconnect.TransmitClientEvent(
    SimConnect.SIMCONNECT_OBJECT_ID_USER,
    MyEvents.ToggleEngine1Failure,
    0,                                           // dwData (unused for toggles)
    SIMCONNECT_GROUP_PRIORITY.HIGHEST,
    SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
```

## Complete TOGGLE_*_FAILURE Event List

These are all stock failure events as of MSFS SDK 0.24.x, sourced from the official Event IDs
documentation and the FSX/MSFS community event list (paruljain/fsx EventIds.txt).

### Engine Failures

| Event Name            | Description                 | Shared Cockpit |
|-----------------------|-----------------------------|----------------|
| `TOGGLE_ENGINE1_FAILURE` | Toggle engine 1 failure  | Yes            |
| `TOGGLE_ENGINE2_FAILURE` | Toggle engine 2 failure  | Yes            |
| `TOGGLE_ENGINE3_FAILURE` | Toggle engine 3 failure  | Yes            |
| `TOGGLE_ENGINE4_FAILURE` | Toggle engine 4 failure  | Yes            |

### Hydraulics & Brakes

| Event Name                  | Description                    | Shared Cockpit |
|-----------------------------|-------------------------------|----------------|
| `TOGGLE_HYDRAULIC_FAILURE`  | Toggle hydraulic system failure | Yes           |
| `TOGGLE_TOTAL_BRAKE_FAILURE`| Toggle brake failure (both)   | Yes            |
| `TOGGLE_LEFT_BRAKE_FAILURE` | Toggle left brake failure     | Yes            |
| `TOGGLE_RIGHT_BRAKE_FAILURE`| Toggle right brake failure    | Yes            |

### Instruments / Pitot-Static

| Event Name                    | Description                        | Shared Cockpit |
|-------------------------------|------------------------------------|----------------|
| `TOGGLE_VACUUM_FAILURE`       | Toggle vacuum system failure       | Yes            |
| `TOGGLE_PITOT_BLOCKAGE`       | Toggle blocked pitot tube          | Yes            |
| `TOGGLE_STATIC_PORT_BLOCKAGE` | Toggle blocked static port         | Yes            |

### Electrical

| Event Name                   | Description                       | Shared Cockpit |
|------------------------------|----------------------------------|----------------|
| `TOGGLE_ELECTRICAL_FAILURE`  | Toggle electrical system failure | Yes            |

## Notes

- All events are **toggles** — the underlying failure state is a boolean; a second transmit clears it.
- "Shared Cockpit" = the event propagates to all connected clients in a shared-cockpit session.
- There is **no** stock `TOGGLE_AVIONICS_FAILURE`, `TOGGLE_GYRO_FAILURE`, or `TOGGLE_FUEL_FAILURE`
  in the standard event set. Custom failures for these must be implemented via WASM gauge variables
  or aircraft-specific events.
- The `dwData` parameter has no effect for these events; pass `0`.
- To **read back** current failure state, poll SimVars such as `"BRAKE LEFT POSITION"`,
  `"HYDRAULIC PRESSURE:1"`, or `"RECIP ENG STARTER ACTIVE:1"` — there is no dedicated
  "engine-1-is-failed" SimVar in stock MSFS.

## Sources

- Official MSFS SDK Event IDs page: https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Aircraft_Misc_Events.htm
- Community event list mirror: https://github.com/paruljain/fsx/blob/master/EventIds.txt
- Python-SimConnect EventList (confirms same set): https://github.com/odwdinc/Python-SimConnect/blob/master/SimConnect/EventList.py
