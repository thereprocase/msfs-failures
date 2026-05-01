# SimConnect Managed (C#) Primer

A condensed guide for C# .NET 8 WPF sidecar apps connecting to MSFS via SimConnect.

## Prerequisites

1. **Install MSFS SDK** (via MSFS Developer Mode → SDK). Default location:
   `C:\MSFS SDK\` (MSFS 2020) or `C:\MSFS 2024 SDK\`
2. **Reference the managed wrapper** — two files from `<SDK>\SimConnect SDK\lib\`:
   - `managed\Microsoft.FlightSimulator.SimConnect.dll` — .NET assembly (add as project reference)
   - `SimConnect.dll` — native x64 DLL (copy to output dir; set "Copy to Output Directory = Always")
3. **Target x64** — SimConnect is 64-bit only. Set platform target to `x64` in project settings.
4. **Target Windows** — add `<TargetFramework>net8.0-windows</TargetFramework>` in .csproj.

> The managed DLL is distributed with the MSFS SDK under the SDK EULA. It is **not** independently
> redistributable; your users must install MSFS SDK or obtain the DLLs themselves. See FsConnect
> (c-true/FsConnect on GitHub) for a NuGet package approach that bundles the binaries under their
> interpretation of the SDK EULA.

## Step 1 — Connect

SimConnect uses Win32 window messages for its callback mechanism. In WPF you need an `HwndSource`
or pass a hidden window handle.

```csharp
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.Windows.Interop;

private const int WM_USER_SIMCONNECT = 0x0402;
private SimConnect _sc;
private HwndSource _hwndSource;

// In Window.Loaded (or constructor after InitializeComponent):
void ConnectToSim()
{
    // Obtain a Win32 HWND for the WPF window
    var helper = new WindowInteropHelper(this);
    helper.EnsureHandle();
    _hwndSource = HwndSource.FromHwnd(helper.Handle);
    _hwndSource.AddHook(WndProc);

    try
    {
        _sc = new SimConnect("MyApp", helper.Handle, WM_USER_SIMCONNECT, null, 0);
        _sc.OnRecvOpen  += OnOpen;
        _sc.OnRecvQuit  += OnQuit;
        _sc.OnRecvException += OnException;
    }
    catch (COMException ex)
    {
        // Sim not running — retry after a timer
        Console.WriteLine($"SimConnect unavailable: {ex.Message}");
    }
}

// WPF message hook
IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
{
    if (msg == WM_USER_SIMCONNECT)
    {
        _sc?.ReceiveMessage();   // dispatches all pending callbacks
        handled = true;
    }
    return IntPtr.Zero;
}
```

> **Sidecar without a visible window**: create a hidden `Window`, call `Show()` then `Hide()`,
> and grab its handle. Or use a background timer calling `_sc.ReceiveMessage()` on the UI thread.

## Step 2 — Subscribe to SimVars (SIMCONNECT_DATA_DEFINITION)

Define an enum for definition IDs and request IDs; use a struct with `SequentialLayout`.

```csharp
enum Defs { FlightData }
enum Reqs { FlightData }

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
struct FlightData
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Title;         // aircraft title
    public double AltitudeFt;
    public double IasMph;
    public double OnGround;      // 1.0 = on ground
}

void OnOpen(SimConnect sc, SIMCONNECT_RECV_OPEN data)
{
    // Register each field in struct order
    sc.AddToDataDefinition(Defs.FlightData, "Title",               null,    SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
    sc.AddToDataDefinition(Defs.FlightData, "Indicated Altitude",  "feet",  SIMCONNECT_DATATYPE.FLOAT64,  0, SimConnect.SIMCONNECT_UNUSED);
    sc.AddToDataDefinition(Defs.FlightData, "Airspeed Indicated",  "knots", SIMCONNECT_DATATYPE.FLOAT64,  0, SimConnect.SIMCONNECT_UNUSED);
    sc.AddToDataDefinition(Defs.FlightData, "Sim On Ground",       "bool",  SIMCONNECT_DATATYPE.FLOAT64,  0, SimConnect.SIMCONNECT_UNUSED);
    sc.RegisterDataDefineStruct<FlightData>(Defs.FlightData);

    // Subscribe: SECOND period, only on change
    sc.RequestDataOnSimObject(
        Reqs.FlightData,
        Defs.FlightData,
        SimConnect.SIMCONNECT_OBJECT_ID_USER,
        SIMCONNECT_PERIOD.SECOND,
        SIMCONNECT_DATA_REQUEST_FLAG.CHANGED,
        0, 0, 0);

    sc.OnRecvSimobjectData += OnData;
}

void OnData(SimConnect sc, SIMCONNECT_RECV_SIMOBJECT_DATA data)
{
    if (data.dwRequestID == (uint)Reqs.FlightData)
    {
        var fd = (FlightData)data.dwData[0];
        Console.WriteLine($"{fd.Title} @ {fd.AltitudeFt:F0} ft, {fd.IasMph:F0} kts");
    }
}
```

## Step 3 — Subscribe to System Events

```csharp
enum SysEvts { Sim, AircraftLoaded, Crash, Pause }

void OnOpen(SimConnect sc, SIMCONNECT_RECV_OPEN data)
{
    // ...data definitions above...

    sc.SubscribeToSystemEvent(SysEvts.Sim,             "Sim");           // 0=paused/stopped, 1=running
    sc.SubscribeToSystemEvent(SysEvts.AircraftLoaded,  "AircraftLoaded");
    sc.SubscribeToSystemEvent(SysEvts.Crash,           "Crashed");
    sc.SubscribeToSystemEvent(SysEvts.Pause,           "Paused");

    sc.OnRecvEvent         += OnEvent;
    sc.OnRecvEventFilename += OnFilenameEvent;
}

void OnEvent(SimConnect sc, SIMCONNECT_RECV_EVENT data)
{
    switch ((SysEvts)data.uEventID)
    {
        case SysEvts.Sim:    Console.WriteLine($"Sim running: {data.dwData == 1}"); break;
        case SysEvts.Crash:  Console.WriteLine("Aircraft crashed"); break;
        case SysEvts.Pause:  Console.WriteLine($"Paused: {data.dwData == 1}"); break;
    }
}

void OnFilenameEvent(SimConnect sc, SIMCONNECT_RECV_EVENT_FILENAME data)
{
    if ((SysEvts)data.uEventID == SysEvts.AircraftLoaded)
        Console.WriteLine($"Aircraft loaded: {data.szFileName}");
}
```

## Step 4 — Trigger Failures

```csharp
enum FailEvts { Engine1, Engine2, Hydraulic, Electrical, Vacuum, Pitot, StaticPort,
                BrakeTotal, BrakeLeft, BrakeRight, Engine3, Engine4 }

void OnOpen(SimConnect sc, SIMCONNECT_RECV_OPEN data)
{
    // ...other setup...

    // Map local enum IDs → sim event names
    sc.MapClientEventToSimEvent(FailEvts.Engine1,    "TOGGLE_ENGINE1_FAILURE");
    sc.MapClientEventToSimEvent(FailEvts.Engine2,    "TOGGLE_ENGINE2_FAILURE");
    sc.MapClientEventToSimEvent(FailEvts.Engine3,    "TOGGLE_ENGINE3_FAILURE");
    sc.MapClientEventToSimEvent(FailEvts.Engine4,    "TOGGLE_ENGINE4_FAILURE");
    sc.MapClientEventToSimEvent(FailEvts.Hydraulic,  "TOGGLE_HYDRAULIC_FAILURE");
    sc.MapClientEventToSimEvent(FailEvts.Electrical, "TOGGLE_ELECTRICAL_FAILURE");
    sc.MapClientEventToSimEvent(FailEvts.Vacuum,     "TOGGLE_VACUUM_FAILURE");
    sc.MapClientEventToSimEvent(FailEvts.Pitot,      "TOGGLE_PITOT_BLOCKAGE");
    sc.MapClientEventToSimEvent(FailEvts.StaticPort, "TOGGLE_STATIC_PORT_BLOCKAGE");
    sc.MapClientEventToSimEvent(FailEvts.BrakeTotal, "TOGGLE_TOTAL_BRAKE_FAILURE");
    sc.MapClientEventToSimEvent(FailEvts.BrakeLeft,  "TOGGLE_LEFT_BRAKE_FAILURE");
    sc.MapClientEventToSimEvent(FailEvts.BrakeRight, "TOGGLE_RIGHT_BRAKE_FAILURE");
}

// Call to toggle a failure on/off
void TriggerFailure(FailEvts evt)
{
    _sc.TransmitClientEvent(
        SimConnect.SIMCONNECT_OBJECT_ID_USER,
        evt,
        0,
        SIMCONNECT_GROUP_PRIORITY.HIGHEST,
        SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
}
```

## Step 5 — Detect Simulator Running State

Poll after connection loss or use the `"Sim"` system event (Step 3).
On connect failure, retry on a timer:

```csharp
DispatcherTimer _retryTimer = new() { Interval = TimeSpan.FromSeconds(5) };

void StartRetry() {
    _retryTimer.Tick += (_, _) => {
        try { ConnectToSim(); _retryTimer.Stop(); }
        catch (COMException) { /* still not running */ }
    };
    _retryTimer.Start();
}

void OnQuit(SimConnect sc, SIMCONNECT_RECV data)
{
    _sc?.Dispose(); _sc = null;
    StartRetry();
}
```

## Common Pitfalls

| Issue | Fix |
|-------|-----|
| `COMException 0xC000014B` on connect | MSFS not running; retry |
| Data corruption / struct misalign | Ensure `Pack = 1` and fields are in AddToDataDefinition order |
| String fields garbled | Use `[MarshalAs(UnmanagedType.ByValTStr, SizeConst=N)]` where N matches SIMCONNECT_DATATYPE |
| 32-bit build fails | SimConnect.dll is x64 only; set `<PlatformTarget>x64</PlatformTarget>` |
| Events not received | Call `ReceiveMessage()` on the UI thread only; don't call from a background thread |
| No WPF HWND available | Use `WindowInteropHelper.EnsureHandle()` before creating `SimConnect` |

## Sources

- Official managed code guide: https://docs.flightsimulator.com/html/Programming_Tools/SimConnect/Programming_SimConnect_Clients_using_Managed_Code.htm
- SimConnect SDK overview: https://docs.flightsimulator.com/html/Programming_Tools/SimConnect/SimConnect_SDK.htm
- SimVar reference: https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm
- CSharpSimConnect example (rolex20): https://github.com/rolex20/CSharpSimConnect
- FsConnect wrapper (c-true): https://github.com/c-true/FsConnect
- theflightsimdev.com tutorial: https://www.theflightsimdev.com/5-reading-simvar-data-with-simconnect/
