# Aircraft Detection via SimConnect

Detecting the loaded aircraft from a sidecar process uses SimVar data requests.
The canonical pattern is to request a "title block" struct once per second (or on change)
and parse the returned strings.

## Key SimVars for Aircraft Identification

| SimVar Name       | SIMCONNECT_DATATYPE | Max Length | Description                                    |
|-------------------|---------------------|-----------|------------------------------------------------|
| `Title`           | STRING256           | 256 chars | Full title from aircraft.cfg — most specific   |
| `ATC MODEL`       | STRING32            | 30 chars  | ICAO model code used by ATC (e.g. "B738")      |
| `ATC TYPE`        | STRING32            | 30 chars  | ICAO type (e.g. "B737" family)                 |
| `ATC AIRLINE`     | STRING64            | 50 chars  | Airline name (e.g. "Southwest Airlines")       |
| `ATC ID`          | STRING16            | 10 chars  | Tail/registration number (e.g. "N12345")       |
| `ATC FLIGHT NUMBER` | STRING8           | 6 chars   | Flight number if set (e.g. "WN1234")           |

> Sources: MSFS SDK SimVars → Aircraft Radio Navigation Variables and Aircraft Misc Variables pages.

## Recommended Detection Strategy

Community apps (MSFS Universal Announcer, FsConnect examples) use this priority order:

1. **`ATC MODEL`** — short ICAO code, unambiguous, good for matching aircraft families.
   Match against a lookup table (e.g. `"B738"` → 737-800, `"A320"` → A320).
2. **`Title`** — full string from aircraft.cfg. Unique per livery but can be long.
   Use for when you need to distinguish between variants or liveries.
3. **`ATC TYPE`** — broader family code; use when `ATC MODEL` is unavailable or blank.

## C# Example: Requesting Aircraft Identity

```csharp
// ---- Enums ----
enum Definitions { AircraftId }
enum Requests    { AircraftId }

// ---- Struct (Sequential layout required) ----
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
struct AircraftIdData
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Title;       // "Asobo Boeing 737-800 Air France"

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string AtcModel;    // "B738"

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string AtcType;     // "B737"

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string AtcId;       // "N12345"
}

// ---- Registration (in OnRecvOpen handler) ----
sc.AddToDataDefinition(Definitions.AircraftId, "Title",      null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
sc.AddToDataDefinition(Definitions.AircraftId, "ATC MODEL",  null, SIMCONNECT_DATATYPE.STRING32,  0, SimConnect.SIMCONNECT_UNUSED);
sc.AddToDataDefinition(Definitions.AircraftId, "ATC TYPE",   null, SIMCONNECT_DATATYPE.STRING32,  0, SimConnect.SIMCONNECT_UNUSED);
sc.AddToDataDefinition(Definitions.AircraftId, "ATC ID",     null, SIMCONNECT_DATATYPE.STRING32,  0, SimConnect.SIMCONNECT_UNUSED);
sc.RegisterDataDefineStruct<AircraftIdData>(Definitions.AircraftId);

// Poll once per second, only on change
sc.RequestDataOnSimObject(
    Requests.AircraftId,
    Definitions.AircraftId,
    SimConnect.SIMCONNECT_OBJECT_ID_USER,
    SIMCONNECT_PERIOD.SECOND,
    SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
    0, 0, 0);

// ---- Receive ----
sc.OnRecvSimobjectData += (sender, data) => {
    if (data.dwRequestID == (uint)Requests.AircraftId)
    {
        var id = (AircraftIdData)data.dwData[0];
        Console.WriteLine($"Title={id.Title} Model={id.AtcModel} Type={id.AtcType} Reg={id.AtcId}");
    }
};
```

## Detecting Aircraft Load / Change Events

SimConnect fires `OnRecvEventFilename` when the aircraft is loaded or changed:

```csharp
sc.SubscribeToSystemEvent(MyEvents.AircraftLoaded, "AircraftLoaded");
sc.OnRecvEventFilename += (sender, data) => {
    // data.szFileName = path to the loaded aircraft.cfg
    Console.WriteLine($"Aircraft loaded: {data.szFileName}");
    // Re-request identity data here
};
```

> **MSFS 2024 caveat**: with the new modular aircraft format, `AircraftLoaded` fires with the
> per-preset subset aircraft.cfg, not the full one. Re-read `Title`/`ATC MODEL` via a data
> request for the authoritative identity after this event.

## Aircraft Family Matching Pattern

To classify rather than exact-match, maintain a lookup table keyed on `ATC MODEL` prefix:

```csharp
static string Classify(string atcModel) => atcModel switch {
    var m when m.StartsWith("B73") => "Boeing 737",
    var m when m.StartsWith("B74") => "Boeing 747",
    var m when m.StartsWith("B75") => "Boeing 757",
    var m when m.StartsWith("B76") => "Boeing 767",
    var m when m.StartsWith("B77") => "Boeing 777",
    var m when m.StartsWith("B78") => "Boeing 787",
    var m when m.StartsWith("A31") => "Airbus A310",
    var m when m.StartsWith("A31") => "Airbus A310",
    var m when m.StartsWith("A32") => "Airbus A320 family",
    var m when m.StartsWith("A33") => "Airbus A330",
    var m when m.StartsWith("A34") => "Airbus A340",
    var m when m.StartsWith("A35") => "Airbus A350",
    var m when m.StartsWith("A38") => "Airbus A380",
    var m when m.StartsWith("C17") => "Boeing C-17",
    _                               => $"Unknown ({atcModel})"
};
```

## Sources

- MSFS SDK SimVars reference (aircraft identity fields):
  https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Aircraft_SimVars/Aircraft_RadioNavigation_Variables.htm
- MSFS Universal Announcer detection logic (ATC MODEL + TITLE priority):
  https://fearlessfrog.github.io/MSFS_Universal_Announcer/statemachine.html
- MSFS DevSupport — AircraftLoaded modular caveat:
  https://devsupport.flightsimulator.com/t/msfs2024-modular-package-simconnect-aircraftloaded-only-serves-the-preset-aircraft-cfg/15386
- FsDeveloper thread — ATC ID registration:
  https://www.fsdeveloper.com/forum/threads/trying-to-get-aircraft-registration-via-simconnect.459006/
